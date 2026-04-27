using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;
using VirtualWebDisplay.UI.TrayIcon;
using VirtualWebDisplay.UI.Forms;
using VirtualWebDisplay.UI.HtmlTemplates;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Parsec;
using VirtualWebDisplay.Streaming;
using VirtualWebDisplay.Streaming.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

var settingsStore = new VirtualScreenSettingsStore();
var settings = settingsStore.Load();
settings.EnsureValid();
AppText.ApplyCulture(settings.UiLanguage);

using var singleInstance = SingleInstanceManager.CreateForCurrentExecutable();
if (!singleInstance.EnsureSingleInstance(TimeSpan.FromSeconds(10)))
{
    MessageBox.Show(
        AppText.Get("Program_SingleInstanceCloseFailed_Message"),
        AppText.Get("Program_SingleInstanceCloseFailed_Title"),
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

var builder = WebApplication.CreateBuilder(args);

var localIp = NetworkAddressHelper.DetectLocalIp();
var hostName = Dns.GetHostName();

using var tray = new VirtualDisplayTrayController(settings, settingsStore, localIp);

// Instanciar los templates HTML
var webImageTemplate = new WebImagePageTemplate();
var rtcTemplate = new RtcPageTemplate();

static string BrowserImageFit(string? fit) =>
    fit?.Trim().ToLowerInvariant() switch
    {
        "contain" => "contain",
        "fill" => "fill",
        _ => "cover",
    };

static string SecurityCookieName(ScreenRuntimeContext runtime) => $"vwd_auth_{runtime.Id}";

static string BuildSecurityPageHtml(ScreenRuntimeContext runtime, HttpContext context)
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

static async Task DisposeRuntimesAsync(IEnumerable<ScreenRuntimeContext> runtimes)
{
    foreach (var runtime in runtimes.Reverse())
        await runtime.DisposeAsync();
}

static async Task WaitForVirtualDisplaysRemovalAsync(IReadOnlyCollection<string> deviceNames, TimeSpan timeout)
{
    if (deviceNames.Count == 0)
        return;

    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        var remaining = Screen.AllScreens
            .Select(screen => screen.DeviceName)
            .Where(name => deviceNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (remaining.Length == 0)
            return;

        await Task.Delay(120);
    }
}
var autoStart = args.Contains("--autostart", StringComparer.OrdinalIgnoreCase);

if (!autoStart && !tray.ShowStartupConfiguration())
    return;

settings.EnsureValid();
AppText.ApplyCulture(settings.UiLanguage);

// Crear runtimes solo para las pantallas habilitadas.
// Cada pantalla usa su puerto configurado individualmente (no se calculan puertos dinámicamente).
var runtimes = new List<ScreenRuntimeContext>
{
    new("screen1", AppText.Get("Runtime_Screen1"), settings.Screen1, hostName, localIp),
};

// Solo agregar Screen2 si está explícitamente habilitada en la configuración.
if (settings.Screen2.Enabled)
    runtimes.Add(new ScreenRuntimeContext("screen2", AppText.Get("Runtime_Screen2"), settings.Screen2, hostName, localIp));

// Solo verificar VDD si al menos una pantalla necesita monitor virtual (no está en modo duplicado).
if (runtimes.Any(r => !VirtualDisplayPlacementOptions.IsDuplicate(r.Config.VirtualDisplayPlacement)))
{
    var (driverReady, driverStatus) = VirtualDisplayManager.VerifyDriverAvailability();
    if (!driverReady)
    {
        InstallDialog.Show(
            AppText.Get("Program_DriverMissing_Title"),
            driverStatus + "\n\n" + AppText.Get("Program_DriverMissing_MessageSuffix"),
            VirtualDisplayManager.InstallUrl);
        return;
    }
}

var certStoreDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    VirtualScreenSettingsStore.DirectoryName);

var (tlsCert, tlsCertDerBytes) = LocalCertificateProvider.GetOrCreate(certStoreDir, localIp, hostName);

// Configurar Kestrel para escuchar solo en los puertos de las pantallas habilitadas.
// Cada pantalla usa 2 puertos consecutivos: Port (HTTP) y Port+1 (HTTPS).
builder.WebHost.ConfigureKestrel(kestrel =>
{
    foreach (var runtime in runtimes)
    {
        // HTTP: puerto configurado para esta pantalla.
        kestrel.ListenAnyIP(runtime.Config.Port);
        // HTTPS: puerto configurado + 1 para esta pantalla.
        kestrel.ListenAnyIP(runtime.Config.Port + 1, listenOptions =>
            listenOptions.UseHttps(tlsCert));
    }
});

var app = builder.Build();
singleInstance.StartShutdownListener(() => app.Lifetime.StopApplication());
var restartRequested = false;

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Deteniendo VirtualWebDisplay...");
});

