using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Shadows;

/// <summary>
/// Manages shadow map creation, updates, and lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This manager handles the allocation of shadow map render targets and
/// the calculation of light-space matrices for shadow rendering.
/// </para>
/// <para>
/// For directional lights, it supports Cascaded Shadow Maps (CSM) with
/// up to 4 cascades. For point lights, it uses cubemap shadow maps.
/// For spot lights, it uses a single 2D shadow map.
/// </para>
/// </remarks>
/// <param name="graphics">The graphics context for creating render targets.</param>
public sealed class ShadowMapManager(IGraphicsContext graphics) : IDisposable
{
    private readonly Dictionary<int, DirectionalShadowData> directionalShadows = [];
    private readonly Dictionary<int, PointShadowData> pointShadows = [];
    private readonly Dictionary<int, SpotShadowData> spotShadows = [];
    private bool disposed;

    /// <summary>
    /// Gets the global shadow settings used for new shadow maps.
    /// </summary>
    public ShadowSettings Settings { get; set; } = ShadowSettings.Default;

    /// <summary>
    /// Gets the directional shadow data for a light entity.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <returns>The shadow data, or null if not found.</returns>
    public DirectionalShadowData? GetDirectionalShadowData(int entityId)
    {
        return directionalShadows.TryGetValue(entityId, out var data) ? data : null;
    }

    /// <summary>
    /// Gets the point shadow data for a light entity.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <returns>The shadow data, or null if not found.</returns>
    public PointShadowData? GetPointShadowData(int entityId)
    {
        return pointShadows.TryGetValue(entityId, out var data) ? data : null;
    }

    /// <summary>
    /// Gets the spot shadow data for a light entity.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <returns>The shadow data, or null if not found.</returns>
    public SpotShadowData? GetSpotShadowData(int entityId)
    {
        return spotShadows.TryGetValue(entityId, out var data) ? data : null;
    }

    /// <summary>
    /// Creates or updates shadow map resources for a directional light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <param name="settings">Optional custom settings. Uses global settings if null.</param>
    public void CreateDirectionalShadowMap(int entityId, ShadowSettings? settings = null)
    {
        var shadowSettings = settings ?? Settings;

        // Remove existing shadow map if settings changed
        if (directionalShadows.TryGetValue(entityId, out var existing))
        {
            if (existing.Settings.Resolution == shadowSettings.Resolution &&
                existing.Settings.CascadeCount == shadowSettings.CascadeCount)
            {
                // Settings unchanged, keep existing
                return;
            }

            // Settings changed, recreate
            DeleteDirectionalShadowMap(entityId);
        }

        // Create new shadow data
        var data = new DirectionalShadowData
        {
            Settings = shadowSettings
        };

        int resolution = shadowSettings.ResolutionPixels;
        int cascadeCount = shadowSettings.ClampedCascadeCount;

        // Create depth-only render targets for each cascade
        for (int i = 0; i < cascadeCount; i++)
        {
            var target = graphics.CreateDepthOnlyRenderTarget(resolution, resolution);
            data.SetCascadeRenderTarget(i, target);
        }

        directionalShadows[entityId] = data;
    }

    /// <summary>
    /// Creates or updates shadow map resources for a point light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <param name="resolution">The cubemap resolution. Defaults to global settings.</param>
    public void CreatePointShadowMap(int entityId, int? resolution = null)
    {
        int size = resolution ?? Settings.ResolutionPixels;

        // Remove existing if resolution changed
        if (pointShadows.TryGetValue(entityId, out var existing))
        {
            if (existing.RenderTarget.Size == size)
            {
                return;
            }

            DeletePointShadowMap(entityId);
        }

        var data = new PointShadowData
        {
            RenderTarget = graphics.CreateCubemapRenderTarget(size, withDepth: true),
            FarPlane = 25f, // Default far plane, will be updated per-frame
            Bias = Settings.DepthBias
        };

        pointShadows[entityId] = data;
    }

    /// <summary>
    /// Creates or updates shadow map resources for a spot light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <param name="resolution">The shadow map resolution. Defaults to global settings.</param>
    public void CreateSpotShadowMap(int entityId, int? resolution = null)
    {
        int size = resolution ?? Settings.ResolutionPixels;

        // Remove existing if resolution changed
        if (spotShadows.TryGetValue(entityId, out var existing))
        {
            if (existing.RenderTarget.Width == size)
            {
                return;
            }

            DeleteSpotShadowMap(entityId);
        }

        var data = new SpotShadowData
        {
            RenderTarget = graphics.CreateDepthOnlyRenderTarget(size, size),
            Bias = Settings.DepthBias
        };

        spotShadows[entityId] = data;
    }

