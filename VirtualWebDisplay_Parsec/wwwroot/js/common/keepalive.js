/**
 * Keep-alive signal para mantener la sesión activa.
 * Envía pings periódicos al servidor para evitar timeout de conexión.
 * Incluye funcionalidad opcional para mantener la pantalla del dispositivo encendida.
 * 
 * @namespace Keepalive
 */
(function(global) {
    'use strict';

    // Logger especializado
    const log = global.Logger ? global.Logger.create('[Keepalive]') : {
        info: console.log.bind(console, '[Keepalive]'),
        warn: console.warn.bind(console, '[Keepalive]'),
        error: console.error.bind(console, '[Keepalive]'),
        debug: console.debug.bind(console, '[Keepalive]')
    };

    // Video WebM en blanco (1x1) para fallback de mantener pantalla encendida en iOS/Safari
    const BLANK_VIDEO_SRC = 'data:video/webm;base64,GkXfo59ChoEBQveBAULygQRC84EIQoKEd2VibUKHgQJChYECGFOAZwEAAAAAAAARm2OBUZ+BByEBAAAAAAAAV1kByx9DkQEAAAAAAAACy1NAdAATiZ8JAAAAAAAAAAAAAAAAAAAAERuCg1ZUuAEAAAAAAAABswAAAAAAABIAAAZ0AAB1v4EHQ4KBCAIAAAAAAAAQU0EAAHPAoX9Cg4EIAgAAAAAAABBTAQAAc8B+f0KBAUKBgQIAAAAAAAAQU0EAAHPAoX9Cg4EIAgAAAAAAABBTAQAAc8B+fw==';

    /**
     * Sistema de keep-alive para mantener sesión activa.
     */
    const Keepalive = {
        // Estado de red (Keepalive)
        _intervalId: null,
        _intervalMs: 10000,
        _visibilityHandler: null,

        // Constantes (sincronizadas con Configuration/TouchInputConstants.cs)
        _MIN_INTERVAL_MS: 1000,      // TouchInputConstants.MinKeepaliveIntervalMs
        _DEFAULT_INTERVAL_MS: 10000, // TouchInputConstants.DefaultKeepaliveIntervalMs

        // Estado de Pantalla Activa (Wake Lock)
        _isAwakeEnabled: false,
        _wakeLock: null,
        _hiddenVideo: null,
        _awakeButton: null,
        _fadeTimeoutId: null,

        /**
         * Inicia el sistema de keep-alive y prepara la pantalla activa.
         * @param {number} [intervalMs=10000] - Intervalo entre pings en milisegundos (mínimo 1000ms)
         */
        start(intervalMs) {
            if (intervalMs && intervalMs >= this._MIN_INTERVAL_MS) {
                this._intervalMs = intervalMs;
            } else {
                this._intervalMs = this._DEFAULT_INTERVAL_MS;
            }

            this._startPingInterval();
            this._setupVisibilityHandler();
            this._initScreenAwake();

            log.info('Started with interval:', this._intervalMs + 'ms');
        },

        /**
         * Detiene el sistema de keep-alive y libera la pantalla activa.
         */
        stop() {
            this._stopPingInterval();
            
            if (this._visibilityHandler) {
                document.removeEventListener('visibilitychange', this._visibilityHandler);
                this._visibilityHandler = null;
            }

            this._releaseWakeLock();
            this._pauseHiddenVideo();

            log.info('Stopped');
        },

        // --- LÓGICA DE KEEPALIVE (RED) ---

        _startPingInterval() {
            if (!this._intervalId) {
                this._ping();
                this._intervalId = setInterval(() => this._ping(), this._intervalMs);
            }
        },

        _stopPingInterval() {
            if (this._intervalId) {
                clearInterval(this._intervalId);
                this._intervalId = null;
            }
        },

        /**
         * Envía un ping al servidor.
         * @private
         */
        _ping() {
            fetch('/keepalive?t=' + Date.now(), {
                method: 'GET',
                cache: 'no-store',
                keepalive: true,
                credentials: 'same-origin'
            }).catch(function() {
                // Silenciar errores de red (servidor caído, etc.)
            });
        },

        _setupVisibilityHandler() {
            if (this._visibilityHandler) return;

            this._visibilityHandler = () => {
                if (document.visibilityState === 'visible') {
                    // Reanudar keepalive de red
                    log.debug('Pestaña visible, reanudando keepalive');
                    this._startPingInterval();
                    
                    // Reanudar wake lock (el SO suele soltarlo al minimizar/cambiar de app)
                    if (this._isAwakeEnabled) {
                        this._applyAwakeState();
                    }
                } else {
                    // Pausar keepalive de red para no gastar batería
                    log.debug('Pestaña oculta, pausando keepalive');
                    this._stopPingInterval();
                }
            };

            document.addEventListener('visibilitychange', this._visibilityHandler);
        },

        // --- LÓGICA DE PANTALLA ACTIVA (WAKE LOCK / VIDEO FALLBACK) ---

        _initScreenAwake() {
            // Cargar estado persistido
            let savedState = 'false';
            try {
                savedState = localStorage.getItem('vwd_keep_awake');
            } catch (e) {
                log.warn('localStorage no disponible (posible modo incógnito)', e);
            }
            this._isAwakeEnabled = savedState === 'true';
            log.debug('Estado inicial de pantalla activa:', this._isAwakeEnabled);

            this._createAwakeButton();

            // Requerimos interacción del usuario para iniciar un video (políticas del navegador).
            // Si estaba en ON, intentamos aplicarlo tras la primera interacción global.
            if (this._isAwakeEnabled) {
                const onFirstInteraction = () => {
                    this._applyAwakeState();
                    document.removeEventListener('click', onFirstInteraction);
                    document.removeEventListener('touchstart', onFirstInteraction);
                };
                document.addEventListener('click', onFirstInteraction, { once: true });
                document.addEventListener('touchstart', onFirstInteraction, { once: true });
            }
        },

        _createAwakeButton() {
            if (this._awakeButton) return;

            this._awakeButton = document.createElement('button');
            this._awakeButton.id = 'vwd-awake-button';
            this._awakeButton.style.position = 'fixed';
            this._awakeButton.style.top = '10px';
            this._awakeButton.style.left = '50%';
            this._awakeButton.style.transform = 'translateX(-50%)';
            this._awakeButton.style.setProperty('z-index', '2147483647', 'important');
            this._awakeButton.style.padding = '6px 16px';
            this._awakeButton.style.backgroundColor = 'rgba(0, 0, 0, 0.6)';
            this._awakeButton.style.color = '#fff';
            this._awakeButton.style.border = '1px solid rgba(255, 255, 255, 0.3)';
            this._awakeButton.style.borderRadius = '20px';
            this._awakeButton.style.cursor = 'pointer';
            this._awakeButton.style.fontFamily = 'sans-serif';
            this._awakeButton.style.fontSize = '12px';
            this._awakeButton.style.backdropFilter = 'blur(4px)';
            this._awakeButton.style.webkitBackdropFilter = 'blur(4px)'; // Para Safari
            this._awakeButton.style.transition = 'background-color 0.2s, opacity 0.5s';
            this._awakeButton.style.appearance = 'none';
            this._awakeButton.style.WebkitAppearance = 'none'; // Evitar estilo nativo de botón iOS
            this._awakeButton.style.pointerEvents = 'auto'; // Asegurar que reciba touch
            this._awakeButton.style.opacity = '0.6'; // Transparencia general del botón

            this._awakeButton.addEventListener('click', (e) => {
                e.stopPropagation(); // Evitar propagación a la pantalla virtual (clicks fantasma)
                this._toggleAwake();
                this._resetFadeTimer();
            });

            this._awakeButton.addEventListener('mouseenter', () => this._resetFadeTimer());
            this._awakeButton.addEventListener('touchstart', () => this._resetFadeTimer(), { passive: true });

            this._updateButtonUI();

            // Asegurarnos de que el body exista antes de hacer appendChild
            const appendToDom = () => {
                if (document.body) document.body.appendChild(this._awakeButton);
            };

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', appendToDom);
            } else {
                appendToDom();
            }

            this._resetFadeTimer();
        },

        _updateButtonUI() {
            if (!this._awakeButton) return;

            if (this._isAwakeEnabled) {
                this._awakeButton.textContent = 'Active: ON';
                this._awakeButton.style.backgroundColor = 'rgba(28, 84, 28, 0.3)'; // Verde
            } else {
                this._awakeButton.textContent = 'Active: OFF';
                this._awakeButton.style.backgroundColor = 'rgba(0, 0, 0, 0.3)';   // Gris oscuro
            }
        },

        _toggleAwake() {
            this._isAwakeEnabled = !this._isAwakeEnabled;
            try {
                localStorage.setItem('vwd_keep_awake', this._isAwakeEnabled.toString());
            } catch (e) {
                log.warn('No se pudo guardar en localStorage', e);
            }
            this._updateButtonUI();
            this._applyAwakeState();
        },

        _resetFadeTimer() {
            if (!this._awakeButton) return;

            this._awakeButton.style.opacity = '0.6';

            if (this._fadeTimeoutId) {
                clearTimeout(this._fadeTimeoutId);
            }

            this._fadeTimeoutId = setTimeout(() => {
                if (this._awakeButton) {
                    this._awakeButton.style.opacity = '0.1';
                }
            }, 3000);
        },

        _applyAwakeState() {
            if (this._isAwakeEnabled) {
                // Priorizar Screen Wake Lock API nativa (Chrome, Edge, etc.)
                if ('wakeLock' in navigator) {
                    this._requestWakeLock();
                } else {
                    // Fallback para iOS Safari
                    this._playHiddenVideo();
                }
            } else {
                this._releaseWakeLock();
                this._pauseHiddenVideo();
            }
        },

        async _requestWakeLock() {
            try {
                if (this._wakeLock) return; // Evitar requests múltiples
                
                this._wakeLock = await navigator.wakeLock.request('screen');
                this._wakeLock.addEventListener('release', () => {
                    log.info('Wake Lock liberado por el SO');
                    this._wakeLock = null;
                });
                log.info('Wake Lock API activado');
            } catch (err) {
                log.warn('Fallo al solicitar Wake Lock API, usando fallback de video', err);
                this._playHiddenVideo(); // Fallback de red en caso de fallo por falta de permisos/focus
            }
        },

        _releaseWakeLock() {
            if (this._wakeLock !== null) {
                this._wakeLock.release().catch(() => {});
                this._wakeLock = null;
            }
        },

        _createHiddenVideo() {
            if (this._hiddenVideo) return;

            this._hiddenVideo = document.createElement('video');
            this._hiddenVideo.setAttribute('playsinline', ''); // Crítico para iOS
            this._hiddenVideo.setAttribute('muted', '');       // Crítico para Autoplay sin iteracción
            this._hiddenVideo.muted = true;
            this._hiddenVideo.loop = true;
            this._hiddenVideo.style.display = 'none';
            this._hiddenVideo.src = BLANK_VIDEO_SRC;
            
            document.body.appendChild(this._hiddenVideo);
        },

        _playHiddenVideo() {
            this._createHiddenVideo();
            this._hiddenVideo.play().then(() => {
                log.info('Video fallback activado para mantener pantalla encendida');
            }).catch(err => {
                // Silencioso. Suele fallar si no hubo interacción previa del usuario.
                log.debug('Video fallback pausado por política del navegador', err);
            });
        },

        _pauseHiddenVideo() {
            if (this._hiddenVideo) {
                this._hiddenVideo.pause();
                log.info('Video fallback detenido');
            }
        }
    };

    // Exponer al scope global
    global.Keepalive = Keepalive;

})(window);
