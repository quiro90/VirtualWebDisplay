using System.Net;
using System.Text.Json;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.HtmlTemplates;

public sealed class SecurityPageTemplate
{
    public string Generate(ScreenRuntimeContext runtime, HttpContext context)
    {
        var state = runtime.SecurityGate.GetClientWindowState(context);
        var title = AppText.Format("Security_Page_Title", runtime.DisplayName);
        var heading = AppText.Get("Security_Page_Heading");
        var description = AppText.Get("Security_Page_Description");
        var submitText = AppText.Get("Security_Page_Submit");
        var inputPlaceholder = AppText.Get("Security_Page_Input_Placeholder");
        var initialStatus = state.RetryAfterSeconds > 0
            ? AppText.Format("Security_Page_Wait", state.RetryAfterSeconds)
            : AppText.Format("Security_Page_Attempts", state.AttemptsRemaining);

        var submitTextJs = JsonSerializer.Serialize(submitText);
        var inputPlaceholderJs = JsonSerializer.Serialize(inputPlaceholder);

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

                    h1 {
                        margin: 0 0 10px;
                        font-size: 22px;
                    }

                    p {
                        margin: 0 0 16px;
                        line-height: 1.45;
                        color: rgba(245, 248, 255, 0.82);
                    }

                    form {
                        display: flex;
                        gap: 10px;
                    }

                    input {
                        flex: 1;
                        border: 1px solid rgba(255, 255, 255, 0.22);
                        background: rgba(0, 0, 0, 0.28);
                        color: #fff;
                        border-radius: 10px;
                        padding: 10px 12px;
                        text-transform: uppercase;
                        letter-spacing: 1px;
                        outline: none;
                    }

                    input:focus {
                        border-color: #8ec5ff;
                        box-shadow: 0 0 0 2px rgba(142, 197, 255, 0.25);
                    }

                    button {
                        border: 0;
                        border-radius: 10px;
                        padding: 10px 14px;
                        background: #2f8fef;
                        color: #fff;
                        font-weight: 600;
                        cursor: pointer;
                    }

                    button:disabled {
                        opacity: 0.65;
                        cursor: not-allowed;
                    }

                    #status {
                        margin-top: 12px;
                        min-height: 20px;
                        font-size: 13px;
                        color: #ffd08a;
                    }
                </style>
            </head>
            <body>
                <main class="wrapper">
                    <section class="card">
                        <h1>{{WebUtility.HtmlEncode(heading)}}</h1>
                        <p>{{WebUtility.HtmlEncode(description)}}</p>

                        <form id="authForm" autocomplete="off">
                            <input id="code" maxlength="6" placeholder="" required />
                            <button id="submit" type="submit">{{WebUtility.HtmlEncode(submitText)}}</button>
                        </form>
                        <div id="status">{{WebUtility.HtmlEncode(initialStatus)}}</div>
                    </section>
                </main>

                <script>
                    (function () {
                        var form = document.getElementById('authForm');
                        var code = document.getElementById('code');
                        var submit = document.getElementById('submit');
                        var status = document.getElementById('status');

                        submit.textContent = {{submitTextJs}};
                        code.setAttribute('placeholder', {{inputPlaceholderJs}});

                        form.addEventListener('submit', async function (event) {
                            event.preventDefault();
                            submit.disabled = true;

                            try {
                                var response = await fetch('/auth/login', {
                                    method: 'POST',
                                    headers: { 'Content-Type': 'application/json' },
                                    body: JSON.stringify({ code: (code.value || '').trim().toUpperCase() })
                                });

                                var payload = await response.json().catch(function () { return {}; });
                                if (response.ok) {
                                    location.reload();
                                    return;
                                }

                                status.textContent = payload.error || 'Error';
                            }
                            catch {
                                status.textContent = 'Error de conexion.';
                            }
                            finally {
                                submit.disabled = false;
                            }
                        });
                    })();
                </script>
            </body>
            </html>
            """;
    }
}
