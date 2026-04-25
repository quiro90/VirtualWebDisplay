using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

// ── Configuration ────────────────────────────────────────────────────────────

using var singleInstance = SingleInstanceManager.CreateForCurrentExecutable();
if (!singleInstance.EnsureSingleInstance(TimeSpan.FromSeconds(10)))
{
    MessageBox.Show(
        "No se pudo cerrar la instancia anterior de VirtualWebDisplay para relanzarla.",
        "VirtualWebDisplay — Relanzamiento fallido",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

var builder = WebApplication.CreateBuilder(args);
var settingsStore = new VirtualScreenSettingsStore();
var config = settingsStore.Load();

VirtualDisplayProfiles.EnsureValidSelection(config);
TransmissionModeOptions.EnsureValidSelection(config);

using var tray = new VirtualDisplayTrayController(config, settingsStore);

// ── Detect local IP ───────────────────────────────────────────────────────────

static string DetectLocalIp() =>
    NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up
                 && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(a => a.Address.ToString())
        .FirstOrDefault() ?? "127.0.0.1";

static string BuildAccessUrl(string host, int port) =>
    port == 80 ? $"http://{host}/" : $"http://{host}:{port}/";

var localIp = DetectLocalIp();
var hostName = Dns.GetHostName();

static void ShowInstallDialog(string title, string message, string installUrl)
{
    using var done = new ManualResetEventSlim(false);
    Exception? error = null;

    var sta = new Thread(() =>
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = true,
                ClientSize = new Size(620, 230),
            };

            var messageLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 18,
                Width = 580,
                Height = 110,
                Text = message,
            };

            var urlLabel = new Label
            {
                AutoSize = true,
                Left = 20,
                Top = 132,
                Text = "Instalador oficial:",
            };

            var urlBox = new TextBox
            {
                Left = 20,
                Top = 154,
                Width = 460,
                ReadOnly = true,
                Text = installUrl,
            };

            var openButton = new Button
            {
                Left = 490,
                Top = 152,
                Width = 110,
                Height = 28,
                Text = "Abrir descarga",
            };

            var copyButton = new Button
            {
                Left = 374,
                Top = 192,
                Width = 110,
                Height = 28,
                Text = "Copiar URL",
            };

            var okButton = new Button
            {
                Left = 490,
                Top = 192,
                Width = 110,
                Height = 28,
                Text = "Cerrar",
                DialogResult = DialogResult.OK,
            };

            openButton.Click += (_, _) =>
            {
                Process.Start(new ProcessStartInfo(installUrl) { UseShellExecute = true });
            };

            copyButton.Click += (_, _) =>
            {
                Clipboard.SetText(installUrl);
                urlBox.Focus();
                urlBox.SelectAll();
                copyButton.Text = "Copiada";
            };

            form.Controls.AddRange([messageLabel, urlLabel, urlBox, openButton, copyButton, okButton]);
            form.AcceptButton = okButton;
            form.CancelButton = okButton;

            form.Shown += (_, _) =>
            {
                urlBox.Focus();
                urlBox.SelectAll();
            };

            form.ShowDialog();
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            done.Set();
        }
    });

    sta.SetApartmentState(ApartmentState.STA);
    sta.IsBackground = true;
    sta.Start();
    done.Wait();

    if (error is not null)
    {
        MessageBox.Show(
            message + $"\n\nInstalador oficial: {installUrl}",
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}

// ── Virtual display (parsec-vdd required) ─────────────────────────────────────

var (driverReady, driverStatus) = VirtualDisplayManager.VerifyDriverAvailability();
if (!driverReady)
{
    ShowInstallDialog(
        "VirtualWebDisplay — Falta Parsec VDD",
        driverStatus + "\n\nEsta versión requiere Parsec VDD para crear y capturar el monitor virtual.",
        VirtualDisplayManager.InstallUrl);
    return;
}

if (!tray.ShowStartupConfiguration())
    return;

var hostUrl = BuildAccessUrl(hostName, config.Port);
var ipUrl = BuildAccessUrl(localIp, config.Port);

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<CaptureService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CaptureService>());
builder.Services.AddSingleton<WebRtcStreamService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<WebRtcStreamService>());

