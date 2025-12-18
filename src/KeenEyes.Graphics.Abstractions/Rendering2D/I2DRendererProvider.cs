namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Interface for graphics contexts that can provide a 2D renderer.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows UI systems to obtain an <see cref="I2DRenderer"/> without
/// coupling to specific graphics backend implementations.
/// </para>
/// </remarks>
public interface I2DRendererProvider
{
    /// <summary>
    /// Gets the 2D renderer, if available.
    /// </summary>
    /// <returns>The 2D renderer, or null if not available.</returns>
    I2DRenderer? Get2DRenderer();
}
