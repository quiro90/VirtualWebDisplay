using System.Diagnostics;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Controles de configuración para una pestaña de pantalla en el formulario de configuración.
/// </summary>
public sealed class ScreenTabControls
{
    private readonly VirtualScreenConfig _baseConfig;
    private readonly bool _allowDisable;
    private readonly bool _portEditable;
    private readonly CheckBox? _enabledCheckBox;
    private readonly ComboBox _placementCombo;
    private readonly NumericUpDown _portInput;
    private readonly ComboBox _transmissionMethodCombo;
    private readonly NumericUpDown _captureIntervalInput;
    private readonly TrackBar _jpegQualitySlider;
    private readonly Label _jpegQualityValueLabel;
    private readonly ComboBox _streamRotationCombo;
    private readonly ComboBox _browserImageFitCombo;
    private readonly Control[] _managedControls;
    private readonly string _localIp;
    private readonly LinkLabel _httpUrlLink;

    public ScreenTabControls(string title, bool allowDisable, bool isInitialStartup, VirtualScreenConfig config, string localIp)
    {
        _baseConfig = config.Clone();
        _allowDisable = allowDisable;
        _portEditable = isInitialStartup;
        _localIp = localIp;
        TabPage = new TabPage(title);

        var currentTop = 14;
        if (allowDisable)
        {
            _enabledCheckBox = new CheckBox
            {
                Left = 14,
                Top = currentTop,
                Width = 180,
                Text = "Habilitar (experimental)",
            };
            TabPage.Controls.Add(_enabledCheckBox);
            currentTop += 28;
        }

        // Fila 1: Puerto | Transmisión | Posición
        var portLabel = CreateLabel("Puerto:", 14, currentTop);
        _portInput = new NumericUpDown
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 72,
            Minimum = 1,
            Maximum = 65535,
        };

