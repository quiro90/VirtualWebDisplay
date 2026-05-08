using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure.Interop;

namespace VirtualWebDisplay.Streaming;

/// <summary>
/// Captures the desktop using DXGI Desktop Duplication when available, and falls back
/// automatically to GDI (<see cref="Graphics.CopyFromScreen"/>) for virtual or indirect
/// display adapters such as Parsec VDD that do not support <c>IDXGIOutputDuplication</c>.
/// <para>
/// Delivers raw BGRA32 frames via <see cref="RawFrameAvailable"/> (consumed by the H.264
/// encoder) and JPEG frames via <see cref="GetCurrentJpegFrame"/> (consumed by the WebImage
/// polling and MJPEG endpoints).
/// </para>
/// </summary>
internal sealed class DxgiCaptureService : BackgroundService, IFrameSource
{
    // How long AcquireNextFrame blocks before returning DXGI_ERROR_WAIT_TIMEOUT.
    // Governs the "no-change" spin rate — 100 ms means at most one no-op per 100 ms.
    private const int FrameAcquireTimeoutMs = 100;

    // Raw HRESULT codes not exposed as named constants in Vortice.
    private const int DxgiErrorWaitTimeout = unchecked((int)0x887A0027);
    private const int DxgiErrorAccessLost  = unchecked((int)0x887A0026);

    private readonly VirtualScreenConfig _config;
    private readonly ILogger<DxgiCaptureService> _logger;
    private readonly Func<string?> _preferredDeviceNameProvider;
    private byte[] _currentJpeg = [];
    private readonly Lock _jpegLock = new();
    private long _lastJpegDemandTicks;
    private int _activeMjpegConsumers;

    // Keep JPEG generation enabled briefly after a /cap request to smooth polling.
    private const double JpegDemandWindowSeconds = 2.0;

    public DxgiCaptureService(
        VirtualScreenConfig config,
        ILogger<DxgiCaptureService> logger,
        Func<string?>? preferredDeviceNameProvider = null)
    {
        _config = config;
        _logger = logger;
        _preferredDeviceNameProvider = preferredDeviceNameProvider ?? (() => null);
    }

    /// <inheritdoc/>
    public byte[] GetCurrentJpegFrame()
    {
        lock (_jpegLock) return _currentJpeg;
    }

    /// <inheritdoc/>
    public void NotifyJpegDemand()
    {
        Volatile.Write(ref _lastJpegDemandTicks, Stopwatch.GetTimestamp());
    }

    /// <inheritdoc/>
    public void EnterMjpegDemand()
    {
        Interlocked.Increment(ref _activeMjpegConsumers);
        NotifyJpegDemand();
    }

    /// <inheritdoc/>
    public void ExitMjpegDemand()
    {
        if (Interlocked.Decrement(ref _activeMjpegConsumers) < 0)
            Interlocked.Exchange(ref _activeMjpegConsumers, 0);
    }

    /// <inheritdoc/>
    public event Action<RawFrame>? RawFrameAvailable;

    // How many consecutive zero-frame DXGI sessions trigger GDI fallback.
    // Indirect display adapters (e.g. Parsec VDD) produce ACCESS_LOST immediately,
    // so after this many retries we give up on DXGI for this session.
    private const int MaxNoFrameAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Diagnostic: log all screens visible at capture startup so we can verify
        // the correct monitor index is being targeted.
        var allScreens = Screen.AllScreens;
        for (int i = 0; i < allScreens.Length; i++)
        {
            var s = allScreens[i];
            _logger.LogInformation(
                "Screen[{i}] '{Device}' {W}x{H} at ({X},{Y}){Primary}",
                i, s.DeviceName, s.Bounds.Width, s.Bounds.Height,
                s.Bounds.Left, s.Bounds.Top,
                s.Primary ? " [PRIMARY]" : "");
        }
        var desiredBounds = ResolveCaptureBounds();
        _logger.LogInformation(
            "Capture targeting monitor index {Index} with bounds ({X},{Y}) {W}x{H}.",
            _config.MonitorIndex,
            desiredBounds.Left,
            desiredBounds.Top,
            desiredBounds.Width,
            desiredBounds.Height);
        var preferredDeviceName = GetPreferredDeviceName();
        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
            _logger.LogInformation("Capture preferred runtime device: {DeviceName}.", preferredDeviceName);

