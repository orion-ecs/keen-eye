namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// An opaque handle to a font resource.
/// </summary>
/// <remarks>
/// <para>
/// Font handles are returned by <see cref="IFontManager"/> when loading fonts
/// and must be used to reference the font in text rendering and measurement operations.
/// </para>
/// <para>
/// Handles are opaque identifiers that avoid exposing backend-specific resource types,
/// enabling portability across different font rendering implementations (stb_truetype, FreeType, etc.).
/// </para>
/// </remarks>
/// <param name="Id">The internal identifier for this font resource.</param>
public readonly record struct FontHandle(int Id)
{
    /// <summary>
    /// An invalid font handle representing no font.
    /// </summary>
    public static readonly FontHandle Invalid = new(-1);

    /// <summary>
    /// Gets whether this handle refers to a valid font resource.
    /// </summary>
    /// <remarks>
    /// A valid handle has a non-negative ID. Note that a valid handle does not guarantee
    /// the resource still exists - it may have been disposed.
    /// </remarks>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"Font({Id})" : "Font(Invalid)";
}
