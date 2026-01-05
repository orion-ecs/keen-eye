// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Component that marks an entity's geometry as a navigation mesh surface.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities with mesh geometry that should be included
/// in navigation mesh baking. The geometry will be processed according to the
/// specified area type and settings.
/// </para>
/// <para>
/// Entities without this component are excluded from navmesh generation unless
/// <see cref="NavMeshModifier"/> is used to include/exclude them.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark a floor entity as walkable
/// world.Spawn("Ground")
///     .With(new Transform3D { Position = Vector3.Zero })
///     .With(new MeshFilter { Mesh = groundMesh })
///     .With(new NavMeshSurface { AreaType = NavAreaType.Walkable })
///     .Build();
/// </code>
/// </example>
public struct NavMeshSurface : IComponent
{
    /// <summary>
    /// Gets or sets the navigation area type for this surface.
    /// </summary>
    /// <remarks>
    /// The area type determines the traversal cost and filtering behavior
    /// for this surface during pathfinding.
    /// </remarks>
    public NavAreaType AreaType;

    /// <summary>
    /// Gets or sets whether this surface should be included in navmesh generation.
    /// </summary>
    public bool IsWalkable;

    /// <summary>
    /// Gets or sets the layer mask for this surface.
    /// </summary>
    /// <remarks>
    /// Used to filter which surfaces are included in the bake based on layer configuration.
    /// </remarks>
    public int Layer;

    /// <summary>
    /// Gets or sets the collection mode for geometry from this surface.
    /// </summary>
    public NavMeshCollectGeometry CollectGeometry;

    /// <summary>
    /// Gets or sets whether to use the object's child renderers.
    /// </summary>
    /// <remarks>
    /// When true, child entities with mesh geometry are also included.
    /// </remarks>
    public bool IncludeChildren;

    /// <summary>
    /// Gets or sets the override voxel size for this surface.
    /// </summary>
    /// <remarks>
    /// If greater than 0, overrides the global voxel size for this surface.
    /// Use for surfaces that need higher or lower detail.
    /// </remarks>
    public float OverrideVoxelSize;

    /// <summary>
    /// Gets or sets the override tile size for this surface.
    /// </summary>
    /// <remarks>
    /// If greater than 0, overrides the global tile size for this surface.
    /// </remarks>
    public int OverrideTileSize;

    /// <summary>
    /// Gets or sets whether this is a default walkable surface.
    /// </summary>
    /// <remarks>
    /// When true, this surface uses default settings without any overrides.
    /// </remarks>
    public bool UseDefaults;

    /// <summary>
    /// Creates a NavMeshSurface with default settings for walkable geometry.
    /// </summary>
    /// <returns>A new NavMeshSurface component.</returns>
    public static NavMeshSurface Create()
        => new()
        {
            AreaType = NavAreaType.Walkable,
            IsWalkable = true,
            Layer = 0,
            CollectGeometry = NavMeshCollectGeometry.RenderMeshes,
            IncludeChildren = true,
            OverrideVoxelSize = 0,
            OverrideTileSize = 0,
            UseDefaults = true
        };

    /// <summary>
    /// Creates a NavMeshSurface for a specific area type.
    /// </summary>
    /// <param name="areaType">The navigation area type.</param>
    /// <returns>A new NavMeshSurface component.</returns>
    public static NavMeshSurface Create(NavAreaType areaType)
        => Create() with { AreaType = areaType };

    /// <summary>
    /// Creates a non-walkable NavMeshSurface (obstacle).
    /// </summary>
    /// <returns>A new NavMeshSurface component configured as an obstacle.</returns>
    public static NavMeshSurface CreateObstacle()
        => new()
        {
            AreaType = NavAreaType.NotWalkable,
            IsWalkable = false,
            Layer = 0,
            CollectGeometry = NavMeshCollectGeometry.RenderMeshes,
            IncludeChildren = true,
            UseDefaults = true
        };
}

/// <summary>
/// Specifies how geometry is collected from a NavMeshSurface.
/// </summary>
public enum NavMeshCollectGeometry
{
    /// <summary>
    /// Use render meshes (visual geometry).
    /// </summary>
    RenderMeshes = 0,

    /// <summary>
    /// Use physics colliders.
    /// </summary>
    PhysicsColliders = 1,

    /// <summary>
    /// Use both render meshes and physics colliders.
    /// </summary>
    Both = 2
}
