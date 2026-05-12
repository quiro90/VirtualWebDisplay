namespace VirtualWebDisplay.Web.Handlers;

internal static class TouchInputActions
{
    internal const string Tap = "tap";
    internal const string RightClick = "rightclick";
    internal const string MiddleClick = "middleclick";
    internal const string DragStart = "dragstart";
    internal const string DragMove = "dragmove";
    internal const string DragEnd = "dragend";
    internal const string ScrollMove = "scrollmove";
    internal const string ScrollEnd = "scrollend";

    internal const string LegacyTouchStart = "touchstart";
    internal const string LegacyTouchMove = "touchmove";
    internal const string LegacyTouchEnd = "touchend";

    internal static string NormalizeAction(string? action) =>
        Normalize(action);

    internal static string NormalizeLegacyType(string? type) =>
        Normalize(type);

    internal static bool IsDragAction(string action) =>
        action is DragStart or DragMove or DragEnd;

    internal static bool IsScrollAction(string action) =>
        action is ScrollMove or ScrollEnd;

    internal static bool IsGestureEndAction(string action) =>
        action is DragEnd or ScrollEnd;

    private static string Normalize(string? value) =>
        (value ?? string.Empty).ToLowerInvariant();
}
