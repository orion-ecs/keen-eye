namespace KeenEyes.TestBridge.UI;

/// <summary>
/// Statistics about UI usage in the world.
/// </summary>
public sealed record UIStatisticsSnapshot
{
    /// <summary>
    /// Gets the total number of UI elements.
    /// </summary>
    public required int TotalElementCount { get; init; }

    /// <summary>
    /// Gets the number of visible UI elements.
    /// </summary>
    public required int VisibleElementCount { get; init; }

    /// <summary>
    /// Gets the number of interactable UI elements.
    /// </summary>
    public required int InteractableCount { get; init; }

    /// <summary>
    /// Gets the entity ID of the currently focused element, or null if none.
    /// </summary>
    public required int? FocusedElementId { get; init; }
}
