using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for dock UI widgets: DockContainer, DockPanel.
/// </summary>
public static partial class WidgetFactory
{
    #region DockContainer

    /// <summary>
    /// Creates a dock container that manages docking zones for panels.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional dock container configuration.</param>
    /// <returns>The created dock container entity.</returns>
    /// <remarks>
    /// <para>
    /// A dock container provides zones where panels can be docked:
    /// - Left, Right, Top, Bottom zones around the edges
    /// - Center zone for the main content area
    /// </para>
    /// <para>
    /// Zones are separated by splitters that can be dragged to resize them.
    /// Multiple panels in the same zone are displayed as tabs.
    /// </para>
    /// </remarks>
    public static Entity CreateDockContainer(
        IWorld world,
        Entity parent,
        DockContainerConfig? config = null)
    {
        config ??= DockContainerConfig.Default;

        // Create main container
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(UIRect.Stretch())
            .With(new UIStyle
            {
                BackgroundColor = Vector4.Zero
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create dock zones
        var leftZone = CreateDockZone(world, container, DockZone.Left, config, config.ShowLeftZone);
        var rightZone = CreateDockZone(world, container, DockZone.Right, config, config.ShowRightZone);
        var topZone = CreateDockZone(world, container, DockZone.Top, config, config.ShowTopZone);
        var bottomZone = CreateDockZone(world, container, DockZone.Bottom, config, config.ShowBottomZone);
        var centerZone = CreateDockZone(world, container, DockZone.Center, config, true);

        // Create preview overlay (hidden by default)
        var previewOverlay = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(100, 100),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetPreviewColor()
            })
            .WithTag<UIDockPreviewTag>()
            .Build();

        world.SetParent(previewOverlay, container);

        // Add dock container component
        world.Add(container, new UIDockContainer
        {
            LeftZone = leftZone,
            RightZone = rightZone,
            TopZone = topZone,
            BottomZone = bottomZone,
            CenterZone = centerZone,
            DraggingPanel = Entity.Null,
            PreviewOverlay = previewOverlay
        });

        return container;
    }

