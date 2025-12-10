using System.Numerics;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial;

/// <summary>
/// Public API for spatial queries provided by the spatial partitioning plugin.
/// </summary>
/// <remarks>
/// <para>
/// This API is exposed as a World extension by the <see cref="SpatialPlugin"/>.
/// Access it using <c>world.GetExtension&lt;SpatialQueryApi&gt;()</c>.
/// </para>
/// <para>
/// All query methods return broadphase candidates - entities that may be within
/// the query region. For exact results, callers should perform narrowphase checks
/// (distance, intersection, containment tests) on the returned entities.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the plugin
/// world.InstallPlugin(new SpatialPlugin(new SpatialConfig()));
///
/// // Get the API
/// var spatial = world.GetExtension&lt;SpatialQueryApi&gt;();
///
/// // Query entities within radius
/// foreach (var entity in spatial.QueryRadius(playerPos, 100f))
/// {
///     // Check if entity is actually within distance (narrowphase)
///     ref readonly var transform = ref world.Get&lt;Transform3D&gt;(entity);
///     float distSq = Vector3.DistanceSquared(playerPos, transform.Position);
///     if (distSq &lt;= 100f * 100f)
///     {
///         // Entity is definitely within radius
///     }
/// }
/// </code>
/// </example>
public sealed class SpatialQueryApi : IDisposable
{
    private readonly IWorld world;
    private readonly ISpatialPartitioner partitioner;

    /// <summary>
    /// Creates a new spatial query API.
    /// </summary>
    /// <param name="world">The world this API is associated with.</param>
    /// <param name="partitioner">The spatial partitioner implementation.</param>
    internal SpatialQueryApi(IWorld world, ISpatialPartitioner partitioner)
    {
        this.world = world;
        this.partitioner = partitioner;
    }

    /// <summary>
    /// Gets the internal partitioner for system use.
    /// </summary>
    internal ISpatialPartitioner Partitioner => partitioner;

    /// <summary>
    /// Queries all entities within a spherical radius of a point.
    /// </summary>
    /// <param name="center">The center point of the query sphere.</param>
    /// <param name="radius">The radius of the query sphere.</param>
    /// <returns>An enumerable of entities within the radius (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it may return entities slightly outside the radius.
    /// Use <see cref="Vector3.Distance"/> or <see cref="Vector3.DistanceSquared"/>
    /// for exact distance checks.
    /// </para>
    /// <para>
    /// Only entities with the <see cref="KeenEyes.Spatial.SpatialIndexed"/> tag are returned.
    /// </para>
    /// </remarks>
    public IEnumerable<Entity> QueryRadius(Vector3 center, float radius)
    {
        return partitioner.QueryRadius(center, radius);
    }

