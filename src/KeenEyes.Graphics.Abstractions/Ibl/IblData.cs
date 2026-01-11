namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Contains the processed IBL textures for a single environment.
/// </summary>
/// <remarks>
/// <para>
/// IBL data consists of three pre-computed textures derived from an HDR environment map:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Irradiance Map</b> - A low-resolution cubemap containing the integral of incoming
/// light over the hemisphere for each direction. Used for diffuse ambient lighting.
/// </description></item>
/// <item><description>
/// <b>Pre-filtered Specular Map</b> - A cubemap with mip levels, where each level
/// represents increasing roughness. Used for specular reflections.
/// </description></item>
/// <item><description>
/// <b>BRDF LUT</b> - A 2D lookup table storing the split-sum approximation of the
/// specular BRDF. Indexed by (NdotV, roughness).
/// </description></item>
/// </list>
/// </remarks>
public struct IblData
{
    /// <summary>
    /// Gets or sets the original environment cubemap texture.
    /// </summary>
    /// <remarks>
    /// This is the source HDR environment map converted to a cubemap.
    /// Can be used for skybox rendering.
    /// </remarks>
    public TextureHandle EnvironmentMap { get; set; }

    /// <summary>
    /// Gets or sets the irradiance cubemap for diffuse IBL.
    /// </summary>
    /// <remarks>
    /// A low-resolution cubemap where each texel contains the average
    /// irradiance from the hemisphere oriented in that direction.
    /// </remarks>
    public TextureHandle IrradianceMap { get; set; }

    /// <summary>
    /// Gets or sets the pre-filtered specular cubemap for specular IBL.
    /// </summary>
    /// <remarks>
    /// A cubemap with mip levels where:
    /// - Level 0 = mirror reflection (roughness 0)
    /// - Higher levels = increasingly blurred (higher roughness)
    /// </remarks>
    public TextureHandle SpecularMap { get; set; }

    /// <summary>
    /// Gets or sets the BRDF lookup texture.
    /// </summary>
    /// <remarks>
    /// A 2D texture storing precomputed BRDF integration values.
    /// R channel = scale, G channel = bias for the Fresnel term.
    /// </remarks>
    public TextureHandle BrdfLut { get; set; }

    /// <summary>
    /// Gets or sets the settings used to generate this IBL data.
    /// </summary>
    public IblSettings Settings { get; set; }

    /// <summary>
    /// Gets or sets the number of mip levels in the specular map.
    /// </summary>
    public int SpecularMipLevels { get; set; }

    /// <summary>
    /// Gets whether this IBL data is valid and ready for use.
    /// </summary>
    public readonly bool IsValid =>
        IrradianceMap.IsValid &&
        SpecularMap.IsValid &&
        BrdfLut.IsValid;

    /// <summary>
    /// Gets whether this IBL data includes the original environment map.
    /// </summary>
    public readonly bool HasEnvironmentMap => EnvironmentMap.IsValid;

    /// <summary>
    /// Creates an invalid/empty IBL data instance.
    /// </summary>
    public static IblData Invalid => new()
    {
        EnvironmentMap = TextureHandle.Invalid,
        IrradianceMap = TextureHandle.Invalid,
        SpecularMap = TextureHandle.Invalid,
        BrdfLut = TextureHandle.Invalid,
        Settings = IblSettings.Default,
        SpecularMipLevels = 0
    };
}

/// <summary>
/// Represents a loaded HDR environment for IBL processing.
/// </summary>
public struct EnvironmentMapData
{
    /// <summary>
    /// Gets or sets the HDR pixel data (RGB float values).
    /// </summary>
    public float[] Pixels { get; set; }

    /// <summary>
    /// Gets or sets the width of the equirectangular map.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the equirectangular map.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets whether this environment map data is valid.
    /// </summary>
    public readonly bool IsValid => Pixels != null && Pixels.Length > 0 && Width > 0 && Height > 0;

    /// <summary>
    /// Gets the number of color channels (always 3 for RGB).
    /// </summary>
    public static int Channels => 3;
}
