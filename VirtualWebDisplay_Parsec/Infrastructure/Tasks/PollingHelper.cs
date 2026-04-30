namespace VirtualWebDisplay.Infrastructure.Tasks;

/// <summary>
/// Helper genérico para polling con timeout.
/// Centraliza el patrón de "esperar hasta que condición se cumpla o timeout expire".
/// </summary>
public static class PollingHelper
{
    /// <summary>
    /// Espera asíncronamente hasta que la condición se cumpla o se agote el timeout.
    /// </summary>
    /// <param name="condition">Función que retorna true cuando la condición se cumple.</param>
    /// <param name="timeout">Tiempo máximo de espera.</param>
    /// <param name="pollInterval">Intervalo entre comprobaciones (default: 100ms).</param>
    /// <param name="cancellationToken">Token de cancelación opcional.</param>
    /// <returns>True si la condición se cumplió antes del timeout, false si expiró.</returns>
    public static async Task<bool> WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (condition())
                return true;

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    /// <summary>
    /// Espera síncronamente hasta que la condición se cumpla o se agote el timeout.
    /// Útil para código que no puede ser async (ej: constructores, P/Invoke callbacks).
    /// </summary>
    public static bool WaitUntil(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return true;

            Thread.Sleep(interval);
        }

        return false;
    }
}
