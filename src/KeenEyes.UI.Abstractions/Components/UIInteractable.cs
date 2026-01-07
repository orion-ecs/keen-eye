namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that enables interaction (clicks, focus, drag) on a UI element.
/// </summary>
/// <remarks>
/// <para>
/// Add this component to any UI element that should respond to user input.
/// The <see cref="State"/> and <see cref="PendingEvents"/> fields are updated
/// automatically by the UI input system.
/// </para>
/// <para>
/// For keyboard navigation, set <see cref="CanFocus"/> to true and use
/// <see cref="TabIndex"/> to control tab order.
/// </para>
/// </remarks>
public struct UIInteractable : IComponent
{
    /// <summary>
    /// Whether this element can receive keyboard focus.
    /// </summary>
    public bool CanFocus;

    /// <summary>
    /// Whether this element responds to click/tap events.
    /// </summary>
    public bool CanClick;

    /// <summary>
    /// Whether this element can be dragged.
    /// </summary>
    public bool CanDrag;

    /// <summary>
    /// Tab order for keyboard navigation. Lower values are focused first.
    /// Elements with the same TabIndex are ordered by hierarchy.
    /// </summary>
    public int TabIndex;

    /// <summary>
    /// Current interaction state (hovered, pressed, focused, dragging).
    /// Set automatically by the UI input system.
    /// </summary>
    public UIInteractionState State;

    /// <summary>
    /// Pending events that occurred this frame.
    /// Cleared automatically at the start of each frame.
    /// </summary>
    public UIEventType PendingEvents;

    /// <summary>
    /// Creates a basic clickable interactable.
    /// </summary>
    public static UIInteractable Clickable() => new()
    {
        CanFocus = false,
        CanClick = true,
        CanDrag = false,
        TabIndex = 0
    };

    /// <summary>
    /// Creates a focusable button-like interactable.
    /// </summary>
    /// <param name="tabIndex">The tab order for keyboard navigation.</param>
    public static UIInteractable Button(int tabIndex = 0) => new()
    {
        CanFocus = true,
        CanClick = true,
        CanDrag = false,
        TabIndex = tabIndex
    };

    /// <summary>
    /// Creates a draggable interactable.
    /// </summary>
    public static UIInteractable Draggable() => new()
    {
        CanFocus = false,
        CanClick = false,
        CanDrag = true,
        TabIndex = 0
    };

    /// <summary>
    /// Checks if a specific event flag is set.
    /// </summary>
    /// <param name="flag">The event flag to check.</param>
    /// <returns>True if the event occurred this frame.</returns>
    public readonly bool HasEvent(UIEventType flag) => (PendingEvents & flag) != 0;

    /// <summary>
    /// Checks if the element is currently hovered.
    /// </summary>
    public readonly bool IsHovered => (State & UIInteractionState.Hovered) != 0;

    /// <summary>
    /// Checks if the element is currently pressed.
    /// </summary>
    public readonly bool IsPressed => (State & UIInteractionState.Pressed) != 0;

    /// <summary>
    /// Checks if the element currently has focus.
    /// </summary>
    public readonly bool IsFocused => (State & UIInteractionState.Focused) != 0;

    /// <summary>
    /// Checks if the element is being dragged.
    /// </summary>
    public readonly bool IsDragging => (State & UIInteractionState.Dragging) != 0;
}
