namespace KeenEyes.TestBridge.Navigation;

/// <summary>
/// Statistics about navigation system usage in the world.
/// </summary>
public sealed record NavigationStatisticsSnapshot
{
    /// <summary>
    /// Gets whether the navigation system is ready and initialized.
    /// </summary>
    public required bool IsReady { get; init; }

    /// <summary>
    /// Gets the name of the current navigation strategy.
    /// </summary>
    public required string Strategy { get; init; }

    /// <summary>
    /// Gets the total number of active navigation agents.
    /// </summary>
    public required int ActiveAgentCount { get; init; }

    /// <summary>
    /// Gets the number of pending path requests.
    /// </summary>
    public required int PendingRequestCount { get; init; }
}
