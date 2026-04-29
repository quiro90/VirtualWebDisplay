using System.Reflection;

namespace VirtualWebDisplay.UI.HtmlTemplates;

/// <summary>
/// Proporciona versionado automático para archivos estáticos (JavaScript, CSS).
/// La versión se sincroniza con la versión del ensamblado definida en el .csproj.
/// Usado para cache busting: al cambiar la versión de la app, los navegadores
/// descargan automáticamente las nuevas versiones de archivos estáticos.
/// </summary>
public static class TemplateVersionHelper
{
    private static readonly string _version;

    static TemplateVersionHelper()
    {
        // Leer versión del ensamblado actual
        var assemblyVersion = Assembly.GetExecutingAssembly()
                                     .GetName()
                                     .Version;

        // Formatear como "Major.Minor.Build" (ej: "1.2.3")
        _version = assemblyVersion != null
            ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
            : "1.0.0";
    }

    /// <summary>
    /// Versión del ensamblado en formato "Major.Minor.Build" (ej: "1.2.3").
    /// Se usa en URLs de archivos estáticos para invalidar cache del navegador.
    /// Ejemplo: &lt;script src="/js/touch-input.js?v=1.2.3"&gt;&lt;/script&gt;
    /// </summary>
    public static string AppVersion => _version;
}
