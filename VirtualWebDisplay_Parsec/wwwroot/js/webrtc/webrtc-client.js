/**
 * Cliente WebRTC para VirtualWebDisplay.
 * Maneja la conexión peer-to-peer, recepción de frames chunkeados
 * y renderizado en canvas con modos de ajuste (fill/cover/contain).
 * 
 * @namespace WebRtcClient
 */
(function(global) {
    'use strict';

    // Logger especializado
    const log = global.Logger ? global.Logger.create('[WebRtcClient]') : {
        info: console.log.bind(console, '[WebRtcClient]'),
        warn: console.warn.bind(console, '[WebRtcClient]'),
        error: console.error.bind(console, '[WebRtcClient]'),
        debug: console.debug.bind(console, '[WebRtcClient]')
    };

    /**
     * Cliente WebRTC para streaming de frames JPEG.
     */
    const WebRtcClient = {
        // Configuración
        _config: null,
        _canvas: null,
        _ctx: null,
        _statusElement: null,

        // Estado de conexión
        _peerConnection: null,
        _dataChannel: null,

        // Reensamblado de frames
        _currentFrameId: -1,
        _frameInfo: null,
        _frameBuffers: [],
        _receivedBytes: 0,

        // Textos localizados
        _texts: {
            connecting: 'Connecting...',
            negotiating: 'Negotiating...',
            connected: 'Connected',
            disconnectedRetrying: 'Disconnected, retrying...',
            errorRetrying: 'Error, retrying...',
            negotiationFailed: 'Negotiation failed',
            viewerLimitFull: 'Viewer limit reached',
            startFailed: 'Start failed'
        },

        /**
         * Inicializa el cliente WebRTC.
         * @param {Object} config - Configuración del cliente
         * @param {string} config.canvasId - ID del elemento canvas
         * @param {string} [config.statusElementId] - ID del elemento de estado
         * @param {string} [config.imageFit='cover'] - Modo de ajuste (fill/cover/contain)
         * @param {Object} [config.texts] - Textos localizados para estados
         */
        init(config) {
            if (!config || !config.canvasId) {
                log.error('Configuration error: canvasId is required');
                return;
            }

            this._config = config;
            this._canvas = document.getElementById(config.canvasId);

            if (!this._canvas) {
                log.error('Canvas element not found:', config.canvasId);
                return;
            }

            this._ctx = this._canvas.getContext('2d');

            if (config.statusElementId) {
                this._statusElement = document.getElementById(config.statusElementId);
            }

            // Aplicar textos localizados si existen
            if (config.texts) {
                Object.assign(this._texts, config.texts);
            }

            // Configurar listeners de resize
            window.addEventListener('resize', () => this._syncCanvasSize());
            this._syncCanvasSize();

            // Iniciar conexión
            this._connect();

            log.info('Initialized', {
                canvasId: config.canvasId,
                imageFit: config.imageFit || 'cover'
            });
        },

        /**
         * Establece el texto de estado.
         * @private
         */
        _setStatus(text) {
            if (this._statusElement) {
                this._statusElement.textContent = text;
            }
        },

        /**
         * Sincroniza el tamaño del canvas con la ventana.
         * @private
         */
        _syncCanvasSize() {
            const w = window.innerWidth;
            const h = window.innerHeight;
            if (this._canvas.width !== w || this._canvas.height !== h) {
                this._canvas.width = w;
                this._canvas.height = h;
            }
        },

        /**
         * Espera a que la recolección ICE esté completa.
         * @private
         */
        _waitForIceGatheringComplete(pc) {
            if (pc.iceGatheringState === 'complete') {
                return Promise.resolve();
            }

            return new Promise((resolve) => {
                const checkState = () => {
                    if (pc.iceGatheringState === 'complete') {
                        pc.removeEventListener('icegatheringstatechange', checkState);
                        resolve();
                    }
                };

                pc.addEventListener('icegatheringstatechange', checkState);
            });
        },

        /**
         * Resetea el ensamblado de frames.
         * @private
         */
        _resetFrameAssembly(meta) {
            this._currentFrameId = meta.id;
            this._frameInfo = meta;
            this._frameBuffers = [];
            this._receivedBytes = 0;
        },

        /**
         * Dibuja una imagen en el canvas con el modo de ajuste configurado.
         * @private
         */
        _drawFit(bitmap) {
            const cw = this._canvas.width;
            const ch = this._canvas.height;
            const bw = bitmap.width;
            const bh = bitmap.height;
            const fit = this._config.imageFit || 'cover';

            this._ctx.clearRect(0, 0, cw, ch);

            if (fit === 'fill') {
                this._ctx.drawImage(bitmap, 0, 0, cw, ch);
            } else if (fit === 'cover') {
                const scale = Math.max(cw / bw, ch / bh);
                const sw = bw * scale;
                const sh = bh * scale;
                this._ctx.drawImage(bitmap, (cw - sw) / 2, (ch - sh) / 2, sw, sh);
            } else {
                // contain
                const scale = Math.min(cw / bw, ch / bh);
                const sw = bw * scale;
                const sh = bh * scale;
                this._ctx.drawImage(bitmap, (cw - sw) / 2, (ch - sh) / 2, sw, sh);
            }
        },

        /**
         * Aplica un frame completo al canvas.
         * @private
         */
        _applyFrame(bytes) {
            createImageBitmap(new Blob([bytes], { type: 'image/jpeg' }))
                .then((bitmap) => {
                    this._syncCanvasSize();
                    this._drawFit(bitmap);
                    bitmap.close();
                });
        },

        /**
         * Conecta al servidor WebRTC.
         * @private
         */
        async _connect() {
            try {
                this._setStatus(this._texts.negotiating);

                const pc = new RTCPeerConnection({ iceServers: [] });
                this._peerConnection = pc;

                const channel = pc.createDataChannel('frames', { 
                    ordered: false, 
                    maxRetransmits: 0 
                });
                channel.binaryType = 'arraybuffer';
                this._dataChannel = channel;

                // Configurar handlers del canal
                channel.onopen = () => {
                    this._setStatus(this._texts.connected);
                };

                channel.onclose = () => {
                    this._setStatus(this._texts.disconnectedRetrying);
                    setTimeout(() => this._connect(), 1500);
                };

                channel.onerror = () => {
                    this._setStatus(this._texts.errorRetrying);
                };

                channel.onmessage = (event) => this._handleMessage(event);

                pc.onconnectionstatechange = () => {
                    if (pc.connectionState === 'failed' || 
                        pc.connectionState === 'disconnected' || 
                        pc.connectionState === 'closed') {
                        this._setStatus(this._texts.disconnectedRetrying);
                    }
                };

                // Crear offer
                const offer = await pc.createOffer();
                await pc.setLocalDescription(offer);
                await this._waitForIceGatheringComplete(pc);

                // Enviar offer al servidor
                const response = await fetch('/webrtc/offer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        sdp: pc.localDescription.sdp, 
                        type: pc.localDescription.type 
                    })
                });

                if (response.status === 429) {
                    const payload = await response.json().catch(() => ({}));
                    this._setStatus(payload.error || this._texts.viewerLimitFull);
                    pc.close();
                    return;
                }

                if (!response.ok) {
                    throw new Error(this._texts.negotiationFailed);
                }

                const answer = await response.json();
                await pc.setRemoteDescription(answer);

            } catch (error) {
                log.error('Connection error:', error);
                this._setStatus(this._texts.startFailed);
                setTimeout(() => this._connect(), 2000);
            }
        },

        /**
         * Maneja mensajes del DataChannel.
         * @private
         */
        _handleMessage(event) {
            // Metadatos JSON
            if (typeof event.data === 'string') {
                try {
                    const meta = JSON.parse(event.data);
                    if (meta.type === 'frame' && meta.size > 0) {
                        this._resetFrameAssembly(meta);
                    }
                } catch (e) {
                    // Ignorar JSON inválido
                }
                return;
            }

            // Chunks binarios
            if (!this._frameInfo) return;

            const data = new Uint8Array(event.data);
            if (data.length < 4) return;

            // Leer frameId (little-endian)
            const chunkFrameId = data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
            if (chunkFrameId !== this._currentFrameId) return;

            const chunk = data.subarray(4);
            this._frameBuffers.push(chunk);
            this._receivedBytes += chunk.byteLength;

            if (this._receivedBytes < this._frameInfo.size) return;

            // Frame completo, reensamblar
            const completedFrame = new Uint8Array(this._frameInfo.size);
            let offset = 0;
            for (let i = 0; i < this._frameBuffers.length; i++) {
                completedFrame.set(this._frameBuffers[i], offset);
                offset += this._frameBuffers[i].byteLength;
            }

            this._applyFrame(completedFrame);

            // Limpiar estado
            this._frameInfo = null;
            this._frameBuffers = [];
            this._receivedBytes = 0;
        }
    };

    // Exponer al scope global
    global.WebRtcClient = WebRtcClient;

})(window);
