using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that manages tooltip display and popover behavior.
/// </summary>
/// <remarks>
/// <para>
/// This system tracks hover state on elements with <see cref="UITooltip"/> components
/// and shows/hides tooltips after the configured delay. It also manages popover
/// open/close behavior based on their trigger type.
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase after
/// <see cref="UISplitterSystem"/>.
/// </para>
/// </remarks>
public sealed class UITooltipSystem : SystemBase
{
    private EventSubscription? pointerEnterSubscription;
    private EventSubscription? pointerExitSubscription;
    private EventSubscription? clickSubscription;

    private Entity currentHoveredElement = Entity.Null;
    private float hoverTime;
    private Vector2 hoverPosition;
    private Entity activeTooltip = Entity.Null;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        pointerEnterSubscription = World.Subscribe<UIPointerEnterEvent>(OnPointerEnter);
        pointerExitSubscription = World.Subscribe<UIPointerExitEvent>(OnPointerExit);
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        pointerEnterSubscription?.Dispose();
        pointerExitSubscription?.Dispose();
        clickSubscription?.Dispose();
        pointerEnterSubscription = null;
        pointerExitSubscription = null;
        clickSubscription = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Update hover timing for tooltip delay
        if (currentHoveredElement.IsValid && World.IsAlive(currentHoveredElement))
        {
            if (World.Has<UITooltip>(currentHoveredElement))
            {
                hoverTime += deltaTime;
                ref readonly var tooltip = ref World.Get<UITooltip>(currentHoveredElement);

                // Show tooltip after delay if not already shown
                if (hoverTime >= tooltip.Delay && !activeTooltip.IsValid)
                {
                    ShowTooltip(currentHoveredElement, tooltip);
                }
            }
        }