    /// <summary>
    /// Creates a dock container with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional dock container configuration.</param>
    /// <returns>The created dock container entity.</returns>
    public static Entity CreateDockContainer(
        IWorld world,
        Entity parent,
        string name,
        DockContainerConfig? config = null)
    {
        config ??= DockContainerConfig.Default;

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(UIRect.Stretch())
            .With(new UIStyle
            {
                BackgroundColor = Vector4.Zero
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var leftZone = CreateDockZone(world, container, $"{name}_LeftZone", DockZone.Left, config, config.ShowLeftZone);
        var rightZone = CreateDockZone(world, container, $"{name}_RightZone", DockZone.Right, config, config.ShowRightZone);
        var topZone = CreateDockZone(world, container, $"{name}_TopZone", DockZone.Top, config, config.ShowTopZone);
        var bottomZone = CreateDockZone(world, container, $"{name}_BottomZone", DockZone.Bottom, config, config.ShowBottomZone);
        var centerZone = CreateDockZone(world, container, $"{name}_CenterZone", DockZone.Center, config, true);

        var previewOverlay = world.Spawn($"{name}_Preview")
            .With(new UIElement { Visible = false, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(100, 100),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetPreviewColor()
            })
            .WithTag<UIDockPreviewTag>()
            .Build();

        world.SetParent(previewOverlay, container);

        world.Add(container, new UIDockContainer
        {
            LeftZone = leftZone,
            RightZone = rightZone,
            TopZone = topZone,
            BottomZone = bottomZone,
            CenterZone = centerZone,
            DraggingPanel = Entity.Null,
            PreviewOverlay = previewOverlay
        });

        return container;
    }

    private static Entity CreateDockZone(
        IWorld world,
        Entity container,
        DockZone zone,
        DockContainerConfig config,
        bool visible)
    {
        var size = zone switch
        {
            DockZone.Left => config.LeftZoneSize,
            DockZone.Right => config.RightZoneSize,
            DockZone.Top => config.TopZoneSize,
            DockZone.Bottom => config.BottomZoneSize,
            _ => 0f
        };

        var isHorizontal = zone is DockZone.Left or DockZone.Right;
        var isCenter = zone == DockZone.Center;

        var zoneEntity = world.Spawn()
            .With(new UIElement { Visible = visible, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = isCenter ? Vector2.Zero : (isHorizontal ? new Vector2(size, 0) : new Vector2(0, size)),
                WidthMode = isCenter ? UISizeMode.Fill : (isHorizontal ? UISizeMode.Fixed : UISizeMode.Fill),
                HeightMode = isCenter ? UISizeMode.Fill : (isHorizontal ? UISizeMode.Fill : UISizeMode.Fixed)
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetZoneBackgroundColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIDockZone(zone)
            {
                Size = size,
                MinSize = config.MinZoneSize,
                IsCollapsed = !visible,
                TabGroup = Entity.Null,
                Container = container
            })
            .Build();

        world.SetParent(zoneEntity, container);

        // Create tab group for this zone
        var tabGroup = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIDockTabGroup
            {
                SelectedIndex = 0,
                DockZone = zoneEntity,
                TabCount = 0
            })
            .Build();

        world.SetParent(tabGroup, zoneEntity);

        // Update zone with tab group reference
        ref var zoneData = ref world.Get<UIDockZone>(zoneEntity);
        zoneData.TabGroup = tabGroup;

        return zoneEntity;
    }

    private static Entity CreateDockZone(
        IWorld world,
        Entity container,
        string name,
        DockZone zone,
        DockContainerConfig config,
        bool visible)
    {
        var size = zone switch
        {
            DockZone.Left => config.LeftZoneSize,
            DockZone.Right => config.RightZoneSize,
            DockZone.Top => config.TopZoneSize,
            DockZone.Bottom => config.BottomZoneSize,
            _ => 0f
        };

        var isHorizontal = zone is DockZone.Left or DockZone.Right;
        var isCenter = zone == DockZone.Center;

        var zoneEntity = world.Spawn(name)
            .With(new UIElement { Visible = visible, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = isCenter ? Vector2.Zero : (isHorizontal ? new Vector2(size, 0) : new Vector2(0, size)),
                WidthMode = isCenter ? UISizeMode.Fill : (isHorizontal ? UISizeMode.Fixed : UISizeMode.Fill),
                HeightMode = isCenter ? UISizeMode.Fill : (isHorizontal ? UISizeMode.Fill : UISizeMode.Fixed)
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetZoneBackgroundColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIDockZone(zone)
            {
                Size = size,
                MinSize = config.MinZoneSize,
                IsCollapsed = !visible,
                TabGroup = Entity.Null,
                Container = container
            })
            .Build();

        world.SetParent(zoneEntity, container);

        var tabGroup = world.Spawn($"{name}_Tabs")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIDockTabGroup
            {
                SelectedIndex = 0,
                DockZone = zoneEntity,
                TabCount = 0
            })
            .Build();

        world.SetParent(tabGroup, zoneEntity);

        ref var zoneData = ref world.Get<UIDockZone>(zoneEntity);
        zoneData.TabGroup = tabGroup;

        return zoneEntity;
    }

    #endregion

    #region DockPanel

    /// <summary>
    /// Creates a dockable panel.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity (usually a dock container or null for floating).</param>
    /// <param name="title">The panel title.</param>
    /// <param name="font">The font to use for the title.</param>
    /// <param name="config">Optional dock panel configuration.</param>
    /// <returns>A tuple containing the panel entity and the content panel entity.</returns>
    /// <remarks>
    /// <para>
    /// Dock panels can be:
    /// - Floating as independent windows
    /// - Docked in a dock zone
    /// - Tabbed with other panels in the same zone
    /// </para>
    /// <para>
    /// Add content to the returned content panel entity.
    /// </para>
    /// </remarks>
    public static (Entity Panel, Entity ContentPanel) CreateDockPanel(
        IWorld world,
        Entity parent,
        string title,
        FontHandle font,
        DockPanelConfig? config = null)
    {
        config ??= DockPanelConfig.Default;

        // Create panel container
        var panel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIDockPanel(title)
            {
                State = DockState.Floating,
                CurrentZone = DockZone.None,
                DockContainer = Entity.Null,
                CanClose = config.CanClose,
                CanFloat = config.CanFloat,
                CanDock = config.CanDock,
                AllowedZones = config.AllowedZones,
                FloatingPosition = Vector2.Zero,
                FloatingSize = new Vector2(config.Width, config.Height)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(panel, parent);
        }

        // Create title bar
        var titleBar = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTitleBarColor(),
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
                CanDrag = true
            })
            .Build();

        world.SetParent(titleBar, panel);

        // Create title label
        var titleLabel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = title,
                Font = font,
                Color = config.GetTitleTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(titleLabel, titleBar);

        // Create close button if enabled
        if (config.CanClose)
        {
            var closeButton = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(config.TitleBarHeight - 8, config.TitleBarHeight - 8),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = Vector4.Zero,
                    CornerRadius = 3f
                })
                .With(new UIText
                {
                    Content = "X",
                    Font = font,
                    Color = config.GetTitleTextColor(),
                    FontSize = config.FontSize * 0.8f,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .Build();

            world.SetParent(closeButton, titleBar);
        }

        // Create content panel
        var contentPanel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                Padding = new UIEdges(8, 8, 8, 8)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, panel);

