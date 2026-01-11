namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Resolution presets for IBL cubemap processing.
/// </summary>
public enum IBLResolution
{
    /// <summary>
    /// Low quality (64x64 irradiance, 128x128 specular).
    /// </summary>
    Low = 64,

    /// <summary>
    /// Medium quality (128x128 irradiance, 256x256 specular).
    /// </summary>
    Medium = 128,

    /// <summary>
    /// High quality (256x256 irradiance, 512x512 specular).
    /// </summary>
    High = 256,

    /// <summary>
    /// Very high quality (512x512 irradiance, 1024x1024 specular).
    /// </summary>
    VeryHigh = 512
}

/// <summary>
/// Configuration settings for Image-Based Lighting (IBL).
/// </summary>
/// <remarks>
/// <para>
/// IBL uses HDR environment maps to provide realistic ambient lighting
/// and reflections. The environment map is processed into:
/// </para>
/// <list type="bullet">
/// <item><description>Irradiance map - diffuse ambient lighting (low resolution)</description></item>
/// <item><description>Pre-filtered specular map - blurred reflections per roughness level</description></item>
/// <item><description>BRDF LUT - precomputed BRDF integration lookup table</description></item>
/// </list>
/// </remarks>
public struct IblSettings
{
    /// <summary>
    /// Gets or sets the resolution for the irradiance map.
    /// </summary>
    /// <remarks>
    /// The irradiance map stores diffuse ambient lighting and can be low resolution
    /// since it represents slowly varying lighting across the hemisphere.
    /// </remarks>
    public IBLResolution IrradianceResolution { get; set; }

    /// <summary>
    /// Gets or sets the resolution for the pre-filtered specular map.
    /// </summary>
    /// <remarks>
    /// Higher resolutions provide sharper reflections on smooth surfaces.
    /// The specular map includes multiple mip levels for different roughness values.
    /// </remarks>
    public IBLResolution SpecularResolution { get; set; }

    /// <summary>
    /// Gets or sets the number of mip levels for the specular map.
    /// </summary>
    /// <remarks>
    /// Each mip level represents a different roughness value, from sharp (level 0)
    /// to fully rough (max level). Typically 5-9 levels are used.
    /// </remarks>
    public int SpecularMipLevels { get; set; }

    /// <summary>
    /// Gets or sets the resolution for the BRDF lookup table.
    /// </summary>
    /// <remarks>
    /// The BRDF LUT is a 2D texture indexed by (NdotV, roughness).
    /// 512x512 is typically sufficient for high quality.
    /// </remarks>
    public int BrdfLutResolution { get; set; }

    /// <summary>
    /// Gets or sets the number of samples for irradiance convolution.
    /// </summary>
    /// <remarks>
    /// Higher sample counts produce smoother irradiance maps but take longer to compute.
    /// </remarks>
    public int IrradianceSampleCount { get; set; }

    /// <summary>
    /// Gets or sets the number of samples for specular pre-filtering.
    /// </summary>
    /// <remarks>
    /// Higher sample counts reduce noise in the pre-filtered specular map.
    /// </remarks>
    public int SpecularSampleCount { get; set; }

    /// <summary>
    /// Gets the irradiance map resolution in pixels.
    /// </summary>
    public readonly int IrradianceResolutionPixels => (int)IrradianceResolution;

    /// <summary>
    /// Gets the specular map resolution in pixels.
    /// </summary>
    public readonly int SpecularResolutionPixels => (int)SpecularResolution * 2;

    /// <summary>
    /// Gets the default IBL settings optimized for quality and performance.
    /// </summary>
    public static IblSettings Default => new()
    {
        IrradianceResolution = IBLResolution.Medium,
        SpecularResolution = IBLResolution.High,
        SpecularMipLevels = 5,
        BrdfLutResolution = 512,
        IrradianceSampleCount = 2048,
        SpecularSampleCount = 1024
    };

    /// <summary>
    /// Gets low quality IBL settings for faster processing.
    /// </summary>
    public static IblSettings Low => new()
    {
        IrradianceResolution = IBLResolution.Low,
        SpecularResolution = IBLResolution.Low,
        SpecularMipLevels = 4,
        BrdfLutResolution = 256,
        IrradianceSampleCount = 512,
        SpecularSampleCount = 256
    };

    /// <summary>
    /// Gets high quality IBL settings for best visual quality.
    /// </summary>
    public static IblSettings High => new()
    {
        IrradianceResolution = IBLResolution.High,
        SpecularResolution = IBLResolution.VeryHigh,
        SpecularMipLevels = 7,
        BrdfLutResolution = 512,
        IrradianceSampleCount = 4096,
        SpecularSampleCount = 2048
    };
}
