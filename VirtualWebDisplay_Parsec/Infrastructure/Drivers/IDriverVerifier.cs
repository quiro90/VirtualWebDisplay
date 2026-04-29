namespace VirtualWebDisplay.Infrastructure.Drivers;

/// <summary>
/// Abstracción para verificar disponibilidad de drivers de display virtual.
/// Permite soportar múltiples implementaciones (Parsec VDD, IddSample, futuro Linux/macOS).
/// </summary>
public interface IDriverVerifier
{
    /// <summary>
    /// Verifica si el driver está disponible y listo para crear displays virtuales.
    /// </summary>
    /// <returns>
    /// (isAvailable: true si el driver está listo, statusMessage: descripción del estado)
    /// </returns>
    (bool isAvailable, string statusMessage) Verify();

    /// <summary>
    /// URL de descarga/instalación del driver.
    /// </summary>
    string InstallUrl { get; }

    /// <summary>
    /// Nombre descriptivo del driver para mensajes de error.
    /// </summary>
    string DriverName { get; }
}
