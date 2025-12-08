using System.Numerics;

using KeenEyes.Spatial;

namespace KeenEyes.Graphics;

/// <summary>
/// Specifies the type of light source.
/// </summary>
public enum LightType
{
    /// <summary>
    /// Directional light with parallel rays (sun-like).
    /// </summary>
    Directional,

    /// <summary>
    /// Point light emitting in all directions from a position.
    /// </summary>
    Point,

    /// <summary>
    /// Spot light emitting in a cone from a position.
    /// </summary>
    Spot
}

/// <summary>
/// Component that defines a light source for scene illumination.
/// </summary>
/// <remarks>
/// <para>
/// Lights require a <see cref="Transform3D"/> component to define their position
/// and direction. For directional lights, only the rotation matters. For point
/// and spot lights, the position is also used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Directional sunlight
/// world.Spawn()
///     .With(new Transform3D(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0, -0.5f, 0), Vector3.One))
///     .With(Light.Directional(new Vector3(1, 0.95f, 0.8f), 1.0f))
///     .Build();
///
/// // Point light
/// world.Spawn()
///     .With(new Transform3D(new Vector3(5, 3, 0), Quaternion.Identity, Vector3.One))
///     .With(Light.Point(new Vector3(1, 0.8f, 0.6f), 1.0f, 10f))
///     .Build();
/// </code>
/// </example>
public struct Light : IComponent
{
    /// <summary>
    /// The type of light (directional, point, or spot).
    /// </summary>
    public LightType Type;

    /// <summary>
    /// The light color (RGB).
    /// </summary>
    public Vector3 Color;

    /// <summary>
    /// The light intensity multiplier.
    /// </summary>
    public float Intensity;

    /// <summary>
    /// The range of the light (for point and spot lights).
    /// </summary>
    public float Range;

    /// <summary>
    /// The inner cone angle in degrees (for spot lights).
    /// Full intensity within this angle.
    /// </summary>
    public float InnerConeAngle;

    /// <summary>
    /// The outer cone angle in degrees (for spot lights).
    /// Light fades to zero at this angle.
    /// </summary>
    public float OuterConeAngle;

    /// <summary>
    /// Whether this light casts shadows.
    /// </summary>
    public bool CastShadows;

    /// <summary>
    /// The shadow bias to prevent shadow acne.
    /// </summary>
    public float ShadowBias;

    /// <summary>
    /// Creates a directional light.
    /// </summary>
    /// <param name="color">The light color.</param>
    /// <param name="intensity">The light intensity.</param>
    /// <returns>A new directional light.</returns>
    public static Light Directional(Vector3 color, float intensity) => new()
    {
        Type = LightType.Directional,
        Color = color,
        Intensity = intensity,
        CastShadows = true,
        ShadowBias = 0.005f
    };

    /// <summary>
    /// Creates a point light.
    /// </summary>
    /// <param name="color">The light color.</param>
    /// <param name="intensity">The light intensity.</param>
    /// <param name="range">The light range.</param>
    /// <returns>A new point light.</returns>
    public static Light Point(Vector3 color, float intensity, float range) => new()
    {
        Type = LightType.Point,
        Color = color,
        Intensity = intensity,
        Range = range,
        CastShadows = false,
        ShadowBias = 0.005f
    };

    /// <summary>
    /// Creates a spot light.
    /// </summary>
    /// <param name="color">The light color.</param>
    /// <param name="intensity">The light intensity.</param>
    /// <param name="range">The light range.</param>
    /// <param name="innerAngle">The inner cone angle in degrees.</param>
    /// <param name="outerAngle">The outer cone angle in degrees.</param>
    /// <returns>A new spot light.</returns>
    public static Light Spot(Vector3 color, float intensity, float range, float innerAngle, float outerAngle) => new()
    {
        Type = LightType.Spot,
        Color = color,
        Intensity = intensity,
        Range = range,
        InnerConeAngle = innerAngle,
        OuterConeAngle = outerAngle,
        CastShadows = true,
        ShadowBias = 0.005f
    };
}
