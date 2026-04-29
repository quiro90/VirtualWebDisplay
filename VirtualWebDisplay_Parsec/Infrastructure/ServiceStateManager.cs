using VirtualWebDisplay.Infrastructure;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Gestiona el estado del servicio de manera centralizada.
/// Implementa una máquina de estados con transiciones válidas y notificaciones.
/// Single Responsibility: Solo gestiona el estado del servicio.
/// </summary>
internal sealed class ServiceStateManager
{
    private ServiceState _currentState;
    private IReadOnlyList<ScreenRuntimeContext> _screenRuntimes;
    private TaskCompletionSource<bool>? _serviceStartSignal;

    public ServiceState CurrentState => _currentState;
    public IReadOnlyList<ScreenRuntimeContext> ScreenRuntimes => _screenRuntimes;
    public bool IsStarted => _currentState == ServiceState.Started;
    public bool IsStopped => _currentState == ServiceState.Stopped;
    public bool IsTransitioning => _currentState is ServiceState.Starting or ServiceState.Stopping;

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
        if (_currentState != ServiceState.Stopped)
            return;

        TransitionTo(ServiceState.Starting);
    }

    /// <summary>
    /// Transiciona el estado a 'Stopping'.
    /// Solo válido desde Started.
    /// </summary>
    public void RequestStop()
    {
        if (_currentState != ServiceState.Started)
            return;

        TransitionTo(ServiceState.Stopping);
    }

    /// <summary>
    /// Completa la transición a 'Started' y registra los runtimes activos.
    /// Solo válido desde Starting.
    /// </summary>
    public void CompleteStart(IReadOnlyList<ScreenRuntimeContext> screenRuntimes)
    {
        if (_currentState != ServiceState.Starting)
            return;

        _screenRuntimes = screenRuntimes ?? [];
        TransitionTo(ServiceState.Started);
        ServiceStarted?.Invoke(_screenRuntimes);
    }

    /// <summary>
    /// Completa la transición a 'Stopped' y limpia los runtimes.
    /// Solo válido desde Stopping.
    /// </summary>
    public void CompleteStop()
    {
        if (_currentState != ServiceState.Stopping)
            return;

        _screenRuntimes = [];
        _serviceStartSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TransitionTo(ServiceState.Stopped);
        ServiceStopped?.Invoke();
    }

    /// <summary>
    /// Obtiene una tarea que se completa cuando se solicita un nuevo inicio.
    /// Usado por el bucle de ApplicationLifecycleManager.
    /// </summary>
    public Task<bool> WaitForStartRequestAsync()
    {
        _serviceStartSignal ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _serviceStartSignal.Task;
    }

    /// <summary>
    /// Señala la solicitud de inicio al Task esperando.
    /// </summary>
    public void SignalStartRequest()
    {
        _serviceStartSignal?.TrySetResult(true);
    }

    /// <summary>
    /// Señala que no se desea reiniciar el servicio (salida de aplicación).
    /// </summary>
    public void SignalNoRestart()
    {
        _serviceStartSignal?.TrySetResult(false);
    }

    private void TransitionTo(ServiceState newState)
    {
        if (_currentState == newState)
            return;

        _currentState = newState;
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
