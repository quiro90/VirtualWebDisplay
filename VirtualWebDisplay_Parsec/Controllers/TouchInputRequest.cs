namespace VirtualWebDisplay.Controllers;

/// <summary>
/// Representa un evento táctil enviado desde el cliente web (tablet).
/// Se usa para simular entrada de mouse desde toques en pantalla táctil.
/// </summary>
public sealed class TouchInputRequest
{
    /// <summary>
    /// Tipo de evento táctil: "touchstart", "touchmove", "touchend".
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Coordenada X en píxeles, relativa al viewport del navegador en la tablet.
    /// </summary>
    public required double X { get; set; }

    /// <summary>
    /// Coordenada Y en píxeles, relativa al viewport del navegador en la tablet.
    /// </summary>
    public required double Y { get; set; }

    /// <summary>
    /// Ancho del viewport en píxeles (necesario para mapeo de coordenadas viewport → pantalla).
    /// </summary>
    public double ViewportWidth { get; set; } = 1.0;

    /// <summary>
    /// Alto del viewport en píxeles (necesario para mapeo de coordenadas).
    /// </summary>
    public double ViewportHeight { get; set; } = 1.0;

    /// <summary>
    /// Número de dedos tocando simultáneamente.
    /// 1 = click izquierdo
    /// 2+ = doble click (o click derecho, según configuración futura)
    /// </summary>
    public int Fingers { get; set; } = 1;

    /// <summary>
    /// Timestamp del evento en milisegundos (desde epoch).
    /// Útil para tracking y debugging.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Acción semántica decidida por el cliente:
    /// "tap" = click rápido, "dragstart" = inicio de drag (hold 1 dedo),
    /// "dragmove" = movimiento durante drag, "dragend" = fin de drag,
    /// "scrollmove" = scroll con 2 dedos, "scrollend" = fin de scroll.
    /// Vacío = comportamiento legacy por Type/Fingers.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Delta de scroll vertical en píxeles (positivo = abajo, negativo = arriba).
    /// Solo presente en action "scrollmove".
    /// </summary>
    public double ScrollDeltaY { get; set; }

    /// <summary>
    /// Delta de scroll horizontal en píxeles (positivo = derecha, negativo = izquierda).
    /// Solo presente en action "scrollmove".
    /// </summary>
    public double ScrollDeltaX { get; set; }
}
