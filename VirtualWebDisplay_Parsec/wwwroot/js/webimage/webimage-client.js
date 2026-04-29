/**
 * Cliente WebImage para VirtualWebDisplay.
 * Implementa polling periódico de imágenes JPEG con preload
 * y manejo de viewport para dispositivos móviles.
 * 
 * @namespace WebImageClient
 */
(function(global) {
    'use strict';

    // Logger especializado
    const log = global.Logger ? global.Logger.create('[WebImageClient]') : {
        info: console.log.bind(console, '[WebImageClient]'),
        warn: console.warn.bind(console, '[WebImageClient]'),
        error: console.error.bind(console, '[WebImageClient]'),
        debug: console.debug.bind(console, '[WebImageClient]')
    };

    /**
     * Cliente de polling de imágenes JPEG.
     */
    const WebImageClient = {
        // Configuración
        _config: null,
        _screenElement: null,

        // Estado
        _seq: 0,
        _intervalMs: 250,
        _isRunning: false,

        // Viewport tracking
        _viewport: null,

        /**
         * Inicializa el cliente WebImage.
         * @param {Object} config - Configuración del cliente
         * @param {string} config.elementId - ID del elemento que mostrará las imágenes
         * @param {number} [config.intervalMs=250] - Intervalo entre frames en milisegundos
         * @param {string} [config.imageFit='cover'] - Modo de ajuste (fill/cover/contain)
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

            if (config.intervalMs && config.intervalMs >= 3) {
                this._intervalMs = config.intervalMs;
            }

            // Configurar viewport tracking
            this._viewport = window.visualViewport;
            this._setupViewport();

            // Configurar prevención de gestos nativos
            this._preventNativeGestures();

            // Iniciar polling
            this._start();

            log.info('Initialized', {
                elementId: config.elementId,
                intervalMs: this._intervalMs,
                imageFit: config.imageFit || 'cover'
            });
        },

        /**
         * Configura el tracking de viewport para dispositivos móviles.
         * @private
         */
        _setupViewport() {
            const syncViewport = () => {
                const width = this._viewport ? this._viewport.width : window.innerWidth;
                const height = this._viewport ? this._viewport.height : window.innerHeight;
                document.documentElement.style.setProperty('--vw', Math.round(width) + 'px');
                document.documentElement.style.setProperty('--vh', Math.round(height) + 'px');
            };

            window.addEventListener('resize', syncViewport);
            window.addEventListener('orientationchange', syncViewport);

            if (this._viewport) {
                this._viewport.addEventListener('resize', syncViewport);
                this._viewport.addEventListener('scroll', syncViewport);
            }

            syncViewport();
        },

        /**
         * Previene gestos nativos del navegador.
         * @private
         */
        _preventNativeGestures() {
            const preventNative = (e) => {
                e.preventDefault();
            };

            // iOS Safari: evita drag-and-drop/long-press sobre la capa de stream
            this._screenElement.addEventListener('dragstart', preventNative, { passive: false });
            this._screenElement.addEventListener('contextmenu', preventNative, { passive: false });
            this._screenElement.addEventListener('touchstart', preventNative, { passive: false });
            this._screenElement.addEventListener('touchmove', preventNative, { passive: false });
            this._screenElement.addEventListener('touchend', preventNative, { passive: false });
            document.addEventListener('gesturestart', preventNative, { passive: false });
            document.addEventListener('gesturechange', preventNative, { passive: false });
        },

        /**
         * Inicia el polling de imágenes.
         * @private
         */
        _start() {
            this._isRunning = true;
            this._next();
        },

        /**
         * Detiene el polling de imágenes.
         */
        stop() {
            this._isRunning = false;
        },

        /**
         * Carga el siguiente frame.
         * @private
         */
        _next() {
            if (!this._isRunning) return;

            const pre = new Image();

            pre.onload = () => {
                this._screenElement.style.backgroundImage = "url('" + pre.src + "')";
                setTimeout(() => this._next(), this._intervalMs);
            };

            pre.onerror = () => {
                setTimeout(() => this._next(), this._intervalMs * 4);
            };

            pre.src = '/cap?s=' + (++this._seq);
        }
    };

    // Exponer al scope global
    global.WebImageClient = WebImageClient;

})(window);
