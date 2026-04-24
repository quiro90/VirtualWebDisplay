using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

// ── Configuration ────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration
    .GetSection("VirtualScreen")
    .Get<VirtualScreenConfig>() ?? new VirtualScreenConfig();

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<CaptureService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CaptureService>());

builder.WebHost.UseUrls($"http://0.0.0.0:{config.Port}");

var app = builder.Build();
var capture = app.Services.GetRequiredService<CaptureService>();

// ── Detect local IP ───────────────────────────────────────────────────────────

static string DetectLocalIp() =>
    NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up
                 && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(a => a.Address.ToString())
        .FirstOrDefault() ?? "127.0.0.1";

var localIp   = DetectLocalIp();
var kindleUrl = $"http://{localIp}:{config.Port}";

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

using var vdd = new VirtualDisplayManager();
var (ok, vddStatus) = vdd.TryCreate(config);

if (!ok)
{
    ShowInstallDialog(
        "Kindle Virtual Screen — Falta Parsec VDD",
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
        "Kindle Virtual Screen — Monitor virtual no detectado",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

app.Lifetime.ApplicationStopping.Register(vdd.Dispose);

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

var browserImageFit = BrowserImageFit(config.BrowserImageFit);

// Warn if MonitorIndex points to a monitor that doesn't exist
if (config.MonitorIndex >= 0 && config.MonitorIndex >= Screen.AllScreens.Length)
{
    MessageBox.Show(
        $"MonitorIndex = {config.MonitorIndex} pero solo hay {Screen.AllScreens.Length} monitor(es).\n\n" +
        $"Monitores disponibles:\n{monitorInfo.Replace("  |  ", "\n")}\n\n" +
        "Corregí MonitorIndex en appsettings.json.",
        "Kindle Virtual Screen — Monitor no encontrado",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning);
    return;
}

// ── Endpoints ────────────────────────────────────────────────────────────────

// Kindle Paperwhite 12 optimised page.
// • viewport=device-width + user-scalable=no → sin zoom, sin scroll horizontal
// • imagen ajustada al viewport visible real del navegador Kindle
// • HTML mínimo: solo la imagen del stream
// • Fullscreen API no está soportada en Silk/Kindle → no se intenta
app.MapGet("/", () => Results.Content($$"""
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0,
              maximum-scale=1.0, minimum-scale=1.0, user-scalable=no">
        <title>Kindle Mirror</title>
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
                /* Evita el tap-highlight azul en Silk */
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
            var INTERVAL = {{(int)(config.CaptureIntervalSeconds * 1000)}};
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

            // Primer frame inmediato, luego bucle
            syncViewport();
            next();
        })();
        </script>
    </body>
    </html>
    """, "text/html"));


// Latest JPEG frame
app.MapGet("/cap", (HttpContext ctx) =>
{
    var frame = capture.GetCurrentFrame();
    if (frame.Length == 0)
        return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable);

    ctx.Response.Headers.CacheControl = "no-store, no-cache";
    return Results.Bytes(frame, "image/jpeg");
});

// Active configuration (useful for debugging from Kindle browser: /config)
app.MapGet("/config", () => Results.Json(config));

// ── Run ──────────────────────────────────────────────────────────────────────

Console.WriteLine("┌─────────────────────────────────────────┐");
Console.WriteLine($"│  📺  Kindle Virtual Screen               │");
Console.WriteLine($"│  Abrí en tu Kindle:                     │");
Console.WriteLine($"│  ➜  {kindleUrl,-36}│");
Console.WriteLine("└─────────────────────────────────────────┘");

app.Run();
