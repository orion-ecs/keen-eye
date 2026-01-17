namespace KeenEyes.TestBridge.UI;

/// <summary>
/// Snapshot of a UI element's state.
/// </summary>
public sealed record UIElementSnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets the element's name, or null if unnamed.
    /// </summary>
    public required string? Name { get; init; }

    /// <summary>
    /// Gets whether the element is visible.
    /// </summary>
    public required bool IsVisible { get; init; }

    /// <summary>
    /// Gets whether the element can receive pointer events.
    /// </summary>
    public required bool IsRaycastTarget { get; init; }

    /// <summary>
    /// Gets the parent entity ID, or null if root.
    /// </summary>
    public required int? ParentId { get; init; }

    /// <summary>
    /// Gets the list of child entity IDs.
    /// </summary>
    public required IReadOnlyList<int> ChildIds { get; init; }

    /// <summary>
    /// Gets whether the element has a UIInteractable component.
    /// </summary>
    public required bool HasInteractable { get; init; }

    /// <summary>
    /// Gets whether the element has a UIText component.
    /// </summary>
    public required bool HasText { get; init; }

    /// <summary>
    /// Gets whether the element has a UIStyle component.
    /// </summary>
    public required bool HasStyle { get; init; }
}
