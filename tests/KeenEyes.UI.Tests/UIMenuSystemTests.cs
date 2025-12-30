using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIMenuSystem menu management.
/// </summary>
public class UIMenuSystemTests
{
    #region Menu Bar Tests

    [Fact]
    public void MenuBarItem_Click_OpensDropdown()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIMenu>(dropdown);
        ref readonly var element = ref world.Get<UIElement>(dropdown);

        Assert.True(menu.IsOpen);
        Assert.True(element.Visible);
    }

    [Fact]
    public void MenuBarItem_ClickSameItem_ClosesDropdown()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar { IsActive = true })
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        ref var menuBarState = ref world.Get<UIMenuBar>(menuBar);
        menuBarState.ActiveMenu = dropdown;

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIMenu>(dropdown);
        ref readonly var updatedMenuBar = ref world.Get<UIMenuBar>(menuBar);

        Assert.False(menu.IsOpen);
        Assert.False(updatedMenuBar.IsActive);
    }

    [Fact]
    public void MenuBarItem_HoverWhileActive_SwitchesMenu()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar { IsActive = true })
            .Build();

        var dropdown1 = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        var dropdown2 = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        ref var menuBarState = ref world.Get<UIMenuBar>(menuBar);
        menuBarState.ActiveMenu = dropdown1;

        var menuBarItem2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown2 })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuBarItem2, new Vector2(100, 20));
        world.Send(pointerEnterEvent);

        menuSystem.Update(0);

        ref readonly var menu1 = ref world.Get<UIMenu>(dropdown1);
        ref readonly var menu2 = ref world.Get<UIMenu>(dropdown2);

        Assert.False(menu1.IsOpen);
        Assert.True(menu2.IsOpen);
    }

    #endregion

    #region Menu Item Tests

    [Fact]
    public void MenuItem_Click_FiresClickEvent()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = menu, IsEnabled = true, ItemId = "action1", Index = 0 })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIMenuItemClickEvent>(e =>
        {
            if (e.MenuItem == menuItem)
            {
                eventFired = true;
            }
        });

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.True(eventFired);
    }

    [Fact]
    public void MenuItem_ClickDisabled_DoesNotFireEvent()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = menu, IsEnabled = false })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIMenuItemClickEvent>(e => eventFired = true);

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.False(eventFired);
    }

    [Fact]
    public void MenuItem_ClickToggle_TogglesState()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = menu, IsEnabled = true, IsChecked = false })
            .With(new UIMenuToggleTag())
            .Build();

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        ref readonly var updatedMenuItem = ref world.Get<UIMenuItem>(menuItem);

        Assert.True(updatedMenuItem.IsChecked);
    }

    [Fact]
    public void MenuItem_ClickSeparator_DoesNothing()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = menu, IsEnabled = true, IsSeparator = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIMenuItemClickEvent>(e => eventFired = true);

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.False(eventFired);
    }

    #endregion

    #region Submenu Tests

    [Fact]
    public void MenuItem_HoverWithSubmenu_OpensSubmenu()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var submenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = submenu })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        menuSystem.Update(0);

        ref readonly var submenuState = ref world.Get<UIMenu>(submenu);

        Assert.True(submenuState.IsOpen);
    }

    [Fact]
    public void MenuItem_ClickWithSubmenu_OpensSubmenu()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var submenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = submenu })
            .Build();

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        ref readonly var submenuState = ref world.Get<UIMenu>(submenu);

        Assert.True(submenuState.IsOpen);
    }

    #endregion

    #region Context Menu Tests

    [Fact]
    public void ContextMenuRequest_OpensMenuAtPosition()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var contextMenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(0, 0, 150, 200))
            .With(new UIMenu())
            .Build();

        var requestEvent = new UIContextMenuRequestEvent(contextMenu, new Vector2(100, 150), Entity.Null);
        world.Send(requestEvent);

        menuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIMenu>(contextMenu);
        ref readonly var rect = ref world.Get<UIRect>(contextMenu);

        Assert.True(menu.IsOpen);
        Assert.True(rect.Offset.Left.ApproximatelyEquals(100f));
        Assert.True(rect.Offset.Top.ApproximatelyEquals(150f));
    }

    #endregion

    #region Menu Close Tests

    [Fact]
    public void ClickOutside_ClosesAllMenus()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        var outsideElement = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var clickEvent = new UIClickEvent(outsideElement, new Vector2(500, 500), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        ref readonly var menuState = ref world.Get<UIMenu>(menu);

        Assert.False(menuState.IsOpen);
    }

    [Fact]
    public void MenuItem_Click_ClosesAllMenus()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = menu, IsEnabled = true })
            .Build();

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        ref readonly var menuState = ref world.Get<UIMenu>(menu);

        Assert.False(menuState.IsOpen);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void MenuOpen_FiresOpenedEvent()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIMenuOpenedEvent>(e =>
        {
            if (e.Menu == dropdown)
            {
                eventFired = true;
            }
        });

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.True(eventFired);
    }

    [Fact]
    public void MenuClose_FiresClosedEvent()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIMenuClosedEvent>(e =>
        {
            if (e.Menu == menu)
            {
                eventFired = true;
            }
        });

        var outsideElement = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var clickEvent = new UIClickEvent(outsideElement, new Vector2(500, 500), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.True(eventFired);
    }

    [Fact]
    public void ToggleMenuItem_FiresToggleChangedEvent()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = menu, IsEnabled = true, IsChecked = false })
            .With(new UIMenuToggleTag())
            .Build();

        bool eventFired = false;
        bool newState = false;
        world.Subscribe<UIMenuToggleChangedEvent>(e =>
        {
            if (e.MenuItem == menuItem)
            {
                eventFired = true;
                newState = e.IsChecked;
            }
        });

        var clickEvent = new UIClickEvent(menuItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.True(eventFired);
        Assert.True(newState);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void MenuBarItem_Click_NoMenuBarComponent_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        // Menu bar item with invalid menu bar reference
        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = Entity.Null, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void MenuBarItem_Click_DropdownWithoutUIMenu_DoesNotOpen()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        // Dropdown without UIMenu component
        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        // Should not throw and menu bar should not become active
        ref readonly var menuBarState = ref world.Get<UIMenuBar>(menuBar);
        Assert.False(menuBarState.IsActive);
    }

    [Fact]
    public void MenuBarItem_Hover_NoMenuBarComponent_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        // Menu bar item with invalid menu bar reference
        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = Entity.Null, DropdownMenu = dropdown })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuBarItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void MenuBarItem_Hover_InactiveMenuBar_DoesNotSwitch()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar { IsActive = false }) // Not active
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuBarItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        menuSystem.Update(0);

        // Menu should not open because menu bar is not active
        ref readonly var menu = ref world.Get<UIMenu>(dropdown);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void MenuItem_Hover_DisabledWithSubmenu_DoesNotOpenSubmenu()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var submenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        // Disabled menu item with submenu
        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = false, Submenu = submenu })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        menuSystem.Update(0);

        ref readonly var submenuState = ref world.Get<UIMenu>(submenu);
        Assert.False(submenuState.IsOpen);
    }

    [Fact]
    public void MenuItem_Hover_InvalidSubmenu_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = Entity.Null })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void ContextMenuRequest_MenuWithoutUIRect_DoesNotOpen()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        // Context menu without UIRect
        var contextMenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        var requestEvent = new UIContextMenuRequestEvent(contextMenu, new Vector2(100, 150), Entity.Null);
        world.Send(requestEvent);

        menuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIMenu>(contextMenu);
        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void ContextMenuRequest_MenuWithoutUIMenu_DoesNothing()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        // Context menu without UIMenu component
        var contextMenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(0, 0, 150, 200))
            .Build();

        var requestEvent = new UIContextMenuRequestEvent(contextMenu, new Vector2(100, 150), Entity.Null);
        world.Send(requestEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void OpenSubmenu_SubmenuWithoutUIMenu_DoesNotOpen()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        // Submenu without UIMenu component
        var submenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = submenu })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void OpenSubmenu_ClosesSiblingSubmenu()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var submenu1 = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        var submenu2 = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        // Set first submenu as open
        ref var parentMenuState = ref world.Get<UIMenu>(parentMenu);
        parentMenuState.OpenSubmenu = submenu1;

        var menuItem1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = submenu1 })
            .Build();

        var menuItem2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = submenu2 })
            .Build();

        // Hover over second menu item to open its submenu
        var pointerEnterEvent = new UIPointerEnterEvent(menuItem2, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        menuSystem.Update(0);

        ref readonly var submenu1State = ref world.Get<UIMenu>(submenu1);
        ref readonly var submenu2State = ref world.Get<UIMenu>(submenu2);

        Assert.False(submenu1State.IsOpen);
        Assert.True(submenu2State.IsOpen);
    }

    [Fact]
    public void OpenMenu_WithoutUIElement_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        // Dropdown without UIElement
        var dropdown = world.Spawn()
            .With(new UIMenu())
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void OpenMenu_WithExistingUIHiddenTag_RemovesTag()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .With(new UIHiddenTag())
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        menuSystem.Update(0);

        Assert.False(world.Has<UIHiddenTag>(dropdown));
    }

    [Fact]
    public void OpenMenu_WithExistingUILayoutDirtyTag_DoesNotAddDuplicate()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .With(new UILayoutDirtyTag())
            .Build();

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        // Should not throw with duplicate tag
        menuSystem.Update(0);
    }

    [Fact]
    public void CloseMenu_WithoutUIElement_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        // Menu without UIElement
        var menu = world.Spawn()
            .With(new UIMenu { IsOpen = true })
            .Build();

        var outsideElement = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var clickEvent = new UIClickEvent(outsideElement, new Vector2(500, 500), MouseButton.Left);
        world.Send(clickEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void CloseMenu_AlreadyHasUIHiddenTag_DoesNotAddDuplicate()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .With(new UIHiddenTag())
            .Build();

        var outsideElement = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var clickEvent = new UIClickEvent(outsideElement, new Vector2(500, 500), MouseButton.Left);
        world.Send(clickEvent);

        // Should not throw with existing tag
        menuSystem.Update(0);
    }

    [Fact]
    public void PositionDropdown_NoUIRect_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar())
            .Build();

        // Dropdown without UIRect
        var dropdown = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        // Menu bar item without UIRect
        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        var clickEvent = new UIClickEvent(menuBarItem, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void PositionSubmenu_NoUIRect_DoesNotThrow()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var parentMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        // Submenu without UIRect
        var submenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIMenu())
            .Build();

        // Menu item without UIRect
        var menuItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuItem { Menu = parentMenu, IsEnabled = true, Submenu = submenu })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(menuItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        // Should not throw
        menuSystem.Update(0);
    }

    [Fact]
    public void UIMenuSystem_Dispose_CleansUpSubscriptions()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        // Should not throw
        menuSystem.Dispose();

        // System should be disposed and not process events
        var menu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenu())
            .Build();

        var clickEvent = new UIClickEvent(menu, new Vector2(50, 20), MouseButton.Left);

        // Should not throw even after dispose
        world.Send(clickEvent);
    }

    [Fact]
    public void SwitchToMenu_SameMenu_DoesNotReopen()
    {
        using var world = new World();
        var menuSystem = new UIMenuSystem();
        world.AddSystem(menuSystem);

        var menuBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBar { IsActive = true })
            .Build();

        var dropdown = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIMenu { IsOpen = true })
            .Build();

        ref var menuBarState = ref world.Get<UIMenuBar>(menuBar);
        menuBarState.ActiveMenu = dropdown;

        var menuBarItem = world.Spawn()
            .With(UIElement.Default)
            .With(new UIMenuBarItem { MenuBar = menuBar, DropdownMenu = dropdown })
            .Build();

        // Hover over same item
        var pointerEnterEvent = new UIPointerEnterEvent(menuBarItem, new Vector2(50, 20));
        world.Send(pointerEnterEvent);

        menuSystem.Update(0);

        // Menu should still be open
        ref readonly var menu = ref world.Get<UIMenu>(dropdown);
        Assert.True(menu.IsOpen);
    }

    #endregion
}
