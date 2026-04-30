namespace VirtualWebDisplay.Web.Security;

/// <summary>
/// Rate limiter simple para proteger contra flooding de eventos.
/// Implementa algoritmo de token bucket.
/// </summary>
internal class RateLimiter
{
    private readonly int _maxEventsPerSecond;
    private readonly object _lock = new object();
    private long _lastResetTicks = Environment.TickCount64;
    private int _tokensAvailable;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="maxEventsPerSecond">Máximo de eventos permitidos por segundo. Default: 100.</param>
    public RateLimiter(int maxEventsPerSecond = 100)
    {
        _maxEventsPerSecond = Math.Max(1, maxEventsPerSecond);
        _tokensAvailable = _maxEventsPerSecond;
    }

    /// <summary>
    /// Verifica si se permite un nuevo evento (consume un token si es así).
    /// </summary>
    /// <returns>true si se permite, false si se debe rechazar (rate limit)</returns>
    public bool AllowRequest()
    {
        lock (_lock)
        {
            var now = Environment.TickCount64;
            var elapsedMs = now - _lastResetTicks;

            // Cada segundo, reponer tokens
            if (elapsedMs >= 1000)
            {
                _tokensAvailable = _maxEventsPerSecond;
                _lastResetTicks = now;
            }

            // Si tenemos tokens disponibles, consumir uno
            if (_tokensAvailable > 0)
            {
                _tokensAvailable--;
                return true;
            }

            // Sin tokens disponibles
            return false;
        }
    }

    /// <summary>
    /// Reset manual del limiter (útil para debugging).
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _tokensAvailable = _maxEventsPerSecond;
            _lastResetTicks = Environment.TickCount64;
        }
    }

    /// <summary>
    /// Obtiene información del estado actual.
    /// </summary>
    public (int tokensAvailable, int maxPerSecond, long elapsedMs) GetStatus()
    {
        lock (_lock)
        {
            var elapsed = Environment.TickCount64 - _lastResetTicks;
            return (_tokensAvailable, _maxEventsPerSecond, elapsed);
        }
    }
}
