namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// Interfaz para templates HTML que generan páginas dinámicamente.
/// </summary>
public interface IHtmlTemplate
{
    /// <summary>
    /// Genera el contenido HTML basado en los parámetros proporcionados.
    /// </summary>
    string Generate(Dictionary<string, object> parameters);
}
