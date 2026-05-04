/**
 * Cliente de entrada táctil para VirtualWebDisplay.
 * Traduce eventos táctiles de tablet/smartphone a comandos de mouse remoto.
 * Soporta gestos avanzados: tap, hold-to-drag, two-finger scroll.
 * 
 * @namespace TouchInput
 */
(function(global) {
    'use strict';

    // Logger especializado
    const log = global.Logger ? global.Logger.create('[TouchInput]') : {
        info: console.log.bind(console, '[TouchInput]'),
        warn: console.warn.bind(console, '[TouchInput]'),
        error: console.error.bind(console, '[TouchInput]'),
        debug: console.debug.bind(console, '[TouchInput]')
    };

    /**
     * Sistema de entrada táctil con soporte de gestos.
     */
    const TouchInput = {
        // Configuration parameters
        _config: null,
        _screenElement: null,

        // Delays & Enablers
        _touchZoomEnabled: true,
        _touchHoldEnabled: true,
        _touchHoldDelayMs: 250,
        _touchScrollEnabled: true,
        _touchScrollDelayMs: 250,

        // Estado de gestos
        _state: {
            mode: 'idle',
            startX: 0,
            startY: 0,
            lastX: 0,
            lastY: 0,
            centerY: 0,
            centerX: 0,
            holdTimer: null,
            // Propiedades de zoom (Pinch-to-zoom temporal)
            pinchBaseDist: 0,
            isZooming: false,
            initialTransform: '',
            initialViewportCenterX: 0,
            initialViewportCenterY: 0
        },

        // Rate limiting y throttling
        _lastTouchTime: 0,
        _touchThrottle: 50,

        // Estadísticas
        _touchEventCount: 0,
        _touchErrorCount: 0,
        _recentLocalEvents: [],
        _recentLatencies: [],

        // Constantes (sincronizadas con Configuration/TouchInputConstants.cs)
        _TAP_MAX_MOVE_PX: 14,        // TouchInputConstants.TapMaxMovePx
        _MIN_THROTTLE_MS: 10,        // TouchInputConstants.MinThrottleMs
        _DEFAULT_THROTTLE_MS: 50,    // TouchInputConstants.DefaultThrottleMs
        _MAX_LATENCY_SAMPLES: 60,    // TouchInputConstants.MaxLatencySamples
        _EVENTS_WINDOW_MS: 1000,     // TouchInputConstants.EventsWindowMs

        // Control de interacción
        _interactionActive: false,

        /**
         * Inicializa el sistema de entrada táctil.
         * @param {Object} config - Configuración del sistema
         * @param {string} config.elementId - ID del elemento HTML que recibirá eventos táctiles
         * @param {number} [config.throttleMs=50] - Throttling de eventos en milisegundos (mínimo 10ms)
         */
        init(config) {
            if (!config || !config.elementId) {
                log.error('Configuration error: elementId is required');
                return;
            }

            this._config = config;
            this._screenElement = document.getElementById(config.elementId);

            if (!this._screenElement) {
                log.error('Element not found:', config.elementId);
                return;
            }

            // Configurar parámetros
            if (config.throttleMs && config.throttleMs >= this._MIN_THROTTLE_MS) {
                this._touchThrottle = config.throttleMs;
            } else {
                this._touchThrottle = this._DEFAULT_THROTTLE_MS;
            }

            this._touchZoomEnabled = config.touchZoomEnabled ?? true;
            this._touchHoldEnabled = config.touchHoldEnabled ?? true;
            this._touchHoldDelayMs = config.touchHoldDelayMs ?? 250;
            this._touchScrollEnabled = config.touchScrollEnabled ?? true;
            this._touchScrollDelayMs = config.touchScrollDelayMs ?? 250;

            // Adjuntar event listeners
            this._attachListeners();

            log.info('Initialized (absolute pointer enabled)', {
                elementId: config.elementId,
                throttleMs: this._touchThrottle,
                touchZoomEnabled: this._touchZoomEnabled,
                touchHoldEnabled: this._touchHoldEnabled,
                touchHoldDelayMs: this._touchHoldDelayMs,
                touchScrollEnabled: this._touchScrollEnabled,
                touchScrollDelayMs: this._touchScrollDelayMs
            });
        },

        /**
         * Obtiene estadísticas de rendimiento del sistema táctil.
         * @returns {Object} Estadísticas actuales
         */
        getStats() {
            const now = Date.now();
            this._pruneWindow(now);

            return {
                eventCount: this._touchEventCount,
                errorCount: this._touchErrorCount,
                localEventsPerSecond: this._recentLocalEvents.length,
                avgLocalLatencyMs: this._avg(this._recentLatencies)
            };
        },

        /**
         * Adjunta todos los event listeners necesarios.
         * @private
         */
        _attachListeners() {
            this._screenElement.addEventListener('touchstart', (e) => this._handleTouchStart(e), { passive: false });
            document.addEventListener('touchmove', (e) => this._handleTouchMove(e), { passive: false });
            document.addEventListener('touchend', (e) => this._handleTouchEnd(e), { passive: false });
            document.addEventListener('touchcancel', (e) => this._handleTouchCancel(e), { passive: false });

            document.addEventListener('visibilitychange', () => {
                if (document.visibilityState === 'hidden') {
                    this._finalizeOnPageStateLoss();
                }
            });

            window.addEventListener('pagehide', () => this._finalizeOnPageStateLoss());
        },

        /**
         * Maneja evento touchstart.
         * @private
         */
        _handleTouchStart(e) {
            const now = Date.now();
            e.preventDefault();

            const rect = this._screenElement.getBoundingClientRect();
            const fingerCount = e.touches.length;
            this._interactionActive = true;

            if (fingerCount >= 2) {
                if (this._state.mode === 'drag') {
                    this._sendEndAction('dragend', now);
                }
                
                try {
                    this._state.pinchBaseDist = this._getPinchDistance(e.touches);
                    this._state.isZooming = false;
                    this._state.initialTransform = this._screenElement.style.transform || '';
                    
                    const absCenter = this._getAbsoluteCenter(e.touches);
                    this._state.initialViewportCenterX = absCenter.x;
                    this._state.initialViewportCenterY = absCenter.y;
                } catch (err) {
                    log.error('Error in pinch start', err);
                }
                
                this._startTwoFingerPending(e.touches, rect, now);
                return;
            }

            if (fingerCount === 1 && (this._state.mode === 'idle' || this._state.mode === 'pendingTap')) {
                this._startSingleFingerPending(e.touches[0], rect, now);
            }
        },

        /**
         * Maneja evento touchmove.
         * @private
         */
        _handleTouchMove(e) {
            if (!this._interactionActive) {
                return;
            }

            const now = Date.now();
            e.preventDefault();

            const rect = this._screenElement.getBoundingClientRect();
            const fingerCount = e.touches.length;

            if (fingerCount >= 2 && this._touchZoomEnabled && (this._state.isZooming || this._state.mode !== 'scroll')) {
                try {
                    const currentDist = this._getPinchDistance(e.touches);

                    // Evaluamos solo por distancia (30px de tolerancia), el tiempo entorpece un pellizco natural
                    if (!this._state.isZooming && Math.abs(currentDist - this._state.pinchBaseDist) > 30) {
                        this._state.isZooming = true;
                        this._state.mode = 'zoom';
                        this._clearHoldTimer();
                        const center = this._getCenter(e.touches, rect);
                        this._screenElement.style.transformOrigin = `${center.x}px ${center.y}px`;
                        this._screenElement.style.transition = 'none';
                    }

                    if (this._state.isZooming) {
                        const scale = Math.max(0.5, currentDist / this._state.pinchBaseDist);

                        const absCenter = this._getAbsoluteCenter(e.touches);
                        const panX = absCenter.x - this._state.initialViewportCenterX;
                        const panY = absCenter.y - this._state.initialViewportCenterY;

                        this._screenElement.style.transform = `translate(${panX}px, ${panY}px) scale(${scale})`;
                        return; // No procesar scroll de servidor mientras se hace zoom
                    }
                } catch (err) {
                    log.info('No soportado', err);
                }
            }

            // Movimiento absoluto mientras no es drag
            if (this._state.mode === 'pendingTap' && fingerCount === 1) {
                const t = e.touches[0];
                this._state.lastX = t.clientX - rect.left;
                this._state.lastY = t.clientY - rect.top;

                if (now - this._lastTouchTime < this._touchThrottle) {
                    return;
                }
                this._lastTouchTime = now;

                this._sendTouchInput({
                    type: 'touchmove',
                    action: 'dragmove',
                    x: this._state.lastX,
                    y: this._state.lastY,
                    viewportWidth: rect.width,
                    viewportHeight: rect.height,
                    fingers: 1,
                    timestamp: now
                });

                return;
            }

            if (now - this._lastTouchTime < this._touchThrottle) {
                return;
            }
            this._lastTouchTime = now;

            if (this._state.mode === 'drag' && fingerCount >= 1) {
                const dragTouch = e.touches[0];
                this._state.lastX = dragTouch.clientX - rect.left;
                this._state.lastY = dragTouch.clientY - rect.top;

                this._touchEventCount++;
                this._recentLocalEvents.push(now);

                this._sendTouchInput({
                    type: 'touchmove',
                    action: 'dragmove',
                    x: this._state.lastX,
                    y: this._state.lastY,
                    viewportWidth: rect.width,
                    viewportHeight: rect.height,
                    fingers: 1,
                    timestamp: now
                });
                return;
            }

            if (this._state.mode === 'scroll' && fingerCount >= 2) {
                const center = this._getCenter(e.touches, rect);
                const deltaY = center.y - this._state.centerY;
                const deltaX = center.x - this._state.centerX;
                this._state.centerY = center.y;
                this._state.centerX = center.x;

                if (Math.abs(deltaY) < 1 && Math.abs(deltaX) < 1) {
                    return;
                }

                this._touchEventCount++;
                this._recentLocalEvents.push(now);

                this._sendTouchInput({
                    type: 'touchmove',
                    action: 'scrollmove',
                    x: center.x,
                    y: center.y,
                    viewportWidth: rect.width,
                    viewportHeight: rect.height,
                    fingers: 2,
                    // Scroll natural: la dirección del scroll es idéntica al movimiento.
                    scrollDeltaY: deltaY,
                    scrollDeltaX: deltaX,
                    timestamp: now
                });
            }
        },

        /**
         * Maneja evento touchend.
         * @private
         */
        _handleTouchEnd(e) {
            if (!this._interactionActive) {
                return;
            }

            const now = Date.now();
            this._touchEventCount++;
            this._recentLocalEvents.push(now);

            if (this._state.mode === 'drag') {
                this._sendEndAction('dragend', now);
                this._resetState();
                return;
            }

            if (this._state.mode === 'scroll' || this._state.mode === 'zoom') {
                if (e.touches.length < 2) {
                    this._sendEndAction('scrollend', now);
                    this._resetState();
                }
                return;
            }

            if (this._state.mode === 'pendingTap') {
                this._clearHoldTimer();

                const rect = this._screenElement.getBoundingClientRect();

                // FIX CLAVE: mover SIEMPRE antes del tap
                this._sendTouchInput({
                    type: 'touchmove',
                    action: 'dragmove',
                    x: this._state.lastX,
                    y: this._state.lastY,
                    viewportWidth: rect.width,
                    viewportHeight: rect.height,
                    fingers: 1,
                    timestamp: now
                });

                const moved = this._distanceFromStart(this._state.lastX, this._state.lastY);
                if (moved <= this._TAP_MAX_MOVE_PX) {
                    this._sendTouchInput({
                        type: 'touchend',
                        action: 'tap',
                        x: this._state.lastX,
                        y: this._state.lastY,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: 1,
                        timestamp: now
                    });
                }

                this._resetState();
                return;
            }

            if (this._state.mode === 'pendingScroll') {
                this._clearHoldTimer();
                this._resetState();
            }

            if (e.touches.length === 0) {
                this._resetState();
            }
        },

        /**
         * Maneja evento touchcancel.
         * @private
         */
        _handleTouchCancel() {
            if (!this._interactionActive) {
                return;
            }
            this._sendEndForCurrentMode(Date.now());
            this._resetState();
        },

        /**
         * Finaliza estado al perder foco de página (visibilitychange, pagehide).
         * @private
         */
        _finalizeOnPageStateLoss() {
            if (!this._interactionActive) {
                return;
            }
            this._sendEndForCurrentMode(Date.now());
            this._resetState();
        },

        /**
         * Inicia pending de un dedo.
         * @private
         */
        _startSingleFingerPending(touch, rect, now) {
            this._state.mode = 'pendingTap';
            this._state.startX = touch.clientX - rect.left;
            this._state.startY = touch.clientY - rect.top;
            this._state.lastX = this._state.startX;
            this._state.lastY = this._state.startY;

            // Mover cursor inmediatamente (modo absoluto)
            this._sendTouchInput({
                type: 'touchmove',
                action: 'dragmove',
                x: this._state.startX,
                y: this._state.startY,
                viewportWidth: rect.width,
                viewportHeight: rect.height,
                fingers: 1,
                timestamp: now
            });

            this._clearHoldTimer();
            if (this._touchHoldEnabled) {
                this._state.holdTimer = setTimeout(() => {
                    if (this._state.mode !== 'pendingTap') {
                        return;
                    }

                    this._state.mode = 'drag';

                    this._sendTouchInput({
                        type: 'touchstart',
                        action: 'dragstart',
                        x: this._state.lastX,
                        y: this._state.lastY,
                        viewportWidth: rect.width,
                        viewportHeight: rect.height,
                        fingers: 1,
                        timestamp: Date.now()
                    });
                }, this._touchHoldDelayMs);
            }

            this._touchEventCount++;
            this._recentLocalEvents.push(now);
        },

        /**
         * Inicia pending de dos dedos.
         * @private
         */
        _startTwoFingerPending(touches, rect, now) {
            this._state.mode = 'pendingScroll';
            const center = this._getCenter(touches, rect);
            this._state.centerY = center.y;
            this._state.centerX = center.x;

            // FIX: posicionar cursor antes del scroll
            this._sendTouchInput({
                type: 'touchmove',
                action: 'dragmove',
                x: center.x,
                y: center.y,
                viewportWidth: rect.width,
                viewportHeight: rect.height,
                fingers: 2,
                timestamp: now
            });

            this._clearHoldTimer();
            if (this._touchScrollEnabled) {
                this._state.holdTimer = setTimeout(() => {
                    if (this._state.mode !== 'pendingScroll') {
                        return;
                    }
                    this._state.mode = 'scroll';
                }, this._touchScrollDelayMs);
            }

            this._touchEventCount++;
            this._recentLocalEvents.push(now);
        },

        /**
         * Envía evento de entrada táctil al servidor.
         * @private
         */
        _sendTouchInput(data) {
            const sentAt = Date.now();
            fetch('/input/touch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data),
                keepalive: true,
                credentials: 'same-origin'
            }).then((resp) => {
                const roundTrip = Date.now() - sentAt;
                this._recentLatencies.push(roundTrip);

                if (!resp.ok) {
                    this._touchErrorCount++;
                    if (resp.status === 429) {
                        log.warn('Rate limited by server');
                    }
                }
            }).catch(() => {
                this._touchErrorCount++;
            });
        },

        /**
         * Envía acción de finalización.
         * @private
         */
        _sendEndAction(action, now) {
            this._sendTouchInput({
                type: 'touchend',
                action: action,
                timestamp: now
            });
        },

        /**
         * Envía finalización según modo actual.
         * @private
         */
        _sendEndForCurrentMode(now) {
            if (this._state.mode === 'drag') {
                this._sendEndAction('dragend', now);
                return;
            }

            if (this._state.mode === 'scroll') {
                this._sendEndAction('scrollend', now);
            }
        },

        /**
         * Limpia el timer de hold.
         * @private
         */
        _clearHoldTimer() {
            if (this._state.holdTimer) {
                clearTimeout(this._state.holdTimer);
                this._state.holdTimer = null;
            }
        },

        /**
         * Resetea el estado a idle.
         * @private
         */
        _resetState() {
            this._clearHoldTimer();
            this._state.mode = 'idle';
            this._interactionActive = false;
            this._resetZoomPeek();
        },

        /**
         * Restaura la vista después de un zoom temporal (Peek).
         * @private
         */
        _resetZoomPeek() {
            if (!this._state.isZooming || !this._screenElement) {
                return;
            }
            
            try {
                this._screenElement.style.transition = 'transform 0.2s ease-out';
                this._screenElement.style.transform = this._state.initialTransform || '';
                setTimeout(() => {
                    if (this._screenElement && !this._state.isZooming) {
                        this._screenElement.style.transition = '';
                        this._screenElement.style.transformOrigin = '';
                    }
                }, 200);
            } catch (err) {
                // Silencioso
            }
            this._state.isZooming = false;
        },

        /**
         * Calcula la distancia entre dos toques (para zoom).
         * @private
         */
        _getPinchDistance(touches) {
            if (touches.length < 2) {
                return 0;
            }
            const dx = touches[0].clientX - touches[1].clientX;
            const dy = touches[0].clientY - touches[1].clientY;
            return Math.sqrt(dx * dx + dy * dy);
        },
        /**
         * Obtiene el centro absoluto en el viewport entre dos toques.
         * @private
         */
        _getAbsoluteCenter(touches) {
            if (touches.length < 2) {
                return { x: 0, y: 0 };
            }
            return {
                x: (touches[0].clientX + touches[1].clientX) / 2,
                y: (touches[0].clientY + touches[1].clientY) / 2
            };
        },

        /**
         * Obtiene el centro entre dos toques, relativo al elemento.
         * @private
         */
        _getCenter(touches, rect) {
            const abs = this._getAbsoluteCenter(touches);
            return {
                x: abs.x - rect.left,
                y: abs.y - rect.top
            };
        },

        /**
         * Calcula distancia desde punto de inicio.
         * @private
         */
        _distanceFromStart(x, y) {
            const dx = x - this._state.startX;
            const dy = y - this._state.startY;
            return Math.sqrt((dx * dx) + (dy * dy));
        },

        /**
         * Calcula promedio de un array.
         * @private
         */
        _avg(arr) {
            if (!arr.length) {
                return 0;
            }
            let sum = 0;
            for (let i = 0; i < arr.length; i++) {
                sum += arr[i];
            }
            return Math.round((sum / arr.length) * 10) / 10;
        },

        /**
         * Limpia ventana de eventos antiguos.
         * @private
         */
        _pruneWindow(now) {
            while (this._recentLocalEvents.length && (now - this._recentLocalEvents[0]) > this._EVENTS_WINDOW_MS) {
                this._recentLocalEvents.shift();
            }

            while (this._recentLatencies.length > this._MAX_LATENCY_SAMPLES) {
                this._recentLatencies.shift();
            }
        }
    };

    // Exponer al scope global
    global.TouchInput = TouchInput;

})(window);