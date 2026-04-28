namespace VirtualWebDisplay.Configuration;

public static class TouchGestureOptions
{
    public const int MinHoldDelayMs = 150;
    public const int MaxHoldDelayMs = 750;
    public const int DefaultHoldDelayMs = 300;

    public static int ClampHoldDelay(int value) => Math.Clamp(value, MinHoldDelayMs, MaxHoldDelayMs);
}
