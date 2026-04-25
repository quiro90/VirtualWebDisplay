using System.Drawing;

public static class VirtualDisplayPlacementOptions
{
    public const string Right = "right";
    public const string Left = "left";
    public const string Top = "top";
    public const string Bottom = "bottom";

    public static string Normalize(string? placement) =>
        placement?.Trim().ToLowerInvariant() switch
        {
            Left or "izquierda" => Left,
            Top or "up" or "arriba" => Top,
            Bottom or "down" or "abajo" => Bottom,
            _ => Right,
        };

    public static string GetDisplayLabel(string? placement) =>
        Normalize(placement) switch
        {
            Left => "izquierda",
            Top => "arriba",
            Bottom => "abajo",
            _ => "derecha",
        };

    public static Point GetPosition(Rectangle primaryBounds, string? placement, int width, int height) =>
        Normalize(placement) switch
        {
            Left => new Point(primaryBounds.Left - width, primaryBounds.Top),
            Top => new Point(primaryBounds.Left, primaryBounds.Top - height),
            Bottom => new Point(primaryBounds.Left, primaryBounds.Bottom),
            _ => new Point(primaryBounds.Right, primaryBounds.Top),
        };
}
