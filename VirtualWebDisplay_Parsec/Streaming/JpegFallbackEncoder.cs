using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace VirtualWebDisplay.Streaming;

/// <summary>
/// Encodes raw BGRA32 pixel frames to JPEG for WebImage polling and MJPEG streaming.
/// Extracts the encoding responsibility from the capture service so it can be reused
/// independently of the capture backend.
/// </summary>
internal static class JpegFallbackEncoder
{
    private static readonly ImageCodecInfo JpegCodec =
        ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

    /// <summary>
    /// Encodes a raw BGRA32 pixel buffer to a JPEG byte array.
    /// The buffer is pinned during encoding so no additional copy is needed.
    /// </summary>
    /// <param name="bgra32">Raw pixel data — 4 bytes per pixel in BGRA order, row-major, no padding.</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="height">Frame height in pixels.</param>
    /// <param name="quality">JPEG quality level, clamped to 1–100.</param>
    public static byte[] Encode(byte[] bgra32, int width, int height, int quality)
    {
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

        var handle = GCHandle.Alloc(bgra32, GCHandleType.Pinned);
        try
        {
            using var bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());
            using var ms = new MemoryStream();
            bitmap.Save(ms, JpegCodec, encoderParams);
            return ms.ToArray();
        }
        finally
        {
            handle.Free();
        }
    }
}
