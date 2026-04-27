using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// HTML template for the WebRTC page (continuous live stream).
/// </summary>
public sealed class RtcPageTemplate : IHtmlTemplate
{
    public string Generate(Dictionary<string, object> parameters)
    {
        var title = parameters.GetValueOrDefault("title", "VirtualWebDisplay") as string ?? "VirtualWebDisplay";
        var browserImageFit = parameters.GetValueOrDefault("browserImageFit", "cover") as string ?? "cover";
        var touchInputEnabledObj = parameters.GetValueOrDefault("touchInputEnabled", false);
        var touchInputEnabled = touchInputEnabledObj is bool boolVal && boolVal;
        var htmlLang = AppText.HtmlLang;
        var statusConnecting = AppText.Get("WebRtc_Status_Connecting");
        var statusNegotiating = AppText.Get("WebRtc_Status_Negotiating");
        var statusConnected = AppText.Get("WebRtc_Status_Connected");
        var statusDisconnectedRetrying = AppText.Get("WebRtc_Status_DisconnectedRetrying");
        var statusErrorRetrying = AppText.Get("WebRtc_Status_ErrorRetrying");
        var statusNegotiationFailed = AppText.Get("WebRtc_Status_NegotiationFailed");
        var statusViewerLimitFull = AppText.Get("Program_ViewerLimit_Full_Error");
        var statusStartFailed = AppText.Get("WebRtc_Status_StartFailed");

        return $$"""
            <!DOCTYPE html>
            <html lang="{{htmlLang}}">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0,
                      maximum-scale=1.0, minimum-scale=1.0, user-scalable=no">
                <title>{{title}}</title>
                <style>
                    *, *::before, *::after { margin: 0; padding: 0; box-sizing: border-box; }

                    html, body {
                        width: 100%; height: 100%;
                        background: #000;
                        overflow: hidden;
                        touch-action: manipulation;
                        -webkit-tap-highlight-color: transparent;
                    }

                    #screen {
                        position: fixed;
                        inset: 0;
                        width: 100vw;
                        height: 100vh;
                        display: block;
                        background: #000;
                    }

                    #mode {
                        position: fixed;
                        right: 10px;
                        bottom: 10px;
                        padding: 6px 10px;
                        border-radius: 999px;
                        background: rgba(0, 0, 0, 0.45);
                        color: #fff;
                        font: 12px/1.2 sans-serif;
                    }

                    #status {
                        position: fixed;
                        left: 10px;
                        bottom: 10px;
                        max-width: calc(100vw - 140px);
                        padding: 6px 10px;
                        border-radius: 999px;
                        background: rgba(0, 0, 0, 0.45);
                        color: #fff;
                        font: 12px/1.2 sans-serif;
                        white-space: nowrap;
                        overflow: hidden;
                        text-overflow: ellipsis;
                    }
                </style>
            </head>
            <body>
                <canvas id="screen"></canvas>
                <div id="mode">WebRTC</div>
                <div id="status">{{statusConnecting}}</div>

                <script>
                (function () {
                    var canvas = document.getElementById('screen');
                    var ctx = canvas.getContext('2d');
                    var status = document.getElementById('status');
                    var fit = '{{browserImageFit}}';
                    var currentFrameId = -1;
                    var frameInfo = null;
                    var frameBuffers = [];
                    var receivedBytes = 0;

                    function setStatus(text) {
                        status.textContent = text;
                    }

                    function waitForIceGatheringComplete(pc) {
                        if (pc.iceGatheringState === 'complete')
                            return Promise.resolve();

                        return new Promise(function (resolve) {
                            function checkState() {
                                if (pc.iceGatheringState === 'complete') {
                                    pc.removeEventListener('icegatheringstatechange', checkState);
                                    resolve();
                                }
                            }

                            pc.addEventListener('icegatheringstatechange', checkState);
                        });
                    }

                    function resetFrameAssembly(meta) {
                        currentFrameId = meta.id;
                        frameInfo = meta;
                        frameBuffers = [];
                        receivedBytes = 0;
                    }

                    function syncCanvasSize() {
                        var w = window.innerWidth, h = window.innerHeight;
                        if (canvas.width !== w || canvas.height !== h) {
                            canvas.width = w;
                            canvas.height = h;
                        }
                    }

                    function drawFit(bitmap) {
                        var cw = canvas.width, ch = canvas.height;
                        var bw = bitmap.width, bh = bitmap.height;
                        ctx.clearRect(0, 0, cw, ch);
                        if (fit === 'fill') {
                            ctx.drawImage(bitmap, 0, 0, cw, ch);
                        } else if (fit === 'cover') {
                            var scale = Math.max(cw / bw, ch / bh);
                            var sw = bw * scale, sh = bh * scale;
                            ctx.drawImage(bitmap, (cw - sw) / 2, (ch - sh) / 2, sw, sh);
                        } else {
                            var scale = Math.min(cw / bw, ch / bh);
                            var sw = bw * scale, sh = bh * scale;
                            ctx.drawImage(bitmap, (cw - sw) / 2, (ch - sh) / 2, sw, sh);
                        }
                    }

                    function applyFrame(bytes) {
                        createImageBitmap(new Blob([bytes], { type: 'image/jpeg' })).then(function (bitmap) {
                            syncCanvasSize();
                            drawFit(bitmap);
                            bitmap.close();
                        });
                    }

                    async function connect() {
                        setStatus('{{statusNegotiating}}');

                        var pc = new RTCPeerConnection({ iceServers: [] });
                        var channel = pc.createDataChannel('frames', { ordered: false, maxRetransmits: 0 });
                        channel.binaryType = 'arraybuffer';

                        channel.onopen = function () {
                            setStatus('{{statusConnected}}');
                        };

                        channel.onclose = function () {
                            setStatus('{{statusDisconnectedRetrying}}');
                            window.setTimeout(connect, 1500);
                        };

                        channel.onerror = function () {
                            setStatus('{{statusErrorRetrying}}');
                        };

                        channel.onmessage = function (event) {
                            if (typeof event.data === 'string') {
                                try {
                                    var meta = JSON.parse(event.data);
                                    if (meta.type === 'frame' && meta.size > 0)
                                        resetFrameAssembly(meta);
                                }
                                catch {
                                }

                                return;
                            }

                            if (!frameInfo)
                                return;

                            var data = new Uint8Array(event.data);
                            if (data.length < 4)
                                return;

                            var chunkFrameId = data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);
                            if (chunkFrameId !== currentFrameId)
                                return;

                            var chunk = data.subarray(4);
                            frameBuffers.push(chunk);
                            receivedBytes += chunk.byteLength;

                            if (receivedBytes < frameInfo.size)
                                return;

                            var completedFrame = new Uint8Array(frameInfo.size);
                            var offset = 0;
                            for (var i = 0; i < frameBuffers.length; i++) {
                                completedFrame.set(frameBuffers[i], offset);
                                offset += frameBuffers[i].byteLength;
                            }

                            applyFrame(completedFrame);
                            frameInfo = null;
                            frameBuffers = [];
                            receivedBytes = 0;
                        };

                        pc.onconnectionstatechange = function () {
                            if (pc.connectionState === 'failed' || pc.connectionState === 'disconnected' || pc.connectionState === 'closed')
                                setStatus('{{statusDisconnectedRetrying}}');
                        };

                        var offer = await pc.createOffer();
                        await pc.setLocalDescription(offer);
                        await waitForIceGatheringComplete(pc);

                        var response = await fetch('/webrtc/offer', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ sdp: pc.localDescription.sdp, type: pc.localDescription.type })
                        });

                        if (response.status === 429) {
                            var payload = await response.json().catch(function () { return {}; });
                            setStatus(payload.error || '{{statusViewerLimitFull}}');
                            pc.close();
                            return;
                        }

                        if (!response.ok)
                            throw new Error('{{statusNegotiationFailed}}');

                        var answer = await response.json();
                        await pc.setRemoteDescription(answer);
                    }

                    window.addEventListener('resize', syncCanvasSize);
                    syncCanvasSize();

                    function startKeepAliveSignal() {
                        function ping() {
                            fetch('/keepalive?t=' + Date.now(), {
                                method: 'GET',
                                cache: 'no-store',
                                keepalive: true,
                                credentials: 'same-origin'
                            }).catch(function () {});
                        }

                        ping();
                        setInterval(ping, 10000);
                    }

                    startKeepAliveSignal();

                    {{TouchInputScriptHelper.GenerateTouchInputScript("screen", 50, touchInputEnabled)}}

                    connect().catch(function () {
                        setStatus('{{statusStartFailed}}');
                        window.setTimeout(connect, 2000);
                    });
                })();
                </script>
            </body>
            </html>
            """;
    }
}
