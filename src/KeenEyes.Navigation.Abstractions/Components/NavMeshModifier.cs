// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Component that modifies how an entity is processed during navmesh baking.
/// </summary>
/// <remarks>
/// <para>
/// Use this component to override area types or exclude specific geometry
/// from navmesh generation without requiring a <see cref="NavMeshSurface"/>.
/// </para>
/// <para>
/// NavMeshModifier affects the entity and optionally its children, allowing
/// hierarchical control over navigation surface properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Exclude a decoration from navmesh
/// world.Spawn("Decoration")
///     .With(new Transform3D { Position = new Vector3(5, 0, 5) })
///     .With(new MeshFilter { Mesh = decorMesh })
///     .With(new NavMeshModifier { IgnoreFromBuild = true })
///     .Build();
///
/// // Mark a bridge as a special area
/// world.Spawn("Bridge")
///     .With(new Transform3D { Position = new Vector3(0, 2, 0) })
///     .With(new MeshFilter { Mesh = bridgeMesh })
///     .With(new NavMeshModifier { OverrideAreaType = true, AreaType = NavAreaType.Road })
///     .Build();
/// </code>
/// </example>
public struct NavMeshModifier : IComponent
{
    /// <summary>
    /// Gets or sets whether to ignore this entity during navmesh building.
    /// </summary>
    /// <remarks>
    /// When true, the entity's geometry is completely excluded from the navmesh.
    /// </remarks>
    public bool IgnoreFromBuild;

    /// <summary>
    /// Gets or sets whether to override the area type.
    /// </summary>
    /// <remarks>
    /// When true, <see cref="AreaType"/> is used instead of the default or
    /// surface-specified area type.
    /// </remarks>
    public bool OverrideAreaType;

    /// <summary>
    /// Gets or sets the area type to use when <see cref="OverrideAreaType"/> is true.
    /// </summary>
    public NavAreaType AreaType;

    /// <summary>
    /// Gets or sets whether this modifier affects child entities.
    /// </summary>
    /// <remarks>
    /// When true, all descendants inherit this modifier's settings unless
    /// they have their own NavMeshModifier.
    /// </remarks>
    public bool AffectChildren;

    /// <summary>
    /// Gets or sets the agent type mask this modifier applies to.
    /// </summary>
    /// <remarks>
    /// -1 means all agent types. Otherwise, only navmeshes for matching
    /// agent types are affected.
    /// </remarks>
    public int AgentTypeMask;

    /// <summary>
    /// Creates a NavMeshModifier with default settings.
    /// </summary>
    /// <returns>A new NavMeshModifier component.</returns>
    public static NavMeshModifier Create()
        => new()
        {
            IgnoreFromBuild = false,
            OverrideAreaType = false,
            AreaType = NavAreaType.Walkable,
            AffectChildren = true,
            AgentTypeMask = -1
        };

    /// <summary>
    /// Creates a NavMeshModifier that excludes geometry from the navmesh.
    /// </summary>
    /// <param name="affectChildren">Whether to exclude children as well.</param>
    /// <returns>A new NavMeshModifier configured to exclude geometry.</returns>
    public static NavMeshModifier CreateExclude(bool affectChildren = true)
        => new()
        {
            IgnoreFromBuild = true,
            AffectChildren = affectChildren,
            AgentTypeMask = -1
        };

    /// <summary>
    /// Creates a NavMeshModifier that overrides the area type.
    /// </summary>
    /// <param name="areaType">The area type to use.</param>
    /// <param name="affectChildren">Whether to apply to children as well.</param>
    /// <returns>A new NavMeshModifier configured to override area type.</returns>
    public static NavMeshModifier CreateAreaOverride(NavAreaType areaType, bool affectChildren = true)
        => new()
        {
            OverrideAreaType = true,
            AreaType = areaType,
            AffectChildren = affectChildren,
            AgentTypeMask = -1
        };
}