    /// <summary>
    /// Updates the light-space matrices for a directional light's shadow cascades.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <param name="lightDirection">The light direction (pointing toward the light).</param>
    /// <param name="cameraView">The camera view matrix.</param>
    /// <param name="cameraProjection">The camera projection matrix.</param>
    /// <param name="cameraNear">The camera near plane.</param>
    public void UpdateDirectionalLightMatrices(
        int entityId,
        Vector3 lightDirection,
        Matrix4x4 cameraView,
        Matrix4x4 cameraProjection,
        float cameraNear)
    {
        if (!directionalShadows.TryGetValue(entityId, out var data))
        {
            return;
        }

        var settings = data.Settings;
        int cascadeCount = settings.ClampedCascadeCount;
        float maxDistance = settings.MaxShadowDistance;

        // Calculate cascade split distances
        var splits = CascadeUtils.CalculateCascadeSplits(
            cameraNear,
            maxDistance,
            cascadeCount,
            settings.CascadeSplitLambda);

        // Update each cascade
        for (int i = 0; i < cascadeCount; i++)
        {
            // Calculate frustum corners for this cascade
            var frustumCorners = CascadeUtils.CalculateCascadeFrustumCorners(
                cameraView,
                cameraProjection,
                cameraNear,
                splits,
                i);

            // Calculate light-space matrix
            var lightSpaceMatrix = CascadeUtils.CalculateLightSpaceMatrix(
                -lightDirection, // Negate because we want direction toward light
                frustumCorners);

            // Stabilize to reduce shadow swimming
            lightSpaceMatrix = CascadeUtils.StabilizeLightSpaceMatrix(
                lightSpaceMatrix,
                settings.ResolutionPixels);

            data.SetLightSpaceMatrix(i, lightSpaceMatrix);
            data.SetCascadeSplit(i, splits[i]);
        }

        directionalShadows[entityId] = data;
    }

    /// <summary>
    /// Updates the light-space matrix for a spot light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <param name="position">The light position.</param>
    /// <param name="direction">The light direction.</param>
    /// <param name="outerConeAngle">The outer cone angle in radians.</param>
    /// <param name="range">The light range.</param>
    public void UpdateSpotLightMatrix(
        int entityId,
        Vector3 position,
        Vector3 direction,
        float outerConeAngle,
        float range)
    {
        if (!spotShadows.TryGetValue(entityId, out var data))
        {
            return;
        }

        // Create view matrix looking in light direction
        var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.99f
            ? Vector3.UnitZ
            : Vector3.UnitY;

        var lightView = Matrix4x4.CreateLookAt(position, position + direction, up);

        // Create perspective projection based on cone angle
        float fov = outerConeAngle * 2f; // Full cone angle
        var lightProjection = Matrix4x4.CreatePerspectiveFieldOfView(
            fov,
            1f, // Square aspect ratio
            0.1f,
            range);

        data.LightSpaceMatrix = lightView * lightProjection;
        data.Bias = Settings.DepthBias;

        spotShadows[entityId] = data;
    }

    /// <summary>
    /// Updates point light shadow data.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    /// <param name="position">The light position.</param>
    /// <param name="range">The light range.</param>
    public void UpdatePointLightData(int entityId, Vector3 position, float range)
    {
        if (!pointShadows.TryGetValue(entityId, out var data))
        {
            return;
        }

        data.LightPosition = position;
        data.FarPlane = range;
        data.Bias = Settings.DepthBias;

        pointShadows[entityId] = data;
    }

    /// <summary>
    /// Deletes shadow map resources for a directional light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    public void DeleteDirectionalShadowMap(int entityId)
    {
        if (!directionalShadows.TryGetValue(entityId, out var data))
        {
            return;
        }

        int cascadeCount = data.Settings.ClampedCascadeCount;
        for (int i = 0; i < cascadeCount; i++)
        {
            var target = data.GetCascadeRenderTarget(i);
            if (target.IsValid)
            {
                graphics.DeleteRenderTarget(target);
            }
        }

        directionalShadows.Remove(entityId);
    }

    /// <summary>
    /// Deletes shadow map resources for a point light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    public void DeletePointShadowMap(int entityId)
    {
        if (!pointShadows.TryGetValue(entityId, out var data))
        {
            return;
        }

        if (data.RenderTarget.IsValid)
        {
            graphics.DeleteCubemapRenderTarget(data.RenderTarget);
        }

        pointShadows.Remove(entityId);
    }

    /// <summary>
    /// Deletes shadow map resources for a spot light.
    /// </summary>
    /// <param name="entityId">The entity ID of the light.</param>
    public void DeleteSpotShadowMap(int entityId)
    {
        if (!spotShadows.TryGetValue(entityId, out var data))
        {
            return;
        }

        if (data.RenderTarget.IsValid)
        {
            graphics.DeleteRenderTarget(data.RenderTarget);
        }

        spotShadows.Remove(entityId);
    }

    /// <summary>
    /// Gets all directional shadow-casting light entity IDs.
    /// </summary>
    public IEnumerable<int> DirectionalShadowLights => directionalShadows.Keys;

    /// <summary>
    /// Gets all point shadow-casting light entity IDs.
    /// </summary>
    public IEnumerable<int> PointShadowLights => pointShadows.Keys;

    /// <summary>
    /// Gets all spot shadow-casting light entity IDs.
    /// </summary>
    public IEnumerable<int> SpotShadowLights => spotShadows.Keys;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Clean up all shadow maps
        foreach (var entityId in directionalShadows.Keys.ToList())
        {
            DeleteDirectionalShadowMap(entityId);
        }

        foreach (var entityId in pointShadows.Keys.ToList())
        {
            DeletePointShadowMap(entityId);
        }

        foreach (var entityId in spotShadows.Keys.ToList())
        {
            DeleteSpotShadowMap(entityId);
        }
    }
}
