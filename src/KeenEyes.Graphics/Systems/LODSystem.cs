using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics;

/// <summary>
/// System that automatically selects mesh LOD levels based on camera distance or screen coverage.
/// </summary>
/// <remarks>
/// <para>
/// The LodSystem processes entities with <see cref="Transform3D"/>, <see cref="Renderable"/>,
/// and <see cref="LodGroup"/> components. For each entity, it calculates the distance from the
/// camera (or screen size) and selects the appropriate LOD level.
/// </para>
/// <para>
/// This system should run BEFORE <see cref="RenderSystem"/> so that mesh selection happens
/// prior to rendering. Register it in the PreUpdate or EarlyUpdate phase.
/// </para>
/// <para>
/// Hysteresis is applied to prevent flickering at LOD boundaries. When transitioning to a
/// lower detail level, the threshold is increased; when transitioning to higher detail,
/// it is decreased.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register LOD system before render system
/// world.AddSystem(new LodSystem { HysteresisFactor = 0.1f });
/// world.AddSystem(new RenderSystem());
/// </code>
/// </example>
public sealed class LodSystem : ISystem
{
    private IWorld? world;

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the hysteresis factor to prevent LOD flickering at boundaries.
    /// Default is 0.05 (5%).
    /// </summary>
    /// <remarks>
    /// When switching to lower detail: threshold * (1 + hysteresis).
    /// When switching to higher detail: threshold * (1 - hysteresis).
    /// </remarks>
    public float HysteresisFactor { get; set; } = 0.05f;

    /// <summary>
    /// Gets or sets the global bias applied to all LOD calculations.
    /// Positive values prefer higher detail, negative values prefer lower detail.
    /// </summary>
    /// <remarks>
    /// For distance mode: Subtracts from calculated distance.
    /// For screen-size mode: Multiplies the calculated screen size.
    /// </remarks>
    public float GlobalBias { get; set; } = 0f;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (world is null)
        {
            return;
        }

        // Find active camera
        Vector3 cameraPosition = Vector3.Zero;
        Camera camera = default;
        bool foundCamera = false;

        // Prefer main camera tag
        foreach (var entity in world.Query<Camera, Transform3D, MainCameraTag>())
        {
            camera = world.Get<Camera>(entity);
            cameraPosition = world.Get<Transform3D>(entity).Position;
            foundCamera = true;
            break;
        }

        // Fall back to any camera
        if (!foundCamera)
        {
            foreach (var entity in world.Query<Camera, Transform3D>())
            {
                camera = world.Get<Camera>(entity);
                cameraPosition = world.Get<Transform3D>(entity).Position;
                foundCamera = true;
                break;
            }
        }

        if (!foundCamera)
        {
            // No camera, can't calculate LOD
            return;
        }

        // Process all entities with LOD groups
        foreach (var entity in world.Query<Transform3D, Renderable, LodGroup>())
        {
            ref readonly var transform = ref world.Get<Transform3D>(entity);
            ref var renderable = ref world.Get<Renderable>(entity);
            ref var lodGroup = ref world.Get<LodGroup>(entity);

            int newLevel = CalculateLODLevel(
                ref lodGroup,
                transform.Position,
                cameraPosition,
                camera);

            // Update mesh if LOD level changed
            if (newLevel != lodGroup.CurrentLevel)
            {
                lodGroup.CurrentLevel = newLevel;
                renderable.MeshId = lodGroup.GetLevel(newLevel).MeshId;
            }
        }
    }

    /// <summary>
    /// Calculates the appropriate LOD level for an entity.
    /// </summary>
    private int CalculateLODLevel(
        ref LodGroup lodGroup,
        Vector3 entityPosition,
        Vector3 cameraPosition,
        Camera camera)
    {
        if (lodGroup.LevelCount <= 1)
        {
            return 0;
        }

        float metric;

        if (lodGroup.SelectionMode == LodSelectionMode.Distance)
        {
            // Calculate distance from camera to entity
            float distance = Vector3.Distance(cameraPosition, entityPosition);

            // Apply biases (global and per-entity)
            distance -= GlobalBias;
            distance -= lodGroup.Bias;
            distance = MathF.Max(0, distance);

            metric = distance;
        }
        else
        {
            // Screen-size mode: calculate projected size
            float distance = Vector3.Distance(cameraPosition, entityPosition);

            // Avoid division by zero for entities at camera position
            if (distance < 0.001f)
            {
                return 0;
            }

            float screenSize = CalculateScreenSize(
                lodGroup.BoundingSphereRadius,
                distance,
                camera);

            // Apply biases
            screenSize *= (1 + GlobalBias * 0.1f);
            screenSize *= (1 + lodGroup.Bias * 0.1f);

            metric = screenSize;
        }

        // Find appropriate LOD level
        int currentLevel = lodGroup.CurrentLevel;
        int newLevel = 0;

        for (int i = 0; i < lodGroup.LevelCount; i++)
        {
            var level = lodGroup.GetLevel(i);
            float threshold = level.Threshold;

            // Apply hysteresis based on transition direction
            if (lodGroup.SelectionMode == LodSelectionMode.Distance)
            {
                // Distance mode: lower index = higher detail = closer distance
                if (i > currentLevel)
                {
                    // Transitioning to lower detail: increase threshold
                    threshold *= (1 + HysteresisFactor);
                }
                else if (i < currentLevel)
                {
                    // Transitioning to higher detail: decrease threshold
                    threshold *= (1 - HysteresisFactor);
                }

                if (metric >= threshold)
                {
                    newLevel = i;
                }
            }
            else
            {
                // Screen-size mode: lower index = higher detail = larger screen size
                if (i > currentLevel)
                {
                    // Transitioning to lower detail: decrease threshold
                    threshold *= (1 - HysteresisFactor);
                }
                else if (i < currentLevel)
                {
                    // Transitioning to higher detail: increase threshold
                    threshold *= (1 + HysteresisFactor);
                }

                if (metric <= threshold)
                {
                    newLevel = i;
                }
            }
        }

        return newLevel;
    }

    /// <summary>
    /// Calculates the projected screen size of a bounding sphere.
    /// </summary>
    /// <param name="radius">The bounding sphere radius in world units.</param>
    /// <param name="distance">The distance from camera to the sphere center.</param>
    /// <param name="camera">The camera component.</param>
    /// <returns>The approximate screen coverage ratio (0-1).</returns>
    private static float CalculateScreenSize(float radius, float distance, Camera camera)
    {
        if (camera.Projection == ProjectionType.Perspective)
        {
            // Perspective projection: projected height = radius / (distance * tan(fov/2))
            float fovRadians = camera.FieldOfView * MathF.PI / 180f;
            float tanHalfFov = MathF.Tan(fovRadians * 0.5f);
            return radius / (distance * tanHalfFov);
        }
        else
        {
            // Orthographic projection: projected height = radius / orthographic size
            return radius / camera.OrthographicSize;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
