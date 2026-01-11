using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Data for a single shadow map (one cascade or one light).
/// </summary>
public struct ShadowMapData
{
    /// <summary>
    /// The render target containing the shadow depth texture.
    /// </summary>
    public RenderTargetHandle RenderTarget;

    /// <summary>
    /// The light-space view-projection matrix used to render the shadow map.
    /// </summary>
    public Matrix4x4 LightSpaceMatrix;

    /// <summary>
    /// The far plane distance for this shadow map.
    /// </summary>
    public float FarPlane;
}

/// <summary>
/// Complete shadow data for a directional light with cascaded shadow maps.
/// </summary>
public struct DirectionalShadowData
{
    /// <summary>
    /// Maximum number of cascades supported.
    /// </summary>
    public const int MaxCascades = 4;

    /// <summary>
    /// The shadow settings used for this light.
    /// </summary>
    public ShadowSettings Settings;

    /// <summary>
    /// The render targets for each cascade (depth-only).
    /// </summary>
    public RenderTargetHandle Cascade0;

    /// <summary>
    /// The render target for cascade 1.
    /// </summary>
    public RenderTargetHandle Cascade1;

    /// <summary>
    /// The render target for cascade 2.
    /// </summary>
    public RenderTargetHandle Cascade2;

    /// <summary>
    /// The render target for cascade 3.
    /// </summary>
    public RenderTargetHandle Cascade3;

    /// <summary>
    /// Light-space matrix for cascade 0.
    /// </summary>
    public Matrix4x4 LightSpaceMatrix0;

    /// <summary>
    /// Light-space matrix for cascade 1.
    /// </summary>
    public Matrix4x4 LightSpaceMatrix1;

    /// <summary>
    /// Light-space matrix for cascade 2.
    /// </summary>
    public Matrix4x4 LightSpaceMatrix2;

    /// <summary>
    /// Light-space matrix for cascade 3.
    /// </summary>
    public Matrix4x4 LightSpaceMatrix3;

    /// <summary>
    /// The cascade split distances (view-space depth at which each cascade ends).
    /// </summary>
    public float Split0;

    /// <summary>
    /// Split distance for cascade 1.
    /// </summary>
    public float Split1;

    /// <summary>
    /// Split distance for cascade 2.
    /// </summary>
    public float Split2;

    /// <summary>
    /// Split distance for cascade 3.
    /// </summary>
    public float Split3;

    /// <summary>
    /// Gets the render target for a specific cascade.
    /// </summary>
    /// <param name="cascadeIndex">The cascade index (0-3).</param>
    /// <returns>The render target handle.</returns>
    public readonly RenderTargetHandle GetCascadeRenderTarget(int cascadeIndex) => cascadeIndex switch
    {
        0 => Cascade0,
        1 => Cascade1,
        2 => Cascade2,
        3 => Cascade3,
        _ => throw new ArgumentOutOfRangeException(nameof(cascadeIndex), "Cascade index must be 0-3")
    };

    /// <summary>
    /// Gets the light-space matrix for a specific cascade.
    /// </summary>
    /// <param name="cascadeIndex">The cascade index (0-3).</param>
    /// <returns>The light-space matrix.</returns>
    public readonly Matrix4x4 GetLightSpaceMatrix(int cascadeIndex) => cascadeIndex switch
    {
        0 => LightSpaceMatrix0,
        1 => LightSpaceMatrix1,
        2 => LightSpaceMatrix2,
        3 => LightSpaceMatrix3,
        _ => throw new ArgumentOutOfRangeException(nameof(cascadeIndex), "Cascade index must be 0-3")
    };

    /// <summary>
    /// Gets the cascade split distance for a specific cascade.
    /// </summary>
    /// <param name="cascadeIndex">The cascade index (0-3).</param>
    /// <returns>The split distance.</returns>
    public readonly float GetCascadeSplit(int cascadeIndex) => cascadeIndex switch
    {
        0 => Split0,
        1 => Split1,
        2 => Split2,
        3 => Split3,
        _ => throw new ArgumentOutOfRangeException(nameof(cascadeIndex), "Cascade index must be 0-3")
    };

    /// <summary>
    /// Sets the render target for a specific cascade.
    /// </summary>
    /// <param name="cascadeIndex">The cascade index (0-3).</param>
    /// <param name="target">The render target handle.</param>
    public void SetCascadeRenderTarget(int cascadeIndex, RenderTargetHandle target)
    {
        switch (cascadeIndex)
        {
            case 0: Cascade0 = target; break;
            case 1: Cascade1 = target; break;
            case 2: Cascade2 = target; break;
            case 3: Cascade3 = target; break;
            default: throw new ArgumentOutOfRangeException(nameof(cascadeIndex), "Cascade index must be 0-3");
        }
    }

    /// <summary>
    /// Sets the light-space matrix for a specific cascade.
    /// </summary>
    /// <param name="cascadeIndex">The cascade index (0-3).</param>
    /// <param name="matrix">The light-space matrix.</param>
    public void SetLightSpaceMatrix(int cascadeIndex, Matrix4x4 matrix)
    {
        switch (cascadeIndex)
        {
            case 0: LightSpaceMatrix0 = matrix; break;
            case 1: LightSpaceMatrix1 = matrix; break;
            case 2: LightSpaceMatrix2 = matrix; break;
            case 3: LightSpaceMatrix3 = matrix; break;
            default: throw new ArgumentOutOfRangeException(nameof(cascadeIndex), "Cascade index must be 0-3");
        }
    }

    /// <summary>
    /// Sets the cascade split distance for a specific cascade.
    /// </summary>
    /// <param name="cascadeIndex">The cascade index (0-3).</param>
    /// <param name="split">The split distance.</param>
    public void SetCascadeSplit(int cascadeIndex, float split)
    {
        switch (cascadeIndex)
        {
            case 0: Split0 = split; break;
            case 1: Split1 = split; break;
            case 2: Split2 = split; break;
            case 3: Split3 = split; break;
            default: throw new ArgumentOutOfRangeException(nameof(cascadeIndex), "Cascade index must be 0-3");
        }
    }
}

/// <summary>
/// Shadow data for a point light using a cubemap shadow map.
/// </summary>
public struct PointShadowData
{
    /// <summary>
    /// The cubemap render target containing the shadow depth.
    /// </summary>
    public CubemapRenderTargetHandle RenderTarget;

    /// <summary>
    /// The far plane distance for the shadow map.
    /// </summary>
    public float FarPlane;

    /// <summary>
    /// The light position in world space.
    /// </summary>
    public Vector3 LightPosition;

    /// <summary>
    /// Shadow bias to prevent shadow acne.
    /// </summary>
    public float Bias;
}

/// <summary>
/// Shadow data for a spot light using a single 2D shadow map.
/// </summary>
public struct SpotShadowData
{
    /// <summary>
    /// The render target containing the shadow depth texture.
    /// </summary>
    public RenderTargetHandle RenderTarget;

    /// <summary>
    /// The light-space view-projection matrix.
    /// </summary>
    public Matrix4x4 LightSpaceMatrix;

    /// <summary>
    /// Shadow bias to prevent shadow acne.
    /// </summary>
    public float Bias;
}
