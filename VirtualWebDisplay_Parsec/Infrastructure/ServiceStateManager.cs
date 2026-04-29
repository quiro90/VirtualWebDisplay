namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Gestiona el estado del servicio de manera centralizada.
/// Implementa una máquina de estados con transiciones válidas y notificaciones.
/// Single Responsibility: Solo gestiona el estado del servicio.
/// Thread-safe: Usa lock para proteger transiciones de estado.
/// </summary>
internal sealed class ServiceStateManager
{
    private readonly object _stateLock = new();
    private ServiceState _currentState;
    private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes;
    private TaskCompletionSource<bool>? _serviceStartSignal;

    public ServiceState CurrentState
    {
        get { lock (_stateLock) return _currentState; }
    }

    public IReadOnlyList<ScreenRuntimeContext> ScreenRuntimes
    {
        get { lock (_stateLock) return _screenRuntimes; }
    }

    public bool IsStarted
    {
        get { lock (_stateLock) return _currentState == ServiceState.Started; }
    }

    public bool IsStopped
    {
        get { lock (_stateLock) return _currentState == ServiceState.Stopped; }
    }

    public bool IsTransitioning
    {
        get { lock (_stateLock) return _currentState is ServiceState.Starting or ServiceState.Stopping; }
    }

    public event Action<ServiceState>? StateChanged;
    public event Action<IReadOnlyList<ScreenRuntimeContext>>? ServiceStarted;
    public event Action? ServiceStopped;

    public ServiceStateManager(ServiceState initialState = ServiceState.Stopped)
    {
        _currentState = initialState;
        _screenRuntimes = [];
    }

    /// <summary>
    /// Transiciona el estado a 'Starting'.
    /// Solo válido desde Stopped.
    /// </summary>
    public void RequestStart()
    {
        lock (_stateLock)
        {
            if (_currentState != ServiceState.Stopped)
                return;

            TransitionTo(ServiceState.Starting);
        }
    }

    /// <summary>
    /// Transiciona el estado a 'Stopping'.
    /// Solo válido desde Started.
    /// </summary>
    public void RequestStop()
    {
        lock (_stateLock)
        {
            if (_currentState != ServiceState.Started)
                return;

            TransitionTo(ServiceState.Stopping);
        }
    }

    /// <summary>
    /// Completa la transición a 'Started' y registra los runtimes activos.
    /// Válido desde Starting (reinicio) o Stopped (inicio inicial).
    /// </summary>
    public void CompleteStart(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        IReadOnlyList<ScreenRuntimeContext> runtimes;

        lock (_stateLock)
        {
            // Permitir transición desde Stopped (startup inicial) o Starting (restart)
            if (_currentState is not (ServiceState.Stopped or ServiceState.Starting))
                return;

            _screenRuntimes = screenRuntimes ?? [];
            runtimes = _screenRuntimes;
            TransitionTo(ServiceState.Started);
        }

        // Disparar eventos fuera del lock para evitar deadlocks
        ServiceStarted?.Invoke(runtimes);
    }

    /// <summary>
    /// Completa la transición a 'Stopped' y limpia los runtimes.
    /// Válido desde Stopping (detención solicitada) o Started (detención forzada/error).
    /// </summary>
    public void CompleteStop()
    {
        lock (_stateLock)
        {
            // Permitir transición desde Started (detención abrupta) o Stopping (detención normal)
            if (_currentState is ServiceState.Stopped)
                return;

            _screenRuntimes = [];
            _serviceStartSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TransitionTo(ServiceState.Stopped);
        }

        // Disparar evento fuera del lock para evitar deadlocks
        ServiceStopped?.Invoke();
    }

    /// <summary>
    /// Obtiene una tarea que se completa cuando se solicita un nuevo inicio.
    /// Usado por el bucle de ApplicationLifecycleManager.
    /// </summary>
    public Task<bool> WaitForStartRequestAsync()
    {
        lock (_stateLock)
        {
            _serviceStartSignal ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _serviceStartSignal.Task;
        }
    }

    /// <summary>
    /// Señala la solicitud de inicio al Task esperando.
    /// </summary>
    public void SignalStartRequest()
    {
        TaskCompletionSource<bool>? signal;
        lock (_stateLock)
        {
            signal = _serviceStartSignal;
        }
        signal?.TrySetResult(true);
    }

    /// <summary>
    /// Señala que no se desea reiniciar el servicio (salida de aplicación).
    /// </summary>
    public void SignalNoRestart()
    {
        TaskCompletionSource<bool>? signal;
        lock (_stateLock)
        {
            signal = _serviceStartSignal;
        }
        signal?.TrySetResult(false);
    }

    /// <summary>
    /// Transición interna de estado. DEBE ser llamado dentro de un lock.
    /// </summary>
    private void TransitionTo(ServiceState newState)
    {
        if (_currentState == newState)
            return;

        _currentState = newState;

        // Disparar StateChanged puede causar deadlock si los handlers también lockean,
        // pero dado que es para UI updates (que usan BeginInvoke), es seguro.
        StateChanged?.Invoke(newState);
    }
}

/// <summary>
/// Estados posibles del servicio.
/// Transiciones válidas: Stopped → Starting → Started → Stopping → Stopped
/// </summary>
internal enum ServiceState
{
    Stopped,
    Starting,
    Started,
    Stopping
}
