using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for display UI widgets: Spinner, ProgressBar, TabView, ScrollView.
/// </summary>
public static partial class WidgetFactory
{
    #region Spinner

    /// <summary>
    /// Creates a spinner/loading indicator widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="config">Optional spinner configuration.</param>
    /// <returns>The spinner entity.</returns>
    /// <remarks>
    /// <para>
    /// The spinner animates automatically via the UISpinnerSystem.
    /// Different styles produce different visual effects:
    /// <list type="bullet">
    /// <item><see cref="SpinnerStyle.Circular"/> - A rotating arc</item>
    /// <item><see cref="SpinnerStyle.Dots"/> - Pulsing dots</item>
    /// <item><see cref="SpinnerStyle.Bar"/> - Oscillating bar</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a default spinner
    /// var spinner = WidgetFactory.CreateSpinner(world);
    ///
    /// // Create a small spinner
    /// var smallSpinner = WidgetFactory.CreateSpinner(world, SpinnerConfig.Small());
    ///
    /// // Create a dot-style spinner
    /// var dotsSpinner = WidgetFactory.CreateSpinner(world, SpinnerConfig.Dots());
    /// </code>
    /// </example>
    public static Entity CreateSpinner(IWorld world, SpinnerConfig? config = null)
    {
        config ??= SpinnerConfig.Default;

        var spinner = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                Size = new Vector2(config.Size, config.Size),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UISpinner(config.Speed)
            {
                Style = config.Style,
                Color = config.GetColor(),
                Thickness = config.Thickness,
                ArcLength = config.ArcLength,
                ElementCount = config.ElementCount
            })
            .Build();

