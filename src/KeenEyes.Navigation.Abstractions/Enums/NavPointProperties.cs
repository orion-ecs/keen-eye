namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Bit flags describing special properties of a <see cref="NavPoint"/> within a path.
/// </summary>
/// <remarks>
/// Providers set these flags on path waypoints so that path-following logic can
/// react to special traversal segments without provider-specific knowledge.
/// </remarks>
[Flags]
public enum NavPointProperties
{
    /// <summary>
    /// No special properties.
    /// </summary>
    None = 0,

    /// <summary>
    /// The point is the entry of an off-mesh connection. The segment from this
    /// point to the next waypoint traverses the connection (jump, ladder,
    /// teleporter, etc.) rather than walkable surface.
    /// </summary>
    OffMeshConnection = 1
}
