using System.Drawing;
using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Infrastructure;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.TrayIcon;

/// <summary>
/// System tray controller for VirtualWebDisplay.
/// Responsibilities: tray threading, UI coordination and menu construction.
/// Menu construction delegates to <see cref="TrayMenuBuilder"/>.
/// Form management delegates to <see cref="ConfigurationFormPresenter"/>.
/// Service state delegates to <see cref="ServiceStateManager"/>.
/// </summary>
public sealed class VirtualDisplayTrayController : IDisposable
{
    private readonly ConfigurationFormPresenter _formPresenter;
    private readonly ServiceStateManager _serviceState;
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _ready = new(false);

    private ApplicationContext? _context;
    private Control? _invoker;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private Action? _exitRequested;
    private Action? _stopRequested;
    private bool _disposed;
    private Icon? _appIcon;

    private static Icon LoadAppIcon()
    {
        var stream = typeof(VirtualDisplayTrayController).Assembly
            .GetManifestResourceStream("VirtualWebDisplay.app.ico");
        return stream is not null ? new Icon(stream) : SystemIcons.Application;
    }

    public VirtualDisplayTrayController(VirtualWebDisplaySettings settings, VirtualScreenSettingsStore settingsStore, AppearanceSettingsStore appearanceStore, string localIp)
    {
        _serviceState = new ServiceStateManager(ServiceState.Stopped);
        _formPresenter = new ConfigurationFormPresenter(settings, settingsStore, appearanceStore, localIp, _serviceState);

        // Suscribirse a eventos de estado del servicio
        _serviceState.StateChanged += OnServiceStateChanged;
        _serviceState.ServiceStarted += OnServiceStarted;
        _serviceState.ServiceStopped += OnServiceStopped;

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
            _formPresenter.OpenStartupForm(
                onConfirmed: () => completion.TrySetResult(true),
                onCancelled: () =>
                {
                    completion.TrySetResult(false);
                    _context?.ExitThread();
                });
        });

        return completion.Task.GetAwaiter().GetResult();
    }

    public void ConfigureRuntimeActions(Action exitRequested, Action stopRequested, IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        _exitRequested = exitRequested;
        _stopRequested = stopRequested;

        _formPresenter.StopRequested    -= StopService;
        _formPresenter.StartupConfirmed -= OnFormStartupConfirmed;
        _formPresenter.StopRequested    += StopService;
        _formPresenter.StartupConfirmed += OnFormStartupConfirmed;

        // Transición de estado: Starting → Started
        _serviceState.CompleteStart(screenRuntimes);
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

        _appIcon      = LoadAppIcon();
        _contextMenu  = BuildContextMenu();
        _notifyIcon   = new NotifyIcon
        {
            Icon             = _appIcon,
            Text             = TrimTrayText(AppText.Get("Common_AppName")),
            Visible          = true,
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

    private ContextMenuStrip BuildContextMenu() =>
        TrayMenuBuilder.Build(
            _serviceState.ScreenRuntimes,
            _serviceState.IsTransitioning,
            _serviceState.WaitForStartRequestAsync(),
            onShowConfiguration: ShowConfigurationDialog,
            onStopService:       StopService,
            onStartService:      StartService,
            onExit:              ExitApplication);

    private void ShowConfigurationDialog() =>
        _formPresenter.ShowConfigurationDialog(_serviceState.ScreenRuntimes);

    private void ExitApplication()
    {
        if (_serviceState.IsStopped)
        {
            // Service is stopped — signal no restart so the wait loop exits.
            _serviceState.SignalNoRestart();
        }
        else
        {
            _exitRequested?.Invoke();
        }
        _context?.ExitThread();
    }

    private void StopService()
    {
        if (_serviceState.IsTransitioning)
            return;

        _serviceState.RequestStop();
        _stopRequested?.Invoke();
    }

    private void StartService()
    {
        if (_serviceState.IsTransitioning)
            return;

        _serviceState.RequestStart();
        _serviceState.SignalStartRequest();
    }

    private void OnFormStartupConfirmed()
    {
        // Handles both initial startup and restart from stopped state.
        StartService();
    }

    private void OnServiceStateChanged(ServiceState newState)
    {
        // Reconstruir menú cuando cambia el estado
        PostToUi(() =>
        {
            _contextMenu?.Dispose();
            _contextMenu = BuildContextMenu();
            if (_notifyIcon is not null)
                _notifyIcon.ContextMenuStrip = _contextMenu;
        });
    }

    private void OnServiceStarted(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        var summary = string.Join(" | ", screenRuntimes.Select(r => $"{r.DisplayName}: {r.HostUrl}"));
        UpdateStatus(summary);

        PostToUi(() =>
        {
            if (_notifyIcon is null)
                return;

            _notifyIcon.BalloonTipTitle = AppText.Get("Tray_BalloonTitle");
            _notifyIcon.BalloonTipText  = string.Join("\n", screenRuntimes.Select(r =>
            {
                var locationText = string.Equals(r.HostUrl, r.IpUrl, StringComparison.OrdinalIgnoreCase)
                    ? $"{r.DisplayName}: {r.HostUrl}"
                    : $"{r.DisplayName}: {r.HostUrl} | {r.IpUrl}";

                return !r.SecurityGate.Enabled
                    ? locationText
                    : $"{locationText} | {AppText.Get("Tray_SecurityCode_Label")}: {r.SecurityGate.AccessCode}";
            }));
            _notifyIcon.ShowBalloonTip(5000);
        });
    }

    private void OnServiceStopped()
    {
        // Notificaciones de UI se manejan automáticamente vía eventos
    }

    public Task<bool> WaitForServiceStartAsync()
        => _serviceState.WaitForStartRequestAsync();

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
