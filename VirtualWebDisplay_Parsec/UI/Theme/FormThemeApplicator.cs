using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using VirtualWebDisplay.Configuration;
using VirtualWebDisplay.UI.Forms;

namespace VirtualWebDisplay.UI.Theme;

/// <summary>
/// Applies the theme palette to WinForms controls recursively.
/// Centralizes theming logic that previously lived in ResolutionConfigurationForm.
/// </summary>
internal static class FormThemeApplicator
{
    /// <summary>
    /// Resolves whether dark mode is active based on the selected preference
    /// or the operating system setting.
    /// </summary>
    internal static bool ResolveDarkMode(string selectedWindowTheme)
    {
        if (selectedWindowTheme == WindowThemeOptions.Dark)
            return true;

        if (selectedWindowTheme == WindowThemeOptions.Light)
            return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value     = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
                return intValue == 0;
        }
        catch { } // Registry read can fail in restricted environments; safe to ignore — falls back to light mode.

        return false;
    }

    /// <summary>
    /// Recursively applies the palette to all child controls of the root container.
    /// </summary>
    internal static void ApplyThemeRecursive(Control root, ThemePalette palette)
    {
        foreach (Control control in root.Controls)
        {
            switch (control)
            {
                case ThemedComboBox c:
                    c.ApplyPalette(
                        backgroundColor:          palette.Input,
                        foregroundColor:          palette.Foreground,
                        borderColor:              palette.Border,
                        selectionBackgroundColor: palette.TitleButton,
                        selectionForegroundColor: palette.TitleForeground,
                        buttonColor:              palette.Button,
                        arrowColor:               palette.ButtonText);
                    break;

                case ThemedNumericUpDown c:
                    c.ApplyPalette(
                        backgroundColor:       palette.Input,
                        foregroundColor:       palette.Foreground,
                        borderColor:           palette.Border,
                        buttonColor:           palette.Button,
                        buttonForegroundColor: palette.ButtonText);
                    c.BackColor = palette.Panel;
                    break;

                case ThemedTrackBar c:
                    c.BackColor = palette.Panel;
                    c.ApplyPalette(
                        trackColor:       palette.Border,
                        activeTrackColor: palette.Link,
                        thumbColor:       palette.TitleButton,
                        tickColor:        palette.Foreground);
                    break;

                case TabControl c:
                    c.BackColor = palette.Panel;
                    c.ForeColor = palette.Foreground;
                    break;

                case TabPage c:
                    c.BackColor = palette.Panel;
                    c.ForeColor = palette.Foreground;
                    break;

                case Button c:
                    c.BackColor                  = palette.Button;
                    c.ForeColor                  = palette.ButtonText;
                    c.FlatStyle                  = FlatStyle.Flat;
                    c.FlatAppearance.BorderColor = palette.Border;
                    break;

                case CheckBox c:
                    c.BackColor = Color.Transparent;
                    c.ForeColor = palette.Foreground;
                    break;

                case LinkLabel c:
                    c.LinkColor        = palette.Link;
                    c.ActiveLinkColor  = palette.LinkActive;
                    c.VisitedLinkColor = palette.Link;
                    c.ForeColor        = palette.Link;
                    break;

                case Label c:
                    c.BackColor = Color.Transparent;
                    c.ForeColor = palette.Foreground;
                    break;

                case TextBox c:
                    c.BackColor = palette.Input;
                    c.ForeColor = palette.Foreground;
                    break;

                case Panel p when p.Tag is "preserve-color":
                    // BackColor intencional (p.ej. panel de advertencia amarillo) — no se toca.
                    p.ForeColor = palette.Foreground;
                    break;

                default:
                    control.BackColor = palette.Panel;
                    control.ForeColor = palette.Foreground;
                    break;
            }

            if (control.HasChildren)
                ApplyThemeRecursive(control, palette);
        }
    }

    /// <summary>
    /// Applies title-bar button style (close, settings).
    /// </summary>
    internal static void StyleTitleButton(Button button, ThemePalette palette)
    {
        button.BackColor                  = palette.TitleButton;
        button.ForeColor                  = palette.TitleForeground;
        button.FlatStyle                  = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = palette.Border;
    }

    /// <summary>
    /// Intenta crear la fuente UI preferida (Segoe UI Variable Text 9pt).
    /// Devuelve null si la fuente no está instalada.
    /// </summary>
    internal static Font? TryCreateUiFont()
    {
        try { return new Font("Segoe UI Variable Text", 9F); }
        catch { return null; }
    }

    /// <summary>
    /// Applies the theme to a ContextMenuStrip and all its items and submenus.
    /// </summary>
    internal static void ApplyThemeToMenu(ContextMenuStrip menu, ThemePalette palette)
    {
        menu.BackColor = palette.Panel;
        menu.ForeColor = palette.Foreground;
        menu.Renderer  = new ThemedMenuRenderer(palette.Panel, palette.Border, palette.Button, palette.Foreground);

        foreach (ToolStripItem rootItem in menu.Items)
        {
            rootItem.BackColor = palette.Panel;
            rootItem.ForeColor = palette.Foreground;

            if (rootItem is not ToolStripMenuItem root)
                continue;

            root.DropDown.BackColor = palette.Panel;
            root.DropDown.ForeColor = palette.Foreground;
            root.DropDown.Renderer  = new ThemedMenuRenderer(palette.Panel, palette.Border, palette.Button, palette.Foreground);

            foreach (ToolStripItem child in root.DropDownItems)
            {
                child.BackColor = palette.Panel;
                child.ForeColor = palette.Foreground;
            }
        }
    }
}
