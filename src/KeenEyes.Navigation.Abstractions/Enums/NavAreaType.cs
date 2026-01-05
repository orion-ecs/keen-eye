namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Defines the type of navigation area for pathfinding cost calculations.
/// </summary>
/// <remarks>
/// <para>
/// Area types determine traversal costs and whether certain agents can
/// navigate through specific regions. Use <see cref="NavAreaMask"/> to
/// filter which areas an agent can traverse.
/// </para>
/// <para>
/// The numeric values correspond to bit positions for use with
/// <see cref="NavAreaMask"/> bitmask operations.
/// </para>
/// </remarks>
public enum NavAreaType
{
    /// <summary>
    /// Standard walkable terrain (default cost multiplier: 1.0).
    /// </summary>
    Walkable = 0,

    /// <summary>
    /// Water areas that may require swimming or special traversal.
    /// </summary>
    Water = 1,

    /// <summary>
    /// Road or paved surface with reduced movement cost.
    /// </summary>
    Road = 2,

    /// <summary>
    /// Grass or vegetation that may slow movement slightly.
    /// </summary>
    Grass = 3,

    /// <summary>
    /// Door or gate that may require interaction to traverse.
    /// </summary>
    Door = 4,

    /// <summary>
    /// Sand or soft terrain with increased movement cost.
    /// </summary>
    Sand = 5,

    /// <summary>
    /// Mud or difficult terrain with high movement cost.
    /// </summary>
    Mud = 6,

    /// <summary>
    /// Ice or slippery surface affecting movement.
    /// </summary>
    Ice = 7,

    /// <summary>
    /// Lava or hazardous terrain (may cause damage).
    /// </summary>
    Hazard = 8,

    /// <summary>
    /// Jump link requiring a jump action to traverse.
    /// </summary>
    Jump = 9,

    /// <summary>
    /// Ladder or climbable surface requiring climb action.
    /// </summary>
    Climb = 10,

    /// <summary>
    /// Off-mesh link for custom traversal behavior.
    /// </summary>
    OffMeshLink = 11,

    /// <summary>
    /// Not walkable - completely blocks navigation.
    /// </summary>
    NotWalkable = 31
}
