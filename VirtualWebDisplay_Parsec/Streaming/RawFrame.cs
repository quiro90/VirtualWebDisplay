namespace VirtualWebDisplay.Streaming;

/// <summary>
/// A raw pixel frame produced by the capture source, ready for encoding.
/// </summary>
/// <param name="Data">
/// Pixel data in BGRA32 format (4 bytes per pixel, row-major, no padding).
/// </param>
/// <param name="Width">Frame width in pixels.</param>
/// <param name="Height">Frame height in pixels.</param>
/// <param name="TimestampUs">Capture timestamp in microseconds (monotonic clock).</param>
internal readonly record struct RawFrame(byte[] Data, int Width, int Height, long TimestampUs);
