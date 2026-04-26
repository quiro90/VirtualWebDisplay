using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Formulario de configuración para las pantallas virtuales.
/// </summary>
public sealed class ResolutionConfigurationForm : Form
{
    private readonly ScreenTabControls _screen1Controls;
    private readonly ScreenTabControls _screen2Controls;
    private readonly Label _headerLabel;
    private readonly Label _languageLabel;
    private readonly TabControl _tabs;
    private readonly Button _acceptButton;
    private readonly Button _cancelButton;
    private readonly bool _isInitialStartup;
    private readonly ComboBox _languageCombo;
    private bool _isUpdatingLanguageSelection;
    private bool _wasStarted;

    public VirtualWebDisplaySettings Selection { get; private set; } = new();
    public bool WasStarted => _wasStarted;

    public event Action<VirtualWebDisplaySettings>? ConfigurationApplied;
    public event Action? RestartRequested;

    private string AcceptButtonText => _wasStarted
        ? AppText.Get("Form_Config_Accept_Restart")
        : (_isInitialStartup ? AppText.Get("Form_Config_Accept_Start") : AppText.Get("Form_Config_Accept_Save"));

    public ResolutionConfigurationForm(VirtualWebDisplaySettings settings, bool isInitialStartup, string localIp, bool hasStarted = false)
    {
        _isInitialStartup = isInitialStartup;
        _wasStarted = hasStarted;
        Text = isInitialStartup ? AppText.Get("Form_Config_TitleStartup") : AppText.Get("Form_Config_Title");
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = isInitialStartup;
        ClientSize = new Size(520, 446);

        var workingCopy = new VirtualWebDisplaySettings
        {
            UiLanguage = settings.UiLanguage,
            Screen1 = settings.Screen1.Clone(),
            Screen2 = settings.Screen2.Clone(),
        };
        workingCopy.EnsureValid();

        _headerLabel = new Label
        {
            AutoSize = true,
            Left = 18,
            Top = 16,
            Font = new Font(Font, FontStyle.Bold),
            Text = AppText.Get("Form_Config_Header"),
        };

        _languageLabel = new Label
        {
            AutoSize = true,
            Left = 300,
            Top = 16,
            Text = AppText.Get("Language_Label"),
        };

        _languageCombo = new ComboBox
        {
            Left = 358,
            Top = 12,
            Width = 144,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _languageCombo.Items.AddRange(AppText.SupportedLanguages.Select(CreateLanguageItem).ToArray<object>());
        _languageCombo.SelectedItem = _languageCombo.Items.Cast<LanguageItem>()
            .First(item => item.Code == AppText.NormalizeLanguage(workingCopy.UiLanguage));
        _languageCombo.SelectedIndexChanged += LanguageCombo_SelectedIndexChanged;

            _tabs = new TabControl
        {
            Left = 18,
            Top = 54,
            Width = 484,
            Height = 300,
        };

        _screen1Controls = new ScreenTabControls(AppText.Get("Form_Config_Tab_Screen1"), allowDisable: false, isInitialStartup, workingCopy.Screen1, localIp);
        _screen2Controls = new ScreenTabControls(AppText.Get("Form_Config_Tab_Screen2"), allowDisable: true,  isInitialStartup, workingCopy.Screen2, localIp);

        _tabs.TabPages.Add(_screen1Controls.TabPage);
        _tabs.TabPages.Add(_screen2Controls.TabPage);

        _acceptButton = new Button
        {
            Left = 326,
            Top = 364,
            Width = 84,
            Height = 28,
            Text = AcceptButtonText,
        };
        _acceptButton.Click += AcceptButton_Click;

        _cancelButton = new Button
        {
            Left = 418,
            Top = 364,
            Width = 84,
            Height = 28,
            Text = isInitialStartup ? AppText.Get("Form_Config_Cancel_Exit") : AppText.Get("Form_Config_Cancel_Close"),
        };
        _cancelButton.Click += (_, _) => Close();

        Controls.AddRange([_headerLabel, _languageLabel, _languageCombo, _tabs, _acceptButton, _cancelButton]);
        AcceptButton = _acceptButton;
        CancelButton = _cancelButton;

        ApplyLocalization();
    }

    public void NotifyStartupCompleted()
    {
        if (!_isInitialStartup || _wasStarted)
            return;

        _wasStarted = true;
        _acceptButton.Text = AcceptButtonText;
    }

    private void AcceptButton_Click(object? sender, EventArgs e)
    {
        if (!ValidateAndBuildSelection(out var selection))
            return;

        Selection = selection;
        ConfigurationApplied?.Invoke(selection);

        if (_wasStarted)
        {
            RestartRequested?.Invoke();
            if (!_isInitialStartup)
                CloseDialog();
        }
        else if (!_isInitialStartup)
        {
            CloseDialog();
        }
    }

    private bool ValidateAndBuildSelection(out VirtualWebDisplaySettings selection)
    {
        selection = new VirtualWebDisplaySettings
        {
            UiLanguage = ((LanguageItem)_languageCombo.SelectedItem!).Code,
            Screen1 = _screen1Controls.BuildConfig(alwaysEnabled: true),
            Screen2 = _screen2Controls.BuildConfig(alwaysEnabled: false),
        };

        selection.EnsureValid();

        if (selection.Screen2.Enabled && selection.Screen1.Port == selection.Screen2.Port)
        {
            MessageBox.Show(
                AppText.Get("Validation_DuplicatePort_Message"),
                AppText.Get("Validation_DuplicatePort_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private void CloseDialog()
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void LanguageCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingLanguageSelection || _languageCombo.SelectedItem is not LanguageItem languageItem)
            return;

        AppText.ApplyCulture(languageItem.Code);
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Text = _isInitialStartup ? AppText.Get("Form_Config_TitleStartup") : AppText.Get("Form_Config_Title");
        _headerLabel.Text = AppText.Get("Form_Config_Header");
        _languageLabel.Text = AppText.Get("Language_Label");

        _screen1Controls.TabPage.Text = AppText.Get("Form_Config_Tab_Screen1");
        _screen2Controls.TabPage.Text = AppText.Get("Form_Config_Tab_Screen2");
        _screen1Controls.ApplyLocalization();
        _screen2Controls.ApplyLocalization();

        _acceptButton.Text = AcceptButtonText;
        _cancelButton.Text = _isInitialStartup ? AppText.Get("Form_Config_Cancel_Exit") : AppText.Get("Form_Config_Cancel_Close");

        var selectedLanguageCode = (_languageCombo.SelectedItem as LanguageItem)?.Code
            ?? AppText.NormalizeLanguage(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

        _isUpdatingLanguageSelection = true;
        _languageCombo.Items.Clear();
        _languageCombo.Items.AddRange(AppText.SupportedLanguages.Select(CreateLanguageItem).ToArray<object>());
        _languageCombo.SelectedItem = _languageCombo.Items.Cast<LanguageItem>()
            .First(item => item.Code == AppText.NormalizeLanguage(selectedLanguageCode));
        _isUpdatingLanguageSelection = false;
    }

    private static LanguageItem CreateLanguageItem(LanguageOption option)
    {
        var displayText = option.Code == "en"
            ? AppText.Get("Language_English")
            : AppText.Get("Language_Spanish");
        return new LanguageItem(option.Code, displayText);
    }

    private sealed record LanguageItem(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
