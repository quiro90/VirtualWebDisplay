using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Infrastructure.Runtime;

namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Maneja entrada de usuario desde cliente (toques t�ctiles de tablet).
/// Traduce eventos t�ctiles a clics de mouse en la pantalla virtual Parsec.
/// Usa mapeo directo de viewport al monitor virtual detectado.
/// Incluye rate limiting para proteger contra flooding de eventos.
/// </summary>
internal sealed class TouchInputHandler
{
    // Configuración de rate limiting
    private const int DEFAULT_MAX_EVENTS_PER_SECOND = 100;

    // Failsafe para evitar LEFTDOWN colgado si se pierde touchend en cliente/red.
    private const int DRAG_STALE_TIMEOUT_MS = 1200;

    private readonly IRuntimeAccessService _runtimeAccess;
    private readonly RateLimiterRegistry _rateLimiterRegistry;
    private readonly InputTelemetry _telemetry;
    private readonly DragStateTracker _dragState;
    private readonly ActionDispatcher _actionDispatcher;
    private readonly TouchInputCoordinateResolver _coordinateResolver;

    internal TouchInputHandler(IRuntimeAccessService runtimeAccess)
    {
        _runtimeAccess = runtimeAccess;
        _rateLimiterRegistry = new RateLimiterRegistry(DEFAULT_MAX_EVENTS_PER_SECOND);
        _telemetry = new InputTelemetry();
        _dragState = new DragStateTracker(DRAG_STALE_TIMEOUT_MS);
        _actionDispatcher = new ActionDispatcher(this);
        _coordinateResolver = new TouchInputCoordinateResolver();
    }

