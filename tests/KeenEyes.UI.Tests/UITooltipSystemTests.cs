using System.Numerics;

using KeenEyes.Common;
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
