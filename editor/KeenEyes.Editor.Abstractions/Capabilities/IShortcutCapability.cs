namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for registering and managing keyboard shortcuts.
/// Allows plugins to add custom shortcuts with conflict detection and rebinding support.
/// </summary>
public interface IShortcutCapability : IEditorCapability
{
    /// <summary>
    /// Registers a new keyboard shortcut.
    /// </summary>
    /// <param name="actionId">The unique identifier for the action.</param>
    /// <param name="displayName">The display name shown in the shortcut editor.</param>
    /// <param name="category">The category for organizing shortcuts.</param>
    /// <param name="defaultShortcut">The default keyboard shortcut (e.g., "Ctrl+S").</param>
    /// <param name="action">The action to execute when the shortcut is triggered.</param>
    /// <returns>The created shortcut binding.</returns>
    ShortcutBinding RegisterShortcut(
        string actionId,
        string displayName,
        string category,
        string defaultShortcut,
        Action action);

    /// <summary>
    /// Registers a new keyboard shortcut with a can-execute predicate.
    /// </summary>
    /// <param name="actionId">The unique identifier for the action.</param>
    /// <param name="displayName">The display name shown in the shortcut editor.</param>
    /// <param name="category">The category for organizing shortcuts.</param>
    /// <param name="defaultShortcut">The default keyboard shortcut (e.g., "Ctrl+S").</param>
    /// <param name="action">The action to execute when the shortcut is triggered.</param>
    /// <param name="canExecute">A predicate that determines if the action can be executed.</param>
    /// <returns>The created shortcut binding.</returns>
    ShortcutBinding RegisterShortcut(
        string actionId,
        string displayName,
        string category,
        string defaultShortcut,
        Action action,
        Func<bool> canExecute);

    /// <summary>
    /// Unregisters a shortcut by its action ID.
    /// </summary>
    /// <param name="actionId">The action ID to unregister.</param>
    /// <returns>True if the shortcut was found and unregistered.</returns>
    bool UnregisterShortcut(string actionId);

    /// <summary>
    /// Gets a shortcut binding by its action ID.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <returns>The shortcut binding, or null if not found.</returns>
    ShortcutBinding? GetShortcut(string actionId);

    /// <summary>
    /// Gets all shortcuts in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>The shortcuts in the specified category.</returns>
    IEnumerable<ShortcutBinding> GetShortcutsInCategory(string category);

    /// <summary>
    /// Gets all registered shortcuts.
    /// </summary>
    /// <returns>All registered shortcuts.</returns>
    IEnumerable<ShortcutBinding> GetAllShortcuts();

    /// <summary>
    /// Gets all shortcut categories.
    /// </summary>
    /// <returns>The category names.</returns>
    IEnumerable<string> GetCategories();

    /// <summary>
    /// Rebinds a shortcut to a new key combination.
    /// </summary>
    /// <param name="actionId">The action ID to rebind.</param>
    /// <param name="newShortcut">The new keyboard shortcut.</param>
    /// <returns>True if the rebind was successful.</returns>
    bool RebindShortcut(string actionId, string newShortcut);

    /// <summary>
    /// Resets a shortcut to its default key combination.
    /// </summary>
    /// <param name="actionId">The action ID to reset.</param>
    /// <returns>True if the reset was successful.</returns>
    bool ResetShortcut(string actionId);

    /// <summary>
    /// Resets all shortcuts to their default key combinations.
    /// </summary>
    void ResetAllShortcuts();

    /// <summary>
    /// Checks if a shortcut string conflicts with an existing binding.
    /// </summary>
    /// <param name="shortcut">The shortcut string to check.</param>
    /// <param name="excludeActionId">An action ID to exclude from the conflict check.</param>
    /// <returns>The conflicting shortcut binding, or null if no conflict.</returns>
    ShortcutBinding? FindConflict(string shortcut, string? excludeActionId = null);

    /// <summary>
    /// Processes a key event and executes any matching shortcuts.
    /// </summary>
    /// <param name="keyEvent">The key event to process.</param>
    /// <returns>True if a shortcut was executed.</returns>
    bool ProcessKeyEvent(KeyEvent keyEvent);

    /// <summary>
    /// Event raised when a shortcut is registered.
    /// </summary>
    event Action<ShortcutBinding>? ShortcutRegistered;

    /// <summary>
    /// Event raised when a shortcut is unregistered.
    /// </summary>
    event Action<string>? ShortcutUnregistered;