        int noFrameAttempts = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (noFrameAttempts >= MaxNoFrameAttempts)
            {
                _logger.LogWarning(
                    "DXGI Desktop Duplication unavailable on monitor {MonitorIndex} after {N} attempts " +
                    "(indirect/virtual display adapter). Falling back to GDI capture.",
                    _config.MonitorIndex, noFrameAttempts);
                await RunGdiCaptureLoopAsync(stoppingToken);
                return;
            }

            try
            {
                desiredBounds = ResolveCaptureBounds();
                preferredDeviceName = GetPreferredDeviceName();
                using var session = DxgiSession.Create(desiredBounds, _config.MonitorIndex, preferredDeviceName, _logger);
                bool anyFrame = RunCaptureLoop(session, stoppingToken);
                // A session that acquired at least one frame is healthy; reset failure counter.
                if (anyFrame) noFrameAttempts = 0;
                else          noFrameAttempts++;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                noFrameAttempts++;
                _logger.LogWarning(ex,
                    "DXGI capture session failed on monitor {MonitorIndex}, reinitializing " +
                    "(attempt {Attempt}/{Max}).",
                    _config.MonitorIndex, noFrameAttempts, MaxNoFrameAttempts);
                try { await Task.Delay(500, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    // How long to wait for the very first frame from a DXGI session before giving up.
    // Parsec VDD (and other indirect adapters) can create a DuplicateOutput successfully
    // but then return DXGI_ERROR_WAIT_TIMEOUT forever — never delivering a frame.
    // After this deadline we exit the loop and count it as a no-frame attempt.
    private const double FirstFrameTimeoutSeconds = 3.0;

    // Some indirect/virtual adapters can report successful AcquireNextFrame calls but
    // return uniform black buffers. If this persists from startup, treat DXGI as unusable
    // for this monitor and fall back to the GDI path.
    private const int MaxInitialBlackFrames = 90;

    // Check monitor topology periodically to adapt live to position changes.
    private const double TopologyCheckIntervalSeconds = 0.5;

    /// <summary>
    /// Runs the DXGI capture loop. Returns <c>true</c> if at least one frame was captured
    /// (indicating the adapter supports duplication), <c>false</c> if the session died
    /// immediately without delivering any frame.
    /// </summary>
    private bool RunCaptureLoop(DxgiSession session, CancellationToken ct)
    {
        var jpegQuality = TransmissionModeOptions.GetEffectiveJpegQuality(_config);
        var captureBounds = ResolveCaptureBounds();
        bool anyFrame = false;
        int consecutiveBlackFrames = 0;
        var sw = Stopwatch.StartNew();
        var topologyCheckSw = Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            if (topologyCheckSw.Elapsed.TotalSeconds >= TopologyCheckIntervalSeconds)
            {
                topologyCheckSw.Restart();
                var latestBounds = ResolveCaptureBounds();
                if (!SameBounds(latestBounds, captureBounds))
                {
                    _logger.LogInformation(
                        "Capture target moved from ({OldX},{OldY}) {OldW}x{OldH} to ({NewX},{NewY}) {NewW}x{NewH}; reinitializing DXGI session.",
                        captureBounds.Left, captureBounds.Top, captureBounds.Width, captureBounds.Height,
                        latestBounds.Left, latestBounds.Top, latestBounds.Width, latestBounds.Height);
                    return false;
                }
            }

            if (!session.TryAcquireFrame(out var rawFrame))
            {
                // If DXGI has never delivered a frame and the deadline expired, exit.
                // This breaks the infinite WAIT_TIMEOUT spin on indirect display adapters.
                if (!anyFrame && sw.Elapsed.TotalSeconds > FirstFrameTimeoutSeconds)
                    return false;
                continue;
            }

            anyFrame = true;

            // Detect black-frame-only startup on unsupported virtual outputs.
            if (IsLikelyBlackFrame(rawFrame.Data))
            {
                consecutiveBlackFrames++;
                if (consecutiveBlackFrames >= MaxInitialBlackFrames)
                {
                    _logger.LogWarning(
                        "DXGI delivered {Count} consecutive black frames on monitor {MonitorIndex}; " +
                        "falling back to GDI capture.",
                        consecutiveBlackFrames,
                        _config.MonitorIndex);
                    return false;
                }
            }
            else
            {
                consecutiveBlackFrames = 0;
            }

            OverlayCursorIfVisible(rawFrame.Data, rawFrame.Width, rawFrame.Height, captureBounds.Left, captureBounds.Top);

            RawFrameAvailable?.Invoke(rawFrame);

            if (ShouldEncodeJpeg())
            {
                var jpeg = JpegFallbackEncoder.Encode(rawFrame.Data, rawFrame.Width, rawFrame.Height, jpegQuality);
                lock (_jpegLock) _currentJpeg = jpeg;
            }
        }

        return anyFrame;
    }

    private Rectangle ResolveCaptureBounds()
    {
        var screen = ResolveTargetScreen(Screen.AllScreens);
        return screen.Bounds;
    }

    private Screen ResolveTargetScreen(Screen[] screens)
    {
        if (screens.Length == 0)
            throw new InvalidOperationException("No screens available for capture.");

        var preferredDeviceName = GetPreferredDeviceName();
        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            var byDevice = screens.FirstOrDefault(s =>
                string.Equals(s.DeviceName, preferredDeviceName, StringComparison.OrdinalIgnoreCase));
            if (byDevice is not null)
                return byDevice;
        }

        // When Windows-managed placement is active, SavedPosition is the most stable
        // identifier across monitor re-ordering and index churn.
        if (_config.SavedPositionX.HasValue && _config.SavedPositionY.HasValue)
        {
            var savedPoint = new Point(_config.SavedPositionX.Value, _config.SavedPositionY.Value);

            // First try exact top-left match.
            var exact = screens.FirstOrDefault(s =>
                s.Bounds.Left == savedPoint.X && s.Bounds.Top == savedPoint.Y);
            if (exact is not null)
                return exact;

            // Fallback: choose the screen that contains saved point, else nearest by center distance.
            var containing = screens.FirstOrDefault(s => s.Bounds.Contains(savedPoint));
            if (containing is not null)
                return containing;

            return screens
                .OrderBy(s => DistanceSquared(savedPoint, new Point(
                    s.Bounds.Left + s.Bounds.Width / 2,
                    s.Bounds.Top + s.Bounds.Height / 2)))
                .First();
        }

        int screenIndex = _config.MonitorIndex >= 0 && _config.MonitorIndex < screens.Length
            ? _config.MonitorIndex
            : Array.FindIndex(screens, s => s.Primary);

        if (screenIndex < 0)
            screenIndex = 0;

        return screens[screenIndex];
    }

    private static long DistanceSquared(Point a, Point b)
    {
        long dx = a.X - b.X;
        long dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private string? GetPreferredDeviceName()
    {
        var name = _preferredDeviceNameProvider();
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    private static bool SameBounds(Rectangle a, Rectangle b) =>
        a.Left == b.Left &&
        a.Top == b.Top &&
        a.Width == b.Width &&
        a.Height == b.Height;

    private static bool IsLikelyBlackFrame(byte[] bgra)
    {
        if (bgra.Length < 4)
            return true;

        // Sample ~2k pixels max, ignoring alpha. A single visible non-black sample is enough.
        int pixelCount = bgra.Length / 4;
        int sampleStep = Math.Max(1, pixelCount / 2048);

        for (int p = 0; p < pixelCount; p += sampleStep)
        {
            int i = p * 4;
            if (bgra[i] != 0 || bgra[i + 1] != 0 || bgra[i + 2] != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// GDI fallback capture loop used when DXGI Desktop Duplication is unavailable
    /// (e.g. Parsec VDD and other indirect display adapters).
    /// Uses <see cref="Graphics.CopyFromScreen"/> which accesses the DWM virtual desktop
    /// composition surface via <c>GetDC(NULL)</c> — the only GDI path that works for
    /// indirect/virtual display adapters.
    /// </summary>
    private async Task RunGdiCaptureLoopAsync(CancellationToken ct)
    {
        // Re-enumerate screens at fallback time and keep adapting live to topology changes.
        var screens = Screen.AllScreens;
        var screen = ResolveTargetScreen(screens);
        int screenIndex = Array.FindIndex(screens, s => s.DeviceName == screen.DeviceName);
        if (screenIndex < 0) screenIndex = 0;

        int srcX = screen.Bounds.Left;
        int srcY = screen.Bounds.Top;
        int width = screen.Bounds.Width;
        int height = screen.Bounds.Height;

        int intervalMs  = (int)Math.Max(1, _config.CaptureIntervalSeconds * 1000);
        var jpegQuality = TransmissionModeOptions.GetEffectiveJpegQuality(_config);

        _logger.LogInformation(
            "GDI capture started: monitor {Index} '{Device}' at ({X},{Y}) {W}x{H}, interval {Ms}ms.",
            screenIndex, screen.DeviceName, srcX, srcY, width, height, intervalMs);

        var topologyCheckSw = Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (topologyCheckSw.Elapsed.TotalSeconds >= TopologyCheckIntervalSeconds)
                {
                    topologyCheckSw.Restart();
                    var latestBounds = ResolveCaptureBounds();
                    var currentBounds = Rectangle.FromLTRB(srcX, srcY, srcX + width, srcY + height);
                    if (!SameBounds(latestBounds, currentBounds))
                    {
                        srcX = latestBounds.Left;
                        srcY = latestBounds.Top;
                        width = latestBounds.Width;
                        height = latestBounds.Height;
                        _logger.LogInformation(
                            "GDI capture target moved; updating capture region to ({X},{Y}) {W}x{H}.",
                            srcX, srcY, width, height);
                    }
                }

                var tsUs     = Stopwatch.GetTimestamp() * 1_000_000L / Stopwatch.Frequency;
                var bytes    = CaptureGdi(srcX, srcY, width, height);
                OverlayCursorIfVisible(bytes, width, height, srcX, srcY);
                var rawFrame = new RawFrame(bytes, width, height, tsUs);

                RawFrameAvailable?.Invoke(rawFrame);

                if (ShouldEncodeJpeg())
                {
                    var jpeg = JpegFallbackEncoder.Encode(bytes, width, height, jpegQuality);
                    lock (_jpegLock) _currentJpeg = jpeg;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GDI capture error on monitor {Index}, continuing.", screenIndex);
            }

            try { await Task.Delay(intervalMs, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private static unsafe void OverlayCursorIfVisible(byte[] bgra32, int width, int height, int captureLeft, int captureTop)
    {
        if (bgra32.Length < width * height * 4)
            return;

        var cursorInfo = new CursorNativeMethods.CURSORINFO
        {
            cbSize = Marshal.SizeOf<CursorNativeMethods.CURSORINFO>()
        };

        if (!CursorNativeMethods.GetCursorInfo(out cursorInfo)
            || (cursorInfo.flags & CursorNativeMethods.CursorShowing) == 0
            || cursorInfo.hCursor == IntPtr.Zero)
            return;

        var iconHandle = CursorNativeMethods.CopyIcon(cursorInfo.hCursor);
        if (iconHandle == IntPtr.Zero)
            return;

        CursorNativeMethods.ICONINFO iconInfo = default;

        try
        {
            if (!CursorNativeMethods.GetIconInfo(iconHandle, out iconInfo))
                return;

            int cursorX = cursorInfo.ptScreenPos.X - captureLeft - iconInfo.xHotspot;
            int cursorY = cursorInfo.ptScreenPos.Y - captureTop - iconInfo.yHotspot;

            // Quick reject if the cursor hotspot is outside the captured frame.
            if (cursorX < -64 || cursorY < -64 || cursorX >= width || cursorY >= height)
                return;

            fixed (byte* ptr = bgra32)
            {
                using var bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, (IntPtr)ptr);
                using var graphics = Graphics.FromImage(bitmap);
                using var icon = Icon.FromHandle(iconHandle);
                graphics.DrawIcon(icon, cursorX, cursorY);
            }
        }
        finally
        {
            if (iconInfo.hbmMask != IntPtr.Zero)
                CursorNativeMethods.DeleteObject(iconInfo.hbmMask);

            if (iconInfo.hbmColor != IntPtr.Zero)
                CursorNativeMethods.DeleteObject(iconInfo.hbmColor);

            CursorNativeMethods.DestroyIcon(iconHandle);
        }
    }

    /// <summary>
    /// Captures a screen region using <c>Graphics.CopyFromScreen</c> which internally calls
    /// <c>BitBlt</c> from <c>GetDC(NULL)</c> (DWM virtual desktop surface). This is the only
    /// GDI approach guaranteed to work for indirect/virtual displays like Parsec VDD.
    /// Returns raw BGRA32 bytes (Format32bppArgb stored as B,G,R,A on Windows x86/x64).
    /// </summary>
    private static byte[] CaptureGdi(int srcX, int srcY, int width, int height)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
            g.CopyFromScreen(srcX, srcY, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            int rowBytes = width * 4;
            var result   = new byte[rowBytes * height];

            if (bmpData.Stride == rowBytes)
            {
                Marshal.Copy(bmpData.Scan0, result, 0, result.Length);
            }
            else
            {
                for (int row = 0; row < height; row++)
                    Marshal.Copy(bmpData.Scan0 + row * bmpData.Stride,
                                 result, row * rowBytes, rowBytes);
            }
            return result;
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    private bool ShouldEncodeJpeg()
    {
        if (Volatile.Read(ref _activeMjpegConsumers) > 0)
            return true;

        var lastDemandTicks = Volatile.Read(ref _lastJpegDemandTicks);
        if (lastDemandTicks <= 0)
            return false;

        var elapsedSeconds = (Stopwatch.GetTimestamp() - lastDemandTicks) / (double)Stopwatch.Frequency;
        return elapsedSeconds <= JpegDemandWindowSeconds;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DxgiSession — owns the entire D3D11 / DXGI resource graph for one
    // capture lifecycle. Recreated by the outer loop on any unrecoverable error.
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class DxgiSession : IDisposable
    {
        private readonly ID3D11Device _device;
        private readonly ID3D11DeviceContext _context;
        private readonly IDXGIOutputDuplication _duplication;
        private readonly int _width;
        private readonly int _height;
        private ID3D11Texture2D? _staging;
        private bool _disposed;

        private DxgiSession(
            ID3D11Device device,
            ID3D11DeviceContext context,
            IDXGIOutputDuplication duplication,
            int width,
            int height)
        {
            _device     = device;
            _context    = context;
            _duplication = duplication;
            _width      = width;
            _height     = height;
        }

        /// <summary>
        /// Initializes D3D11 device and DXGI output duplication for the target monitor.
        /// </summary>
        internal static DxgiSession Create(Rectangle targetBounds, int monitorIndex, string? preferredDeviceName, ILogger logger)
        {
            FindAdapterAndOutput(targetBounds, monitorIndex, preferredDeviceName, logger, out var adapter, out var output1);
            try
            {
                D3D11.D3D11CreateDevice(
                    adapter,
                    DriverType.Unknown,        // must be Unknown when a specific adapter is supplied
                    DeviceCreationFlags.None,
                    default,                   // use runtime default feature levels
                    out var device).CheckError();

                var context     = device!.ImmediateContext;
                var outputDesc  = output1.Description;
                var duplication = output1.DuplicateOutput(device);

                int w = outputDesc.DesktopCoordinates.Right  - outputDesc.DesktopCoordinates.Left;
                int h = outputDesc.DesktopCoordinates.Bottom - outputDesc.DesktopCoordinates.Top;

                return new DxgiSession(device, context, duplication, w, h);
            }
            finally
            {
                adapter.Dispose();
                output1.Dispose();
            }
        }

        /// <summary>
        /// Tries to acquire the next changed desktop frame.
        /// Returns <c>false</c> on <c>DXGI_ERROR_WAIT_TIMEOUT</c> (screen unchanged).
        /// Throws on ACCESS_LOST or other errors — the outer loop will recreate the session.
        /// </summary>
        internal bool TryAcquireFrame(out RawFrame rawFrame)
        {
            var hr = _duplication.AcquireNextFrame(FrameAcquireTimeoutMs, out _, out var resource);

            if (hr.Code == DxgiErrorWaitTimeout)
            {
                rawFrame = default;
                return false;
            }

            hr.CheckError(); // throws SharpGenException on ACCESS_LOST and other failure codes

            try
            {
                // GPU copy to the persistent staging texture — must happen while frame is held.
                using var texture = resource.QueryInterface<ID3D11Texture2D>();
                _context.CopyResource(EnsureStagingTexture(), texture);
            }
            finally
            {
                resource.Dispose();
                _duplication.ReleaseFrame(); // release ASAP, before the CPU Map call below
            }

            var bytes = MapStagingToBytes();
            var tsUs  = Stopwatch.GetTimestamp() * 1_000_000L / Stopwatch.Frequency;
            rawFrame  = new RawFrame(bytes, _width, _height, tsUs);
            return true;
        }

        private unsafe byte[] MapStagingToBytes()
        {
            var staging = EnsureStagingTexture();
            var mapped  = _context.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            try
            {
                int rowBytes = _width * 4; // BGRA32: 4 bytes per pixel
                var data     = new byte[rowBytes * _height];

                if ((int)mapped.RowPitch == rowBytes)
                {
                    // Pitch matches width — single flat copy.
                    Marshal.Copy(mapped.DataPointer, data, 0, data.Length);
                }
                else
                {
                    // GPU alignment adds padding — copy row by row.
                    var src = (byte*)mapped.DataPointer;
                    for (int y = 0; y < _height; y++)
                    {
                        new ReadOnlySpan<byte>(src + (long)y * mapped.RowPitch, rowBytes)
                            .CopyTo(data.AsSpan(y * rowBytes));
                    }
                }

                return data;
            }
            finally
            {
                _context.Unmap(staging, 0);
            }
        }

        private ID3D11Texture2D EnsureStagingTexture()
        {
            if (_staging is not null)
                return _staging;

            _staging = _device.CreateTexture2D(new Texture2DDescription
            {
                Format            = Format.B8G8R8A8_UNorm,
                Width             = (uint)_width,
                Height            = (uint)_height,
                ArraySize         = 1,
                MipLevels         = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage             = ResourceUsage.Staging,
                CPUAccessFlags    = CpuAccessFlags.Read,
                BindFlags         = BindFlags.None,
                MiscFlags         = ResourceOptionFlags.None,
            });

            return _staging;
        }

        /// <summary>
        /// Enumerates DXGI adapters and outputs to find the one that hosts the target
        /// monitor (matched by HMONITOR). Falls back to the primary display if the
        /// index cannot be matched.
        /// </summary>
        private static void FindAdapterAndOutput(
            Rectangle targetBounds,
            int monitorIndex,
            string? preferredDeviceName,
            ILogger logger,
            out IDXGIAdapter1 foundAdapter,
            out IDXGIOutput1 foundOutput)
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

            IDXGIAdapter1? bestAdapter = null;
            IDXGIOutput1? bestOutput = null;
            long bestOverlap = 0;
            long bestDistance = long.MaxValue;
            var targetCenter = new Point(
                targetBounds.Left + targetBounds.Width / 2,
                targetBounds.Top + targetBounds.Height / 2);

            for (uint ai = 0; factory.EnumAdapters1(ai, out var adapter).Success; ai++)
            {
                for (uint oi = 0; adapter.EnumOutputs(oi, out var output).Success; oi++)
                {
                    var desc = output.Description;
                    var dc = desc.DesktopCoordinates;
                    var outputBounds = Rectangle.FromLTRB(dc.Left, dc.Top, dc.Right, dc.Bottom);

                    // Preferred path: match the runtime DeviceName directly.
                    if (!string.IsNullOrWhiteSpace(preferredDeviceName)
                        && string.Equals(desc.DeviceName, preferredDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation(
                            "DXGI output matched by runtime device name: {DeviceName}.",
                            preferredDeviceName);
                        foundAdapter = adapter;
                        foundOutput = output.QueryInterface<IDXGIOutput1>();
                        output.Dispose();
                        return;
                    }

                    // Fast path: exact bounds match.
                    if (outputBounds.Left == targetBounds.Left
                        && outputBounds.Top == targetBounds.Top
                        && outputBounds.Width == targetBounds.Width
                        && outputBounds.Height == targetBounds.Height)
                    {
                        foundAdapter = adapter;
                        foundOutput  = output.QueryInterface<IDXGIOutput1>();
                        output.Dispose();
                        return;                           // adapter ownership transferred to caller
                    }

                    var overlap = IntersectionArea(outputBounds, targetBounds);
                    var center = new Point(outputBounds.Left + outputBounds.Width / 2, outputBounds.Top + outputBounds.Height / 2);
                    var distance = DistanceSquared(targetCenter, center);

                    bool better = overlap > bestOverlap || (overlap == bestOverlap && distance < bestDistance);
                    if (better)
                    {
                        bestAdapter?.Dispose();
                        bestOutput?.Dispose();

                        bestAdapter = adapter.QueryInterface<IDXGIAdapter1>();
                        bestOutput = output.QueryInterface<IDXGIOutput1>();
                        bestOverlap = overlap;
                        bestDistance = distance;
                    }

                    output.Dispose();
                }
                adapter.Dispose();
            }

            if (bestAdapter is not null && bestOutput is not null)
            {
                logger.LogWarning(
                    "Monitor index {Index} could not be matched exactly in DXGI; using nearest output with bounds ({X},{Y}) {W}x{H}.",
                    monitorIndex,
                    targetBounds.Left,
                    targetBounds.Top,
                    targetBounds.Width,
                    targetBounds.Height);

                foundAdapter = bestAdapter;
                foundOutput = bestOutput;
                return;
            }

            // Fallback: use first adapter, first output.
            logger.LogWarning(
                "Monitor index {Index} could not be matched in DXGI output enumeration; using primary display.",
                monitorIndex);

            factory.EnumAdapters1(0, out var fallbackAdapter).CheckError();
            fallbackAdapter.EnumOutputs(0, out var fallbackOutput).CheckError();
            foundAdapter = fallbackAdapter;
            foundOutput  = fallbackOutput.QueryInterface<IDXGIOutput1>();
            fallbackOutput.Dispose();
        }

        private static long IntersectionArea(Rectangle a, Rectangle b)
        {
            int left = Math.Max(a.Left, b.Left);
            int top = Math.Max(a.Top, b.Top);
            int right = Math.Min(a.Right, b.Right);
            int bottom = Math.Min(a.Bottom, b.Bottom);

            int width = right - left;
            int height = bottom - top;
            if (width <= 0 || height <= 0)
                return 0;

            return (long)width * height;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _staging?.Dispose();
            _duplication.Dispose();
            _context.Dispose();
            _device.Dispose();
        }
    }
}
