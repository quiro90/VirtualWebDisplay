/**
 * Cliente WebRTC para VirtualWebDisplay.
 * Recibe el stream H.264 del servidor a través de un VideoTrack RTP
 * y lo muestra en un elemento <video> nativo.
 *
 * @namespace WebRtcClient
 */
(function(global) {
    'use strict';

    const log = global.Logger ? global.Logger.create('[WebRtcClient]') : {
        info:  console.log.bind(console,   '[WebRtcClient]'),
        warn:  console.warn.bind(console,  '[WebRtcClient]'),
        error: console.error.bind(console, '[WebRtcClient]'),
        debug: console.debug.bind(console, '[WebRtcClient]')
    };

    const WebRtcClient = {
        _config: null,
        _videoElement: null,
        _statusElement: null,
        _peerConnection: null,
        _statsTimerId: null,
        _reconnectTimerId: null,
        _lastBytesReceived: 0,
        _stalledIntervals: 0,

        _texts: {
            connecting:           'Connecting...',
            negotiating:          'Negotiating...',
            connected:            'Connected',
            disconnectedRetrying: 'Disconnected, retrying...',
            errorRetrying:        'Error, retrying...',
            negotiationFailed:    'Negotiation failed',
            viewerLimitFull:      'Viewer limit reached',
            startFailed:          'Start failed'
        },

        /**
         * Initializes the WebRTC client.
         * @param {Object} config
         * @param {string} config.videoId           - ID of the <video> element
         * @param {string} [config.statusElementId] - ID of the status label
         * @param {Object} [config.texts]           - Localized status strings
         */
        init(config) {
            if (!config || !config.videoId) {
                log.error('Configuration error: videoId is required');
                return;
            }

            this._config = config;
            this._videoElement = document.getElementById(config.videoId);

            if (!this._videoElement) {
                log.error('Video element not found:', config.videoId);
                return;
            }

            if (config.statusElementId) {
                this._statusElement = document.getElementById(config.statusElementId);
            }

            if (config.texts) {
                Object.assign(this._texts, config.texts);
            }

            this._videoElement.addEventListener('loadedmetadata', () => {
                log.info('Video metadata loaded', {
                    width: this._videoElement.videoWidth,
                    height: this._videoElement.videoHeight
                });
            });

            this._connect();

            log.info('Initialized', { videoId: config.videoId });
        },

        _setStatus(text) {
            if (this._statusElement) this._statusElement.textContent = text;
        },

        _waitForIceGatheringComplete(pc) {
            if (pc.iceGatheringState === 'complete') return Promise.resolve();
            return new Promise((resolve) => {
                const check = () => {
                    if (pc.iceGatheringState === 'complete') {
                        pc.removeEventListener('icegatheringstatechange', check);
                        resolve();
                    }
                };
                pc.addEventListener('icegatheringstatechange', check);
            });
        },

        async _connect() {
            try {
                this._cleanupPeerConnection();
                this._setStatus(this._texts.connecting);

                const pc = new RTCPeerConnection({ iceServers: [] });
                this._peerConnection = pc;
                this._lastBytesReceived = 0;
                this._stalledIntervals = 0;

                pc.addTransceiver('video', { direction: 'recvonly' });

                pc.ontrack = (event) => {
                    if (event.track.kind === 'video') {
                        event.track.onunmute = () => log.info('Video track unmuted');
                        event.track.onmute = () => log.warn('Video track muted');

                        this._videoElement.srcObject = event.streams[0] ?? new MediaStream([event.track]);
                        this._videoElement.play().catch(() => {});
                        this._setStatus(this._texts.connected);
                        log.info('Video track attached');
                    }
                };

                pc.onconnectionstatechange = () => {
                    log.debug('Connection state:', pc.connectionState);
                    if (pc.connectionState === 'failed' ||
                        pc.connectionState === 'disconnected' ||
                        pc.connectionState === 'closed') {
                        this._setStatus(this._texts.disconnectedRetrying);
                        this._scheduleReconnect(1500);
                    }
                };

                pc.oniceconnectionstatechange = () => {
                    log.debug('ICE state:', pc.iceConnectionState);
                };

                this._setStatus(this._texts.negotiating);

                const offer = await pc.createOffer();
                await pc.setLocalDescription(offer);
                await this._waitForIceGatheringComplete(pc);

                const response = await fetch('/webrtc/offer', {
                    method:  'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body:    JSON.stringify({
                        sdp:  pc.localDescription.sdp,
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

                this._startStatsLoop(pc);

            } catch (error) {
                log.error('Connection error:', error);
                this._setStatus(this._texts.startFailed);
                this._scheduleReconnect(2000);
            }
        },

        _scheduleReconnect(delayMs) {
            if (this._reconnectTimerId) {
                clearTimeout(this._reconnectTimerId);
            }
            this._reconnectTimerId = setTimeout(() => {
                this._reconnectTimerId = null;
                this._connect();
            }, delayMs);
        },

        _cleanupPeerConnection() {
            if (this._statsTimerId) {
                clearInterval(this._statsTimerId);
                this._statsTimerId = null;
            }

            if (this._peerConnection) {
                try { this._peerConnection.close(); } catch (_) {}
                this._peerConnection = null;
            }
        },

        _startStatsLoop(pc) {
            if (this._statsTimerId) {
                clearInterval(this._statsTimerId);
            }

            this._statsTimerId = setInterval(async () => {
                if (this._peerConnection !== pc) return;

                try {
                    const stats = await pc.getStats();
                    let inboundVideo = null;

                    stats.forEach((report) => {
                        if (report.type === 'inbound-rtp' && report.kind === 'video') {
                            inboundVideo = report;
                        }
                    });

                    if (!inboundVideo) {
                        log.warn('No inbound-rtp video stats yet');
                        return;
                    }

                    const bytes = inboundVideo.bytesReceived || 0;
                    const framesDecoded = inboundVideo.framesDecoded || 0;
                    const packetsLost = inboundVideo.packetsLost || 0;
                    const framesPerSecond = inboundVideo.framesPerSecond || 0;

                    if (bytes <= this._lastBytesReceived) {
                        this._stalledIntervals++;
                    } else {
                        this._stalledIntervals = 0;
                    }

                    this._lastBytesReceived = bytes;

                    log.info('RTC stats', {
                        bytesReceived: bytes,
                        framesDecoded,
                        framesPerSecond,
                        packetsLost,
                        stalledIntervals: this._stalledIntervals
                    });

                    if (this._stalledIntervals >= 4 && pc.connectionState === 'connected') {
                        log.warn('RTC appears stalled while connected; reconnecting');
                        this._setStatus(this._texts.errorRetrying);
                        this._scheduleReconnect(800);
                    }
                } catch (error) {
                    log.warn('Error reading RTC stats', error);
                }
            }, 2000);
        }
    };

    global.WebRtcClient = WebRtcClient;

})(window);
