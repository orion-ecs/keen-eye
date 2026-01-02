namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for customizing editor menus and toolbars.
/// Allows plugins to add menu items, context menus, and toolbar buttons.
/// </summary>
public interface IMenuCapability : IEditorCapability
{
    /// <summary>
    /// Adds a menu item to the main menu bar.
    /// </summary>
    /// <param name="path">The menu path (e.g., "File/Export/Scene").</param>
    /// <param name="command">The command to execute when clicked.</param>
    void AddMenuItem(MenuPath path, EditorCommand command);

    /// <summary>
    /// Adds a context menu item that appears when right-clicking on specific types.
    /// </summary>
    /// <typeparam name="T">The target type for the context menu.</typeparam>
    /// <param name="path">The menu path within the context menu.</param>
    /// <param name="command">The command to execute when clicked.</param>
    void AddContextMenuItem<T>(MenuPath path, EditorCommand<T> command);

    /// <summary>
    /// Adds a button to the editor toolbar.
    /// </summary>
    /// <param name="section">The toolbar section to add to.</param>
    /// <param name="command">The command to execute when clicked.</param>
    void AddToolbarButton(ToolbarSection section, EditorCommand command);

    /// <summary>
    /// Removes a menu item.
    /// </summary>
    /// <param name="path">The menu path to remove.</param>
    /// <returns>True if the menu item was found and removed.</returns>
    bool RemoveMenuItem(MenuPath path);

    /// <summary>
    /// Removes a toolbar button.
    /// </summary>
    /// <param name="commandId">The ID of the command to remove.</param>
    /// <returns>True if the button was found and removed.</returns>
    bool RemoveToolbarButton(string commandId);

    /// <summary>
    /// Gets all menu items at a given path.
    /// </summary>
    /// <param name="parentPath">The parent menu path.</param>
    /// <returns>The child menu items.</returns>
    IEnumerable<MenuItemInfo> GetMenuItems(MenuPath parentPath);
}

/// <summary>
/// Represents a path in the menu hierarchy.
/// </summary>
public readonly struct MenuPath : IEquatable<MenuPath>
{
    private readonly string[] segments;

    /// <summary>
    /// Creates a new menu path from segments.
    /// </summary>
    /// <param name="segments">The path segments.</param>
    public MenuPath(params string[] segments)
    {
        this.segments = segments ?? [];
    }

    /// <summary>
    /// Creates a menu path from a string like "File/Export/Scene".
    /// </summary>
    /// <param name="path">The path string.</param>
    /// <returns>The parsed menu path.</returns>
    public static MenuPath Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new MenuPath([]);
        }

        return new MenuPath(path.Split('/'));
    }

    /// <summary>
    /// Gets the path segments.
    /// </summary>
    public ReadOnlySpan<string> Segments => segments ?? [];

    /// <summary>
    /// Gets the parent path (all segments except the last).
    /// </summary>
    public MenuPath Parent => segments.Length > 1
        ? new MenuPath(segments[..^1])
        : new MenuPath([]);

    /// <summary>
    /// Gets the final segment (the menu item name).
    /// </summary>
    public string Name => segments.Length > 0 ? segments[^1] : string.Empty;

    /// <summary>
    /// Gets whether this is a root-level menu.
    /// </summary>
    public bool IsRoot => segments.Length <= 1;

    /// <summary>
    /// Gets the full path as a string.
    /// </summary>
    public override string ToString() => string.Join("/", segments ?? []);

    /// <inheritdoc/>
    public bool Equals(MenuPath other) =>
        segments.SequenceEqual(other.segments ?? []);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is MenuPath other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var segment in segments ?? [])
        {
            hash.Add(segment);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Implicit conversion from string.
    /// </summary>
    public static implicit operator MenuPath(string path) => Parse(path);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(MenuPath left, MenuPath right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(MenuPath left, MenuPath right) => !left.Equals(right);
}

/// <summary>
/// Represents an editor command that can be invoked from menus or buttons.
/// </summary>
public class EditorCommand
{
    /// <summary>
    /// Gets the unique identifier for this command.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the command.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the action to execute.
    /// </summary>
    public required Action Execute { get; init; }

    /// <summary>
    /// Gets a function that determines if the command can be executed.
    /// </summary>
    public Func<bool>? CanExecute { get; init; }

    /// <summary>
    /// Gets the optional keyboard shortcut.
    /// </summary>
    public string? Shortcut { get; init; }

    /// <summary>
    /// Gets the optional icon identifier.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the optional tooltip text.
    /// </summary>
    public string? Tooltip { get; init; }
}

/// <summary>
/// Represents an editor command that operates on a specific target type.
/// Used for context menu commands where an action needs a target object.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
public sealed class EditorCommand<T>
{
    /// <summary>
    /// Gets the unique identifier for this command.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the command.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the action to execute with the target.
    /// </summary>
    public required Action<T> Execute { get; init; }

    /// <summary>
    /// Gets a function that determines if the command can be executed on a target.
    /// </summary>
    public Func<T, bool>? CanExecute { get; init; }

    /// <summary>
    /// Gets the optional keyboard shortcut.
    /// </summary>
    public string? Shortcut { get; init; }

    /// <summary>
    /// Gets the optional icon identifier.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the optional tooltip text.
    /// </summary>
    public string? Tooltip { get; init; }
}

/// <summary>
/// Defines standard toolbar sections.
/// </summary>
public enum ToolbarSection
{
    /// <summary>
    /// File operations (save, load, etc.).
    /// </summary>
    File,

    /// <summary>
    /// Edit operations (undo, redo, etc.).
    /// </summary>
    Edit,

    /// <summary>
    /// Play mode controls.
    /// </summary>
    PlayMode,

    /// <summary>
    /// Transform tools (move, rotate, scale).
    /// </summary>
    Tools,

    /// <summary>
    /// View options (grid, gizmos, etc.).
    /// </summary>
    View,

    /// <summary>
    /// Custom plugin section.
    /// </summary>
    Custom
}

/// <summary>
/// Information about a registered menu item.
/// </summary>
public sealed class MenuItemInfo
{
    /// <summary>
    /// Gets the menu path.
    /// </summary>
    public required MenuPath Path { get; init; }

    /// <summary>
    /// Gets the associated command.
    /// </summary>
    public required EditorCommand Command { get; init; }

    /// <summary>
    /// Gets whether this item has children (is a submenu).
    /// </summary>
    public bool HasChildren { get; init; }

    /// <summary>
    /// Gets the order priority for sorting.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Gets whether to show a separator before this item.
    /// </summary>
    public bool SeparatorBefore { get; init; }
}
