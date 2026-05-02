using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Helpers;

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
    private readonly CheckBox _touchInputCheckBox;
    private readonly CheckBox _touchPreserveCursorCheckBox;
    private readonly CheckBox _touchZoomCheckBox;
    private readonly CheckBox _touchHoldCheckBox;
    private readonly ThemedNumericUpDown _touchHoldDelayInput;
    private readonly CheckBox _touchScrollCheckBox;
    private readonly ThemedNumericUpDown _touchScrollDelayInput;
    private readonly Panel _touchSectionDivider;
    private readonly Panel _securitySectionDivider;
    private readonly CheckBox _screenSecurityCheckBox;
    private readonly TextBox _screenSecurityCodeTextBox;
    private readonly Button _screenSecurityCodeToggleButton;
    private readonly ToolTip _helpToolTip;
    private readonly Control[] _managedControls;
    private readonly string _localIp;
    private readonly Button _windowsDisplayButton;
    private string _runtimeSecurityCode = string.Empty;
    private bool _showSecurityCode;
    private bool _serviceRunning;

    public event Action<bool>? TouchInputChanged;
    public event Action<bool>? TouchPreserveCursorChanged;
    public event Action<bool, int>? TouchHoldChanged;
    public event Action<bool, int>? TouchScrollChanged;

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

        _fitLabel = CreateLabel(AppText.Get("Tab_Label_BrowserFit"), 280, currentTop);
        _browserImageFitCombo = new ThemedComboBox
        {
            Left = 280,
            Top = currentTop + 18,
            Width = 180,
        };

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

        // Fila: Posición | Botón configuración Windows
        _placementLabel = CreateLabel(AppText.Get("Tab_Label_Placement"), 14, currentTop);
        _placementCombo = new ThemedComboBox
        {
            Left = 14,
            Top = currentTop + 18,
            Width = 192,
        };

        _windowsDisplayButton = new Button
        {
            Left = 220,
            Top = currentTop + 14,
            Width = 260,
            Height = 28,
            Text = AppText.Get("Tab_Button_OpenWindowsDisplay"),
        };
        _windowsDisplayButton.Click += (_, _) => ShellHelper.OpenUrl("ms-settings:display");

        currentTop += 55;

        // ── Sección: Seguridad y Espectadores ──────────────────────────────
        _securitySectionDivider = new Panel
        {
            Left = 14,
            Top = currentTop,
            Width = 460,
            Height = 1,
            BorderStyle = BorderStyle.FixedSingle,
        };

        currentTop += 10;

        // Fila: Numero de receptores permitidos | Seguridad
        _maxViewersLabel = CreateLabel(AppText.Get("Tab_Label_MaxViewers"), 14, currentTop + 3);
        _maxViewersInput = new ThemedNumericUpDown
        {
            Left = 140,
            Top = currentTop+25,
            Width = 62,
            Minimum = 0,
            Maximum = 99,
        };

        _screenSecurityCheckBox = new CheckBox
        {
            Left = 220,
            Top = currentTop,
            Width = 260,
            Text = AppText.Get("Tab_Label_ScreenSecurity"),
        };

        currentTop += 28;

        _screenSecurityCodeTextBox = new TextBox
        {
            Left = 240,
            Top = currentTop,
            Width = 180,
            ReadOnly = true,
            PlaceholderText = AppText.Get("Tab_SecurityCode_Pending"),
        };

        _screenSecurityCodeToggleButton = new Button
        {
            Left = 426,
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

        currentTop += 32;

        // ── Sección operativa: Táctil/Gestos ────────────────────────────────
        _touchSectionDivider = new Panel
        {
            Left = 14,
            Top = currentTop,
            Width = 460,
            Height = 1,
            BorderStyle = BorderStyle.FixedSingle,
        };

        currentTop += 10;

        int blockLeft = 14;


        // CheckBox "Entrada Táctil"
        _touchInputCheckBox = new CheckBox
        {
            Left = blockLeft,
            Top = currentTop,
            Width = 150,
            Text = AppText.Get("Tab_Section_TouchInput"),
        };

        // CheckBox "Recordar posición del puntero"
        _touchPreserveCursorCheckBox = new CheckBox
        {
            Left = blockLeft + 160,
            Top = currentTop,
            Width = 200,
            Text = AppText.Get("Tab_TouchPreserveCursor_Checkbox"),
        };

        // Fila 2: Zoom, Hold, Scroll
        _touchZoomCheckBox = new CheckBox
        {
            Left = blockLeft,
            Top = currentTop + 24,
            Width = 140,
            Text = AppText.Get("Tab_TouchZoom_Checkbox")
        };

        _touchHoldCheckBox = new CheckBox
        {
            Left = blockLeft,
            Top = currentTop + 48,
            Width = 140,
            Text = AppText.Get("Tab_TouchHold_Checkbox")
        };

        _touchHoldDelayInput = new ThemedNumericUpDown
        {
            Left = blockLeft + 140,
            Top = currentTop + 48,
            Width = 56,
            Minimum = TouchGestureOptions.MinDelayMs,
            Maximum = TouchGestureOptions.MaxDelayMs,
            DecimalPlaces = 0,
            Increment = 10M,
        };

        _touchScrollCheckBox = new CheckBox
        {
            Left = blockLeft,
            Top = currentTop + 72,
            Width = 140,
            Text = AppText.Get("Tab_TouchScroll_Checkbox")
        };

        _touchScrollDelayInput = new ThemedNumericUpDown
        {
            Left = blockLeft + 140,
            Top = currentTop + 72,
            Width = 56,
            Minimum = TouchGestureOptions.MinDelayMs,
            Maximum = TouchGestureOptions.MaxDelayMs,
            DecimalPlaces = 0,
            Increment = 10M,
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
            _fitLabel,
            _browserImageFitCombo,
            _maxViewersLabel,
            _maxViewersInput,
            _screenSecurityCheckBox,
            _screenSecurityCodeTextBox,
            _screenSecurityCodeToggleButton,
            _windowsDisplayButton,
            _securitySectionDivider,
            _touchSectionDivider,
            _touchInputCheckBox,
            _touchPreserveCursorCheckBox,
            _touchZoomCheckBox,
            _touchHoldCheckBox,
            _touchHoldDelayInput,
            _touchScrollCheckBox,
            _touchScrollDelayInput,
        ];


        TabPage.Controls.AddRange(_managedControls);

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
            _touchPreserveCursorCheckBox.Enabled = _touchInputCheckBox.Checked;
            TouchInputChanged?.Invoke(_touchInputCheckBox.Checked);
        };
        _touchPreserveCursorCheckBox.CheckedChanged += (_, _) =>
        {
            UpdateState();
            TouchPreserveCursorChanged?.Invoke(_touchPreserveCursorCheckBox.Checked);
        };
        _touchZoomCheckBox.CheckedChanged += (_, _) =>
        {
            UpdateState();
        };

        _touchHoldCheckBox.CheckedChanged += (_, _) =>
        {
            UpdateState();
            TouchHoldChanged?.Invoke(_touchHoldCheckBox.Checked, (int)_touchHoldDelayInput.Value);
        };
        _touchHoldDelayInput.ValueChanged += (_, _) =>
        {
            UpdateState();
            TouchHoldChanged?.Invoke(_touchHoldCheckBox.Checked, (int)_touchHoldDelayInput.Value);
        };

        _touchScrollCheckBox.CheckedChanged += (_, _) =>
        {
            UpdateState();
            TouchScrollChanged?.Invoke(_touchScrollCheckBox.Checked, (int)_touchScrollDelayInput.Value);
        };
        _touchScrollDelayInput.ValueChanged += (_, _) =>
        {
            UpdateState();
            TouchScrollChanged?.Invoke(_touchScrollCheckBox.Checked, (int)_touchScrollDelayInput.Value);
        };
        _screenSecurityCheckBox.CheckedChanged += (_, _) => UpdateState();
    }

    public TabPage TabPage { get; }

    public string GetAccessUrl()
    {
        var port = (int)_portInput.Value;
        return $"http://{_localIp}:{port}";
    }

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
        _touchInputCheckBox.Text = AppText.Get("Tab_Section_TouchInput");
        _touchPreserveCursorCheckBox.Text = AppText.Get("Tab_TouchPreserveCursor_Checkbox");
        _touchZoomCheckBox.Text = AppText.Get("Tab_TouchZoom_Checkbox");
        _touchHoldCheckBox.Text = AppText.Get("Tab_TouchHold_Checkbox");
        _touchScrollCheckBox.Text = AppText.Get("Tab_TouchScroll_Checkbox");
        _screenSecurityCheckBox.Text = AppText.Get("Tab_Label_ScreenSecurity");
        _windowsDisplayButton.Text = AppText.Get("Tab_Button_OpenWindowsDisplay");
        _screenSecurityCodeTextBox.PlaceholderText = AppText.Get("Tab_SecurityCode_Pending");

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
        config.TouchPreserveCursor = _touchPreserveCursorCheckBox.Checked;

        config.TouchZoomEnabled = _touchZoomCheckBox.Checked;
        
        config.TouchHoldEnabled = _touchHoldCheckBox.Checked;
        config.TouchHoldDelayMs = (int)_touchHoldDelayInput.Value;
        
        config.TouchScrollEnabled = _touchScrollCheckBox.Checked;
        config.TouchScrollDelayMs = (int)_touchScrollDelayInput.Value;

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
        _touchPreserveCursorCheckBox.Checked = config.TouchPreserveCursor;

        _touchZoomCheckBox.Checked = config.TouchZoomEnabled;

        _touchHoldCheckBox.Checked = config.TouchHoldEnabled;
        _touchHoldDelayInput.Value = Math.Clamp(config.TouchHoldDelayMs, _touchHoldDelayInput.Minimum, _touchHoldDelayInput.Maximum);

        _touchScrollCheckBox.Checked = config.TouchScrollEnabled;
        _touchScrollDelayInput.Value = Math.Clamp(config.TouchScrollDelayMs, _touchScrollDelayInput.Minimum, _touchScrollDelayInput.Maximum);

        _screenSecurityCheckBox.Checked = config.ScreenSecurityEnabled;

        RefreshTransmissionOptions(config.TransmissionMethod);
        RefreshPlacementOptions(config.VirtualDisplayPlacement);
        RefreshBrowserFitOptions(config.BrowserImageFit);

        UpdateState();
    }

    private void UpdateState()
    {
        var enabled = IsTabEnabled();

        // Controles que permanecen habilitados durante el servicio (ajustes en caliente)
        var hotReloadControls = new Control[]
        {
            _windowsDisplayButton,
            _touchInputCheckBox,
            _touchZoomCheckBox,
            _touchHoldCheckBox,
            _touchHoldDelayInput,
            _touchScrollCheckBox,
            _touchScrollDelayInput,
            _screenSecurityCodeTextBox,
            _screenSecurityCodeToggleButton
        };

        foreach (var control in _managedControls)
        {
            if (_serviceRunning)
            {
                // Mientras el servicio corre, bloquea configuración pesada pero permite ajustes en caliente
                control.Enabled = hotReloadControls.Contains(control);
            }
            else
            {
                control.Enabled = enabled && (control != _portInput || _portEditable);
            }
        }

        _jpegQualityValueLabel.Text = $"{_jpegQualitySlider.Value}%";

        UpdateSecurityCodePreview(enabled);
        UpdateTouchDependentControls();
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

    private void ApplyHelpTooltips()
    {
        SetHelpToolTip(AppText.Get("Tab_Help_Port"), _portLabel, _portInput);
        SetHelpToolTip(AppText.Get("Tab_Help_Transmission"), _methodLabel, _transmissionMethodCombo);
        SetHelpToolTip(AppText.Get("Tab_Help_Placement"), _placementLabel, _placementCombo);
        SetHelpToolTip(AppText.Get("Tab_Help_CaptureInterval"), _captureIntervalLabel, _captureIntervalInput);
        SetHelpToolTip(AppText.Get("Tab_Help_MaxViewers"), _maxViewersLabel, _maxViewersInput);
        SetHelpToolTip(AppText.Get("Tab_TouchPreserveCursor_Help"), _touchPreserveCursorCheckBox);
        SetHelpToolTip(AppText.Get("Tab_Help_ScreenSecurity"), _screenSecurityCheckBox, _screenSecurityCodeTextBox, _screenSecurityCodeToggleButton);
        SetHelpToolTip(AppText.Get("Tab_Help_TouchGestureDelay"), _touchHoldDelayInput, _touchScrollDelayInput);
    }

    /// <summary>
    /// Actualiza el estado de habilitación de los controles dependientes de touch.
    /// Master switch: si TouchInput está desmarcado, deshabilita todos los sub-controles.
    /// </summary>
    private void UpdateTouchDependentControls()
    {
        var touchEnabled = _touchInputCheckBox.Checked;
        var tabEnabled = IsTabEnabled();

        // Si el touch está deshabilitado, deshabilitar todos los sub-controles
        // Pero solo si el servicio está corriendo o la tab está habilitada
        var baseTouchState = _serviceRunning ? touchEnabled : (tabEnabled && touchEnabled);

        _touchPreserveCursorCheckBox.Enabled = baseTouchState;
        
        _touchZoomCheckBox.Enabled = baseTouchState;
        
        _touchHoldCheckBox.Enabled = baseTouchState;
        _touchHoldDelayInput.Enabled = baseTouchState && _touchHoldCheckBox.Checked;
        
        _touchScrollCheckBox.Enabled = baseTouchState;
        _touchScrollDelayInput.Enabled = baseTouchState && _touchScrollCheckBox.Checked;
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

    private void RefreshTransmissionOptions(string? defaultMethod = null)
    {
        var selectedMethod = defaultMethod
            ?? (_transmissionMethodCombo.SelectedItem as TransmissionMethodItem)?.Method
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

    private void RefreshPlacementOptions(string? defaultPlacement = null)
    {
        var selectedPlacement = defaultPlacement
            ?? (_placementCombo.SelectedItem as PlacementItem)?.Placement
            ?? "windows_managed";

        _placementCombo.Items.Clear();
        _placementCombo.Items.AddRange(
        [
            new PlacementItem("windows_managed", AppText.Get("Tab_Placement_WindowsManaged")),
            new PlacementItem(VirtualDisplayPlacementOptions.Duplicate, AppText.Get("Tab_Placement_Duplicate")),
        ]);

        _placementCombo.SelectedItem = _placementCombo.Items.Cast<PlacementItem>()
            .FirstOrDefault(item => string.Equals(item.Placement, selectedPlacement, StringComparison.OrdinalIgnoreCase))
            ?? _placementCombo.Items.Cast<PlacementItem>().First(item => item.Placement == "windows_managed");
    }

    private void RefreshBrowserFitOptions(string? defaultFit = null)
    {
        var selectedFit = defaultFit ?? (_browserImageFitCombo.SelectedItem as ImageFitItem)?.Fit ?? "fill";

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
