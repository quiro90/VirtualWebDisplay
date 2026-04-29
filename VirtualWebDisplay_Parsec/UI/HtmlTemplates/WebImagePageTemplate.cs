using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// HTML template for the WebImage page (periodically refreshed JPEG image).
/// Refactored to use external JavaScript files for better maintainability.
/// </summary>
public sealed class WebImagePageTemplate : IHtmlTemplate
{
    // Versión dinámica sincronizada con el ensamblado (cache busting automático)
    private static string AppVersion => TemplateVersionHelper.AppVersion;

    public string Generate(Dictionary<string, object> parameters)
    {
        // Usar helpers para evitar duplicación
        var title = TemplateParameterHelper.GetTitle(parameters);
        var browserImageFit = TemplateParameterHelper.GetBrowserImageFit(parameters);
        var backgroundSize = TemplateParameterHelper.GetBackgroundSize(browserImageFit);
        var intervalMs = TemplateParameterHelper.GetIntervalMs(parameters);
        var gestureHoldDelayMs = TemplateParameterHelper.GetGestureHoldDelayMs(parameters);
        var throttleMs = TemplateParameterHelper.CalculateThrottleMs(intervalMs);
        var htmlLang = AppText.HtmlLang;

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

                    :root {
                        --vw: 100vw;
                        --vh: 100vh;
                    }

                    html, body {
                        width: 100%; height: 100%;
                        background: #000;
                        overflow: hidden;
                        touch-action: manipulation;
                        -webkit-user-select: none;
                        user-select: none;
                        -webkit-touch-callout: none;
                        -webkit-tap-highlight-color: transparent;
                    }

                    #screen {
                        position: fixed;
                        inset: 0;
                        width: var(--vw);
                        height: var(--vh);
                        background-position: center center;
                        background-repeat: no-repeat;
                        background-size: {{backgroundSize}};
                        display: block;
                        image-rendering: auto;
                        background-color: #000;
                        touch-action: none;
                        -webkit-user-drag: none;
                        -webkit-touch-callout: none;
                        -webkit-user-select: none;
                        user-select: none;
                    }
                </style>
            </head>
            <body>
                <div id="screen" aria-label="screen" role="img"></div>

                <!-- External JavaScript modules -->
                <script src="/js/common/logger.js?v={{AppVersion}}"></script>
                <script src="/js/common/keepalive.js?v={{AppVersion}}"></script>
                <script src="/js/webimage/webimage-client.js?v={{AppVersion}}"></script>
                <script src="/js/touch/touch-input.js?v={{AppVersion}}"></script>

                <!-- Initialization -->
                <script>
                (function() {
                    'use strict';

                    // Initialize keep-alive
                    if (typeof Keepalive !== 'undefined') {
                        Keepalive.start(10000);
                    }

                    // Initialize WebImage client
                    if (typeof WebImageClient !== 'undefined') {
                        WebImageClient.init({
                            elementId: 'screen',
                            intervalMs: {{intervalMs}},
                            imageFit: '{{browserImageFit}}'
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
