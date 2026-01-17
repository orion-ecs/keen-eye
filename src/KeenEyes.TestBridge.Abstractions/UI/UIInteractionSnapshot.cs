namespace KeenEyes.TestBridge.UI;

/// <summary>
/// Snapshot of a UI element's interaction state.
/// </summary>
public sealed record UIInteractionSnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets whether the element can receive keyboard focus.
    /// </summary>
    public required bool CanFocus { get; init; }

    /// <summary>
    /// Gets whether the element responds to click/tap events.
    /// </summary>
    public required bool CanClick { get; init; }

    /// <summary>
    /// Gets whether the element can be dragged.
    /// </summary>
    public required bool CanDrag { get; init; }

    /// <summary>
    /// Gets whether the element is currently hovered.
    /// </summary>
    public required bool IsHovered { get; init; }

    /// <summary>
    /// Gets whether the element is currently pressed.
    /// </summary>
    public required bool IsPressed { get; init; }

    /// <summary>
    /// Gets whether the element currently has focus.
    /// </summary>
    public required bool IsFocused { get; init; }

    /// <summary>
    /// Gets whether the element is being dragged.
    /// </summary>
    public required bool IsDragging { get; init; }
}
