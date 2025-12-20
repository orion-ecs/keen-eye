using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Particles.Data;

/// <summary>
/// Blend modes for particle rendering.
/// </summary>
public enum BlendMode
{
    /// <summary>Standard alpha blending (SrcAlpha, OneMinusSrcAlpha).</summary>
    Transparent,

    /// <summary>Additive blending for glow effects (SrcAlpha, One).</summary>
    Additive,

    /// <summary>Multiply blending (DstColor, Zero).</summary>
    Multiply,

    /// <summary>Pre-multiplied alpha (One, OneMinusSrcAlpha).</summary>
    Premultiplied
}

/// <summary>
/// Extension methods for converting <see cref="BlendMode"/> to graphics blend factors.
/// </summary>
public static class BlendModeExtensions
{
    /// <summary>
    /// Converts a blend mode to its corresponding source and destination blend factors.
    /// </summary>
    /// <param name="mode">The blend mode to convert.</param>
    /// <returns>A tuple containing the source and destination blend factors.</returns>
    public static (BlendFactor Src, BlendFactor Dst) ToBlendFactors(this BlendMode mode) => mode switch
    {
        BlendMode.Transparent => (BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha),
        BlendMode.Additive => (BlendFactor.SrcAlpha, BlendFactor.One),
        BlendMode.Multiply => (BlendFactor.DstColor, BlendFactor.Zero),
        BlendMode.Premultiplied => (BlendFactor.One, BlendFactor.OneMinusSrcAlpha),
        _ => (BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha)
    };
}
