using VirtualWebDisplay.Infrastructure;
using System.Windows.Forms;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja entrada de usuario desde cliente (toques t�ctiles de tablet).
/// Traduce eventos t�ctiles a clics de mouse en la pantalla virtual Parsec.
/// Usa mapeo directo de viewport al monitor virtual detectado.
/// Incluye rate limiting para proteger contra flooding de eventos.
/// </summary>
internal static class InputHandler
{
    private const string ActionTap = "tap";
    private const string ActionRightClick = "rightclick";
    private const string ActionMiddleClick = "middleclick";
    private const string ActionDragStart = "dragstart";
    private const string ActionDragMove = "dragmove";
    private const string ActionDragEnd = "dragend";
    private const string ActionScrollMove = "scrollmove";
    private const string ActionScrollEnd = "scrollend";

    private const string LegacyTypeTouchStart = "touchstart";
    private const string LegacyTypeTouchMove = "touchmove";
    private const string LegacyTypeTouchEnd = "touchend";

    // Configuraci�n de rate limiting
    private const int DEFAULT_MAX_EVENTS_PER_SECOND = 100;

    // Failsafe para evitar LEFTDOWN colgado si se pierde touchend en cliente/red.
    private const int DRAG_STALE_TIMEOUT_MS = 1200;

    private static readonly RateLimiterRegistry _rateLimiterRegistry = new(DEFAULT_MAX_EVENTS_PER_SECOND);
    private static readonly InputTelemetry _telemetry = new();
    private static readonly DragStateTracker _dragState = new(DRAG_STALE_TIMEOUT_MS);

