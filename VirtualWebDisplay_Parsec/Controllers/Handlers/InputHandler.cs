using VirtualWebDisplay.Infrastructure;
using System.Windows.Forms;

namespace VirtualWebDisplay.Controllers.Handlers;

/// <summary>
/// Maneja entrada de usuario desde cliente (toques táctiles de tablet).
/// Traduce eventos táctiles a clics de mouse en la pantalla virtual Parsec.
/// Usa mapeo directo de viewport al monitor virtual detectado.
/// Incluye rate limiting para proteger contra flooding de eventos.
/// </summary>
internal static class InputHandler
{
    // Rate limiters por cliente/sesión (viewerKey)
    private static readonly Dictionary<string, RateLimiter> _rateLimiters = new();
    private static readonly object _rateLimiterLock = new object();

    // Configuración de rate limiting
    private const int DEFAULT_MAX_EVENTS_PER_SECOND = 100;

    // Estadísticas básicas de entrada táctil (Sprint 2)
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
    private static readonly System.Threading.Timer _dragSafetyTimer = new(_ => ReleaseDragIfStale(), null, 500, 200);

    /// <summary>
    /// POST /input/touch - Recibe eventos táctiles y los convierte en clics de mouse.
    /// Soporta tanto WebImage como WebRTC (ambos modos de transmisión).
    /// </summary>
    internal static IResult HandleTouchInput(
        HttpContext ctx,
        TouchInputRequest request,
        IReadOnlyList<ScreenRuntimeContext> runtimes)
    {
        // Validación básica
        if (request == null)
            return Results.BadRequest(new { error = "Request body required" });

        if (string.IsNullOrEmpty(request.Type))
            return Results.BadRequest(new { error = "Type field required" });

        // Resolver runtime y verificar autorización
        var runtime = RuntimeAccessHelper.ResolveRuntime(ctx, runtimes);

        if (!RuntimeAccessHelper.IsAuthorized(ctx, runtime))
            return RuntimeAccessHelper.UnauthorizedResult(runtime);

        // Gate de touch en tiempo real: si la app lo desactiva, ignoramos eventos aunque el cliente los siga enviando.
        if (!runtime.Config.TouchInputEnabled)
            return Results.NoContent();

        // Registrar telemetría básica por evento
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RegisterEvent(nowMs);
        RegisterLatency(nowMs, request.Timestamp);

        // Rate limiting por cliente/sesión
        var viewerKey = RuntimeAccessHelper.ResolveViewerKey(ctx, runtime);
        if (!CheckRateLimit(viewerKey))
        {
            RegisterRateLimitedEvent();
            System.Diagnostics.Debug.WriteLine($"[InputHandler] Rate limit exceeded for viewer: {viewerKey}");
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        try
        {
            var targetBounds = ResolveTargetMonitorBounds(runtime);

            // Mapear coordenadas viewport → pantalla virtual (considerando rotación)
            var (screenX, screenY) = MapCoordinates(
                request.X,
                request.Y,
                request.ViewportWidth,
                request.ViewportHeight,
                targetBounds.Width,
                targetBounds.Height);

            // Convertir coordenadas relativas del monitor virtual a coordenadas absolutas del escritorio.
            var desktopX = targetBounds.Left + screenX;
            var desktopY = targetBounds.Top + screenY;

            System.Diagnostics.Debug.WriteLine(
                $"[InputHandler] Bounds({targetBounds.Left},{targetBounds.Top},{targetBounds.Width}x{targetBounds.Height}) " +
                $"Config({runtime.Config.Width}x{runtime.Config.Height}) -> desktop({desktopX},{desktopY})");

            // Procesar segun accion semantica (decidida por el cliente JS)
            var action = (request.Action ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
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
                    MouseInputHelper.LeftClickPreservingCursor(desktopX, desktopY);
                    break;
                case "rightclick":
                    EndDragIfActive();
                    MouseInputHelper.RightClickPreservingCursor(desktopX, desktopY);
                    break;
                case "middleclick":
                    EndDragIfActive();
                    MouseInputHelper.MiddleClickPreservingCursor(desktopX, desktopY);
                    break;
                case "dragstart":
                    MouseInputHelper.LeftDownAt(desktopX, desktopY);
                    MarkDragStarted(nowMs);
                    break;
                case "dragmove":
                    MouseInputHelper.MoveMouse(desktopX, desktopY);
                    MarkDragActivity(nowMs);
                    break;
                case "dragend":
                    EndDragIfActive();
                    break;
                case "scrollmove":
                    EndDragIfActive();
                    MouseInputHelper.ScrollWheel((int)request.ScrollDeltaY);
                    break;
                case "scrollend":
                    EndDragIfActive();
                    break;
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
    /// GET /input/stats - Devuelve estadísticas agregadas de entrada táctil.
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

        // Clamp a [0, 1] para evitar coordenadas inválidas
        normX = Math.Clamp(normX, 0, 1);
        normY = Math.Clamp(normY, 0, 1);

        // Paso 2: Mapear directo a resolución local del monitor.
        int screenX = (int)Math.Round(normX * Math.Max(1, screenWidth - 1));
        int screenY = (int)Math.Round(normY * Math.Max(1, screenHeight - 1));

        // Asegurar que están dentro de límites válidos
        screenX = Math.Clamp(screenX, 0, screenWidth - 1);
        screenY = Math.Clamp(screenY, 0, screenHeight - 1);

        System.Diagnostics.Debug.WriteLine(
            $"[InputHandler] MapCoordinates: viewport({viewportX:F1},{viewportY:F1}) → localScreen({screenX},{screenY})");

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
    private static void ProcessTouchMove(int screenX, int screenY)
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
                ProcessTouchMove(desktopX, desktopY);
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
            MouseInputHelper.LeftUp();
    }

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
            System.Diagnostics.Debug.WriteLine("[InputHandler] Drag failsafe released stale LEFTDOWN");
        }
    }

    /// <summary>
    /// Verifica si el cliente está dentro del límite de eventos por segundo.
    /// Mantiene un rate limiter por cliente/sesión.
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
}