        // Update popover positions if needed (for follow-mouse behavior)
        // Currently not implemented - popovers stay anchored
    }

    private void OnPointerEnter(UIPointerEnterEvent e)
    {
        // Track the hovered element for tooltip timing
        if (World.Has<UITooltip>(e.Element))
        {
            currentHoveredElement = e.Element;
            hoverTime = 0f;
            hoverPosition = e.Position;
        }

        // Handle hover-triggered popovers
        if (World.Has<UIPopover>(e.Element))
        {
            ref var popover = ref World.Get<UIPopover>(e.Element);
            if (popover.Trigger == PopoverTrigger.Hover && !popover.IsOpen)
            {
                OpenPopover(e.Element, ref popover);
            }
        }
    }

    private void OnPointerExit(UIPointerExitEvent e)
    {
        // Hide tooltip when leaving the triggering element
        if (e.Element == currentHoveredElement)
        {
            HideTooltip();
            currentHoveredElement = Entity.Null;
            hoverTime = 0f;
        }

        // Handle hover-triggered popover closing
        if (World.Has<UIPopover>(e.Element))
        {
            ref var popover = ref World.Get<UIPopover>(e.Element);
            if (popover.Trigger == PopoverTrigger.Hover && popover.IsOpen)
            {
                ClosePopover(e.Element, ref popover);
            }
        }
    }

    private void OnClick(UIClickEvent e)
    {
        // Handle click-triggered popovers
        if (World.Has<UIPopover>(e.Element))
        {
            ref var popover = ref World.Get<UIPopover>(e.Element);
            if (popover.Trigger == PopoverTrigger.Click)
            {
                if (popover.IsOpen)
                {
                    ClosePopover(e.Element, ref popover);
                }
                else
                {
                    OpenPopover(e.Element, ref popover);
                }

                return;
            }
        }

        // Close popovers when clicking outside (if configured)
        ClosePopoversOnClickOutside(e.Element);
    }

    private void ShowTooltip(Entity element, in UITooltip tooltip)
    {
        // Calculate tooltip position
        Vector2 position = CalculateTooltipPosition(element, tooltip);

        // Create the tooltip entity
        activeTooltip = World.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Offset = new UIEdges(position.X, position.Y, 0, 0),
                Size = new Vector2(tooltip.MaxWidth > 0 ? tooltip.MaxWidth : 200, 30),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                LocalZIndex = 1000  // Tooltips on top
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.1f, 0.1f, 0.12f, 0.95f),
                CornerRadius = 4,
                Padding = new UIEdges(8, 6, 8, 6)
            })
            .With(new UIText
            {
                Content = tooltip.Text,
                Color = new Vector4(0.9f, 0.9f, 0.9f, 1f),
                FontSize = 13,
                WordWrap = tooltip.MaxWidth > 0
            })
            .With(new UITooltipVisibleTag())
            .Build();

        // Find a canvas to parent the tooltip to
        var canvas = FindCanvas();
        if (canvas.IsValid)
        {
            World.SetParent(activeTooltip, canvas);
        }

        // Fire event
        World.Send(new UITooltipShowEvent(element, tooltip.Text, position));
    }

    private void HideTooltip()
    {
        if (activeTooltip.IsValid && World.IsAlive(activeTooltip))
        {
            var element = currentHoveredElement;
            World.Despawn(activeTooltip);
            World.Send(new UITooltipHideEvent(element));
        }

        activeTooltip = Entity.Null;
    }

    private Vector2 CalculateTooltipPosition(Entity element, in UITooltip tooltip)
    {
        // Get element bounds
        if (!World.Has<UIRect>(element))
        {
            return hoverPosition + new Vector2(10, 10);
        }

        ref readonly var rect = ref World.Get<UIRect>(element);
        var bounds = rect.ComputedBounds;

        // Default to below the element
        float x = bounds.X;
        float y = bounds.Y + bounds.Height + 5;

        switch (tooltip.Position)
        {
            case TooltipPosition.Above:
                y = bounds.Y - 35;  // Estimate tooltip height
                break;
            case TooltipPosition.Below:
                y = bounds.Y + bounds.Height + 5;
                break;
            case TooltipPosition.Left:
                x = bounds.X - 210;  // Estimate tooltip width
                y = bounds.Y;
                break;
            case TooltipPosition.Right:
                x = bounds.X + bounds.Width + 5;
                y = bounds.Y;
                break;
            case TooltipPosition.Auto:
            default:
                // Default to below, could add screen boundary checking
                break;
        }

        return new Vector2(x, y);
    }

    private void OpenPopover(Entity popoverEntity, ref UIPopover popover)
    {
        popover.IsOpen = true;

        // Make the popover visible
        if (World.Has<UIElement>(popoverEntity))
        {
            ref var element = ref World.Get<UIElement>(popoverEntity);
            element.Visible = true;
        }

        if (World.Has<UIHiddenTag>(popoverEntity))
        {
            World.Remove<UIHiddenTag>(popoverEntity);
        }

        // Mark layout dirty
        if (!World.Has<UILayoutDirtyTag>(popoverEntity))
        {
            World.Add(popoverEntity, new UILayoutDirtyTag());
        }

        World.Send(new UIPopoverOpenedEvent(popoverEntity, popover.TriggerElement));
    }

    private void ClosePopover(Entity popoverEntity, ref UIPopover popover)
    {
        popover.IsOpen = false;

        // Hide the popover
        if (World.Has<UIElement>(popoverEntity))
        {
            ref var element = ref World.Get<UIElement>(popoverEntity);
            element.Visible = false;
        }

        if (!World.Has<UIHiddenTag>(popoverEntity))
        {
            World.Add(popoverEntity, new UIHiddenTag());
        }

        World.Send(new UIPopoverClosedEvent(popoverEntity));
    }

    private void ClosePopoversOnClickOutside(Entity clickedElement)
    {
        // Query all open popovers and close those configured to close on outside click
        foreach (var entity in World.Query<UIPopover>())
        {
            ref var popover = ref World.Get<UIPopover>(entity);
            if (popover.IsOpen && popover.CloseOnClickOutside)
            {
                // Check if click was outside this popover
                if (!IsDescendantOf(clickedElement, entity) && clickedElement != entity)
                {
                    ClosePopover(entity, ref popover);
                }
            }
        }
    }

    private bool IsDescendantOf(Entity child, Entity potentialAncestor)
    {
        var current = child;
        while (current.IsValid)
        {
            if (current == potentialAncestor)
            {
                return true;
            }

            current = World.GetParent(current);
        }

        return false;
    }

    private Entity FindCanvas()
    {
        // Find a root canvas to parent tooltips to
        foreach (var entity in World.Query<UIRootTag>())
        {
            return entity;
        }

        return Entity.Null;
    }
}
