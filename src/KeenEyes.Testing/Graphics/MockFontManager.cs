using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// A mock implementation of <see cref="IFontManager"/> for testing text layout
/// and rendering without real fonts.
/// </summary>
/// <remarks>
/// <para>
/// MockFontManager provides configurable font metrics for testing text layout code
/// without loading real font files. It tracks all font operations for verification.
/// </para>
/// <para>
/// Configure <see cref="DefaultCharWidth"/>, <see cref="DefaultLineHeight"/>, etc.
/// to control text measurement behavior in tests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var fontManager = new MockFontManager();
/// fontManager.DefaultCharWidth = 10f;
/// fontManager.DefaultLineHeight = 20f;
///
/// var font = fontManager.LoadFont("test.ttf", 16);
/// var size = fontManager.MeasureText(font, "Hello");
///
/// size.X.Should().Be(50); // 5 chars * 10 width
/// </code>
/// </example>
public sealed class MockFontManager : IFontManager
{
    private int nextHandleId = 1;
    private bool disposed;

    /// <summary>
    /// Gets the dictionary of loaded fonts by handle.
    /// </summary>
    public Dictionary<FontHandle, MockFontInfo> Fonts { get; } = [];

    /// <summary>
    /// Gets the list of font paths that were loaded.
    /// </summary>
    public List<string> LoadedFontPaths { get; } = [];

    #region Configurable Metrics

    /// <summary>
    /// Gets or sets the default character width for measurement.
    /// </summary>
    public float DefaultCharWidth { get; set; } = 8f;

    /// <summary>
    /// Gets or sets the default line height.
    /// </summary>
    public float DefaultLineHeight { get; set; } = 16f;

    /// <summary>
    /// Gets or sets the default ascender (height above baseline).
    /// </summary>
    public float DefaultAscent { get; set; } = 12f;

    /// <summary>
    /// Gets or sets the default descender (depth below baseline).
    /// </summary>
    public float DefaultDescent { get; set; } = 4f;

    /// <summary>
    /// Gets or sets whether font loading should fail.
    /// </summary>
    public bool ShouldFailLoad { get; set; }

    /// <summary>
    /// Gets or sets custom character widths for specific characters.
    /// </summary>
    public Dictionary<char, float> CharacterWidths { get; } = [];

    /// <summary>
    /// Gets or sets custom kerning pairs.
    /// </summary>
    public Dictionary<(char First, char Second), float> KerningPairs { get; } = [];

    #endregion

    #region IResourceManager Implementation

    /// <inheritdoc />
    public int Count => Fonts.Count;

    /// <inheritdoc />
    public int Capacity => Fonts.Count;

    /// <inheritdoc />
    public bool IsValid(FontHandle handle)
    {
        return Fonts.ContainsKey(handle);
    }

    /// <inheritdoc />
    public bool Release(FontHandle handle)
    {
        return Fonts.Remove(handle);
    }

    /// <inheritdoc />
    public void ReleaseAll()
    {
        Fonts.Clear();
    }

    #endregion

    #region Font Loading

    /// <inheritdoc />
    public FontHandle LoadFont(string path, float size)
    {
        if (ShouldFailLoad)
        {
            throw new InvalidOperationException("Mock font loading failed (ShouldFailLoad = true)");
        }

        LoadedFontPaths.Add(path);
        var handle = new FontHandle(nextHandleId++);
        Fonts[handle] = new MockFontInfo(path, null, size);
        return handle;
    }

    /// <inheritdoc />
    public FontHandle LoadFontFromMemory(ReadOnlySpan<byte> data, float size, string? name = null)
    {
        if (ShouldFailLoad)
        {
            throw new InvalidOperationException("Mock font loading failed (ShouldFailLoad = true)");
        }

        var handle = new FontHandle(nextHandleId++);
        Fonts[handle] = new MockFontInfo(null, name ?? "memory-font", size) { DataSize = data.Length };
        return handle;
    }

    /// <inheritdoc />
    public FontHandle CreateSizedFont(FontHandle baseFont, float newSize)
    {
        if (!Fonts.TryGetValue(baseFont, out var baseInfo))
        {
            throw new InvalidOperationException("Base font handle is invalid.");
        }

        var handle = new FontHandle(nextHandleId++);
        Fonts[handle] = new MockFontInfo(baseInfo.Path, baseInfo.Name, newSize) { BaseFont = baseFont };
        return handle;
    }

    #endregion

    #region Font Metrics

    /// <inheritdoc />
    public float GetFontSize(FontHandle font)
    {
        return Fonts.TryGetValue(font, out var info) ? info.Size : 0;
    }

    /// <inheritdoc />
    public float GetLineHeight(FontHandle font)
    {
        if (Fonts.TryGetValue(font, out var info) && info.CustomLineHeight.HasValue)
        {
            return info.CustomLineHeight.Value;
        }

        return DefaultLineHeight;
    }

    /// <inheritdoc />
    public float GetBaseline(FontHandle font)
    {
        return GetAscent(font);
    }

    /// <inheritdoc />
    public float GetAscent(FontHandle font)
    {
        if (Fonts.TryGetValue(font, out var info) && info.CustomAscent.HasValue)
        {
            return info.CustomAscent.Value;
        }

        return DefaultAscent;
    }

