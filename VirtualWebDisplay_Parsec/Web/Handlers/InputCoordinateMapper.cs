namespace VirtualWebDisplay.Web.Handlers;

/// <summary>
/// Mapea coordenadas del viewport del navegador a coordenadas locales del monitor objetivo.
/// Extraído de InputHandler para permitir testing unitario independiente.
/// </summary>
internal static class InputCoordinateMapper
{
    /// <summary>
    /// Convierte coordenadas de viewport a píxeles de pantalla.
    /// Normaliza a [0,1] y luego escala a la resolución del monitor, clampeando para evitar
    /// coordenadas fuera de rango que causarían comportamiento indefinido en Windows.
    /// </summary>
    internal static (int screenX, int screenY) Map(
        double viewportX,
        double viewportY,
        double viewportWidth,
        double viewportHeight,
        int screenWidth,
        int screenHeight)
    {
        double normX = viewportWidth > 0 ? viewportX / viewportWidth : 0;
        double normY = viewportHeight > 0 ? viewportY / viewportHeight : 0;

        normX = Math.Clamp(normX, 0.0, 1.0);
        normY = Math.Clamp(normY, 0.0, 1.0);

        int screenX = (int)Math.Round(normX * Math.Max(1, screenWidth - 1));
        int screenY = (int)Math.Round(normY * Math.Max(1, screenHeight - 1));

        screenX = Math.Clamp(screenX, 0, Math.Max(0, screenWidth - 1));
        screenY = Math.Clamp(screenY, 0, Math.Max(0, screenHeight - 1));

        return (screenX, screenY);
    }
}