        var methodLabel = CreateLabel("Transmisión:", 98, currentTop);
        _transmissionMethodCombo = new ComboBox
        {
            Left = 98,
            Top = currentTop + 18,
            Width = 170,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _transmissionMethodCombo.Items.AddRange(
        [
            new TransmissionMethodItem(TransmissionModeOptions.WebImage, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.WebImage)),
            new TransmissionMethodItem(TransmissionModeOptions.Rtc, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.Rtc)),
        ]);

        var placementLabel = CreateLabel("Posición:", 280, currentTop);
        _placementCombo = new ComboBox
        {
            Left = 280,
            Top = currentTop + 18,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _placementCombo.Items.AddRange(
        [
            new PlacementItem("right", "Derecha"),
            new PlacementItem("left", "Izquierda"),
            new PlacementItem("top", "Arriba"),
            new PlacementItem("bottom", "Abajo"),
            new PlacementItem(VirtualDisplayPlacementOptions.Duplicate, "Duplicar"),
        ]);

        currentTop += 54;

        // Fila 2: Rotación stream
        var rotationLabel = CreateLabel("Rotación stream:", 14, currentTop);
        _streamRotationCombo = new ComboBox
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 230,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _streamRotationCombo.Items.AddRange(
        [
            new StreamRotationItem(0,   "Sin rotación (0°)"),
            new StreamRotationItem(90,  "Rotar 90°"),
            new StreamRotationItem(180, "Rotar 180°"),
            new StreamRotationItem(270, "Rotar 270°"),
        ]);

        currentTop += 54;

        // Fila 3: Actualizar cada (ms) | Calidad JPEG
        var captureIntervalLabel = CreateLabel("Actualizar cada (ms):", 14, currentTop);
        _captureIntervalInput = new NumericUpDown
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 72,
            Minimum = 3M,
            Maximum = 500M,
            DecimalPlaces = 0,
            Increment = 1M,
        };

        var qualityLabel = CreateLabel("Calidad JPEG:", 100, currentTop);
        _jpegQualitySlider = new TrackBar
        {
            Left = 100,
            Top = currentTop + 10,
            Width = 280,
            Minimum = 10,
            Maximum = 100,
            TickFrequency = 10,
            SmallChange = 5,
            LargeChange = 10,
        };
        _jpegQualityValueLabel = new Label
        {
            Left = 388,
            Top = currentTop + 20,
            Width = 50,
        };

        currentTop += 54;

        // Fila 4: Ajuste | Botón configuración Windows
        var fitLabel = CreateLabel("Ajuste:", 14, currentTop);
        _browserImageFitCombo = new ComboBox
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _browserImageFitCombo.Items.AddRange(
        [
            new ImageFitItem("fill", "Estirar (llenar)"),
            new ImageFitItem("cover", "Recortar (cover)"),
            new ImageFitItem("contain", "Contener (barras)"),
        ]);

        var windowsDisplayButton = new Button
        {
            Left = 206,
            Top = currentTop + 14,
            Width = 254,
            Height = 28,
            Text = "Configurar pantallas (Windows) ↗",
        };
        windowsDisplayButton.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true }); }
            catch { }
        };

        currentTop += 54;

        _httpUrlLink = new LinkLabel
        {
            Left = 14,
            Top = currentTop,
            Width = 440,
            AutoSize = false,
            Text = $"http://{_localIp}:{config.Port}",
        };

        _managedControls =
        [
            placementLabel,
            _placementCombo,
            portLabel,
            _portInput,
            methodLabel,
            _transmissionMethodCombo,
            captureIntervalLabel,
            _captureIntervalInput,
            qualityLabel,
            _jpegQualitySlider,
            _jpegQualityValueLabel,
            rotationLabel,
            _streamRotationCombo,
            fitLabel,
            _browserImageFitCombo,
        ];

        TabPage.Controls.AddRange(_managedControls);
        TabPage.Controls.Add(_httpUrlLink);
        TabPage.Controls.Add(windowsDisplayButton);

        _httpUrlLink.LinkClicked += (_, _) => OpenUrl(_httpUrlLink.Text);

        Initialize(config);

        if (_enabledCheckBox is not null)
            _enabledCheckBox.CheckedChanged += (_, _) => UpdateState();
        _placementCombo.SelectedIndexChanged += (_, _) => UpdateState();
        _portInput.ValueChanged += (_, _) => UpdateState();
        _transmissionMethodCombo.SelectedIndexChanged += (_, _) => UpdateState();
        _captureIntervalInput.ValueChanged += (_, _) => UpdateState();
        _jpegQualitySlider.ValueChanged += (_, _) => UpdateState();
        _streamRotationCombo.SelectedIndexChanged += (_, _) => UpdateState();
        _browserImageFitCombo.SelectedIndexChanged += (_, _) => UpdateState();
    }

    public TabPage TabPage { get; }

    public VirtualScreenConfig BuildConfig(bool alwaysEnabled)
    {
        var config = _baseConfig.Clone();
        config.Enabled = alwaysEnabled || _enabledCheckBox?.Checked == true;
        config.Port = (int)_portInput.Value;
        config.TransmissionMethod = ((TransmissionMethodItem)_transmissionMethodCombo.SelectedItem!).Method;
        config.CaptureIntervalSeconds = (double)_captureIntervalInput.Value / 1000.0;
        config.JpegQuality = _jpegQualitySlider.Value;
        config.StreamRotationDegrees = ((StreamRotationItem)_streamRotationCombo.SelectedItem!).Degrees;
        config.BrowserImageFit = ((ImageFitItem)_browserImageFitCombo.SelectedItem!).Fit;
        config.VirtualDisplayPlacement = ((PlacementItem)_placementCombo.SelectedItem!).Placement;

        TransmissionModeOptions.EnsureValidSelection(config);
        return config;
    }

    private void Initialize(VirtualScreenConfig config)
    {
        if (_enabledCheckBox is not null)
            _enabledCheckBox.Checked = config.Enabled;

        _portInput.Value = Math.Max(_portInput.Minimum, Math.Min(_portInput.Maximum, config.Port));
        _captureIntervalInput.Value = Math.Clamp((decimal)(config.CaptureIntervalSeconds * 1000), _captureIntervalInput.Minimum, _captureIntervalInput.Maximum);
        _jpegQualitySlider.Value = Math.Clamp(config.JpegQuality, _jpegQualitySlider.Minimum, _jpegQualitySlider.Maximum);

        var validDegrees = new[] { 0, 90, 180, 270 };
        var degrees = validDegrees.Contains(config.StreamRotationDegrees) ? config.StreamRotationDegrees : 0;
        _streamRotationCombo.SelectedItem = _streamRotationCombo.Items.Cast<StreamRotationItem>()
            .First(item => item.Degrees == degrees);

        var normalizedPlacement = VirtualDisplayPlacementOptions.Normalize(config.VirtualDisplayPlacement);
        _placementCombo.SelectedItem = _placementCombo.Items.Cast<PlacementItem>()
            .FirstOrDefault(item => item.Placement == normalizedPlacement)
            ?? _placementCombo.Items.Cast<PlacementItem>().First(item => item.Placement == VirtualDisplayPlacementOptions.Right);

        var normalizedFit = config.BrowserImageFit?.Trim().ToLowerInvariant() ?? "fill";
        _browserImageFitCombo.SelectedItem = _browserImageFitCombo.Items.Cast<ImageFitItem>()
            .FirstOrDefault(item => item.Fit == normalizedFit)
            ?? _browserImageFitCombo.Items.Cast<ImageFitItem>().First(item => item.Fit == "fill");

        _transmissionMethodCombo.SelectedItem = _transmissionMethodCombo.Items.Cast<TransmissionMethodItem>()
            .First(item => item.Method == TransmissionModeOptions.NormalizeMethod(config.TransmissionMethod));

        UpdateState();
    }

    private void UpdateState()
    {
        var enabled = !_allowDisable || _enabledCheckBox?.Checked == true;
        foreach (var control in _managedControls)
            control.Enabled = enabled && (control != _portInput || _portEditable);

        _jpegQualityValueLabel.Text = $"{_jpegQualitySlider.Value}%";

        var port = (int)_portInput.Value;
        _httpUrlLink.Text = $"http://{_localIp}:{port}";
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static Label CreateLabel(string text, int left, int top) => new()
    {
        AutoSize = true,
        Left = left,
        Top = top,
        Text = text,
    };

    private sealed record TransmissionMethodItem(string Method, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record StreamRotationItem(int Degrees, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record PlacementItem(string Placement, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record ImageFitItem(string Fit, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