builder.WebHost.UseUrls($"http://0.0.0.0:{config.Port}");

var app = builder.Build();
var capture = app.Services.GetRequiredService<CaptureService>();
var webRtc = app.Services.GetRequiredService<WebRtcStreamService>();

singleInstance.StartShutdownListener(() => app.Lifetime.StopApplication());

using var vdd = new VirtualDisplayManager();
var (ok, vddStatus) = vdd.TryCreate(config);

if (!ok)
{
    ShowInstallDialog(
        "VirtualWebDisplay — Falta Parsec VDD",
        vddStatus + "\n\nEsta versión requiere Parsec VDD para crear y capturar el monitor virtual.",
        VirtualDisplayManager.InstallUrl);
    return;
}

if (vdd.WindowsMonitorIndex is int virtualMonitorIndex)
    config.MonitorIndex = virtualMonitorIndex;
else if (config.MonitorIndex < 0)
{
    MessageBox.Show(
        vddStatus + "\n\nNo se pudo identificar el monitor virtual de Windows para capturarlo automáticamente.",
        "VirtualWebDisplay — Monitor virtual no detectado",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

app.Lifetime.ApplicationStopping.Register(vdd.Dispose);
tray.ConfigureRuntimeActions(
    updatedConfig => vdd.TryReconfigure(updatedConfig),
    () => app.Lifetime.StopApplication(),
    hostUrl,
    string.Equals(hostUrl, ipUrl, StringComparison.OrdinalIgnoreCase) ? null : ipUrl);

// ── Build monitor summary ─────────────────────────────────────────────────────

static string MonitorSummary()
{
    var screens = Screen.AllScreens;
    var lines = screens.Select((s, i) =>
        $"[{i}] {s.Bounds.Width}×{s.Bounds.Height}{(s.Primary ? " (primario)" : "")}");
    return string.Join("  |  ", lines);
}

var monitorInfo = MonitorSummary();

static string BrowserImageFit(string? fit) =>
    fit?.Trim().ToLowerInvariant() switch
    {
        "contain" => "contain",
        "fill" => "fill",
        _ => "cover",
    };

static string BuildWebImagePage(string browserImageFit, int intervalMs) => $$"""
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0,
              maximum-scale=1.0, minimum-scale=1.0, user-scalable=no">
        <title>VirtualWebDisplay</title>
        <style>
            *, *::before, *::after { margin: 0; padding: 0; box-sizing: border-box; }

            :root {
                --vw: 100vw;
                --vh: 85vh;
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
        })();
        </script>
    </body>
    </html>
    """;

static string BuildRtcPage(string browserImageFit) => $$"""
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0,
              maximum-scale=1.0, minimum-scale=1.0, user-scalable=no">
        <title>VirtualWebDisplay</title>
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
                object-fit: {{browserImageFit}};
                object-position: center center;
                display: block;
                image-rendering: auto;
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
        <img id="screen" alt="">
        <div id="mode">WebRTC</div>
        <div id="status">Conectando…</div>

        <script>
        (function () {
            var img = document.getElementById('screen');
            var status = document.getElementById('status');
            var currentUrl = null;
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
                frameInfo = meta;
                frameBuffers = [];
                receivedBytes = 0;
            }

            function applyFrame(bytes) {
                var blob = new Blob([bytes], { type: 'image/jpeg' });
                var nextUrl = URL.createObjectURL(blob);
                img.onload = function () {
                    if (currentUrl)
                        URL.revokeObjectURL(currentUrl);
                    currentUrl = nextUrl;
                };
                img.src = nextUrl;
            }

            async function connect() {
                setStatus('Negociando WebRTC…');

                var pc = new RTCPeerConnection({ iceServers: [] });
                var channel = pc.createDataChannel('frames', { ordered: true });
                channel.binaryType = 'arraybuffer';

                channel.onopen = function () {
                    setStatus('WebRTC conectado');
                };

                channel.onclose = function () {
                    setStatus('WebRTC desconectado. Reintentando…');
                    window.setTimeout(connect, 1500);
                };

                channel.onerror = function () {
                    setStatus('Error WebRTC. Reintentando…');
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

                    var chunk = new Uint8Array(event.data);
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
                        setStatus('WebRTC desconectado. Reintentando…');
                };

                var offer = await pc.createOffer();
                await pc.setLocalDescription(offer);
                await waitForIceGatheringComplete(pc);

                var response = await fetch('/webrtc/offer', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ sdp: pc.localDescription.sdp, type: pc.localDescription.type })
                });

                if (!response.ok)
                    throw new Error('No se pudo negociar la sesión WebRTC.');

                var answer = await response.json();
                await pc.setRemoteDescription(answer);
            }

            connect().catch(function () {
                setStatus('No se pudo iniciar WebRTC. Reintentando…');
                window.setTimeout(connect, 2000);
            });
        })();
        </script>
    </body>
    </html>
    """;

// Warn if MonitorIndex points to a monitor that doesn't exist
if (config.MonitorIndex >= 0 && config.MonitorIndex >= Screen.AllScreens.Length)
{
    MessageBox.Show(
        $"MonitorIndex = {config.MonitorIndex} pero solo hay {Screen.AllScreens.Length} monitor(es).\n\n" +
        $"Monitores disponibles:\n{monitorInfo.Replace("  |  ", "\n")}\n\n" +
        "Corregí MonitorIndex en appsettings.json.",
        "VirtualWebDisplay — Monitor no encontrado",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

// ── Endpoints ────────────────────────────────────────────────────────────────

// Kindle/e-ink page for WebImage and continuous stream page for tablets.
app.MapGet("/", () =>
{
    var browserImageFit = BrowserImageFit(config.BrowserImageFit);
    return Results.Content(
        TransmissionModeOptions.IsWebImage(config.TransmissionMethod)
            ? BuildWebImagePage(browserImageFit, Math.Max(10, (int)Math.Round(config.CaptureIntervalSeconds * 1000)))
            : BuildRtcPage(browserImageFit),
        "text/html");
});


// Latest JPEG frame
app.MapGet("/cap", (HttpContext ctx) =>
{
    var frame = capture.GetCurrentFrame();
    if (frame.Length == 0)
        return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable);

    ctx.Response.Headers.CacheControl = "no-store, no-cache";
    return Results.Bytes(frame, "image/jpeg");
});

app.MapPost("/webrtc/offer", async (WebRtcSessionOffer offer, CancellationToken cancellationToken) =>
{
    if (!TransmissionModeOptions.IsRtc(config.TransmissionMethod))
        return Results.BadRequest(new { error = "WebRTC no está habilitado para el método de retransmisión actual." });

    if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
        return Results.BadRequest(new { error = "Oferta WebRTC inválida." });

    var answer = await webRtc.CreateAnswerAsync(offer, cancellationToken);
    return Results.Json(answer);
});

app.MapGet("/mjpeg", async (HttpContext ctx) =>
{
    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
    ctx.Response.Headers.CacheControl = "no-store, no-cache";
    ctx.Response.Headers.Pragma = "no-cache";
    ctx.Response.Headers.Connection = "keep-alive";
    ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";

    byte[]? lastFrame = null;
    var token = ctx.RequestAborted;

    while (!token.IsCancellationRequested)
    {
        var frame = capture.GetCurrentFrame();
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

// Active configuration (useful for debugging from Kindle browser: /config)
app.MapGet("/config", () => Results.Json(config));

// ── Run ──────────────────────────────────────────────────────────────────────

Console.WriteLine("┌─────────────────────────────────────────┐");
Console.WriteLine($"│  📺  VirtualWebDisplay                   │");
Console.WriteLine($"│  Método: {TransmissionModeOptions.GetDisplayName(config.TransmissionMethod),-31}│");
Console.WriteLine($"│  Host: {hostUrl,-33}│");
Console.WriteLine($"│  IP:   {ipUrl,-33}│");
Console.WriteLine("└─────────────────────────────────────────┘");

tray.UpdateStatus($"Transmitiendo en {hostUrl}");

app.Run();