    /// <inheritdoc />
    public float GetDescent(FontHandle font)
    {
        if (Fonts.TryGetValue(font, out var info) && info.CustomDescent.HasValue)
        {
            return info.CustomDescent.Value;
        }

        return DefaultDescent;
    }

    #endregion

    #region Text Measurement

    /// <inheritdoc />
    public Vector2 MeasureText(FontHandle font, ReadOnlySpan<char> text)
    {
        var width = MeasureTextWidth(font, text);
        var height = GetLineHeight(font);

        // Count newlines for height
        var lines = 1;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                lines++;
            }
        }

        return new Vector2(width, height * lines);
    }

    /// <inheritdoc />
    public float MeasureTextWidth(FontHandle font, ReadOnlySpan<char> text)
    {
        float width = 0;
        float maxLineWidth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\n')
            {
                maxLineWidth = Math.Max(maxLineWidth, width);
                width = 0;
                continue;
            }

            width += GetCharacterAdvance(font, c);

            // Add kerning if not last character
            if (i < text.Length - 1 && text[i + 1] != '\n')
            {
                width += GetKerning(font, c, text[i + 1]);
            }
        }

        return Math.Max(maxLineWidth, width);
    }

    /// <inheritdoc />
    public float GetCharacterAdvance(FontHandle font, char character)
    {
        if (CharacterWidths.TryGetValue(character, out var width))
        {
            return width;
        }

        return DefaultCharWidth;
    }

    /// <inheritdoc />
    public float GetKerning(FontHandle font, char first, char second)
    {
        if (KerningPairs.TryGetValue((first, second), out var kerning))
        {
            return kerning;
        }

        return 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<int> CalculateWordWrap(FontHandle font, ReadOnlySpan<char> text, float maxWidth)
    {
        var breakPoints = new List<int>();
        float lineWidth = 0;
        int lastSpaceIndex = -1;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '\n')
            {
                breakPoints.Add(i);
                lineWidth = 0;
                lastSpaceIndex = -1;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                lastSpaceIndex = i;
            }

            float charWidth = GetCharacterAdvance(font, c);
            if (i < text.Length - 1)
            {
                charWidth += GetKerning(font, c, text[i + 1]);
            }

            if (lineWidth + charWidth > maxWidth && lineWidth > 0)
            {
                // Need to wrap
                if (lastSpaceIndex > (breakPoints.Count > 0 ? breakPoints[^1] : -1))
                {
                    // Wrap at last space
                    breakPoints.Add(lastSpaceIndex);
                    lineWidth = 0;
                    // Remeasure from last space to current position
                    for (int j = lastSpaceIndex + 1; j <= i; j++)
                    {
                        lineWidth += GetCharacterAdvance(font, text[j]);
                    }
                }
                else
                {
                    // No space to wrap at, break at current position
                    breakPoints.Add(i);
                    lineWidth = charWidth;
                }
            }
            else
            {
                lineWidth += charWidth;
            }
        }

        return breakPoints;
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Sets custom metrics for a specific font.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <param name="lineHeight">The custom line height.</param>
    /// <param name="ascent">The custom ascent.</param>
    /// <param name="descent">The custom descent.</param>
    public void SetFontMetrics(FontHandle font, float? lineHeight = null, float? ascent = null, float? descent = null)
    {
        if (Fonts.TryGetValue(font, out var info))
        {
            info.CustomLineHeight = lineHeight;
            info.CustomAscent = ascent;
            info.CustomDescent = descent;
        }
    }

    /// <summary>
    /// Resets all state.
    /// </summary>
    public void Reset()
    {
        Fonts.Clear();
        LoadedFontPaths.Clear();
        CharacterWidths.Clear();
        KerningPairs.Clear();
        nextHandleId = 1;
        DefaultCharWidth = 8f;
        DefaultLineHeight = 16f;
        DefaultAscent = 12f;
        DefaultDescent = 4f;
        ShouldFailLoad = false;
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            ReleaseAll();
        }
    }
}

/// <summary>
/// Information about a loaded mock font.
/// </summary>
/// <param name="Path">The file path the font was loaded from.</param>
/// <param name="Name">The font name.</param>
/// <param name="Size">The font size.</param>
public sealed class MockFontInfo(string? Path, string? Name, float Size)
{
    /// <summary>
    /// Gets the file path the font was loaded from.
    /// </summary>
    public string? Path { get; } = Path;

    /// <summary>
    /// Gets the font name.
    /// </summary>
    public string? Name { get; } = Name;

    /// <summary>
    /// Gets the font size.
    /// </summary>
    public float Size { get; } = Size;

    /// <summary>
    /// Gets or sets the base font handle if this was created via CreateSizedFont.
    /// </summary>
    public FontHandle? BaseFont { get; set; }

    /// <summary>
    /// Gets or sets the data size if loaded from memory.
    /// </summary>
    public int? DataSize { get; set; }

    /// <summary>
    /// Gets or sets custom line height for this font.
    /// </summary>
    public float? CustomLineHeight { get; set; }

    /// <summary>
    /// Gets or sets custom ascent for this font.
    /// </summary>
    public float? CustomAscent { get; set; }

    /// <summary>
    /// Gets or sets custom descent for this font.
    /// </summary>
    public float? CustomDescent { get; set; }
}
