/**
 * Sistema para mantener la pantalla del dispositivo encendida.
 * (Conserva el nombre Keepalive por compatibilidad con código existente).
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
        _visibilityHandler: null,

        // Estado de Pantalla Activa (Wake Lock)
        _isAwakeEnabled: false,
        _wakeLock: null,
        _hiddenVideo: null,
        _awakeButton: null,
        _fadeTimeoutId: null,

        // RAF loop + watchdog de video
        _rafId: null,
        _rafCanvas: null,
        _rafCtx: null,
        _videoCheckIntervalId: null,

        // AudioContext silencioso (mecanismo principal para iOS)
        _audioCtx: null,
        _audioOscillator: null,

        /**
         * Inicia el sistema para mantener la pantalla activa.
         */
        start() {
            this._setupVisibilityHandler();
            this._initScreenAwake();

            log.info('Screen awake system started');
        },

        /**
         * Detiene el sistema y libera la pantalla activa.
         */
        stop() {
            if (this._visibilityHandler) {
                document.removeEventListener('visibilitychange', this._visibilityHandler);
                this._visibilityHandler = null;
            }

            this._releaseWakeLock();
            this._pauseHiddenVideo();
            this._stopRafLoop();
            this._stopVideoCheck();
            this._stopSilentAudio();

            log.info('Stopped');
        },

        _setupVisibilityHandler() {
            if (this._visibilityHandler) return;

            this._visibilityHandler = () => {
                if (document.visibilityState === 'visible') {
                    // Reanudar wake lock (el SO suele soltarlo al minimizar/cambiar de app)
                    if (this._isAwakeEnabled) {
                        this._applyAwakeState();
                    }
                } else {
                    // Pausar pantalla activa para no gastar batería en segundo plano
                    this._releaseWakeLock();
                    this._pauseHiddenVideo();
                    this._stopRafLoop();
                    this._stopVideoCheck();
                    this._stopSilentAudio();
                }
            };

            document.addEventListener('visibilitychange', this._visibilityHandler);
        },

        // --- LÓGICA DE PANTALLA ACTIVA (WAKE LOCK / VIDEO FALLBACK) ---

        _initScreenAwake() {
            // Siempre inicia en OFF: el usuario debe activarlo manualmente
            this._isAwakeEnabled = false;
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
            this._awakeButton.style.bottom = '12px';
            this._awakeButton.style.right = '10%';
            this._awakeButton.style.transform = 'translateX(-10%)';
            this._awakeButton.style.setProperty('z-index', '2147483647', 'important');
            this._awakeButton.style.padding = '2px 16px';
            this._awakeButton.style.backgroundColor = 'rgba(0, 0, 0, 0.7)';
            this._awakeButton.style.color = '#fff';
            this._awakeButton.style.border = '1px solid rgba(255, 255, 255, 0.3)';
            this._awakeButton.style.borderRadius = '16px';
            this._awakeButton.style.cursor = 'pointer';
            this._awakeButton.style.fontFamily = 'sans-serif';
            this._awakeButton.style.fontSize = '12px';
            this._awakeButton.style.backdropFilter = 'blur(4px)';
            this._awakeButton.style.webkitBackdropFilter = 'blur(4px)'; // Para Safari
            this._awakeButton.style.transition = 'background-color 0.2s, opacity 0.5s';
            this._awakeButton.style.appearance = 'none';
            this._awakeButton.style.WebkitAppearance = 'none'; // Evitar estilo nativo de botón iOS
            this._awakeButton.style.pointerEvents = 'auto'; // Asegurar que reciba touch
            this._awakeButton.style.opacity = '0.7'; // Transparencia general del botón

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
                this._awakeButton.textContent = 'Light:ON';
                this._awakeButton.style.backgroundColor = 'rgba(28, 84, 28, 0.3)'; // Verde
            } else {
                this._awakeButton.textContent = 'Light:OFF';
                this._awakeButton.style.backgroundColor = 'rgba(0, 0, 0, 0.3)';   // Gris oscuro
            }
        },

        _toggleAwake() {
            this._isAwakeEnabled = !this._isAwakeEnabled;
            this._updateButtonUI();
            this._applyAwakeState();
        },

        _resetFadeTimer() {
            if (!this._awakeButton) return;

            this._awakeButton.style.opacity = '0.7';

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
                // 1. WakeLock API nativa (Chrome, Edge, Safari 16.4+ con HTTPS)
                if ('wakeLock' in navigator) {
                    this._requestWakeLock();
                }
                // 2. AudioContext silencioso: mecanismo principal para iOS
                //    No requiere HTTPS ni soporte de formato de video.
                this._startSilentAudio();
                // 3. Video loop como capa adicional (desktop/Android)
                this._playHiddenVideo();
                // 4. RAF canvas + watchdog de video
                this._startRafLoop();
                this._startVideoCheck();
            } else {
                this._releaseWakeLock();
                this._stopSilentAudio();
                this._pauseHiddenVideo();
                this._stopRafLoop();
                this._stopVideoCheck();
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
            this._hiddenVideo.setAttribute('muted', '');       // Crítico para Autoplay sin interacción
            this._hiddenVideo.muted = true;
            this._hiddenVideo.loop = true;
            // IMPORTANTE: display:none impide que iOS Safari mantenga el video activo.
            // Usar 1x1px fuera de la vista pero renderizado.
            this._hiddenVideo.style.cssText = 'position:fixed;bottom:-2px;right:-2px;width:1px;height:1px;opacity:0.01;pointer-events:none;';
            this._hiddenVideo.src = BLANK_VIDEO_SRC;
            
            document.body.appendChild(this._hiddenVideo);
        },

        // --- RAF LOOP (mantiene el pipeline de render del navegador activo) ---

        _startRafLoop() {
            if (this._rafId) return;

            if (!this._rafCanvas) {
                this._rafCanvas = document.createElement('canvas');
                this._rafCanvas.width = 1;
                this._rafCanvas.height = 1;
                this._rafCanvas.style.cssText = 'position:fixed;bottom:-2px;left:-2px;width:1px;height:1px;opacity:0.01;pointer-events:none;';
                document.body.appendChild(this._rafCanvas);
                this._rafCtx = this._rafCanvas.getContext('2d');
            }

            let tick = 0;
            const loop = () => {
                if (!this._isAwakeEnabled) {
                    this._rafId = null;
                    return;
                }
                tick = 1 - tick;
                // Alternar un pixel para forzar repaint real
                this._rafCtx.fillStyle = tick ? '#000001' : '#000000';
                this._rafCtx.fillRect(0, 0, 1, 1);
                this._rafId = requestAnimationFrame(loop);
            };
            this._rafId = requestAnimationFrame(loop);
            log.debug('RAF loop iniciado');
        },

        _stopRafLoop() {
            if (this._rafId) {
                cancelAnimationFrame(this._rafId);
                this._rafId = null;
                log.debug('RAF loop detenido');
            }
        },

        // --- WATCHDOG de video (reintenta play() si iOS lo pausó silenciosamente) ---

        _startVideoCheck() {
            if (this._videoCheckIntervalId) return;
            this._videoCheckIntervalId = setInterval(() => {
                if (this._isAwakeEnabled && this._hiddenVideo && this._hiddenVideo.paused) {
                    log.debug('Video fallback pausado inesperadamente, reiniciando...');
                    this._hiddenVideo.play().catch(() => {});
                }
            }, 5000);
        },

        _stopVideoCheck() {
            if (this._videoCheckIntervalId) {
                clearInterval(this._videoCheckIntervalId);
                this._videoCheckIntervalId = null;
            }
        },

        // --- AudioContext SILENCIOSO (previene apagado en iOS sin HTTPS) ---

        _startSilentAudio() {
            if (this._audioCtx) {
                // Ya existe: reanudar si fue suspendido al volver de segundo plano
                if (this._audioCtx.state === 'suspended') {
                    this._audioCtx.resume().catch(() => {});
                    log.debug('AudioContext reanudado');
                }
                return;
            }
            try {
                const AudioCtx = window.AudioContext || window.webkitAudioContext;
                if (!AudioCtx) return;
                this._audioCtx = new AudioCtx();
                const gainNode = this._audioCtx.createGain();
                gainNode.gain.value = 0.000001; // Prácticamente inaudible
                this._audioOscillator = this._audioCtx.createOscillator();
                this._audioOscillator.frequency.value = 1; // 1 Hz, sub-sónico
                this._audioOscillator.connect(gainNode);
                gainNode.connect(this._audioCtx.destination);
                this._audioOscillator.start();
                log.info('AudioContext silencioso activado (previene apagado en iOS)');
            } catch (err) {
                log.warn('No se pudo iniciar AudioContext silencioso', err);
            }
        },

        _stopSilentAudio() {
            if (this._audioOscillator) {
                try { this._audioOscillator.stop(); } catch(e) {}
                this._audioOscillator = null;
            }
            if (this._audioCtx) {
                try { this._audioCtx.close(); } catch(e) {}
                this._audioCtx = null;
            }
            log.debug('AudioContext detenido');
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
