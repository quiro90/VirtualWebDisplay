using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.Web.HtmlTemplates;

/// <summary>
/// HTML template for the WebRTC page (continuous live stream).
/// Refactored to use external JavaScript files for better maintainability.
/// </summary>
public sealed class RtcPageTemplate : IHtmlTemplate
{
    // Versión dinámica sincronizada con el ensamblado (cache busting automático)
    private static string AppVersion => TemplateVersionHelper.AppVersion;

    public string Generate(Dictionary<string, object> parameters)
    {
        // Usar helpers para evitar duplicación
        var title = TemplateParameterHelper.GetTitle(parameters);
        var browserImageFit = TemplateParameterHelper.GetBrowserImageFit(parameters);
        var intervalMs = TemplateParameterHelper.GetIntervalMs(parameters);
        var gestureHoldDelayMs = TemplateParameterHelper.GetGestureHoldDelayMs(parameters);
        var throttleMs = TemplateParameterHelper.CalculateThrottleMs(intervalMs);

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

                <!-- External JavaScript modules -->
                <script src="/js/common/logger.js?v={{AppVersion}}"></script>
                <script src="/js/common/keepalive.js?v={{AppVersion}}"></script>
                <script src="/js/webrtc/webrtc-client.js?v={{AppVersion}}"></script>
                <script src="/js/touch/touch-input.js?v={{AppVersion}}"></script>

                <!-- Initialization -->
                <script>
                (function() {
                    'use strict';

                    // Initialize keep-alive
                    if (typeof Keepalive !== 'undefined') {
                        Keepalive.start(10000);
                    }

                    // Initialize WebRTC client
                    if (typeof WebRtcClient !== 'undefined') {
                        WebRtcClient.init({
                            canvasId: 'screen',
                            statusElementId: 'status',
                            imageFit: '{{browserImageFit}}',
                            texts: {
                                connecting: '{{statusConnecting}}',
                                negotiating: '{{statusNegotiating}}',
                                connected: '{{statusConnected}}',
                                disconnectedRetrying: '{{statusDisconnectedRetrying}}',
                                errorRetrying: '{{statusErrorRetrying}}',
                                negotiationFailed: '{{statusNegotiationFailed}}',
                                viewerLimitFull: '{{statusViewerLimitFull}}',
                                startFailed: '{{statusStartFailed}}'
                            }
                        });
                    }

                    // Initialize touch input
                    if (typeof TouchInput !== 'undefined') {
                        TouchInput.init({
                            elementId: 'screen',
                            throttleMs: {{throttleMs}},
                            holdDelayMs: {{gestureHoldDelayMs}}
                        });
                    }
                })();
                </script>
            </body>
            </html>
            """;
    }
}
