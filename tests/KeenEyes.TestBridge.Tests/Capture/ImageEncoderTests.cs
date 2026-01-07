using KeenEyes.TestBridge.Capture;
using StbImageSharp;

namespace KeenEyes.TestBridge.Tests.Capture;

public class ImageEncoderTests
{
    #region PNG Encoding

    [Fact]
    public void EncodePng_WithValidPixels_ProducesValidPng()
    {
        // Create a simple 2x2 red image
        var pixels = CreateSolidColorImage(2, 2, 255, 0, 0, 255);

        var encoded = ImageEncoder.EncodePng(pixels, 2, 2);

        encoded.ShouldNotBeEmpty();
        // Verify PNG magic bytes
        encoded[0].ShouldBe((byte)0x89);
        encoded[1].ShouldBe((byte)'P');
        encoded[2].ShouldBe((byte)'N');
        encoded[3].ShouldBe((byte)'G');
    }

    [Fact]
    public void EncodePng_RoundTrips_WithCorrectPixels()
    {
        const int width = 4;
        const int height = 4;
        var originalPixels = CreateGradientImage(width, height);

        var encoded = ImageEncoder.EncodePng(originalPixels, width, height);

        // Decode and verify
        using var stream = new MemoryStream(encoded);
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        result.Width.ShouldBe(width);
        result.Height.ShouldBe(height);
        result.Data.Length.ShouldBe(originalPixels.Length);

        // Verify pixels match
        for (var i = 0; i < originalPixels.Length; i++)
        {
            result.Data[i].ShouldBe(originalPixels[i], $"Pixel mismatch at index {i}");
        }
    }

    #endregion

    #region JPEG Encoding

    [Fact]
    public void EncodeJpeg_WithValidPixels_ProducesValidJpeg()
    {
        var pixels = CreateSolidColorImage(2, 2, 0, 255, 0, 255);

        var encoded = ImageEncoder.EncodeJpeg(pixels, 2, 2);

        encoded.ShouldNotBeEmpty();
        // Verify JPEG magic bytes (FFD8FF)
        encoded[0].ShouldBe((byte)0xFF);
        encoded[1].ShouldBe((byte)0xD8);
        encoded[2].ShouldBe((byte)0xFF);
    }

    [Fact]
    public void EncodeJpeg_DropsAlphaChannel_ProducesRgb()
    {
        const int width = 4;
        const int height = 4;
        // Create image with alpha channel
        var pixels = CreateSolidColorImage(width, height, 100, 150, 200, 128);

        var encoded = ImageEncoder.EncodeJpeg(pixels, width, height);

        // Decode and verify - JPEG should have RGB only
        using var stream = new MemoryStream(encoded);
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);

        result.Comp.ShouldBe(ColorComponents.RedGreenBlue);
    }

    [Fact]
    public void EncodeJpeg_WithCustomQuality_ProducesValidJpeg()
    {
        var pixels = CreateGradientImage(8, 8);

        var highQuality = ImageEncoder.EncodeJpeg(pixels, 8, 8, 100);
        var lowQuality = ImageEncoder.EncodeJpeg(pixels, 8, 8, 10);

        highQuality.ShouldNotBeEmpty();
        lowQuality.ShouldNotBeEmpty();
        // Higher quality typically produces larger files
        highQuality.Length.ShouldBeGreaterThan(lowQuality.Length);
    }

    #endregion

    #region BMP Encoding

    [Fact]
    public void EncodeBmp_WithValidPixels_ProducesValidBmp()
    {
        var pixels = CreateSolidColorImage(2, 2, 0, 0, 255, 255);

        var encoded = ImageEncoder.EncodeBmp(pixels, 2, 2);

        encoded.ShouldNotBeEmpty();
        // Verify BMP magic bytes (BM)
        encoded[0].ShouldBe((byte)'B');
        encoded[1].ShouldBe((byte)'M');
    }

    #endregion

    #region Format Selection

    [Fact]
    public void Encode_WithPngFormat_CallsEncodePng()
    {
        var pixels = CreateSolidColorImage(2, 2, 255, 255, 255, 255);

        var encoded = ImageEncoder.Encode(pixels, 2, 2, ImageFormat.Png);

        // Verify PNG magic bytes
        encoded[0].ShouldBe((byte)0x89);
    }

    [Fact]
    public void Encode_WithJpegFormat_CallsEncodeJpeg()
    {
        var pixels = CreateSolidColorImage(2, 2, 255, 255, 255, 255);

        var encoded = ImageEncoder.Encode(pixels, 2, 2, ImageFormat.Jpeg);

        // Verify JPEG magic bytes
        encoded[0].ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Encode_WithBmpFormat_CallsEncodeBmp()
    {
        var pixels = CreateSolidColorImage(2, 2, 255, 255, 255, 255);

        var encoded = ImageEncoder.Encode(pixels, 2, 2, ImageFormat.Bmp);

        // Verify BMP magic bytes
        encoded[0].ShouldBe((byte)'B');
    }

    [Fact]
    public void Encode_WithInvalidFormat_ThrowsArgumentOutOfRange()
    {
        var pixels = CreateSolidColorImage(2, 2, 255, 255, 255, 255);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ImageEncoder.Encode(pixels, 2, 2, (ImageFormat)999));
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateSolidColorImage(int width, int height, byte r, byte g, byte b, byte a)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }
        return pixels;
    }

    private static byte[] CreateGradientImage(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i] = (byte)(x * 255 / Math.Max(1, width - 1));
                pixels[i + 1] = (byte)(y * 255 / Math.Max(1, height - 1));
                pixels[i + 2] = 128;
                pixels[i + 3] = 255;
            }
        }
        return pixels;
    }

    #endregion
}
