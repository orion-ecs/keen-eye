using System.Runtime.InteropServices;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="SystemFonts"/>, the shared locator that picks a default UI font.
/// </summary>
/// <remarks>
/// <para>
/// Regression coverage for #1365. Every windowed sample used to pick the first candidate path
/// that <c>File.Exists</c> returned true for, and on macOS the first candidate was
/// <c>/System/Library/Fonts/Helvetica.ttc</c> - a TrueType Collection, which the single-face
/// rasterizer rejects with a bare <c>stbtt_InitFont failed</c>. Existence is not loadability, so
/// an unusable file shadowed every later candidate that would have worked.
/// </para>
/// <para>
/// The macOS paths do not exist on the Linux and Windows machines that run this suite, so the
/// fall-through behaviour is proven against synthetic files rather than real system fonts, and
/// the platform ordering is asserted as a pure list property.
/// </para>
/// </remarks>
public sealed class SystemFontsTests : IDisposable
{
    /// <summary>
    /// The first four bytes of every TrueType Collection.
    /// </summary>
    private static readonly byte[] collectionMagic = [0x74, 0x74, 0x63, 0x66];

    private readonly string tempDirectory;

    /// <summary>
    /// Creates a private scratch directory for the synthetic font files.
    /// </summary>
    public SystemFontsTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"keeneyes-fonts-{Guid.NewGuid():N}");
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

    #region IsUsable Tests

    [Fact]
    public void IsUsable_WithTrueTypeCollection_ReturnsFalse()
    {
        var collection = WriteCollection("Helvetica.ttc");

        Assert.False(SystemFonts.IsUsable(collection));
    }

    [Fact]
    public void IsUsable_WithRealTrueTypeFont_ReturnsTrue()
    {
        var font = FindInstalledTrueTypeFont();
        Assert.SkipWhen(font is null, "No installed .ttf font found on this machine.");

        Assert.True(SystemFonts.IsUsable(font!));
    }

    [Fact]
    public void IsUsable_WithNonexistentPath_ReturnsFalse()
    {
        Assert.False(SystemFonts.IsUsable(Path.Combine(tempDirectory, "no-such-font.ttf")));
    }

    [Fact]
    public void IsUsable_WithEmptyPath_ReturnsFalse()
    {
        Assert.False(SystemFonts.IsUsable(string.Empty));
    }

    [Fact]
    public void IsUsable_WithTruncatedFile_ReturnsFalse()
    {
        // Shorter than the 4-byte tag: unidentifiable, so it must be skipped rather than returned.
        var truncated = Path.Combine(tempDirectory, "truncated.ttf");
        File.WriteAllBytes(truncated, [0x00, 0x01]);

        Assert.False(SystemFonts.IsUsable(truncated));
    }

    [Fact]
    public void IsUsable_WithNonFontFile_ReturnsFalse()
    {
        // A .ttf extension over WOFF2 content: the extension lies, the leading tag does not.
        var woff2 = Path.Combine(tempDirectory, "webfont.ttf");
        File.WriteAllBytes(woff2, "wOF2placeholder"u8.ToArray());

        Assert.False(SystemFonts.IsUsable(woff2));
    }

    [Fact]
    public void IsUsable_WithDirectory_ReturnsFalse()
    {
        // Directories exist, which is exactly the trap the old File.Exists-free check would
        // have fallen into if the candidate list ever named one.
        Assert.False(SystemFonts.IsUsable(tempDirectory));
    }

    [Theory]
    // The four leading tags a single-face rasterizer can open at offset zero.
    [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00 })]  // TrueType outlines
    [InlineData(new byte[] { 0x74, 0x72, 0x75, 0x65 })]  // "true" - legacy Macintosh TrueType
    [InlineData(new byte[] { 0x74, 0x79, 0x70, 0x31 })]  // "typ1" - Type 1 in an sfnt wrapper
    [InlineData(new byte[] { 0x4F, 0x54, 0x54, 0x4F })]  // "OTTO" - OpenType with CFF outlines
    public void IsUsable_WithSingleFaceTag_ReturnsTrue(byte[] tag)
    {
        var path = Path.Combine(tempDirectory, $"face-{Convert.ToHexString(tag)}.ttf");
        File.WriteAllBytes(path, [.. tag, 0x00, 0x00, 0x00, 0x00]);

        Assert.True(SystemFonts.IsUsable(path));
    }

    #endregion

    #region IsFontCollection Tests

    [Fact]
    public void IsFontCollection_WithCollectionFile_ReturnsTrue()
    {
        Assert.True(SystemFonts.IsFontCollection(WriteCollection("Collection.ttc")));
    }

    [Fact]
    public void IsFontCollection_WithSingleFaceFile_ReturnsFalse()
    {
        var single = Path.Combine(tempDirectory, "single.ttf");
        File.WriteAllBytes(single, [0x00, 0x01, 0x00, 0x00, 0x00, 0x00]);

        Assert.False(SystemFonts.IsFontCollection(single));
    }

    [Fact]
    public void IsFontCollection_WithMissingFile_ReturnsFalse()
    {
        Assert.False(SystemFonts.IsFontCollection(Path.Combine(tempDirectory, "absent.ttc")));
    }

    [Fact]
    public void IsFontCollection_WithCollectionData_ReturnsTrue()
    {
        Assert.True(SystemFonts.IsFontCollection(new byte[] { 0x74, 0x74, 0x63, 0x66, 0x00, 0x01 }));
    }

    [Fact]
    public void IsFontCollection_WithDataShorterThanTheTag_ReturnsFalse()
    {
        Assert.False(SystemFonts.IsFontCollection(new byte[] { 0x74, 0x74, 0x63 }));
    }

    #endregion

    #region FindFirstUsable Tests

    /// <summary>
    /// The behavioural heart of #1365: a candidate that exists but cannot be loaded must not
    /// shadow a later candidate that can.
    /// </summary>
    /// <remarks>
    /// This is the case that reproduces the macOS report. The old implementation returned the
    /// first path <c>File.Exists</c> accepted, which here would be the collection.
    /// </remarks>
    [Fact]
    public void FindFirstUsable_WithNonexistentThenCollectionThenValidFont_ReturnsTheValidFont()
    {
        var validFont = FindInstalledTrueTypeFont();
        Assert.SkipWhen(validFont is null, "No installed .ttf font found on this machine.");

        var missing = Path.Combine(tempDirectory, "missing.ttf");
        var collection = WriteCollection("Helvetica.ttc");

        var found = SystemFonts.FindFirstUsable([missing, collection, validFont!]);

        Assert.Equal(validFont, found);
    }

    [Fact]
    public void FindFirstUsable_WithNoLoadableCandidate_ReturnsNull()
    {
        var missing = Path.Combine(tempDirectory, "missing.ttf");
        var collection = WriteCollection("OnlyCollection.ttc");

        Assert.Null(SystemFonts.FindFirstUsable([missing, collection]));
    }

    [Fact]
    public void FindFirstUsable_WithEmptyCandidateList_ReturnsNull()
    {
        Assert.Null(SystemFonts.FindFirstUsable([]));
    }

    [Fact]
    public void FindFirstUsable_WithNullCandidates_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SystemFonts.FindFirstUsable(null!));
    }

    [Fact]
    public void FindFirstUsable_PrefersAnEarlierUsableCandidateOverALaterOne()
    {
        var first = Path.Combine(tempDirectory, "first.ttf");
        var second = Path.Combine(tempDirectory, "second.ttf");
        File.WriteAllBytes(first, [0x00, 0x01, 0x00, 0x00]);
        File.WriteAllBytes(second, [0x00, 0x01, 0x00, 0x00]);

        Assert.Equal(first, SystemFonts.FindFirstUsable([first, second]));
    }

    /// <summary>
    /// Covers what the four call sites actually do: resolve a font on the machine running them.
    /// </summary>
    [Fact]
    public void FindFirstUsable_OnThisMachine_ResolvesALoadableFontWhenOneIsInstalled()
    {
        Assert.SkipWhen(
            FindInstalledTrueTypeFont() is null,
            "No installed .ttf font found on this machine.");

        var found = SystemFonts.FindFirstUsable();

        Assert.NotNull(found);
        Assert.True(SystemFonts.IsUsable(found));
        Assert.False(SystemFonts.IsFontCollection(found));
    }

    #endregion

    #region Candidate Ordering Tests

    /// <summary>
    /// The macOS ordering that unblocks the samples, asserted as a pure list property so it is
    /// verifiable on any platform.
    /// </summary>
    [Fact]
    public void GetCandidates_ForMacOS_ListsEverySingleFontFileBeforeAnyCollection()
    {
        var candidates = SystemFonts.GetCandidates(OSPlatform.OSX);

        var firstCollection = IndexOfFirst(candidates, p => p.EndsWith(".ttc", StringComparison.Ordinal));
        var lastSingleFont = IndexOfLast(candidates, p => !p.EndsWith(".ttc", StringComparison.Ordinal));

        Assert.True(firstCollection >= 0, "Expected the macOS list to still mention a .ttc.");
        Assert.True(lastSingleFont >= 0, "Expected the macOS list to offer single-font files.");
        Assert.True(
            lastSingleFont < firstCollection,
            $"Single-font candidates must precede collections. '{candidates[firstCollection]}' at "
            + $"index {firstCollection} comes before '{candidates[lastSingleFont]}' at index "
            + $"{lastSingleFont}, which is the ordering that made every macOS sample fail (#1365).");
    }

    [Fact]
    public void GetCandidates_ForMacOS_LeadsWithASupplementalSingleFontFile()
    {
        var candidates = SystemFonts.GetCandidates(OSPlatform.OSX);

        // Apple moved the classic single-font TTFs into Supplemental; the collections that
        // remain in /System/Library/Fonts are what the old list reached first.
        Assert.StartsWith("/System/Library/Fonts/Supplemental/", candidates[0], StringComparison.Ordinal);
        Assert.EndsWith(".ttf", candidates[0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(@"C:\Windows\Fonts\segoeui.ttf")]
    [InlineData(@"C:\Windows\Fonts\arial.ttf")]
    [InlineData(@"C:\Windows\Fonts\calibri.ttf")]
    [InlineData(@"C:\Windows\Fonts\verdana.ttf")]
    [InlineData(@"C:\Windows\Fonts\tahoma.ttf")]
    public void GetCandidates_ForWindows_KeepsTheExistingEntries(string expected)
    {
        Assert.Contains(expected, SystemFonts.GetCandidates(OSPlatform.Windows));
    }

    [Theory]
    [InlineData("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")]
    [InlineData("/usr/share/fonts/TTF/DejaVuSans.ttf")]
    public void GetCandidates_ForLinux_KeepsTheExistingEntries(string expected)
    {
        Assert.Contains(expected, SystemFonts.GetCandidates(OSPlatform.Linux));
    }

    [Fact]
    public void GetCandidates_ForWindows_LeadsWithSegoeUi()
    {
        Assert.Equal(@"C:\Windows\Fonts\segoeui.ttf", SystemFonts.GetCandidates(OSPlatform.Windows)[0]);
    }

    [Fact]
    public void GetCandidates_ForLinux_LeadsWithDejaVuSans()
    {
        Assert.Equal(
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            SystemFonts.GetCandidates(OSPlatform.Linux)[0]);
    }

    [Fact]
    public void GetCandidates_ForAnUnrecognizedPlatform_FallsBackToTheLinuxList()
    {
        Assert.Equal(
            SystemFonts.GetCandidates(OSPlatform.Linux),
            SystemFonts.GetCandidates(OSPlatform.FreeBSD));
    }

    [Fact]
    public void Candidates_MatchesTheListForTheRunningOperatingSystem()
    {
        var expected = OperatingSystem.IsWindows()
            ? SystemFonts.GetCandidates(OSPlatform.Windows)
            : OperatingSystem.IsMacOS()
                ? SystemFonts.GetCandidates(OSPlatform.OSX)
                : SystemFonts.GetCandidates(OSPlatform.Linux);

        Assert.Equal(expected, SystemFonts.Candidates);
    }

    #endregion

    #region Diagnostic Message Tests

    [Fact]
    public void DescribeUnsupportedCollection_NamesTheFileAndTheFormat()
    {
        var message = SystemFonts.DescribeUnsupportedCollection("/System/Library/Fonts/Helvetica.ttc");

        Assert.Contains("/System/Library/Fonts/Helvetica.ttc", message, StringComparison.Ordinal);
        Assert.Contains("collection", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ttcf", message, StringComparison.Ordinal);
        Assert.Contains("not supported", message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    /// <summary>
    /// Writes a file whose header is the <c>ttcf</c> tag, as a real TrueType Collection's is.
    /// </summary>
    /// <param name="fileName">The file name to create inside the scratch directory.</param>
    /// <returns>The full path of the created file.</returns>
    private string WriteCollection(string fileName)
    {
        var path = Path.Combine(tempDirectory, fileName);

        // ttcf, version 1.0, one face, offset 12 - enough structure to look like the real thing.
        File.WriteAllBytes(path, [.. collectionMagic, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]);

        return path;
    }

    /// <summary>
    /// Finds a real single-face TrueType font installed on the machine running the tests.
    /// </summary>
    /// <returns>The font path, or <see langword="null"/> when the machine has none.</returns>
    private static string? FindInstalledTrueTypeFont()
    {
        foreach (var candidate in SystemFonts.Candidates)
        {
            if (SystemFonts.IsUsable(candidate))
            {
                return candidate;
            }
        }

        // Fall back to a scan so a machine with fonts in a non-standard prefix still exercises
        // the positive case instead of skipping it.
        foreach (var root in (string[])["/usr/share/fonts", "/usr/local/share/fonts"])
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.ttf", SearchOption.AllDirectories))
            {
                if (SystemFonts.IsUsable(file))
                {
                    return file;
                }
            }
        }

        return null;
    }

    private static int IndexOfFirst(IReadOnlyList<string> values, Func<string, bool> predicate)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (predicate(values[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfLast(IReadOnlyList<string> values, Func<string, bool> predicate)
    {
        for (int i = values.Count - 1; i >= 0; i--)
        {
            if (predicate(values[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
