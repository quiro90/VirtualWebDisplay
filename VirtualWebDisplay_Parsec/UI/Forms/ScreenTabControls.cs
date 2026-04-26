using System.Diagnostics;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Localization;

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
    private readonly Label _portLabel;
    private readonly Label _methodLabel;
    private readonly Label _placementLabel;
    private readonly ComboBox _placementCombo;
    private readonly NumericUpDown _portInput;
    private readonly ComboBox _transmissionMethodCombo;
    private readonly Label _captureIntervalLabel;
    private readonly NumericUpDown _captureIntervalInput;
    private readonly Label _qualityLabel;
    private readonly TrackBar _jpegQualitySlider;
    private readonly Label _jpegQualityValueLabel;
    private readonly Label _rotationLabel;
    private readonly ComboBox _streamRotationCombo;
    private readonly Label _fitLabel;
    private readonly ComboBox _browserImageFitCombo;
    private readonly Control[] _managedControls;
    private readonly string _localIp;
    private readonly LinkLabel _httpUrlLink;
    private readonly Button _windowsDisplayButton;

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
                Text = AppText.Get("Tab_EnableExperimental"),
            };
            TabPage.Controls.Add(_enabledCheckBox);
            currentTop += 28;
        }

        // Fila 1: Puerto | Transmisión | Posición
        _portLabel = CreateLabel(AppText.Get("Tab_Label_Port"), 14, currentTop);
        _portInput = new NumericUpDown
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 72,
            Minimum = 1,
            Maximum = 65535,
        };

        _methodLabel = CreateLabel(AppText.Get("Tab_Label_Transmission"), 98, currentTop);
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

        _placementLabel = CreateLabel(AppText.Get("Tab_Label_Placement"), 280, currentTop);
        _placementCombo = new ComboBox
        {
            Left = 280,
            Top = currentTop + 18,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _placementCombo.Items.AddRange(
        [
            new PlacementItem("right", AppText.Get("Tab_Placement_Right")),
            new PlacementItem("left", AppText.Get("Tab_Placement_Left")),
            new PlacementItem("top", AppText.Get("Tab_Placement_Top")),
            new PlacementItem("bottom", AppText.Get("Tab_Placement_Bottom")),
            new PlacementItem(VirtualDisplayPlacementOptions.Duplicate, AppText.Get("Tab_Placement_Duplicate")),
        ]);

        currentTop += 54;

        // Fila 2: Rotación stream
        _rotationLabel = CreateLabel(AppText.Get("Tab_Label_StreamRotation"), 14, currentTop);
        _streamRotationCombo = new ComboBox
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 230,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _streamRotationCombo.Items.AddRange(
        [
            new StreamRotationItem(0,   AppText.Get("Tab_Rotation_0")),
            new StreamRotationItem(90,  AppText.Get("Tab_Rotation_90")),
            new StreamRotationItem(180, AppText.Get("Tab_Rotation_180")),
            new StreamRotationItem(270, AppText.Get("Tab_Rotation_270")),
        ]);

        currentTop += 54;

        // Fila 3: Actualizar cada (ms) | Calidad JPEG
        _captureIntervalLabel = CreateLabel(AppText.Get("Tab_Label_CaptureIntervalMs"), 14, currentTop);
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

        _qualityLabel = CreateLabel(AppText.Get("Tab_Label_JpegQuality"), 100, currentTop);
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
        _fitLabel = CreateLabel(AppText.Get("Tab_Label_BrowserFit"), 14, currentTop);
        _browserImageFitCombo = new ComboBox
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _browserImageFitCombo.Items.AddRange(
        [
            new ImageFitItem("fill", AppText.Get("Tab_BrowserFit_Fill")),
            new ImageFitItem("cover", AppText.Get("Tab_BrowserFit_Cover")),
            new ImageFitItem("contain", AppText.Get("Tab_BrowserFit_Contain")),
        ]);

        _windowsDisplayButton = new Button
        {
            Left = 206,
            Top = currentTop + 14,
            Width = 254,
            Height = 28,
            Text = AppText.Get("Tab_Button_OpenWindowsDisplay"),
        };
        _windowsDisplayButton.Click += (_, _) =>
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
            _placementLabel,
            _placementCombo,
            _portLabel,
            _portInput,
            _methodLabel,
            _transmissionMethodCombo,
            _captureIntervalLabel,
            _captureIntervalInput,
            _qualityLabel,
            _jpegQualitySlider,
            _jpegQualityValueLabel,
            _rotationLabel,
            _streamRotationCombo,
            _fitLabel,
            _browserImageFitCombo,
        ];

        TabPage.Controls.AddRange(_managedControls);
        TabPage.Controls.Add(_httpUrlLink);
        TabPage.Controls.Add(_windowsDisplayButton);

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

    public void ApplyLocalization()
    {
        _enabledCheckBox?.SetBounds(_enabledCheckBox.Left, _enabledCheckBox.Top, _enabledCheckBox.Width, _enabledCheckBox.Height);
        if (_enabledCheckBox is not null)
            _enabledCheckBox.Text = AppText.Get("Tab_EnableExperimental");

        _portLabel.Text = AppText.Get("Tab_Label_Port");
        _methodLabel.Text = AppText.Get("Tab_Label_Transmission");
        _placementLabel.Text = AppText.Get("Tab_Label_Placement");
        _rotationLabel.Text = AppText.Get("Tab_Label_StreamRotation");
        _captureIntervalLabel.Text = AppText.Get("Tab_Label_CaptureIntervalMs");
        _qualityLabel.Text = AppText.Get("Tab_Label_JpegQuality");
        _fitLabel.Text = AppText.Get("Tab_Label_BrowserFit");
        _windowsDisplayButton.Text = AppText.Get("Tab_Button_OpenWindowsDisplay");

        RefreshTransmissionOptions();
        RefreshPlacementOptions();
        RefreshRotationOptions();
        RefreshBrowserFitOptions();
    }

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

    private void RefreshTransmissionOptions()
    {
        var selectedMethod = (_transmissionMethodCombo.SelectedItem as TransmissionMethodItem)?.Method
            ?? TransmissionModeOptions.WebImage;

        _transmissionMethodCombo.Items.Clear();
        _transmissionMethodCombo.Items.AddRange(
        [
            new TransmissionMethodItem(TransmissionModeOptions.WebImage, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.WebImage)),
            new TransmissionMethodItem(TransmissionModeOptions.Rtc, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.Rtc)),
        ]);

        _transmissionMethodCombo.SelectedItem = _transmissionMethodCombo.Items.Cast<TransmissionMethodItem>()
            .FirstOrDefault(item => item.Method == TransmissionModeOptions.NormalizeMethod(selectedMethod))
            ?? _transmissionMethodCombo.Items.Cast<TransmissionMethodItem>().First();
    }

    private void RefreshPlacementOptions()
    {
        var selectedPlacement = (_placementCombo.SelectedItem as PlacementItem)?.Placement
            ?? VirtualDisplayPlacementOptions.Right;

        _placementCombo.Items.Clear();
        _placementCombo.Items.AddRange(
        [
            new PlacementItem("right", AppText.Get("Tab_Placement_Right")),
            new PlacementItem("left", AppText.Get("Tab_Placement_Left")),
            new PlacementItem("top", AppText.Get("Tab_Placement_Top")),
            new PlacementItem("bottom", AppText.Get("Tab_Placement_Bottom")),
            new PlacementItem(VirtualDisplayPlacementOptions.Duplicate, AppText.Get("Tab_Placement_Duplicate")),
        ]);

        _placementCombo.SelectedItem = _placementCombo.Items.Cast<PlacementItem>()
            .FirstOrDefault(item => item.Placement == VirtualDisplayPlacementOptions.Normalize(selectedPlacement))
            ?? _placementCombo.Items.Cast<PlacementItem>().First(item => item.Placement == VirtualDisplayPlacementOptions.Right);
    }

    private void RefreshRotationOptions()
    {
        var selectedDegrees = (_streamRotationCombo.SelectedItem as StreamRotationItem)?.Degrees ?? 0;

        _streamRotationCombo.Items.Clear();
        _streamRotationCombo.Items.AddRange(
        [
            new StreamRotationItem(0, AppText.Get("Tab_Rotation_0")),
            new StreamRotationItem(90, AppText.Get("Tab_Rotation_90")),
            new StreamRotationItem(180, AppText.Get("Tab_Rotation_180")),
            new StreamRotationItem(270, AppText.Get("Tab_Rotation_270")),
        ]);

        _streamRotationCombo.SelectedItem = _streamRotationCombo.Items.Cast<StreamRotationItem>()
            .FirstOrDefault(item => item.Degrees == selectedDegrees)
            ?? _streamRotationCombo.Items.Cast<StreamRotationItem>().First(item => item.Degrees == 0);
    }

    private void RefreshBrowserFitOptions()
    {
        var selectedFit = (_browserImageFitCombo.SelectedItem as ImageFitItem)?.Fit ?? "fill";

        _browserImageFitCombo.Items.Clear();
        _browserImageFitCombo.Items.AddRange(
        [
            new ImageFitItem("fill", AppText.Get("Tab_BrowserFit_Fill")),
            new ImageFitItem("cover", AppText.Get("Tab_BrowserFit_Cover")),
            new ImageFitItem("contain", AppText.Get("Tab_BrowserFit_Contain")),
        ]);

        _browserImageFitCombo.SelectedItem = _browserImageFitCombo.Items.Cast<ImageFitItem>()
            .FirstOrDefault(item => item.Fit == selectedFit)
            ?? _browserImageFitCombo.Items.Cast<ImageFitItem>().First(item => item.Fit == "fill");
    }

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
