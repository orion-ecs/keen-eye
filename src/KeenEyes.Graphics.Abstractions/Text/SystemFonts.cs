using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Locates a usable system font file for applications that need a default UI font.
/// </summary>
/// <remarks>
/// <para>
/// Selection is by <em>loadability</em>, not by mere existence. A candidate path that
/// exists but cannot be handed to a rasterizer is skipped and the search continues with
/// the next candidate. This matters on macOS, where most system fonts ship as TrueType
/// Collections (<c>.ttc</c>) that single-face rasterizers reject: picking the first
/// existing path would let an unusable file shadow every later candidate that works.
/// </para>
/// <para>
/// The candidate lists are deliberately short and ordered — single-font files first —
/// rather than an exhaustive survey of every font an operating system might ship. The
/// usability check, not the list, is what keeps this correct when a platform relocates
/// or reformats its fonts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var fontPath = SystemFonts.FindFirstUsable();
/// if (fontPath is null)
/// {
///     Console.WriteLine("No usable system font found; text will not be drawn.");
/// }
/// else
/// {
///     var font = fontManager.LoadFont(fontPath, 16f);
/// }
/// </code>
/// </example>
public static class SystemFonts
{
    /// <summary>
    /// The <c>ttcf</c> tag, big-endian, that begins every TrueType Collection file.
    /// </summary>
    private const uint CollectionTag = 0x74746366;

    /// <summary>
    /// Leading 4-byte tags that identify a single-face font a rasterizer can open at
    /// offset zero: <c>0x00010000</c> (TrueType outlines), <c>true</c> (legacy Macintosh
    /// TrueType), <c>typ1</c> (PostScript Type 1 in an sfnt wrapper), and <c>OTTO</c>
    /// (OpenType with CFF outlines).
    /// </summary>
    private static readonly uint[] singleFaceTags =
    [
        0x00010000,
        0x74727565,
        0x74797031,
        0x4F54544F,
    ];

    /// <summary>
    /// Candidate font paths for Windows, in preference order.
    /// </summary>
    private static readonly string[] windowsCandidates =
    [
        @"C:\Windows\Fonts\segoeui.ttf",
        @"C:\Windows\Fonts\arial.ttf",
        @"C:\Windows\Fonts\calibri.ttf",
        @"C:\Windows\Fonts\verdana.ttf",
        @"C:\Windows\Fonts\tahoma.ttf",
    ];

    /// <summary>
    /// Candidate font paths for macOS, in preference order.
    /// </summary>
    /// <remarks>
    /// Single-font <c>.ttf</c> files come first. Modern macOS keeps most system faces in
    /// <c>/System/Library/Fonts</c> as TrueType Collections and puts the classic
    /// single-font files under <c>/System/Library/Fonts/Supplemental</c>. The collections
    /// are listed last so a rasterizer that gains collection support can still reach them,
    /// while <see cref="IsUsable(string)"/> skips them until then.
    /// </remarks>
    private static readonly string[] macOSCandidates =
    [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Verdana.ttf",
        "/System/Library/Fonts/Supplemental/Tahoma.ttf",
        "/Library/Fonts/Arial.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
    ];

    /// <summary>
    /// Candidate font paths for Linux and other Unix-like systems, in preference order.
    /// </summary>
    private static readonly string[] linuxCandidates =
    [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/TTF/DejaVuSans.ttf",
        "/usr/share/fonts/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
    ];

    /// <summary>
    /// Gets the candidate font paths for the operating system this process is running on,
    /// in preference order.
    /// </summary>
    public static IReadOnlyList<string> Candidates => GetCandidates(CurrentPlatform());

    /// <summary>
    /// Gets the candidate font paths for a specific operating system, in preference order.
    /// </summary>
    /// <param name="platform">The operating system whose candidates to return.</param>
    /// <returns>
    /// The candidate paths, most preferred first. Platforms other than
    /// <see cref="OSPlatform.Windows"/> and <see cref="OSPlatform.OSX"/> receive the
    /// freedesktop-style list used on Linux.
    /// </returns>
    /// <remarks>
    /// The paths are returned unfiltered: they are not checked for existence or
    /// loadability. Use <see cref="FindFirstUsable()"/> to get a path that can actually
    /// be loaded.
    /// </remarks>
    public static IReadOnlyList<string> GetCandidates(OSPlatform platform)
    {
        if (platform == OSPlatform.Windows)
        {
            return windowsCandidates;
        }

        if (platform == OSPlatform.OSX)
        {
            return macOSCandidates;
        }

        return linuxCandidates;
    }

