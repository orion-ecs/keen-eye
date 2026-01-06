namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component indicating a node is in collapsed state.
/// </summary>
/// <remarks>
/// <para>
/// When a node is collapsed, it displays only the header bar with port stubs.
/// The expanded height is stored to restore the node when expanded.
/// </para>
/// <para>
/// Add this component to collapse a node; remove it to expand.
/// </para>
/// </remarks>
public struct GraphNodeCollapsed : IComponent
{
    /// <summary>
    /// The height of the node when expanded.
    /// </summary>
    /// <remarks>
    /// Stored when collapsing so the node can be restored to its original size.
    /// </remarks>
    public float ExpandedHeight;
}
