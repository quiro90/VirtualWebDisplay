using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Forms;

namespace VirtualWebDisplay.UI.TrayIcon;

/// <summary>
/// Gestiona la presentación del formulario de configuración desde el tray.
/// Encapsula la creación, apertura y ciclo de vida de <see cref="ResolutionConfigurationForm"/>.
/// </summary>
internal sealed class ConfigurationFormPresenter
{
    private readonly VirtualWebDisplaySettings    _settings;
    private readonly VirtualScreenSettingsStore   _settingsStore;
    private readonly AppearanceSettingsStore      _appearanceStore;
    private readonly string                       _localIp;

    private ResolutionConfigurationForm? _startupForm;
    private ResolutionConfigurationForm? _configForm;

    internal ConfigurationFormPresenter(
        VirtualWebDisplaySettings settings,
        VirtualScreenSettingsStore settingsStore,
        AppearanceSettingsStore appearanceStore,
        string localIp)
    {
        _settings      = settings;
        _settingsStore = settingsStore;
        _appearanceStore = appearanceStore;
        _localIp       = localIp;
    }

    // ── Startup form ────────────────────────────────────────────────────────

    /// <summary>
    /// Crea y muestra el formulario de configuración inicial.
    /// Los callbacks <paramref name="onConfirmed"/> y <paramref name="onCancelled"/>
    /// se invocan desde el hilo de UI cuando el usuario decide.
    /// El bloqueo hasta la decisión es responsabilidad del caller.
    /// </summary>
    internal void OpenStartupForm(Action onConfirmed, Action onCancelled)
    {
        _startupForm = CreateForm(isInitialStartup: true, hasStarted: false, screenRuntimes: null);

        _startupForm.FormClosed += (_, _) =>
        {
            if (!_startupForm.WasStarted)
            {
                _startupForm = null;
                onCancelled();
            }
        };

        _startupForm.StartupConfirmed += onConfirmed;
        _startupForm.StopRequested    += () => { };   // no-op en startup

        _startupForm.Show();
    }

    internal void ClearStartupForm() => _startupForm = null;

    // ── Config dialog ────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra el diálogo de configuración. Si el startup form ya está abierto, lo trae al frente.
    /// </summary>
    internal void ShowConfigurationDialog(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        if (_startupForm is not null && !_startupForm.IsDisposed)
        {
            _startupForm.BringToFront();
            _startupForm.Activate();
            return;
        }

        var hasStarted = screenRuntimes.Count > 0;
        _configForm = CreateForm(isInitialStartup: false, hasStarted, hasStarted ? screenRuntimes : null);
        try
        {
            _configForm.ShowDialog();
        }
        finally
        {
            _configForm.Dispose();
            _configForm = null;
        }
    }

    // ── Service state notifications ──────────────────────────────────────────

    internal void NotifyServiceStarted(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        _startupForm?.NotifyServiceStarted(screenRuntimes);
        _configForm?.NotifyServiceStarted(screenRuntimes);
    }

    internal void NotifyServiceStopped()
    {
        _startupForm?.NotifyServiceStopped();
        _configForm?.NotifyServiceStopped();
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    private ResolutionConfigurationForm CreateForm(
        bool isInitialStartup,
        bool hasStarted,
        IReadOnlyList<ScreenRuntimeContext>? screenRuntimes)
    {
        var form = new ResolutionConfigurationForm(
            _settings, isInitialStartup, _localIp, hasStarted, screenRuntimes, _appearanceStore);

        form.ConfigurationSaved += ApplySelection;
        form.StopRequested      += () => StopRequested?.Invoke();
        form.StartupConfirmed   += () => StartupConfirmed?.Invoke();
        return form;
    }

    /// <summary>Se dispara cuando el formulario solicita detener el servicio.</summary>
    internal event Action? StopRequested;

    /// <summary>Se dispara cuando el formulario confirma inicio del servicio.</summary>
    internal event Action? StartupConfirmed;

    // ── Settings persistence ─────────────────────────────────────────────────

    private void ApplySelection(VirtualWebDisplaySettings selection)
    {
        selection.EnsureValid();
        selection.Screen1.CopyTo(_settings.Screen1);
        selection.Screen2.CopyTo(_settings.Screen2);
        _settings.UiLanguage  = selection.UiLanguage;
        _settings.WindowTheme = selection.WindowTheme;
        _settings.EnsureValid();
        _settingsStore.Save(_settings);
    }
}