    /// <summary>
    /// POST /input/touch - Recibe eventos t�ctiles y los convierte en clics de mouse.
    /// Soporta tanto WebImage como WebRTC (ambos modos de transmisi�n).
    /// </summary>
    internal IResult HandleTouchInput(
        HttpContext ctx,
        TouchInputRequest request)
    {
        if (!TouchInputRequestValidator.TryValidate(request, out var validationError))
            return _runtimeAccess.BadRequestError(validationError);

        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
            return runtimeError!;

        // Gate de touch en tiempo real: si la app lo desactiva, ignoramos eventos aunque el cliente los siga enviando.
        if (!runtime.Config.TouchInputEnabled)
            return Results.NoContent();

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var viewerKey = _runtimeAccess.ResolveViewerKey(ctx, runtime);
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
            var action = TouchInputActions.NormalizeAction(request.Action);

            // -----------------------------------------------------------------------
            // MANEJO ESPECIAL DE FIN DE GESTOS (DRAGEND / SCROLLEND):
            // La prioridad absoluta es finalizar la interacci�n.
            // Las coordenadas pueden llegar nulas/incompletas cuando el dedo abandona
            // el viewport, lo que antes causaba 400 Bad Request. Ahora se toleran.
            // -----------------------------------------------------------------------
            if (_actionDispatcher.TryHandlePreCoordinateAction(action, runtime, out var preCoordinateResult))
                return preCoordinateResult;

            if (!_coordinateResolver.TryResolveDesktopCoordinates(request, runtime, out var desktopX, out var desktopY, out var coordError))
            {
                _telemetry.RegisterError();
                return _runtimeAccess.BadRequestError(coordError);
            }

            return _actionDispatcher.ExecutePostCoordinateAction(runtime, action, request, desktopX, desktopY, nowMs);
        }
        catch (Exception ex)
        {
            _telemetry.RegisterError();
            System.Diagnostics.Debug.WriteLine($"[InputHandler] Error: {ex.Message}");
            return _runtimeAccess.InternalServerErrorResult();
        }
    }

    private bool TryHandleGestureEndAction(string action, ScreenRuntimeContext runtime, out IResult result)
    {
        if (!TouchInputActions.IsGestureEndAction(action))
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

    private IResult HandleLegacyAction(TouchInputRequest request, int desktopX, int desktopY)
    {
        // Compatibilidad backward con clientes antiguos que solo enviaban Type.
        if (!ProcessLegacyEvent(request, desktopX, desktopY))
        {
            _telemetry.RegisterError();
            return _runtimeAccess.BadRequestError($"Unknown legacy type: {request.Type}");
        }

        return Results.Ok();
    }

    private IResult HandleSemanticAction(
        ScreenRuntimeContext runtime,
        string action,
        int desktopX,
        int desktopY,
        long nowMs,
        TouchInputRequest request)
    {
        switch (action)
        {
            case TouchInputActions.Tap:
                ExecutePointerAction(MouseClickType.Left, runtime, desktopX, desktopY);
                return Results.Ok();

            case TouchInputActions.RightClick:
                ExecutePointerAction(MouseClickType.Right, runtime, desktopX, desktopY);
                return Results.Ok();

            case TouchInputActions.MiddleClick:
                ExecutePointerAction(MouseClickType.Middle, runtime, desktopX, desktopY);
                return Results.Ok();

            case TouchInputActions.DragStart:
            case TouchInputActions.DragMove:
            case TouchInputActions.DragEnd:
                return ExecuteGestureAction(runtime, action, desktopX, desktopY, nowMs, request);

            case TouchInputActions.ScrollMove:
            case TouchInputActions.ScrollEnd:
                return ExecuteGestureAction(runtime, action, desktopX, desktopY, nowMs, request);

            default:
                _telemetry.RegisterError();
                return _runtimeAccess.BadRequestError($"Unknown action: {action}");
        }
    }

    private bool TryHandleDisabledSemanticAction(
        ScreenRuntimeContext runtime,
        string action,
        out IResult result)
    {
        if (TouchInputActions.IsDragAction(action) && !runtime.Config.TouchHoldEnabled)
        {
            result = Results.NoContent();
            return true;
        }

        if (TouchInputActions.IsScrollAction(action) && !runtime.Config.TouchScrollEnabled)
        {
            result = Results.NoContent();
            return true;
        }

        result = Results.Empty;
        return false;
    }

    private void ExecutePointerAction(
        MouseClickType clickType,
        ScreenRuntimeContext runtime,
        int desktopX,
        int desktopY)
    {
        EndDragIfActive();
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor && clickType == MouseClickType.Left);

        ExecuteClick(clickType, desktopX, desktopY, runtime.Config.TouchPreserveCursor);
    }

    private bool TryRejectByRateLimit(
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
            result = _runtimeAccess.TooManyRequestsResult();
            return true;
        }

        result = Results.Empty;
        return false;
    }

    /// <summary>
    /// GET /input/stats - Devuelve estad�sticas agregadas de entrada t�ctil.
    /// </summary>
    internal IResult HandleTouchStats(HttpContext ctx)
    {
        if (!_runtimeAccess.TryResolveAuthorizedRuntime(ctx, out var runtime, out var runtimeError))
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

    private TouchStatsSnapshot GetTouchStatsSnapshot(long nowMs) =>
        _telemetry.GetSnapshot(nowMs);

    /// <summary>
    /// Procesa evento touchstart:
    /// 1 dedo = click izquierdo, 2 dedos = click derecho, 3+ dedos = click central.
    /// </summary>
    private void ProcessTouchStart(int screenX, int screenY, int fingers)
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
    private void ProcessTouchMove()
    {
        // Intencionalmente no movemos el cursor real del PC durante touchmove.
        // Este comportamiento prioriza no desplazar el puntero local del usuario.
    }

    /// <summary>
    /// Procesa evento touchend: suelta el boton izquierdo si estaba presionado.
    /// </summary>
    private void ProcessTouchEnd()
    {
        EndDragIfActive();
        RestoreCursorIfNeeded(true);
    }

    private bool ProcessLegacyEvent(TouchInputRequest request, int desktopX, int desktopY)
    {
        var type = TouchInputActions.NormalizeLegacyType(request.Type);
        switch (type)
        {
            case TouchInputActions.LegacyTouchStart:
                ProcessTouchStart(desktopX, desktopY, request.Fingers);
                return true;
            case TouchInputActions.LegacyTouchMove:
                ProcessTouchMove();
                return true;
            case TouchInputActions.LegacyTouchEnd:
                ProcessTouchEnd();
                return true;
            default:
                return false;
        }
    }

    private void MarkDragStarted(long nowMs)
    {
        _dragState.MarkStarted(nowMs);
    }

    private void MarkDragActivity(long nowMs)
    {
        _dragState.MarkActivity(nowMs);
    }

    private void EndDragIfActive()
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
    private void ReleaseDragIfStale()
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_dragState.TryReleaseIfStale(nowMs))
        {
            MouseInputHelper.LeftUp();
            RestoreCursorIfNeeded(true);
            System.Diagnostics.Debug.WriteLine("[InputHandler] Failsafe: drag stale liberado por timeout.");
        }
    }

    private void SaveCursorIfNeeded(bool shouldPreserve)
    {
        if (shouldPreserve)
            MouseInputHelper.SaveCurrentCursorPosition();
    }

    private void RestoreCursorIfNeeded(bool shouldPreserve)
    {
        if (shouldPreserve)
            MouseInputHelper.RestoreLastCursorPosition();
    }

    /// <summary>
    /// Verifica si el cliente est� dentro del l�mite de eventos por segundo.
    /// Mantiene un rate limiter por cliente/sesi�n.
    /// </summary>
    private bool CheckRateLimit(string viewerKey)
    {
        return _rateLimiterRegistry.AllowRequest(viewerKey);
    }

    private void RegisterEvent(long nowMs)
    {
        _telemetry.RegisterEvent(nowMs);
    }

    private void RegisterRateLimitedEvent()
    {
        _telemetry.RegisterRateLimitedEvent();
    }

    private void RegisterLatency(long nowMs, long requestTimestamp)
    {
        _telemetry.RegisterLatency(nowMs, requestTimestamp);
    }

    /// <summary>
    /// Ejecuta un click del tipo especificado, eligiendo autom�ticamente entre
    /// el m�todo normal o el que preserva el cursor seg�n la configuraci�n.
    /// </summary>
    private void ExecuteClick(MouseClickType clickType, int x, int y, bool preserveCursor)
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
    private IResult ExecuteGestureAction(
        ScreenRuntimeContext runtime,
        string action,
        int desktopX,
        int desktopY,
        long nowMs,
        TouchInputRequest request)
    {
        switch (action)
        {
            case TouchInputActions.DragStart:
                ExecuteDragStart(runtime, desktopX, desktopY, nowMs);
                break;

            case TouchInputActions.DragMove:
                ExecuteDragMove(runtime, desktopX, desktopY, nowMs);
                break;

            case TouchInputActions.DragEnd:
                ExecuteDragEnd(runtime);
                break;

            case TouchInputActions.ScrollMove:
                ExecuteScrollMove(runtime, desktopX, desktopY, request);
                break;

            case TouchInputActions.ScrollEnd:
                ExecuteScrollEnd(runtime);
                break;
        }

        return Results.Ok();
    }

    private void ExecuteDragStart(ScreenRuntimeContext runtime, int desktopX, int desktopY, long nowMs)
    {
        // FIX: antes de iniciar un nuevo drag, liberar cualquier drag previo
        EndDragIfActive();
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor);
        MouseInputHelper.LeftDownAt(desktopX, desktopY);
        MarkDragStarted(nowMs);
    }

    private void ExecuteDragMove(ScreenRuntimeContext runtime, int desktopX, int desktopY, long nowMs)
    {
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor);
        MouseInputHelper.MoveMouse(desktopX, desktopY);
        MarkDragActivity(nowMs);
    }

    private void ExecuteDragEnd(ScreenRuntimeContext runtime)
    {
        EndDragIfActive();
        RestoreCursorIfNeeded(runtime.Config.TouchPreserveCursor);
    }

    private void ExecuteScrollMove(ScreenRuntimeContext runtime, int desktopX, int desktopY, TouchInputRequest request)
    {
        SaveCursorIfNeeded(runtime.Config.TouchPreserveCursor);
        MouseInputHelper.MoveMouse(desktopX, desktopY);
        int dy = (int)(request.ScrollDeltaY ?? 0.0);
        int dx = (int)(request.ScrollDeltaX ?? 0.0);
        // Scroll invertido: invertir dy respecto al comportamiento anterior, dx se mantiene.
        MouseInputHelper.Scroll(dy, dx);
    }

    private void ExecuteScrollEnd(ScreenRuntimeContext runtime)
    {
        EndDragIfActive();
        RestoreCursorIfNeeded(runtime.Config.TouchPreserveCursor);
    }

    private sealed class ActionDispatcher
    {
        private readonly TouchInputHandler _owner;

        internal ActionDispatcher(TouchInputHandler owner)
        {
            _owner = owner;
        }

        internal bool TryHandlePreCoordinateAction(
            string action,
            ScreenRuntimeContext runtime,
            out IResult result) =>
            _owner.TryHandleGestureEndAction(action, runtime, out result);

        internal IResult ExecutePostCoordinateAction(
            ScreenRuntimeContext runtime,
            string action,
            TouchInputRequest request,
            int desktopX,
            int desktopY,
            long nowMs)
        {
            if (_owner.TryHandleDisabledSemanticAction(runtime, action, out var disabledActionResult))
                return disabledActionResult;

            return string.IsNullOrEmpty(action)
                ? _owner.HandleLegacyAction(request, desktopX, desktopY)
                : _owner.HandleSemanticAction(runtime, action, desktopX, desktopY, nowMs, request);
        }
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
