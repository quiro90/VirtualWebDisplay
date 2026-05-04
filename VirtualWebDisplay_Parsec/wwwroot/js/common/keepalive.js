/**
 * Usa Screen Wake Lock API (requiere HTTPS).
 * En iOS con HTTP, el sistema operativo ignora cualquier mecanismo web;
 * la solución confiable en ese caso es: Ajustes → Pantalla y Brillo → Bloqueo automático → Nunca.
 *
 * @namespace Keepalive
 */
(function(global) {
    'use strict';

    const log = global.Logger ? global.Logger.create('[Keepalive]') : {
        info: console.log.bind(console, '[Keepalive]'),
        warn: console.warn.bind(console, '[Keepalive]'),
        error: console.error.bind(console, '[Keepalive]'),
        debug: console.debug.bind(console, '[Keepalive]')
    };

    const Keepalive = {
        _visibilityHandler: null,
        _isAwakeEnabled: false,
        _wakeLock: null,
        _awakeButton: null,
        _fadeTimeoutId: null,
        _toastEl: null,

        start() {
            this._setupVisibilityHandler();
            this._createAwakeButton();
            log.info('Screen awake system started');
        },

        stop() {
            if (this._visibilityHandler) {
                document.removeEventListener('visibilitychange', this._visibilityHandler);
                this._visibilityHandler = null;
            }
            this._releaseWakeLock();
            log.info('Stopped');
        },

        _setupVisibilityHandler() {
            if (this._visibilityHandler) return;
            this._visibilityHandler = () => {
                if (document.visibilityState === 'visible') {
                    if (this._isAwakeEnabled) this._requestWakeLock();
                } else {
                    this._releaseWakeLock();
                }
            };
            document.addEventListener('visibilitychange', this._visibilityHandler);
        },

        _createAwakeButton() {
            if (this._awakeButton) return;

            this._awakeButton = document.createElement('button');
            this._awakeButton.id = 'vwd-awake-button';
            this._awakeButton.style.cssText = [
                'position:fixed',
                'bottom:12px',
                'right:16px',
                'z-index:2147483647',
                'padding:4px 14px',
                'background:rgba(0,0,0,0.3)',
                'color:#fff',
                'border:1px solid rgba(255,255,255,0.3)',
                'border-radius:16px',
                'cursor:pointer',
                'font-family:sans-serif',
                'font-size:12px',
                'backdrop-filter:blur(4px)',
                '-webkit-backdrop-filter:blur(4px)',
                'transition:background-color 0.2s, opacity 0.5s',
                'appearance:none',
                '-webkit-appearance:none',
                'pointer-events:auto',
                'opacity:0.7'
            ].join(';');

            this._awakeButton.addEventListener('click', (e) => {
                e.stopPropagation();
                this._toggleAwake();
                this._resetFadeTimer();
            });
            this._awakeButton.addEventListener('mouseenter', () => this._resetFadeTimer());
            this._awakeButton.addEventListener('touchstart', () => this._resetFadeTimer(), { passive: true });

            this._updateButtonUI();

            const appendToDom = () => { if (document.body) document.body.appendChild(this._awakeButton); };
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
                this._awakeButton.style.backgroundColor = 'rgba(28, 84, 28, 0.4)';
            } else {
                this._awakeButton.textContent = 'Light:OFF';
                this._awakeButton.style.backgroundColor = 'rgba(0, 0, 0, 0.3)';
            }
        },

        _toggleAwake() {
            this._isAwakeEnabled = !this._isAwakeEnabled;
            this._updateButtonUI();
            if (this._isAwakeEnabled) {
                this._requestWakeLock();
            } else {
                this._releaseWakeLock();
            }
        },

        async _requestWakeLock() {
            // Wake Lock API: funciona en Chrome/Edge y Safari 16.4+ con HTTPS.
            // En HTTP o iOS antiguo no está disponible; se muestra un aviso al usuario.
            if (!('wakeLock' in navigator)) {
                log.warn('Wake Lock API no disponible (requiere HTTPS en iOS)');
                this._showHint();
                return;
            }
            try {
                if (this._wakeLock) return;
                this._wakeLock = await navigator.wakeLock.request('screen');
                this._wakeLock.addEventListener('release', () => {
                    log.info('Wake Lock liberado por el SO');
                    this._wakeLock = null;
                });
                log.info('Wake Lock activado');
            } catch (err) {
                log.warn('Wake Lock rechazado:', err.message);
                // Puede fallar si la página no tiene foco o no es HTTPS
                this._showHint();
            }
        },

        _releaseWakeLock() {
            if (this._wakeLock) {
                this._wakeLock.release().catch(() => {});
                this._wakeLock = null;
            }
        },

        _resetFadeTimer() {
            if (!this._awakeButton) return;
            this._awakeButton.style.opacity = '0.7';
            if (this._fadeTimeoutId) clearTimeout(this._fadeTimeoutId);
            this._fadeTimeoutId = setTimeout(() => {
                if (this._awakeButton) this._awakeButton.style.opacity = '0.1';
            }, 3000);
        },

        // Muestra un toast con la solución cuando Wake Lock no está disponible
        _showHint() {
            if (this._toastEl) return;
            const toast = document.createElement('div');
            toast.style.cssText = [
                'position:fixed',
                'bottom:50px',
                'right:12px',
                'z-index:2147483647',
                'max-width:240px',
                'padding:10px 14px',
                'background:rgba(0,0,0,0.85)',
                'color:#fff',
                'border:1px solid rgba(255,255,255,0.2)',
                'border-radius:12px',
                'font-family:sans-serif',
                'font-size:12px',
                'line-height:1.5',
                'pointer-events:none',
                'opacity:1',
                'transition:opacity 0.5s'
            ].join(';');
            toast.innerHTML = [
                '<b>Pantalla activa puede no estar disponible</b><br>',
                'iOS/Safari, <b>probar HTTPS</b> o<br>',
                'Ajustes → Pantalla y Brillo<br>',
                '→ <b>Bloqueo automático → Nunca</b>'
            ].join('');
            document.body.appendChild(toast);
            this._toastEl = toast;
            setTimeout(() => {
                toast.style.opacity = '0';
                setTimeout(() => {
                    toast.remove();
                    this._toastEl = null;
                }, 500);
            }, 6000);
        }
    };

    global.Keepalive = Keepalive;

})(window);
