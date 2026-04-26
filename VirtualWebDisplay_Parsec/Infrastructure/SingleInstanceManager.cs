using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace VirtualWebDisplay.Infrastructure;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

public sealed class SingleInstanceManager : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _restartEvent;
    private readonly string _processPath;
    private bool _ownsMutex;
    private bool _disposed;
    private Action? _shutdownAction;
    private CancellationTokenSource? _listenerCancellation;
    private Task? _listenerTask;

    private SingleInstanceManager(string mutexName, string restartEventName, string processPath)
    {
        _mutex = new Mutex(false, mutexName);
        _restartEvent = new EventWaitHandle(false, EventResetMode.AutoReset, restartEventName);
        _processPath = processPath;
        _ownsMutex = _mutex.WaitOne(0);
    }

    public static SingleInstanceManager CreateForCurrentExecutable()
    {
        var processPath = Path.GetFullPath(Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(processPath)))[..24];
        return new SingleInstanceManager($"Local\\VirtualWebDisplay_Mutex_{hash}", $"Local\\VirtualWebDisplay_Restart_{hash}", processPath);
    }

    public bool EnsureSingleInstance(TimeSpan timeout)
    {
        if (_ownsMutex)
            return true;

        _restartEvent.Set();

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (_mutex.WaitOne(TimeSpan.FromMilliseconds(250)))
                {
                    _ownsMutex = true;
                    return true;
                }
            }
            catch (AbandonedMutexException)
            {
                // El proceso anterior terminÃ³ sin liberar el mutex; lo adquirimos igual.
                _ownsMutex = true;
                return true;
            }
        }

        CloseOtherInstances();

        try
        {
            if (_mutex.WaitOne(TimeSpan.FromSeconds(5)))
            {
                _ownsMutex = true;
                return true;
            }
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true;
            return true;
        }

        return false;
    }

    public void StartShutdownListener(Action shutdownAction)
    {
        _shutdownAction = shutdownAction;
        _listenerCancellation?.Cancel();
        _listenerCancellation?.Dispose();
        _listenerCancellation = new CancellationTokenSource();
        var token = _listenerCancellation.Token;

        _listenerTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_restartEvent.WaitOne(TimeSpan.FromMilliseconds(500)))
                {
                    _shutdownAction?.Invoke();
                    break;
                }

                try
                {
                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void CloseOtherInstances()
    {
        var currentProcess = Process.GetCurrentProcess();
        var processName = currentProcess.ProcessName;

        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                if (process.Id == currentProcess.Id)
                    continue;

                var otherPath = process.MainModule?.FileName;
                if (!string.Equals(Path.GetFullPath(otherPath ?? string.Empty), _processPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (process.CloseMainWindow() && process.WaitForExit(3000))
                    continue;

                process.Kill(true);
                process.WaitForExit(5000);
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _listenerCancellation?.Cancel();

        try
        {
            _listenerTask?.Wait(1000);
        }
        catch
        {
        }

        _listenerCancellation?.Dispose();
        _restartEvent.Dispose();

        if (_ownsMutex)
        {
            try { _mutex.ReleaseMutex(); }
            catch (ApplicationException) { }
            _ownsMutex = false;
        }

        _mutex.Dispose();
    }
}

