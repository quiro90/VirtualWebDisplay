/**
 * Keep-alive signal para mantener la sesión activa.
 * Envía pings periódicos al servidor para evitar timeout de conexión.
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

    /**
     * Sistema de keep-alive para mantener sesión activa.
     */
    const Keepalive = {
        _intervalId: null,
        _intervalMs: 10000,

        // Constantes (sincronizadas con Configuration/TouchInputConstants.cs)
        _MIN_INTERVAL_MS: 1000,      // TouchInputConstants.MinKeepaliveIntervalMs
        _DEFAULT_INTERVAL_MS: 10000, // TouchInputConstants.DefaultKeepaliveIntervalMs

        /**
         * Inicia el sistema de keep-alive.
         * @param {number} [intervalMs=10000] - Intervalo entre pings en milisegundos (mínimo 1000ms)
         */
        start(intervalMs) {
            if (intervalMs && intervalMs >= this._MIN_INTERVAL_MS) {
                this._intervalMs = intervalMs;
            } else {
                this._intervalMs = this._DEFAULT_INTERVAL_MS;
            }

            this._ping();
            this._intervalId = setInterval(() => this._ping(), this._intervalMs);

            log.info('Started with interval:', this._intervalMs + 'ms');
        },

        /**
         * Detiene el sistema de keep-alive.
         */
        stop() {
            if (this._intervalId) {
                clearInterval(this._intervalId);
                this._intervalId = null;
                log.info('Stopped');
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
        }
    };

    // Exponer al scope global
    global.Keepalive = Keepalive;

})(window);
