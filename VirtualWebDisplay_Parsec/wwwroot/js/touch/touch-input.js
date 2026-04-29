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
        // Configuración
        _config: null,
        _screenElement: null,

        // Estado de gestos
        _state: {
            mode: 'idle',
            startX: 0,
            startY: 0,
            lastX: 0,
            lastY: 0,
            centerY: 0,
            centerX: 0,
            holdTimer: null
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
        _HOLD_DELAY_MS: 300,
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
         * @param {number} [config.holdDelayMs=300] - Delay para activar hold-to-drag en milisegundos
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

            if (config.holdDelayMs && config.holdDelayMs >= 100) {
                this._HOLD_DELAY_MS = config.holdDelayMs;
            }

            // Adjuntar event listeners
            this._attachListeners();

            log.info('Initialized (absolute pointer enabled)', {
                elementId: config.elementId,
                throttleMs: this._touchThrottle,
                holdDelayMs: this._HOLD_DELAY_MS
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
            if (!this._interactionActive) return;

            const now = Date.now();
            e.preventDefault();

            const rect = this._screenElement.getBoundingClientRect();
            const fingerCount = e.touches.length;

            // Movimiento absoluto mientras no es drag
            if (this._state.mode === 'pendingTap' && fingerCount === 1) {
                const t = e.touches[0];
                this._state.lastX = t.clientX - rect.left;
                this._state.lastY = t.clientY - rect.top;

                if (now - this._lastTouchTime < this._touchThrottle) return;
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

            if (now - this._lastTouchTime < this._touchThrottle) return;
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

                const invDeltaY = -deltaY;
                const invDeltaX = -deltaX;

                if (Math.abs(invDeltaY) < 1 && Math.abs(invDeltaX) < 1) return;

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
                    scrollDeltaY: invDeltaY,
                    scrollDeltaX: invDeltaX,
                    timestamp: now
                });
            }
        },

        /**
         * Maneja evento touchend.
         * @private
         */
        _handleTouchEnd(e) {
            if (!this._interactionActive) return;

            const now = Date.now();
            this._touchEventCount++;
            this._recentLocalEvents.push(now);

            if (this._state.mode === 'drag') {
                this._sendEndAction('dragend', now);
                this._resetState();
                return;
            }

            if (this._state.mode === 'scroll') {
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
            if (!this._interactionActive) return;
            this._sendEndForCurrentMode(Date.now());
            this._resetState();
        },

        /**
         * Finaliza estado al perder foco de página (visibilitychange, pagehide).
         * @private
         */
        _finalizeOnPageStateLoss() {
            if (!this._interactionActive) return;
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
            this._state.holdTimer = setTimeout(() => {
                if (this._state.mode !== 'pendingTap') return;

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
            }, this._HOLD_DELAY_MS);

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
            this._state.holdTimer = setTimeout(() => {
                if (this._state.mode !== 'pendingScroll') return;
                this._state.mode = 'scroll';
            }, this._HOLD_DELAY_MS);

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
        },

        /**
         * Obtiene el centro entre dos toques.
         * @private
         */
        _getCenter(touches, rect) {
            const t1 = touches[0];
            const t2 = touches[1];
            return {
                x: ((t1.clientX + t2.clientX) / 2) - rect.left,
                y: ((t1.clientY + t2.clientY) / 2) - rect.top
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
            if (!arr.length) return 0;
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

    // Compatibilidad con código legacy que usa window.VirtualWebDisplayTouchInput
    global.VirtualWebDisplayTouchInput = {
        getStats: function() {
            return TouchInput.getStats();
        }
    };

})(window);
