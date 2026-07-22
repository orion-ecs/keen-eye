using System.Numerics;

namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Component describing an off-mesh connection between two points on the
/// navigation mesh, such as a jump, ladder, or teleporter.
/// </summary>
/// <remarks>
/// <para>
/// Off-mesh links bridge parts of the navigation mesh that are not connected
/// by walkable surface. Entities carrying this component are collected when
/// the navigation mesh is built, and each link becomes an off-mesh connection
/// in the baked mesh.
/// </para>
/// <para>
/// <see cref="Start"/> and <see cref="End"/> are world-space positions. Both
/// must lie within <see cref="Radius"/> of the walkable surface for the link
/// to connect during the build.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.Spawn("LedgeJump")
///     .With(OffMeshLink.Create(new Vector3(5, 0, 5), new Vector3(19, 0, 5)))
///     .Build();
/// </code>
/// </example>
public struct OffMeshLink : IComponent
{
    /// <summary>
    /// The world-space start position of the link.
    /// </summary>
    public Vector3 Start;

    /// <summary>
    /// The world-space end position of the link.
    /// </summary>
    public Vector3 End;

    /// <summary>
    /// The radius within which each endpoint snaps to the navigation mesh.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Whether the link can be traversed in both directions.
    /// When false, agents can only travel from <see cref="Start"/> to <see cref="End"/>.
    /// </summary>
    public bool Bidirectional;

    /// <summary>
    /// The navigation area type assigned to the link.
    /// </summary>
    /// <remarks>
    /// The area type determines the pathfinding cost multiplier (settable via
    /// the provider's SetAreaCost) and which agents may use the link through
    /// their <see cref="NavAreaMask"/>.
    /// </remarks>
    public NavAreaType AreaType;

    /// <summary>
    /// Modifier applied to the agent's movement speed while traversing the link.
    /// </summary>
    /// <remarks>
    /// Traversal speed is the agent's speed divided by this value: a modifier of
    /// 2 makes crossing take twice as long. Pathfinding cost is controlled by
    /// the area cost of <see cref="AreaType"/>, not by this modifier.
    /// </remarks>
    public float CostModifier;

    /// <summary>
    /// Creates a bidirectional off-mesh link with default radius, area type, and cost.
    /// </summary>
    /// <param name="start">The world-space start position.</param>
    /// <param name="end">The world-space end position.</param>
    /// <returns>A new OffMeshLink component.</returns>
    public static OffMeshLink Create(Vector3 start, Vector3 end)
        => new()
        {
            Start = start,
            End = end,
            Radius = 0.5f,
            Bidirectional = true,
            AreaType = NavAreaType.OffMeshLink,
            CostModifier = 1f
        };
}