    /// <summary>
    /// Finds the first candidate font for the current operating system that can be loaded.
    /// </summary>
    /// <returns>The path of the first usable font, or <see langword="null"/> if none is usable.</returns>
    public static string? FindFirstUsable() => FindFirstUsable(Candidates);

    /// <summary>
    /// Finds the first path in a caller-supplied list that can be loaded.
    /// </summary>
    /// <param name="candidates">The candidate paths to test, most preferred first.</param>
    /// <returns>The path of the first usable font, or <see langword="null"/> if none is usable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="candidates"/> is null.</exception>
    public static string? FindFirstUsable(IEnumerable<string> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        foreach (var path in candidates)
        {
            if (IsUsable(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a font file exists, is readable, and holds a single face that a
    /// rasterizer can open at offset zero.
    /// </summary>
    /// <param name="path">The font file path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the file begins with a single-face sfnt tag;
    /// <see langword="false"/> when it is missing, unreadable, too short to identify, a
    /// TrueType Collection, or any other format the rasterizer cannot open directly.
    /// </returns>
    /// <remarks>
    /// This inspects only the 4-byte tag at the start of the file, so it is cheap enough
    /// to run over a whole candidate list. It rules out the formats that fail at load
    /// time; it does not prove that the rest of the file is well-formed.
    /// </remarks>
    public static bool IsUsable(string path)
    {
        if (!TryReadTag(path, out var tag))
        {
            return false;
        }

        return Array.IndexOf(singleFaceTags, tag) >= 0;
    }

    /// <summary>
    /// Determines whether a font file is a TrueType Collection.
    /// </summary>
    /// <param name="path">The font file path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the file begins with the <c>ttcf</c> tag; otherwise
    /// <see langword="false"/>, including when the file is missing or unreadable.
    /// </returns>
    public static bool IsFontCollection(string path)
        => TryReadTag(path, out var tag) && tag == CollectionTag;

    /// <summary>
    /// Determines whether in-memory font data is a TrueType Collection.
    /// </summary>
    /// <param name="data">The font file bytes to test.</param>
    /// <returns>
    /// <see langword="true"/> when the data begins with the <c>ttcf</c> tag; otherwise
    /// <see langword="false"/>, including when the data is shorter than four bytes.
    /// </returns>
    public static bool IsFontCollection(ReadOnlySpan<byte> data)
        => data.Length >= 4 && BinaryPrimitives.ReadUInt32BigEndian(data) == CollectionTag;

    /// <summary>
    /// Builds the message used when a TrueType Collection is handed to a loader that only
    /// supports single-face fonts.
    /// </summary>
    /// <param name="path">The font file path or name to name in the message.</param>
    /// <returns>The diagnostic message.</returns>
    /// <remarks>
    /// The wording states the observation (the file's leading tag) and the remedy, and
    /// stops short of guessing why the caller reached for a collection.
    /// </remarks>
    public static string DescribeUnsupportedCollection(string path)
        => $"Font '{path}' is a TrueType Collection: its first four bytes are the 'ttcf' tag, "
            + "so it holds several faces rather than one. Collections are not supported. "
            + "Use a single-font .ttf or .otf file instead - on macOS the single-font files "
            + "live in /System/Library/Fonts/Supplemental, and SystemFonts.FindFirstUsable() "
            + "picks one automatically.";

    /// <summary>
    /// Reads the 4-byte tag at the start of a file.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="tag">The tag, interpreted big-endian, when the read succeeds.</param>
    /// <returns><see langword="true"/> when four bytes were read; otherwise <see langword="false"/>.</returns>
    private static bool TryReadTag(string path, out uint tag)
    {
        tag = 0;

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[4];

            if (stream.ReadAtLeast(header, 4, throwOnEndOfStream: false) < 4)
            {
                return false;
            }

            tag = BinaryPrimitives.ReadUInt32BigEndian(header);
            return true;
        }
        catch (IOException)
        {
            // Missing, locked, or a directory: not a font we can use. Fall through so the
            // caller tries the next candidate rather than failing the whole search.
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the operating system this process is running on, as an <see cref="OSPlatform"/>.
    /// </summary>
    /// <returns>
    /// <see cref="OSPlatform.Windows"/>, <see cref="OSPlatform.OSX"/>, or
    /// <see cref="OSPlatform.Linux"/> for everything else.
    /// </returns>
    private static OSPlatform CurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        return OSPlatform.Linux;
    }
}
