using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// Template para la página de WebImage (imagen JPEG refrescada periódicamente).
/// </summary>
public sealed class WebImagePageTemplate : IHtmlTemplate
{
    public string Generate(Dictionary<string, object> parameters)
    {
        var title = parameters.GetValueOrDefault("title", "VirtualWebDisplay") as string ?? "VirtualWebDisplay";
        var browserImageFit = parameters.GetValueOrDefault("browserImageFit", "cover") as string ?? "cover";
        var intervalMs = parameters.GetValueOrDefault("intervalMs", 250);
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
                        -webkit-tap-highlight-color: transparent;
                    }

                    #screen {
                        position: fixed;
                        inset: 0;
                        width: var(--vw);
                        height: var(--vh);
                        object-fit: {{browserImageFit}};
                        object-position: center center;
                        display: block;
                        image-rendering: auto;
                        background: #000;
                    }
                </style>
            </head>
            <body>
                <img id="screen" src="/cap" alt="">

                <script>
                (function () {
                    var INTERVAL = {{intervalMs}};
                    var img = document.getElementById('screen');
                    var seq = 0;
                    var viewport = window.visualViewport;

                    function syncViewport() {
                        var width = viewport ? viewport.width : window.innerWidth;
                        var height = viewport ? viewport.height : window.innerHeight;
                        document.documentElement.style.setProperty('--vw', Math.round(width) + 'px');
                        document.documentElement.style.setProperty('--vh', Math.round(height) + 'px');
                    }

                    window.addEventListener('resize', syncViewport);
                    window.addEventListener('orientationchange', syncViewport);
                    if (viewport) {
                        viewport.addEventListener('resize', syncViewport);
                        viewport.addEventListener('scroll', syncViewport);
                    }

                    function next() {
                        var pre = new Image();
                        pre.onload = function () {
                            img.src = this.src;
                            setTimeout(next, INTERVAL);
                        };
                        pre.onerror = function () {
                            setTimeout(next, INTERVAL * 4);
                        };
                        pre.src = '/cap?s=' + (++seq);
                    }

                    syncViewport();
                    next();

                    if ('wakeLock' in navigator) {
                        var wakeLock = null;
                        function acquireWakeLock() {
                            navigator.wakeLock.request('screen').then(function (lock) { wakeLock = lock; }).catch(function () {});
                        }
                        acquireWakeLock();
                        document.addEventListener('visibilitychange', function () {
                            if (document.visibilityState === 'visible') acquireWakeLock();
                        });
                    }
                })();
                </script>
            </body>
            </html>
            """;
    }
}
