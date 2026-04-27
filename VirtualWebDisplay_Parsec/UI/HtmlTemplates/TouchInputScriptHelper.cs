namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// Helper para generar script de Touch Input compartido entre templates.
/// DRY: Evita repetición de código JavaScript entre WebImagePageTemplate y RtcPageTemplate.
/// </summary>
internal static class TouchInputScriptHelper
{
    /// <summary>
    /// Genera el script JavaScript para capturar y enviar eventos táctiles.
    /// Se inyecta en ambos templates (WebImage y WebRTC).
    /// 
    /// Parameters esperados:
    /// - screenElementId: ID del elemento HTML que recibe toques ("screen" para WebImage, "screen" para WebRTC)
    /// - throttleMs: Mínimo de ms entre eventos (default: 50ms)
    /// - touchInputEnabledDefault: reservado para compatibilidad de firma
    /// </summary>
    public static string GenerateTouchInputScript(string screenElementId, int throttleMs = 50, bool touchInputEnabledDefault = false)
    {
        // Validar parámetros
        if (string.IsNullOrWhiteSpace(screenElementId))
            screenElementId = "screen";

        if (throttleMs < 10)
            throttleMs = 10;

        return $$"""
            // ────────────────────────────────────────────────────────────────
            // TOUCH INPUT HANDLING (Virtual Mouse from Tablet)
            // 1 finger = left-click, 2 fingers = right-click, 3+ fingers = middle-click
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
                var lastInputAt = 0;
                var serverStats = null;

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

                function renderStatsPanel() {
                    var now = Date.now();
                    pruneWindow(now);
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

                        renderStatsPanel();
                    }).catch(function (err) {
                        touchErrorCount++;
                        console.error('[TouchInput] Error sending:', err);
                        renderStatsPanel();
                    });
                }

                function handleTouchStart(e) {
                    var now = Date.now();
                    if (now - lastTouchTime < touchThrottle) {
                        console.debug('[TouchInput] Throttled (too fast)');
                        return;
                    }
                    lastTouchTime = now;
                    e.preventDefault();

                    var touch = e.touches[0];
                    var rect = screenElement.getBoundingClientRect();
                    touchEventCount++;
                    lastInputAt = now;
                    recentLocalEvents.push(now);

                    sendTouchInput({
                        type: 'touchstart',
                        x: touch.clientX - rect.left,
                        y: touch.clientY - rect.top,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: e.touches.length,
                        timestamp: now
                    });

                    console.debug('[TouchInput] Start - ' + e.touches.length + ' finger(s)');
                }

                function handleTouchMove(e) {
                    var now = Date.now();
                    if (now - lastTouchTime < touchThrottle) return;
                    lastTouchTime = now;
                    e.preventDefault();

                    var touch = e.touches[0];
                    var rect = screenElement.getBoundingClientRect();
                    touchEventCount++;
                    lastInputAt = now;
                    recentLocalEvents.push(now);

                    sendTouchInput({
                        type: 'touchmove',
                        x: touch.clientX - rect.left,
                        y: touch.clientY - rect.top,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: e.touches.length,
                        timestamp: now
                    });
                }

                function handleTouchEnd(e) {
                    var now = Date.now();
                    touchEventCount++;
                    lastInputAt = now;
                    recentLocalEvents.push(now);
                    sendTouchInput({
                        type: 'touchend',
                        timestamp: now
                    });
                    console.debug('[TouchInput] End');
                }

                // Registrar listeners
                document.addEventListener('touchstart', handleTouchStart, false);
                document.addEventListener('touchmove', handleTouchMove, false);
                document.addEventListener('touchend', handleTouchEnd, false);

                renderStatsPanel();

                // Exponer métodos para debugging (opcional)
                window.VirtualWebDisplayTouchInput = {
                    getStats: function() {
                        return {
                            eventCount: touchEventCount,
                            errorCount: touchErrorCount,
                            localEventsPerSecond: recentLocalEvents.length,
                            avgLocalLatencyMs: avg(recentLatencies),
                            serverStats: serverStats
                        };
                    }
                };

                console.log('[TouchInput] Initialized - Element: {{screenElementId}}, Throttle: {{throttleMs}}ms');
            })();
            """;
    }
}
