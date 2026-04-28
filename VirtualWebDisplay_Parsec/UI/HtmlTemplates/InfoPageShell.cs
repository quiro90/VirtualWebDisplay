using System.Net;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.HtmlTemplates;

internal static class InfoPageShell
{
    public static string Wrap(string title, string bodyContent, string? pageStyles = null)
    {
        pageStyles ??= string.Empty;

        return $$"""
            <!DOCTYPE html>
            <html lang="{{AppText.HtmlLang}}">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{{WebUtility.HtmlEncode(title)}}</title>
                <style>
                    html, body {
                        margin: 0;
                        width: 100%;
                        height: 100%;
                        font-family: Segoe UI, Arial, sans-serif;
                        background: radial-gradient(circle at top, #1a1f2a 0%, #0c1018 60%, #06090f 100%);
                        color: #f5f8ff;
                    }

                    .wrapper {
                        min-height: 100%;
                        display: grid;
                        place-items: center;
                        padding: 20px;
                    }

                    .card {
                        width: min(420px, 92vw);
                        background: rgba(8, 12, 18, 0.85);
                        border: 1px solid rgba(255, 255, 255, 0.08);
                        border-radius: 14px;
                        padding: 24px;
                        box-shadow: 0 20px 45px rgba(0, 0, 0, 0.45);
                    }

                    {{pageStyles}}
                </style>
            </head>
            <body>
                {{bodyContent}}
            </body>
            </html>
            """;
    }
}