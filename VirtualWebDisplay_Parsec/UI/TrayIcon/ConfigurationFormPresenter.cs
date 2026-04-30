using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;
using VirtualWebDisplay.UI.Forms;
using VirtualWebDisplay.UI.Helpers;

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
    private readonly ServiceStateManager          _serviceState;
    private readonly string                       _localIp;

    private ResolutionConfigurationForm? _startupForm;
    private ResolutionConfigurationForm? _configForm;

    internal ConfigurationFormPresenter(
        VirtualWebDisplaySettings settings,
        VirtualScreenSettingsStore settingsStore,
        AppearanceSettingsStore appearanceStore,
        string localIp,
        ServiceStateManager serviceState)
    {
        _settings        = settings;
        _settingsStore   = settingsStore;
        _appearanceStore = appearanceStore;
        _localIp         = localIp;
        _serviceState    = serviceState;

        // Suscribirse a cambios de estado del servicio
        _serviceState.ServiceStarted += OnServiceStarted;
        _serviceState.ServiceStopped += OnServiceStopped;
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

        var hasStarted = _serviceState.IsStarted;
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

    // ── Service state notifications (privados, manejados por eventos) ────────

    private void OnServiceStarted(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        _startupForm.InvokeSafely(() => _startupForm?.NotifyServiceStarted(screenRuntimes));
        _configForm.InvokeSafely(() => _configForm?.NotifyServiceStarted(screenRuntimes));
    }

    private void OnServiceStopped()
    {
        _startupForm.InvokeSafely(() => _startupForm?.NotifyServiceStopped());
        _configForm.InvokeSafely(() => _configForm?.NotifyServiceStopped());
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
        form.Screen1TouchInputChanged += enabled => ApplyTouchInputChange("screen1", enabled);
        form.Screen2TouchInputChanged += enabled => ApplyTouchInputChange("screen2", enabled);
        form.Screen1TouchGestureHoldDelayChanged += value => ApplyTouchGestureHoldDelayChange("screen1", value);
        form.Screen2TouchGestureHoldDelayChanged += value => ApplyTouchGestureHoldDelayChange("screen2", value);
        form.Screen1TouchModeChanged += (preserveCursor, gesturesEnabled) => ApplyTouchModeChange("screen1", preserveCursor, gesturesEnabled);
        form.Screen2TouchModeChanged += (preserveCursor, gesturesEnabled) => ApplyTouchModeChange("screen2", preserveCursor, gesturesEnabled);
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

    private void ApplyTouchInputChange(string screenId, bool enabled)
    {
        ApplyScreenPropertyChange(screenId, screen => screen.TouchInputEnabled = enabled);
    }

    private void ApplyTouchGestureHoldDelayChange(string screenId, int holdDelayMs)
    {
        var clamped = TouchGestureOptions.ClampHoldDelay(holdDelayMs);
        ApplyScreenPropertyChange(screenId, screen => screen.TouchGestureHoldDelayMs = clamped);
    }

    private void ApplyTouchModeChange(string screenId, bool preserveCursor, bool gesturesEnabled)
    {
        ApplyScreenPropertyChange(screenId, screen =>
        {
            screen.TouchPreserveCursor = preserveCursor;
            screen.TouchGesturesEnabled = gesturesEnabled;
        });
    }

    /// <summary>
    /// Método genérico para aplicar cambios a propiedades de las pantallas virtuales.
    /// Evita duplicación de la lógica de resolución de screenId y guardado.
    /// </summary>
    private void ApplyScreenPropertyChange(string screenId, Action<VirtualScreenConfig> applyChange)
    {
        VirtualScreenConfig? targetScreen = screenId.ToLowerInvariant() switch
        {
            "screen1" => _settings.Screen1,
            "screen2" => _settings.Screen2,
            _ => null
        };

        if (targetScreen is null)
            return;

        applyChange(targetScreen);
        _settingsStore.Save(_settings);
    }
}
