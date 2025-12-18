using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for overlay UI widgets: Tooltip, Popover.
/// </summary>
public static partial class WidgetFactory
{
    #region Tooltip

    /// <summary>
    /// Adds a tooltip to an existing UI element.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="element">The element to add the tooltip to.</param>
    /// <param name="text">The tooltip text to display.</param>
    /// <param name="config">Optional tooltip configuration.</param>
    /// <remarks>
    /// <para>
    /// This method adds a <see cref="UITooltip"/> component to the specified element.
    /// The tooltip will be displayed automatically by the UITooltipSystem when the
    /// user hovers over the element.
    /// </para>
    /// </remarks>
    public static void AddTooltip(
        IWorld world,
        Entity element,
        string text,
        TooltipConfig? config = null)
    {
        config ??= TooltipConfig.Default;

        world.Add(element, new UITooltip(text)
        {
            Delay = config.Delay,
            MaxWidth = config.MaxWidth,
            Position = config.Position,
            FollowMouse = config.FollowMouse
        });
    }

    /// <summary>
    /// Removes a tooltip from an existing UI element.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="element">The element to remove the tooltip from.</param>
    public static void RemoveTooltip(IWorld world, Entity element)
    {
        if (world.Has<UITooltip>(element))
        {
            world.Remove<UITooltip>(element);
        }
    }

    #endregion

    #region Popover

    /// <summary>
    /// Creates a popover widget attached to a trigger element.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="trigger">The element that triggers the popover.</param>
    /// <param name="config">Optional popover configuration.</param>
    /// <returns>The created popover container entity. Add content as children.</returns>
    /// <remarks>
    /// <para>
    /// The popover is initially hidden (<see cref="UIPopover.IsOpen"/> = false).
    /// It will be shown/hidden automatically by the UIPopoverSystem based on the
    /// trigger type (click, hover, or manual).
    /// </para>
    /// <para>
    /// Add UI elements as children of the returned entity to populate the popover content.
    /// </para>
    /// </remarks>
    public static Entity CreatePopover(
        IWorld world,
        Entity trigger,
        PopoverConfig? config = null)
    {
        config ??= PopoverConfig.Default;

        var popover = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.CornerRadius,
                Padding = new UIEdges(8, 8, 8, 8)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .With(new UIPopover
            {
                IsOpen = false,
                TriggerElement = trigger,
                Trigger = config.Trigger,
                Position = config.Position,
                CloseOnClickOutside = config.CloseOnClickOutside,
                Offset = Vector2.Zero
            })
            .Build();

        // Popover is parented to the trigger for positioning
        if (trigger.IsValid)
        {
            world.SetParent(popover, trigger);
        }

        return popover;
    }

    /// <summary>
    /// Creates a popover widget with a name attached to a trigger element.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="trigger">The element that triggers the popover.</param>
    /// <param name="config">Optional popover configuration.</param>
    /// <returns>The created popover container entity. Add content as children.</returns>
    public static Entity CreatePopover(
        IWorld world,
        string name,
        Entity trigger,
        PopoverConfig? config = null)
    {
        config ??= PopoverConfig.Default;

        var popover = world.Spawn(name)
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.CornerRadius,
                Padding = new UIEdges(8, 8, 8, 8)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .With(new UIPopover
            {
                IsOpen = false,
                TriggerElement = trigger,
                Trigger = config.Trigger,
                Position = config.Position,
                CloseOnClickOutside = config.CloseOnClickOutside,
                Offset = Vector2.Zero
            })
            .Build();

        if (trigger.IsValid)
        {
            world.SetParent(popover, trigger);
        }

        return popover;
    }

    /// <summary>
    /// Opens a popover programmatically.
    /// </summary>
    /// <param name="world">The world containing the popover.</param>
    /// <param name="popover">The popover entity to open.</param>
    public static void OpenPopover(IWorld world, Entity popover)
    {
        if (world.Has<UIPopover>(popover))
        {
            ref var popoverData = ref world.Get<UIPopover>(popover);
            popoverData.IsOpen = true;

            ref var element = ref world.Get<UIElement>(popover);
            element.Visible = true;
        }
    }

    /// <summary>
    /// Closes a popover programmatically.
    /// </summary>
    /// <param name="world">The world containing the popover.</param>
    /// <param name="popover">The popover entity to close.</param>
    public static void ClosePopover(IWorld world, Entity popover)
    {
        if (world.Has<UIPopover>(popover))
        {
            ref var popoverData = ref world.Get<UIPopover>(popover);
            popoverData.IsOpen = false;

            ref var element = ref world.Get<UIElement>(popover);
            element.Visible = false;
        }
    }

    #endregion
}
