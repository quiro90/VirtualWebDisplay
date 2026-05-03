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
    // Rate limiters por cliente/sesi�n (viewerKey)
    private static readonly Dictionary<string, RateLimiter> _rateLimiters = new();
    private static readonly object _rateLimiterLock = new object();

    // Configuraci�n de rate limiting
    private const int DEFAULT_MAX_EVENTS_PER_SECOND = 100;

    // mouse virtual
    private static int _virtualX;
    private static int _virtualY;
    private static bool _virtualInitialized;

    // Estad�sticas b�sicas de entrada t�ctil (Sprint 2)
    private static long _totalEvents;
    private static long _totalErrors;
    private static long _rateLimitedEvents;
    private static long _totalLatencyMs;
    private static long _latencySamples;
    private static long _lastInputUnixMs;
    private static readonly Queue<long> _eventsWindowMs = new();
    private static readonly object _statsLock = new object();

    // Failsafe para evitar LEFTDOWN colgado si se pierde touchend en cliente/red.
    private static readonly object _dragStateLock = new object();
    private static bool _dragIsActive;
    private static long _dragLastActivityUnixMs;
    private const int DRAG_STALE_TIMEOUT_MS = 1200;

    private static ScreenRuntimeContext? _runtime = null;

    /// <summary>
    /// POST /input/touch - Recibe eventos t�ctiles y los convierte en clics de mouse.
    /// Soporta tanto WebImage como WebRTC (ambos modos de transmisi�n).
    /// </summary>
    internal static IResult HandleTouchInput(
        HttpContext ctx,
        TouchInputRequest request,
        IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        // Validaci�n b�sica
        if (request == null)
            return Results.BadRequest(new { error = "Request body required" });

        if (string.IsNullOrEmpty(request.Type))
            return Results.BadRequest(new { error = "Type field required" });

        // Resolver runtime y verificar autorizaci�n
        _runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!RuntimeAccessHelper.IsAuthorized(ctx, _runtime))
            return RuntimeAccessHelper.UnauthorizedResult(_runtime);

        // Gate de touch en tiempo real: si la app lo desactiva, ignoramos eventos aunque el cliente los siga enviando.
        if (!_runtime.Config.TouchInputEnabled)
            return Results.NoContent();

        // Registrar telemetr�a b�sica por evento
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RegisterEvent(nowMs);
        RegisterLatency(nowMs, request.Timestamp);

        // Rate limiting por cliente/sesi�n
        var viewerKey = RuntimeAccessHelper.ResolveViewerKey(ctx, _runtime);
        if (!CheckRateLimit(viewerKey))
        {
            RegisterRateLimitedEvent();
            System.Diagnostics.Debug.WriteLine($"[InputHandler] Rate limit exceeded for viewer: {viewerKey}");
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        // -----------------------------------------------------------------------
        // FAILSAFE PROACTIVO: antes de procesar cualquier evento nuevo, liberar
        // el bot�n si el drag qued� colgado por inactividad (red inestable,
        // p�rdida de eventos, nueva secuencia sin haber cerrado la anterior).
        // -----------------------------------------------------------------------
        ReleaseDragIfStale();

        try
        {
            // Determinar acci�n sem�ntica temprano para aplicar l�gica diferenciada.
            var action = (request.Action ?? string.Empty).ToLowerInvariant();

            // -----------------------------------------------------------------------
            // MANEJO ESPECIAL DE FIN DE GESTOS (DRAGEND / SCROLLEND):
            // La prioridad absoluta es finalizar la interacci�n.
            // Las coordenadas pueden llegar nulas/incompletas cuando el dedo abandona
            // el viewport, lo que antes causaba 400 Bad Request. Ahora se toleran.
            // -----------------------------------------------------------------------
            if (action == "dragend" || action == "scrollend")
            {
                EndDragIfActive();
                if (_runtime.Config.TouchPreserveCursor)
                {
                    MouseInputHelper.RestoreLastCursorPosition();
                }
                System.Diagnostics.Debug.WriteLine($"[InputHandler] {action}: finalizado (coordenadas opcionales ignoradas).");
                return Results.Ok();
            }

            // Para el resto de acciones, las coordenadas son necesarias.
            // Si llegan nulas (cliente defectuoso), rechazar con 400 s�lo si no es dragend.
            if (request.X is null || request.Y is null)
            {
                RegisterError();
                return Results.BadRequest(new { error = "Coordinates X and Y are required for this action." });
            }

            var targetBounds = ResolveTargetMonitorBounds(_runtime);

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
            var desktopX = targetBounds.Left + screenX;
            var desktopY = targetBounds.Top + screenY;

            _virtualX = desktopX;
            _virtualY = desktopY;
            _virtualInitialized = true;

            System.Diagnostics.Debug.WriteLine(
                $"[InputHandler] Bounds({targetBounds.Left},{targetBounds.Top},{targetBounds.Width}x{targetBounds.Height}) " +
                $"Config({_runtime.Config.Width}x{_runtime.Config.Height}) -> desktop({desktopX},{desktopY})");

            // Procesar segun accion semantica (decidida por el cliente JS)
            if (string.IsNullOrEmpty(action))
            {
                // Compatibilidad backward con clientes antiguos que solo enviaban Type.
                if (!ProcessLegacyEvent(request, desktopX, desktopY))
                {
                    RegisterError();
                    return Results.BadRequest(new { error = $"Unknown legacy type: {request.Type}" });
                }

                return Results.Ok();
            }

            switch (action)
            {
                case "tap":
                    EndDragIfActive();
                    if (_runtime.Config.TouchPreserveCursor)
                        MouseInputHelper.SaveCurrentCursorPosition();
                    ExecuteClick(MouseClickType.Left, _virtualX, _virtualY, _runtime.Config.TouchPreserveCursor);
                    break;

                case "rightclick":
                    EndDragIfActive();
                    ExecuteClick(MouseClickType.Right, _virtualX, _virtualY, _runtime.Config.TouchPreserveCursor);
                    break;

                case "middleclick":
                    EndDragIfActive();
                    ExecuteClick(MouseClickType.Middle, _virtualX, _virtualY, _runtime.Config.TouchPreserveCursor);
                    break;

                case "dragstart":
                case "dragmove":
                case "dragend":
                    if (!_runtime.Config.TouchHoldEnabled)
                        return Results.NoContent();
                    return ExecuteGestureAction(action, nowMs, request);

                case "scrollmove":
                case "scrollend":
                    if (!_runtime.Config.TouchScrollEnabled)
                        return Results.NoContent();
                    return ExecuteGestureAction(action, nowMs, request);

                default:
                    RegisterError();
                    return Results.BadRequest(new { error = $"Unknown action: {action}" });
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            RegisterError();
            System.Diagnostics.Debug.WriteLine($"[InputHandler] Error: {ex.Message}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// GET /input/stats - Devuelve estad�sticas agregadas de entrada t�ctil.
    /// </summary>
    internal static IResult HandleTouchStats(HttpContext ctx, IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var eps = GetEventsPerSecond(nowMs);
        var lastInputAgoMs = GetLastInputAgoMs(nowMs);
        var avgLatencyMs = GetAverageLatencyMs();
        var totalEvents = Interlocked.Read(ref _totalEvents);
        var totalErrors = Interlocked.Read(ref _totalErrors);
        var rateLimitedEvents = Interlocked.Read(ref _rateLimitedEvents);

        return Results.Json(new
        {
            totalEvents,
            totalErrors,
            rateLimitedEvents,
            eventsPerSecond = eps,
            avgLatencyMs,
            lastInputAgoMs,
            touchInputEnabled = runtime.Config.TouchInputEnabled
        });
    }

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
        // Paso 1: Normalizar coordenadas viewport a [0, 1]
        double normX = viewportWidth > 0 ? viewportX / viewportWidth : 0;
        double normY = viewportHeight > 0 ? viewportY / viewportHeight : 0;

        // Clamp a [0, 1] para evitar coordenadas inv�lidas (dedo fuera de viewport)
        normX = Math.Clamp(normX, 0.0, 1.0);
        normY = Math.Clamp(normY, 0.0, 1.0);

        // Paso 2: Mapear directo a resoluci�n local del monitor.
        int screenX = (int)Math.Round(normX * Math.Max(1, screenWidth - 1));
        int screenY = (int)Math.Round(normY * Math.Max(1, screenHeight - 1));

        // Asegurar que est�n dentro de l�mites v�lidos (defensa en profundidad)
        screenX = Math.Clamp(screenX, 0, Math.Max(0, screenWidth - 1));
        screenY = Math.Clamp(screenY, 0, Math.Max(0, screenHeight - 1));

        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] MapCoordinates: viewport({viewportX:F1},{viewportY:F1}) ? localScreen({screenX},{screenY})");

        return (screenX, screenY);
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
        MouseInputHelper.RestoreLastCursorPosition();
    }

    private static bool ProcessLegacyEvent(TouchInputRequest request, int desktopX, int desktopY)
    {
        var type = (request.Type ?? string.Empty).ToLowerInvariant();
        switch (type)
        {
            case "touchstart":
                ProcessTouchStart(desktopX, desktopY, request.Fingers);
                return true;
            case "touchmove":
                ProcessTouchMove();
                return true;
            case "touchend":
                ProcessTouchEnd();
                return true;
            default:
                return false;
        }
    }

    private static void MarkDragStarted(long nowMs)
    {
        lock (_dragStateLock)
        {
            _dragIsActive = true;
            _dragLastActivityUnixMs = nowMs;
        }
    }

    private static void MarkDragActivity(long nowMs)
    {
        lock (_dragStateLock)
        {
            if (_dragIsActive)
                _dragLastActivityUnixMs = nowMs;
        }
    }

    private static void EndDragIfActive()
    {
        bool shouldRelease;
        lock (_dragStateLock)
        {
            shouldRelease = _dragIsActive;
            _dragIsActive = false;
            _dragLastActivityUnixMs = 0;
        }

        if (shouldRelease)
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
        bool shouldRelease;

        lock (_dragStateLock)
        {
            shouldRelease = _dragIsActive && (nowMs - _dragLastActivityUnixMs) >= DRAG_STALE_TIMEOUT_MS;
            if (shouldRelease)
            {
                _dragIsActive = false;
                _dragLastActivityUnixMs = 0;
            }
        }

        if (shouldRelease)
        {
            MouseInputHelper.LeftUp();
            MouseInputHelper.RestoreLastCursorPosition();
            System.Diagnostics.Debug.WriteLine("[InputHandler] Failsafe: drag stale liberado por timeout.");
        }
    }

    /// <summary>
    /// Verifica si el cliente est� dentro del l�mite de eventos por segundo.
    /// Mantiene un rate limiter por cliente/sesi�n.
    /// </summary>
    private static bool CheckRateLimit(string viewerKey)
    {
        if (string.IsNullOrEmpty(viewerKey))
            viewerKey = "default";

        lock (_rateLimiterLock)
        {
            if (!_rateLimiters.TryGetValue(viewerKey, out var limiter))
            {
                limiter = new RateLimiter(DEFAULT_MAX_EVENTS_PER_SECOND);
                _rateLimiters[viewerKey] = limiter;
            }

            return limiter.AllowRequest();
        }
    }

    private static void RegisterEvent(long nowMs)
    {
        Interlocked.Increment(ref _totalEvents);
        Interlocked.Exchange(ref _lastInputUnixMs, nowMs);

        lock (_statsLock)
        {
            _eventsWindowMs.Enqueue(nowMs);
            PruneWindowLocked(nowMs);
        }
    }

    private static void RegisterError()
    {
        Interlocked.Increment(ref _totalErrors);
    }

    private static void RegisterRateLimitedEvent()
    {
        Interlocked.Increment(ref _rateLimitedEvents);
    }

    private static void RegisterLatency(long nowMs, long requestTimestamp)
    {
        if (requestTimestamp <= 0)
            return;

        var latency = nowMs - requestTimestamp;
        if (latency < 0 || latency > 60_000)
            return;

        Interlocked.Add(ref _totalLatencyMs, latency);
        Interlocked.Increment(ref _latencySamples);
    }

    private static int GetEventsPerSecond(long nowMs)
    {
        lock (_statsLock)
        {
            PruneWindowLocked(nowMs);
            return _eventsWindowMs.Count;
        }
    }

    private static void PruneWindowLocked(long nowMs)
    {
        while (_eventsWindowMs.Count > 0 && nowMs - _eventsWindowMs.Peek() > 1000)
            _eventsWindowMs.Dequeue();
    }

    private static double GetAverageLatencyMs()
    {
        var samples = Interlocked.Read(ref _latencySamples);
        if (samples <= 0)
            return 0;

        var totalLatency = Interlocked.Read(ref _totalLatencyMs);
        return Math.Round((double)totalLatency / samples, 1);
    }

    private static long GetLastInputAgoMs(long nowMs)
    {
        var lastInput = Interlocked.Read(ref _lastInputUnixMs);
        if (lastInput <= 0)
            return -1;

        var delta = nowMs - lastInput;
        return delta < 0 ? 0 : delta;
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
    private static IResult ExecuteGestureAction(string action, long nowMs, TouchInputRequest request)
    {
        if (_runtime == null) return Results.Problem();

        switch (action)
        {
            case "dragstart":
                // FIX: antes de iniciar un nuevo drag, liberar cualquier drag previo
                EndDragIfActive();
                if (_runtime.Config.TouchPreserveCursor)
                    MouseInputHelper.SaveCurrentCursorPosition();
                MouseInputHelper.LeftDownAt(_virtualX, _virtualY);
                MarkDragStarted(nowMs);
                break;

            case "dragmove":
                if (_runtime.Config.TouchPreserveCursor)
                    MouseInputHelper.SaveCurrentCursorPosition();
                MouseInputHelper.MoveMouse(_virtualX, _virtualY);
                MarkDragActivity(nowMs);
                break;

            case "dragend":
                EndDragIfActive();
                // Restaurar puntero solo si TouchPreserveCursor est� activo
                if (_runtime.Config.TouchPreserveCursor)
                {
                    MouseInputHelper.RestoreLastCursorPosition();
                }
                break;

            case "scrollmove":
                if (_runtime.Config.TouchPreserveCursor)
                    MouseInputHelper.SaveCurrentCursorPosition();
                MouseInputHelper.MoveMouse(_virtualX, _virtualY);
                int dy = (int)(request.ScrollDeltaY ?? 0.0);
                int dx = (int)(request.ScrollDeltaX ?? 0.0);
                // Se invierte dy para un comportamiento natural, dx se mantiene
                MouseInputHelper.Scroll(-dy, dx);
                break;

            case "scrollend":
                EndDragIfActive();
                if (_runtime.Config.TouchPreserveCursor)
                {
                    MouseInputHelper.RestoreLastCursorPosition();
                }
                break;
        }

        return Results.Ok();
    }

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