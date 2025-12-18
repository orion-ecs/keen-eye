using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for menu UI widgets: MenuBar, Menu, ContextMenu, RadialMenu.
/// </summary>
public static partial class WidgetFactory
{
    #region MenuBar

    /// <summary>
    /// Creates a menu bar widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for menu text.</param>
    /// <param name="menus">The menu definitions (label and items).</param>
    /// <param name="config">Optional menu bar configuration.</param>
    /// <returns>The created menu bar entity.</returns>
    /// <remarks>
    /// <para>
    /// The menu bar is a horizontal strip containing top-level menu items (File, Edit, etc.).
    /// Clicking a menu item opens its dropdown. While open, hovering over other items
    /// switches to their dropdowns.
    /// </para>
    /// </remarks>
    public static Entity CreateMenuBar(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<(string Label, IEnumerable<MenuItemDef> Items)> menus,
        MenuBarConfig? config = null)
    {
        config ??= MenuBarConfig.Default;

        // Create menu bar container
        var menuBar = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                Padding = UIEdges.Symmetric(4, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 0
            })
            .With(new UIMenuBar { ActiveMenu = Entity.Null, IsActive = false })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(menuBar, parent);
        }

        var menuIndex = 0;
        foreach (var (label, items) in menus)
        {
            // Create menu bar item
            var menuBarItem = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0, 0.5f),
                    Size = new Vector2(0, config.Height),
                    WidthMode = UISizeMode.FitContent,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = Vector4.Zero,
                    Padding = UIEdges.Symmetric(config.ItemPadding, 0)
                })
                .With(new UIText
                {
                    Content = label,
                    Font = font,
                    Color = config.GetTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .Build();

            world.SetParent(menuBarItem, menuBar);

            // Create dropdown menu for this menu bar item
            var dropdown = CreateMenu(world, menuBarItem, font, items, MenuConfig.Default);

            // Link the menu bar item to its dropdown
            world.Add(menuBarItem, new UIMenuBarItem
            {
                MenuBar = menuBar,
                DropdownMenu = dropdown,
                Label = label,
                Index = menuIndex
            });

            // Link the dropdown to its menu bar item
            ref var menuData = ref world.Get<UIMenu>(dropdown);
            menuData.MenuBarItem = menuBarItem;

            menuIndex++;
        }

        return menuBar;
    }

    #endregion

    #region Menu

    /// <summary>
    /// Creates a dropdown or popup menu widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity (menu bar item or trigger element).</param>
    /// <param name="font">The font to use for menu text.</param>
    /// <param name="items">The menu item definitions.</param>
    /// <param name="config">Optional menu configuration.</param>
    /// <returns>The created menu entity.</returns>
    public static Entity CreateMenu(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<MenuItemDef> items,
        MenuConfig? config = null)
    {
        config ??= MenuConfig.Default;

        var itemList = items.ToList();

        // Create menu container
        var menu = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 1),
                AnchorMax = new Vector2(0, 1),
                Pivot = new Vector2(0, 0),
                Size = new Vector2(config.MinWidth, 0),
                WidthMode = config.MaxWidth > 0 ? UISizeMode.Fixed : UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.CornerRadius,
                Padding = new UIEdges(4, 4, 4, 4)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 2
            })
            .With(new UIMenu
            {
                IsOpen = false,
                ParentMenu = Entity.Null,
                OpenSubmenu = Entity.Null,
                SelectedIndex = -1,
                ItemCount = itemList.Count,
                MenuBarItem = Entity.Null
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(menu, parent);
        }

        // Create menu items
        var itemIndex = 0;
        foreach (var itemDef in itemList)
        {
            CreateMenuItemEntity(world, menu, font, itemDef, itemIndex, config);
            itemIndex++;
        }

        return menu;
    }

    /// <summary>
    /// Creates a context menu (right-click menu) widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="font">The font to use for menu text.</param>
    /// <param name="items">The menu item definitions.</param>
    /// <param name="config">Optional menu configuration.</param>
    /// <returns>The created context menu entity.</returns>
    /// <remarks>
    /// <para>
    /// Context menus are not parented to any element. They are positioned absolutely
    /// when shown via <see cref="UIContextMenuRequestEvent"/>.
    /// </para>
    /// </remarks>
    public static Entity CreateContextMenu(
        IWorld world,
        FontHandle font,
        IEnumerable<MenuItemDef> items,
        MenuConfig? config = null)
    {
        config ??= MenuConfig.Default;

        var itemList = items.ToList();

        var menu = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.MinWidth, 0),
                WidthMode = config.MaxWidth > 0 ? UISizeMode.Fixed : UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.CornerRadius,
                Padding = new UIEdges(4, 4, 4, 4)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 2
            })
            .With(new UIMenu
            {
                IsOpen = false,
                ParentMenu = Entity.Null,
                OpenSubmenu = Entity.Null,
                SelectedIndex = -1,
                ItemCount = itemList.Count,
                MenuBarItem = Entity.Null
            })
            .Build();

        var itemIndex = 0;
        foreach (var itemDef in itemList)
        {
            CreateMenuItemEntity(world, menu, font, itemDef, itemIndex, config);
            itemIndex++;
        }

        return menu;
    }

    private static Entity CreateMenuItemEntity(
        IWorld world,
        Entity menu,
        FontHandle font,
        MenuItemDef itemDef,
        int index,
        MenuConfig config)
    {
        if (itemDef.IsSeparator)
        {
            // Create separator
            var separator = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, 1),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetSeparatorColor(),
                    Padding = UIEdges.Symmetric(8, 4)
                })
                .With(new UIMenuItem
                {
                    Menu = menu,
                    Submenu = Entity.Null,
                    IsEnabled = false,
                    IsChecked = false,
                    IsSeparator = true,
                    Index = index,
                    Label = "",
                    ItemId = ""
                })
                .Build();

            world.SetParent(separator, menu);
            return separator;
        }

        // Create regular menu item
        var textColor = itemDef.IsEnabled ? config.GetTextColor() : config.GetDisabledTextColor();

        var item = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.ItemHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = Vector4.Zero,
                Padding = UIEdges.Symmetric(8, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .With(new UIInteractable
            {
                CanClick = itemDef.IsEnabled
            })
            .With(new UIMenuItem
            {
                Menu = menu,
                Submenu = Entity.Null,
                IsEnabled = itemDef.IsEnabled,
                IsChecked = itemDef.IsChecked,
                IsSeparator = false,
                Index = index,
                Label = itemDef.Label,
                ItemId = itemDef.ItemId ?? itemDef.Label
            })
            .Build();

        world.SetParent(item, menu);

        // Create label
        var label = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, config.ItemHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = (itemDef.IsToggle && itemDef.IsChecked ? "✓ " : "") + itemDef.Label,
                Font = font,
                Color = textColor,
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(label, item);

        // Create shortcut label if present
        if (!string.IsNullOrEmpty(itemDef.Shortcut))
        {
            var shortcutLabel = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(1, 0.5f),
                    Size = new Vector2(0, config.ItemHeight),
                    WidthMode = UISizeMode.FitContent,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIText
                {
                    Content = itemDef.Shortcut,
                    Font = font,
                    Color = config.GetShortcutColor(),
                    FontSize = config.FontSize * 0.9f,
                    HorizontalAlign = TextAlignH.Right,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIMenuShortcut(itemDef.Shortcut))
                .Build();

            world.SetParent(shortcutLabel, item);
        }

        // Add submenu indicator if has submenu
        if (itemDef.SubmenuItems != null)
        {
            var arrow = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(1, 0.5f),
                    Size = new Vector2(16, config.ItemHeight),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIText
                {
                    Content = "▶",
                    Font = font,
                    Color = textColor,
                    FontSize = config.FontSize * 0.7f,
                    HorizontalAlign = TextAlignH.Right,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(arrow, item);

            // Create submenu
            var submenu = CreateMenu(world, item, font, itemDef.SubmenuItems, config);

            // Update submenu positioning for submenu (opens to the right)
            ref var submenuRect = ref world.Get<UIRect>(submenu);
            submenuRect.AnchorMin = new Vector2(1, 0);
            submenuRect.AnchorMax = new Vector2(1, 0);
            submenuRect.Pivot = new Vector2(0, 1);

            // Link submenu
            ref var menuData = ref world.Get<UIMenu>(submenu);
            menuData.ParentMenu = menu;

            ref var itemData = ref world.Get<UIMenuItem>(item);
            itemData.Submenu = submenu;

            world.Add(item, new UIMenuSubmenuTag());
        }

        // Add toggle tag if needed
        if (itemDef.IsToggle)
        {
            world.Add(item, new UIMenuToggleTag());
        }

        return item;
    }

    #endregion

    #region RadialMenu

    /// <summary>
    /// Creates a radial (pie) menu widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="font">The font to use for slice labels.</param>
    /// <param name="slices">The slice definitions.</param>
    /// <param name="config">Optional radial menu configuration.</param>
    /// <returns>The created radial menu entity.</returns>
    /// <remarks>
    /// <para>
    /// Radial menus are typically used with gamepads. The menu appears at a position
    /// and the user selects by moving in the direction of the desired option.
    /// </para>
    /// </remarks>
    public static Entity CreateRadialMenu(
        IWorld world,
        FontHandle font,
        IEnumerable<RadialSliceDef> slices,
        RadialMenuConfig? config = null)
    {
        config ??= RadialMenuConfig.Default;

        var sliceList = slices.ToList();
        var sliceCount = sliceList.Count;
        var sliceAngle = 2 * MathF.PI / sliceCount;

        // Create radial menu container
        var menuSize = config.OuterRadius * 2;
        var menu = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(menuSize, menuSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = Vector4.Zero  // Slices handle their own rendering
            })
            .With(new UIRadialMenu(sliceCount)
            {
                IsOpen = false,
                SelectedIndex = -1,
                InnerRadius = config.InnerRadius,
                OuterRadius = config.OuterRadius,
                OpenProgress = 0f,
                StartAngle = config.StartAngle
            })
            .With(new UIRadialMenuInputState
            {
                InputDirection = Vector2.Zero,
                InputMagnitude = 0f,
                IsTriggerHeld = false,
                OpenTime = 0f
            })
            .Build();

        // Create slices
        for (var i = 0; i < sliceCount; i++)
        {
            var sliceDef = sliceList[i];
            var startAngle = config.StartAngle + i * sliceAngle;
            var endAngle = startAngle + sliceAngle;

            var slice = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = sliceDef.IsEnabled
                        ? config.GetBackgroundColor()
                        : config.GetDisabledColor()
                })
                .With(new UIRadialSlice(menu, i)
                {
                    StartAngle = startAngle,
                    EndAngle = endAngle,
                    HasSubmenu = sliceDef.SubSlices != null,
                    IsEnabled = sliceDef.IsEnabled,
                    Label = sliceDef.Label,
                    ItemId = sliceDef.ItemId ?? sliceDef.Label,
                    Submenu = Entity.Null
                })
                .Build();

            world.SetParent(slice, menu);

            // Add label if enabled
            if (config.ShowLabels)
            {
                // Position label at midpoint of slice arc
                var midAngle = (startAngle + endAngle) / 2;
                var labelDistance = (config.InnerRadius + config.OuterRadius) / 2;
                var labelX = MathF.Cos(midAngle) * labelDistance;
                var labelY = MathF.Sin(midAngle) * labelDistance;

                var label = world.Spawn()
                    .With(new UIElement { Visible = true, RaycastTarget = false })
                    .With(new UIRect
                    {
                        AnchorMin = new Vector2(0.5f, 0.5f),
                        AnchorMax = new Vector2(0.5f, 0.5f),
                        Pivot = new Vector2(0.5f, 0.5f),
                        Offset = new UIEdges(labelX + config.OuterRadius, labelY + config.OuterRadius, 0, 0),
                        Size = new Vector2(config.OuterRadius * 0.6f, 20),
                        WidthMode = UISizeMode.Fixed,
                        HeightMode = UISizeMode.Fixed
                    })
                    .With(new UIText
                    {
                        Content = sliceDef.Label,
                        Font = font,
                        Color = config.GetTextColor(),
                        FontSize = config.FontSize,
                        HorizontalAlign = TextAlignH.Center,
                        VerticalAlign = TextAlignV.Middle
                    })
                    .Build();

                world.SetParent(label, slice);
            }

            // Create submenu if defined
            if (sliceDef.SubSlices != null)
            {
                var submenu = CreateRadialMenu(world, font, sliceDef.SubSlices, config);
                ref var sliceData = ref world.Get<UIRadialSlice>(slice);
                sliceData.Submenu = submenu;
            }
        }

        return menu;
    }

    /// <summary>
    /// Opens a radial menu at the specified position.
    /// </summary>
    /// <param name="world">The world containing the menu.</param>
    /// <param name="menu">The radial menu entity.</param>
    /// <param name="position">The center position for the menu.</param>
    public static void OpenRadialMenu(IWorld world, Entity menu, Vector2 position)
    {
        if (!world.Has<UIRadialMenu>(menu))
        {
            return;
        }

        ref var radialMenu = ref world.Get<UIRadialMenu>(menu);
        radialMenu.IsOpen = true;
        radialMenu.CenterPosition = position;
        radialMenu.OpenProgress = 0f;
        radialMenu.SelectedIndex = -1;

        ref var rect = ref world.Get<UIRect>(menu);
        rect.Offset = new UIEdges(position.X, position.Y, 0, 0);

        ref var element = ref world.Get<UIElement>(menu);
        element.Visible = true;

        world.Add(menu, new UIRadialMenuOpenTag());
    }

    /// <summary>
    /// Closes a radial menu.
    /// </summary>
    /// <param name="world">The world containing the menu.</param>
    /// <param name="menu">The radial menu entity.</param>
    public static void CloseRadialMenu(IWorld world, Entity menu)
    {
        if (!world.Has<UIRadialMenu>(menu))
        {
            return;
        }

        ref var radialMenu = ref world.Get<UIRadialMenu>(menu);
        radialMenu.IsOpen = false;
        radialMenu.SelectedIndex = -1;

        ref var element = ref world.Get<UIElement>(menu);
        element.Visible = false;

        if (world.Has<UIRadialMenuOpenTag>(menu))
        {
            world.Remove<UIRadialMenuOpenTag>(menu);
        }
    }

    #endregion
}
