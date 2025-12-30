using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory menu and modal widget creation methods.
/// </summary>
public class WidgetFactoryMenuModalTests
{
    private static readonly FontHandle testFont = new(1);

    #region MenuBar Tests

    [Fact]
    public void CreateMenuBar_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var menus = new (string, IEnumerable<MenuItemDef>)[]
        {
            ("File", new[] { new MenuItemDef("New"), new MenuItemDef("Open") })
        };

        var menuBar = WidgetFactory.CreateMenuBar(world, parent, testFont, menus);

        Assert.True(world.Has<UIElement>(menuBar));
        Assert.True(world.Has<UIRect>(menuBar));
        Assert.True(world.Has<UIStyle>(menuBar));
        Assert.True(world.Has<UILayout>(menuBar));
        Assert.True(world.Has<UIMenuBar>(menuBar));
    }

    [Fact]
    public void CreateMenuBar_HasHorizontalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var menus = new (string, IEnumerable<MenuItemDef>)[]
        {
            ("File", new[] { new MenuItemDef("New") })
        };

        var menuBar = WidgetFactory.CreateMenuBar(world, parent, testFont, menus);

        ref readonly var layout = ref world.Get<UILayout>(menuBar);
        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
    }

    [Fact]
    public void CreateMenuBar_CreatesMenuBarItems()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var menus = new (string, IEnumerable<MenuItemDef>)[]
        {
            ("File", new[] { new MenuItemDef("New") }),
            ("Edit", new[] { new MenuItemDef("Cut") })
        };

        var menuBar = WidgetFactory.CreateMenuBar(world, parent, testFont, menus);

        var children = world.GetChildren(menuBar).ToList();
        Assert.Equal(2, children.Count);
        Assert.All(children, child => Assert.True(world.Has<UIMenuBarItem>(child)));
    }

    [Fact]
    public void CreateMenuBar_MenuBarItemsHaveDropdowns()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var menus = new (string, IEnumerable<MenuItemDef>)[]
        {
            ("File", new[] { new MenuItemDef("New") })
        };

        var menuBar = WidgetFactory.CreateMenuBar(world, parent, testFont, menus);

        var menuBarItem = world.GetChildren(menuBar).First();
        ref readonly var menuBarItemData = ref world.Get<UIMenuBarItem>(menuBarItem);
        Assert.NotEqual(Entity.Null, menuBarItemData.DropdownMenu);
        Assert.True(world.Has<UIMenu>(menuBarItemData.DropdownMenu));
    }

    [Fact]
    public void CreateMenuBar_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var menus = new (string, IEnumerable<MenuItemDef>)[]
        {
            ("File", new[] { new MenuItemDef("New") })
        };
        var config = new MenuBarConfig { Height = 40, ItemPadding = 20 };

        var menuBar = WidgetFactory.CreateMenuBar(world, parent, testFont, menus, config);

        ref readonly var rect = ref world.Get<UIRect>(menuBar);
        Assert.Equal(40, rect.Size.Y);
    }

    #endregion

    #region Menu Tests

    [Fact]
    public void CreateMenu_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { new MenuItemDef("Item 1"), new MenuItemDef("Item 2") };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        Assert.True(world.Has<UIElement>(menu));
        Assert.True(world.Has<UIRect>(menu));
        Assert.True(world.Has<UIStyle>(menu));
        Assert.True(world.Has<UILayout>(menu));
        Assert.True(world.Has<UIMenu>(menu));
    }

    [Fact]
    public void CreateMenu_InitiallyHidden()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { new MenuItemDef("Item 1") };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        ref readonly var element = ref world.Get<UIElement>(menu);
        Assert.False(element.Visible);
    }

    [Fact]
    public void CreateMenu_CreatesMenuItems()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { new MenuItemDef("Item 1"), new MenuItemDef("Item 2"), new MenuItemDef("Item 3") };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        ref readonly var menuData = ref world.Get<UIMenu>(menu);
        Assert.Equal(3, menuData.ItemCount);
    }

    [Fact]
    public void CreateMenu_WithSeparator_CreatesSeparatorItem()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[]
        {
            new MenuItemDef("Item 1"),
            MenuItemDef.Separator(),
            new MenuItemDef("Item 2")
        };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        var children = world.GetChildren(menu).ToList();
        var separator = children[1];
        ref readonly var menuItem = ref world.Get<UIMenuItem>(separator);
        Assert.True(menuItem.IsSeparator);
    }

    [Fact]
    public void CreateMenu_WithSubmenu_CreatesSubmenuItem()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[]
        {
            new MenuItemDef("Item 1"),
            new MenuItemDef("Submenu", SubmenuItems: new[] { new MenuItemDef("Sub Item") })
        };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        var children = world.GetChildren(menu).ToList();
        var submenuItem = children[1];
        Assert.True(world.Has<UIMenuSubmenuTag>(submenuItem));
        ref readonly var menuItem = ref world.Get<UIMenuItem>(submenuItem);
        Assert.NotEqual(Entity.Null, menuItem.Submenu);
    }

    [Fact]
    public void CreateMenu_WithToggle_CreatesToggleItem()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { new MenuItemDef("Toggle Item", IsToggle: true, IsChecked: true) };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        var children = world.GetChildren(menu).ToList();
        var toggleItem = children[0];
        Assert.True(world.Has<UIMenuToggleTag>(toggleItem));
        ref readonly var menuItem = ref world.Get<UIMenuItem>(toggleItem);
        Assert.True(menuItem.IsChecked);
    }

    [Fact]
    public void CreateMenu_WithShortcut_CreatesShortcutLabel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { new MenuItemDef("Save", Shortcut: "Ctrl+S") };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        var menuItem = world.GetChildren(menu).First();
        var menuItemChildren = world.GetChildren(menuItem).ToList();
        var shortcutEntity = menuItemChildren.FirstOrDefault(c => world.Has<UIMenuShortcut>(c));
        Assert.NotEqual(Entity.Null, shortcutEntity);
    }

    [Fact]
    public void CreateMenu_DisabledItem_IsNotClickable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { new MenuItemDef("Disabled", IsEnabled: false) };

        var menu = WidgetFactory.CreateMenu(world, parent, testFont, items);

        var menuItem = world.GetChildren(menu).First();
        ref readonly var interactable = ref world.Get<UIInteractable>(menuItem);
        Assert.False(interactable.CanClick);
    }

    #endregion

    #region ContextMenu Tests

    [Fact]
    public void CreateContextMenu_HasRequiredComponents()
    {
        using var world = new World();
        var items = new[] { new MenuItemDef("Copy"), new MenuItemDef("Paste") };

        var contextMenu = WidgetFactory.CreateContextMenu(world, testFont, items);

        Assert.True(world.Has<UIElement>(contextMenu));
        Assert.True(world.Has<UIRect>(contextMenu));
        Assert.True(world.Has<UIStyle>(contextMenu));
        Assert.True(world.Has<UILayout>(contextMenu));
        Assert.True(world.Has<UIMenu>(contextMenu));
    }

    [Fact]
    public void CreateContextMenu_InitiallyHidden()
    {
        using var world = new World();
        var items = new[] { new MenuItemDef("Copy") };

        var contextMenu = WidgetFactory.CreateContextMenu(world, testFont, items);

        ref readonly var element = ref world.Get<UIElement>(contextMenu);
        Assert.False(element.Visible);
    }

    [Fact]
    public void CreateContextMenu_HasNoParent()
    {
        using var world = new World();
        var items = new[] { new MenuItemDef("Copy") };

        var contextMenu = WidgetFactory.CreateContextMenu(world, testFont, items);

        Assert.Equal(Entity.Null, world.GetParent(contextMenu));
    }

    #endregion

    #region RadialMenu Tests

    [Fact]
    public void CreateRadialMenu_HasRequiredComponents()
    {
        using var world = new World();
        var slices = new[]
        {
            new RadialSliceDef("Attack"),
            new RadialSliceDef("Defend"),
            new RadialSliceDef("Item"),
            new RadialSliceDef("Run")
        };

        var radialMenu = WidgetFactory.CreateRadialMenu(world, testFont, slices);

        Assert.True(world.Has<UIElement>(radialMenu));
        Assert.True(world.Has<UIRect>(radialMenu));
        Assert.True(world.Has<UIStyle>(radialMenu));
        Assert.True(world.Has<UIRadialMenu>(radialMenu));
        Assert.True(world.Has<UIRadialMenuInputState>(radialMenu));
    }

    [Fact]
    public void CreateRadialMenu_InitiallyHidden()
    {
        using var world = new World();
        var slices = new[] { new RadialSliceDef("Option 1") };

        var radialMenu = WidgetFactory.CreateRadialMenu(world, testFont, slices);

        ref readonly var element = ref world.Get<UIElement>(radialMenu);
        Assert.False(element.Visible);
    }

    [Fact]
    public void CreateRadialMenu_CreatesSlices()
    {
        using var world = new World();
        var slices = new[]
        {
            new RadialSliceDef("Option 1"),
            new RadialSliceDef("Option 2"),
            new RadialSliceDef("Option 3")
        };

        var radialMenu = WidgetFactory.CreateRadialMenu(world, testFont, slices);

        ref readonly var radialMenuData = ref world.Get<UIRadialMenu>(radialMenu);
        Assert.Equal(3, radialMenuData.SliceCount);

        var children = world.GetChildren(radialMenu).ToList();
        Assert.Equal(3, children.Count);
        Assert.All(children, child => Assert.True(world.Has<UIRadialSlice>(child)));
    }

    [Fact]
    public void CreateRadialMenu_AppliesConfig()
    {
        using var world = new World();
        var slices = new[] { new RadialSliceDef("Option 1") };
        var config = new RadialMenuConfig { InnerRadius = 50, OuterRadius = 150 };

        var radialMenu = WidgetFactory.CreateRadialMenu(world, testFont, slices, config);

        ref readonly var radialMenuData = ref world.Get<UIRadialMenu>(radialMenu);
        Assert.Equal(50, radialMenuData.InnerRadius);
        Assert.Equal(150, radialMenuData.OuterRadius);
    }

    [Fact]
    public void OpenRadialMenu_MakesMenuVisible()
    {
        using var world = new World();
        var slices = new[] { new RadialSliceDef("Option 1") };
        var radialMenu = WidgetFactory.CreateRadialMenu(world, testFont, slices);

        WidgetFactory.OpenRadialMenu(world, radialMenu, new Vector2(400, 300));

        ref readonly var element = ref world.Get<UIElement>(radialMenu);
        Assert.True(element.Visible);
        ref readonly var radialMenuData = ref world.Get<UIRadialMenu>(radialMenu);
        Assert.True(radialMenuData.IsOpen);
    }

    [Fact]
    public void CloseRadialMenu_HidesMenu()
    {
        using var world = new World();
        var slices = new[] { new RadialSliceDef("Option 1") };
        var radialMenu = WidgetFactory.CreateRadialMenu(world, testFont, slices);
        WidgetFactory.OpenRadialMenu(world, radialMenu, new Vector2(400, 300));

        WidgetFactory.CloseRadialMenu(world, radialMenu);

        ref readonly var element = ref world.Get<UIElement>(radialMenu);
        Assert.False(element.Visible);
        ref readonly var radialMenuData = ref world.Get<UIRadialMenu>(radialMenu);
        Assert.False(radialMenuData.IsOpen);
    }

    [Fact]
    public void OpenRadialMenu_WithInvalidEntity_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw when trying to open non-radial-menu entity
        WidgetFactory.OpenRadialMenu(world, Entity.Null, Vector2.Zero);
    }

    [Fact]
    public void CloseRadialMenu_WithInvalidEntity_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw when trying to close non-radial-menu entity
        WidgetFactory.CloseRadialMenu(world, Entity.Null);
    }

    #endregion

    #region Modal Tests

    [Fact]
    public void CreateModal_ReturnsModalBackdropAndContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, contentPanel) = WidgetFactory.CreateModal(world, parent, testFont);

        Assert.True(world.IsAlive(modal));
        Assert.True(world.IsAlive(backdrop));
        Assert.True(world.IsAlive(contentPanel));
    }

    [Fact]
    public void CreateModal_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, _, _) = WidgetFactory.CreateModal(world, parent, testFont);

        Assert.True(world.Has<UIElement>(modal));
        Assert.True(world.Has<UIRect>(modal));
        Assert.True(world.Has<UIStyle>(modal));
        Assert.True(world.Has<UILayout>(modal));
        Assert.True(world.Has<UIModal>(modal));
    }

    [Fact]
    public void CreateModal_InitiallyHidden()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, _) = WidgetFactory.CreateModal(world, parent, testFont);

        ref readonly var modalElement = ref world.Get<UIElement>(modal);
        Assert.False(modalElement.Visible);
        ref readonly var backdropElement = ref world.Get<UIElement>(backdrop);
        Assert.False(backdropElement.Visible);
    }

    [Fact]
    public void CreateModal_BackdropHasModalReference()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, _) = WidgetFactory.CreateModal(world, parent, testFont);

        Assert.True(world.Has<UIModalBackdrop>(backdrop));
        ref readonly var backdropData = ref world.Get<UIModalBackdrop>(backdrop);
        Assert.Equal(modal, backdropData.Modal);
    }

    [Fact]
    public void CreateModal_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ModalConfig(Width: 500, Height: 400, Title: "Test Modal", CornerRadius: 10);

        var (modal, _, _) = WidgetFactory.CreateModal(world, parent, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(modal);
        Assert.Equal(500, rect.Size.X);
        Assert.Equal(400, rect.Size.Y);
        ref readonly var modalData = ref world.Get<UIModal>(modal);
        Assert.Equal("Test Modal", modalData.Title);
    }

    [Fact]
    public void CreateModal_WithButtons_CreatesActionButtons()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var buttons = new[]
        {
            new ModalButtonDef("Cancel", ModalResult.Cancel),
            new ModalButtonDef("OK", ModalResult.OK, IsPrimary: true)
        };

        var (modal, _, _) = WidgetFactory.CreateModal(world, parent, testFont, buttons: buttons);

        // Check that modal children include a footer with buttons
        var modalChildren = world.GetChildren(modal).ToList();
        var footer = modalChildren.LastOrDefault();
        if (footer != Entity.Null)
        {
            var buttonChildren = world.GetChildren(footer).ToList();
            var modalButtons = buttonChildren.Where(c => world.Has<UIModalButton>(c)).ToList();
            Assert.Equal(2, modalButtons.Count);
        }
    }

    [Fact]
    public void CreateModal_WithCloseButton_HasCloseButton()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ModalConfig(ShowCloseButton: true);

        var (modal, _, _) = WidgetFactory.CreateModal(world, parent, testFont, config);

        // Find close button in hierarchy by traversing modal children
        var hasCloseButton = FindEntityWithComponent<UIModalCloseButton>(world, modal);
        Assert.True(hasCloseButton);
    }

    [Fact]
    public void CreateModal_WithName_SetsEntityNames()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, contentPanel) = WidgetFactory.CreateModal(world, parent, "TestModal", testFont);

        Assert.Equal("TestModal", world.GetName(modal));
        Assert.Equal("TestModal_Backdrop", world.GetName(backdrop));
        Assert.Equal("TestModal_Content", world.GetName(contentPanel));
    }

    #endregion

    #region Alert Tests

    [Fact]
    public void CreateAlert_ReturnsModalBackdropAndContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, contentPanel) = WidgetFactory.CreateAlert(world, parent, "Test message", testFont);

        Assert.True(world.IsAlive(modal));
        Assert.True(world.IsAlive(backdrop));
        Assert.True(world.IsAlive(contentPanel));
    }

    [Fact]
    public void CreateAlert_HasMessageInContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, _, contentPanel) = WidgetFactory.CreateAlert(world, parent, "Alert message!", testFont);

        var contentChildren = world.GetChildren(contentPanel).ToList();
        var messageLabel = contentChildren.FirstOrDefault(c => world.Has<UIText>(c));
        Assert.NotEqual(Entity.Null, messageLabel);
        ref readonly var text = ref world.Get<UIText>(messageLabel);
        Assert.Equal("Alert message!", text.Content);
    }

    [Fact]
    public void CreateAlert_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AlertConfig { Title = "Warning!", OkButtonText = "Got it" };

        var (modal, _, _) = WidgetFactory.CreateAlert(world, parent, "Message", testFont, config);

        ref readonly var modalData = ref world.Get<UIModal>(modal);
        Assert.Equal("Warning!", modalData.Title);
    }

    #endregion

    #region Confirm Tests

    [Fact]
    public void CreateConfirm_ReturnsModalBackdropAndContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, contentPanel) = WidgetFactory.CreateConfirm(world, parent, "Are you sure?", testFont);

        Assert.True(world.IsAlive(modal));
        Assert.True(world.IsAlive(backdrop));
        Assert.True(world.IsAlive(contentPanel));
    }

    [Fact]
    public void CreateConfirm_HasTwoButtons()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, _, _) = WidgetFactory.CreateConfirm(world, parent, "Are you sure?", testFont);

        // Find all modal buttons by traversing modal hierarchy
        var modalButtons = CountEntitiesWithComponent<UIModalButton>(world, modal);
        Assert.Equal(2, modalButtons);
    }

    [Fact]
    public void CreateConfirm_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ConfirmConfig { Title = "Confirm Delete", OkButtonText = "Delete", CancelButtonText = "Keep" };

        var (modal, _, _) = WidgetFactory.CreateConfirm(world, parent, "Delete this item?", testFont, config);

        ref readonly var modalData = ref world.Get<UIModal>(modal);
        Assert.Equal("Confirm Delete", modalData.Title);
    }

    #endregion

    #region Prompt Tests

    [Fact]
    public void CreatePrompt_ReturnsModalBackdropContentPanelAndTextInput()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (modal, backdrop, contentPanel, textInput) = WidgetFactory.CreatePrompt(world, parent, "Enter name:", testFont);

        Assert.True(world.IsAlive(modal));
        Assert.True(world.IsAlive(backdrop));
        Assert.True(world.IsAlive(contentPanel));
        Assert.True(world.IsAlive(textInput));
    }

    [Fact]
    public void CreatePrompt_TextInputHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, _, _, textInput) = WidgetFactory.CreatePrompt(world, parent, "Enter name:", testFont);

        Assert.True(world.Has<UIElement>(textInput));
        Assert.True(world.Has<UIRect>(textInput));
        Assert.True(world.Has<UIStyle>(textInput));
        Assert.True(world.Has<UIText>(textInput));
        Assert.True(world.Has<UIInteractable>(textInput));
        Assert.True(world.Has<UITextInput>(textInput));
    }

    [Fact]
    public void CreatePrompt_AppliesInitialValue()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PromptConfig { InitialValue = "Default Value" };

        var (_, _, _, textInput) = WidgetFactory.CreatePrompt(world, parent, "Enter value:", testFont, config);

        ref readonly var text = ref world.Get<UIText>(textInput);
        Assert.Equal("Default Value", text.Content);
    }

    [Fact]
    public void CreatePrompt_AppliesPlaceholder()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PromptConfig { Placeholder = "Type here..." };

        var (_, _, _, textInput) = WidgetFactory.CreatePrompt(world, parent, "Enter value:", testFont, config);

        ref readonly var textInputData = ref world.Get<UITextInput>(textInput);
        Assert.Equal("Type here...", textInputData.PlaceholderText);
    }

    #endregion

    #region Helper Methods

    private static Entity CreateRootEntity(World world)
    {
        var root = world.Spawn("Root")
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);
        layout.Update(0);

        return root;
    }

    private static bool FindEntityWithComponent<T>(World world, Entity root) where T : struct, IComponent
    {
        var stack = new Stack<Entity>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (world.Has<T>(current))
            {
                return true;
            }
            foreach (var child in world.GetChildren(current))
            {
                stack.Push(child);
            }
        }
        return false;
    }

    private static int CountEntitiesWithComponent<T>(World world, Entity root) where T : struct, IComponent
    {
        var count = 0;
        var stack = new Stack<Entity>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (world.Has<T>(current))
            {
                count++;
            }
            foreach (var child in world.GetChildren(current))
            {
                stack.Push(child);
            }
        }
        return count;
    }

    #endregion
}