    /// <summary>
    /// Event raised when a shortcut is rebound.
    /// </summary>
    event Action<string, string, string>? ShortcutRebound;
}

/// <summary>
/// Represents a keyboard shortcut binding.
/// </summary>
public sealed class ShortcutBinding
{
    /// <summary>
    /// Gets the unique identifier for the action.
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// Gets the display name shown in the shortcut editor.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the category for organizing shortcuts.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the default keyboard shortcut.
    /// </summary>
    public required string DefaultShortcut { get; init; }

    /// <summary>
    /// Gets or sets the current keyboard shortcut.
    /// </summary>
    public string CurrentShortcut { get; set; } = string.Empty;

    /// <summary>
    /// Gets the action to execute.
    /// </summary>
    public required Action Execute { get; init; }

    /// <summary>
    /// Gets the predicate that determines if the action can be executed.
    /// </summary>
    public Func<bool>? CanExecute { get; init; }

    /// <summary>
    /// Gets whether the shortcut has been modified from its default.
    /// </summary>
    public bool IsModified => CurrentShortcut != DefaultShortcut;

    /// <summary>
    /// Gets whether this shortcut is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the parsed key combination for the current shortcut.
    /// </summary>
    public KeyCombination? ParsedShortcut { get; set; }
}

/// <summary>
/// Represents a key combination for shortcuts.
/// </summary>
public readonly struct KeyCombination : IEquatable<KeyCombination>
{
    /// <summary>
    /// Gets the key modifiers (Ctrl, Alt, Shift, etc.).
    /// </summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>
    /// Gets the primary key.
    /// </summary>
    public Key Key { get; init; }

    /// <summary>
    /// Creates a new key combination.
    /// </summary>
    public KeyCombination(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Parses a shortcut string like "Ctrl+Shift+S" into a KeyCombination.
    /// </summary>
    /// <param name="shortcut">The shortcut string to parse.</param>
    /// <returns>The parsed key combination, or null if invalid.</returns>
    public static KeyCombination? Parse(string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return null;
        }

        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = KeyModifiers.None;
        Key? key = null;

        foreach (var part in parts)
        {
            var normalized = part.ToUpperInvariant();

            // Check for modifiers
            if (normalized is "CTRL" or "CONTROL")
            {
                modifiers |= KeyModifiers.Control;
            }
            else if (normalized is "ALT")
            {
                modifiers |= KeyModifiers.Alt;
            }
            else if (normalized is "SHIFT")
            {
                modifiers |= KeyModifiers.Shift;
            }
            else if (normalized is "CMD" or "COMMAND" or "META" or "WIN" or "SUPER")
            {
                modifiers |= KeyModifiers.Meta;
            }
            else if (Enum.TryParse<Key>(normalized, true, out var parsedKey))
            {
                key = parsedKey;
            }
            else
            {
                // Try single character keys
                if (normalized.Length == 1 && char.IsLetterOrDigit(normalized[0]) &&
                    Enum.TryParse<Key>(normalized, true, out var charKey))
                {
                    key = charKey;
                }
            }
        }

        if (key is null)
        {
            return null;
        }

        return new KeyCombination(key.Value, modifiers);
    }

    /// <summary>
    /// Converts the key combination to a display string.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(KeyModifiers.Meta))
        {
            parts.Add("Cmd");
        }

        parts.Add(Key.ToString());

        return string.Join("+", parts);
    }

    /// <inheritdoc/>
    public bool Equals(KeyCombination other) =>
        Key == other.Key && Modifiers == other.Modifiers;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is KeyCombination other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Key, Modifiers);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(KeyCombination left, KeyCombination right) =>
        left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(KeyCombination left, KeyCombination right) =>
        !left.Equals(right);
}

/// <summary>
/// Key modifier flags.
/// </summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>
    /// No modifiers.
    /// </summary>
    None = 0,

    /// <summary>
    /// Control key (Ctrl on Windows/Linux, Control on macOS).
    /// </summary>
    Control = 1 << 0,

    /// <summary>
    /// Alt key (Alt on Windows/Linux, Option on macOS).
    /// </summary>
    Alt = 1 << 1,

    /// <summary>
    /// Shift key.
    /// </summary>
    Shift = 1 << 2,

    /// <summary>
    /// Meta key (Windows key on Windows, Command on macOS).
    /// </summary>
    Meta = 1 << 3
}

