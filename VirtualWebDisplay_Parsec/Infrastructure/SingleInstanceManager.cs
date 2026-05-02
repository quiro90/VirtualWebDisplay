using System;
using System.Threading;

namespace VirtualWebDisplay.Infrastructure;

/// <summary>
/// Gestiona la ejecución de una única instancia de la aplicación usando un Mutex global.
/// Permite que una segunda instancia le pida a la primera que se muestre en primer plano.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _eventWaitHandle;
    private readonly bool _isFirstInstance;
    private Thread? _listenerThread;

    /// <summary>
    /// Se dispara cuando una segunda instancia solicita que la instancia principal se muestre.
    /// </summary>
    public event Action? ShowApplicationRequested;

    public SingleInstanceManager(string appId)
    {
        // Usamos un Mutex global para detectar si ya hay una instancia corriendo.
        _mutex = new Mutex(true, $"Global\\{appId}_Mutex", out _isFirstInstance);

        // Usamos un EventWaitHandle global para que la segunda instancia pueda comunicarse con la primera.
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, $"Global\\{appId}_Event");

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
        _listenerThread = new Thread(() =>
        {
            try
            {
                // El hilo se bloquea aquí hasta que se recibe una señal.
                while (_eventWaitHandle.WaitOne())
                {
                    // Se recibió una señal, invocamos el evento para mostrar la ventana.
                    ShowApplicationRequested?.Invoke();
                }
            }
            catch (ObjectDisposedException)
            {
                // El EventWaitHandle fue cerrado, lo cual es esperado al cerrar la app. El hilo termina limpiamente.
            }
        })
        {
            IsBackground = true,
            Name = "SingleInstanceSignalListener"
        };
        _listenerThread.Start();
    }

    public void Dispose()
    {
        _eventWaitHandle.Close(); // Esto desbloquea el hilo listener.
        if (_isFirstInstance)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }
}