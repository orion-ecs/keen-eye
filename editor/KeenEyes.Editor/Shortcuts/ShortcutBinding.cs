namespace KeenEyes.Editor.Shortcuts;

/// <summary>
/// Represents a registered shortcut binding that maps a key combination to an action.
/// </summary>
/// <param name="ActionId">Unique identifier for this action (e.g., "file.save", "edit.undo").</param>
/// <param name="DisplayName">Human-readable name for the action.</param>
/// <param name="Category">Category for grouping in shortcut settings (e.g., "File", "Edit").</param>
/// <param name="DefaultShortcut">The default key combination for this action.</param>
public sealed class ShortcutBinding(
    string ActionId,
    string DisplayName,
    string Category,
    KeyCombination DefaultShortcut)
{
    /// <summary>
    /// Gets the unique identifier for this action.
    /// </summary>
    public string ActionId { get; } = ActionId;

    /// <summary>
    /// Gets the human-readable name for the action.
    /// </summary>
    public string DisplayName { get; } = DisplayName;

    /// <summary>
    /// Gets the category for grouping in shortcut settings.
    /// </summary>
    public string Category { get; } = Category;

    /// <summary>
    /// Gets the default key combination for this action.
    /// </summary>
    public KeyCombination DefaultShortcut { get; } = DefaultShortcut;

    /// <summary>
    /// Gets or sets the current key combination (may differ from default if rebound).
    /// </summary>
    public KeyCombination CurrentShortcut { get; set; } = DefaultShortcut;

    /// <summary>
    /// Gets or sets whether this shortcut is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the action to execute when this shortcut is triggered.
    /// </summary>
    public Action? Action { get; internal set; }

    /// <summary>
    /// Gets whether the current shortcut differs from the default.
    /// </summary>
    public bool IsCustomized => CurrentShortcut != DefaultShortcut;

    /// <summary>
    /// Resets the current shortcut to the default.
    /// </summary>
    public void ResetToDefault()
    {
        CurrentShortcut = DefaultShortcut;
    }
}
