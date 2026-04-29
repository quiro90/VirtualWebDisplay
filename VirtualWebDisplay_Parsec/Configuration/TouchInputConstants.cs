namespace VirtualWebDisplay.Configuration;

/// <summary>
/// Constantes de configuración para el sistema de entrada táctil.
/// Centraliza valores que se comparten entre C# (servidor) y JavaScript (cliente).
/// </summary>
public static class TouchInputConstants
{
    /// <summary>
    /// Distancia máxima en píxeles que se permite mover el dedo antes de que un tap se considere un drag.
    /// Valor por defecto: 14px (aproximadamente 3-4mm en pantallas típicas de móvil).
    /// </summary>
    public const int TapMaxMovePx = 14;

    /// <summary>
    /// Tiempo máximo en milisegundos sin actividad de drag antes de liberar automáticamente el botón del mouse.
    /// Previene que el botón quede "colgado" si se pierden eventos de red.
    /// Valor por defecto: 1200ms (1.2 segundos).
    /// </summary>
    public const int DragStaleTimeoutMs = 1200;

    /// <summary>
    /// Throttling mínimo entre eventos táctiles en milisegundos.
    /// Evita flooding de eventos al servidor.
    /// Valor por defecto: 10ms (máximo ~100 eventos/segundo).
    /// </summary>
    public const int MinThrottleMs = 10;

    /// <summary>
    /// Intervalo por defecto entre eventos táctiles en milisegundos.
    /// Valor por defecto: 50ms (20 eventos/segundo).
    /// </summary>
    public const int DefaultThrottleMs = 50;

    /// <summary>
    /// Intervalo mínimo para keep-alive en milisegundos.
    /// Valor por defecto: 1000ms (1 segundo).
    /// </summary>
    public const int MinKeepaliveIntervalMs = 1000;

    /// <summary>
    /// Intervalo por defecto para keep-alive en milisegundos.
    /// Valor por defecto: 10000ms (10 segundos).
    /// </summary>
    public const int DefaultKeepaliveIntervalMs = 10000;

    /// <summary>
    /// Tamaño máximo de la ventana de latencias recientes para cálculo de promedio.
    /// Valor por defecto: 60 muestras.
    /// </summary>
    public const int MaxLatencySamples = 60;

    /// <summary>
    /// Ventana de tiempo en milisegundos para contar eventos por segundo.
    /// Valor por defecto: 1000ms (1 segundo).
    /// </summary>
    public const int EventsWindowMs = 1000;
}
