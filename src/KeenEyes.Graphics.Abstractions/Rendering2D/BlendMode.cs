namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Blend modes for 2D rendering.
/// </summary>
/// <remarks>
/// Each mode maps to a fixed pair of <see cref="BlendFactor"/> values applied by the
/// renderer when a batch is flushed. Use <see cref="I2DRenderer.SetBlendMode"/> to
/// switch modes between draws.
/// </remarks>
public enum BlendMode
{
    /// <summary>Standard alpha blending (SrcAlpha, OneMinusSrcAlpha).</summary>
    Alpha,

    /// <summary>Additive blending for glow effects (SrcAlpha, One).</summary>
    Additive,

    /// <summary>Multiply blending (DstColor, OneMinusSrcAlpha).</summary>
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
        BlendMode.Additive => (BlendFactor.SrcAlpha, BlendFactor.One),
        BlendMode.Multiply => (BlendFactor.DstColor, BlendFactor.OneMinusSrcAlpha),
        BlendMode.Premultiplied => (BlendFactor.One, BlendFactor.OneMinusSrcAlpha),
        _ => (BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha)
    };
}
