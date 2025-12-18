namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Interface for graphics contexts that can provide a text renderer.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows UI systems to obtain an <see cref="ITextRenderer"/> without
/// coupling to specific graphics backend implementations.
/// </para>
/// </remarks>
public interface ITextRendererProvider
{
    /// <summary>
    /// Gets the text renderer, if available.
    /// </summary>
    /// <returns>The text renderer, or null if not available.</returns>
    ITextRenderer? GetTextRenderer();
}
