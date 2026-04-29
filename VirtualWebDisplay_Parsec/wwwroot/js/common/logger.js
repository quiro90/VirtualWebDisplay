/**
 * Sistema de logging configurable para módulos JavaScript de VirtualWebDisplay.
 * Permite controlar la verbosidad de logs en diferentes entornos (development/production).
 * 
 * @namespace Logger
 */
(function(global) {
    'use strict';

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
        _level: LogLevel.INFO, // Nivel por defecto
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

    // Detectar entorno automáticamente
    const isProduction = global.location && global.location.hostname !== 'localhost';
    Logger.setLevel(isProduction ? LogLevel.WARN : LogLevel.INFO);

    // Exponer al scope global
    global.Logger = Logger;
    global.LogLevel = LogLevel;

})(window);
