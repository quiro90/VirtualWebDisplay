using System.Windows.Forms;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.Configuration.Models;
using VirtualWebDisplay.Localization;

namespace VirtualWebDisplay.UI.Forms;

/// <summary>
/// Valida y construye el objeto <see cref="VirtualWebDisplaySettings"/> a partir
/// de los valores del formulario de configuración.
/// </summary>
internal static class SettingsFormValidator
{
    /// <summary>
    /// Intenta construir un <see cref="VirtualWebDisplaySettings"/> validado.
    /// Muestra un MessageBox si la validación falla.
    /// </summary>
    /// <returns><c>true</c> si la configuración es válida; <c>false</c> en caso contrario.</returns>
    internal static bool TryBuild(
        string languageCode,
        string windowTheme,
        VirtualScreenConfig screen1Config,
        VirtualScreenConfig screen2Config,
        out VirtualWebDisplaySettings settings)
    {
        settings = new VirtualWebDisplaySettings
        {
            UiLanguage  = languageCode,
            WindowTheme = windowTheme,
            Screen1     = screen1Config,
            Screen2     = screen2Config,
        };
        settings.EnsureValid();

        if (settings.Screen2.Enabled && settings.Screen1.Port == settings.Screen2.Port)
        {
            MessageBox.Show(
                AppText.Get("Validation_DuplicatePort_Message"),
                AppText.Get("Validation_DuplicatePort_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }
}
