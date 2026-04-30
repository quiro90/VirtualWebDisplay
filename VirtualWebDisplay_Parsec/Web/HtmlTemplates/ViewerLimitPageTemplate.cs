using System.Net;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.HtmlTemplates;

public sealed class ViewerLimitPageTemplate
{
    public string Generate(ScreenRuntimeContext runtime)
    {
        var title = AppText.Format("Security_Page_Title", runtime.DisplayName);
        var message = AppText.Get("Program_ViewerLimit_Full_Message");

        var pageStyles = """
            .card {
                text-align: center;
            }

            h1 {
                margin: 0;
                font-size: 20px;
            }
            """;

        var bodyContent = $$"""
            <main class="wrapper">
                <section class="card">
                    <h1>&#128683; {{WebUtility.HtmlEncode(message)}}</h1>
                </section>
            </main>
            """;

        return InfoPageShell.Wrap(title, bodyContent, pageStyles);
    }
}
