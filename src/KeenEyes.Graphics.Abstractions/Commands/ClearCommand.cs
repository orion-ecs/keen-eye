using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Command to clear render buffers.
/// </summary>
/// <param name="Mask">The buffers to clear.</param>
/// <param name="Color">The clear color (optional, for color buffer).</param>
/// <param name="Depth">The clear depth value (optional, default 1.0).</param>
/// <param name="Stencil">The clear stencil value (optional, default 0).</param>
/// <remarks>
/// This command clears the specified render buffers. When clearing the color buffer,
/// the <paramref name="Color"/> value is used. Depth buffer clears use <paramref name="Depth"/>,
/// and stencil buffer clears use <paramref name="Stencil"/>.
/// </remarks>
/// <example>
/// <code>
/// // Clear color and depth
/// var clear = new ClearCommand(
///     ClearMask.ColorBuffer | ClearMask.DepthBuffer,
///     new Vector4(0.1f, 0.1f, 0.1f, 1f));
/// </code>
/// </example>
public readonly record struct ClearCommand(
    ClearMask Mask,
    Vector4 Color = default,
    float Depth = 1f,
    int Stencil = 0) : IRenderCommand
{
    /// <summary>
    /// Sort key for clear commands. Clear commands execute first (sort key 0).
    /// </summary>
    public ulong SortKey => 0;

    /// <summary>
    /// Creates a command to clear the color buffer with the specified color.
    /// </summary>
    /// <param name="color">The clear color.</param>
    /// <returns>A clear command for the color buffer.</returns>
    public static ClearCommand ColorOnly(Vector4 color) =>
        new(ClearMask.ColorBuffer, color);

    /// <summary>
    /// Creates a command to clear color and depth buffers.
    /// </summary>
    /// <param name="color">The clear color.</param>
    /// <param name="depth">The clear depth value.</param>
    /// <returns>A clear command for color and depth buffers.</returns>
    public static ClearCommand ColorAndDepth(Vector4 color, float depth = 1f) =>
        new(ClearMask.ColorBuffer | ClearMask.DepthBuffer, color, depth);

    /// <summary>
    /// Creates a command to clear all buffers.
    /// </summary>
    /// <param name="color">The clear color.</param>
    /// <param name="depth">The clear depth value.</param>
    /// <param name="stencil">The clear stencil value.</param>
    /// <returns>A clear command for all buffers.</returns>
    public static ClearCommand All(Vector4 color, float depth = 1f, int stencil = 0) =>
        new(ClearMask.ColorBuffer | ClearMask.DepthBuffer | ClearMask.StencilBuffer, color, depth, stencil);
}
