using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.UI.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.TrayIcon;

/// <summary>
/// Controlador del ícono de bandeja (system tray) para VirtualWebDisplay.
/// </summary>
public sealed class VirtualDisplayTrayController : IDisposable
{
    private readonly VirtualWebDisplaySettings _settings;
    private readonly VirtualScreenSettingsStore _settingsStore;
    private readonly AppearanceSettingsStore _appearanceStore;
    private readonly string _localIp;
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new(false);

    private ApplicationContext? _context;
    private Control? _invoker;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private Action? _exitRequested;
    private Action? _stopRequested;
    private TaskCompletionSource<bool>? _serviceStartSignal;
    private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes = [];
    private bool _serviceActionPending;
    private bool _disposed;
    private Icon? _appIcon;
    private ResolutionConfigurationForm? _startupForm;
    private ResolutionConfigurationForm? _configForm;

    private static Icon LoadAppIcon()
    {
        var stream = typeof(VirtualDisplayTrayController).Assembly
            .GetManifestResourceStream("VirtualWebDisplay.app.ico");
        return stream is not null ? new Icon(stream) : SystemIcons.Application;
    }

    public VirtualDisplayTrayController(VirtualWebDisplaySettings settings, VirtualScreenSettingsStore settingsStore, AppearanceSettingsStore appearanceStore, string localIp)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _appearanceStore = appearanceStore;
        _localIp = localIp;
        _uiThread = new Thread(RunUiThread)
        {
            IsBackground = true,
            Name = "VirtualWebDisplayTray",
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _ready.Wait();
    }

    public bool ShowStartupConfiguration()
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        PostToUi(() =>
        {
            _startupForm = CreateConfigurationForm(isInitialStartup: true, hasStarted: false);

            _startupForm.FormClosed += (_, _) =>
            {
                if (_startupForm.WasStarted)
                {
                    _startupForm = null;
                }
                else
                {
                    completion.TrySetResult(false);
                    _context?.ExitThread();
                }
            };

            _startupForm.StartupConfirmed += () => completion.TrySetResult(true);

            _startupForm.Show();
        });

