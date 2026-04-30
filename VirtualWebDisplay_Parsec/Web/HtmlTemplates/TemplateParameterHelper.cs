namespace VirtualWebDisplay.Web.HtmlTemplates;

/// <summary>
/// Helper para procesar parámetros comunes de templates HTML.
/// Evita duplicación de código entre WebImagePageTemplate y RtcPageTemplate.
/// </summary>
internal static class TemplateParameterHelper
{
    /// <summary>
    /// Extrae y valida el título del diccionario de parámetros.
    /// </summary>
    public static string GetTitle(Dictionary<string, object> parameters) =>
        parameters.GetValueOrDefault("title", "VirtualWebDisplay") as string ?? "VirtualWebDisplay";

    /// <summary>
    /// Extrae y valida el modo de ajuste de imagen del navegador.
    /// </summary>
    public static string GetBrowserImageFit(Dictionary<string, object> parameters) =>
        parameters.GetValueOrDefault("browserImageFit", "cover") as string ?? "cover";

    /// <summary>
    /// Convierte el modo de ajuste de imagen a CSS background-size.
    /// </summary>
    public static string GetBackgroundSize(string browserImageFit) =>
        browserImageFit switch
        {
            "contain" => "contain",
            "cover" => "cover",
            _ => "100% 100%"
        };

    /// <summary>
    /// Extrae y convierte el intervalo de captura en milisegundos.
    /// </summary>
    public static int GetIntervalMs(Dictionary<string, object> parameters)
    {
        var intervalMsObj = parameters.GetValueOrDefault("intervalMs", 250);
        return intervalMsObj is int intVal ? intVal : Convert.ToInt32(intervalMsObj);
    }

    /// <summary>
    /// Extrae y convierte el delay de hold para gestos táctiles.
    /// </summary>
    public static int GetGestureHoldDelayMs(Dictionary<string, object> parameters)
    {
        var gestureHoldDelayMsObj = parameters.GetValueOrDefault("gestureHoldDelayMs", 300);
        return gestureHoldDelayMsObj is int holdInt ? holdInt : Convert.ToInt32(gestureHoldDelayMsObj);
    }

    /// <summary>
    /// Calcula el throttling de eventos táctiles basándose en el intervalo de captura.
    /// Usa la constante mínima definida en TouchInputConstants.
    /// </summary>
    public static int CalculateThrottleMs(int intervalMs) =>
        (int)Math.Round(Math.Max(Configuration.TouchInputConstants.MinThrottleMs, intervalMs / 5.0));
}
