using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for toolbar and status bar UI widgets.
/// </summary>
public static partial class WidgetFactory
{
    #region Toolbar

    /// <summary>
    /// Creates a toolbar widget with the specified items.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="items">The toolbar items (buttons and separators).</param>
    /// <param name="config">Optional toolbar configuration.</param>
    /// <returns>The created toolbar entity.</returns>
    /// <remarks>
    /// <para>
    /// The toolbar automatically arranges its buttons horizontally or vertically
    /// based on the orientation setting. Toggle buttons maintain their pressed state
    /// and can be grouped for radio-button behavior.
    /// </para>
    /// </remarks>
    public static Entity CreateToolbar(
        IWorld world,
        Entity parent,
        IEnumerable<ToolbarItemDef> items,
        ToolbarConfig? config = null)
    {
        return CreateToolbar(world, "Toolbar", parent, items, config);
    }

    /// <summary>
    /// Creates a named toolbar widget with the specified items.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="items">The toolbar items (buttons and separators).</param>
    /// <param name="config">Optional toolbar configuration.</param>
    /// <returns>The created toolbar entity.</returns>
    public static Entity CreateToolbar(
        IWorld world,
        string name,
        Entity parent,
        IEnumerable<ToolbarItemDef> items,
        ToolbarConfig? config = null)
    {
        config ??= ToolbarConfig.Default;

        var isHorizontal = config.Orientation == LayoutDirection.Horizontal;

        var toolbar = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = isHorizontal ? new Vector2(1, 0) : new Vector2(0, 1),
                Pivot = Vector2.Zero,
                Size = new Vector2(
                    isHorizontal ? 0 : config.ButtonSize + config.GetPadding().HorizontalSize,
                    isHorizontal ? config.ButtonSize + config.GetPadding().VerticalSize : 0),
                WidthMode = isHorizontal ? UISizeMode.Fill : UISizeMode.Fixed,
                HeightMode = isHorizontal ? UISizeMode.Fixed : UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                Padding = config.GetPadding()
            })
            .With(new UILayout
            {
                Direction = config.Orientation,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = config.Spacing
            })
            .With(new UIToolbar(config.Orientation))
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(toolbar, parent);
        }

        int buttonIndex = 0;
        int itemIndex = 0;

        foreach (var item in items)
        {
            switch (item)
            {
                case ToolbarItemDef.Button buttonItem:
                    CreateToolbarButton(world, toolbar, buttonItem.Definition, buttonIndex++, itemIndex, config);
                    break;

                case ToolbarItemDef.Separator:
                    CreateToolbarSeparator(world, toolbar, itemIndex, config);
                    break;
            }
            itemIndex++;
        }

        // Update button count
        ref var toolbarData = ref world.Get<UIToolbar>(toolbar);
        toolbarData.ButtonCount = buttonIndex;

        return toolbar;
    }

    private static Entity CreateToolbarButton(
        IWorld world,
        Entity toolbar,
        ToolbarButtonDef def,
        int buttonIndex,
        int itemIndex,
        ToolbarConfig config)
    {
        var buttonBuilder = world.Spawn($"ToolbarButton_{buttonIndex}")
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.ButtonSize, config.ButtonSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetButtonColor(),
                CornerRadius = 4f
            })
            .With(UIInteractable.Clickable())
            .With(new UIToolbarButton(toolbar)
            {
                IsToggle = def.IsToggle,
                IsPressed = false,
                TooltipText = def.Tooltip,
                Index = buttonIndex
            });

        var button = buttonBuilder.Build();

        // Add disabled tag if not enabled
        if (!def.IsEnabled)
        {
            world.Add(button, new UIDisabledTag());
        }

        world.SetParent(button, toolbar);

        // Add tooltip if specified
        if (!string.IsNullOrEmpty(def.Tooltip))
        {
            AddTooltip(world, button, def.Tooltip, new TooltipConfig(Delay: 0.3f));
        }

        // Add icon if specified
        if (def.Icon.IsValid)
        {
            var icon = world.Spawn($"ToolbarButton_{buttonIndex}_Icon")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Offset = UIEdges.All(4),
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle())
                .With(new UIImage { Texture = def.Icon, Tint = Vector4.One })
                .Build();

            world.SetParent(icon, button);
        }

        return button;
    }

    private static Entity CreateToolbarSeparator(
        IWorld world,
        Entity toolbar,
        int itemIndex,
        ToolbarConfig config)
    {
        var isHorizontal = config.Orientation == LayoutDirection.Horizontal;

        var separator = world.Spawn($"ToolbarSeparator_{itemIndex}")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = isHorizontal
                    ? new Vector2(config.SeparatorWidth + 6, config.ButtonSize * 0.6f)
                    : new Vector2(config.ButtonSize * 0.6f, config.SeparatorWidth + 6),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle())
            .With(new UIToolbarSeparator(toolbar) { Index = itemIndex })
            .Build();

        world.SetParent(separator, toolbar);

        // Create the actual separator line
        var line = world.Spawn($"ToolbarSeparator_{itemIndex}_Line")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = isHorizontal
                    ? new Vector2(config.SeparatorWidth, config.ButtonSize * 0.5f)
                    : new Vector2(config.ButtonSize * 0.5f, config.SeparatorWidth),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle { BackgroundColor = config.GetSeparatorColor() })
            .Build();

        world.SetParent(line, separator);

        return separator;
    }

    /// <summary>
    /// Sets the pressed state of a toolbar toggle button.
    /// </summary>
    /// <param name="world">The world containing the button.</param>
    /// <param name="button">The toolbar button entity.</param>
    /// <param name="pressed">Whether the button should be pressed.</param>
    public static void SetToolbarButtonPressed(IWorld world, Entity button, bool pressed)
    {
        if (!world.Has<UIToolbarButton>(button))
        {
            return;
        }

        ref var toolbarButton = ref world.Get<UIToolbarButton>(button);
        toolbarButton.IsPressed = pressed;
    }

    #endregion

    #region StatusBar

    /// <summary>
    /// Creates a status bar widget with the specified sections.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for status text.</param>
    /// <param name="sections">The section definitions.</param>
    /// <param name="config">Optional status bar configuration.</param>
    /// <returns>The created status bar entity.</returns>
    /// <remarks>
    /// <para>
    /// The status bar displays at the bottom of its parent and contains sections
    /// that can be fixed-width or flexible. Sections are separated by thin dividers.
    /// </para>
    /// </remarks>
    public static Entity CreateStatusBar(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<StatusBarSectionDef> sections,
        StatusBarConfig? config = null)
    {
        return CreateStatusBar(world, "StatusBar", parent, font, sections, config);
    }

    /// <summary>
    /// Creates a named status bar widget with the specified sections.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for status text.</param>
    /// <param name="sections">The section definitions.</param>
    /// <param name="config">Optional status bar configuration.</param>
    /// <returns>The created status bar entity.</returns>
    public static Entity CreateStatusBar(
        IWorld world,
        string name,
        Entity parent,
        FontHandle font,
        IEnumerable<StatusBarSectionDef> sections,
        StatusBarConfig? config = null)
    {
        config ??= StatusBarConfig.Default;

        var statusBar = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 1),
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 1),
                Size = new Vector2(0, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                Padding = config.GetPadding()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 0
            })
            .With(new UIStatusBar())
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(statusBar, parent);
        }

        var sectionList = sections.ToList();
        var sectionCount = sectionList.Count;

        for (int i = 0; i < sectionCount; i++)
        {
            var sectionDef = sectionList[i];
            CreateStatusBarSection(world, statusBar, font, sectionDef, i, config);

            // Add separator between sections (except after the last one)
            if (i < sectionCount - 1)
            {
                CreateStatusBarSeparator(world, statusBar, i, config);
            }
        }

        // Update section count
        ref var statusBarData = ref world.Get<UIStatusBar>(statusBar);
        statusBarData.SectionCount = sectionCount;

        return statusBar;
    }

    private static Entity CreateStatusBarSection(
        IWorld world,
        Entity statusBar,
        FontHandle font,
        StatusBarSectionDef def,
        int index,
        StatusBarConfig config)
    {
        var section = world.Spawn($"StatusBarSection_{index}")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(def.IsFlexible ? def.MinWidth : def.Width, config.Height - config.GetPadding().VerticalSize),
                WidthMode = def.IsFlexible ? UISizeMode.Fill : UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle { Padding = new UIEdges(4, 0, 4, 0) })
            .With(new UIStatusBarSection(statusBar, index)
            {
                Width = def.Width,
                IsFlexible = def.IsFlexible,
                MinWidth = def.MinWidth
            })
            .Build();

        world.SetParent(section, statusBar);

        // Create the text label
        var label = world.Spawn($"StatusBarSection_{index}_Text")
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
            .With(new UIStyle())
            .With(new UIText
            {
                Content = def.InitialText,
                Font = font,
                FontSize = config.FontSize,
                Color = config.GetTextColor(),
                HorizontalAlign = def.TextAlign,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(label, section);

        return section;
    }

    private static Entity CreateStatusBarSeparator(
        IWorld world,
        Entity statusBar,
        int afterIndex,
        StatusBarConfig config)
    {
        var separator = world.Spawn($"StatusBarSeparator_{afterIndex}")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(1, config.Height * 0.6f),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle { BackgroundColor = config.GetSeparatorColor() })
            .Build();

        world.SetParent(separator, statusBar);

        return separator;
    }

    /// <summary>
    /// Updates the text of a status bar section.
    /// </summary>
    /// <param name="world">The world containing the status bar.</param>
    /// <param name="statusBar">The status bar entity.</param>
    /// <param name="sectionIndex">The index of the section to update.</param>
    /// <param name="text">The new text to display.</param>
    public static void SetStatusBarSectionText(IWorld world, Entity statusBar, int sectionIndex, string text)
    {
        // Find the section's text label by querying children
        foreach (var entity in world.Query<UIStatusBarSection>())
        {
            ref readonly var section = ref world.Get<UIStatusBarSection>(entity);
            if (section.StatusBar == statusBar && section.Index == sectionIndex)
            {
                // Find the text child
                foreach (var child in world.GetChildren(entity))
                {
                    if (world.Has<UIText>(child))
                    {
                        ref var uiText = ref world.Get<UIText>(child);
                        uiText.Content = text;
                        return;
                    }
                }
            }
        }
    }

    #endregion
}
