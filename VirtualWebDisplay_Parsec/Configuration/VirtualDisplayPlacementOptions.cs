using System.Drawing;

namespace VirtualWebDisplay.Configuration;

public static class VirtualDisplayPlacementOptions
{
    public const string Right = "right";
    public const string Left = "left";
    public const string Top = "top";
    public const string Bottom = "bottom";
    public const string Duplicate = "duplicate";

    public static bool IsDuplicate(string? placement) =>
        string.Equals(placement?.Trim(), Duplicate, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? placement) =>
        placement?.Trim().ToLowerInvariant() switch
        {
            Left => Left,
            Top or "up" => Top,
            Bottom or "down" => Bottom,
            Duplicate => Duplicate,
            _ => Right,
        };

    public static string GetLocalizationKey(string? placement) =>
        Normalize(placement) switch
        {
            Left => "Tab_Placement_Left",
            Top => "Tab_Placement_Top",
            Bottom => "Tab_Placement_Bottom",
            Duplicate => "Tab_Placement_Duplicate",
            _ => "Tab_Placement_Right",
        };

    public static Point GetPosition(Rectangle primaryBounds, string? placement, int width, int height) =>
        Normalize(placement) switch
        {
            Left => new Point(primaryBounds.Left - width, primaryBounds.Top),
            Top => new Point(primaryBounds.Left, primaryBounds.Top - height),
            Bottom => new Point(primaryBounds.Left, primaryBounds.Bottom),
            Duplicate => new Point(primaryBounds.Left, primaryBounds.Top),
            _ => new Point(primaryBounds.Right, primaryBounds.Top),
        };
}

