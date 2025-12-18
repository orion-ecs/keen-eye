using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using FontStashSharp;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Text;

/// <summary>
/// Manages font resources using FontStashSharp for atlas generation and text measurement.
/// </summary>
/// <remarks>
/// <para>
/// This manager uses FontStashSharp to handle font loading, glyph atlas generation,
/// and text measurement. Each font file is loaded once as a FontSystem, and different
/// sizes create cached DynamicSpriteFont instances.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
internal sealed class SilkFontManager : IFontManager
{
    private readonly Dictionary<int, FontEntry> fonts = [];
    private readonly Dictionary<string, FontSystem> fontSystems = [];
    private int nextFontId;
    private bool disposed;

    /// <summary>
    /// Creates a new font manager.
    /// </summary>
    /// <param name="device">The graphics device for texture creation.</param>
    public SilkFontManager(IGraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        TextureManager = new FontStashTextureManager(device);
    }

    /// <summary>
    /// Gets the texture manager used by this font manager.
    /// </summary>
    internal FontStashTextureManager TextureManager { get; }

    #region IFontManager Implementation

    /// <inheritdoc />
    public FontHandle LoadFont(string path, float size)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Font path cannot be null or empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Font file not found: {path}", path);
        }

        // Get or create FontSystem for this file
        if (!fontSystems.TryGetValue(path, out var fontSystem))
        {
            fontSystem = CreateFontSystem();
            fontSystem.AddFont(File.ReadAllBytes(path));
            fontSystems[path] = fontSystem;
        }

        return CreateFontHandle(fontSystem, size);
    }

    /// <inheritdoc />
    public FontHandle LoadFontFromMemory(ReadOnlySpan<byte> data, float size, string? name = null)
    {
        var key = name ?? $"MemoryFont_{nextFontId}";

        if (!fontSystems.TryGetValue(key, out var fontSystem))
        {
            fontSystem = CreateFontSystem();
            fontSystem.AddFont(data.ToArray());
            fontSystems[key] = fontSystem;
        }

        return CreateFontHandle(fontSystem, size);
    }

    /// <inheritdoc />
    public FontHandle CreateSizedFont(FontHandle baseFont, float newSize)
    {
        if (!fonts.TryGetValue(baseFont.Id, out var baseEntry))
        {
            throw new ArgumentException("Invalid font handle.", nameof(baseFont));
        }

        return CreateFontHandle(baseEntry.FontSystem, newSize);
    }

    /// <inheritdoc />
    public float GetFontSize(FontHandle font)
    {
        return fonts.TryGetValue(font.Id, out var entry) ? entry.Size : 0;
    }

    /// <inheritdoc />
    public float GetLineHeight(FontHandle font)
    {
        if (!fonts.TryGetValue(font.Id, out var entry))
        {
            return 0;
        }

        return entry.SpriteFont.LineHeight;
    }

    /// <inheritdoc />
    public float GetBaseline(FontHandle font)
    {
        // FontStashSharp doesn't expose baseline directly; use ascent as approximation
        return GetAscent(font);
    }

    /// <inheritdoc />
    public float GetAscent(FontHandle font)
    {
        if (!fonts.TryGetValue(font.Id, out var entry))
        {
            return 0;
        }

        // FontStashSharp LineHeight includes ascent + descent + leading
        // Approximate ascent as ~80% of line height
        return entry.SpriteFont.LineHeight * 0.8f;
    }

    /// <inheritdoc />
    public float GetDescent(FontHandle font)
    {
        if (!fonts.TryGetValue(font.Id, out var entry))
        {
            return 0;
        }

        // Approximate descent as ~20% of line height
        return entry.SpriteFont.LineHeight * 0.2f;
    }

    /// <inheritdoc />
    public Vector2 MeasureText(FontHandle font, ReadOnlySpan<char> text)
    {
        if (!fonts.TryGetValue(font.Id, out var entry))
        {
            return Vector2.Zero;
        }

        var bounds = entry.SpriteFont.MeasureString(text.ToString());
        return new Vector2(bounds.X, bounds.Y);
    }

    /// <inheritdoc />
    public float MeasureTextWidth(FontHandle font, ReadOnlySpan<char> text)
    {
        return MeasureText(font, text).X;
    }

    /// <inheritdoc />
    public float GetCharacterAdvance(FontHandle font, char character)
    {
        if (!fonts.TryGetValue(font.Id, out var entry))
        {
            return 0;
        }

        var bounds = entry.SpriteFont.MeasureString(character.ToString());
        return bounds.X;
    }

    /// <inheritdoc />
    public float GetKerning(FontHandle font, char first, char second)
    {
        // FontStashSharp handles kerning internally during rendering
        return 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<int> CalculateWordWrap(FontHandle font, ReadOnlySpan<char> text, float maxWidth)
    {
        if (!fonts.TryGetValue(font.Id, out var entry))
        {
            return Array.Empty<int>();
        }

        var breaks = new List<int>();
        int lastSpace = -1;
        float currentWidth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == ' ' || c == '\t')
            {
                lastSpace = i;
            }

            if (c == '\n')
            {
                breaks.Add(i);
                currentWidth = 0;
                lastSpace = -1;
                continue;
            }

            var charWidth = entry.SpriteFont.MeasureString(c.ToString()).X;
            currentWidth += charWidth;

            if (currentWidth > maxWidth && lastSpace > 0)
            {
                breaks.Add(lastSpace);
                currentWidth = 0;

                // Measure from last space to current position
                for (int j = lastSpace + 1; j <= i; j++)
                {
                    currentWidth += entry.SpriteFont.MeasureString(text[j].ToString()).X;
                }

                lastSpace = -1;
            }
        }

        return breaks;
    }

    #endregion

    #region IResourceManager Implementation

    /// <inheritdoc />
    public int Count => fonts.Count;

    /// <inheritdoc />
    public int Capacity => int.MaxValue;

    /// <inheritdoc />
    public bool IsValid(FontHandle handle)
    {
        return handle.IsValid && fonts.ContainsKey(handle.Id);
    }

    /// <inheritdoc />
    public bool Release(FontHandle handle)
    {
        return fonts.Remove(handle.Id);
    }

    /// <inheritdoc />
    public void ReleaseAll()
    {
        fonts.Clear();
    }

    #endregion

    /// <summary>
    /// Gets the DynamicSpriteFont for a font handle.
    /// </summary>
    /// <param name="handle">The font handle.</param>
    /// <returns>The DynamicSpriteFont, or null if the handle is invalid.</returns>
    internal DynamicSpriteFont? GetSpriteFont(FontHandle handle)
    {
        return fonts.TryGetValue(handle.Id, out var entry) ? entry.SpriteFont : null;
    }

    private static FontSystem CreateFontSystem()
    {
        var settings = new FontSystemSettings
        {
            TextureWidth = 1024,
            TextureHeight = 1024,
            // Increase font resolution for sharper text
            FontResolutionFactor = 2,
            KernelWidth = 2,
            KernelHeight = 2,
        };

        return new FontSystem(settings);
    }

    private FontHandle CreateFontHandle(FontSystem fontSystem, float size)
    {
        var spriteFont = fontSystem.GetFont(size);
        var id = nextFontId++;

        fonts[id] = new FontEntry(fontSystem, spriteFont, size);

        return new FontHandle(id);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Dispose all font systems
        foreach (var fontSystem in fontSystems.Values)
        {
            fontSystem.Dispose();
        }

        fontSystems.Clear();
        fonts.Clear();
    }

    /// <summary>
    /// Stores font data for a handle.
    /// </summary>
    private sealed record FontEntry(FontSystem FontSystem, DynamicSpriteFont SpriteFont, float Size);
}
