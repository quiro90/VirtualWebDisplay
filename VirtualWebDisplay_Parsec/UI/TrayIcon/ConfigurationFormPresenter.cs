using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Forms;

namespace VirtualWebDisplay.UI.TrayIcon;

/// <summary>
/// Manages the presentation of the configuration form from the tray.
/// Encapsulates creation, opening and lifecycle of <see cref="ResolutionConfigurationForm"/>.
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
    /// Creates and shows the initial configuration form.
    /// The <paramref name="onConfirmed"/> and <paramref name="onCancelled"/> callbacks
    /// are invoked from the UI thread when the user decides.
    /// Blocking until the decision is the caller's responsibility.
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
    /// Shows the configuration dialog. If the startup form is already open, brings it to front.
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

    /// <summary>Raised when the form requests the service to stop.</summary>
    internal event Action? StopRequested;

    /// <summary>Raised when the form confirms service startup.</summary>
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
