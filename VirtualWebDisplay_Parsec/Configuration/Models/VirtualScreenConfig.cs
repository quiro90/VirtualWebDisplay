using System.Text.Json.Serialization;

namespace VirtualWebDisplay.Configuration.Models;

public sealed class VirtualScreenConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>Resolución efectiva en runtime. No se persiste: la gestiona VirtualDisplayResolutionStore.</summary>
    [JsonIgnore]
    public int Width { get; set; } = 1080;

    /// <summary>Resolución efectiva en runtime. No se persiste: la gestiona VirtualDisplayResolutionStore.</summary>
    [JsonIgnore]
    public int Height { get; set; } = 1920;

    /// <summary>Perfil de resolución de la UI. No se persiste: la resolución activa viene de VirtualDisplayResolutionStore.</summary>
    [JsonIgnore]
    public string Profile { get; set; } = string.Empty;

    /// <summary>Orientación calculada en runtime. No se persiste.</summary>
    [JsonIgnore]
    public bool Landscape { get; set; }

    /// <summary>Ancho personalizado de la UI. No se persiste.</summary>
    [JsonIgnore]
    public int CustomWidth { get; set; } = 1080;

    /// <summary>Alto personalizado de la UI. No se persiste.</summary>
    [JsonIgnore]
    public int CustomHeight { get; set; } = 1920;

    /// <summary>
    /// Retransmission mode exposed by the local web server.
    /// WebImage = polling JPEG image.
    /// Rtc = continuous live stream page, better suited for tablets.
    /// </summary>
    public string TransmissionMethod { get; set; } = TransmissionModeOptions.Rtc;

    /// <summary>Capture interval in seconds. E.g. 0.008 = 8ms (~125 FPS)</summary>
    public double CaptureIntervalSeconds { get; set; } = 0.004;

    /// <summary>JPEG quality 1-100. Lower = faster transfer (better for e-ink).</summary>
    public int JpegQuality { get; set; } = 40;

    /// <summary>
    /// Enables password protection for this screen.
    /// When enabled, the web host requires a runtime-generated access code.
    /// </summary>
    public bool ScreenSecurityEnabled { get; set; }

    /// <summary>
    /// Maximum simultaneous viewers allowed for this screen.
    /// 0 = unlimited.
    /// </summary>
    public int MaxViewers { get; set; } = 1;

    /// <summary>
    /// Whether touch input should start enabled on the web page for this screen.
    /// </summary>
    public bool TouchInputEnabled { get; set; }

    /// <summary>
    /// Whether the zoom (pinch) gesture is enabled.
    /// </summary>
    public bool TouchZoomEnabled { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds before triggering the zoom gesture.
    /// </summary>
    public int TouchZoomDelayMs { get; set; } = 50;

    /// <summary>
    /// Whether the hold (long press) gesture is enabled.
    /// </summary>
    public bool TouchHoldEnabled { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds before triggering the hold gesture.
    /// </summary>
    public int TouchHoldDelayMs { get; set; } = 250;

    /// <summary>
    /// Whether the scroll (two fingers) gesture is enabled.
    /// </summary>
    public bool TouchScrollEnabled { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds before triggering the scroll gesture.
    /// </summary>
    public int TouchScrollDelayMs { get; set; } = 250;

    /// <summary>
    /// Whether to preserve cursor position after tap actions.
    /// If true, taps execute at the touch coordinate but restore the cursor position.
    /// If false, the cursor moves to the touch coordinate (current behavior).
    /// </summary>
    public bool TouchPreserveCursor { get; set; } = false;

    public int Port { get; set; } = 8000;

    /// <summary>
    /// Index of the monitor to capture from Screen.AllScreens.
    /// -1 = auto: use the created Parsec virtual display.
    ///  0 = primary monitor.
    ///  1 = second monitor (physical or virtual).
    /// Normalmente conviene dejar -1 para capturar automáticamente el monitor virtual creado por la app.
    /// </summary>
    public int MonitorIndex { get; set; } = -1;

    /// <summary>
    /// Side of the primary monitor where the virtual display should be attached.
    /// Values accepted: right, left, top, bottom.
    /// También acepta: derecha, izquierda, arriba, abajo.
    /// </summary>
    public string VirtualDisplayPlacement { get; set; } = "right";

    /// <summary>
    /// How the Kindle page should fit the incoming image into the visible browser area.
    /// contain = mantiene toda la imagen y puede dejar franjas negras.
    /// cover   = llena toda el área visible recortando sobrantes.
    /// fill    = llena toda el área visible deformando la imagen si hace falta.
    /// Para Kindle Paperwhite 12 en navegador, "contain" es el valor recomendado.
    /// </summary>
    public string BrowserImageFit { get; set; } = "contain";

    /// <summary>
    /// Modo de acceso a la red para la transmisión.
    /// WiFi = Acceso estándar, USB = Anclaje de red USB (max 1 viewer, sin seguridad).
    /// </summary>
    public NetworkAccessMode NetworkMode { get; set; } = NetworkAccessMode.WiFi;

    public VirtualScreenConfig Clone() => new()
    {
        Enabled = Enabled,
        Width = Width,
        Height = Height,
        Profile = Profile,
        Landscape = Landscape,
        CustomWidth = CustomWidth,
        CustomHeight = CustomHeight,
        TransmissionMethod = TransmissionMethod,
        CaptureIntervalSeconds = CaptureIntervalSeconds,
        JpegQuality = JpegQuality,
        ScreenSecurityEnabled = ScreenSecurityEnabled,
        MaxViewers = MaxViewers,
        TouchInputEnabled = TouchInputEnabled,
        TouchZoomEnabled = TouchZoomEnabled,
        TouchZoomDelayMs = TouchZoomDelayMs,
        TouchHoldEnabled = TouchHoldEnabled,
        TouchHoldDelayMs = TouchHoldDelayMs,
        TouchScrollEnabled = TouchScrollEnabled,
        TouchScrollDelayMs = TouchScrollDelayMs,
        TouchPreserveCursor = TouchPreserveCursor,
        Port = Port,
        MonitorIndex = MonitorIndex,
        VirtualDisplayPlacement = VirtualDisplayPlacement,
        BrowserImageFit = BrowserImageFit,
        NetworkMode = NetworkMode,
    };

    public void CopyTo(VirtualScreenConfig target)
    {
        target.Enabled = Enabled;
        target.Width = Width;
        target.Height = Height;
        target.Profile = Profile;
        target.Landscape = Landscape;
        target.CustomWidth = CustomWidth;
        target.CustomHeight = CustomHeight;
        target.TransmissionMethod = TransmissionMethod;
        target.CaptureIntervalSeconds = CaptureIntervalSeconds;
        target.JpegQuality = JpegQuality;
        target.ScreenSecurityEnabled = ScreenSecurityEnabled;
        target.MaxViewers = MaxViewers;
        target.TouchInputEnabled = TouchInputEnabled;
        target.TouchZoomEnabled = TouchZoomEnabled;
        target.TouchZoomDelayMs = TouchZoomDelayMs;
        target.TouchHoldEnabled = TouchHoldEnabled;
        target.TouchHoldDelayMs = TouchHoldDelayMs;
        target.TouchScrollEnabled = TouchScrollEnabled;
        target.TouchScrollDelayMs = TouchScrollDelayMs;
        target.TouchPreserveCursor = TouchPreserveCursor;
        target.Port = Port;
        target.MonitorIndex = MonitorIndex;
        target.VirtualDisplayPlacement = VirtualDisplayPlacement;
        target.BrowserImageFit = BrowserImageFit;
        target.NetworkMode = NetworkMode;
    }
}