    /// <summary>
    /// Queries all entities within a spherical radius of a point, filtered by component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <param name="center">The center point of the query sphere.</param>
    /// <param name="radius">The radius of the query sphere.</param>
    /// <returns>An enumerable of entities within the radius that have component T.</returns>
    /// <remarks>
    /// This is a convenience method that combines spatial query with component filtering.
    /// Equivalent to <c>QueryRadius(center, radius).Where(e => world.Has&lt;T&gt;(e))</c>.
    /// </remarks>
    public IEnumerable<Entity> QueryRadius<T>(Vector3 center, float radius)
        where T : struct, IComponent
    {
        foreach (var entity in QueryRadius(center, radius))
        {
            if (world.Has<T>(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Queries all entities within an axis-aligned bounding box.
    /// </summary>
    /// <param name="min">The minimum corner of the query box.</param>
    /// <param name="max">The maximum corner of the query box.</param>
    /// <returns>An enumerable of entities within the box (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it may return entities slightly outside the box.
    /// Use AABB intersection tests for exact results.
    /// </para>
    /// <para>
    /// Only entities with the <see cref="KeenEyes.Spatial.SpatialIndexed"/> tag are returned.
    /// </para>
    /// </remarks>
    public IEnumerable<Entity> QueryBounds(Vector3 min, Vector3 max)
    {
        return partitioner.QueryBounds(min, max);
    }

    /// <summary>
    /// Queries all entities within an axis-aligned bounding box, filtered by component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <param name="min">The minimum corner of the query box.</param>
    /// <param name="max">The maximum corner of the query box.</param>
    /// <returns>An enumerable of entities within the box that have component T.</returns>
    /// <remarks>
    /// This is a convenience method that combines spatial query with component filtering.
    /// </remarks>
    public IEnumerable<Entity> QueryBounds<T>(Vector3 min, Vector3 max)
        where T : struct, IComponent
    {
        foreach (var entity in QueryBounds(min, max))
        {
            if (world.Has<T>(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Queries all entities at a specific point.
    /// </summary>
    /// <param name="point">The point to query.</param>
    /// <returns>An enumerable of entities at or near the point (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it returns entities in the same spatial region
    /// as the point. Use containment tests for exact results.
    /// </para>
    /// <para>
    /// Only entities with the <see cref="KeenEyes.Spatial.SpatialIndexed"/> tag are returned.
    /// </para>
    /// </remarks>
    public IEnumerable<Entity> QueryPoint(Vector3 point)
    {
        return partitioner.QueryPoint(point);
    }

    /// <summary>
    /// Queries all entities at a specific point, filtered by component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <param name="point">The point to query.</param>
    /// <returns>An enumerable of entities at or near the point that have component T.</returns>
    /// <remarks>
    /// This is a convenience method that combines spatial query with component filtering.
    /// </remarks>
    public IEnumerable<Entity> QueryPoint<T>(Vector3 point)
        where T : struct, IComponent
    {
        foreach (var entity in QueryPoint(point))
        {
            if (world.Has<T>(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Queries all entities within a view frustum (camera-based culling).
    /// </summary>
    /// <param name="frustum">The view frustum to query.</param>
    /// <returns>An enumerable of entities within the frustum (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it may return entities slightly outside the frustum.
    /// Use <see cref="Frustum.Contains"/> or <see cref="Frustum.Intersects(Vector3, Vector3)"/>
    /// for exact containment tests.
    /// </para>
    /// <para>
    /// Frustum culling is typically used for rendering optimization - only entities
    /// visible to the camera are returned. Use <see cref="Frustum.FromMatrix"/> to create
    /// a frustum from a view-projection matrix.
    /// </para>
    /// <para>
    /// Only entities with the <see cref="KeenEyes.Spatial.SpatialIndexed"/> tag are returned.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var spatial = world.GetExtension&lt;SpatialQueryApi&gt;();
    /// var viewProj = camera.ViewMatrix * camera.ProjectionMatrix;
    /// var frustum = Frustum.FromMatrix(viewProj);
    ///
    /// foreach (var entity in spatial.QueryFrustum(frustum))
    /// {
    ///     // Render entities visible to the camera
    ///     ref readonly var transform = ref world.Get&lt;Transform3D&gt;(entity);
    ///     RenderEntity(entity, transform);
    /// }
    /// </code>
    /// </example>
    public IEnumerable<Entity> QueryFrustum(Frustum frustum)
    {
        return partitioner.QueryFrustum(frustum);
    }

    /// <summary>
    /// Queries all entities within a view frustum, filtered by component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <param name="frustum">The view frustum to query.</param>
    /// <returns>An enumerable of entities within the frustum that have component T.</returns>
    /// <remarks>
    /// This is a convenience method that combines frustum culling with component filtering.
    /// Useful for rendering only entities of specific types (e.g., renderables, particles).
    /// </remarks>
    public IEnumerable<Entity> QueryFrustum<T>(Frustum frustum)
        where T : struct, IComponent
    {
        foreach (var entity in QueryFrustum(frustum))
        {
            if (world.Has<T>(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets the total number of entities currently in the spatial index.
    /// </summary>
    public int EntityCount => partitioner.EntityCount;

    /// <summary>
    /// Queries all entities within a spherical radius into a caller-provided buffer (zero-allocation).
    /// </summary>
    /// <param name="center">The center point of the query sphere.</param>
    /// <param name="radius">The radius of the query sphere.</param>
    /// <param name="results">The buffer to write results into. Use stackalloc or ArrayPool for optimal performance.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a zero-allocation query method for performance-critical code paths like
    /// collision detection or AI queries that run every frame.
    /// </para>
    /// <para>
    /// Example usage with stackalloc:
    /// <code>
    /// Span&lt;Entity&gt; buffer = stackalloc Entity[256];
    /// int count = spatial.QueryRadius(center, radius, buffer);
    /// if (count >= 0)
    /// {
    ///     foreach (var entity in buffer[..count])
    ///     {
    ///         // Process entity
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public int QueryRadius(Vector3 center, float radius, Span<Entity> results)
    {
        return partitioner.QueryRadius(center, radius, results);
    }

    /// <summary>
    /// Queries all entities within an axis-aligned bounding box into a caller-provided buffer (zero-allocation).
    /// </summary>
    /// <param name="min">The minimum corner of the query box.</param>
    /// <param name="max">The maximum corner of the query box.</param>
    /// <param name="results">The buffer to write results into. Use stackalloc or ArrayPool for optimal performance.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// This is a zero-allocation query method for performance-critical code paths.
    /// </remarks>
    public int QueryBounds(Vector3 min, Vector3 max, Span<Entity> results)
    {
        return partitioner.QueryBounds(min, max, results);
    }

    /// <summary>
    /// Queries all entities at a specific point into a caller-provided buffer (zero-allocation).
    /// </summary>
    /// <param name="point">The point to query.</param>
    /// <param name="results">The buffer to write results into. Use stackalloc or ArrayPool for optimal performance.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// This is a zero-allocation query method for performance-critical code paths.
    /// </remarks>
    public int QueryPoint(Vector3 point, Span<Entity> results)
    {
        return partitioner.QueryPoint(point, results);
    }

    /// <summary>
    /// Queries all entities within a view frustum into a caller-provided buffer (zero-allocation).
    /// </summary>
    /// <param name="frustum">The view frustum to query.</param>
    /// <param name="results">The buffer to write results into. Use stackalloc or ArrayPool for optimal performance.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// This is a zero-allocation query method for performance-critical code paths like
    /// frustum culling for rendering.
    /// </remarks>
    public int QueryFrustum(Frustum frustum, Span<Entity> results)
    {
        return partitioner.QueryFrustum(frustum, results);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        partitioner.Dispose();
    }
}
