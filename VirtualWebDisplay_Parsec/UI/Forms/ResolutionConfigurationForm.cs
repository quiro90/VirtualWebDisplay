using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Theme;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Formulario de configuración para las pantallas virtuales.
/// </summary>
public sealed class ResolutionConfigurationForm : Form
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    private readonly ScreenTabControls _screen1Controls;
    private readonly ScreenTabControls _screen2Controls;
    private readonly ModernTabControl _tabs;
    private readonly CheckBox _enableScreen2Check;
    private readonly Button _acceptButton;
    private readonly Button _cancelButton;
    private readonly Panel _titleBarPanel;
    private readonly Label _titleLabel;
    private readonly Button _configurationButton;
    private readonly Button _closeButton;
    private readonly ContextMenuStrip _configurationMenu;
    private readonly ToolStripMenuItem _languageMenuItem;
    private readonly ToolStripMenuItem _windowStyleMenuItem;
    private readonly ToolStripSeparator _configurationMenuSeparator;
    private readonly ToolStripMenuItem _customModesMenuItem;
    private readonly ToolStripMenuItem _aboutMenuItem;
    private readonly bool _isInitialStartup;
    private readonly AppearanceSettingsStore? _appearanceStore;

    private readonly Label _screen1Indicator;
    private readonly Label _screen2Indicator;
    private readonly ToolTip _screenIndicatorTooltip;

    private string _selectedLanguageCode;
    private string _selectedWindowTheme;
    private ServiceState _serviceState;

    public VirtualWebDisplaySettings Selection { get; private set; } = new();
    public bool WasStarted => _serviceState == ServiceState.Started;

    public event Action<VirtualWebDisplaySettings>? ConfigurationSaved;
    public event Action? StartupConfirmed;
    public event Action? StopRequested;
    public event Action<bool>? Screen1TouchInputChanged;
    public event Action<bool>? Screen2TouchInputChanged;
    public event Action<int>? Screen1TouchGestureHoldDelayChanged;
    public event Action<int>? Screen2TouchGestureHoldDelayChanged;
    public event Action<bool, bool>? Screen1TouchModeChanged; // (preserveCursor, gesturesEnabled)
    public event Action<bool, bool>? Screen2TouchModeChanged; // (preserveCursor, gesturesEnabled)

    private string AcceptButtonText => _serviceState switch
    {
        ServiceState.Started => AppText.Get("Form_Config_Accept_Stop"),
        ServiceState.Stopping => AppText.Get("Form_Config_Accept_Stopping"),
        ServiceState.Starting => AppText.Get("Form_Config_Accept_Starting"),
        _ => AppText.Get("Form_Config_Accept_Start")
    };

    public ResolutionConfigurationForm(
        VirtualWebDisplaySettings settings,
        bool isInitialStartup,
        string localIp,
        bool hasStarted = false,
        IReadOnlyList<ScreenRuntimeContext>? screenRuntimes = null,
        AppearanceSettingsStore? appearanceStore = null)
    {
        _isInitialStartup = isInitialStartup;
        _serviceState = hasStarted ? ServiceState.Started : ServiceState.Stopped;
        _appearanceStore = appearanceStore;

        var uiFont = FormThemeApplicator.TryCreateUiFont();
        if (uiFont is not null)
            Font = uiFont;

        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = isInitialStartup;
        ClientSize = new Size(540, 545);

        var workingCopy = new VirtualWebDisplaySettings
        {
            UiLanguage = settings.UiLanguage,
            WindowTheme = settings.WindowTheme,
            Screen1 = settings.Screen1.Clone(),
            Screen2 = settings.Screen2.Clone(),
        };
        workingCopy.EnsureValid();

        _selectedLanguageCode = AppText.NormalizeLanguage(workingCopy.UiLanguage);
        _selectedWindowTheme = WindowThemeOptions.Normalize(workingCopy.WindowTheme);

        _titleBarPanel = new Panel
        {
            Left = 0,
            Top = 0,
            Width = ClientSize.Width,
            Height = 46,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        _titleBarPanel.MouseDown += TitleBar_MouseDown;

        _titleLabel = new Label
        {
            AutoSize = true,
            Left = 14,
            Top = 14,
            Font = new Font(Font, FontStyle.Bold),
            Text = AppText.Get("Common_AppDisplayName"),
        };
        _titleLabel.MouseDown += TitleBar_MouseDown;

        _configurationMenu = new ContextMenuStrip();
        _configurationMenu.ShowImageMargin = false;
        _configurationMenu.ShowCheckMargin = false;
        _configurationMenu.Padding = Padding.Empty;
        _languageMenuItem = new ToolStripMenuItem(AppText.Get("Form_Config_Menu_Language"));
        _windowStyleMenuItem = new ToolStripMenuItem(AppText.Get("Form_Config_Menu_WindowStyle"));
        _configurationMenuSeparator = new ToolStripSeparator();
        _customModesMenuItem = new ToolStripMenuItem(AppText.Get("Form_Config_Menu_CustomModes"));
        _customModesMenuItem.Click += (_, _) => ShowCustomModesDialog();
        _aboutMenuItem = new ToolStripMenuItem(AppText.Get("Form_Config_Menu_About"));
        _aboutMenuItem.Click += (_, _) => ShowAboutDialog();
        _configurationMenu.Items.AddRange([_languageMenuItem, _windowStyleMenuItem, _configurationMenuSeparator, _customModesMenuItem, _aboutMenuItem]);

        _configurationButton = new Button
        {
            Width = 36,
            Height = 30,
            Left = ClientSize.Width - 84,
            Top = 8,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Text = "🔧",
            Font = new Font(Font.FontFamily, 12, FontStyle.Regular),
            TabStop = false,
        };
        _configurationButton.FlatAppearance.BorderSize = 1;
        _configurationButton.Click += (_, _) => _configurationMenu.Show(_configurationButton, new Point(0, _configurationButton.Height));

        _closeButton = new Button
        {
            Width = 36,
            Height = 30,
            Left = ClientSize.Width - 42,
            Top = 8,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Text = "X",
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
            TabStop = false,
        };
        _closeButton.FlatAppearance.BorderSize = 1;
        _closeButton.Click += (_, _) => Close();

        _enableScreen2Check = new CheckBox
        {
            AutoSize = true,
            Left = 18,
            Top = 58,
            Text = AppText.Get("Form_Config_EnableScreen2"),
        };

        _tabs = new ModernTabControl
        {
            Left = 10,
            Top = 86,
            Width = 520,
            Height = 408,
        };

        _screen1Controls = new ScreenTabControls(
            AppText.Get("Form_Config_Tab_Screen1"),
            allowDisable: false,
            isInitialStartup,
            workingCopy.Screen1,
            localIp);

        _screen2Controls = new ScreenTabControls(
            AppText.Get("Form_Config_Tab_Screen2"),
            allowDisable: true,
            isInitialStartup,
            workingCopy.Screen2,
            localIp,
            showEnableToggle: false);

        _tabs.TabPages.Add(_screen1Controls.TabPage);
        _tabs.TabPages.Add(_screen2Controls.TabPage);

        _acceptButton = new Button
        {
            Left   = 352,
            Top    = 503,
            Width  = 84,
            Height = 30,
            Text   = AcceptButtonText,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        _acceptButton.Click += AcceptButton_Click;

        _cancelButton = new Button
        {
            Left   = 444,
            Top    = 503,
            Width  = 84,
            Height = 30,
            Text   = isInitialStartup ? AppText.Get("Form_Config_Cancel_Exit") : AppText.Get("Form_Config_Cancel_Close"),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        _cancelButton.Click += (_, _) => Close();

        // Indicadores de pantallas (inferior izquierdo)
        _screenIndicatorTooltip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 300,
            ReshowDelay = 150,
            ShowAlways = true,
        };

        _screen1Indicator = CreateScreenIndicator(12, "1⇗: 📺", _screen1Controls);
        _screen2Indicator = CreateScreenIndicator(100, "2⇗: 📺", _screen2Controls);
        _screen2Indicator.Visible = false;

        _titleBarPanel.Controls.AddRange([_titleLabel, _configurationButton, _closeButton]);
        Controls.AddRange([_titleBarPanel, _enableScreen2Check, _tabs, _screen1Indicator, _screen2Indicator, _acceptButton, _cancelButton]);

        AcceptButton = _acceptButton;
        CancelButton = _cancelButton;

        _enableScreen2Check.Checked = workingCopy.Screen2.Enabled;
        _screen2Controls.SetEnabledState(_enableScreen2Check.Checked);

        // Los indicadores solo son visibles cuando el servicio está iniciado
        UpdateScreenIndicatorsVisibility();

        _enableScreen2Check.CheckedChanged += (_, _) =>
        {
            _screen2Controls.SetEnabledState(_enableScreen2Check.Checked);
            UpdateScreenIndicatorsVisibility();
        };
        _screen1Controls.TouchInputChanged += enabled => Screen1TouchInputChanged?.Invoke(enabled);
        _screen2Controls.TouchInputChanged += enabled => Screen2TouchInputChanged?.Invoke(enabled);
        _screen1Controls.TouchGestureHoldDelayChanged += value => Screen1TouchGestureHoldDelayChanged?.Invoke(value);
        _screen2Controls.TouchGestureHoldDelayChanged += value => Screen2TouchGestureHoldDelayChanged?.Invoke(value);
        _screen1Controls.TouchModeChanged += (preserveCursor, gesturesEnabled) => Screen1TouchModeChanged?.Invoke(preserveCursor, gesturesEnabled);
        _screen2Controls.TouchModeChanged += (preserveCursor, gesturesEnabled) => Screen2TouchModeChanged?.Invoke(preserveCursor, gesturesEnabled);

        ApplyLocalization();
        ApplyTheme();
        ApplyRuntimeSecurityCodes(screenRuntimes);
        if (_serviceState == ServiceState.Started)
            SetConfigurationControlsLocked(true);
    }

    public void NotifyServiceStarted(IReadOnlyList<ScreenRuntimeContext>? screenRuntimes = null)
    {
        _serviceState = ServiceState.Started;
        UpdateAcceptButtonState();
        ApplyRuntimeSecurityCodes(screenRuntimes);
        SetConfigurationControlsLocked(true);
        UpdateScreenIndicatorsVisibility();
    }

    private void AcceptButton_Click(object? sender, EventArgs e)
    {
        if (_serviceState is ServiceState.Starting or ServiceState.Stopping)
            return;

        if (!TrySaveSelection())
            return;

        if (_serviceState == ServiceState.Started)
        {
            // Service is running → stop it; form stays open and NotifyServiceStopped() will flip the button.
            _serviceState = ServiceState.Stopping;
            UpdateAcceptButtonState();
            StopRequested?.Invoke();
        }
        else
        {
            // Service is stopped → start it; form stays open and NotifyServiceStarted() will flip the button.
            _serviceState = ServiceState.Starting;
            UpdateAcceptButtonState();
            StartupConfirmed?.Invoke();
        }
    }

    private bool TrySaveSelection()
    {
        if (!ValidateAndBuildSelection(out var selection))
            return false;

        Selection = selection;
        ConfigurationSaved?.Invoke(selection);
        return true;
    }

    private bool ValidateAndBuildSelection(out VirtualWebDisplaySettings selection) =>
        SettingsFormValidator.TryBuild(
            _selectedLanguageCode,
            _selectedWindowTheme,
            _screen1Controls.BuildConfig(alwaysEnabled: true),
            _screen2Controls.BuildConfig(alwaysEnabled: false),
            out selection);

    private void CloseDialog()
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isInitialStartup && _serviceState == ServiceState.Stopped && DialogResult != DialogResult.OK)
        {
            TrySaveSelection();
        }

        base.OnFormClosing(e);
    }

    private void ApplyRuntimeSecurityCodes(IReadOnlyList<ScreenRuntimeContext>? screenRuntimes)
    {
        var screen1Code = screenRuntimes?
            .FirstOrDefault(runtime => string.Equals(runtime.Id, "screen1", StringComparison.OrdinalIgnoreCase))?
            .SecurityGate.AccessCode;

        var screen2Code = screenRuntimes?
            .FirstOrDefault(runtime => string.Equals(runtime.Id, "screen2", StringComparison.OrdinalIgnoreCase))?
            .SecurityGate.AccessCode;

        _screen1Controls.SetRuntimeSecurityCode(screen1Code);
        _screen2Controls.SetRuntimeSecurityCode(screen2Code);
    }

    private void ApplyLocalization()
    {
        Text = _isInitialStartup ? AppText.Get("Form_Config_TitleStartup") : AppText.Get("Form_Config_Title");
        _titleLabel.Text = AppText.Get("Common_AppDisplayName");
        _enableScreen2Check.Text = AppText.Get("Form_Config_EnableScreen2");

        _screen1Controls.TabPage.Text = AppText.Get("Form_Config_Tab_Screen1");
        _screen2Controls.TabPage.Text = AppText.Get("Form_Config_Tab_Screen2");
        _screen1Controls.ApplyLocalization();
        _screen2Controls.ApplyLocalization();

        UpdateAcceptButtonState();
        _cancelButton.Text = _isInitialStartup ? AppText.Get("Form_Config_Cancel_Exit") : AppText.Get("Form_Config_Cancel_Close");

        // Actualizar tooltips de indicadores de pantalla
        UpdateScreenIndicatorTooltip(_screen1Indicator, _screen1Controls.GetAccessUrl());
        if (_screen2Indicator.Visible)
            UpdateScreenIndicatorTooltip(_screen2Indicator, _screen2Controls.GetAccessUrl());

        BuildConfigurationMenu();
    }

    private void BuildConfigurationMenu()
    {
        _languageMenuItem.Text    = AppText.Get("Form_Config_Menu_Language");
        _windowStyleMenuItem.Text = AppText.Get("Form_Config_Menu_WindowStyle");
        _customModesMenuItem.Text = AppText.Get("Form_Config_Menu_CustomModes");
        _aboutMenuItem.Text       = AppText.Get("Form_Config_Menu_About");

        _languageMenuItem.DropDownItems.Clear();
        foreach (var language in AppText.SupportedLanguages)
        {
            var displayText = language.Code == "en"
                ? AppText.Get("Language_English")
                : AppText.Get("Language_Spanish");

            var item = new ToolStripMenuItem(displayText)
            {
                Tag = language.Code,
                Checked = language.Code == _selectedLanguageCode,
            };
            item.Click += (_, _) => SelectLanguage(language.Code);
            _languageMenuItem.DropDownItems.Add(item);
        }

        _windowStyleMenuItem.DropDownItems.Clear();
        AddThemeItem(WindowThemeOptions.System, AppText.Get("Form_Config_WindowStyle_System"));
        AddThemeItem(WindowThemeOptions.Light, AppText.Get("Form_Config_WindowStyle_Light"));
        AddThemeItem(WindowThemeOptions.Dark, AppText.Get("Form_Config_WindowStyle_Dark"));

        if (_languageMenuItem.DropDown is ToolStripDropDownMenu languageMenu)
        {
            languageMenu.ShowImageMargin = false;
            languageMenu.ShowCheckMargin = false;
            languageMenu.Padding = Padding.Empty;
        }

        if (_windowStyleMenuItem.DropDown is ToolStripDropDownMenu windowStyleMenu)
        {
            windowStyleMenu.ShowImageMargin = false;
            windowStyleMenu.ShowCheckMargin = true;
            windowStyleMenu.Padding = Padding.Empty;
        }
    }

    private void AddThemeItem(string theme, string text)
    {
        var item = new ToolStripMenuItem(text)
        {
            Tag = theme,
            Checked = _selectedWindowTheme == theme,
        };
        item.Click += (_, _) => SelectTheme(theme);
        _windowStyleMenuItem.DropDownItems.Add(item);
    }

    private void SelectLanguage(string languageCode)
    {
        _selectedLanguageCode = AppText.NormalizeLanguage(languageCode);
        AppText.ApplyCulture(_selectedLanguageCode);
        SaveAppearanceIfAvailable();
        ApplyLocalization();
    }

    private void SelectTheme(string theme)
    {
        _selectedWindowTheme = WindowThemeOptions.Normalize(theme);
        SaveAppearanceIfAvailable();
        ApplyTheme();
    }

    private void SaveAppearanceIfAvailable()
    {
        _appearanceStore?.Save(new AppearanceSettings
        {
            UiLanguage = _selectedLanguageCode,
            WindowTheme = _selectedWindowTheme,
        });
    }

    public void NotifyServiceStopped()
    {
        _serviceState = ServiceState.Stopped;
        UpdateAcceptButtonState();
        SetConfigurationControlsLocked(false);
        UpdateScreenIndicatorsVisibility();
    }

    private void SetConfigurationControlsLocked(bool locked)
    {
        _enableScreen2Check.Enabled = !locked;
        _screen1Controls.SetServiceRunning(locked);
        _screen2Controls.SetServiceRunning(locked);
    }

    private void UpdateAcceptButtonState()
    {
        _acceptButton.Text = AcceptButtonText;
        _acceptButton.Enabled = _serviceState is ServiceState.Started or ServiceState.Stopped;
    }

    private void UpdateScreenIndicatorsVisibility()
    {
        _screen1Indicator.Visible = _serviceState == ServiceState.Started;
        _screen2Indicator.Visible = _serviceState == ServiceState.Started && _enableScreen2Check.Checked;
    }

    private void ApplyTheme()
    {
        var dark    = FormThemeApplicator.ResolveDarkMode(_selectedWindowTheme);
        var palette = dark ? ThemePalette.Dark() : ThemePalette.Light();

        BackColor = palette.Background;
        ForeColor = palette.Foreground;

        _titleBarPanel.BackColor = palette.TitleBackground;
        _titleLabel.ForeColor    = palette.TitleForeground;

        FormThemeApplicator.ApplyThemeRecursive(this, palette);
        FormThemeApplicator.StyleTitleButton(_configurationButton, palette);
        FormThemeApplicator.StyleTitleButton(_closeButton, palette);

        // Aplicar tema a indicadores de pantalla
        _screen1Indicator.ForeColor = palette.Link;
        _screen2Indicator.ForeColor = palette.Link;

        _tabs.ApplyPalette(
            tabBackground:         palette.Button,
            tabSelectedBackground: palette.TitleButton,
            tabForeground:         palette.Foreground,
            tabSelectedForeground: palette.TitleForeground,
            tabBorder:             palette.Border,
            pageBackground:        palette.Panel);

        FormThemeApplicator.ApplyThemeToMenu(_configurationMenu, palette);
        Invalidate();
    }

    private void ShowCustomModesDialog()
    {
        using var dlg = new CustomModesDialog(_selectedWindowTheme);
        dlg.ShowDialog(this);
    }

    private void ShowAboutDialog()
    {
        var dark    = FormThemeApplicator.ResolveDarkMode(_selectedWindowTheme);
        var palette = dark ? ThemePalette.Dark() : ThemePalette.Light();
        AboutDialog.Show(this, palette.Background, palette.Foreground, palette.Panel, palette.Border);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();
        SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
    }

    private Label CreateScreenIndicator(int left, string text, ScreenTabControls screenControls)
    {
        var indicator = new Label
        {
            Left = left,
            Top = 503,
            Width = 80,
            Height = 30,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Tag = screenControls, // Guardar referencia al control de pantalla
        };

        indicator.Click += ScreenIndicator_Click;
        indicator.MouseEnter += (_, _) => UpdateScreenIndicatorTooltip(indicator, screenControls.GetAccessUrl());

        return indicator;
    }

    private void ScreenIndicator_Click(object? sender, EventArgs e)
    {
        if (sender is not Label label || e is not MouseEventArgs mouseArgs || label.Tag is not ScreenTabControls screenControls)
            return;

        var url = screenControls.GetAccessUrl();

        // Click en el emoji de pantalla (📺) → copiar URL
        if (mouseArgs.X > 30)
        {
            Clipboard.SetText(url);
            _screenIndicatorTooltip.Show(AppText.Get("Form_Config_ScreenIndicator_UrlCopied"), label, 1000);
        }
        // Click en número o flecha → abrir navegador
        else
        {
            OpenUrl(url);
        }
    }

    private void UpdateScreenIndicatorTooltip(Label indicator, string url)
    {
        _screenIndicatorTooltip.SetToolTip(indicator, AppText.Format("Form_Config_ScreenIndicator_Tooltip", url));
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);


}
