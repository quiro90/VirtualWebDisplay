using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

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
var settings = settingsStore.Load();
settings.EnsureValid();

using var tray = new VirtualDisplayTrayController(settings, settingsStore);

var localIp = NetworkAddressHelper.DetectLocalIp();
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

            openButton.Click += (_, _) => Process.Start(new ProcessStartInfo(installUrl) { UseShellExecute = true });
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

static string BrowserImageFit(string? fit) =>
    fit?.Trim().ToLowerInvariant() switch
    {
        "contain" => "contain",
        "fill" => "fill",
        _ => "cover",
    };

static string BuildWebImagePage(string title, string browserImageFit, int intervalMs) => $$"""
    <!DOCTYPE html>
    <html lang="es">
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
        })();
        </script>
    </body>
    </html>
    """;

static string BuildRtcPage(string title, string browserImageFit) => $$"""
    <!DOCTYPE html>
    <html lang="es">
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
                // ordered: false + maxRetransmits: 0 → sin retransmisión ni head-of-line blocking.
                // Un frame perdido o incompleto se descarta; el siguiente llega sin delay acumulado.
                var channel = pc.createDataChannel('frames', { ordered: false, maxRetransmits: 0 });
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

                    var data = new Uint8Array(event.data);
                    if (data.length < 4)
                        return;

                    // First 4 bytes: little-endian uint32 frameId.
                    // Discard chunks that belong to a superseded frame.
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


static async Task DisposeRuntimesAsync(IEnumerable<ScreenRuntimeContext> runtimes)
{
    foreach (var runtime in runtimes.Reverse())
        await runtime.DisposeAsync();
}

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

settings.EnsureValid();

var runtimes = new List<ScreenRuntimeContext>
{
    new("screen1", "Pantalla 1", settings.Screen1, hostName, localIp),
};

if (settings.Screen2.Enabled)
    runtimes.Add(new ScreenRuntimeContext("screen2", "Pantalla 2", settings.Screen2, hostName, localIp));

builder.WebHost.UseUrls(runtimes.Select(runtime => $"http://0.0.0.0:{runtime.Config.Port}").ToArray());

var app = builder.Build();
singleInstance.StartShutdownListener(() => app.Lifetime.StopApplication());

try
{
    foreach (var runtime in runtimes)
    {
        var (ok, vddStatus) = runtime.DisplayManager.TryCreate(runtime.Config);
        if (!ok)
        {
            ShowInstallDialog(
                $"VirtualWebDisplay — Error en {runtime.DisplayName}",
                vddStatus + "\n\nEsta versión requiere Parsec VDD para crear y capturar el monitor virtual.",
                VirtualDisplayManager.InstallUrl);
            return;
        }

        if (runtime.DisplayManager.WindowsMonitorIndex is int virtualMonitorIndex)
            runtime.Config.MonitorIndex = virtualMonitorIndex;
        else if (runtime.Config.MonitorIndex < 0)
        {
            MessageBox.Show(
                vddStatus + $"\n\nNo se pudo identificar el monitor virtual de Windows para {runtime.DisplayName}.",
                "VirtualWebDisplay — Monitor virtual no detectado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        await runtime.StartAsync(CancellationToken.None);
    }

    tray.ConfigureRuntimeActions(
        () => app.Lifetime.StopApplication(),
        runtimes);

    ScreenRuntimeContext ResolveRuntime(HttpContext context) =>
        runtimes.FirstOrDefault(runtime => runtime.Config.Port == context.Connection.LocalPort) ?? runtimes[0];

    app.MapGet("/", (HttpContext ctx) =>
    {
        var runtime = ResolveRuntime(ctx);
        var browserImageFit = BrowserImageFit(runtime.Config.BrowserImageFit);
        return Results.Content(
            TransmissionModeOptions.IsWebImage(runtime.Config.TransmissionMethod)
                ? BuildWebImagePage(runtime.DisplayName, browserImageFit, Math.Max(10, (int)Math.Round(runtime.Config.CaptureIntervalSeconds * 1000)))
                : BuildRtcPage(runtime.DisplayName, browserImageFit),
            "text/html");
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
            return Results.BadRequest(new { error = "WebRTC no está habilitado para el método de retransmisión actual." });

        if (!string.Equals(offer.Type, "offer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(offer.Sdp))
            return Results.BadRequest(new { error = "Oferta WebRTC inválida." });

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

    Console.WriteLine("┌─────────────────────────────────────────┐");
    Console.WriteLine("│  📺  VirtualWebDisplay                   │");
    foreach (var runtime in runtimes)
    {
        Console.WriteLine($"│  {runtime.DisplayName,-10}: {runtime.HostUrl,-20}│");
        if (!string.Equals(runtime.HostUrl, runtime.IpUrl, StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"│  {"IP",-10}: {runtime.IpUrl,-20}│");
    }
    Console.WriteLine("└─────────────────────────────────────────┘");

    app.Run();
}
finally
{
    await DisposeRuntimesAsync(runtimes);
}
