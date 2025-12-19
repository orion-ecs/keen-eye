namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that enables text editing functionality for an element.
/// </summary>
/// <remarks>
/// <para>
/// When present alongside <see cref="UIText"/>, the element becomes an editable text field.
/// The UITextInputSystem processes keyboard input when the element is focused.
/// </para>
/// <para>
/// The element must have <see cref="UIInteractable.CanFocus"/> set to true
/// and should receive focus through clicking or tab navigation.
/// </para>
/// </remarks>
public struct UITextInput : IComponent
{
    /// <summary>
    /// Current cursor position within the text (0-based character index).
    /// </summary>
    public int CursorPosition;

    /// <summary>
    /// Start of text selection. If equal to <see cref="SelectionEnd"/>, no text is selected.
    /// </summary>
    public int SelectionStart;

    /// <summary>
    /// End of text selection. If equal to <see cref="SelectionStart"/>, no text is selected.
    /// </summary>
    public int SelectionEnd;

    /// <summary>
    /// Whether the text field is currently in editing mode (focused and accepting input).
    /// </summary>
    public bool IsEditing;

    /// <summary>
    /// Maximum number of characters allowed. 0 means unlimited.
    /// </summary>
    public int MaxLength;

    /// <summary>
    /// Whether to allow multiple lines of text.
    /// </summary>
    public bool Multiline;

    /// <summary>
    /// The placeholder text to display when the field is empty.
    /// </summary>
    public string PlaceholderText;

    /// <summary>
    /// Whether the field currently shows placeholder text.
    /// </summary>
    public bool ShowingPlaceholder;

    /// <summary>
    /// Creates a single-line text input configuration.
    /// </summary>
    /// <param name="placeholder">Placeholder text when empty.</param>
    /// <param name="maxLength">Maximum characters (0 for unlimited).</param>
    public static UITextInput SingleLine(string placeholder = "", int maxLength = 0) => new()
    {
        CursorPosition = 0,
        SelectionStart = 0,
        SelectionEnd = 0,
        IsEditing = false,
        MaxLength = maxLength,
        Multiline = false,
        PlaceholderText = placeholder,
        ShowingPlaceholder = true
    };

    /// <summary>
    /// Creates a multi-line text input configuration.
    /// </summary>
    /// <param name="placeholder">Placeholder text when empty.</param>
    /// <param name="maxLength">Maximum characters (0 for unlimited).</param>
    public static UITextInput MultiLine(string placeholder = "", int maxLength = 0) => new()
    {
        CursorPosition = 0,
        SelectionStart = 0,
        SelectionEnd = 0,
        IsEditing = false,
        MaxLength = maxLength,
        Multiline = true,
        PlaceholderText = placeholder,
        ShowingPlaceholder = true
    };

    /// <summary>
    /// Gets whether any text is currently selected.
    /// </summary>
    public readonly bool HasSelection => SelectionStart != SelectionEnd;

    /// <summary>
    /// Gets the normalized selection range (start is always less than end).
    /// </summary>
    public readonly (int Start, int End) GetSelectionRange()
    {
        return SelectionStart <= SelectionEnd
            ? (SelectionStart, SelectionEnd)
            : (SelectionEnd, SelectionStart);
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectionStart = CursorPosition;
        SelectionEnd = CursorPosition;
    }
}

/// <summary>
/// Event raised when the text content of a text input field changes.
/// </summary>
/// <param name="Element">The text input entity that changed.</param>
/// <param name="OldText">The previous text content.</param>
/// <param name="NewText">The new text content.</param>
public readonly record struct UITextChangedEvent(Entity Element, string OldText, string NewText);
