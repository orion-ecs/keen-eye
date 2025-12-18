using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for window UI widgets: Window, Splitter.
/// </summary>
public static partial class WidgetFactory
{
    #region Window

    /// <summary>
    /// Creates a floating window widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="title">The window title.</param>
    /// <param name="font">The font to use for the title.</param>
    /// <param name="config">Optional window configuration.</param>
    /// <returns>A tuple containing the window entity and the content panel entity.</returns>
    /// <remarks>
    /// <para>
    /// The window is composed of:
    /// - A window container with absolute positioning
    /// - A title bar with drag capability
    /// - An optional close button
    /// - A content panel for child elements
    /// </para>
    /// <para>
    /// Add children to the returned content panel entity.
    /// </para>
    /// </remarks>
    public static (Entity Window, Entity ContentPanel) CreateWindow(
        IWorld world,
        Entity parent,
        string title,
        FontHandle font,
        UIWindowConfig? config = null)
    {
        config ??= UIWindowConfig.Default;

        // Create window container
        var window = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Offset = new UIEdges(config.X, config.Y, 0, 0),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor(),
                CornerRadius = config.CornerRadius
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIWindow(title)
            {
                CanDrag = config.CanDrag,
                CanResize = config.CanResize,
                CanClose = config.CanClose,
                MinSize = new Vector2(config.MinWidth, config.MinHeight)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(window, parent);
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
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.Symmetric(8f, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .With(new UIInteractable
            {
                CanDrag = config.CanDrag
            })
            .With(new UIWindowTitleBar(window))
            .Build();

        world.SetParent(titleBar, window);

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
                    BackgroundColor = config.GetCloseButtonColor(),
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
                .With(new UIWindowCloseButton(window))
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

        world.SetParent(contentPanel, window);

        return (window, contentPanel);
    }

    /// <summary>
    /// Creates a floating window widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="title">The window title.</param>
    /// <param name="font">The font to use for the title.</param>
    /// <param name="config">Optional window configuration.</param>
    /// <returns>A tuple containing the window entity and the content panel entity.</returns>
    public static (Entity Window, Entity ContentPanel) CreateWindow(
        IWorld world,
        Entity parent,
        string name,
        string title,
        FontHandle font,
        UIWindowConfig? config = null)
    {
        config ??= UIWindowConfig.Default;

        var window = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Offset = new UIEdges(config.X, config.Y, 0, 0),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor(),
                CornerRadius = config.CornerRadius
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIWindow(title)
            {
                CanDrag = config.CanDrag,
                CanResize = config.CanResize,
                CanClose = config.CanClose,
                MinSize = new Vector2(config.MinWidth, config.MinHeight)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(window, parent);
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
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.Symmetric(8f, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .With(new UIInteractable
            {
                CanDrag = config.CanDrag
            })
            .With(new UIWindowTitleBar(window))
            .Build();

        world.SetParent(titleBar, window);

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
                    BackgroundColor = config.GetCloseButtonColor(),
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
                .With(new UIWindowCloseButton(window))
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

        world.SetParent(contentPanel, window);

        return (window, contentPanel);
    }

    #endregion

    #region Splitter

    /// <summary>
    /// Creates a splitter widget that divides space between two panes.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional splitter configuration.</param>
    /// <returns>A tuple containing the splitter container, first pane, and second pane entities.</returns>
    /// <remarks>
    /// <para>
    /// The splitter is composed of:
    /// - A container with the split orientation
    /// - A first pane (left or top)
    /// - A draggable handle
    /// - A second pane (right or bottom)
    /// </para>
    /// <para>
    /// Drag the handle to resize the panes. The split ratio is stored in the
    /// container's <see cref="UISplitter"/> component.
    /// </para>
    /// </remarks>
    public static (Entity Container, Entity FirstPane, Entity SecondPane) CreateSplitter(
        IWorld world,
        Entity parent,
        SplitterConfig? config = null)
    {
        config ??= SplitterConfig.Default;

        var isHorizontal = config.Orientation == LayoutDirection.Horizontal;

        // Create container
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = config.Orientation,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UISplitter(config.Orientation, config.InitialRatio)
            {
                MinFirstPane = config.MinFirstPane,
                MinSecondPane = config.MinSecondPane,
                HandleSize = config.HandleSize
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create first pane
        var firstPane = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = isHorizontal ? UISizeMode.Percentage : UISizeMode.Fill,
                HeightMode = isHorizontal ? UISizeMode.Fill : UISizeMode.Percentage
            })
            .With(new UIStyle
            {
                BackgroundColor = config.FirstPaneColor ?? Vector4.Zero
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UISplitterFirstPane(container))
            .Build();

        // Set percentage based on initial ratio
        ref var firstPaneRect = ref world.Get<UIRect>(firstPane);
        if (isHorizontal)
        {
            firstPaneRect.Size = new Vector2(config.InitialRatio * 100, 0);
        }
        else
        {
            firstPaneRect.Size = new Vector2(0, config.InitialRatio * 100);
        }

        world.SetParent(firstPane, container);

        // Create handle
        var handle = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = isHorizontal
                    ? new Vector2(config.HandleSize, 0)
                    : new Vector2(0, config.HandleSize),
                WidthMode = isHorizontal ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = isHorizontal ? UISizeMode.Fill : UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetHandleColor()
            })
            .With(new UIInteractable
            {
                CanDrag = true
            })
            .With(new UISplitterHandle(container))
            .Build();

        world.SetParent(handle, container);

        // Create second pane
        var secondPane = world.Spawn()
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
                BackgroundColor = config.SecondPaneColor ?? Vector4.Zero
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UISplitterSecondPane(container))
            .Build();

        world.SetParent(secondPane, container);

        return (container, firstPane, secondPane);
    }

    /// <summary>
    /// Creates a splitter widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional splitter configuration.</param>
    /// <returns>A tuple containing the splitter container, first pane, and second pane entities.</returns>
    public static (Entity Container, Entity FirstPane, Entity SecondPane) CreateSplitter(
        IWorld world,
        Entity parent,
        string name,
        SplitterConfig? config = null)
    {
        config ??= SplitterConfig.Default;

        var isHorizontal = config.Orientation == LayoutDirection.Horizontal;

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = config.Orientation,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UISplitter(config.Orientation, config.InitialRatio)
            {
                MinFirstPane = config.MinFirstPane,
                MinSecondPane = config.MinSecondPane,
                HandleSize = config.HandleSize
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var firstPane = world.Spawn($"{name}_FirstPane")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = isHorizontal ? UISizeMode.Percentage : UISizeMode.Fill,
                HeightMode = isHorizontal ? UISizeMode.Fill : UISizeMode.Percentage
            })
            .With(new UIStyle
            {
                BackgroundColor = config.FirstPaneColor ?? Vector4.Zero
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UISplitterFirstPane(container))
            .Build();

        ref var firstPaneRect = ref world.Get<UIRect>(firstPane);
        if (isHorizontal)
        {
            firstPaneRect.Size = new Vector2(config.InitialRatio * 100, 0);
        }
        else
        {
            firstPaneRect.Size = new Vector2(0, config.InitialRatio * 100);
        }

        world.SetParent(firstPane, container);

        var handle = world.Spawn($"{name}_Handle")
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = isHorizontal
                    ? new Vector2(config.HandleSize, 0)
                    : new Vector2(0, config.HandleSize),
                WidthMode = isHorizontal ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = isHorizontal ? UISizeMode.Fill : UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetHandleColor()
            })
            .With(new UIInteractable
            {
                CanDrag = true
            })
            .With(new UISplitterHandle(container))
            .Build();

        world.SetParent(handle, container);

        var secondPane = world.Spawn($"{name}_SecondPane")
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
                BackgroundColor = config.SecondPaneColor ?? Vector4.Zero
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UISplitterSecondPane(container))
            .Build();

        world.SetParent(secondPane, container);

        return (container, firstPane, secondPane);
    }

    #endregion
}
