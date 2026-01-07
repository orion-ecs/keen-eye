namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Flags representing the current interaction state of a UI element.
/// </summary>
[Flags]
public enum UIInteractionState : byte
{
    /// <summary>
    /// Element is in its normal, idle state.
    /// </summary>
    None = 0,

    /// <summary>
    /// Pointer is hovering over the element.
    /// </summary>
    Hovered = 1 << 0,

    /// <summary>
    /// Element is being pressed (pointer down).
    /// </summary>
    Pressed = 1 << 1,

    /// <summary>
    /// Element has keyboard focus.
    /// </summary>
    Focused = 1 << 2,

    /// <summary>
    /// Element is being dragged.
    /// </summary>
    Dragging = 1 << 3
}
