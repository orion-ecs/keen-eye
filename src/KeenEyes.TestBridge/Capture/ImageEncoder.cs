using StbImageWriteSharp;

namespace KeenEyes.TestBridge.Capture;

/// <summary>
/// Encodes raw pixel data to various image formats.
/// </summary>
internal static class ImageEncoder
{
    /// <summary>
    /// Default JPEG quality (0-100).
    /// </summary>
    private const int DefaultJpegQuality = 90;

    /// <summary>
    /// Encodes pixel data to the specified image format.
    /// </summary>
    /// <param name="pixels">Raw RGBA pixel data.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="format">The target image format.</param>
    /// <returns>Encoded image bytes.</returns>
    public static byte[] Encode(byte[] pixels, int width, int height, ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Png => EncodePng(pixels, width, height),
            ImageFormat.Jpeg => EncodeJpeg(pixels, width, height),
            ImageFormat.Bmp => EncodeBmp(pixels, width, height),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format")
        };
    }

    /// <summary>
    /// Encodes pixel data to PNG format.
    /// </summary>
    /// <param name="pixels">Raw RGBA pixel data.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>PNG encoded image bytes.</returns>
    public static byte[] EncodePng(byte[] pixels, int width, int height)
    {
        using var stream = new MemoryStream();
        var writer = new ImageWriter();
        writer.WritePng(pixels, width, height, ColorComponents.RedGreenBlueAlpha, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Encodes pixel data to JPEG format.
    /// </summary>
    /// <param name="pixels">Raw RGBA pixel data.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="quality">JPEG quality (0-100). Defaults to 90.</param>
    /// <returns>JPEG encoded image bytes.</returns>
    public static byte[] EncodeJpeg(byte[] pixels, int width, int height, int quality = DefaultJpegQuality)
    {
        // JPEG doesn't support alpha, convert RGBA to RGB
        var rgbPixels = ConvertRgbaToRgb(pixels);

        using var stream = new MemoryStream();
        var writer = new ImageWriter();
        writer.WriteJpg(rgbPixels, width, height, ColorComponents.RedGreenBlue, stream, quality);
        return stream.ToArray();
    }

    /// <summary>
    /// Encodes pixel data to BMP format.
    /// </summary>
    /// <param name="pixels">Raw RGBA pixel data.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>BMP encoded image bytes.</returns>
    public static byte[] EncodeBmp(byte[] pixels, int width, int height)
    {
        using var stream = new MemoryStream();
        var writer = new ImageWriter();
        writer.WriteBmp(pixels, width, height, ColorComponents.RedGreenBlueAlpha, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Converts RGBA pixel data to RGB by removing the alpha channel.
    /// </summary>
    /// <param name="rgbaPixels">RGBA pixel data (4 bytes per pixel).</param>
    /// <returns>RGB pixel data (3 bytes per pixel).</returns>
    private static byte[] ConvertRgbaToRgb(byte[] rgbaPixels)
    {
        var pixelCount = rgbaPixels.Length / 4;
        var rgbPixels = new byte[pixelCount * 3];

        for (var i = 0; i < pixelCount; i++)
        {
            var rgbaOffset = i * 4;
            var rgbOffset = i * 3;
            rgbPixels[rgbOffset] = rgbaPixels[rgbaOffset];         // R
            rgbPixels[rgbOffset + 1] = rgbaPixels[rgbaOffset + 1]; // G
            rgbPixels[rgbOffset + 2] = rgbaPixels[rgbaOffset + 2]; // B
            // Skip alpha
        }

        return rgbPixels;
    }
}
