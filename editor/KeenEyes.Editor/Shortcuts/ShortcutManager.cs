using System.Text.Json;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Shortcuts;

/// <summary>
/// Manages keyboard shortcuts for the editor, including registration, rebinding, and persistence.
/// </summary>
public sealed class ShortcutManager
{
    private readonly Dictionary<string, ShortcutBinding> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<KeyCombination, string> _shortcutToAction = [];
    private KeyModifiers _currentModifiers;

    /// <summary>
    /// Raised when a shortcut conflict is detected during rebinding.
    /// </summary>
    public event EventHandler<ShortcutConflictEventArgs>? ConflictDetected;

    /// <summary>
    /// Raised when a shortcut binding changes.
    /// </summary>
    public event EventHandler<ShortcutChangedEventArgs>? ShortcutChanged;

    /// <summary>
    /// Gets all registered shortcut bindings.
    /// </summary>
    public IEnumerable<ShortcutBinding> Bindings => _bindings.Values;

    /// <summary>
    /// Gets all categories that have registered shortcuts.
    /// </summary>
    public IEnumerable<string> Categories => _bindings.Values.Select(b => b.Category).Distinct().Order();

    /// <summary>
    /// Gets bindings for a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Bindings in the specified category.</returns>
    public IEnumerable<ShortcutBinding> GetBindingsForCategory(string category)
    {
        return _bindings.Values.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Registers a new shortcut action.
    /// </summary>
    /// <param name="actionId">Unique identifier for this action.</param>
    /// <param name="displayName">Human-readable name for the action.</param>
    /// <param name="category">Category for grouping.</param>
    /// <param name="defaultShortcut">The default key combination.</param>
    /// <param name="action">The action to execute when triggered.</param>
    /// <returns>The created binding for further configuration.</returns>
    /// <exception cref="ArgumentException">Thrown if an action with this ID already exists.</exception>
    public ShortcutBinding Register(
        string actionId,
        string displayName,
        string category,
        KeyCombination defaultShortcut,
        Action action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentNullException.ThrowIfNull(action);

        if (_bindings.ContainsKey(actionId))
        {
            throw new ArgumentException($"Action '{actionId}' is already registered.", nameof(actionId));
        }

        var binding = new ShortcutBinding(actionId, displayName, category, defaultShortcut)
        {
            Action = action
        };

        _bindings[actionId] = binding;

        if (defaultShortcut.IsValid)
        {
            _shortcutToAction[defaultShortcut] = actionId;
        }

        return binding;
    }

    /// <summary>
    /// Registers a shortcut using a string representation (e.g., "Ctrl+S").
    /// </summary>
    /// <param name="actionId">Unique identifier for this action.</param>
    /// <param name="displayName">Human-readable name for the action.</param>
    /// <param name="category">Category for grouping.</param>
    /// <param name="shortcutString">String representation of the shortcut.</param>
    /// <param name="action">The action to execute when triggered.</param>
    /// <returns>The created binding for further configuration.</returns>
    public ShortcutBinding Register(
        string actionId,
        string displayName,
        string category,
        string shortcutString,
        Action action)
    {
        return Register(actionId, displayName, category, KeyCombination.Parse(shortcutString), action);
    }

    /// <summary>
    /// Gets the binding for a specific action ID.
    /// </summary>
    /// <param name="actionId">The action ID to look up.</param>
    /// <returns>The binding, or null if not found.</returns>
    public ShortcutBinding? GetBinding(string actionId)
    {
        return _bindings.GetValueOrDefault(actionId);
    }

    /// <summary>
    /// Gets the current shortcut for an action.
    /// </summary>
    /// <param name="actionId">The action ID to look up.</param>
    /// <returns>The current key combination, or <see cref="KeyCombination.None"/> if not found.</returns>
    public KeyCombination GetShortcut(string actionId)
    {
        return _bindings.TryGetValue(actionId, out var binding)
            ? binding.CurrentShortcut
            : KeyCombination.None;
    }

    /// <summary>
    /// Gets the shortcut display string for an action (for menu hints).
    /// </summary>
    /// <param name="actionId">The action ID to look up.</param>
    /// <returns>The shortcut string, or null if not bound.</returns>
    public string? GetShortcutString(string actionId)
    {
        var shortcut = GetShortcut(actionId);
        return shortcut.IsValid ? shortcut.ToString() : null;
    }

    /// <summary>
    /// Rebinds an action to a new key combination.
    /// </summary>
    /// <param name="actionId">The action ID to rebind.</param>
    /// <param name="newShortcut">The new key combination.</param>
    /// <returns>True if successful, false if there was a conflict.</returns>
    public bool Rebind(string actionId, KeyCombination newShortcut)
    {
        if (!_bindings.TryGetValue(actionId, out var binding))
        {
            return false;
        }

        // Check for conflicts
        if (newShortcut.IsValid &&
            _shortcutToAction.TryGetValue(newShortcut, out var conflictingAction) &&
            !conflictingAction.Equals(actionId, StringComparison.OrdinalIgnoreCase))
        {
            var conflictBinding = _bindings[conflictingAction];
            ConflictDetected?.Invoke(this, new ShortcutConflictEventArgs(binding, conflictBinding, newShortcut));
            return false;
        }

        // Remove old shortcut mapping
        var oldShortcut = binding.CurrentShortcut;
        if (oldShortcut.IsValid)
        {
            _shortcutToAction.Remove(oldShortcut);
        }

        // Update binding and mapping
        binding.CurrentShortcut = newShortcut;
        if (newShortcut.IsValid)
        {
            _shortcutToAction[newShortcut] = actionId;
        }

        ShortcutChanged?.Invoke(this, new ShortcutChangedEventArgs(binding, oldShortcut, newShortcut));
        return true;
    }

    /// <summary>
    /// Rebinds an action using a string representation.
    /// </summary>
    /// <param name="actionId">The action ID to rebind.</param>
    /// <param name="shortcutString">String representation of the new shortcut.</param>
    /// <returns>True if successful, false if there was a conflict.</returns>
    public bool Rebind(string actionId, string shortcutString)
    {
        return Rebind(actionId, KeyCombination.Parse(shortcutString));
    }

    /// <summary>
    /// Resets a specific shortcut to its default binding.
    /// </summary>
    /// <param name="actionId">The action ID to reset.</param>
    public void ResetToDefault(string actionId)
    {
        if (_bindings.TryGetValue(actionId, out var binding))
        {
            Rebind(actionId, binding.DefaultShortcut);
        }
    }

    /// <summary>
    /// Resets all shortcuts to their default bindings.
    /// </summary>
    public void ResetAllToDefaults()
    {
        foreach (var binding in _bindings.Values)
        {
            Rebind(binding.ActionId, binding.DefaultShortcut);
        }
    }

    /// <summary>
    /// Processes a key down event and triggers matching shortcuts.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The active modifier keys.</param>
    /// <returns>True if a shortcut was triggered.</returns>
    public bool ProcessKeyDown(Key key, KeyModifiers modifiers)
    {
        _currentModifiers = modifiers;

        // Don't trigger shortcuts for modifier keys alone
        if (IsModifierKey(key))
        {
            return false;
        }

        var combination = new KeyCombination(key, modifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super));

        if (_shortcutToAction.TryGetValue(combination, out var actionId) &&
            _bindings.TryGetValue(actionId, out var binding) &&
            binding.IsEnabled &&
            binding.Action != null)
        {
            binding.Action.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Processes a key up event to track modifier state.
    /// </summary>
    /// <param name="key">The key that was released.</param>
    /// <param name="modifiers">The active modifier keys.</param>
    public void ProcessKeyUp(Key key, KeyModifiers modifiers)
    {
        _currentModifiers = modifiers;
    }

    /// <summary>
    /// Gets the current modifier key state.
    /// </summary>
    public KeyModifiers CurrentModifiers => _currentModifiers;

    /// <summary>
    /// Finds which action is bound to a key combination.
    /// </summary>
    /// <param name="shortcut">The key combination to look up.</param>
    /// <returns>The action ID, or null if not bound.</returns>
    public string? FindActionForShortcut(KeyCombination shortcut)
    {
        return _shortcutToAction.GetValueOrDefault(shortcut);
    }

    /// <summary>
    /// Saves custom shortcut bindings to a JSON file.
    /// </summary>
    /// <param name="filePath">Path to save to.</param>
    public void Save(string filePath)
    {
        var customBindings = _bindings.Values
            .Where(b => b.IsCustomized)
            .ToDictionary(b => b.ActionId, b => b.CurrentShortcut.ToString());

        var json = JsonSerializer.Serialize(customBindings, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads custom shortcut bindings from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to load from.</param>
    public void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var customBindings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (customBindings != null)
            {
                foreach (var (actionId, shortcutString) in customBindings)
                {
                    var shortcut = KeyCombination.Parse(shortcutString);
                    if (shortcut.IsValid && _bindings.ContainsKey(actionId))
                    {
                        Rebind(actionId, shortcut);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load shortcuts: {ex.Message}");
        }
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftShift or Key.RightShift
            or Key.LeftControl or Key.RightControl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftSuper or Key.RightSuper;
    }
}

/// <summary>
/// Event arguments for shortcut conflict detection.
/// </summary>
/// <param name="Binding">The binding being modified.</param>
/// <param name="ConflictingBinding">The existing binding that conflicts.</param>
/// <param name="AttemptedShortcut">The shortcut that was attempted.</param>
public sealed class ShortcutConflictEventArgs(
    ShortcutBinding Binding,
    ShortcutBinding ConflictingBinding,
    KeyCombination AttemptedShortcut) : EventArgs
{
    /// <summary>Gets the binding being modified.</summary>
    public ShortcutBinding Binding { get; } = Binding;

    /// <summary>Gets the existing binding that conflicts.</summary>
    public ShortcutBinding ConflictingBinding { get; } = ConflictingBinding;

    /// <summary>Gets the shortcut that was attempted.</summary>
    public KeyCombination AttemptedShortcut { get; } = AttemptedShortcut;
}

/// <summary>
/// Event arguments for shortcut change notifications.
/// </summary>
/// <param name="Binding">The binding that changed.</param>
/// <param name="OldShortcut">The previous shortcut.</param>
/// <param name="NewShortcut">The new shortcut.</param>
public sealed class ShortcutChangedEventArgs(
    ShortcutBinding Binding,
    KeyCombination OldShortcut,
    KeyCombination NewShortcut) : EventArgs
{
    /// <summary>Gets the binding that changed.</summary>
    public ShortcutBinding Binding { get; } = Binding;

    /// <summary>Gets the previous shortcut.</summary>
    public KeyCombination OldShortcut { get; } = OldShortcut;

    /// <summary>Gets the new shortcut.</summary>
    public KeyCombination NewShortcut { get; } = NewShortcut;
}
