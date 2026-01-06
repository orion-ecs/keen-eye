namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Types of interactive widgets that can appear in node bodies.
/// </summary>
public enum WidgetType
{
    /// <summary>
    /// No widget type specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Editable floating-point number field.
    /// </summary>
    FloatField,

    /// <summary>
    /// Editable integer number field.
    /// </summary>
    IntField,

    /// <summary>
    /// Color picker with swatch and popup.
    /// </summary>
    ColorPicker,

    /// <summary>
    /// Dropdown selection with expandable options.
    /// </summary>
    Dropdown,

    /// <summary>
    /// Draggable slider for numeric range.
    /// </summary>
    Slider,

    /// <summary>
    /// Multi-line text editor.
    /// </summary>
    TextArea
}
