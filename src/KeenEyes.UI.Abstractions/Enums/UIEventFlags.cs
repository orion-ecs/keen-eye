namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Flags representing pending UI events on an element.
/// Used for polling-based event handling in systems.
/// </summary>
[Flags]
public enum UIEventType : ushort
{
    /// <summary>
    /// No pending events.
    /// </summary>
    None = 0,

    /// <summary>
    /// Pointer entered the element bounds.
    /// </summary>
    PointerEnter = 1 << 0,

    /// <summary>
    /// Pointer exited the element bounds.
    /// </summary>
    PointerExit = 1 << 1,

    /// <summary>
    /// Pointer button was pressed on the element.
    /// </summary>
    PointerDown = 1 << 2,

    /// <summary>
    /// Pointer button was released on the element.
    /// </summary>
    PointerUp = 1 << 3,

    /// <summary>
    /// Element was clicked (press and release on same element).
    /// </summary>
    Click = 1 << 4,

    /// <summary>
    /// Element was double-clicked.
    /// </summary>
    DoubleClick = 1 << 5,

    /// <summary>
    /// Drag operation started on the element.
    /// </summary>
    DragStart = 1 << 6,

    /// <summary>
    /// Drag operation ended.
    /// </summary>
    DragEnd = 1 << 7,

    /// <summary>
    /// Element gained keyboard focus.
    /// </summary>
    FocusGained = 1 << 8,

    /// <summary>
    /// Element lost keyboard focus.
    /// </summary>
    FocusLost = 1 << 9,

    /// <summary>
    /// Element's value changed (for input elements).
    /// </summary>
    ValueChanged = 1 << 10,

    /// <summary>
    /// Submit action was triggered (Enter key on focused element).
    /// </summary>
    Submit = 1 << 11
}
