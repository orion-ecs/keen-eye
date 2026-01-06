namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component tracking the currently focused widget in a graph canvas.
/// </summary>
/// <remarks>
/// <para>
/// Added to a canvas entity when a widget receives input focus. Stores the
/// node, widget ID, and any active edit state (e.g., text buffer for editable fields).
/// </para>
/// <para>
/// Only one widget can be focused at a time per canvas.
/// </para>
/// </remarks>
public struct WidgetFocus : IComponent
{
    /// <summary>
    /// The node entity containing the focused widget.
    /// </summary>
    public Entity Node;

    /// <summary>
    /// The unique identifier of the focused widget within the node.
    /// </summary>
    /// <remarks>
    /// Widget IDs are assigned by the node's RenderBody implementation and
    /// should be stable across frames.
    /// </remarks>
    public int WidgetId;

    /// <summary>
    /// The type of the focused widget.
    /// </summary>
    public WidgetType Type;

    /// <summary>
    /// The text buffer for editable fields (FloatField, IntField, TextArea).
    /// </summary>
    /// <remarks>
    /// Contains the current text being edited. Committed to the actual value
    /// when focus is lost or Enter is pressed.
    /// </remarks>
    public string EditBuffer;

    /// <summary>
    /// The cursor position within the edit buffer.
    /// </summary>
    public int CursorPosition;

    /// <summary>
    /// Whether the dropdown is currently expanded (for Dropdown widgets).
    /// </summary>
    public bool IsExpanded;

    /// <summary>
    /// The initial value when drag started (for Slider widgets).
    /// </summary>
    public float DragStartValue;
}