    /// <summary>
    /// POST /input/touch - Recibe eventos t�ctiles y los convierte en clics de mouse.
    /// Soporta tanto WebImage como WebRTC (ambos modos de transmisi�n).
    /// </summary>
    internal static IResult HandleTouchInput(
        HttpContext ctx,
        TouchInputRequest request,
        IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        if (!TryValidateTouchRequest(request, out var validationError))
            return validationError;

        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out var runtime, out var runtimeError))
            return runtimeError!;

        // Gate de touch en tiempo real: si la app lo desactiva, ignoramos eventos aunque el cliente los siga enviando.
        if (!runtime.Config.TouchInputEnabled)
            return Results.NoContent();

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var viewerKey = RuntimeAccessHelper.ResolveViewerKey(ctx, runtime);
        if (TryRejectByRateLimit(request, nowMs, viewerKey, out var rateLimitResult))
            return rateLimitResult;

        // -----------------------------------------------------------------------
        // FAILSAFE PROACTIVO: antes de procesar cualquier evento nuevo, liberar
        // el bot�n si el drag qued� colgado por inactividad (red inestable,
        // p�rdida de eventos, nueva secuencia sin haber cerrado la anterior).
        // -----------------------------------------------------------------------
        ReleaseDragIfStale();

        try
        {
            // Determinar acci�n sem�ntica temprano para aplicar l�gica diferenciada.
            var action = NormalizeAction(request.Action);

            // -----------------------------------------------------------------------
            // MANEJO ESPECIAL DE FIN DE GESTOS (DRAGEND / SCROLLEND):
            // La prioridad absoluta es finalizar la interacci�n.
            // Las coordenadas pueden llegar nulas/incompletas cuando el dedo abandona
            // el viewport, lo que antes causaba 400 Bad Request. Ahora se toleran.
            // -----------------------------------------------------------------------
            if (ActionDispatcher.TryHandlePreCoordinateAction(action, runtime, out var preCoordinateResult))
                return preCoordinateResult;

            if (!TryResolveDesktopCoordinates(request, runtime, out var desktopX, out var desktopY, out var coordError))
            {
                _telemetry.RegisterError();
                return coordError;
            }

            return ActionDispatcher.ExecutePostCoordinateAction(runtime, action, request, desktopX, desktopY, nowMs);
        }
        catch (Exception ex)
        {
            _telemetry.RegisterError();
            System.Diagnostics.Debug.WriteLine($"[InputHandler] Error: {ex.Message}");
            return RuntimeAccessHelper.InternalServerErrorResult();
        }
    }

    private static bool TryValidateTouchRequest(TouchInputRequest request, out IResult errorResult)
    {
        if (request == null)
        {
            errorResult = RuntimeAccessHelper.BadRequestError("Request body required");
            return false;
        }

        if (string.IsNullOrEmpty(request.Type))
        {
            errorResult = RuntimeAccessHelper.BadRequestError("Type field required");
            return false;
        }

        errorResult = Results.Empty;
        return true;
    }

    private static bool TryHandleGestureEndAction(string action, ScreenRuntimeContext runtime, out IResult result)
    {
        if (!IsGestureEndAction(action))
        {
            result = Results.Empty;
            return false;
        }

        EndDragIfActive();
        if (runtime.Config.TouchPreserveCursor)
            MouseInputHelper.RestoreLastCursorPosition();

        System.Diagnostics.Debug.WriteLine($"[InputHandler] {action}: finalizado (coordenadas opcionales ignoradas).");
        result = Results.Ok();
        return true;
    }

    private static bool TryResolveDesktopCoordinates(
        TouchInputRequest request,
        ScreenRuntimeContext runtime,
        out int desktopX,
        out int desktopY,
        out IResult errorResult)
    {
        desktopX = 0;
        desktopY = 0;

        // Para el resto de acciones, las coordenadas son necesarias.
        // Si llegan nulas (cliente defectuoso), rechazar con 400 s�lo si no es dragend/scrollend.
        if (request.X is null || request.Y is null)
        {
            errorResult = RuntimeAccessHelper.BadRequestError("Coordinates X and Y are required for this action.");
            return false;
        }

        var targetBounds = ResolveTargetMonitorBounds(runtime);

        // Mapear coordenadas viewport ? pantalla virtual (considerando rotaci�n).
        // Usamos los valores con fallback seguro para ViewportWidth/Height.
        var (screenX, screenY) = MapCoordinates(
            request.X.Value,
            request.Y.Value,
            request.ViewportWidth ?? 1.0,
            request.ViewportHeight ?? 1.0,
            targetBounds.Width,
            targetBounds.Height);

        // Convertir coordenadas relativas del monitor virtual a coordenadas absolutas del escritorio.
        desktopX = targetBounds.Left + screenX;
        desktopY = targetBounds.Top + screenY;

        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] Bounds({targetBounds.Left},{targetBounds.Top},{targetBounds.Width}x{targetBounds.Height}) " +
            $"Config({runtime.Config.Width}x{runtime.Config.Height}) -> desktop({desktopX},{desktopY})");

        errorResult = Results.Empty;
        return true;
    }

    private static IResult HandleLegacyAction(TouchInputRequest request, int desktopX, int desktopY)
    {
        // Compatibilidad backward con clientes antiguos que solo enviaban Type.
        if (!ProcessLegacyEvent(request, desktopX, desktopY))
        {
            _telemetry.RegisterError();
            return RuntimeAccessHelper.BadRequestError($"Unknown legacy type: {request.Type}");
        }

        return Results.Ok();
    }

    private static IResult HandleSemanticAction(
        ScreenRuntimeContext runtime,
        string action,
        int desktopX,
        int desktopY,
        long nowMs,
        TouchInputRequest request)
    {
        switch (action)
        {
            case ActionTap:
                ExecutePointerAction(MouseClickType.Left, runtime, desktopX, desktopY);
                return Results.Ok();

            case ActionRightClick:
                ExecutePointerAction(MouseClickType.Right, runtime, desktopX, desktopY);
                return Results.Ok();

            case ActionMiddleClick:
                ExecutePointerAction(MouseClickType.Middle, runtime, desktopX, desktopY);
                return Results.Ok();

            case ActionDragStart:
            case ActionDragMove:
            case ActionDragEnd:
                return ExecuteGestureAction(runtime, action, desktopX, desktopY, nowMs, request);

            case ActionScrollMove:
            case ActionScrollEnd:
                return ExecuteGestureAction(runtime, action, desktopX, desktopY, nowMs, request);

            default:
                _telemetry.RegisterError();
                return RuntimeAccessHelper.BadRequestError($"Unknown action: {action}");
        }
    }

    private static bool TryHandleDisabledSemanticAction(
        ScreenRuntimeContext runtime,
        string action,
        out IResult result)
    {
        if (IsDragAction(action) && !runtime.Config.TouchHoldEnabled)
        {
            result = Results.NoContent();
            return true;
        }

        if (IsScrollAction(action) && !runtime.Config.TouchScrollEnabled)
        {
            result = Results.NoContent();
            return true;
        }

        result = Results.Empty;
        return false;
    }

    private static void ExecutePointerAction(
        MouseClickType clickType,
        ScreenRuntimeContext runtime,
        int desktopX,
        int desktopY)
    {
        EndDragIfActive();
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor && clickType == MouseClickType.Left);

        ExecuteClick(clickType, desktopX, desktopY, runtime.Config.TouchPreserveCursor);
    }

    private static bool TryRejectByRateLimit(
        TouchInputRequest request,
        long nowMs,
        string viewerKey,
        out IResult result)
    {
        RegisterEvent(nowMs);
        RegisterLatency(nowMs, request.Timestamp);

        if (!CheckRateLimit(viewerKey))
        {
            RegisterRateLimitedEvent();
            System.Diagnostics.Debug.WriteLine($"[InputHandler] Rate limit exceeded for viewer: {viewerKey}");
            result = RuntimeAccessHelper.TooManyRequestsResult();
            return true;
        }

        result = Results.Empty;
        return false;
    }

    /// <summary>
    /// GET /input/stats - Devuelve estad�sticas agregadas de entrada t�ctil.
    /// </summary>
    internal static IResult HandleTouchStats(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        if (!RuntimeAccessHelper.TryResolveAuthorizedRuntime(ctx, runtimes, out var runtime, out var runtimeError))
            return runtimeError!;

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var stats = GetTouchStatsSnapshot(nowMs);

        return Results.Json(new
        {
            totalEvents = stats.TotalEvents,
            totalErrors = stats.TotalErrors,
            rateLimitedEvents = stats.RateLimitedEvents,
            eventsPerSecond = stats.EventsPerSecond,
            avgLatencyMs = stats.AverageLatencyMs,
            lastInputAgoMs = stats.LastInputAgoMs,
            touchInputEnabled = runtime.Config.TouchInputEnabled
        });
    }

    private static TouchStatsSnapshot GetTouchStatsSnapshot(long nowMs) =>
        _telemetry.GetSnapshot(nowMs);

    /// <summary>
    /// Mapea coordenadas del viewport del navegador a coordenadas locales del monitor objetivo.
    /// Usa Math.Clamp para garantizar que los valores nunca excedan los l�mites del monitor,
    /// evitando coordenadas negativas o fuera de rango que causar�an comportamiento indefinido en Windows.
    /// </summary>
    private static (int screenX, int screenY) MapCoordinates(
        double viewportX,
        double viewportY,
        double viewportWidth,
        double viewportHeight,
        int screenWidth,
        int screenHeight)
    {
        var result = InputCoordinateMapper.Map(viewportX, viewportY, viewportWidth, viewportHeight, screenWidth, screenHeight);
        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] MapCoordinates: viewport({viewportX:F1},{viewportY:F1}) -> localScreen({result.screenX},{result.screenY})");
        return result;
    }

    /// <summary>
    /// Obtiene bounds reales del monitor virtual dentro del escritorio extendido.
    /// Si no se encuentra el monitor, usa PrimaryScreen como fallback seguro.
    /// </summary>
    private static System.Drawing.Rectangle ResolveTargetMonitorBounds(ScreenRuntimeContext runtime)
    {
        var screens = Screen.AllScreens;

        if (!string.IsNullOrWhiteSpace(runtime.DisplayManager.WindowsDeviceName))
        {
            var matchByName = screens.FirstOrDefault(s =>
                string.Equals(s.DeviceName, runtime.DisplayManager.WindowsDeviceName, StringComparison.OrdinalIgnoreCase));
            if (matchByName is not null)
                return matchByName.Bounds;
        }

        if (runtime.Config.MonitorIndex >= 0 && runtime.Config.MonitorIndex < screens.Length)
            return screens[runtime.Config.MonitorIndex].Bounds;

        return Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, runtime.Config.Width, runtime.Config.Height);
    }

    /// <summary>
    /// Procesa evento touchstart:
    /// 1 dedo = click izquierdo, 2 dedos = click derecho, 3+ dedos = click central.
    /// </summary>
    private static void ProcessTouchStart(int screenX, int screenY, int fingers)
    {
        if (fingers == 1)
        {
            // Un dedo: LEFTDOWN mantenido hasta touchend (permite press-and-hold)
            MouseInputHelper.LeftDownPreservingCursor(screenX, screenY);
            MarkDragStarted(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
        else if (fingers == 2)
        {
            // Dos dedos = click derecho, restaurando el cursor original del PC.
            EndDragIfActive();
            MouseInputHelper.RightClickPreservingCursor(screenX, screenY);
        }
        else if (fingers >= 3)
        {
            // Tres o mas dedos = click central, restaurando el cursor original del PC.
            EndDragIfActive();
            MouseInputHelper.MiddleClickPreservingCursor(screenX, screenY);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] TouchStart: ({screenX}, {screenY}), fingers={fingers}");
    }

    /// <summary>
    /// Procesa evento touchmove: mueve el cursor sin hacer click.
    /// </summary>
    private static void ProcessTouchMove()
    {
        // Intencionalmente no movemos el cursor real del PC durante touchmove.
        // Este comportamiento prioriza no desplazar el puntero local del usuario.
    }

    /// <summary>
    /// Procesa evento touchend: suelta el boton izquierdo si estaba presionado.
    /// </summary>
    private static void ProcessTouchEnd()
    {
        EndDragIfActive();
        RestoreCursorIfNeeded(true);
    }

    private static bool ProcessLegacyEvent(TouchInputRequest request, int desktopX, int desktopY)
    {
        var type = NormalizeLegacyType(request.Type);
        switch (type)
        {
            case LegacyTypeTouchStart:
                ProcessTouchStart(desktopX, desktopY, request.Fingers);
                return true;
            case LegacyTypeTouchMove:
                ProcessTouchMove();
                return true;
            case LegacyTypeTouchEnd:
                ProcessTouchEnd();
                return true;
            default:
                return false;
        }
    }

    private static void MarkDragStarted(long nowMs)
    {
        _dragState.MarkStarted(nowMs);
    }

    private static void MarkDragActivity(long nowMs)
    {
        _dragState.MarkActivity(nowMs);
    }

    private static void EndDragIfActive()
    {
        if (_dragState.TryEnd())
        {
            MouseInputHelper.LeftUp();
            System.Diagnostics.Debug.WriteLine("[InputHandler] EndDragIfActive: LeftUp ejecutado.");
        }
    }

    /// <summary>
    /// Libera el bot�n izquierdo si el drag lleva m�s de DRAG_STALE_TIMEOUT_MS ms sin actividad.
    /// Se llama al inicio de cada request para limpiar estado inconsistente por eventos perdidos.
    /// </summary>
    private static void ReleaseDragIfStale()
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_dragState.TryReleaseIfStale(nowMs))
        {
            MouseInputHelper.LeftUp();
            RestoreCursorIfNeeded(true);
            System.Diagnostics.Debug.WriteLine("[InputHandler] Failsafe: drag stale liberado por timeout.");
        }
    }

    private static void SaveCursorIfNeeded(bool shouldPreserve)
    {
        if (shouldPreserve)
            MouseInputHelper.SaveCurrentCursorPosition();
    }

    private static void RestoreCursorIfNeeded(bool shouldPreserve)
    {
        if (shouldPreserve)
            MouseInputHelper.RestoreLastCursorPosition();
    }

    /// <summary>
    /// Verifica si el cliente est� dentro del l�mite de eventos por segundo.
    /// Mantiene un rate limiter por cliente/sesi�n.
    /// </summary>
    private static bool CheckRateLimit(string viewerKey)
    {
        return _rateLimiterRegistry.AllowRequest(viewerKey);
    }

    private static void RegisterEvent(long nowMs)
    {
        _telemetry.RegisterEvent(nowMs);
    }

    private static void RegisterRateLimitedEvent()
    {
        _telemetry.RegisterRateLimitedEvent();
    }

    private static void RegisterLatency(long nowMs, long requestTimestamp)
    {
        _telemetry.RegisterLatency(nowMs, requestTimestamp);
    }

    /// <summary>
    /// Ejecuta un click del tipo especificado, eligiendo autom�ticamente entre
    /// el m�todo normal o el que preserva el cursor seg�n la configuraci�n.
    /// </summary>
    private static void ExecuteClick(MouseClickType clickType, int x, int y, bool preserveCursor)
    {
        switch (clickType)
        {
            case MouseClickType.Left:
                if (preserveCursor)
                    MouseInputHelper.LeftClickPreservingCursor(x, y);
                else
                    MouseInputHelper.LeftClick(x, y);
                break;

            case MouseClickType.Right:
                if (preserveCursor)
                    MouseInputHelper.RightClickPreservingCursor(x, y);
                else
                    MouseInputHelper.RightClick(x, y);
                break;

            case MouseClickType.Middle:
                if (preserveCursor)
                    MouseInputHelper.MiddleClickPreservingCursor(x, y);
                else
                    MouseInputHelper.MiddleClick(x, y);
                break;
        }
    }

    /// <summary>
    /// Ejecuta una acci�n de gesto (drag/scroll). Centraliza la l�gica repetitiva.
    /// </summary>
    private static IResult ExecuteGestureAction(
        ScreenRuntimeContext runtime,
        string action,
        int desktopX,
        int desktopY,
        long nowMs,
        TouchInputRequest request)
    {
        switch (action)
        {
            case ActionDragStart:
                ExecuteDragStart(runtime, desktopX, desktopY, nowMs);
                break;

            case ActionDragMove:
                ExecuteDragMove(runtime, desktopX, desktopY, nowMs);
                break;

            case ActionDragEnd:
                ExecuteDragEnd(runtime);
                break;

            case ActionScrollMove:
                ExecuteScrollMove(runtime, desktopX, desktopY, request);
                break;

            case ActionScrollEnd:
                ExecuteScrollEnd(runtime);
                break;
        }

        return Results.Ok();
    }

    private static void ExecuteDragStart(ScreenRuntimeContext runtime, int desktopX, int desktopY, long nowMs)
    {
        // FIX: antes de iniciar un nuevo drag, liberar cualquier drag previo
        EndDragIfActive();
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor);
        MouseInputHelper.LeftDownAt(desktopX, desktopY);
        MarkDragStarted(nowMs);
    }

    private static void ExecuteDragMove(ScreenRuntimeContext runtime, int desktopX, int desktopY, long nowMs)
    {
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor);
        MouseInputHelper.MoveMouse(desktopX, desktopY);
        MarkDragActivity(nowMs);
    }

    private static void ExecuteDragEnd(ScreenRuntimeContext runtime)
    {
        EndDragIfActive();
        RestoreCursorIfNeeded(runtime.Config.TouchPreserveCursor);
    }

    private static void ExecuteScrollMove(ScreenRuntimeContext runtime, int desktopX, int desktopY, TouchInputRequest request)
    {
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor);
        MouseInputHelper.MoveMouse(desktopX, desktopY);
        int dy = (int)(request.ScrollDeltaY ?? 0.0);
        int dx = (int)(request.ScrollDeltaX ?? 0.0);
        // Scroll invertido: invertir dy respecto al comportamiento anterior, dx se mantiene.
        MouseInputHelper.Scroll(dy, dx);
    }

    private static void ExecuteScrollEnd(ScreenRuntimeContext runtime)
    {
        EndDragIfActive();
        RestoreCursorIfNeeded(runtime.Config.TouchPreserveCursor);
    }

    private readonly record struct TouchStatsSnapshot(
        long TotalEvents,
        long TotalErrors,
        long RateLimitedEvents,
        int EventsPerSecond,
        double AverageLatencyMs,
        long LastInputAgoMs);

    private sealed class RateLimiterRegistry
    {
        private readonly int _maxEventsPerSecond;
        private readonly Dictionary<string, RateLimiter> _rateLimiters = new();
        private readonly object _lock = new();

        internal RateLimiterRegistry(int maxEventsPerSecond)
        {
            _maxEventsPerSecond = maxEventsPerSecond;
        }

        internal bool AllowRequest(string viewerKey)
        {
            if (string.IsNullOrEmpty(viewerKey))
                viewerKey = "default";

            lock (_lock)
            {
                if (!_rateLimiters.TryGetValue(viewerKey, out var limiter))
                {
                    limiter = new RateLimiter(_maxEventsPerSecond);
                    _rateLimiters[viewerKey] = limiter;
                }

                return limiter.AllowRequest();
            }
        }
    }

    private sealed class InputTelemetry
    {
        private long _totalEvents;
        private long _totalErrors;
        private long _rateLimitedEvents;
        private long _totalLatencyMs;
        private long _latencySamples;
        private long _lastInputUnixMs;
        private readonly Queue<long> _eventsWindowMs = new();
        private readonly object _statsLock = new();

        internal void RegisterEvent(long nowMs)
        {
            Interlocked.Increment(ref _totalEvents);
            Interlocked.Exchange(ref _lastInputUnixMs, nowMs);

            lock (_statsLock)
            {
                _eventsWindowMs.Enqueue(nowMs);
                PruneWindowLocked(nowMs);
            }
        }

        internal void RegisterError() => Interlocked.Increment(ref _totalErrors);

        internal void RegisterRateLimitedEvent() => Interlocked.Increment(ref _rateLimitedEvents);

        internal void RegisterLatency(long nowMs, long requestTimestamp)
        {
            if (requestTimestamp <= 0)
                return;

            var latency = nowMs - requestTimestamp;
            if (latency < 0 || latency > 60_000)
                return;

            Interlocked.Add(ref _totalLatencyMs, latency);
            Interlocked.Increment(ref _latencySamples);
        }

        internal TouchStatsSnapshot GetSnapshot(long nowMs) => new(
            TotalEvents: Interlocked.Read(ref _totalEvents),
            TotalErrors: Interlocked.Read(ref _totalErrors),
            RateLimitedEvents: Interlocked.Read(ref _rateLimitedEvents),
            EventsPerSecond: GetEventsPerSecond(nowMs),
            AverageLatencyMs: GetAverageLatencyMs(),
            LastInputAgoMs: GetLastInputAgoMs(nowMs));

        private int GetEventsPerSecond(long nowMs)
        {
            lock (_statsLock)
            {
                PruneWindowLocked(nowMs);
                return _eventsWindowMs.Count;
            }
        }

        private void PruneWindowLocked(long nowMs)
        {
            while (_eventsWindowMs.Count > 0 && nowMs - _eventsWindowMs.Peek() > 1000)
                _eventsWindowMs.Dequeue();
        }

        private double GetAverageLatencyMs()
        {
            var samples = Interlocked.Read(ref _latencySamples);
            if (samples <= 0)
                return 0;

            var totalLatency = Interlocked.Read(ref _totalLatencyMs);
            return Math.Round((double)totalLatency / samples, 1);
        }

        private long GetLastInputAgoMs(long nowMs)
        {
            var lastInput = Interlocked.Read(ref _lastInputUnixMs);
            if (lastInput <= 0)
                return -1;

            var delta = nowMs - lastInput;
            return delta < 0 ? 0 : delta;
        }
    }

    private sealed class DragStateTracker
    {
        private readonly int _staleTimeoutMs;
        private readonly object _lock = new();
        private bool _isActive;
        private long _lastActivityUnixMs;

        internal DragStateTracker(int staleTimeoutMs)
        {
            _staleTimeoutMs = staleTimeoutMs;
        }

        internal void MarkStarted(long nowMs)
        {
            lock (_lock)
            {
                _isActive = true;
                _lastActivityUnixMs = nowMs;
            }
        }

        internal void MarkActivity(long nowMs)
        {
            lock (_lock)
            {
                if (_isActive)
                    _lastActivityUnixMs = nowMs;
            }
        }

        internal bool TryEnd()
        {
            lock (_lock)
            {
                var shouldRelease = _isActive;
                _isActive = false;
                _lastActivityUnixMs = 0;
                return shouldRelease;
            }
        }

        internal bool TryReleaseIfStale(long nowMs)
        {
            lock (_lock)
            {
                var shouldRelease = _isActive && (nowMs - _lastActivityUnixMs) >= _staleTimeoutMs;
                if (!shouldRelease)
                    return false;

                _isActive = false;
                _lastActivityUnixMs = 0;
                return true;
            }
        }
    }

    private static class ActionDispatcher
    {
        internal static bool TryHandlePreCoordinateAction(
            string action,
            ScreenRuntimeContext runtime,
            out IResult result) =>
            TryHandleGestureEndAction(action, runtime, out result);

        internal static IResult ExecutePostCoordinateAction(
            ScreenRuntimeContext runtime,
            string action,
            TouchInputRequest request,
            int desktopX,
            int desktopY,
            long nowMs)
        {
            if (TryHandleDisabledSemanticAction(runtime, action, out var disabledActionResult))
                return disabledActionResult;

            return string.IsNullOrEmpty(action)
                ? HandleLegacyAction(request, desktopX, desktopY)
                : HandleSemanticAction(runtime, action, desktopX, desktopY, nowMs, request);
        }
    }

    private static string NormalizeAction(string? action) =>
        (action ?? string.Empty).ToLowerInvariant();

    private static string NormalizeLegacyType(string? type) =>
        (type ?? string.Empty).ToLowerInvariant();

    private static bool IsDragAction(string action) =>
        action is ActionDragStart or ActionDragMove or ActionDragEnd;

    private static bool IsScrollAction(string action) =>
        action is ActionScrollMove or ActionScrollEnd;

    private static bool IsGestureEndAction(string action) =>
        action is ActionDragEnd or ActionScrollEnd;

    /// <summary>
    /// Enum para identificar el tipo de click de mouse.
    /// </summary>
    private enum MouseClickType
    {
        Left,
        Right,
        Middle
    }
}