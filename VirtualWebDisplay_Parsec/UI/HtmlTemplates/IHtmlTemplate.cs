namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// Interface for HTML templates that generate pages dynamically.
/// </summary>
public interface IHtmlTemplate
{
    /// <summary>
    /// Generates the HTML content based on the provided parameters.
    /// </summary>
    string Generate(Dictionary<string, object> parameters);
}
