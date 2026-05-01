namespace VirtualWebDisplay.Configuration;

public static class TouchGestureOptions
{
    public const int MinDelayMs = 10;
    public const int MaxDelayMs = 2000;

    public static int ClampDelay(int value) => Math.Clamp(value, MinDelayMs, MaxDelayMs);
}
