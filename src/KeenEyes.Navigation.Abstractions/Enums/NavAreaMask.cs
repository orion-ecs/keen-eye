namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Bitmask for filtering navigable area types.
/// </summary>
/// <remarks>
/// <para>
/// Use this mask to specify which <see cref="NavAreaType"/> values an agent
/// can traverse. Combine flags using bitwise OR to allow multiple area types.
/// </para>
/// <para>
/// Example: <c>NavAreaMask.Walkable | NavAreaMask.Road</c> allows traversal
/// of both walkable terrain and roads.
/// </para>
/// </remarks>
[Flags]
public enum NavAreaMask : uint
{
    /// <summary>
    /// No areas are traversable.
    /// </summary>
    None = 0,

    /// <summary>
    /// Standard walkable terrain.
    /// </summary>
    Walkable = 1u << NavAreaType.Walkable,

    /// <summary>
    /// Water areas.
    /// </summary>
    Water = 1u << NavAreaType.Water,

    /// <summary>
    /// Road surfaces.
    /// </summary>
    Road = 1u << NavAreaType.Road,

    /// <summary>
    /// Grass areas.
    /// </summary>
    Grass = 1u << NavAreaType.Grass,

    /// <summary>
    /// Door areas.
    /// </summary>
    Door = 1u << NavAreaType.Door,

    /// <summary>
    /// Sand areas.
    /// </summary>
    Sand = 1u << NavAreaType.Sand,

    /// <summary>
    /// Mud areas.
    /// </summary>
    Mud = 1u << NavAreaType.Mud,

    /// <summary>
    /// Ice areas.
    /// </summary>
    Ice = 1u << NavAreaType.Ice,

    /// <summary>
    /// Hazard areas.
    /// </summary>
    Hazard = 1u << NavAreaType.Hazard,

    /// <summary>
    /// Jump links.
    /// </summary>
    Jump = 1u << NavAreaType.Jump,

    /// <summary>
    /// Climb surfaces.
    /// </summary>
    Climb = 1u << NavAreaType.Climb,

    /// <summary>
    /// Off-mesh links.
    /// </summary>
    OffMeshLink = 1u << NavAreaType.OffMeshLink,

    /// <summary>
    /// All standard ground-based areas (Walkable, Road, Grass, Sand).
    /// </summary>
    Ground = Walkable | Road | Grass | Sand,

    /// <summary>
    /// All traversable areas (excludes NotWalkable).
    /// </summary>
    All = uint.MaxValue & ~(1u << NavAreaType.NotWalkable)
}