        return spinner;
    }

    /// <summary>
    /// Creates a spinner centered within a parent container.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="config">Optional spinner configuration.</param>
    /// <returns>The spinner entity.</returns>
    public static Entity CreateSpinner(IWorld world, Entity parent, SpinnerConfig? config = null)
    {
        var spinner = CreateSpinner(world, config);

        if (parent.IsValid)
        {
            world.SetParent(spinner, parent);
        }

        return spinner;
    }

    /// <summary>
    /// Creates a spinner with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional spinner configuration.</param>
    /// <returns>The spinner entity.</returns>
    public static Entity CreateSpinner(IWorld world, string name, SpinnerConfig? config = null)
    {
        config ??= SpinnerConfig.Default;

        var spinner = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                Size = new Vector2(config.Size, config.Size),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UISpinner(config.Speed)
            {
                Style = config.Style,
                Color = config.GetColor(),
                Thickness = config.Thickness,
                ArcLength = config.ArcLength,
                ElementCount = config.ElementCount
            })
            .Build();

        return spinner;
    }

    /// <summary>
    /// Creates a full-screen loading overlay with a centered spinner.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent to attach to (typically a root canvas).</param>
    /// <param name="spinnerConfig">Optional spinner configuration.</param>
    /// <param name="backdropColor">Optional backdrop color (defaults to semi-transparent black).</param>
    /// <returns>A tuple containing the overlay entity and spinner entity.</returns>
    /// <remarks>
    /// <para>
    /// The loading overlay blocks user interaction by covering the entire screen.
    /// It includes a centered spinner.
    /// </para>
    /// </remarks>
    public static (Entity Overlay, Entity Spinner) CreateLoadingOverlay(
        IWorld world,
        Entity parent,
        SpinnerConfig? spinnerConfig = null,
        Vector4? backdropColor = null)
    {
        spinnerConfig ??= SpinnerConfig.Large();
        var backdrop = backdropColor ?? new Vector4(0, 0, 0, 0.5f);

        // Create the overlay backdrop
        var overlay = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(UIRect.Stretch())
            .With(new UIStyle
            {
                BackgroundColor = backdrop
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Center,
                CrossAxisAlign = LayoutAlign.Center
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(overlay, parent);
        }

        // Create centered spinner
        var spinner = CreateSpinner(world, overlay, spinnerConfig);

        return (overlay, spinner);
    }

    /// <summary>
    /// Creates a loading overlay with a message.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent to attach to.</param>
    /// <param name="message">The loading message to display.</param>
    /// <param name="font">The font to use for the message.</param>
    /// <param name="spinnerConfig">Optional spinner configuration.</param>
    /// <param name="backdropColor">Optional backdrop color.</param>
    /// <returns>A tuple containing the overlay, spinner, and message text entities.</returns>
    public static (Entity Overlay, Entity Spinner, Entity Message) CreateLoadingOverlay(
        IWorld world,
        Entity parent,
        string message,
        FontHandle font,
        SpinnerConfig? spinnerConfig = null,
        Vector4? backdropColor = null)
    {
        var (overlay, spinner) = CreateLoadingOverlay(world, parent, spinnerConfig, backdropColor);

        // Add message text below spinner
        var messageText = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                Size = new Vector2(0, 24),
                WidthMode = UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIText
            {
                Content = message,
                Font = font,
                Color = new Vector4(1, 1, 1, 1),
                FontSize = 14,
                HorizontalAlign = TextAlignH.Center
            })
            .Build();

        world.SetParent(messageText, overlay);

        return (overlay, spinner, messageText);
    }

    #endregion

    #region ProgressBar

    /// <summary>
    /// Creates a progress bar widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for the label (if shown).</param>
    /// <param name="config">Optional progress bar configuration.</param>
    /// <returns>The created progress bar container entity.</returns>
    /// <remarks>
    /// <para>
    /// The progress bar is composed of a track and a fill entity.
    /// </para>
    /// <para>
    /// To update the progress, modify the fill entity's <see cref="UIRect.AnchorMax"/>.X value
    /// with the normalized progress (0-1).
    /// </para>
    /// </remarks>
    public static Entity CreateProgressBar(
        IWorld world,
        Entity parent,
        FontHandle font,
        ProgressBarConfig? config = null)
    {
        config ??= ProgressBarConfig.Default;

        var normalizedValue = (config.Value - config.MinValue) / (config.MaxValue - config.MinValue);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        // Create container (track)
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTrackColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create fill
        var fill = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(normalizedValue, 1),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFillColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        world.SetParent(fill, container);

        // Create label if requested
        if (config.ShowLabel)
        {
            var percentage = (int)(normalizedValue * 100);
            var labelEntity = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(UIRect.Stretch())
                .With(new UIText
                {
                    Content = $"{percentage}%",
                    Font = font,
                    Color = config.GetLabelColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(labelEntity, container);
        }

        return container;
    }

    /// <summary>
    /// Creates a progress bar widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="font">The font to use for the label (if shown).</param>
    /// <param name="config">Optional progress bar configuration.</param>
    /// <returns>The created progress bar container entity.</returns>
    public static Entity CreateProgressBar(
        IWorld world,
        Entity parent,
        string name,
        FontHandle font,
        ProgressBarConfig? config = null)
    {
        config ??= ProgressBarConfig.Default;

        var normalizedValue = (config.Value - config.MinValue) / (config.MaxValue - config.MinValue);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTrackColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var fill = world.Spawn($"{name}_Fill")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(normalizedValue, 1),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFillColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        world.SetParent(fill, container);

        if (config.ShowLabel)
        {
            var percentage = (int)(normalizedValue * 100);
            var labelEntity = world.Spawn($"{name}_Label")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(UIRect.Stretch())
                .With(new UIText
                {
                    Content = $"{percentage}%",
                    Font = font,
                    Color = config.GetLabelColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(labelEntity, container);
        }

        return container;
    }

    #endregion

    #region TabView

    /// <summary>
    /// Creates a tab view widget with a tab bar and content area.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="tabs">The tab configurations.</param>
    /// <param name="font">The font to use for tab labels.</param>
    /// <param name="config">Optional tab view configuration.</param>
    /// <returns>A tuple containing the tab view container and an array of content panel entities.</returns>
    /// <remarks>
    /// <para>
    /// The tab view is composed of:
    /// - A container with vertical layout
    /// - A tab bar with horizontal layout containing tab buttons
    /// - Content panels for each tab (only the selected one is visible)
    /// </para>
    /// <para>
    /// The selected tab index is stored in the container's UIScrollable.ScrollPosition.X as an integer.
    /// </para>
    /// </remarks>
    public static (Entity TabView, Entity[] ContentPanels) CreateTabView(
        IWorld world,
        Entity parent,
        TabConfig[] tabs,
        FontHandle font,
        TabViewConfig? config = null)
    {
        config ??= TabViewConfig.Default;

        var selectedIndex = Math.Clamp(config.SelectedIndex, 0, Math.Max(0, tabs.Length - 1));

        // Create container with tab view state
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreateTabViewRect(config))
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UITabViewState(selectedIndex))
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create tab bar
        var tabBar = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TabBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTabBarColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = config.TabSpacing
            })
            .Build();

        world.SetParent(tabBar, container);

        // Create tabs with UITabButton component for tab switching
        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;
            var tab = tabs[i];

            var tabButton = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0, 1),
                    Size = new Vector2(tab.MinWidth, config.TabBarHeight - 4),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = isActive ? config.GetActiveTabColor() : config.GetTabColor(),
                    CornerRadius = 4f,
                    Padding = UIEdges.Symmetric(tab.Padding, 0)
                })
                .With(new UIText
                {
                    Content = tab.Label,
                    Font = font,
                    Color = isActive ? config.GetActiveTabTextColor() : config.GetTabTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanFocus = true
                })
                .With(new UITabButton(i, container))
                .Build();

            world.SetParent(tabButton, tabBar);
        }

        // Create content area with clipping to prevent overflow
        var contentArea = world.Spawn()
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
                BackgroundColor = config.GetContentColor()
            })
            .WithTag<UIClipChildrenTag>()
            .Build();

        world.SetParent(contentArea, container);

        // Create content panels wrapped in ScrollViews for scrollable content
        var contentPanels = new Entity[tabs.Length];
        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;

            // Create a ScrollView for each tab panel
            var (scrollView, scrollContent) = CreateScrollView(world, Entity.Null, new ScrollViewConfig
            {
                ShowVerticalScrollbar = true,
                ShowHorizontalScrollbar = false,
                ScrollbarWidth = 8f
            });

            // Configure scroll view visibility and add tab panel component
            ref var scrollElement = ref world.Get<UIElement>(scrollView);
            scrollElement.Visible = isActive;

            // Configure scroll view to stretch
            ref var scrollRect = ref world.Get<UIRect>(scrollView);
            scrollRect.AnchorMin = Vector2.Zero;
            scrollRect.AnchorMax = Vector2.One;
            scrollRect.Pivot = Vector2.Zero;
            scrollRect.Size = Vector2.Zero;
            scrollRect.WidthMode = UISizeMode.Fill;
            scrollRect.HeightMode = UISizeMode.Fill;

            // Add tab panel component to scroll view
            world.Add(scrollView, new UITabPanel(i, container));

            // Add UIHiddenTag to non-selected panels so layout system skips them
            if (!isActive)
            {
                world.Add(scrollView, new UIHiddenTag());
            }

            world.SetParent(scrollView, contentArea);

            // Return the content panel (inner container) for users to add content to
            contentPanels[i] = scrollContent;
        }

        return (container, contentPanels);
    }

    /// <summary>
    /// Creates a tab view widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="tabs">The tab configurations.</param>
    /// <param name="font">The font to use for tab labels.</param>
    /// <param name="config">Optional tab view configuration.</param>
    /// <returns>A tuple containing the tab view container and an array of content panel entities.</returns>
    public static (Entity TabView, Entity[] ContentPanels) CreateTabView(
        IWorld world,
        Entity parent,
        string name,
        TabConfig[] tabs,
        FontHandle font,
        TabViewConfig? config = null)
    {
        config ??= TabViewConfig.Default;

        var selectedIndex = Math.Clamp(config.SelectedIndex, 0, Math.Max(0, tabs.Length - 1));

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreateTabViewRect(config))
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UITabViewState(selectedIndex))
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var tabBar = world.Spawn($"{name}_TabBar")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TabBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTabBarColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = config.TabSpacing
            })
            .Build();

        world.SetParent(tabBar, container);

        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;
            var tab = tabs[i];

            var tabButton = world.Spawn($"{name}_Tab_{i}")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0, 1),
                    Size = new Vector2(tab.MinWidth, config.TabBarHeight - 4),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = isActive ? config.GetActiveTabColor() : config.GetTabColor(),
                    CornerRadius = 4f,
                    Padding = UIEdges.Symmetric(tab.Padding, 0)
                })
                .With(new UIText
                {
                    Content = tab.Label,
                    Font = font,
                    Color = isActive ? config.GetActiveTabTextColor() : config.GetTabTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanFocus = true
                })
                .With(new UITabButton(i, container))
                .Build();

            world.SetParent(tabButton, tabBar);
        }

        // Create content area with clipping to prevent overflow
        var contentArea = world.Spawn($"{name}_Content")
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
                BackgroundColor = config.GetContentColor()
            })
            .WithTag<UIClipChildrenTag>()
            .Build();

        world.SetParent(contentArea, container);

        // Create content panels wrapped in ScrollViews for scrollable content
        var contentPanels = new Entity[tabs.Length];
        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;

            // Create a ScrollView for each tab panel
            var (scrollView, scrollContent) = CreateScrollView(world, Entity.Null, new ScrollViewConfig
            {
                ShowVerticalScrollbar = true,
                ShowHorizontalScrollbar = false,
                ScrollbarWidth = 8f
            });

            // Configure scroll view visibility
            ref var scrollElement = ref world.Get<UIElement>(scrollView);
            scrollElement.Visible = isActive;

            // Configure scroll view to stretch
            ref var scrollRect = ref world.Get<UIRect>(scrollView);
            scrollRect.AnchorMin = Vector2.Zero;
            scrollRect.AnchorMax = Vector2.One;
            scrollRect.Pivot = Vector2.Zero;
            scrollRect.Size = Vector2.Zero;
            scrollRect.WidthMode = UISizeMode.Fill;
            scrollRect.HeightMode = UISizeMode.Fill;

            // Add tab panel component to scroll view
            world.Add(scrollView, new UITabPanel(i, container));

            // Add UIHiddenTag to non-selected panels so layout system skips them
            if (!isActive)
            {
                world.Add(scrollView, new UIHiddenTag());
            }

            world.SetParent(scrollView, contentArea);

            // Return the content panel (inner container) for users to add content to
            contentPanels[i] = scrollContent;
        }

        return (container, contentPanels);
    }

    private static UIRect CreateTabViewRect(TabViewConfig config)
    {
        if (config.Width.HasValue && config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width.Value, config.Height.Value),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            };
        }
        else if (config.Width.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width.Value, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            };
        }
        else if (config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.Height.Value),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            };
        }
        else
        {
            return UIRect.Stretch();
        }
    }

    #endregion

    #region ScrollView

    /// <summary>
    /// Creates a scroll view widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional scroll view configuration.</param>
    /// <returns>A tuple containing the scroll view container and the content panel entity.</returns>
    /// <remarks>
    /// <para>
    /// The scroll view is composed of:
    /// - A viewport that clips content
    /// - A content panel that can be larger than the viewport
    /// - Optional scrollbars
    /// </para>
    /// <para>
    /// Add children to the returned content panel entity.
    /// The scroll position is stored in the container's <see cref="UIScrollable"/> component.
    /// </para>
    /// </remarks>
    public static (Entity ScrollView, Entity ContentPanel) CreateScrollView(
        IWorld world,
        Entity parent,
        ScrollViewConfig? config = null)
    {
        config ??= ScrollViewConfig.Default;

        // Create container with clipping enabled
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(CreateScrollViewRect(config))
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UIScrollable
            {
                ScrollPosition = Vector2.Zero,
                ContentSize = new Vector2(
                    config.ContentWidth ?? 0,
                    config.ContentHeight ?? 0),
                HorizontalScroll = config.ShowHorizontalScrollbar,
                VerticalScroll = config.ShowVerticalScrollbar
            })
            .WithTag<UIClipChildrenTag>()
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create content panel
        var contentWidth = config.ContentWidth ?? 0;
        var contentHeight = config.ContentHeight ?? 0;

        // Use stretching anchors when Fill mode is needed, otherwise point anchors for fixed size
        var useHorizontalStretch = !config.ContentWidth.HasValue;
        var useVerticalStretch = !config.ContentHeight.HasValue;

        var contentPanel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(useHorizontalStretch ? 1 : 0, useVerticalStretch ? 1 : 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(contentWidth, contentHeight),
                WidthMode = config.ContentWidth.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.ContentHeight.HasValue ? UISizeMode.Fixed : UISizeMode.FitContent
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, container);

        // Create vertical scrollbar if enabled
        if (config.ShowVerticalScrollbar)
        {
            var vScrollbar = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = new Vector2(1, 0),
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(1, 0),
                    Size = new Vector2(config.ScrollbarWidth, 0),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(vScrollbar, container);

            // Create thumb
            var vThumb = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0.3f), // Initial size, will be updated by system
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .With(new UIScrollbarThumb(container, isVertical: true))
                .Build();

            world.SetParent(vThumb, vScrollbar);
        }

        // Create horizontal scrollbar if enabled
        if (config.ShowHorizontalScrollbar)
        {
            var hScrollbar = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0),
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.ScrollbarWidth),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(hScrollbar, container);

            var hThumb = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(0.3f, 1), // Initial size, will be updated by system
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .With(new UIScrollbarThumb(container, isVertical: false))
                .Build();

            world.SetParent(hThumb, hScrollbar);
        }

        return (container, contentPanel);
    }

    /// <summary>
    /// Creates a scroll view widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional scroll view configuration.</param>
    /// <returns>A tuple containing the scroll view container and the content panel entity.</returns>
    public static (Entity ScrollView, Entity ContentPanel) CreateScrollView(
        IWorld world,
        Entity parent,
        string name,
        ScrollViewConfig? config = null)
    {
        config ??= ScrollViewConfig.Default;

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(CreateScrollViewRect(config))
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UIScrollable
            {
                ScrollPosition = Vector2.Zero,
                ContentSize = new Vector2(
                    config.ContentWidth ?? 0,
                    config.ContentHeight ?? 0),
                HorizontalScroll = config.ShowHorizontalScrollbar,
                VerticalScroll = config.ShowVerticalScrollbar
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var contentWidth = config.ContentWidth ?? 0;
        var contentHeight = config.ContentHeight ?? 0;

        // Use stretching anchors when Fill mode is needed, otherwise point anchors for fixed size
        var useHorizontalStretch = !config.ContentWidth.HasValue;
        var useVerticalStretch = !config.ContentHeight.HasValue;

        var contentPanel = world.Spawn($"{name}_Content")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(useHorizontalStretch ? 1 : 0, useVerticalStretch ? 1 : 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(contentWidth, contentHeight),
                WidthMode = config.ContentWidth.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.ContentHeight.HasValue ? UISizeMode.Fixed : UISizeMode.FitContent
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, container);

        if (config.ShowVerticalScrollbar)
        {
            var vScrollbar = world.Spawn($"{name}_VScrollbar")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = new Vector2(1, 0),
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(1, 0),
                    Size = new Vector2(config.ScrollbarWidth, 0),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(vScrollbar, container);

            var vThumb = world.Spawn($"{name}_VThumb")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0.3f),
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .With(new UIScrollbarThumb(container, isVertical: true))
                .Build();

            world.SetParent(vThumb, vScrollbar);
        }

        if (config.ShowHorizontalScrollbar)
        {
            var hScrollbar = world.Spawn($"{name}_HScrollbar")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0),
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.ScrollbarWidth),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(hScrollbar, container);

            var hThumb = world.Spawn($"{name}_HThumb")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(0.3f, 1),
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .With(new UIScrollbarThumb(container, isVertical: false))
                .Build();

            world.SetParent(hThumb, hScrollbar);
        }

        return (container, contentPanel);
    }

    private static UIRect CreateScrollViewRect(ScrollViewConfig config)
    {
        if (config.Width.HasValue && config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width.Value, config.Height.Value),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            };
        }
        else if (config.Width.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width.Value, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            };
        }
        else if (config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.Height.Value),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            };
        }
        else
        {
            return UIRect.Stretch();
        }
    }

    #endregion
}