        return completion.Task.GetAwaiter().GetResult();
    }

    public void ConfigureRuntimeActions(Action exitRequested, Action stopRequested, IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        _exitRequested = exitRequested;
        _stopRequested = stopRequested;
        _serviceStartSignal = null; // service is now running
        _serviceActionPending = false;
        _screenRuntimes = screenRuntimes;

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _contextMenu?.Dispose();
            _contextMenu = BuildContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;

            _startupForm?.NotifyServiceStarted(_screenRuntimes);
            _configForm?.NotifyServiceStarted(_screenRuntimes);
        });

        var summary = string.Join(" | ", _screenRuntimes.Select(runtime => $"{runtime.DisplayName}: {runtime.HostUrl}"));
        UpdateStatus(summary);

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _notifyIcon.BalloonTipTitle = AppText.Get("Tray_BalloonTitle");
            _notifyIcon.BalloonTipText = string.Join("\n", _screenRuntimes.Select(runtime =>
            {
                var locationText = string.Equals(runtime.HostUrl, runtime.IpUrl, StringComparison.OrdinalIgnoreCase)
                    ? $"{runtime.DisplayName}: {runtime.HostUrl}"
                    : $"{runtime.DisplayName}: {runtime.HostUrl} | {runtime.IpUrl}";

                if (!runtime.SecurityGate.Enabled)
                    return locationText;

                return $"{locationText} | {AppText.Get("Tray_SecurityCode_Label")}: {runtime.SecurityGate.AccessCode}";
            }));
            _notifyIcon.ShowBalloonTip(5000);
        });
    }

    public void UpdateStatus(string status)
    {
        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _notifyIcon.Text = TrimTrayText(AppText.Format("Tray_Status_Format", status));
        });
    }

    private void RunUiThread()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        _context = new ApplicationContext();
        _invoker = new Control();
        _invoker.CreateControl();

        _appIcon = LoadAppIcon();
        _contextMenu = BuildContextMenu();
        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = TrimTrayText(AppText.Get("Common_AppName")),
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowConfigurationDialog();
        _ready.Set();

        Application.Run(_context);

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu?.Dispose();
        _appIcon?.Dispose();
        _invoker.Dispose();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(AppText.Get("Tray_Menu_Configuration"), null, (_, _) => ShowConfigurationDialog());

        if (_screenRuntimes.Count > 0)
        {
            foreach (var runtime in _screenRuntimes)
                menu.Items.Add(AppText.Format("Tray_Menu_OpenDisplay", runtime.DisplayName), null, (_, _) => OpenStreamUrl(runtime.HostUrl));

            menu.Items.Add(new ToolStripSeparator());
        }

        menu.Items.Add(new ToolStripSeparator());
        if (_screenRuntimes.Count > 0 && !_serviceActionPending)
            menu.Items.Add(AppText.Get("Tray_Menu_Stop"), null, (_, _) => StopService());
        else if (_screenRuntimes.Count == 0 && _serviceStartSignal is not null && !_serviceActionPending)
            menu.Items.Add(AppText.Get("Tray_Menu_Start"), null, (_, _) => StartService());

        menu.Items.Add(AppText.Get("Tray_Menu_Exit"), null, (_, _) => ExitApplication());
        return menu;
    }

    private void ShowConfigurationDialog()
    {
        if (_startupForm is not null && !_startupForm.IsDisposed)
        {
            _startupForm.BringToFront();
            _startupForm.Activate();
            return;
        }

        var hasStarted = _screenRuntimes.Count > 0;
        _configForm = CreateConfigurationForm(isInitialStartup: false, hasStarted);
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

    private ResolutionConfigurationForm CreateConfigurationForm(bool isInitialStartup, bool hasStarted)
    {
        var screenRuntimes = hasStarted ? _screenRuntimes : null;
        var form = new ResolutionConfigurationForm(_settings, isInitialStartup, _localIp, hasStarted, screenRuntimes, _appearanceStore);

        form.ConfigurationSaved += ApplySelection;
        form.StopRequested += StopService;
        form.StartupConfirmed += OnFormStartupConfirmed;

        return form;
    }

    private void ApplySelection(VirtualWebDisplaySettings selection)
    {
        selection.EnsureValid();

        selection.Screen1.CopyTo(_settings.Screen1);
        selection.Screen2.CopyTo(_settings.Screen2);
        _settings.UiLanguage = selection.UiLanguage;
        _settings.WindowTheme = selection.WindowTheme;
        _settings.EnsureValid();
        _settingsStore.Save(_settings);
    }

    private static void OpenStreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void ExitApplication()
    {
        if (_serviceStartSignal is not null && !_serviceStartSignal.Task.IsCompleted)
        {
            // Service is stopped — signal no restart so the wait loop exits.
            _serviceStartSignal.TrySetResult(false);
        }
        else
        {
            _exitRequested?.Invoke();
        }
        _context?.ExitThread();
    }

    private void StopService()
    {
        if (_serviceActionPending)
            return;

        _serviceActionPending = true;
        _stopRequested?.Invoke();
    }

    private void StartService()
    {
        if (_serviceActionPending)
            return;

        _serviceActionPending = true;
        _serviceStartSignal?.TrySetResult(true);
    }

    private void OnFormStartupConfirmed()
    {
        // Handles both initial startup (no-op when signal is null) and
        // restart from stopped state (resolves _serviceStartSignal).
        StartService();
    }

    public void NotifyServiceStopped()
    {
        _serviceActionPending = false;
        _serviceStartSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _screenRuntimes = [];

        PostToUi(() =>
        {
            _contextMenu?.Dispose();
            _contextMenu = BuildContextMenu();
            if (_notifyIcon is not null)
                _notifyIcon.ContextMenuStrip = _contextMenu;

            _startupForm?.NotifyServiceStopped();
            _configForm?.NotifyServiceStopped();
        });
    }

    public Task<bool> WaitForServiceStartAsync()
        => _serviceStartSignal?.Task ?? Task.FromResult(false);

    private void PostToUi(Action action)
    {
        if (_invoker is null || _invoker.IsDisposed || !_invoker.IsHandleCreated)
            return;

        try
        {
            _invoker.BeginInvoke(action);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // El control fue destruido entre el guard y el BeginInvoke (race condition al cerrar).
        }
    }

    private static string TrimTrayText(string text) =>
        text.Length <= 63 ? text : text[..63];

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        PostToUi(() => _context?.ExitThread());
        if (!_uiThread.Join(1500))
            _uiThread.Interrupt();
        _ready.Dispose();
    }
}
