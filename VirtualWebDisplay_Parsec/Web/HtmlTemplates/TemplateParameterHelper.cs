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
    public static bool GetTouchZoomEnabled(Dictionary<string, object> parameters)
        => parameters.TryGetValue("touchZoomEnabled", out var val) && Convert.ToBoolean(val);

    public static int GetTouchZoomDelayMs(Dictionary<string, object> parameters)
        => parameters.TryGetValue("touchZoomDelayMs", out var val) ? Convert.ToInt32(val) : 50;

    public static bool GetTouchHoldEnabled(Dictionary<string, object> parameters)
        => parameters.TryGetValue("touchHoldEnabled", out var val) && Convert.ToBoolean(val);

    public static int GetTouchHoldDelayMs(Dictionary<string, object> parameters)
        => parameters.TryGetValue("touchHoldDelayMs", out var val) ? Convert.ToInt32(val) : 250;

    public static bool GetTouchScrollEnabled(Dictionary<string, object> parameters)
        => parameters.TryGetValue("touchScrollEnabled", out var val) && Convert.ToBoolean(val);

    public static int GetTouchScrollDelayMs(Dictionary<string, object> parameters)
        => parameters.TryGetValue("touchScrollDelayMs", out var val) ? Convert.ToInt32(val) : 250;

    /// <summary>
    /// Calcula el throttling de eventos táctiles basándose en el intervalo de captura.
    /// Usa la constante mínima definida en TouchInputConstants.
    /// </summary>
    public static int CalculateThrottleMs(int intervalMs) =>
        (int)Math.Round(Math.Max(Configuration.TouchInputConstants.MinThrottleMs, intervalMs / 5.0));
}