        return (panel, contentPanel);
    }

    /// <summary>
    /// Creates a dockable panel with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity (usually a dock container or null for floating).</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="title">The panel title.</param>
    /// <param name="font">The font to use for the title.</param>
    /// <param name="config">Optional dock panel configuration.</param>
    /// <returns>A tuple containing the panel entity and the content panel entity.</returns>
    public static (Entity Panel, Entity ContentPanel) CreateDockPanel(
        IWorld world,
        Entity parent,
        string name,
        string title,
        FontHandle font,
        DockPanelConfig? config = null)
    {
        config ??= DockPanelConfig.Default;

        var panel = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIDockPanel(title)
            {
                State = DockState.Floating,
                CurrentZone = DockZone.None,
                DockContainer = Entity.Null,
                CanClose = config.CanClose,
                CanFloat = config.CanFloat,
                CanDock = config.CanDock,
                AllowedZones = config.AllowedZones,
                FloatingPosition = Vector2.Zero,
                FloatingSize = new Vector2(config.Width, config.Height)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(panel, parent);
        }

        var titleBar = world.Spawn($"{name}_TitleBar")
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTitleBarColor(),
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
                CanDrag = true
            })
            .Build();

        world.SetParent(titleBar, panel);

        var titleLabel = world.Spawn($"{name}_Title")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = title,
                Font = font,
                Color = config.GetTitleTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(titleLabel, titleBar);

        if (config.CanClose)
        {
            var closeButton = world.Spawn($"{name}_CloseButton")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(config.TitleBarHeight - 8, config.TitleBarHeight - 8),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = Vector4.Zero,
                    CornerRadius = 3f
                })
                .With(new UIText
                {
                    Content = "X",
                    Font = font,
                    Color = config.GetTitleTextColor(),
                    FontSize = config.FontSize * 0.8f,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .Build();

            world.SetParent(closeButton, titleBar);
        }

        var contentPanel = world.Spawn($"{name}_Content")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                Padding = new UIEdges(8, 8, 8, 8)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, panel);

        return (panel, contentPanel);
    }

    /// <summary>
    /// Docks a panel to a specific zone in a dock container.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="panel">The panel to dock.</param>
    /// <param name="container">The dock container.</param>
    /// <param name="zone">The zone to dock to.</param>
    public static void DockPanel(IWorld world, Entity panel, Entity container, DockZone zone)
    {
        if (!world.Has<UIDockPanel>(panel) || !world.Has<UIDockContainer>(container))
        {
            return;
        }

        ref var panelData = ref world.Get<UIDockPanel>(panel);
        ref var containerData = ref world.Get<UIDockContainer>(container);

        // Get the zone entity
        var zoneEntity = zone switch
        {
            DockZone.Left => containerData.LeftZone,
            DockZone.Right => containerData.RightZone,
            DockZone.Top => containerData.TopZone,
            DockZone.Bottom => containerData.BottomZone,
            DockZone.Center => containerData.CenterZone,
            _ => Entity.Null
        };

        if (!zoneEntity.IsValid)
        {
            return;
        }

        // Check if panel is allowed in this zone
        if ((panelData.AllowedZones & zone) == 0)
        {
            return;
        }

        // Update panel state
        panelData.State = DockState.Docked;
        panelData.CurrentZone = zone;
        panelData.DockContainer = container;

        // Parent panel to the zone's tab group
        ref var zoneData = ref world.Get<UIDockZone>(zoneEntity);
        world.SetParent(panel, zoneData.TabGroup);

        // Make zone visible
        zoneData.IsCollapsed = false;
        ref var zoneElement = ref world.Get<UIElement>(zoneEntity);
        zoneElement.Visible = true;

        // Update tab count
        ref var tabGroup = ref world.Get<UIDockTabGroup>(zoneData.TabGroup);
        tabGroup.TabCount++;
    }

    /// <summary>
    /// Undocks a panel, making it a floating window.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="panel">The panel to undock.</param>
    /// <param name="position">The floating window position.</param>
    public static void UndockPanel(IWorld world, Entity panel, Vector2 position)
    {
        if (!world.Has<UIDockPanel>(panel))
        {
            return;
        }

        ref var panelData = ref world.Get<UIDockPanel>(panel);

        if (panelData.State != DockState.Docked)
        {
            return;
        }

        // Update panel state
        panelData.State = DockState.Floating;
        panelData.FloatingPosition = position;
        var previousZone = panelData.CurrentZone;
        panelData.CurrentZone = DockZone.None;

        // Remove from parent (dock zone)
        world.SetParent(panel, Entity.Null);

        // Update panel position
        ref var rect = ref world.Get<UIRect>(panel);
        rect.Offset = new UIEdges(position.X, position.Y, 0, 0);
        rect.Size = panelData.FloatingSize;

        // Update tab count in previous zone
        if (panelData.DockContainer.IsValid && world.Has<UIDockContainer>(panelData.DockContainer))
        {
            ref var containerData = ref world.Get<UIDockContainer>(panelData.DockContainer);
            var zoneEntity = previousZone switch
            {
                DockZone.Left => containerData.LeftZone,
                DockZone.Right => containerData.RightZone,
                DockZone.Top => containerData.TopZone,
                DockZone.Bottom => containerData.BottomZone,
                DockZone.Center => containerData.CenterZone,
                _ => Entity.Null
            };

            if (zoneEntity.IsValid && world.Has<UIDockZone>(zoneEntity))
            {
                ref var zoneData = ref world.Get<UIDockZone>(zoneEntity);
                if (zoneData.TabGroup.IsValid && world.Has<UIDockTabGroup>(zoneData.TabGroup))
                {
                    ref var tabGroup = ref world.Get<UIDockTabGroup>(zoneData.TabGroup);
                    tabGroup.TabCount = Math.Max(0, tabGroup.TabCount - 1);
                }
            }
        }

        panelData.DockContainer = Entity.Null;
    }

    #endregion
}
