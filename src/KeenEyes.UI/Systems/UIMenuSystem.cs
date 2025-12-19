using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that manages menu bars, dropdown menus, context menus, and submenus.
/// </summary>
/// <remarks>
/// <para>
/// This system handles:
/// <list type="bullet">
/// <item>Menu bar item clicks to open dropdown menus</item>
/// <item>Hover switching between open menus in a menu bar</item>
/// <item>Menu item clicks and toggle state changes</item>
/// <item>Submenu opening on hover</item>
/// <item>Keyboard navigation within menus</item>
/// <item>Closing menus when clicking outside</item>
/// <item>Context menu requests</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIMenuSystem : SystemBase
{
    private EventSubscription? clickSubscription;
    private EventSubscription? pointerEnterSubscription;
    private EventSubscription? contextMenuSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
        pointerEnterSubscription = World.Subscribe<UIPointerEnterEvent>(OnPointerEnter);
        contextMenuSubscription = World.Subscribe<UIContextMenuRequestEvent>(OnContextMenuRequest);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        clickSubscription?.Dispose();
        pointerEnterSubscription?.Dispose();
        contextMenuSubscription?.Dispose();
        clickSubscription = null;
        pointerEnterSubscription = null;
        contextMenuSubscription = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Menu behavior is primarily event-driven
        // Could add keyboard navigation polling here if needed
    }

    private void OnClick(UIClickEvent e)
    {
        // Check if clicking a menu bar item
        if (World.Has<UIMenuBarItem>(e.Element))
        {
            HandleMenuBarItemClick(e.Element);
            return;
        }

        // Check if clicking a menu item
        if (World.Has<UIMenuItem>(e.Element))
        {
            HandleMenuItemClick(e.Element);
            return;
        }

        // Click outside menus - close all open menus
        CloseAllMenus();
    }

    private void OnPointerEnter(UIPointerEnterEvent e)
    {
        // Check if hovering over a menu bar item while menu bar is active
        if (World.Has<UIMenuBarItem>(e.Element))
        {
            ref readonly var item = ref World.Get<UIMenuBarItem>(e.Element);
            if (World.Has<UIMenuBar>(item.MenuBar))
            {
                ref var menuBar = ref World.Get<UIMenuBar>(item.MenuBar);
                if (menuBar.IsActive && menuBar.ActiveMenu.IsValid)
                {
                    // Switch to this menu
                    SwitchToMenu(ref menuBar, item);
                }
            }

            return;
        }

        // Check if hovering over a menu item with submenu
        if (World.Has<UIMenuItem>(e.Element))
        {
            ref readonly var menuItem = ref World.Get<UIMenuItem>(e.Element);
            if (menuItem.Submenu.IsValid && World.IsAlive(menuItem.Submenu) && menuItem.IsEnabled)
            {
                OpenSubmenu(e.Element, menuItem);
            }
        }
    }

    private void OnContextMenuRequest(UIContextMenuRequestEvent e)
    {
        // Close any other open menus first
        CloseAllMenus();

        // Position and show the context menu
        if (World.Has<UIMenu>(e.Menu) && World.Has<UIRect>(e.Menu))
        {
            ref var menu = ref World.Get<UIMenu>(e.Menu);
            ref var rect = ref World.Get<UIRect>(e.Menu);

            // Position at requested location
            rect.Offset = new UIEdges(e.Position.X, e.Position.Y, 0, 0);

            OpenMenu(e.Menu, ref menu, null);
        }
    }

    private void HandleMenuBarItemClick(Entity element)
    {
        ref readonly var item = ref World.Get<UIMenuBarItem>(element);

        if (!World.Has<UIMenuBar>(item.MenuBar))
        {
            return;
        }

        ref var menuBar = ref World.Get<UIMenuBar>(item.MenuBar);

        if (menuBar.ActiveMenu.IsValid && menuBar.ActiveMenu == item.DropdownMenu)
        {
            // Clicking the same menu item - close it
            CloseMenuAndChildren(item.DropdownMenu);
            menuBar.ActiveMenu = Entity.Null;
            menuBar.IsActive = false;
        }
        else
        {
            // Close current menu if any
            if (menuBar.ActiveMenu.IsValid)
            {
                CloseMenuAndChildren(menuBar.ActiveMenu);
            }

            // Open the new menu
            if (item.DropdownMenu.IsValid && World.Has<UIMenu>(item.DropdownMenu))
            {
                ref var menu = ref World.Get<UIMenu>(item.DropdownMenu);
                PositionDropdownBelowItem(item.DropdownMenu, element);
                OpenMenu(item.DropdownMenu, ref menu, null);
                menuBar.ActiveMenu = item.DropdownMenu;
                menuBar.IsActive = true;
            }
        }
    }

    private void HandleMenuItemClick(Entity element)
    {
        ref var menuItem = ref World.Get<UIMenuItem>(element);

        if (!menuItem.IsEnabled || menuItem.IsSeparator)
        {
            return;
        }

        // Handle submenu items (check IsAlive since default Entity(0,0) passes IsValid)
        if (menuItem.Submenu.IsValid && World.IsAlive(menuItem.Submenu))
        {
            OpenSubmenu(element, menuItem);
            return;
        }

        // Handle toggle items
        if (World.Has<UIMenuToggleTag>(element))
        {
            menuItem.IsChecked = !menuItem.IsChecked;
            World.Send(new UIMenuToggleChangedEvent(element, menuItem.IsChecked));
            // Don't close the menu for toggle items
            return;
        }

        // Regular item click - fire event and close menus
        World.Send(new UIMenuItemClickEvent(element, menuItem.Menu, menuItem.ItemId, menuItem.Index));
        CloseAllMenus();
    }

    private void SwitchToMenu(ref UIMenuBar menuBar, in UIMenuBarItem newItem)
    {
        if (menuBar.ActiveMenu == newItem.DropdownMenu)
        {
            return;
        }

        // Close current menu
        if (menuBar.ActiveMenu.IsValid)
        {
            CloseMenuAndChildren(menuBar.ActiveMenu);
        }

        // Open new menu
        if (newItem.DropdownMenu.IsValid && World.Has<UIMenu>(newItem.DropdownMenu))
        {
            ref var menu = ref World.Get<UIMenu>(newItem.DropdownMenu);

            // Find the menu bar item entity to position relative to
            Entity menuBarItemEntity = Entity.Null;
            foreach (var entity in World.Query<UIMenuBarItem>())
            {
                ref readonly var item = ref World.Get<UIMenuBarItem>(entity);
                if (item.DropdownMenu == newItem.DropdownMenu)
                {
                    menuBarItemEntity = entity;
                    break;
                }
            }

            if (menuBarItemEntity.IsValid)
            {
                PositionDropdownBelowItem(newItem.DropdownMenu, menuBarItemEntity);
            }

            OpenMenu(newItem.DropdownMenu, ref menu, null);
            menuBar.ActiveMenu = newItem.DropdownMenu;
        }
    }

    private void OpenSubmenu(Entity menuItemEntity, in UIMenuItem menuItem)
    {
        if (!menuItem.Submenu.IsValid || !World.Has<UIMenu>(menuItem.Submenu))
        {
            return;
        }

        // Close any sibling submenu first
        if (World.Has<UIMenu>(menuItem.Menu))
        {
            ref var parentMenu = ref World.Get<UIMenu>(menuItem.Menu);
            if (parentMenu.OpenSubmenu.IsValid && parentMenu.OpenSubmenu != menuItem.Submenu)
            {
                CloseMenuAndChildren(parentMenu.OpenSubmenu);
            }

            parentMenu.OpenSubmenu = menuItem.Submenu;
        }

        ref var submenu = ref World.Get<UIMenu>(menuItem.Submenu);

        // Position submenu to the right of the parent item
        PositionSubmenuBesideItem(menuItem.Submenu, menuItemEntity);

        OpenMenu(menuItem.Submenu, ref submenu, menuItem.Menu);
    }

    private void OpenMenu(Entity menuEntity, ref UIMenu menu, Entity? parentMenu)
    {
        menu.IsOpen = true;
        menu.ParentMenu = parentMenu ?? Entity.Null;
        menu.SelectedIndex = -1;

        // Make visible
        if (World.Has<UIElement>(menuEntity))
        {
            ref var element = ref World.Get<UIElement>(menuEntity);
            element.Visible = true;
        }

        if (World.Has<UIHiddenTag>(menuEntity))
        {
            World.Remove<UIHiddenTag>(menuEntity);
        }

        if (!World.Has<UILayoutDirtyTag>(menuEntity))
        {
            World.Add(menuEntity, new UILayoutDirtyTag());
        }

        World.Send(new UIMenuOpenedEvent(menuEntity, parentMenu));
    }

    private void CloseMenu(Entity menuEntity)
    {
        if (!World.Has<UIMenu>(menuEntity))
        {
            return;
        }

        ref var menu = ref World.Get<UIMenu>(menuEntity);
        menu.IsOpen = false;
        menu.SelectedIndex = -1;

        // Hide
        if (World.Has<UIElement>(menuEntity))
        {
            ref var element = ref World.Get<UIElement>(menuEntity);
            element.Visible = false;
        }

        if (!World.Has<UIHiddenTag>(menuEntity))
        {
            World.Add(menuEntity, new UIHiddenTag());
        }

        World.Send(new UIMenuClosedEvent(menuEntity));
    }

    private void CloseMenuAndChildren(Entity menuEntity)
    {
        if (!World.Has<UIMenu>(menuEntity))
        {
            return;
        }

        ref var menu = ref World.Get<UIMenu>(menuEntity);

        // Recursively close submenus first
        if (menu.OpenSubmenu.IsValid)
        {
            CloseMenuAndChildren(menu.OpenSubmenu);
            menu.OpenSubmenu = Entity.Null;
        }

        CloseMenu(menuEntity);
    }

    private void CloseAllMenus()
    {
        // Close all open menus
        foreach (var entity in World.Query<UIMenu>())
        {
            ref var menu = ref World.Get<UIMenu>(entity);
            if (menu.IsOpen)
            {
                CloseMenu(entity);
                menu.OpenSubmenu = Entity.Null;
            }
        }

        // Reset all menu bars
        foreach (var entity in World.Query<UIMenuBar>())
        {
            ref var menuBar = ref World.Get<UIMenuBar>(entity);
            menuBar.ActiveMenu = Entity.Null;
            menuBar.IsActive = false;
        }
    }

    private void PositionDropdownBelowItem(Entity menuEntity, Entity itemEntity)
    {
        if (!World.Has<UIRect>(menuEntity) || !World.Has<UIRect>(itemEntity))
        {
            return;
        }

        ref var menuRect = ref World.Get<UIRect>(menuEntity);

        // The menu is anchored to (0, 1) (bottom-left of parent) with Pivot (0, 0),
        // which already positions it directly below the parent menu bar item.
        // No additional offset is needed.
        menuRect.Offset = UIEdges.Zero;
    }

    private void PositionSubmenuBesideItem(Entity submenuEntity, Entity itemEntity)
    {
        if (!World.Has<UIRect>(submenuEntity) || !World.Has<UIRect>(itemEntity))
        {
            return;
        }

        ref var submenuRect = ref World.Get<UIRect>(submenuEntity);

        // The submenu is anchored to (1, 0) (top-right of parent) with Pivot (0, 0),
        // which already positions it to the right of the parent menu item.
        // Use a small negative left offset to create a slight overlap.
        submenuRect.Offset = new UIEdges(-2, 0, 0, 0);
    }
}
