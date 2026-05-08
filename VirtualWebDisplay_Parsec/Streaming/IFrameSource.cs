namespace VirtualWebDisplay.Streaming;

/// <summary>
/// Abstraction over a screen capture source that provides both JPEG frames
/// (for WebImage / MJPEG modes) and raw pixel frames (for H.264 encoding).
/// </summary>
internal interface IFrameSource
{
    /// <summary>
    /// Returns the latest captured frame encoded as JPEG.
    /// Returns an empty array when no frame has been captured yet.
    /// Used by WebImage polling and MJPEG endpoints.
    /// </summary>
    byte[] GetCurrentJpegFrame();

    /// <summary>
    /// Raised on the capture thread each time a new raw pixel frame is available.
    /// Subscribers must return quickly; offload heavy work to a background queue.
    /// </summary>
    event Action<RawFrame>? RawFrameAvailable;
}
