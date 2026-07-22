using System.Numerics;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Describes an off-mesh connection to bake into a navigation mesh.
/// </summary>
/// <remarks>
/// <para>
/// This is the plain data shape consumed by mesh builders. It carries the same
/// information as the <see cref="Components.OffMeshLink"/> component without
/// coupling the builder to a world; callers collect link components (or any
/// other source of links) into definitions before building.
/// </para>
/// </remarks>
/// <param name="Start">The world-space start position of the connection.</param>
/// <param name="End">The world-space end position of the connection.</param>
/// <param name="Radius">The radius within which each endpoint snaps to the walkable surface.</param>
/// <param name="Bidirectional">
/// Whether the connection can be traversed in both directions. When false, it
/// is only traversable from <paramref name="Start"/> to <paramref name="End"/>.
/// </param>
/// <param name="AreaType">The navigation area type assigned to the connection polygon.</param>
/// <param name="CostModifier">
/// Modifier applied to agent movement speed while traversing the link.
/// Pathfinding cost is controlled by the area cost of <paramref name="AreaType"/>.
/// </param>
public sealed record OffMeshLinkDefinition(
    Vector3 Start,
    Vector3 End,
    float Radius,
    bool Bidirectional,
    NavAreaType AreaType,
    float CostModifier = 1f);
