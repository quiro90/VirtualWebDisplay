using System;
using System.Threading;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Gestiona la activación de una única instancia de la UI de la aplicación.
/// Usa un Mutex para detectar si ya hay una instancia corriendo y un EventWaitHandle
/// para que una segunda instancia le pida a la primera que se muestre en primer plano.
/// </summary>
public sealed class SingleInstanceActivator : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _eventWaitHandle;
    private readonly bool _isFirstInstance;
    private RegisteredWaitHandle? _registeredWaitHandle;

    /// <summary>
    /// Se dispara cuando una segunda instancia solicita que la instancia principal se muestre.
    /// </summary>
    public event Action? ShowApplicationRequested;

    public SingleInstanceActivator(string appId)
    {
        // Usamos un Mutex local para detectar si ya hay una instancia corriendo en esta sesión.
        _mutex = new Mutex(true, $"Local\\{appId}_Mutex", out _isFirstInstance);

        // Usamos un EventWaitHandle local para que la segunda instancia pueda comunicarse con la primera.
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, $"Local\\{appId}_Event");

        if (_isFirstInstance)
        {
            StartListeningForSignals();
        }
    }

    /// <summary>
    /// Indica si esta es la primera instancia de la aplicación.
    /// </summary>
    public bool IsFirstInstance => _isFirstInstance;

    /// <summary>
    /// Envía una señal a la primera instancia para que se muestre y luego sale.
    /// Este método debe ser llamado por la segunda instancia.
    /// </summary>
    public void SignalFirstInstanceAndExit()
    {
        if (!_isFirstInstance)
        {
            _eventWaitHandle.Set();
        }
    }

    private void StartListeningForSignals()
    {
        // Utilizamos el ThreadPool del sistema en lugar de bloquear un hilo dedicado,
        // lo cual es mucho más eficiente en memoria (~1MB ahorrado) y recursos.
        _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            _eventWaitHandle,
            (state, timedOut) =>
            {
                if (!timedOut)
                {
                    ShowApplicationRequested?.Invoke();
                }
            },
            state: null,
            timeout: Timeout.InfiniteTimeSpan,
            executeOnlyOnce: false);
    }

    public void Dispose()
    {
        _registeredWaitHandle?.Unregister(null);
        _eventWaitHandle.Dispose();

        // Solo liberamos el mutex si realmente lo adquirimos
        if (_isFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Si el mutex ya fue liberado o no pertenece a este hilo, lo ignoramos
                // Esto puede ocurrir en escenarios async donde el hilo cambia
            }
        }
        _mutex.Dispose();
    }
}