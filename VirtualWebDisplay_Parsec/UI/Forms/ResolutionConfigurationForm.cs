using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Formulario de configuración para las pantallas virtuales.
/// </summary>
public sealed class ResolutionConfigurationForm : Form
{
    private readonly ScreenTabControls _screen1Controls;
    private readonly ScreenTabControls _screen2Controls;
    private readonly Button _acceptButton;
    private readonly bool _isInitialStartup;
    private bool _wasStarted;

    public VirtualWebDisplaySettings Selection { get; private set; } = new();
    public bool WasStarted => _wasStarted;

    public event Action<VirtualWebDisplaySettings>? ConfigurationApplied;
    public event Action? RestartRequested;

    private string AcceptButtonText => _wasStarted ? "Reiniciar" : (_isInitialStartup ? "Iniciar" : "Guardar");

    public ResolutionConfigurationForm(VirtualWebDisplaySettings settings, bool isInitialStartup, string localIp, bool hasStarted = false)
    {
        _isInitialStartup = isInitialStartup;
        _wasStarted = hasStarted;
        Text = isInitialStartup ? "VirtualWebDisplay — Configuración de pantallas" : "VirtualWebDisplay — Configuración";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = isInitialStartup;
        ClientSize = new Size(520, 420);

        var workingCopy = new VirtualWebDisplaySettings
        {
            Screen1 = settings.Screen1.Clone(),
            Screen2 = settings.Screen2.Clone(),
        };
        workingCopy.EnsureValid();

        var descriptionLabel = new Label
        {
            AutoSize = false,
            Left = 18,
            Top = 14,
            Width = 484,
            Height = 44,
            Text = isInitialStartup
                ? "Configura las Pantallas."
                : "Ajusta la configuración de cada pantalla. Los cambios se guardan para el próximo inicio de la aplicación.",
        };

        var tabs = new TabControl
        {
            Left = 18,
            Top = 62,
            Width = 484,
            Height = 300,
        };

        _screen1Controls = new ScreenTabControls("Pantalla 1", allowDisable: false, isInitialStartup, workingCopy.Screen1, localIp);
        _screen2Controls = new ScreenTabControls("Pantalla 2", allowDisable: true,  isInitialStartup, workingCopy.Screen2, localIp);

        tabs.TabPages.Add(_screen1Controls.TabPage);
        tabs.TabPages.Add(_screen2Controls.TabPage);

        _acceptButton = new Button
        {
            Left = 326,
            Top = 374,
            Width = 84,
            Height = 28,
            Text = AcceptButtonText,
        };
        _acceptButton.Click += AcceptButton_Click;

        var cancelButton = new Button
        {
            Left = 418,
            Top = 374,
            Width = 84,
            Height = 28,
            Text = isInitialStartup ? "Salir" : "Cerrar",
        };
        cancelButton.Click += (_, _) => Close();

        Controls.AddRange([descriptionLabel, tabs, _acceptButton, cancelButton]);
        AcceptButton = _acceptButton;
        CancelButton = cancelButton;
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
            Screen1 = _screen1Controls.BuildConfig(alwaysEnabled: true),
            Screen2 = _screen2Controls.BuildConfig(alwaysEnabled: false),
        };

        selection.EnsureValid();

        if (selection.Screen2.Enabled && selection.Screen1.Port == selection.Screen2.Port)
        {
            MessageBox.Show(
                "La Pantalla 2 debe usar un puerto distinto al de la Pantalla 1.",
                "VirtualWebDisplay — Puerto duplicado",
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
}
