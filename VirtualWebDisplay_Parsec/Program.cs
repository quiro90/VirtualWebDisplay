using System.Diagnostics;
using System.Drawing;
using System.Net;
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

    app.MapGet("/", (HttpContext ctx) =>
    {
        var runtime = ResolveRuntime(ctx);
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
        var frame = runtime.CaptureService.GetCurrentFrame();
        if (frame.Length == 0)
            return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable);

        ctx.Response.Headers.CacheControl = "no-store, no-cache";
        return Results.Bytes(frame, "image/jpeg");
    });

    app.MapPost("/webrtc/offer", async (HttpContext ctx, WebRtcSessionOffer offer, CancellationToken cancellationToken) =>
    {
        var runtime = ResolveRuntime(ctx);
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
