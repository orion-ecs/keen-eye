using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UITooltipSystem tooltip and popover management.
/// </summary>
public class UITooltipSystemTests
{
    #region Tooltip Delay Tests

    [Fact]
    public void Tooltip_HoverBeforeDelay_DoesNotShow()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(element, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0.2f);

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Empty(tooltips);
    }

    [Fact]
    public void Tooltip_HoverAfterDelay_ShowsTooltip()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(element, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0.6f);

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.NotEmpty(tooltips);
    }

    [Fact]
    public void Tooltip_ExitBeforeDelay_DoesNotShow()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(element, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0.3f);

        var pointerExitEvent = new UIPointerExitEvent(element);
        world.Send(pointerExitEvent);

        tooltipSystem.Update(0.3f);

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Empty(tooltips);
    }

    [Fact]
    public void Tooltip_Exit_HidesTooltip()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(element, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0.6f);

        var pointerExitEvent = new UIPointerExitEvent(element);
        world.Send(pointerExitEvent);

        tooltipSystem.Update(0);

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Empty(tooltips);
    }

    #endregion

    #region Tooltip Event Tests

    [Fact]
    public void Tooltip_Show_FiresShowEvent()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();

        bool eventFired = false;
        string? tooltipText = null;
        world.Subscribe<UITooltipShowEvent>(e =>
        {
            if (e.Element == element)
            {
                eventFired = true;
                tooltipText = e.Text;
            }
        });

        var pointerEnterEvent = new UIPointerEnterEvent(element, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0.6f);

        Assert.True(eventFired);
        Assert.Equal("Hint", tooltipText);
    }

    [Fact]
    public void Tooltip_Hide_FiresHideEvent()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(element, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0.6f);

        bool eventFired = false;
        world.Subscribe<UITooltipHideEvent>(e =>
        {
            if (e.Element == element)
            {
                eventFired = true;
            }
        });

        var pointerExitEvent = new UIPointerExitEvent(element);
        world.Send(pointerExitEvent);

        tooltipSystem.Update(0);

        Assert.True(eventFired);
    }

    #endregion

    #region Popover Click Trigger Tests

    [Fact]
    public void Popover_ClickTrigger_OpensOnClick()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = false })
            .Build();

        var clickEvent = new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);
        ref readonly var element = ref world.Get<UIElement>(popover);

        Assert.True(popoverState.IsOpen);
        Assert.True(element.Visible);
    }

    [Fact]
    public void Popover_ClickTriggerWhenOpen_ClosesOnClick()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = true })
            .Build();

        var clickEvent = new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);
        ref readonly var element = ref world.Get<UIElement>(popover);

        Assert.False(popoverState.IsOpen);
        Assert.False(element.Visible);
    }

    #endregion

    #region Popover Hover Trigger Tests

    [Fact]
    public void Popover_HoverTrigger_OpensOnHover()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIPopover { Trigger = PopoverTrigger.Hover, IsOpen = false })
            .Build();

        var pointerEnterEvent = new UIPointerEnterEvent(popover, new Vector2(150, 125));
        world.Send(pointerEnterEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);

        Assert.True(popoverState.IsOpen);
    }

    [Fact]
    public void Popover_HoverTrigger_ClosesOnExit()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { Trigger = PopoverTrigger.Hover, IsOpen = true })
            .Build();

        var pointerExitEvent = new UIPointerExitEvent(popover);
        world.Send(pointerExitEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);

        Assert.False(popoverState.IsOpen);
    }

    #endregion

    #region Popover Close On Click Outside Tests

    [Fact]
    public void Popover_ClickOutside_ClosesPopover()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { IsOpen = true, CloseOnClickOutside = true })
            .Build();

        var outsideElement = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var clickEvent = new UIClickEvent(outsideElement, new Vector2(500, 500), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);

        Assert.False(popoverState.IsOpen);
    }

    [Fact]
    public void Popover_ClickInside_DoesNotClose()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { IsOpen = true, CloseOnClickOutside = true })
            .Build();

        var childElement = world.Spawn()
            .With(UIElement.Default)
            .Build();
        world.SetParent(childElement, popover);

        var clickEvent = new UIClickEvent(childElement, new Vector2(150, 125), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);

        Assert.True(popoverState.IsOpen);
    }

    [Fact]
    public void Popover_NoCloseOnOutside_RemainsOpen()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { IsOpen = true, CloseOnClickOutside = false })
            .Build();

        var outsideElement = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var clickEvent = new UIClickEvent(outsideElement, new Vector2(500, 500), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);

        Assert.True(popoverState.IsOpen);
    }

    #endregion

    #region Tooltip Position Tests

    [Fact]
    public void Tooltip_PositionAbove_CalculatesCorrectly()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(100, 100, 200, 50)
            })
            .With(new UITooltip { Text = "Above", Delay = 0f, Position = TooltipPosition.Above })
            .Build();
        world.SetParent(element, canvas);

        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.1f);

        var tooltip = world.Query<UITooltipVisibleTag>().FirstOrDefault();
        Assert.True(tooltip.IsValid);
    }

    [Fact]
    public void Tooltip_PositionLeft_CalculatesCorrectly()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(300, 100, 200, 50)
            })
            .With(new UITooltip { Text = "Left", Delay = 0f, Position = TooltipPosition.Left })
            .Build();
        world.SetParent(element, canvas);

        world.Send(new UIPointerEnterEvent(element, new Vector2(350, 125)));
        tooltipSystem.Update(0.1f);

        var tooltip = world.Query<UITooltipVisibleTag>().FirstOrDefault();
        Assert.True(tooltip.IsValid);
    }

    [Fact]
    public void Tooltip_PositionRight_CalculatesCorrectly()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(100, 100, 200, 50)
            })
            .With(new UITooltip { Text = "Right", Delay = 0f, Position = TooltipPosition.Right })
            .Build();
        world.SetParent(element, canvas);

        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.1f);

        var tooltip = world.Query<UITooltipVisibleTag>().FirstOrDefault();
        Assert.True(tooltip.IsValid);
    }

    [Fact]
    public void Tooltip_ElementWithoutUIRect_UsesHoverPosition()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        // Element without UIRect
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UITooltip { Text = "No Rect", Delay = 0f })
            .Build();
        world.SetParent(element, canvas);

        world.Send(new UIPointerEnterEvent(element, new Vector2(250, 175)));
        tooltipSystem.Update(0.1f);

        var tooltip = world.Query<UITooltipVisibleTag>().FirstOrDefault();
        Assert.True(tooltip.IsValid);
    }

    [Fact]
    public void Tooltip_WithMaxWidth_SetsWordWrap()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Long text tooltip", Delay = 0f, MaxWidth = 150 })
            .Build();
        world.SetParent(element, canvas);

        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.1f);

        var tooltip = world.Query<UITooltipVisibleTag>().FirstOrDefault();
        Assert.True(tooltip.IsValid);

        ref readonly var text = ref world.Get<UIText>(tooltip);
        Assert.True(text.WordWrap);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Tooltip_HoverElementWithoutTooltipComponent_DoesNothing()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            // No UITooltip component
            .Build();

        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.6f);

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Empty(tooltips);
    }

    [Fact]
    public void Tooltip_AlreadyShown_DoesNotShowAgain()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0.5f })
            .Build();
        world.SetParent(element, canvas);

        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.6f);

        // First tooltip should be shown
        var tooltipsAfterFirst = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Single(tooltipsAfterFirst);

        // Continue hovering - should not create another tooltip
        tooltipSystem.Update(0.5f);

        var tooltipsAfterSecond = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Single(tooltipsAfterSecond);
    }

    [Fact]
    public void Tooltip_NoCanvas_StillCreatesTooltip()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        // No UIRootTag canvas

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "No Canvas", Delay = 0f })
            .Build();

        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.1f);

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.NotEmpty(tooltips);
    }

    [Fact]
    public void Tooltip_Dispose_CleansUpSubscriptions()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        // Dispose should not throw
        tooltipSystem.Dispose();

        // System should be inactive after dispose
        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "After Dispose", Delay = 0f })
            .Build();

        // Events should not be processed
        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));

        var tooltips = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Empty(tooltips);
    }

    [Fact]
    public void Tooltip_ExitDifferentElement_DoesNotHideActiveTooltip()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element1 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Element 1", Delay = 0f })
            .Build();

        var element2 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 200, 200, 50))
            .Build();

        // Show tooltip for element1
        world.Send(new UIPointerEnterEvent(element1, new Vector2(150, 125)));
        tooltipSystem.Update(0.1f);

        var tooltipsBefore = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Single(tooltipsBefore);

        // Exit element2 - should not affect element1's tooltip
        world.Send(new UIPointerExitEvent(element2));
        tooltipSystem.Update(0);

        var tooltipsAfter = world.Query<UITooltipVisibleTag>().ToList();
        Assert.Single(tooltipsAfter);
    }

    [Fact]
    public void HideTooltip_ActiveTooltipDespawned_DoesNotThrow()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 50))
            .With(new UITooltip { Text = "Hint", Delay = 0f })
            .Build();

        // Show tooltip
        world.Send(new UIPointerEnterEvent(element, new Vector2(150, 125)));
        tooltipSystem.Update(0.1f);

        var tooltip = world.Query<UITooltipVisibleTag>().FirstOrDefault();
        Assert.True(tooltip.IsValid);

        // Manually despawn the tooltip
        world.Despawn(tooltip);

        // Exiting should not throw
        world.Send(new UIPointerExitEvent(element));
        tooltipSystem.Update(0);
    }

    #endregion

    #region Popover Edge Cases

    [Fact]
    public void Popover_HoverTrigger_AlreadyOpen_DoesNotReopenOnEnter()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { Trigger = PopoverTrigger.Hover, IsOpen = true })
            .Build();

        int openEventCount = 0;
        world.Subscribe<UIPopoverOpenedEvent>(e => openEventCount++);

        world.Send(new UIPointerEnterEvent(popover, new Vector2(150, 125)));
        tooltipSystem.Update(0);

        Assert.Equal(0, openEventCount); // Should not fire open event again
    }

    [Fact]
    public void Popover_HoverTrigger_NotOpen_DoesNotCloseOnExit()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIPopover { Trigger = PopoverTrigger.Hover, IsOpen = false })
            .Build();

        int closeEventCount = 0;
        world.Subscribe<UIPopoverClosedEvent>(e => closeEventCount++);

        world.Send(new UIPointerExitEvent(popover));
        tooltipSystem.Update(0);

        Assert.Equal(0, closeEventCount); // Should not fire close event
    }

    [Fact]
    public void Popover_OpenWithHiddenTag_RemovesTag()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = false })
            .With(new UIHiddenTag())
            .Build();

        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        Assert.False(world.Has<UIHiddenTag>(popover));
    }

    [Fact]
    public void Popover_Open_MarksLayoutDirty()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = false })
            .Build();

        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        Assert.True(world.Has<UILayoutDirtyTag>(popover));
    }

    [Fact]
    public void Popover_Close_AddsHiddenTag()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = true })
            .Build();

        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        Assert.True(world.Has<UIHiddenTag>(popover));
    }

    [Fact]
    public void Popover_Close_AlreadyHasHiddenTag_DoesNotDuplicate()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = true })
            .With(new UIHiddenTag()) // Already has hidden tag
            .Build();

        // Should not throw (no duplicate component)
        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        Assert.True(world.Has<UIHiddenTag>(popover));
    }

    [Fact]
    public void Popover_ClickOnSelf_DoesNotCloseOnClickOutside()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { IsOpen = true, CloseOnClickOutside = true, Trigger = PopoverTrigger.Manual })
            .Build();

        // Click on the popover itself - should not close
        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);
        Assert.True(popoverState.IsOpen);
    }

    [Fact]
    public void Popover_OpenWithoutUIElement_DoesNotThrow()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = false })
            // No UIElement
            .Build();

        // Should not throw
        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);
        Assert.True(popoverState.IsOpen);
    }

    [Fact]
    public void Popover_CloseWithoutUIElement_DoesNotThrow()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = true })
            // No UIElement
            .Build();

        // Should not throw
        world.Send(new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left));
        tooltipSystem.Update(0);

        ref readonly var popoverState = ref world.Get<UIPopover>(popover);
        Assert.False(popoverState.IsOpen);
    }

    #endregion

    #region Popover Event Tests

    [Fact]
    public void Popover_Open_FiresOpenedEvent()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = false })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIPopoverOpenedEvent>(e =>
        {
            if (e.Popover == popover)
            {
                eventFired = true;
            }
        });

        var clickEvent = new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        Assert.True(eventFired);
    }

    [Fact]
    public void Popover_Close_FiresClosedEvent()
    {
        using var world = new World();
        var tooltipSystem = new UITooltipSystem();
        world.AddSystem(tooltipSystem);

        var popover = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIPopover { Trigger = PopoverTrigger.Click, IsOpen = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIPopoverClosedEvent>(e =>
        {
            if (e.Popover == popover)
            {
                eventFired = true;
            }
        });

        var clickEvent = new UIClickEvent(popover, new Vector2(150, 125), MouseButton.Left);
        world.Send(clickEvent);

        tooltipSystem.Update(0);

        Assert.True(eventFired);
    }

    #endregion
}
