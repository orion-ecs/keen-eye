using KeenEyes.Graphics.Silk.Text;
using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests the diagnostics <c>SilkFontManager</c> produces for fonts it cannot load.
/// </summary>
/// <remarks>
/// Regression coverage for #1365. Handing a TrueType Collection to FontStashSharp surfaced a
/// bare <c>Error: stbtt_InitFont failed</c> that named neither the file nor the format, which is
/// what left the macOS report guessing. Per the #1274 diagnostics contract the message must state
/// the observation and the remedy without asserting a cause that was never checked.
/// </remarks>
public sealed class SilkFontManagerCollectionTests : IDisposable
{
    /// <summary>
    /// A minimal TrueType Collection header: the <c>ttcf</c> tag, version 1.0, one face.
    /// </summary>
    private static readonly byte[] collectionBytes =
        [0x74, 0x74, 0x63, 0x66, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01];

    private readonly string tempDirectory;

    /// <summary>
    /// Creates a private scratch directory for the synthetic font files.
    /// </summary>
    public SilkFontManagerCollectionTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"keeneyes-fontmgr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover scratch directory must never fail a test run.
        }
    }

    [Fact]
    public void LoadFont_WithTrueTypeCollection_ThrowsNamingTheFileAndTheFormat()
    {
        using var device = new MockGraphicsDevice();
        using var manager = new SilkFontManager(device);

        var path = Path.Combine(tempDirectory, "Helvetica.ttc");
        File.WriteAllBytes(path, collectionBytes);

        var ex = Assert.Throws<NotSupportedException>(() => manager.LoadFont(path, 14f));

        Assert.Contains(path, ex.Message, StringComparison.Ordinal);
        Assert.Contains("collection", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFontFromMemory_WithTrueTypeCollection_ThrowsNamingTheFontAndTheFormat()
    {
        using var device = new MockGraphicsDevice();
        using var manager = new SilkFontManager(device);

        var ex = Assert.Throws<NotSupportedException>(
            () => manager.LoadFontFromMemory(collectionBytes, 14f, "Helvetica"));

        Assert.Contains("Helvetica", ex.Message, StringComparison.Ordinal);
        Assert.Contains("collection", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFont_WithDataTheRasterizerRejects_ThrowsNamingTheFile()
    {
        using var device = new MockGraphicsDevice();
        using var manager = new SilkFontManager(device);

        // Not a collection, just not a font: the boundary wrap must still name the file rather
        // than let FontStashSharp's anonymous message escape. An all-zero buffer is the safe
        // shape of "invalid": stb reads a table count of zero and gives up immediately, where
        // arbitrary bytes would send its table walk past the end of the buffer.
        var path = Path.Combine(tempDirectory, "not-a-font.ttf");
        File.WriteAllBytes(path, new byte[1024]);

        var ex = Assert.Throws<InvalidOperationException>(() => manager.LoadFont(path, 14f));

        Assert.Contains(path, ex.Message, StringComparison.Ordinal);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void LoadFont_WithMissingFile_ThrowsFileNotFoundException()
    {
        using var device = new MockGraphicsDevice();
        using var manager = new SilkFontManager(device);

        var path = Path.Combine(tempDirectory, "absent.ttf");

        // A distinct condition gets a distinct exception: "the file is not there" must not be
        // reported as "the format is unsupported".
        Assert.Throws<FileNotFoundException>(() => manager.LoadFont(path, 14f));
    }
}