try
{
    foreach (var runtime in runtimes)
    {
        if (VirtualDisplayPlacementOptions.IsDuplicate(runtime.Config.VirtualDisplayPlacement))
        {
            // Modo duplicado: capturar el monitor principal sin crear un monitor virtual.
            var primaryIndex = Array.FindIndex(Screen.AllScreens, s => s.Primary);
            runtime.Config.MonitorIndex = primaryIndex >= 0 ? primaryIndex : 0;
            await runtime.StartAsync(CancellationToken.None);
            continue;
        }

        var (ok, vddStatus) = runtime.DisplayManager.TryCreate(runtime.Config);
        if (!ok)
        {
            await DisposeRuntimesAsync(runtimes);
            InstallDialog.Show(
                AppText.Format("Program_DisplayError_Title", runtime.DisplayName),
                vddStatus + "\n\n" + AppText.Get("Program_DriverMissing_MessageSuffix"),
                VirtualDisplayManager.InstallUrl);
            return;
        }

        if (runtime.DisplayManager.WindowsMonitorIndex is int virtualMonitorIndex)
            runtime.Config.MonitorIndex = virtualMonitorIndex;
        else if (runtime.Config.MonitorIndex < 0)
        {
            await DisposeRuntimesAsync(runtimes);
            MessageBox.Show(
                vddStatus + "\n\n" + AppText.Format("Program_MonitorNotDetected_Message", runtime.DisplayName),
                AppText.Get("Program_MonitorNotDetected_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        await runtime.StartAsync(CancellationToken.None);
    }

    tray.ConfigureRuntimeActions(
        () => app.Lifetime.StopApplication(),
        () =>
        {
            restartRequested = true;
            app.Lifetime.StopApplication();
        },
        runtimes);

    ScreenRuntimeContext ResolveRuntime(HttpContext context) =>
        runtimes.FirstOrDefault(runtime => runtime.Config.Port == context.Connection.LocalPort) ?? runtimes[0];

    bool IsAuthorized(HttpContext context, ScreenRuntimeContext runtime) =>
        !runtime.SecurityGate.Enabled || runtime.SecurityGate.IsAuthorized(context, SecurityCookieName(runtime));

    IResult UnauthorizedResult(ScreenRuntimeContext runtime)
    {
        if (runtime.SecurityGate.Enabled)
            return Results.Json(
                new { error = AppText.Get("Program_Security_MissingCode_Error") },
                statusCode: StatusCodes.Status401Unauthorized);

        return Results.Unauthorized();
    }

    app.MapPost("/auth/login", (HttpContext ctx, SecurityLoginRequest request) =>
    {
        var runtime = ResolveRuntime(ctx);
        if (!runtime.SecurityGate.Enabled)
            return Results.Ok(new { authorized = true });

        var result = runtime.SecurityGate.TryAuthorize(ctx, SecurityCookieName(runtime), request.Code);
        if (result.Authorized)
            return Results.Ok(new { authorized = true });

        if (result.TooManyAttempts)
        {
            return Results.Json(
                new
                {
                    error = AppText.Format("Program_Security_TooManyAttempts_Error", result.RetryAfterSeconds),
                    retryAfterSeconds = result.RetryAfterSeconds,
                    attemptsRemaining = 0,
                },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        return Results.Json(
            new
            {
                error = AppText.Get("Program_Security_InvalidCode_Error"),
                attemptsRemaining = result.AttemptsRemaining,
            },
            statusCode: StatusCodes.Status401Unauthorized);
    });

    app.MapGet("/", (HttpContext ctx) =>
    {
        var runtime = ResolveRuntime(ctx);
        if (!IsAuthorized(ctx, runtime))
            return Results.Content(BuildSecurityPageHtml(runtime, ctx), "text/html");

        var browserImageFit = BrowserImageFit(runtime.Config.BrowserImageFit);

        string html;
        if (TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod))
        {
            var parameters = new Dictionary<string, object>
            {
                ["title"] = runtime.DisplayName,
                ["browserImageFit"] = browserImageFit,
                ["intervalMs"] = Math.Max(3, (int)Math.Round(runtime.Config.CaptureIntervalSeconds * 1000))
            };
            html = webImageTemplate.Generate(parameters);
        }
        else
        {
            var parameters = new Dictionary<string, object>
            {
                ["title"] = runtime.DisplayName,
                ["browserImageFit"] = browserImageFit
            };
            html = rtcTemplate.Generate(parameters);
        }

        return Results.Content(html, "text/html");
    });

    app.MapGet("/cap", (HttpContext ctx) =>
    {
        var runtime = ResolveRuntime(ctx);
        if (!IsAuthorized(ctx, runtime))
            return UnauthorizedResult(runtime);

        var frame = runtime.CaptureService.GetCurrentFrame();
        if (frame.Length == 0)
            return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable);

        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        return Results.Bytes(frame, "image/jpeg");
    });

    app.MapPost("/webrtc/offer", async (HttpContext ctx, WebRtcSessionOffer offer, CancellationToken cancellationToken) =>
    {
        var runtime = ResolveRuntime(ctx);
        if (!IsAuthorized(ctx, runtime))
            return UnauthorizedResult(runtime);

        if (!TransmissionModeOptions.IsRtc(runtime.Config.TransmissionMethod))
            return Results.BadRequest(new { error = AppText.Get("Program_WebRtcDisabled_Error") });

        if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
            return Results.BadRequest(new { error = AppText.Get("Program_WebRtcInvalidOffer_Error") });

        var answer = await runtime.WebRtcStreamService.CreateAnswerAsync(offer, cancellationToken);
        return Results.Json(answer);
    });

    app.MapGet("/mjpeg", async (HttpContext ctx) =>
    {
        var runtime = ResolveRuntime(ctx);
        if (!IsAuthorized(ctx, runtime))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = AppText.Get("Program_Security_MissingCode_Error") });
            return;
        }

        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        ctx.Response.Headers.Pragma = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";
        ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";

        byte[]? lastFrame = null;
        var token = ctx.RequestAborted;

        while (!token.IsCancellationRequested)
        {
            var frame = runtime.CaptureService.GetCurrentFrame();
            if (frame.Length == 0 || ReferenceEquals(frame, lastFrame))
            {
                await Task.Delay(10, token);
                continue;
            }

            lastFrame = frame;
            await ctx.Response.WriteAsync("--frame\r\n", token);
            await ctx.Response.WriteAsync("Content-Type: image/jpeg\r\n", token);
            await ctx.Response.WriteAsync($"Content-Length: {frame.Length}\r\n\r\n", token);
            await ctx.Response.Body.WriteAsync(frame, token);
            await ctx.Response.WriteAsync("\r\n", token);
            await ctx.Response.Body.FlushAsync(token);
        }
    });

    app.MapGet("/cert", () =>
    {
        return Results.Bytes(
            tlsCertDerBytes,
            "application/x-x509-ca-cert",
            LocalCertificateProvider.CrtDownloadFileName);
    });

    app.MapGet("/config", (HttpContext ctx) =>
    {
        var runtime = ResolveRuntime(ctx);
        if (!IsAuthorized(ctx, runtime))
            return UnauthorizedResult(runtime);

        return Results.Json(new
        {
            runtime.DisplayName,
            runtime.Config,
            runtime.HostUrl,
            runtime.IpUrl,
        });
    });

    Console.WriteLine("┌──────────────────────────────────────────────────────┐");
    Console.WriteLine("│  📺  VirtualWebDisplay                                │");
    foreach (var runtime in runtimes)
    {
        var httpsHostUrl = runtime.HostUrl.Replace($":{runtime.Config.Port}", $":{runtime.Config.Port + 1}").Replace("http://", "https://");
        var httpsIpUrl   = runtime.IpUrl.Replace($":{runtime.Config.Port}", $":{runtime.Config.Port + 1}").Replace("http://", "https://");
        Console.WriteLine($"│  {runtime.DisplayName,-10}  HTTP : {runtime.HostUrl,-28}│");
        Console.WriteLine($"│  {string.Empty,-10}  HTTPS: {httpsHostUrl,-28}│");
        if (!string.Equals(runtime.HostUrl, runtime.IpUrl, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"│  {"IP",-10}  HTTP : {runtime.IpUrl,-28}│");
            Console.WriteLine($"│  {string.Empty,-10}  HTTPS: {httpsIpUrl,-28}│");
        }
        if (runtime.SecurityGate.Enabled)
            Console.WriteLine($"│  {"Clave",-10}       {runtime.SecurityGate.AccessCode,-28}│");
        var certUrl = $"{runtime.IpUrl}/cert";
        Console.WriteLine($"│  {"Cert",-10}       {certUrl,-28}│");
    }
    Console.WriteLine("│                                                      │");
    Console.WriteLine("│  ⚠  Para HTTPS sin warning en Safari/iOS:            │");
    Console.WriteLine("│     1. Abre /cert desde Safari en el dispositivo      │");
    Console.WriteLine("│     2. Instala el perfil (Ajustes → Perfil descargado)│");
    Console.WriteLine("│     3. Ajustes → General → Acerca → Conf. de cert.   │");
    Console.WriteLine("└──────────────────────────────────────────────────────┘");

    await app.RunAsync();
}
finally
{
    Console.WriteLine("Limpiando recursos...");
    var createdVirtualDeviceNames = runtimes
        .Select(runtime => runtime.DisplayManager.WindowsDeviceName)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    await DisposeRuntimesAsync(runtimes);

    if (restartRequested)
    {
        await WaitForVirtualDisplaysRemovalAsync(createdVirtualDeviceNames, TimeSpan.FromSeconds(6));
        await Task.Delay(200);
    }

    Console.WriteLine("Recursos liberados.");
}

// Liberar el mutex explícitamente antes de Exit para que la nueva instancia
// (en caso de reinicio) pueda adquirirlo sin AbandonedMutexException.
singleInstance.Dispose();

if (restartRequested)
{
    var processPath = Environment.ProcessPath
        ?? Process.GetCurrentProcess().MainModule?.FileName;
    if (processPath is not null)
        Process.Start(new ProcessStartInfo(processPath, "--autostart") { UseShellExecute = true });
}

// Forzar la terminación del proceso una vez que todo el cleanup completó.
// SIPSorcery y Kestrel pueden dejar threads internos que impiden la salida natural.
Environment.Exit(0);

public sealed record SecurityLoginRequest(string? Code);
