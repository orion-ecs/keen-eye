namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Interface for graphics contexts that can provide a font manager.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows systems to obtain an <see cref="IFontManager"/> without
/// coupling to specific graphics backend implementations.
/// </para>
/// </remarks>
public interface IFontManagerProvider
{
    /// <summary>
    /// Gets the font manager, if available.
    /// </summary>
    /// <returns>The font manager, or null if not available.</returns>
    IFontManager? GetFontManager();
}
