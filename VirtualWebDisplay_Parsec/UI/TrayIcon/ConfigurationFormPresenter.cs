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

        if (_configForm is not null && !_configForm.IsDisposed)
        {
            _configForm.BringToFront();
            _configForm.Activate();
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
            _configForm?.Dispose();
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
        form.Screen1TouchInputChange += enabled => ApplyTouchInputChange("screen1", enabled);
        form.Screen2TouchInputChange += enabled => ApplyTouchInputChange("screen2", enabled);
        form.Screen1TouchZoomChanged += (enabled, delay) => ApplyTouchGestureChange("screen1", "zoom", enabled, delay);
        form.Screen1TouchHoldChanged += (enabled, delay) => ApplyTouchGestureChange("screen1", "hold", enabled, delay);
        form.Screen1TouchScrollChanged += (enabled, delay) => ApplyTouchGestureChange("screen1", "scroll", enabled, delay);
        form.Screen1TouchPreserveCursorChanged += (enabled) => ApplyScreenPropertyChange("screen1", screen => screen.TouchPreserveCursor = enabled);

        form.Screen2TouchZoomChanged += (enabled, delay) => ApplyTouchGestureChange("screen2", "zoom", enabled, delay);
        form.Screen2TouchHoldChanged += (enabled, delay) => ApplyTouchGestureChange("screen2", "hold", enabled, delay);
        form.Screen2TouchScrollChanged += (enabled, delay) => ApplyTouchGestureChange("screen2", "scroll", enabled, delay);
        form.Screen2TouchPreserveCursorChanged += (enabled) => ApplyScreenPropertyChange("screen2", screen => screen.TouchPreserveCursor = enabled);
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

    private void ApplyTouchGestureChange(string screenId, string gesture, bool enabled, int delayMs)
    {
        var clamped = TouchGestureOptions.ClampDelay(delayMs);
        ApplyScreenPropertyChange(screenId, screen =>
        {
            switch (gesture)
            {
                case "zoom":
                    screen.TouchZoomEnabled = enabled;
                    screen.TouchZoomDelayMs = clamped;
                    break;
                case "hold":
                    screen.TouchHoldEnabled = enabled;
                    screen.TouchHoldDelayMs = clamped;
                    break;
                case "scroll":
                    screen.TouchScrollEnabled = enabled;
                    screen.TouchScrollDelayMs = clamped;
                    break;
            }
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
