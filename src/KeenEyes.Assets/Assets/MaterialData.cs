using System.Numerics;

namespace KeenEyes.Assets;

/// <summary>
/// CPU-side PBR material properties extracted from a glTF file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MaterialData"/> is a data-only record that does not hold GPU resources.
/// Texture indices reference the <see cref="TextureData"/> array in the parent
/// <see cref="ModelAsset"/>.
/// </para>
/// <para>
/// Material properties follow the glTF 2.0 PBR metallic-roughness model:
/// </para>
/// <list type="bullet">
/// <item><description>Base color - RGBA diffuse/albedo color</description></item>
/// <item><description>Metallic - 0.0 (dielectric) to 1.0 (metallic)</description></item>
/// <item><description>Roughness - 0.0 (smooth) to 1.0 (rough)</description></item>
/// <item><description>Emissive - Self-illumination color and intensity</description></item>
/// <item><description>Normal map - Tangent-space surface perturbation</description></item>
/// <item><description>Occlusion - Ambient occlusion factor</description></item>
/// </list>
/// </remarks>
/// <param name="Name">The material name from the glTF file.</param>
/// <param name="BaseColorFactor">Base color multiplier (RGBA). Default is white (1,1,1,1).</param>
/// <param name="MetallicFactor">Metallic multiplier. 0 = dielectric, 1 = metallic.</param>
/// <param name="RoughnessFactor">Roughness multiplier. 0 = smooth/mirror, 1 = rough/diffuse.</param>
/// <param name="EmissiveFactor">Emissive color (RGB). Default is black (no emission).</param>
/// <param name="AlphaCutoff">Alpha cutoff threshold for <see cref="AlphaMode.Mask"/> mode.</param>
/// <param name="AlphaMode">How alpha channel affects rendering.</param>
/// <param name="DoubleSided">Whether the material is rendered on both sides.</param>
/// <param name="BaseColorTextureIndex">Index into texture array, or -1 if none.</param>
/// <param name="NormalTextureIndex">Index into texture array, or -1 if none.</param>
/// <param name="MetallicRoughnessTextureIndex">Index into texture array, or -1 if none. G=roughness, B=metallic.</param>
/// <param name="OcclusionTextureIndex">Index into texture array, or -1 if none. R=occlusion.</param>
/// <param name="EmissiveTextureIndex">Index into texture array, or -1 if none.</param>
/// <param name="NormalScale">Scale factor for normal map. Default is 1.0.</param>
/// <param name="OcclusionStrength">Strength of occlusion effect. Default is 1.0.</param>
public sealed record MaterialData(
    string Name,
    Vector4 BaseColorFactor,
    float MetallicFactor,
    float RoughnessFactor,
    Vector3 EmissiveFactor,
    float AlphaCutoff,
    AlphaMode AlphaMode,
    bool DoubleSided,
    int BaseColorTextureIndex,
    int NormalTextureIndex,
    int MetallicRoughnessTextureIndex,
    int OcclusionTextureIndex,
    int EmissiveTextureIndex,
    float NormalScale = 1.0f,
    float OcclusionStrength = 1.0f)
{
    /// <summary>
    /// Default PBR material with white base color and no textures.
    /// </summary>
    /// <remarks>
    /// Default values:
    /// <list type="bullet">
    /// <item><description>Base color: White (1, 1, 1, 1)</description></item>
    /// <item><description>Metallic: 0 (dielectric)</description></item>
    /// <item><description>Roughness: 0.5 (medium)</description></item>
    /// <item><description>Emissive: Black (no emission)</description></item>
    /// <item><description>Alpha mode: Opaque</description></item>
    /// <item><description>All texture indices: -1 (no texture)</description></item>
    /// </list>
    /// </remarks>
    public static MaterialData Default => new(
        Name: "Default",
        BaseColorFactor: Vector4.One,
        MetallicFactor: 0f,
        RoughnessFactor: 0.5f,
        EmissiveFactor: Vector3.Zero,
        AlphaCutoff: 0.5f,
        AlphaMode: AlphaMode.Opaque,
        DoubleSided: false,
        BaseColorTextureIndex: -1,
        NormalTextureIndex: -1,
        MetallicRoughnessTextureIndex: -1,
        OcclusionTextureIndex: -1,
        EmissiveTextureIndex: -1);

    /// <summary>
    /// Gets whether this material has a base color texture.
    /// </summary>
    public bool HasBaseColorTexture => BaseColorTextureIndex >= 0;

    /// <summary>
    /// Gets whether this material has a normal map texture.
    /// </summary>
    public bool HasNormalTexture => NormalTextureIndex >= 0;

    /// <summary>
    /// Gets whether this material has a metallic-roughness texture.
    /// </summary>
    public bool HasMetallicRoughnessTexture => MetallicRoughnessTextureIndex >= 0;

    /// <summary>
    /// Gets whether this material has an occlusion texture.
    /// </summary>
    public bool HasOcclusionTexture => OcclusionTextureIndex >= 0;

    /// <summary>
    /// Gets whether this material has an emissive texture.
    /// </summary>
    public bool HasEmissiveTexture => EmissiveTextureIndex >= 0;

    /// <summary>
    /// Gets whether this material uses any textures.
    /// </summary>
    public bool HasAnyTexture =>
        HasBaseColorTexture ||
        HasNormalTexture ||
        HasMetallicRoughnessTexture ||
        HasOcclusionTexture ||
        HasEmissiveTexture;

    /// <summary>
    /// Gets whether this material requires alpha blending.
    /// </summary>
    public bool RequiresBlending => AlphaMode == AlphaMode.Blend;

    /// <summary>
    /// Gets whether this material requires alpha testing.
    /// </summary>
    public bool RequiresAlphaTest => AlphaMode == AlphaMode.Mask;
}
