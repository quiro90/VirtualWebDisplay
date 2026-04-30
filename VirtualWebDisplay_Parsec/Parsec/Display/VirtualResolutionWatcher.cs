using Microsoft.Win32;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Parsec;

namespace VirtualWebDisplay.Parsec.Display;

/// <summary>
/// Detecta cambios de resolución de los monitores virtuales activos y los persiste
/// en <c>virtualscreen.display.json</c> para restaurarlos en el próximo arranque.
///
/// <para>
/// <b>Por qué un hilo STA dedicado:</b> <see cref="SystemEvents.DisplaySettingsChanged"/>
/// sólo se dispara si existe un message-pump Win32 en el hilo que hizo el subscribe.
/// El hilo async principal no tiene pump, por lo que el evento nunca llegaría.
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
        var current = ReadCurrentResolutions();

        var anyRestored = false;
        foreach (var runtime in _runtimes)
        {
            // Sincronizar siempre el Config con la resolución real de Windows.
            if (current.TryGetValue(runtime.Id, out var cur))
            {
                runtime.Config.Width  = cur.Width;
                runtime.Config.Height = cur.Height;
            }

            if (!saved.TryGetValue(runtime.Id, out var res))
                continue; // Sin historial: se usará la que Windows asignó.

            var deviceName = runtime.DisplayManager.WindowsDeviceName;
            if (string.IsNullOrWhiteSpace(deviceName))
                continue;

            // Solo aplicar si la resolución guardada difiere de la actual.
            if (current.TryGetValue(runtime.Id, out cur) && cur == res)
                continue;

            runtime.Config.Width  = res.Width;
            runtime.Config.Height = res.Height;

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

    private Dictionary<string, (int Width, int Height)> ReadCurrentResolutions()
    {
        var result = new Dictionary<string, (int Width, int Height)>();
        foreach (var runtime in _runtimes)
        {
            var deviceName = runtime.DisplayManager.WindowsDeviceName;
            if (string.IsNullOrWhiteSpace(deviceName))
                continue;

            var res = VirtualDisplayManager.TryGetCurrentResolution(deviceName);
            if (res is null)
                continue;

            result[runtime.Id] = res.Value;
        }
        return result;
    }

    private void PersistCurrentResolutions()
    {
        var current = ReadCurrentResolutions();
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
