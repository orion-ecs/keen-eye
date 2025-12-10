namespace KeenEyes.Spatial;

/// <summary>
/// Tag component that marks an entity for inclusion in spatial partitioning.
/// </summary>
/// <remarks>
/// <para>
/// Entities with this tag will be automatically indexed by spatial partitioning systems
/// based on their <see cref="KeenEyes.Common.Transform3D"/> position. This enables efficient spatial
/// queries for collision detection, proximity searches, and rendering culling.
/// </para>
/// <para>
/// The spatial partitioning system (provided by <see cref="SpatialPlugin"/>) will automatically track position
/// changes and update the spatial index. Entities without this tag are not indexed
/// and won't be returned by spatial queries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the spatial plugin
/// world.InstallPlugin(new SpatialPlugin());
///
/// // Create an entity that participates in spatial partitioning
/// var entity = world.Spawn()
///     .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
///     .WithTag&lt;SpatialIndexed&gt;()
///     .Build();
///
/// // Query nearby entities
/// var spatial = world.GetExtension&lt;SpatialQueryApi&gt;();
/// foreach (var nearby in spatial.QueryRadius(position, radius))
/// {
///     // Process nearby entities
/// }
/// </code>
/// </example>
public struct SpatialIndexed : ITagComponent
{
}
