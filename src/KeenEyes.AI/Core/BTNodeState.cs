namespace KeenEyes.AI;

/// <summary>
/// Represents the execution state of a behavior tree node.
/// </summary>
public enum BTNodeState
{
    /// <summary>
    /// The node is still executing and will continue in the next tick.
    /// </summary>
    Running,

    /// <summary>
    /// The node completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The node failed to complete its task.
    /// </summary>
    Failure
}
