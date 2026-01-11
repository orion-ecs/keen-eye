namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Shadow map resolution presets.
/// </summary>
public enum ShadowResolution
{
    /// <summary>
    /// Low resolution (512x512). Good for performance, visible aliasing.
    /// </summary>
    Low = 512,

    /// <summary>
    /// Medium resolution (1024x1024). Balanced quality/performance.
    /// </summary>
    Medium = 1024,

    /// <summary>
    /// High resolution (2048x2048). Good quality, higher memory usage.
    /// </summary>
    High = 2048,

    /// <summary>
    /// Very high resolution (4096x4096). Best quality, significant memory usage.
    /// </summary>
    VeryHigh = 4096
}

/// <summary>
/// Shadow filtering modes for soft shadows.
/// </summary>
public enum ShadowFilterMode
{
    /// <summary>
    /// Hard shadows with no filtering. Sharp edges, best performance.
    /// </summary>
    Hard,

    /// <summary>
    /// 3x3 Percentage Closer Filtering. Soft edges, good performance.
    /// </summary>
    Pcf3x3,

    /// <summary>
    /// 5x5 Percentage Closer Filtering. Softer edges, moderate performance impact.
    /// </summary>
    Pcf5x5,

    /// <summary>
    /// 7x7 Percentage Closer Filtering. Very soft edges, higher performance cost.
    /// </summary>
    Pcf7x7
}

/// <summary>
/// Shadow mapping configuration settings.
/// </summary>
public struct ShadowSettings
{
    /// <summary>
    /// Default shadow settings with medium quality.
    /// </summary>
    public static readonly ShadowSettings Default = new()
    {
        Resolution = ShadowResolution.Medium,
        FilterMode = ShadowFilterMode.Pcf3x3,
        CascadeCount = 4,
        CascadeSplitLambda = 0.75f,
        MaxShadowDistance = 100f,
        NormalBias = 0.01f,
        DepthBias = 0.005f
    };

    /// <summary>
    /// The resolution of shadow maps.
    /// </summary>
    public ShadowResolution Resolution;

    /// <summary>
    /// The filtering mode for soft shadows.
    /// </summary>
    public ShadowFilterMode FilterMode;

    /// <summary>
    /// Number of shadow map cascades for directional lights (1-4).
    /// More cascades provide better quality at different distances but cost more.
    /// </summary>
    public int CascadeCount;

    /// <summary>
    /// Controls the distribution of cascade splits between logarithmic and uniform.
    /// 0.0 = uniform distribution, 1.0 = logarithmic distribution.
    /// Values around 0.75 provide a good balance.
    /// </summary>
    public float CascadeSplitLambda;

    /// <summary>
    /// Maximum distance from the camera at which shadows are rendered.
    /// </summary>
    public float MaxShadowDistance;

    /// <summary>
    /// Bias applied along the surface normal to reduce shadow acne.
    /// </summary>
    public float NormalBias;

    /// <summary>
    /// Bias applied along the light direction to reduce shadow acne.
    /// </summary>
    public float DepthBias;

    /// <summary>
    /// Gets the resolution in pixels.
    /// </summary>
    public readonly int ResolutionPixels => (int)Resolution;

    /// <summary>
    /// Gets the clamped cascade count (1-4).
    /// </summary>
    public readonly int ClampedCascadeCount => Math.Clamp(CascadeCount, 1, 4);
}