/// <summary>
/// Represents a keyboard key event.
/// </summary>
public sealed class KeyEvent
{
    /// <summary>
    /// Gets the key that was pressed or released.
    /// </summary>
    public required Key Key { get; init; }

    /// <summary>
    /// Gets the current modifiers.
    /// </summary>
    public required KeyModifiers Modifiers { get; init; }

    /// <summary>
    /// Gets whether this is a key down event.
    /// </summary>
    public required bool IsDown { get; init; }

    /// <summary>
    /// Gets whether this is a key repeat event.
    /// </summary>
    public bool IsRepeat { get; init; }

    /// <summary>
    /// Gets the key combination for this event.
    /// </summary>
    public KeyCombination Combination => new(Key, Modifiers);
}

/// <summary>
/// Standard shortcut categories.
/// </summary>
public static class ShortcutCategories
{
    /// <summary>
    /// File operations (save, open, etc.).
    /// </summary>
    public const string File = "File";

    /// <summary>
    /// Edit operations (undo, redo, copy, paste, etc.).
    /// </summary>
    public const string Edit = "Edit";

    /// <summary>
    /// View operations (toggle panels, zoom, etc.).
    /// </summary>
    public const string View = "View";

    /// <summary>
    /// Selection operations.
    /// </summary>
    public const string Selection = "Selection";

    /// <summary>
    /// Transform operations.
    /// </summary>
    public const string Transform = "Transform";

    /// <summary>
    /// Play mode operations.
    /// </summary>
    public const string PlayMode = "Play Mode";

    /// <summary>
    /// Navigation operations.
    /// </summary>
    public const string Navigation = "Navigation";

    /// <summary>
    /// Tool activation shortcuts.
    /// </summary>
    public const string Tools = "Tools";

    /// <summary>
    /// Custom plugin shortcuts.
    /// </summary>
    public const string Custom = "Custom";
}

/// <summary>
/// Common keyboard keys.
/// </summary>
public enum Key
{
    /// <summary>Unknown key.</summary>
    Unknown = 0,

    // Letters
    /// <summary>A key.</summary>
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    /// <summary>N key.</summary>
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    /// <summary>0 key.</summary>
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Function keys
    /// <summary>F1 key.</summary>
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Special keys
    /// <summary>Escape key.</summary>
    Escape,
    /// <summary>Tab key.</summary>
    Tab,
    /// <summary>Space key.</summary>
    Space,
    /// <summary>Enter key.</summary>
    Enter,
    /// <summary>Backspace key.</summary>
    Backspace,
    /// <summary>Delete key.</summary>
    Delete,
    /// <summary>Insert key.</summary>
    Insert,
    /// <summary>Home key.</summary>
    Home,
    /// <summary>End key.</summary>
    End,
    /// <summary>Page Up key.</summary>
    PageUp,
    /// <summary>Page Down key.</summary>
    PageDown,

    // Arrow keys
    /// <summary>Left arrow key.</summary>
    Left,
    /// <summary>Right arrow key.</summary>
    Right,
    /// <summary>Up arrow key.</summary>
    Up,
    /// <summary>Down arrow key.</summary>
    Down,

    // Numpad
    /// <summary>Numpad 0.</summary>
    NumPad0, NumPad1, NumPad2, NumPad3, NumPad4,
    /// <summary>Numpad 5.</summary>
    NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
    /// <summary>Numpad Add.</summary>
    NumPadAdd,
    /// <summary>Numpad Subtract.</summary>
    NumPadSubtract,
    /// <summary>Numpad Multiply.</summary>
    NumPadMultiply,
    /// <summary>Numpad Divide.</summary>
    NumPadDivide,
    /// <summary>Numpad Decimal.</summary>
    NumPadDecimal,
    /// <summary>Numpad Enter.</summary>
    NumPadEnter,

    // Punctuation
    /// <summary>Minus key.</summary>
    Minus,
    /// <summary>Equals key.</summary>
    Equals,
    /// <summary>Left bracket key.</summary>
    LeftBracket,
    /// <summary>Right bracket key.</summary>
    RightBracket,
    /// <summary>Backslash key.</summary>
    Backslash,
    /// <summary>Semicolon key.</summary>
    Semicolon,
    /// <summary>Apostrophe key.</summary>
    Apostrophe,
    /// <summary>Comma key.</summary>
    Comma,
    /// <summary>Period key.</summary>
    Period,
    /// <summary>Slash key.</summary>
    Slash,
    /// <summary>Grave accent key.</summary>
    GraveAccent
}
