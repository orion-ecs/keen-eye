namespace KeenEyes.Assets;

/// <summary>
/// Defines how the alpha channel is interpreted for rendering.
/// </summary>
/// <remarks>
/// These modes correspond to the glTF 2.0 alpha modes and control how
/// transparency is handled during rendering.
/// </remarks>
public enum AlphaMode
{
    /// <summary>
    /// Alpha value is ignored and the rendered output is fully opaque.
    /// </summary>
    Opaque,

    /// <summary>
    /// Alpha value is used to determine whether pixels are fully opaque or fully transparent.
    /// Pixels with alpha below <see cref="MaterialData.AlphaCutoff"/> are discarded.
    /// </summary>
    Mask,

    /// <summary>
    /// Alpha value is used for blending the source and destination colors.
    /// Requires proper depth sorting for correct rendering.
    /// </summary>
    Blend
}
