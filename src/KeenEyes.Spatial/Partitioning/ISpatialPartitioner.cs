using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Partitioning;

/// <summary>
/// Interface for spatial partitioning implementations that organize entities
/// for efficient spatial queries.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface provide different strategies for organizing
/// entities in space (grid, quadtree, octree, etc.). All implementations must
/// support insertion, removal, updates, and spatial queries.
/// </para>
/// <para>
/// This interface is internal - users interact with spatial partitioning through
/// the <see cref="SpatialQueryApi"/> extension API.
/// </para>
/// </remarks>
internal interface ISpatialPartitioner : IDisposable
{
    /// <summary>
    /// Inserts or updates an entity in the spatial index at the specified position.
    /// </summary>
    /// <param name="entity">The entity to index.</param>
    /// <param name="position">The world position of the entity.</param>
    /// <remarks>
    /// If the entity is already indexed, this updates its position.
    /// Entities without bounds are treated as points.
    /// </remarks>
    void Update(Entity entity, Vector3 position);

    /// <summary>
    /// Inserts or updates an entity in the spatial index with bounds.
    /// </summary>
    /// <param name="entity">The entity to index.</param>
    /// <param name="position">The world position of the entity.</param>
    /// <param name="bounds">The axis-aligned bounding box of the entity.</param>
    /// <remarks>
    /// This overload allows entities with spatial extent (not just points)
    /// to be indexed more accurately for collision and intersection queries.
    /// </remarks>
    void Update(Entity entity, Vector3 position, SpatialBounds bounds);

    /// <summary>
    /// Removes an entity from the spatial index.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <remarks>
    /// This should be called when an entity is despawned or no longer needs
    /// to be spatially indexed. Removing a non-indexed entity is a no-op.
    /// </remarks>
    void Remove(Entity entity);

    /// <summary>
    /// Queries all entities within a spherical radius of a point.
    /// </summary>
    /// <param name="center">The center point of the query sphere.</param>
    /// <param name="radius">The radius of the query sphere.</param>
    /// <returns>An enumerable of entities within the radius (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it may return false positives (entities slightly
    /// outside the radius). Callers should perform narrowphase distance checks if
    /// exact results are required.
    /// </para>
    /// <para>
    /// Results are unordered and may contain duplicates in some implementations.
    /// </para>
    /// </remarks>
    IEnumerable<Entity> QueryRadius(Vector3 center, float radius);

    /// <summary>
    /// Queries all entities within an axis-aligned bounding box.
    /// </summary>
    /// <param name="min">The minimum corner of the query box.</param>
    /// <param name="max">The maximum corner of the query box.</param>
    /// <returns>An enumerable of entities within the box (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it may return false positives (entities slightly
    /// outside the box). Callers should perform narrowphase intersection tests if
    /// exact results are required.
    /// </para>
    /// <para>
    /// Results are unordered and may contain duplicates in some implementations.
    /// </para>
    /// </remarks>
    IEnumerable<Entity> QueryBounds(Vector3 min, Vector3 max);

    /// <summary>
    /// Queries all entities at a specific point.
    /// </summary>
    /// <param name="point">The point to query.</param>
    /// <returns>An enumerable of entities containing the point (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query - it may return false positives (entities near
    /// but not containing the point). Callers should perform narrowphase containment
    /// tests if exact results are required.
    /// </para>
    /// <para>
    /// For point-based entities (without bounds), this returns entities in the same
    /// spatial cell/region as the query point.
    /// </para>
    /// </remarks>
    IEnumerable<Entity> QueryPoint(Vector3 point);

    /// <summary>
    /// Queries all entities within a view frustum.
    /// </summary>
    /// <param name="frustum">The view frustum to query.</param>
    /// <returns>An enumerable of entities within the frustum (broadphase candidates).</returns>
    /// <remarks>
    /// <para>
    /// This is a broadphase query optimized for camera visibility culling.
    /// It may return false positives (entities outside the frustum).
    /// Callers should perform narrowphase frustum tests if exact results are required.
    /// </para>
    /// <para>
    /// Results are unordered and may contain duplicates in some implementations.
    /// </para>
    /// <para>
    /// Use <see cref="Frustum.FromMatrix"/> to create a frustum from a view-projection matrix.
    /// </para>
    /// </remarks>
    IEnumerable<Entity> QueryFrustum(Frustum frustum);

    /// <summary>
    /// Gets the total number of entities currently indexed.
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// Clears all entities from the spatial index.
    /// </summary>
    void Clear();

    /// <summary>
    /// Queries all entities within a spherical radius into a caller-provided buffer.
    /// </summary>
    /// <param name="center">The center point of the query sphere.</param>
    /// <param name="radius">The radius of the query sphere.</param>
    /// <param name="results">The buffer to write results into.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a zero-allocation query method for performance-critical code paths.
    /// Use stackalloc or ArrayPool for the buffer.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Deduplication uses linear scan (O(n) per entity).
    /// For queries with many overlapping cells/nodes and large result sets (>1000 entities),
    /// this can approach O(n²) complexity. Typical queries (&lt;100 results) have negligible overhead.
    /// </para>
    /// </remarks>
    int QueryRadius(Vector3 center, float radius, Span<Entity> results);

    /// <summary>
    /// Queries all entities within an axis-aligned bounding box into a caller-provided buffer.
    /// </summary>
    /// <param name="min">The minimum corner of the query box.</param>
    /// <param name="max">The maximum corner of the query box.</param>
    /// <param name="results">The buffer to write results into.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a zero-allocation query method for performance-critical code paths.
    /// Use stackalloc or ArrayPool for the buffer.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Deduplication uses linear scan (O(n) per entity).
    /// For queries with many overlapping cells/nodes and large result sets (>1000 entities),
    /// this can approach O(n²) complexity. Typical queries (&lt;100 results) have negligible overhead.
    /// </para>
    /// </remarks>
    int QueryBounds(Vector3 min, Vector3 max, Span<Entity> results);

    /// <summary>
    /// Queries all entities at a specific point into a caller-provided buffer.
    /// </summary>
    /// <param name="point">The point to query.</param>
    /// <param name="results">The buffer to write results into.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a zero-allocation query method for performance-critical code paths.
    /// Use stackalloc or ArrayPool for the buffer.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Deduplication uses linear scan (O(n) per entity).
    /// For queries with many overlapping cells/nodes and large result sets (>1000 entities),
    /// this can approach O(n²) complexity. Typical queries (&lt;100 results) have negligible overhead.
    /// </para>
    /// </remarks>
    int QueryPoint(Vector3 point, Span<Entity> results);

    /// <summary>
    /// Queries all entities within a view frustum into a caller-provided buffer.
    /// </summary>
    /// <param name="frustum">The view frustum to query.</param>
    /// <param name="results">The buffer to write results into.</param>
    /// <returns>
    /// The number of entities written to the buffer, or -1 if the buffer was too small.
    /// When -1 is returned, the buffer contains partial results up to its capacity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is a zero-allocation query method for performance-critical code paths.
    /// Use stackalloc or ArrayPool for the buffer.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Deduplication uses linear scan (O(n) per entity).
    /// For queries with many overlapping cells/nodes and large result sets (>1000 entities),
    /// this can approach O(n²) complexity. Typical queries (&lt;100 results) have negligible overhead.
    /// </para>
    /// </remarks>
    int QueryFrustum(Frustum frustum, Span<Entity> results);
}
