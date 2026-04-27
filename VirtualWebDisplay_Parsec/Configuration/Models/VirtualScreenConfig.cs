namespace VirtualWebDisplay.Configuration.Models;

public sealed class VirtualScreenConfig
{
    public bool Enabled { get; set; } = true;

    public int Width { get; set; } = 800;
    public int Height { get; set; } = 1280;

    public string Profile { get; set; } = string.Empty;
    public bool Landscape { get; set; }
    public int CustomWidth { get; set; } = 800;
    public int CustomHeight { get; set; } = 1280;

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

    public int Port { get; set; } = 8000;

    /// <summary>Legacy: migrado a StreamRotationDegrees. Se mantiene para leer configs guardadas antiguas.</summary>
    public bool RotateForPortrait { get; set; }

    /// <summary>Degrees to rotate the captured frame before encoding: 0, 90, 180 or 270.</summary>
    public int StreamRotationDegrees { get; set; }

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
        Port = Port,
        RotateForPortrait = RotateForPortrait,
        StreamRotationDegrees = StreamRotationDegrees,
        MonitorIndex = MonitorIndex,
        VirtualDisplayPlacement = VirtualDisplayPlacement,
        BrowserImageFit = BrowserImageFit,
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
        target.Port = Port;
        target.RotateForPortrait = RotateForPortrait;
        target.StreamRotationDegrees = StreamRotationDegrees;
        target.MonitorIndex = MonitorIndex;
        target.VirtualDisplayPlacement = VirtualDisplayPlacement;
        target.BrowserImageFit = BrowserImageFit;
    }
}
