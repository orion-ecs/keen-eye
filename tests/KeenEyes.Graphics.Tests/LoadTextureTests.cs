// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for texture loading functionality in SilkGraphicsContext.
/// </summary>
public class LoadTextureTests : IDisposable
{
    private readonly string tempDir;

    public LoadTextureTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "KeenEyesTextureTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Validation Tests

    [Fact]
    public void LoadTexture_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();

        // Act & Assert
        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null values
        Assert.ThrowsAny<ArgumentException>(() => context.LoadTexture(null!));
    }

    [Fact]
    public void LoadTexture_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.LoadTexture(string.Empty));
    }

    [Fact]
    public void LoadTexture_WithWhitespacePath_ThrowsArgumentException()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.LoadTexture("   "));
    }

    [Fact]
    public void LoadTexture_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();
        var nonExistentPath = Path.Combine(tempDir, "does_not_exist.png");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => context.LoadTexture(nonExistentPath));
    }

    [Fact]
    public void LoadTexture_WithInvalidImageFile_ThrowsArgumentException()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();
        var invalidPath = Path.Combine(tempDir, "invalid.png");
        File.WriteAllText(invalidPath, "This is not an image file");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => context.LoadTexture(invalidPath));
        Assert.Contains("unsupported format", ex.Message.ToLowerInvariant());
    }

    #endregion

    #region Valid Image Loading Tests

    [Fact]
    public void LoadTexture_WithValidPngFile_ReturnsTextureHandle()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();
        var pngPath = CreateTestPngFile(16, 16, "test.png");

        // Act
        var handle = context.LoadTexture(pngPath);

        // Assert
        Assert.True(handle.Id > 0);
        Assert.Equal(16, handle.Width);
        Assert.Equal(16, handle.Height);
    }

    [Fact]
    public void LoadTexture_WithValidPngFile_CreatesCorrectDimensions()
    {
        // Arrange
        using var context = new MockSilkGraphicsContext();
        var pngPath = CreateTestPngFile(32, 24, "rect.png");

        // Act
        var handle = context.LoadTexture(pngPath);

        // Assert
        Assert.Equal(32, handle.Width);
        Assert.Equal(24, handle.Height);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid PNG file for testing.
    /// </summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>The full path to the created file.</returns>
    private string CreateTestPngFile(int width, int height, string fileName)
    {
        var path = Path.Combine(tempDir, fileName);

        // Create a minimal PNG file with solid color
        // PNG format: signature + IHDR chunk + IDAT chunk + IEND chunk
        using var stream = File.Create(path);

        // Use StbImageWriteSharp for simpler image creation
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;     // R
            pixels[i + 1] = 128; // G
            pixels[i + 2] = 64;  // B
            pixels[i + 3] = 255; // A
        }

        // Write PNG using StbImageWriteSharp
        var writer = new StbImageWriteSharp.ImageWriter();
        writer.WritePng(pixels, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);

        return path;
    }

    #endregion
}

/// <summary>
/// Mock SilkGraphicsContext for testing texture loading without OpenGL.
/// </summary>
internal sealed class MockSilkGraphicsContext : IDisposable
{
    private readonly MockGraphicsDevice device = new();
    private int nextTextureId = 1;
    private bool disposed;

    public MockGraphicsDevice Device => device;

    /// <summary>
    /// Loads a texture from a file path using StbImageSharp.
    /// This mirrors the implementation in SilkGraphicsContext.
    /// </summary>
    public KeenEyes.Graphics.Abstractions.TextureHandle LoadTexture(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Texture file not found.", path);
        }

        using var stream = File.OpenRead(path);
        StbImageSharp.ImageResult image;

        try
        {
            image = StbImageSharp.ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new ArgumentException($"Failed to load texture from '{path}'. The file may be corrupted or in an unsupported format.", nameof(path), ex);
        }

        return CreateTexture(image.Width, image.Height, image.Data);
    }

    /// <summary>
    /// Creates a texture from raw RGBA pixel data.
    /// </summary>
    public KeenEyes.Graphics.Abstractions.TextureHandle CreateTexture(int width, int height, ReadOnlySpan<byte> pixels)
    {
        // Simulate texture creation
        int id = nextTextureId++;
        device.GenTexture();
        device.BindTexture(KeenEyes.Graphics.Abstractions.TextureTarget.Texture2D, (uint)id);
        device.Calls.Add($"TexImage2D: {width}x{height}");
        device.BindTexture(KeenEyes.Graphics.Abstractions.TextureTarget.Texture2D, 0);

        return new KeenEyes.Graphics.Abstractions.TextureHandle(id, width, height);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            device.Dispose();
        }
    }
}
