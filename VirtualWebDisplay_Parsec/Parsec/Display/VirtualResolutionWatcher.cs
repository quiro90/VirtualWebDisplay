using Microsoft.Win32;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Parsec;

namespace VirtualWebDisplay.Parsec.Display;

/// <summary>
/// Detecta cambios de resoluci�n de los monitores virtuales activos y los persiste
/// en <c>virtualscreen.display.json</c> para restaurarlos en el pr�ximo arranque.
///
/// <para>
/// <b>Por qu� un hilo STA dedicado:</b> <see cref="SystemEvents.DisplaySettingsChanged"/>
/// s�lo se dispara si existe un message-pump Win32 en el hilo que hizo el subscribe.
/// El hilo async principal no tiene pump, por lo que el evento nunca llegar�a.
/// </para>
/// </summary>
internal sealed class VirtualResolutionWatcher : IDisposable
{
    private readonly IReadOnlyList<ScreenRuntimeContext> _runtimes;
    private readonly VirtualDisplayResolutionStore _store;
    private readonly Thread _pumpThread;
    private readonly PumpContext _pumpContext = new();
    private volatile bool _disposed;

    public VirtualResolutionWatcher(
        IReadOnlyList<ScreenRuntimeContext> runtimes,
        VirtualDisplayResolutionStore store)
    {
        _runtimes = runtimes;
        _store = store;

        _pumpThread = new Thread(RunPump)
        {
            IsBackground = true,
            Name = "ResolutionWatcher.Pump",
        };
        _pumpThread.SetApartmentState(ApartmentState.STA);
        _pumpThread.Start();
    }

    // -- Aplicar resolucion guardada ----------------------------------------------

    /// <summary>
    /// Aplica la resolucion guardada a los runtimes activos. Llamar justo despues
    /// de que los displays virtuales hayan sido creados (post-TryCreate).
    /// Si no hay resolucion guardada, guarda la que Windows asigno actualmente.
    /// </summary>
    public void RestoreOrSeedResolutions()
    {
        var saved = _store.Load();
        var current = ReadCurrentMetrics();

        var anyRestored = false;
        foreach (var runtime in _runtimes)
        {
            // Sincronizar siempre el Config con la resoluci�n real de Windows.
            if (current.TryGetValue(runtime.Id, out var cur))
            {
                runtime.Config.Width  = cur.Width;
                runtime.Config.Height = cur.Height;
                if (string.Equals(runtime.Config.VirtualDisplayPlacement?.Trim(), "windows_managed", StringComparison.OrdinalIgnoreCase))
                {
                    runtime.Config.SavedPositionX = cur.X;
                    runtime.Config.SavedPositionY = cur.Y;
                }
            }

            if (!saved.TryGetValue(runtime.Id, out var res))
                continue; // Sin historial: se usar� la que Windows asign�.

            var deviceName = runtime.DisplayManager.WindowsDeviceName;
            if (string.IsNullOrWhiteSpace(deviceName))
                continue;

            // Solo aplicar si la resoluci�n guardada difiere de la actual.
            if (current.TryGetValue(runtime.Id, out cur) && cur.Width == res.Width && cur.Height == res.Height && cur.X == res.X && cur.Y == res.Y)
                continue;

            runtime.Config.Width  = res.Width;
            runtime.Config.Height = res.Height;
            runtime.Config.SavedPositionX = res.X;
            runtime.Config.SavedPositionY = res.Y;

            var (ok, _) = runtime.DisplayManager.TryReconfigure(runtime.Config);
            if (ok)
                anyRestored = true;
        }

        if (!anyRestored)
            PersistCurrentResolutions();
    }

    // -- Message pump -------------------------------------------------------------

    private void RunPump()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        try
        {
            System.Windows.Forms.Application.Run(_pumpContext);
        }
        finally
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
    }

    // -- Handler ------------------------------------------------------------------

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_disposed)
            return;

        PersistCurrentResolutions();
    }

    // -- Helpers ------------------------------------------------------------------

    private Dictionary<string, (int Width, int Height, int X, int Y)> ReadCurrentMetrics()
    {
        var result = new Dictionary<string, (int Width, int Height, int X, int Y)>();
        foreach (var runtime in _runtimes)
        {
            var deviceName = runtime.DisplayManager.WindowsDeviceName;
            if (string.IsNullOrWhiteSpace(deviceName))
                continue;

            var res = VirtualDisplayManager.TryGetCurrentDisplayMetrics(deviceName);
            if (res is null)
                continue;

            result[runtime.Id] = res.Value;
        }
        return result;
    }

    private void PersistCurrentResolutions()
    {
        var current = ReadCurrentMetrics();
        if (current.Count == 0)
            return;

        var saved = _store.Load();
        foreach (var kv in current)
            saved[kv.Key] = kv.Value;

        try
        {
            _store.Save(saved);
        }
        catch
        {
            // No critico.
        }
    }

    // -- Dispose ------------------------------------------------------------------

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        PersistCurrentResolutions();

        _pumpContext.RequestStop();
        _pumpThread.Join(500);
    }

    // -- ApplicationContext dedicado ----------------------------------------------

    /// <summary>
    /// Contexto de aplicacion minimo que permite detener el pump desde un hilo externo
    /// de forma segura, sin interferir con otros pumps activos (p.ej. el tray icon).
    /// </summary>
    private sealed class PumpContext : System.Windows.Forms.ApplicationContext
    {
        public void RequestStop() => ExitThread();
    }
}
