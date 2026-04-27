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
    private readonly ToolStripMenuItem _aboutMenuItem;
    private readonly bool _isInitialStartup;
    private readonly AppearanceSettingsStore? _appearanceStore;

    private string _selectedLanguageCode;
    private string _selectedWindowTheme;
    private bool _wasStarted;
    private bool _serviceActionPending;
    private bool _pendingStartAction;

    public VirtualWebDisplaySettings Selection { get; private set; } = new();
    public bool WasStarted => _wasStarted;

    public event Action<VirtualWebDisplaySettings>? ConfigurationSaved;
    public event Action? StartupConfirmed;
    public event Action? StopRequested;

    private string AcceptButtonText => _wasStarted
        ? (_serviceActionPending ? AppText.Get("Form_Config_Accept_Stopping") : AppText.Get("Form_Config_Accept_Stop"))
        : (_serviceActionPending && _pendingStartAction ? AppText.Get("Form_Config_Accept_Starting") : AppText.Get("Form_Config_Accept_Start"));

    public ResolutionConfigurationForm(
        VirtualWebDisplaySettings settings,
        bool isInitialStartup,
        string localIp,
        bool hasStarted = false,
        IReadOnlyList<ScreenRuntimeContext>? screenRuntimes = null,
        AppearanceSettingsStore? appearanceStore = null)
    {
        _isInitialStartup = isInitialStartup;
        _wasStarted = hasStarted;
        _appearanceStore = appearanceStore;

        var uiFont = TryCreateUiFont();
        if (uiFont is not null)
            Font = uiFont;

        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = isInitialStartup;
        ClientSize = new Size(540, 550);

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
        _aboutMenuItem = new ToolStripMenuItem(AppText.Get("Form_Config_Menu_About"));
        _aboutMenuItem.Click += (_, _) => ShowAboutDialog();
        _configurationMenu.Items.AddRange([_languageMenuItem, _windowStyleMenuItem, _configurationMenuSeparator, _aboutMenuItem]);

        _configurationButton = new Button
        {
            Width = 36,
            Height = 30,
            Left = ClientSize.Width - 84,
            Top = 8,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Text = "⚙",
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
            Left = 18,
            Top = 86,
            Width = 504,
            Height = 390,
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
            Left   = 346,
            Top    = 498,
            Width  = 84,
            Height = 30,
            Text   = AcceptButtonText,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        _acceptButton.Click += AcceptButton_Click;

        _cancelButton = new Button
        {
            Left   = 438,
            Top    = 498,
            Width  = 84,
            Height = 30,
            Text   = isInitialStartup ? AppText.Get("Form_Config_Cancel_Exit") : AppText.Get("Form_Config_Cancel_Close"),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        _cancelButton.Click += (_, _) => Close();

        _titleBarPanel.Controls.AddRange([_titleLabel, _configurationButton, _closeButton]);
        Controls.AddRange([_titleBarPanel, _enableScreen2Check, _tabs, _acceptButton, _cancelButton]);

        AcceptButton = _acceptButton;
        CancelButton = _cancelButton;

        _enableScreen2Check.Checked = workingCopy.Screen2.Enabled;
        _screen2Controls.SetEnabledState(_enableScreen2Check.Checked);
        _enableScreen2Check.CheckedChanged += (_, _) => _screen2Controls.SetEnabledState(_enableScreen2Check.Checked);

        ApplyLocalization();
        ApplyTheme();
        ApplyRuntimeSecurityCodes(screenRuntimes);
    }

    public void NotifyServiceStarted(IReadOnlyList<ScreenRuntimeContext>? screenRuntimes = null)
    {
        _wasStarted = true;
        _serviceActionPending = false;
        _pendingStartAction = false;
        UpdateAcceptButtonState();
        ApplyRuntimeSecurityCodes(screenRuntimes);
    }

    private void AcceptButton_Click(object? sender, EventArgs e)
    {
        if (_serviceActionPending)
            return;

        if (!TrySaveSelection())
            return;

        if (_wasStarted)
        {
            // Service is running → stop it; form stays open and NotifyServiceStopped() will flip the button.
            _serviceActionPending = true;
            _pendingStartAction = false;
            UpdateAcceptButtonState();
            StopRequested?.Invoke();
        }
        else
        {
            // Service is stopped → start it; form stays open and NotifyServiceStarted() will flip the button.
            _serviceActionPending = true;
            _pendingStartAction = true;
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
        if (_isInitialStartup && !_wasStarted && DialogResult != DialogResult.OK)
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

        BuildConfigurationMenu();
    }

    private void BuildConfigurationMenu()
    {
        _languageMenuItem.Text = AppText.Get("Form_Config_Menu_Language");
        _windowStyleMenuItem.Text = AppText.Get("Form_Config_Menu_WindowStyle");
        _aboutMenuItem.Text = AppText.Get("Form_Config_Menu_About");

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
        _wasStarted = false;
        _serviceActionPending = false;
        _pendingStartAction = false;
        UpdateAcceptButtonState();
    }

    private void UpdateAcceptButtonState()
    {
        _acceptButton.Text = AcceptButtonText;
        _acceptButton.Enabled = !_serviceActionPending;
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

    private void ShowAboutDialog()
    {
        var dark    = FormThemeApplicator.ResolveDarkMode(_selectedWindowTheme);
        var palette = dark ? ThemePalette.Dark() : ThemePalette.Light();
        AboutDialog.Show(this, palette.Background, palette.Foreground, palette.Panel, palette.Border);
    }

    private static Font? TryCreateUiFont()
    {
        try
        {
            return new Font("Segoe UI Variable Text", 9F);
        }
        catch
        {
            return null;
        }
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();
        SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);


}
