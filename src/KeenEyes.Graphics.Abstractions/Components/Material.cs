using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Component containing material properties for rendering.
/// </summary>
/// <remarks>
/// <para>
/// Materials define how a surface appears when rendered, including color,
/// textures, and shader-specific parameters. For complex materials with
/// many textures, consider using a material asset system instead.
/// </para>
/// </remarks>
public struct Material : IComponent
{
    /// <summary>
    /// The handle to the shader program.
    /// </summary>
    public int ShaderId;

    /// <summary>
    /// The handle to the primary (albedo/diffuse) texture.
    /// Use 0 for no texture.
    /// </summary>
    public int TextureId;

    /// <summary>
    /// The handle to the normal map texture.
    /// Use 0 for no normal map.
    /// </summary>
    public int NormalMapId;

    /// <summary>
    /// The base color tint applied to the material.
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// The emissive color for self-illumination.
    /// </summary>
    public Vector3 EmissiveColor;

    /// <summary>
    /// The metallic factor (0 = dielectric, 1 = metal).
    /// </summary>
    public float Metallic;

    /// <summary>
    /// The roughness factor (0 = smooth/mirror, 1 = rough/diffuse).
    /// </summary>
    public float Roughness;

    /// <summary>
    /// Creates a default material with white color and reasonable PBR defaults.
    /// </summary>
    public static Material Default => new()
    {
        ShaderId = 0,
        TextureId = 0,
        NormalMapId = 0,
        Color = Vector4.One,
        EmissiveColor = Vector3.Zero,
        Metallic = 0f,
        Roughness = 0.5f
    };

    /// <summary>
    /// Creates a simple unlit material with the specified color.
    /// </summary>
    /// <param name="color">The material color.</param>
    /// <returns>A new material instance.</returns>
    public static Material Unlit(Vector4 color) => new()
    {
        Color = color,
        Metallic = 0f,
        Roughness = 1f
    };
}
