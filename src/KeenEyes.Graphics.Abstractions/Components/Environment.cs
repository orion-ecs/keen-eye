using KeenEyes.Common;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Component that provides environmental lighting and skybox rendering.
/// </summary>
/// <remarks>
/// <para>
/// The Environment component enables Image-Based Lighting (IBL) from HDR environment maps.
/// When attached to an entity, it provides:
/// </para>
/// <list type="bullet">
/// <item><description>Diffuse ambient lighting from the irradiance map</description></item>
/// <item><description>Specular reflections from the pre-filtered environment map</description></item>
/// <item><description>Optional skybox rendering using the environment cubemap</description></item>
/// </list>
/// <para>
/// Only one active Environment should be present in a scene at a time.
/// Use the <see cref="ActiveEnvironmentTag"/> to mark which environment is active.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create an environment entity with IBL
/// var env = world.Spawn()
///     .With(new Environment
///     {
///         IBLDataId = loadedIblData.Id,
///         Intensity = 1.0f,
///         Rotation = 0f,
///         AffectsLighting = true,
///         RenderSkybox = true
///     })
///     .WithTag&lt;ActiveEnvironmentTag&gt;()
///     .Build();
/// </code>
/// </example>
public struct Environment : IComponent
{
    /// <summary>
    /// Gets or sets the ID of the IBL data to use.
    /// </summary>
    /// <remarks>
    /// This references pre-processed IBL data containing irradiance,
    /// specular, and BRDF lookup textures.
    /// </remarks>
    public int IBLDataId;

    /// <summary>
    /// Gets or sets the intensity multiplier for IBL lighting.
    /// </summary>
    /// <remarks>
    /// Values greater than 1.0 increase the brightness of environment lighting.
    /// Default is 1.0.
    /// </remarks>
    public float Intensity;

    /// <summary>
    /// Gets or sets the Y-axis rotation of the environment in degrees.
    /// </summary>
    /// <remarks>
    /// Rotates the environment map around the vertical axis.
    /// Useful for aligning the environment with the scene.
    /// </remarks>
    public float Rotation;

    /// <summary>
    /// Gets or sets whether this environment affects scene lighting.
    /// </summary>
    /// <remarks>
    /// When true, the IBL irradiance and specular maps contribute
    /// to the lighting of PBR materials.
    /// </remarks>
    public bool AffectsLighting;

    /// <summary>
    /// Gets or sets whether to render the environment as a skybox.
    /// </summary>
    /// <remarks>
    /// When true, the environment cubemap is rendered as a background skybox.
    /// </remarks>
    public bool RenderSkybox;

    /// <summary>
    /// Gets or sets the exposure adjustment for the skybox.
    /// </summary>
    /// <remarks>
    /// Adjusts the brightness of the skybox independently of IBL lighting.
    /// Default is 1.0.
    /// </remarks>
    public float SkyboxExposure;

    /// <summary>
    /// Gets or sets the blur level for the skybox (0 = sharp, 1 = fully blurred).
    /// </summary>
    /// <remarks>
    /// Uses the specular map mip levels to blur the skybox.
    /// Useful for stylized or fog-like effects.
    /// </remarks>
    public float SkyboxBlur;

    /// <summary>
    /// Creates a default environment configuration.
    /// </summary>
    public static Environment Default => new()
    {
        IBLDataId = 0,
        Intensity = 1.0f,
        Rotation = 0f,
        AffectsLighting = true,
        RenderSkybox = true,
        SkyboxExposure = 1.0f,
        SkyboxBlur = 0f
    };
}

/// <summary>
/// Tag component marking the active environment for the scene.
/// </summary>
/// <remarks>
/// Only one entity should have this tag at a time. The render system
/// uses this to find which environment to apply for IBL and skybox rendering.
/// </remarks>
public struct ActiveEnvironmentTag : IComponent;
