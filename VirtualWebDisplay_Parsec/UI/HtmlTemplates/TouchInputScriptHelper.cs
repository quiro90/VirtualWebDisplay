using VirtualWebDisplay.Configuration;

namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// Helper para generar script de Touch Input compartido entre templates.
/// DRY: Evita repetición de código JavaScript entre WebImagePageTemplate y RtcPageTemplate.
/// </summary>
internal static class TouchInputScriptHelper
{
    public static string GenerateKeepAliveScript(int intervalMs = 10000)
    {
        if (intervalMs < 1000)
            intervalMs = 1000;

        return $$"""
            function startKeepAliveSignal() {
                function ping() {
                    fetch('/keepalive?t=' + Date.now(), {
                        method: 'GET',
                        cache: 'no-store',
                        keepalive: true,
                        credentials: 'same-origin'
                    }).catch(function () {});
                }

                ping();
                setInterval(ping, {{intervalMs}});
            }

            startKeepAliveSignal();
            """;
    }

    /// <summary>
    /// Genera el script JavaScript para capturar y enviar eventos táctiles.
    /// Se inyecta en ambos templates (WebImage y WebRTC).
    /// 
    /// Parameters esperados:
    /// - screenElementId: ID del elemento HTML que recibe toques ("screen" para WebImage, "screen" para WebRTC)
    /// - throttleMs: Mínimo de ms entre eventos (default: 50ms)
    /// - holdDelayMs: ms necesarios para activar drag/scroll por hold
    /// </summary>
    public static string GenerateTouchInputScript(string screenElementId, int throttleMs = 50, int holdDelayMs = TouchGestureOptions.DefaultHoldDelayMs)
    {
        // Validar parámetros
        if (string.IsNullOrWhiteSpace(screenElementId))
            screenElementId = "screen";

        if (throttleMs < 10)
            throttleMs = 10;

        holdDelayMs = TouchGestureOptions.ClampHoldDelay(holdDelayMs);

        return $$"""
            // ────────────────────────────────────────────────────────────────
            // TOUCH INPUT HANDLING (Virtual Mouse from Tablet)
            // 1 dedo: tap = click; hold = drag-and-drop
            // 2 dedos: hold = scroll
            // + modo absoluto: el cursor se posiciona al tocar
            // ────────────────────────────────────────────────────────────────
            (function() {
                var screenElement = document.getElementById('{{screenElementId}}');
                if (!screenElement) {
                    console.error('[TouchInput] Element not found: {{screenElementId}}');
                    return;
                }

                var lastTouchTime = 0;
                var touchThrottle = {{throttleMs}};
                var touchEventCount = 0;
                var touchErrorCount = 0;
                var recentLocalEvents = [];
                var recentLatencies = [];
                var HOLD_DELAY_MS = {{holdDelayMs}};
                var TAP_MAX_MOVE_PX = 14;
                var interactionActive = false;

                var state = {
                    mode: 'idle',
                    startX: 0,
                    startY: 0,
                    lastX: 0,
                    lastY: 0,
                    centerY: 0,
                    holdTimer: null
                };

                function avg(arr) {
                    if (!arr.length) return 0;
                    var sum = 0;
                    for (var i = 0; i < arr.length; i++) sum += arr[i];
                    return Math.round((sum / arr.length) * 10) / 10;
                }

                function pruneWindow(now) {
                    while (recentLocalEvents.length && (now - recentLocalEvents[0]) > 1000)
                        recentLocalEvents.shift();

                    while (recentLatencies.length > 60)
                        recentLatencies.shift();
                }

                function sendTouchInput(data) {
                    var sentAt = Date.now();
                    fetch('/input/touch', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(data),
                        keepalive: true,
                        credentials: 'same-origin'
                    }).then(function(resp) {
                        var roundTrip = Date.now() - sentAt;
                        recentLatencies.push(roundTrip);

                        if (!resp.ok) {
                            touchErrorCount++;
                            if (resp.status === 429)
                                console.warn('[TouchInput] Rate limited by server');
                        }
                    }).catch(function () {
                        touchErrorCount++;
                    });
                }

                function sendEndAction(action, now) {
                    sendTouchInput({
                        type: 'touchend',
                        action: action,
                        timestamp: now
                    });
                }

                function sendEndForCurrentMode(now) {
                    if (state.mode === 'drag') {
                        sendEndAction('dragend', now);
                        return;
                    }

                    if (state.mode === 'scroll')
                        sendEndAction('scrollend', now);
                }

                function clearHoldTimer() {
                    if (state.holdTimer) {
                        clearTimeout(state.holdTimer);
                        state.holdTimer = null;
                    }
                }

                function resetState() {
                    clearHoldTimer();
                    state.mode = 'idle';
                    interactionActive = false;
                }

                function getCenter(touches, rect) {
                    var t1 = touches[0];
                    var t2 = touches[1];
                    return {
                        x: ((t1.clientX + t2.clientX) / 2) - rect.left,
                        y: ((t1.clientY + t2.clientY) / 2) - rect.top
                    };
                }

                function distanceFromStart(x, y) {
                    var dx = x - state.startX;
                    var dy = y - state.startY;
                    return Math.sqrt((dx * dx) + (dy * dy));
                }

                function startSingleFingerPending(touch, rect, now) {
                    state.mode = 'pendingTap';
                    state.startX = touch.clientX - rect.left;
                    state.startY = touch.clientY - rect.top;
                    state.lastX = state.startX;
                    state.lastY = state.startY;

                    // 👉 mover cursor inmediatamente (modo absoluto)
                    sendTouchInput({
                        type: 'touchmove',
                        action: 'dragmove',
                        x: state.startX,
                        y: state.startY,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: 1,
                        timestamp: now
                    });

                    clearHoldTimer();
                    state.holdTimer = setTimeout(function () {
                        if (state.mode !== 'pendingTap')
                            return;

                        state.mode = 'drag';

                        sendTouchInput({
                            type: 'touchstart',
                            action: 'dragstart',
                            x: state.lastX,
                            y: state.lastY,
                            viewportWidth: rect.width,
                            viewportHeight: rect.height,
                            fingers: 1,
                            timestamp: Date.now()
                        });
                    }, HOLD_DELAY_MS);

                    touchEventCount++;
                    recentLocalEvents.push(now);
                }

                function startTwoFingerPending(touches, rect, now) {
                    state.mode = 'pendingScroll';
                    var center = getCenter(touches, rect);
                    state.centerY = center.y;
                    state.centerX = center.x;

                    // FIX: posicionar cursor antes del scroll
                    sendTouchInput({
                        type: 'touchmove',
                        action: 'dragmove',
                        x: center.x,
                        y: center.y,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: 2,
                        timestamp: now
                    });

                    clearHoldTimer();
                    state.holdTimer = setTimeout(function () {
                        if (state.mode !== 'pendingScroll')
                            return;

                        state.mode = 'scroll';
                    }, HOLD_DELAY_MS);

                    touchEventCount++;
                    recentLocalEvents.push(now);
                }

                function handleTouchStart(e) {
                    var now = Date.now();
                    e.preventDefault();

                    var rect = screenElement.getBoundingClientRect();
                    var fingerCount = e.touches.length;
                    interactionActive = true;

                    if (fingerCount >= 2) {
                        if (state.mode === 'drag')
                            sendEndAction('dragend', now);

                        startTwoFingerPending(e.touches, rect, now);
                        return;
                    }

                    if (fingerCount === 1 && (state.mode === 'idle' || state.mode === 'pendingTap')) {
                        startSingleFingerPending(e.touches[0], rect, now);
                    }
                }

                function handleTouchMove(e) {
                    if (!interactionActive)
                        return;

                    var now = Date.now();
                    e.preventDefault();

                    var rect = screenElement.getBoundingClientRect();
                    var fingerCount = e.touches.length;

                    // 👉 movimiento absoluto mientras no es drag
                    if (state.mode === 'pendingTap' && fingerCount === 1) {
                        var t = e.touches[0];
                        state.lastX = t.clientX - rect.left;
                        state.lastY = t.clientY - rect.top;

                        if (now - lastTouchTime < touchThrottle) return;
                        lastTouchTime = now;

                        sendTouchInput({
                            type: 'touchmove',
                            action: 'dragmove',
                            x: state.lastX,
                            y: state.lastY,
                            viewportWidth: rect.width,
                            viewportHeight: rect.height,
                            fingers: 1,
                            timestamp: now
                        });

                        return;
                    }

                    if (now - lastTouchTime < touchThrottle) return;
                    lastTouchTime = now;

                    if (state.mode === 'drag' && fingerCount >= 1) {
                        var dragTouch = e.touches[0];
                        state.lastX = dragTouch.clientX - rect.left;
                        state.lastY = dragTouch.clientY - rect.top;

                        touchEventCount++;
                        recentLocalEvents.push(now);

                        sendTouchInput({
                            type: 'touchmove',
                            action: 'dragmove',
                            x: state.lastX,
                            y: state.lastY,
                            viewportWidth: rect.width,
                            viewportHeight: rect.height,
                            fingers: 1,
                            timestamp: now
                        });
                        return;
                    }

                    if (state.mode === 'scroll' && fingerCount >= 2) {
                        var center = getCenter(e.touches, rect);
                        var deltaY = center.y - state.centerY;
                        var deltaX = center.x - state.centerX;
                        state.centerY = center.y;
                        state.centerX = center.x;

                        var invDeltaY = -deltaY;
                        var invDeltaX = -deltaX;

                        if (Math.abs(invDeltaY) < 1 && Math.abs(invDeltaX) < 1)
                            return;

                        touchEventCount++;
                        recentLocalEvents.push(now);

                        sendTouchInput({
                            type: 'touchmove',
                            action: 'scrollmove',
                            x: center.x,
                            y: center.y,
                            viewportWidth: rect.width,
                            viewportHeight: rect.height,
                            fingers: 2,
                            scrollDeltaY: invDeltaY,
                            scrollDeltaX: invDeltaX,
                            timestamp: now
                        });
                    }
                }

                function handleTouchEnd(e) {
                if (!interactionActive)
                    return;

                var now = Date.now();
                touchEventCount++;
                recentLocalEvents.push(now);

                if (state.mode === 'drag') {
                    sendEndAction('dragend', now);
                    resetState();
                    return;
                }

                if (state.mode === 'scroll') {
                    if (e.touches.length < 2) {
                        sendEndAction('scrollend', now);
                        resetState();
                    }
                    return;
                }

                if (state.mode === 'pendingTap') {
                    clearHoldTimer();

                    var rect = screenElement.getBoundingClientRect();

                    // 👉 FIX CLAVE: mover SIEMPRE antes del tap
                    sendTouchInput({
                        type: 'touchmove',
                        action: 'dragmove',
                        x: state.lastX,
                        y: state.lastY,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: 1,
                        timestamp: now
                    });

                    var moved = distanceFromStart(state.lastX, state.lastY);
                    if (moved <= TAP_MAX_MOVE_PX) {
                        sendTouchInput({
                            type: 'touchend',
                            action: 'tap',
                            x: state.lastX,
                            y: state.lastY,
                            viewportWidth: rect.width,
                            viewportHeight: rect.height,
                            fingers: 1,
                            timestamp: now
                        });
                    }

                    resetState();
                    return;
                }

                if (state.mode === 'pendingScroll') {
                    clearHoldTimer();
                    resetState();
                }

                if (e.touches.length === 0)
                    resetState();
            }

                function handleTouchCancel() {
                    if (!interactionActive)
                        return;

                    sendEndForCurrentMode(Date.now());
                    resetState();
                }

                function finalizeOnPageStateLoss() {
                    if (!interactionActive)
                        return;

                    sendEndForCurrentMode(Date.now());
                    resetState();
                }

                screenElement.addEventListener('touchstart', handleTouchStart, { passive: false });
                document.addEventListener('touchmove', handleTouchMove, { passive: false });
                document.addEventListener('touchend', handleTouchEnd, { passive: false });
                document.addEventListener('touchcancel', handleTouchCancel, { passive: false });

                document.addEventListener('visibilitychange', function () {
                    if (document.visibilityState === 'hidden')
                        finalizeOnPageStateLoss();
                });

                window.addEventListener('pagehide', finalizeOnPageStateLoss);

                window.VirtualWebDisplayTouchInput = {
                    getStats: function() {
                        pruneWindow(Date.now());
                        return {
                            eventCount: touchEventCount,
                            errorCount: touchErrorCount,
                            localEventsPerSecond: recentLocalEvents.length,
                            avgLocalLatencyMs: avg(recentLatencies)
                        };
                    }
                };

                console.log('[TouchInput] Initialized (absolute pointer enabled)');
            })();
            """;
    }
}
