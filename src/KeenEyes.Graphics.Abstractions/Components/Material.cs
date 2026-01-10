using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Graphics.Abstractions;

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
    /// Pixels with alpha below <see cref="Material.AlphaCutoff"/> are discarded.
    /// </summary>
    Mask,

    /// <summary>
    /// Alpha value is used for blending the source and destination colors.
    /// Requires proper depth sorting for correct rendering.
    /// </summary>
    Blend
}

/// <summary>
/// Component containing PBR material properties for rendering.
/// </summary>
/// <remarks>
/// <para>
/// Materials define how a surface appears when rendered using the metallic-roughness
/// PBR workflow. This component supports all standard glTF 2.0 material properties.
/// </para>
/// <para>
/// Texture slots use integer handles (0 = no texture). The actual texture binding
/// is handled by the render system based on the graphics backend.
/// </para>
/// <para>
/// Standard texture slot assignments:
/// </para>
/// <list type="bullet">
/// <item><description>Slot 0: Base Color (Albedo)</description></item>
/// <item><description>Slot 1: Normal Map</description></item>
/// <item><description>Slot 2: Metallic-Roughness (G=roughness, B=metallic)</description></item>
/// <item><description>Slot 3: Occlusion (R=occlusion)</description></item>
/// <item><description>Slot 4: Emissive</description></item>
/// </list>
/// </remarks>
public struct Material : IComponent
{
    /// <summary>
    /// The handle to the shader program.
    /// </summary>
    public int ShaderId;

    /// <summary>
    /// The handle to the base color (albedo) texture.
    /// Use 0 for no texture; <see cref="BaseColorFactor"/> is used instead.
    /// </summary>
    public int BaseColorTextureId;

    /// <summary>
    /// The handle to the normal map texture.
    /// Use 0 for no normal map (flat surface normals).
    /// </summary>
    public int NormalMapId;

    /// <summary>
    /// The handle to the metallic-roughness texture.
    /// G channel = roughness, B channel = metallic.
    /// Use 0 for no texture; factors are used instead.
    /// </summary>
    public int MetallicRoughnessTextureId;

    /// <summary>
    /// The handle to the occlusion texture.
    /// R channel = occlusion factor.
    /// Use 0 for no texture (no ambient occlusion).
    /// </summary>
    public int OcclusionTextureId;

    /// <summary>
    /// The handle to the emissive texture.
    /// RGB channels = emissive color.
    /// Use 0 for no texture; <see cref="EmissiveFactor"/> is used instead.
    /// </summary>
    public int EmissiveTextureId;

    /// <summary>
    /// The base color factor (RGBA). Multiplied with base color texture if present.
    /// </summary>
    public Vector4 BaseColorFactor;

    /// <summary>
    /// The metallic factor (0 = dielectric, 1 = metal).
    /// Multiplied with metallic texture B channel if present.
    /// </summary>
    public float MetallicFactor;

    /// <summary>
    /// The roughness factor (0 = smooth/mirror, 1 = rough/diffuse).
    /// Multiplied with metallic-roughness texture G channel if present.
    /// </summary>
    public float RoughnessFactor;

    /// <summary>
    /// The emissive factor (RGB). Multiplied with emissive texture if present.
    /// </summary>
    public Vector3 EmissiveFactor;

    /// <summary>
    /// The scale factor for the normal map. Default is 1.0.
    /// </summary>
    public float NormalScale;

    /// <summary>
    /// The strength of the occlusion effect. Default is 1.0.
    /// </summary>
    public float OcclusionStrength;

    /// <summary>
    /// The alpha cutoff threshold for <see cref="AlphaMode.Mask"/> mode.
    /// Pixels with alpha below this value are discarded.
    /// </summary>
    public float AlphaCutoff;

    /// <summary>
    /// How the alpha channel affects rendering.
    /// </summary>
    public AlphaMode AlphaMode;

    /// <summary>
    /// Whether the material is rendered on both sides.
    /// When false, back faces are culled.
    /// </summary>
    public bool DoubleSided;

    /// <summary>
    /// Creates a default material with white color and reasonable PBR defaults.
    /// </summary>
    public static Material Default => new()
    {
        ShaderId = 0,
        BaseColorTextureId = 0,
        NormalMapId = 0,
        MetallicRoughnessTextureId = 0,
        OcclusionTextureId = 0,
        EmissiveTextureId = 0,
        BaseColorFactor = Vector4.One,
        MetallicFactor = 0f,
        RoughnessFactor = 0.5f,
        EmissiveFactor = Vector3.Zero,
        NormalScale = 1f,
        OcclusionStrength = 1f,
        AlphaCutoff = 0.5f,
        AlphaMode = AlphaMode.Opaque,
        DoubleSided = false
    };

    /// <summary>
    /// Creates a simple unlit material with the specified color.
    /// </summary>
    /// <param name="color">The material color.</param>
    /// <returns>A new material instance.</returns>
    public static Material Unlit(Vector4 color) => new()
    {
        BaseColorFactor = color,
        MetallicFactor = 0f,
        RoughnessFactor = 1f,
        NormalScale = 1f,
        OcclusionStrength = 1f,
        AlphaCutoff = 0.5f,
        AlphaMode = AlphaMode.Opaque
    };

}
