using System.Drawing;

namespace VirtualWebDisplay.UI.Theme;

/// <summary>
/// Color palette for the application's visual theme.
/// </summary>
internal sealed record ThemePalette(
    Color Background,
    Color Panel,
    Color Foreground,
    Color Border,
    Color Button,
    Color ButtonText,
    Color Input,
    Color Link,
    Color LinkActive,
    Color TitleBackground,
    Color TitleForeground,
    Color TitleButton,
    Color WarningBackground,
    Color WarningForeground,
    Color WarningIcon)
{
    public static ThemePalette Light() => new(
        Background:      Color.FromArgb(244, 246, 250),
        Panel:           Color.White,
        Foreground:      Color.FromArgb(38,  44,  53),
        Border:          Color.FromArgb(206, 213, 224),
        Button:          Color.FromArgb(236, 240, 247),
        ButtonText:      Color.FromArgb(30,  36,  46),
        Input:           Color.White,
        Link:            Color.FromArgb(17,  92,  203),
        LinkActive:      Color.FromArgb(8,   69,  156),
        TitleBackground: Color.FromArgb(246, 249, 254),
        TitleForeground: Color.FromArgb(28,  36,  48),
        TitleButton:     Color.FromArgb(228, 236, 247),
        WarningBackground: Color.FromArgb(255, 251, 180),
        WarningForeground: Color.FromArgb(100, 60,  0),
        WarningIcon:       Color.FromArgb(120, 80,  0));

    public static ThemePalette Dark() => new(
        Background:      Color.FromArgb(20,  24,  31),
        Panel:           Color.FromArgb(30,  35,  44),
        Foreground:      Color.FromArgb(227, 233, 243),
        Border:          Color.FromArgb(64,  73,  90),
        Button:          Color.FromArgb(50,  60,  75),
        ButtonText:      Color.FromArgb(235, 241, 250),
        Input:           Color.FromArgb(40,  48,  60),
        Link:            Color.FromArgb(129, 182, 255),
        LinkActive:      Color.FromArgb(172, 208, 255),
        TitleBackground: Color.FromArgb(15,  19,  24),
        TitleForeground: Color.FromArgb(242, 245, 250),
        TitleButton:     Color.FromArgb(34,  44,  56),
        WarningBackground: Color.FromArgb(80,  70,  40),
        WarningForeground: Color.FromArgb(255, 251, 180),
        WarningIcon:       Color.FromArgb(255, 220, 100));
}
