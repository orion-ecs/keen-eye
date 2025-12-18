namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a menu bar (horizontal strip at top with menu items like File, Edit, View).
/// </summary>
/// <remarks>
/// <para>
/// A menu bar contains multiple top-level menu items. Clicking a menu item opens
/// its associated dropdown menu. While a menu is open, hovering over other menu
/// items switches to their dropdowns.
/// </para>
/// </remarks>
public struct UIMenuBar : IComponent
{
    /// <summary>
    /// The currently open dropdown menu, or Entity.Null if none.
    /// </summary>
    public Entity ActiveMenu;

    /// <summary>
    /// Whether menu switching on hover is currently active (becomes true when any menu is open).
    /// </summary>
    public bool IsActive;
}

/// <summary>
/// Component for a dropdown menu or context menu.
/// </summary>
/// <remarks>
/// <para>
/// Menus can be standalone (context menus) or attached to a menu bar item.
/// They contain menu items that can be clicked, toggle state, or open submenus.
/// </para>
/// </remarks>
public struct UIMenu : IComponent
{
    /// <summary>
    /// Whether this menu is currently visible.
    /// </summary>
    public bool IsOpen;

    /// <summary>
    /// For submenus, the parent menu. Entity.Null for top-level menus.
    /// </summary>
    public Entity ParentMenu;

    /// <summary>
    /// Currently open child submenu, if any.
    /// </summary>
    public Entity OpenSubmenu;

    /// <summary>
    /// Index of the currently highlighted item for keyboard navigation (-1 for none).
    /// </summary>
    public int SelectedIndex;

    /// <summary>
    /// Total number of items in this menu.
    /// </summary>
    public int ItemCount;

    /// <summary>
    /// For menu bar menus, the menu bar item that opens this menu.
    /// </summary>
    public Entity MenuBarItem;
}

/// <summary>
/// Component for a clickable item within a menu.
/// </summary>
/// <remarks>
/// <para>
/// Menu items can be:
/// <list type="bullet">
/// <item>Regular items that fire an event when clicked</item>
/// <item>Toggle items that maintain checked state</item>
/// <item>Submenu parents that open a nested menu</item>
/// <item>Separators (non-interactive dividers)</item>
/// </list>
/// </para>
/// </remarks>
public struct UIMenuItem : IComponent
{
    /// <summary>
    /// The menu that contains this item.
    /// </summary>
    public Entity Menu;

    /// <summary>
    /// If this item opens a submenu, reference to that submenu.
    /// </summary>
    public Entity Submenu;

    /// <summary>
    /// Whether this item can be interacted with.
    /// </summary>
    public bool IsEnabled;

    /// <summary>
    /// For toggle items, whether currently checked.
    /// </summary>
    public bool IsChecked;

    /// <summary>
    /// If true, this item renders as a separator line.
    /// </summary>
    public bool IsSeparator;

    /// <summary>
    /// The item's position within the menu (0-based).
    /// </summary>
    public int Index;

    /// <summary>
    /// The item's display label.
    /// </summary>
    public string Label;

    /// <summary>
    /// Unique identifier for this menu item (for event handling).
    /// </summary>
    public string ItemId;
}

/// <summary>
/// Component for displaying a keyboard shortcut on a menu item.
/// </summary>
/// <param name="shortcut">The shortcut text to display (e.g., "Ctrl+S").</param>
public struct UIMenuShortcut(string shortcut) : IComponent
{
    /// <summary>
    /// The shortcut display text.
    /// </summary>
    public string Shortcut = shortcut;
}

/// <summary>
/// Component for a menu bar top-level item (the clickable text like "File", "Edit").
/// </summary>
public struct UIMenuBarItem : IComponent
{
    /// <summary>
    /// The menu bar this item belongs to.
    /// </summary>
    public Entity MenuBar;

    /// <summary>
    /// The dropdown menu that opens when this item is clicked.
    /// </summary>
    public Entity DropdownMenu;

    /// <summary>
    /// The item's display label.
    /// </summary>
    public string Label;

    /// <summary>
    /// Position index in the menu bar.
    /// </summary>
    public int Index;
}

/// <summary>
/// Tag component for menu items that act as toggles.
/// </summary>
public struct UIMenuToggleTag : ITagComponent;

/// <summary>
/// Tag component for menu items that have submenus.
/// </summary>
public struct UIMenuSubmenuTag : ITagComponent;
