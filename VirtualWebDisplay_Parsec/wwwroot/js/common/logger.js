/**
 * Sistema de logging configurable para módulos JavaScript de VirtualWebDisplay.
 * Permite controlar la verbosidad de logs en diferentes entornos (development/production).
 * 
 * @namespace Logger
 */
(function(global) {
    'use strict';

    const NoopLogger = Object.freeze({
        error() {},
        warn() {},
        info() {},
        debug() {}
    });

    /**
     * Niveles de logging disponibles.
     * @enum {number}
     */
    const LogLevel = {
        SILENT: 0,  // Sin logs
        ERROR: 1,   // Solo errores críticos
        WARN: 2,    // Advertencias + errores
        INFO: 3,    // Información general + advertencias + errores
        DEBUG: 4    // Todo (debugging completo)
    };

    /**
     * Sistema de logging centralizado.
     */
    const Logger = {
        _level: LogLevel.SILENT, // Nivel por defecto
        _prefix: '',

        /**
         * Configura el nivel de logging.
         * @param {number} level - Nivel de logging (use LogLevel enum)
         */
        setLevel(level) {
            if (level >= LogLevel.SILENT && level <= LogLevel.DEBUG) {
                this._level = level;
            }
        },

        /**
         * Establece el prefijo para todos los logs.
         * @param {string} prefix - Prefijo a agregar (ej: '[TouchInput]')
         */
        setPrefix(prefix) {
            this._prefix = prefix;
        },

        /**
         * Log de error (nivel ERROR).
         * @param {...*} args - Argumentos a loguear
         */
        error(...args) {
            if (this._level >= LogLevel.ERROR) {
                console.error(this._prefix, ...args);
            }
        },

        /**
         * Log de advertencia (nivel WARN).
         * @param {...*} args - Argumentos a loguear
         */
        warn(...args) {
            if (this._level >= LogLevel.WARN) {
                console.warn(this._prefix, ...args);
            }
        },

        /**
         * Log informativo (nivel INFO).
         * @param {...*} args - Argumentos a loguear
         */
        info(...args) {
            if (this._level >= LogLevel.INFO) {
                console.info(this._prefix, ...args);
            }
        },

        /**
         * Log de debugging (nivel DEBUG).
         * @param {...*} args - Argumentos a loguear
         */
        debug(...args) {
            if (this._level >= LogLevel.DEBUG) {
                console.debug(this._prefix, ...args);
            }
        },

        /**
         * Crea un logger especializado con prefijo fijo.
         * @param {string} prefix - Prefijo para el nuevo logger
         * @returns {Object} Nuevo logger con prefijo
         */
        create(prefix) {
            if (Logger._level <= LogLevel.SILENT) {
                return NoopLogger;
            }

            return {
                error: (...args) => {
                    if (Logger._level >= LogLevel.ERROR) {
                        console.error(prefix, ...args);
                    }
                },
                warn: (...args) => {
                    if (Logger._level >= LogLevel.WARN) {
                        console.warn(prefix, ...args);
                    }
                },
                info: (...args) => {
                    if (Logger._level >= LogLevel.INFO) {
                        console.info(prefix, ...args);
                    }
                },
                debug: (...args) => {
                    if (Logger._level >= LogLevel.DEBUG) {
                        console.debug(prefix, ...args);
                    }
                }
            };
        }
    };

    function tryGetConfiguredLevel() {
        if (!global.location) {
            return null;
        }

        const search = new URLSearchParams(global.location.search || '');
        const value = (search.get('log') || '').trim().toLowerCase();
        switch (value) {
        case 'silent':
        case 'off':
            return LogLevel.SILENT;
        case 'error':
            return LogLevel.ERROR;
        case 'warn':
        case 'warning':
            return LogLevel.WARN;
        case 'info':
            return LogLevel.INFO;
        case 'debug':
            return LogLevel.DEBUG;
        default:
            return null;
        }
    }

    function isDevelopmentHost(hostname) {
        if (!hostname) {
            return false;
        }

        return hostname === 'localhost'
            || hostname === '127.0.0.1'
            || hostname === '::1';
    }

    const configuredLevel = tryGetConfiguredLevel();
    if (configuredLevel !== null) {
        Logger.setLevel(configuredLevel);
    } else {
        const hostname = global.location ? global.location.hostname : '';
        Logger.setLevel(isDevelopmentHost(hostname) ? LogLevel.DEBUG : LogLevel.SILENT);
    }

    // Exponer al scope global
    global.Logger = Logger;
    global.LogLevel = LogLevel;
    global.NoopLogger = NoopLogger;

})(window);
