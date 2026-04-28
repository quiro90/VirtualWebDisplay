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
    private bool? _forcedEnabledState;
    private readonly Label _portLabel;
    private readonly Label _methodLabel;
    private readonly Label _placementLabel;
    private readonly ThemedComboBox _placementCombo;
    private readonly ThemedNumericUpDown _portInput;
    private readonly ThemedComboBox _transmissionMethodCombo;
    private readonly Label _captureIntervalLabel;
    private readonly ThemedNumericUpDown _captureIntervalInput;
    private readonly Label _qualityLabel;
    private readonly ThemedTrackBar _jpegQualitySlider;
    private readonly Label _jpegQualityValueLabel;
    private readonly Label _fitLabel;
    private readonly ThemedComboBox _browserImageFitCombo;
    private readonly Label _maxViewersLabel;
    private readonly ThemedNumericUpDown _maxViewersInput;
    private readonly Label _touchInputLabel;
    private readonly CheckBox _touchInputCheckBox;
    private readonly Label _touchGestureLabel;
    private readonly ThemedNumericUpDown _touchGestureInput;
    private readonly Label _touchGestureSuffixLabel;
    private readonly CheckBox _screenSecurityCheckBox;
    private readonly TextBox _screenSecurityCodeTextBox;
    private readonly Button _screenSecurityCodeToggleButton;
    private readonly ToolTip _helpToolTip;
    private readonly Control[] _managedControls;
    private readonly string _localIp;
    private readonly LinkLabel _httpUrlLink;
    private readonly Label _accessUrlPrefixLabel;
    private readonly Button _windowsDisplayButton;
    private string _runtimeSecurityCode = string.Empty;
    private bool _showSecurityCode;
    private bool _serviceRunning;

    public event Action<bool>? TouchInputChanged;
    public event Action<int>? TouchGestureHoldDelayChanged;

    public ScreenTabControls(
        string title,
        bool allowDisable,
        bool isInitialStartup,
        VirtualScreenConfig config,
        string localIp,
        bool showEnableToggle = true)
    {
        _baseConfig = config.Clone();
        _allowDisable = allowDisable;
        _portEditable = isInitialStartup;
        _localIp = localIp;
        TabPage = new TabPage(title);
        _helpToolTip = new ToolTip
        {
            AutoPopDelay = 12000,
            InitialDelay = 300,
            ReshowDelay = 150,
            ShowAlways = true,
        };

        var currentTop = 14;
        if (allowDisable && showEnableToggle)
        {
            _enabledCheckBox = new CheckBox
            {
                Left = 14,
                Top = currentTop,
                Width = 270,
                Text = AppText.Get("Tab_EnableExperimental"),
            };
            TabPage.Controls.Add(_enabledCheckBox);
            currentTop += 28;
        }

        // Fila 1: Puerto | Transmisión | Posición
        _portLabel = CreateLabel(AppText.Get("Tab_Label_Port"), 14, currentTop);
        _portInput = new ThemedNumericUpDown
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 72,
            Minimum = 1,
            Maximum = 65535,
        };

        _methodLabel = CreateLabel(AppText.Get("Tab_Label_Transmission"), 98, currentTop);
        _transmissionMethodCombo = new ThemedComboBox
        {
            Left = 98,
            Top = currentTop + 18,
            Width = 170,
        };
        _transmissionMethodCombo.Items.AddRange(
        [
            new TransmissionMethodItem(TransmissionModeOptions.WebImage, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.WebImage)),
            new TransmissionMethodItem(TransmissionModeOptions.Rtc, TransmissionModeOptions.GetDisplayName(TransmissionModeOptions.Rtc)),
        ]);

        _placementLabel = CreateLabel(AppText.Get("Tab_Label_Placement"), 280, currentTop);
        _placementCombo = new ThemedComboBox
        {
            Left = 280,
            Top = currentTop + 18,
            Width = 180,
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

        // Fila 2: Actualizar cada (ms) | Calidad JPEG
        _captureIntervalLabel = CreateLabel(AppText.Get("Tab_Label_CaptureIntervalMs"), 14, currentTop);
        _captureIntervalInput = new ThemedNumericUpDown
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 72,
            Minimum = 1M,
            Maximum = 300M,
            DecimalPlaces = 0,
            Increment = 1M,
        };

        _qualityLabel = CreateLabel(AppText.Get("Tab_Label_JpegQuality"), 140, currentTop);
        _jpegQualitySlider = new ThemedTrackBar
        {
            Left = 140,
            Top = currentTop + 12,
            Width = 236,
            Minimum = 10,
            Maximum = 100,
            TickFrequency = 10,
            SmallChange = 5,
            LargeChange = 10,
        };
        _jpegQualityValueLabel = new Label
        {
            Left = 384,
            Top = currentTop + 20,
            Width = 50,
        };

        currentTop += 54;

        // Fila 4: Ajuste | Botón configuración Windows
        _fitLabel = CreateLabel(AppText.Get("Tab_Label_BrowserFit"), 14, currentTop);
        _browserImageFitCombo = new ThemedComboBox
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 192,
        };
        _browserImageFitCombo.Items.AddRange(
        [
            new ImageFitItem("fill", AppText.Get("Tab_BrowserFit_Fill")),
            new ImageFitItem("cover", AppText.Get("Tab_BrowserFit_Cover")),
            new ImageFitItem("contain", AppText.Get("Tab_BrowserFit_Contain")),
        ]);

        _windowsDisplayButton = new Button
        {
            Left = 216,
            Top = currentTop + 14,
            Width = 244,
            Height = 28,
            Text = AppText.Get("Tab_Button_OpenWindowsDisplay"),
        };
        _windowsDisplayButton.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true }); }
            catch { }
        };

        currentTop += 54;

        // Fila 5: Numero de receptores permitidos
        _maxViewersLabel = CreateLabel(AppText.Get("Tab_Label_MaxViewers"), 14, currentTop + 3);
        _maxViewersInput = new ThemedNumericUpDown
        {
            Left = 248,
            Top = currentTop,
            Width = 62,
            Minimum = 0,
            Maximum = 99,
        };

        currentTop += 28;

        // Fila 6: Checkbox Táctil/Normal + Gestos (ms) alineados
        _touchInputCheckBox = new CheckBox
        {
            Left = 300,
            Top = currentTop + 1,
            Width = 70,
            Text = "Normal",
        };
        _touchGestureLabel = CreateLabel(AppText.Get("Tab_Label_TouchGestures"), 380, currentTop + 3);
        _touchGestureInput = new ThemedNumericUpDown
        {
            Left = 446,
            Top = currentTop,
            Width = 44,
            Minimum = TouchGestureOptions.MinHoldDelayMs,
            Maximum = TouchGestureOptions.MaxHoldDelayMs,
            DecimalPlaces = 0,
            Increment = 10M,
        };
        _touchGestureSuffixLabel = CreateLabel("ms", 492, currentTop + 3);
        currentTop += 28;

        _screenSecurityCheckBox = new CheckBox
        {
            Left = 14,
            Top = currentTop,
            Width = 380,
            Text = AppText.Get("Tab_Label_ScreenSecurity"),
        };

        currentTop += 24;

        _screenSecurityCodeTextBox = new TextBox
        {
            Left = 34,
            Top = currentTop,
            Width = 200,
            ReadOnly = true,
            PlaceholderText = AppText.Get("Tab_SecurityCode_Pending"),
        };

        _screenSecurityCodeToggleButton = new Button
        {
            Left = 240,
            Top = currentTop - 1,
            Width = 32,
            Height = 24,
            Text = "👁",
        };
        _screenSecurityCodeToggleButton.Click += (_, _) =>
        {
            _showSecurityCode = !_showSecurityCode;
            UpdateState();
        };

        currentTop += 30;

        _httpUrlLink = new LinkLabel
        {
            Left = 86,
            Top = currentTop + 1,
            Width = 366,
            AutoSize = false,
            Text = $"http://{_localIp}:{config.Port}",
        };

        _accessUrlPrefixLabel = CreateLabel(AppText.Get("Tab_AccessUrlPrefix"), 14, currentTop + 3);

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
            _fitLabel,
            _browserImageFitCombo,
            _maxViewersLabel,
            _maxViewersInput,
            _touchInputCheckBox,
            _touchGestureLabel,
            _touchGestureInput,
            _touchGestureSuffixLabel,
            _screenSecurityCheckBox,
            _screenSecurityCodeTextBox,
            _screenSecurityCodeToggleButton,
            _accessUrlPrefixLabel,
            _windowsDisplayButton,
        ];

        TabPage.Controls.AddRange(_managedControls);
        TabPage.Controls.Add(_httpUrlLink);

        _httpUrlLink.LinkClicked += (_, _) => OpenUrl(_httpUrlLink.Text);

        Initialize(config);
        ApplyHelpTooltips();

        if (_enabledCheckBox is not null)
            _enabledCheckBox.CheckedChanged += (_, _) => UpdateState();
        _placementCombo.SelectedIndexChanged += (_, _) => UpdateState();
        _portInput.ValueChanged += (_, _) => UpdateState();
        _transmissionMethodCombo.SelectedIndexChanged += (_, _) => UpdateState();
        _captureIntervalInput.ValueChanged += (_, _) => UpdateState();
        _jpegQualitySlider.ValueChanged += (_, _) => UpdateState();
        _browserImageFitCombo.SelectedIndexChanged += (_, _) => UpdateState();
        _maxViewersInput.ValueChanged += (_, _) => UpdateState();
        _touchInputCheckBox.CheckedChanged += (_, _) =>
        {
            UpdateState();
            TouchInputChanged?.Invoke(_touchInputCheckBox.Checked);
        };
        _touchGestureInput.ValueChanged += (_, _) =>
        {
            UpdateState();
            TouchGestureHoldDelayChanged?.Invoke((int)_touchGestureInput.Value);
        };
        _screenSecurityCheckBox.CheckedChanged += (_, _) => UpdateState();
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
        _captureIntervalLabel.Text = AppText.Get("Tab_Label_CaptureIntervalMs");
        _qualityLabel.Text = AppText.Get("Tab_Label_JpegQuality");
        _fitLabel.Text = AppText.Get("Tab_Label_BrowserFit");
        _maxViewersLabel.Text = AppText.Get("Tab_Label_MaxViewers");
        // _touchInputLabel eliminado
        _touchGestureLabel.Text = AppText.Get("Tab_Label_TouchGestures");
        _accessUrlPrefixLabel.Text = AppText.Get("Tab_AccessUrlPrefix");
        _screenSecurityCheckBox.Text = AppText.Get("Tab_Label_ScreenSecurity");
        _windowsDisplayButton.Text = AppText.Get("Tab_Button_OpenWindowsDisplay");
        _screenSecurityCodeTextBox.PlaceholderText = AppText.Get("Tab_SecurityCode_Pending");
        UpdateTouchInputToggleText();

        RefreshTransmissionOptions();
        RefreshPlacementOptions();
        RefreshBrowserFitOptions();
        ApplyHelpTooltips();
        UpdateState();
    }

    public void SetRuntimeSecurityCode(string? securityCode)
    {
        _runtimeSecurityCode = (securityCode ?? string.Empty).Trim().ToUpperInvariant();
        _showSecurityCode = false;
        UpdateState();
    }

    public VirtualScreenConfig BuildConfig(bool alwaysEnabled)
    {
        var config = _baseConfig.Clone();
        config.Enabled = alwaysEnabled || IsTabEnabled();
        config.Port = (int)_portInput.Value;
        config.TransmissionMethod = ((TransmissionMethodItem)_transmissionMethodCombo.SelectedItem!).Method;
        config.CaptureIntervalSeconds = (double)_captureIntervalInput.Value / 1000.0;
        config.JpegQuality = _jpegQualitySlider.Value;
        config.MaxViewers = (int)_maxViewersInput.Value;
        config.TouchInputEnabled = _touchInputCheckBox.Checked;
        config.TouchGestureHoldDelayMs = (int)_touchGestureInput.Value;
        config.ScreenSecurityEnabled = _screenSecurityCheckBox.Checked;
        config.BrowserImageFit = ((ImageFitItem)_browserImageFitCombo.SelectedItem!).Fit;
        config.VirtualDisplayPlacement = ((PlacementItem)_placementCombo.SelectedItem!).Placement;

        TransmissionModeOptions.EnsureValidSelection(config);
        return config;
    }

    private void Initialize(VirtualScreenConfig config)
    {
        if (_enabledCheckBox is not null)
            _enabledCheckBox.Checked = config.Enabled;
        else if (_allowDisable)
            _forcedEnabledState = config.Enabled;

        _portInput.Value = Math.Max(_portInput.Minimum, Math.Min(_portInput.Maximum, config.Port));
        _captureIntervalInput.Value = Math.Clamp((decimal)(config.CaptureIntervalSeconds * 1000), _captureIntervalInput.Minimum, _captureIntervalInput.Maximum);
        _jpegQualitySlider.Value = Math.Clamp(config.JpegQuality, _jpegQualitySlider.Minimum, _jpegQualitySlider.Maximum);
        _maxViewersInput.Value = Math.Clamp(config.MaxViewers, 0, 99);
        _touchInputCheckBox.Checked = config.TouchInputEnabled;
        _touchGestureInput.Value = Math.Clamp(config.TouchGestureHoldDelayMs, _touchGestureInput.Minimum, _touchGestureInput.Maximum);
        _screenSecurityCheckBox.Checked = config.ScreenSecurityEnabled;

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
        var enabled = IsTabEnabled();
        foreach (var control in _managedControls)
        {
            if (_serviceRunning)
            {
                // Mientras el servicio corre, bloquea configuración pesada pero permite ajustes en caliente
                // de touch/gestos y ver/copiar el código activo.
                control.Enabled = control == _windowsDisplayButton
                    || control == _touchInputCheckBox
                    || control == _touchGestureInput
                    || control == _touchGestureLabel
                    || control == _touchGestureSuffixLabel
                    || control == _screenSecurityCodeTextBox
                    || control == _screenSecurityCodeToggleButton;
            }
            else
            {
                control.Enabled = enabled && (control != _portInput || _portEditable);
            }
        }

        _jpegQualityValueLabel.Text = $"{_jpegQualitySlider.Value}%";
        UpdateTouchInputToggleText();

        var port = (int)_portInput.Value;
        _httpUrlLink.Text = $"http://{_localIp}:{port}";

        UpdateSecurityCodePreview(enabled);
    }

    public void SetEnabledState(bool enabled)
    {
        if (!_allowDisable)
            return;

        _forcedEnabledState = enabled;
        if (_enabledCheckBox is not null && _enabledCheckBox.Checked != enabled)
            _enabledCheckBox.Checked = enabled;
        else
            UpdateState();
    }

    /// <summary>
    /// Bloquea o desbloquea los controles de configuración según si el servicio está activo.
    /// Mientras el servicio corre, solo el botón de configuración de Windows permanece habilitado.
    /// </summary>
    public void SetServiceRunning(bool running)
    {
        _serviceRunning = running;
        UpdateState();
    }

    public bool IsEnabledState() => IsTabEnabled();

    private bool IsTabEnabled()
    {
        if (!_allowDisable)
            return true;

        return _forcedEnabledState
            ?? (_enabledCheckBox?.Checked == true);
    }

    private void UpdateSecurityCodePreview(bool tabEnabled)
    {
        if (!tabEnabled || !_screenSecurityCheckBox.Checked)
        {
            DisableSecurityCodePreview(AppText.Get("Tab_SecurityCode_Disabled"));
            return;
        }

        if (string.IsNullOrWhiteSpace(_runtimeSecurityCode))
        {
            DisableSecurityCodePreview(AppText.Get("Tab_SecurityCode_Pending"));
            return;
        }

        _screenSecurityCodeTextBox.Text = _runtimeSecurityCode;
        _screenSecurityCodeTextBox.UseSystemPasswordChar = !_showSecurityCode;
        _screenSecurityCodeToggleButton.Enabled = true;
        _screenSecurityCodeToggleButton.Text = _showSecurityCode ? "🙈" : "👁";
    }

    private void DisableSecurityCodePreview(string text)
    {
        _showSecurityCode = false;
        _screenSecurityCodeTextBox.UseSystemPasswordChar = false;
        _screenSecurityCodeTextBox.Text = text;
        _screenSecurityCodeToggleButton.Enabled = false;
        _screenSecurityCodeToggleButton.Text = "👁";
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private void ApplyHelpTooltips()
    {
        SetHelpToolTip(AppText.Get("Tab_Help_Port"), _portLabel, _portInput);
        SetHelpToolTip(AppText.Get("Tab_Help_Transmission"), _methodLabel, _transmissionMethodCombo);
        SetHelpToolTip(AppText.Get("Tab_Help_Placement"), _placementLabel, _placementCombo);
        SetHelpToolTip(AppText.Get("Tab_Help_CaptureInterval"), _captureIntervalLabel, _captureIntervalInput);
        SetHelpToolTip(AppText.Get("Tab_Help_MaxViewers"), _maxViewersLabel, _maxViewersInput);
        SetHelpToolTip(AppText.Get("Tab_Help_TouchGestures"), _touchGestureLabel, _touchGestureInput, _touchGestureSuffixLabel);
        SetHelpToolTip(AppText.Get("Tab_Help_ScreenSecurity"), _screenSecurityCheckBox, _screenSecurityCodeTextBox, _screenSecurityCodeToggleButton);
        SetHelpToolTip(AppText.Get("Tab_Help_AccessUrl"), _accessUrlPrefixLabel, _httpUrlLink);
    }

    private void UpdateTouchInputToggleText()
    {
        _touchInputCheckBox.Text = _touchInputCheckBox.Checked ? "Táctil" : "Normal";
    }

    private void SetHelpToolTip(string text, params Control[] controls)
    {
        foreach (var control in controls)
            _helpToolTip.SetToolTip(control, text);
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

    private sealed record PlacementItem(string Placement, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record ImageFitItem(string Fit, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
