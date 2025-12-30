using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIDockSystem dock panel management.
/// </summary>
public class UIDockSystemTests
{
    #region Panel Docking Tests

    [Fact]
    public void DockPanel_ToCenterZone_AddsToZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanDock = true, AllowedZones = DockZone.Center })
            .Build();

        dockSystem.DockPanel(panel, DockZone.Center, container);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
        Assert.Equal(DockZone.Center, panelState.CurrentZone);
    }

    [Fact]
    public void DockPanel_ToLeftZone_AddsToZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var leftZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.LeftZone = leftZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanDock = true, AllowedZones = DockZone.Left })
            .Build();

        dockSystem.DockPanel(panel, DockZone.Left, container);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
        Assert.Equal(DockZone.Left, panelState.CurrentZone);
    }

    [Fact]
    public void DockPanel_FiresDockedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel())
            .Build();

        bool eventFired = false;
        world.Subscribe<UIDockPanelDockedEvent>(e =>
        {
            if (e.Panel == panel)
            {
                eventFired = true;
            }
        });

        dockSystem.DockPanel(panel, DockZone.Center, container);

        Assert.True(eventFired);
    }

    #endregion

    #region Panel Floating Tests

    [Fact]
    public void FloatPanel_SetsFloatingState()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel { CanFloat = true, FloatingSize = new Vector2(300, 200) })
            .Build();

        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);
        ref readonly var rect = ref world.Get<UIRect>(panel);

        Assert.Equal(DockState.Floating, panelState.State);
        Assert.True(rect.Offset.Left.ApproximatelyEquals(100f));
        Assert.True(rect.Offset.Top.ApproximatelyEquals(100f));
    }

    [Fact]
    public void FloatPanel_NonFloatable_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanFloat = false, State = DockState.Docked })
            .Build();

        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
    }

    [Fact]
    public void FloatPanel_FiresUndockedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                State = DockState.Docked,
                CurrentZone = DockZone.Center,
                DockContainer = container,
                FloatingSize = new Vector2(300, 200)
            })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIDockPanelUndockedEvent>(e =>
        {
            if (e.Panel == panel)
            {
                eventFired = true;
            }
        });

        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        Assert.True(eventFired);
    }

    #endregion

    #region Zone Detection Tests

    [Fact]
    public void ZoneDetection_LeftEdge_ReturnsLeftZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new System.Numerics.Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(100, 300));
        world.Send(dragEvent);

        dockSystem.Update(0);
    }

    [Fact]
    public void ZoneDetection_RightEdge_ReturnsRightZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new System.Numerics.Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(700, 300));
        world.Send(dragEvent);

        dockSystem.Update(0);
    }

    [Fact]
    public void ZoneDetection_Center_ReturnsCenterZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new System.Numerics.Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(400, 300));
        world.Send(dragEvent);

        dockSystem.Update(0);
    }

    #endregion

    #region Tab Selection Tests

    [Fact]
    public void SelectTab_UpdatesSelectedIndex()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 3, SelectedIndex = 0 })
            .Build();

        dockSystem.SelectTab(tabGroup, 1);

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);

        Assert.Equal(1, tabGroupState.SelectedIndex);
    }

    [Fact]
    public void SelectTab_ShowsSelectedPanel()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 2, SelectedIndex = 0 })
            .Build();

        var panel0 = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var panel1 = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIHiddenTag())
            .Build();

        var tab0 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTab(panel0, tabGroup) { Index = 0 })
            .Build();

        var tab1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTab(panel1, tabGroup) { Index = 1 })
            .Build();

        dockSystem.SelectTab(tabGroup, 1);

        ref readonly var panel0Element = ref world.Get<UIElement>(panel0);
        ref readonly var panel1Element = ref world.Get<UIElement>(panel1);

        Assert.False(panel0Element.Visible);
        Assert.True(panel1Element.Visible);
    }

    [Fact]
    public void SelectTab_FiresTabChangedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 3, SelectedIndex = 0 })
            .Build();

        bool eventFired = false;
        int oldIndex = -1;
        int newIndex = -1;
        world.Subscribe<UIDockTabChangedEvent>(e =>
        {
            if (e.TabGroup == tabGroup)
            {
                eventFired = true;
                oldIndex = e.PreviousIndex;
                newIndex = e.NewIndex;
            }
        });

        dockSystem.SelectTab(tabGroup, 1);

        Assert.True(eventFired);
        Assert.Equal(0, oldIndex);
        Assert.Equal(1, newIndex);
    }

    [Fact]
    public void TabClick_SelectsTab()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 2, SelectedIndex = 0 })
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var tab = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTab(panel, tabGroup) { Index = 1 })
            .Build();

        var clickEvent = new UIClickEvent(tab, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        dockSystem.Update(0);

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);

        Assert.Equal(1, tabGroupState.SelectedIndex);
    }

    #endregion

    #region Drag Tests

    [Fact]
    public void DragStart_OnDockPanel_MarksDragging()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanFloat = true, DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragStartEvent = new UIDragStartEvent(titleBar, new Vector2(100, 50));
        world.Send(dragStartEvent);

        dockSystem.Update(0);

        Assert.True(world.Has<UIDockDraggingTag>(panel));
    }

    [Fact]
    public void DragEnd_OnDockPanel_RemovesDraggingTag()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanFloat = true, DockContainer = container })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(200, 150));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        Assert.False(world.Has<UIDockDraggingTag>(panel));
    }

    #endregion

    #region Request Event Tests

    [Fact]
    public void DockRequest_DocksPanel()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel())
            .Build();

        var dockRequest = new UIDockRequestEvent(panel, DockZone.Center, container);
        world.Send(dockRequest);

        dockSystem.Update(0);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
    }

    [Fact]
    public void FloatRequest_FloatsPanel()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel { CanFloat = true, FloatingSize = new Vector2(300, 200) })
            .Build();

        var floatRequest = new UIFloatRequestEvent(panel, new Vector2(100, 100));
        world.Send(floatRequest);

        dockSystem.Update(0);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Floating, panelState.State);
    }

    #endregion

    #region State Change Event Tests

    [Fact]
    public void DockPanel_FiresStateChangedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { State = DockState.Floating })
            .Build();

        bool eventFired = false;
        DockState oldState = DockState.Floating;
        DockState newState = DockState.Floating;
        world.Subscribe<UIDockStateChangedEvent>(e =>
        {
            if (e.Panel == panel)
            {
                eventFired = true;
                oldState = e.OldState;
                newState = e.NewState;
            }
        });

        dockSystem.DockPanel(panel, DockZone.Center, container);

        Assert.True(eventFired);
        Assert.Equal(DockState.Floating, oldState);
        Assert.Equal(DockState.Docked, newState);
    }

    #endregion

    #region Additional Zone Detection Tests

    [Fact]
    public void ZoneDetection_TopEdge_ReturnsTopZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // Position at top edge (Y near 0)
        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(400, 50));
        world.Send(dragEvent);

        dockSystem.Update(0);
    }

    [Fact]
    public void ZoneDetection_BottomEdge_ReturnsBottomZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // Position at bottom edge (Y near 600)
        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(400, 550));
        world.Send(dragEvent);

        dockSystem.Update(0);
    }

    [Fact]
    public void ZoneDetection_OutsideBounds_ReturnsNone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // Position outside container bounds
        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(-100, 300));
        world.Send(dragEvent);

        dockSystem.Update(0);
    }

    #endregion

    #region Drag Floating Panel Tests

    [Fact]
    public void Drag_FloatingPanel_MovesPanel()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                State = DockState.Floating,
                DockContainer = container
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // Drag with delta
        var dragEvent = new UIDragEvent(titleBar, new Vector2(100, 100), new Vector2(50, 30));
        world.Send(dragEvent);

        dockSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.True(rect.Offset.Left.ApproximatelyEquals(150f));
        Assert.True(rect.Offset.Top.ApproximatelyEquals(130f));
    }

    [Fact]
    public void DragStart_NonFloatableDockedPanel_DoesNotMarkDragging()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel
            {
                CanFloat = false,
                State = DockState.Docked,
                DockContainer = container
            })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragStartEvent = new UIDragStartEvent(titleBar, new Vector2(100, 50));
        world.Send(dragStartEvent);

        dockSystem.Update(0);

        Assert.False(world.Has<UIDockDraggingTag>(panel));
    }

    [Fact]
    public void DragStart_NonTitleBarElement_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        var dragStartEvent = new UIDragStartEvent(button, new Vector2(100, 50));
        world.Send(dragStartEvent);

        dockSystem.Update(0);

        // No exception thrown - test passes
    }

    [Fact]
    public void DragEnd_DocksToTargetZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var leftZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.LeftZone = leftZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel
            {
                CanDock = true,
                CanFloat = true,
                AllowedZones = DockZone.Left | DockZone.Right,
                State = DockState.Floating,
                DockContainer = container
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // End drag at left edge (should dock to left zone)
        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(50, 300));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Docked, panelState.State);
        Assert.Equal(DockZone.Left, panelState.CurrentZone);
    }

    [Fact]
    public void DragEnd_FloatsWhenOutsideZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                Offset = new UIEdges(0, 0, 0, 0),
                Size = new Vector2(800, 600),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIDockPanel
            {
                CanDock = true,
                CanFloat = true,
                AllowedZones = DockZone.Center,
                State = DockState.Docked,
                CurrentZone = DockZone.Center,
                DockContainer = container,
                FloatingSize = new Vector2(300, 200)
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // End drag outside container bounds
        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(-100, 300));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelState.State);
    }

    #endregion

    #region DockPanel Edge Cases

    [Fact]
    public void DockPanel_InvalidPanelEntity_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockContainer())
            .Build();

        // Should not throw when passing invalid panel
        dockSystem.DockPanel(Entity.Null, DockZone.Center, container);
    }

    [Fact]
    public void DockPanel_InvalidContainerEntity_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel())
            .Build();

        // Should not throw when passing invalid container
        dockSystem.DockPanel(panel, DockZone.Center, Entity.Null);
    }

    [Fact]
    public void DockPanel_ToRightZone_AddsToZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var rightZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.RightZone = rightZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanDock = true, AllowedZones = DockZone.Right })
            .Build();

        dockSystem.DockPanel(panel, DockZone.Right, container);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
        Assert.Equal(DockZone.Right, panelState.CurrentZone);
    }

    [Fact]
    public void DockPanel_ToTopZone_AddsToZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var topZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.TopZone = topZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanDock = true, AllowedZones = DockZone.Top })
            .Build();

        dockSystem.DockPanel(panel, DockZone.Top, container);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
        Assert.Equal(DockZone.Top, panelState.CurrentZone);
    }

    [Fact]
    public void DockPanel_ToBottomZone_AddsToZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var bottomZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.BottomZone = bottomZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanDock = true, AllowedZones = DockZone.Bottom })
            .Build();

        dockSystem.DockPanel(panel, DockZone.Bottom, container);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);

        Assert.Equal(DockState.Docked, panelState.State);
        Assert.Equal(DockZone.Bottom, panelState.CurrentZone);
    }

    [Fact]
    public void DockPanel_MoveBetweenZones_UpdatesZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var leftZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        var rightZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.LeftZone = leftZone;
        containerState.RightZone = rightZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanDock = true, AllowedZones = DockZone.Left | DockZone.Right })
            .Build();

        // Dock to left
        dockSystem.DockPanel(panel, DockZone.Left, container);

        ref var panelState = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockZone.Left, panelState.CurrentZone);

        // Move to right
        dockSystem.DockPanel(panel, DockZone.Right, container);

        Assert.Equal(DockZone.Right, panelState.CurrentZone);
    }

    #endregion

    #region Tab Group with Multiple Panels

    [Fact]
    public void DockPanel_ToZoneWithTabGroup_CreatesTab()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 0, SelectedIndex = -1 })
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone { TabGroup = tabGroup })
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { Title = "Panel 1" })
            .Build();

        dockSystem.DockPanel(panel, DockZone.Center, container);

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);
        Assert.Equal(1, tabGroupState.TabCount);
    }

    [Fact]
    public void DockPanel_MultipleToZoneWithTabGroup_CreatesTabs()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 0, SelectedIndex = -1 })
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone { TabGroup = tabGroup })
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { Title = "Panel 1" })
            .Build();

        var panel2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { Title = "Panel 2" })
            .Build();

        dockSystem.DockPanel(panel1, DockZone.Center, container);
        dockSystem.DockPanel(panel2, DockZone.Center, container);

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);
        Assert.Equal(2, tabGroupState.TabCount);
    }

    [Fact]
    public void SelectTab_InvalidIndex_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 2, SelectedIndex = 0 })
            .Build();

        // Select negative index
        dockSystem.SelectTab(tabGroup, -1);

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);
        Assert.Equal(0, tabGroupState.SelectedIndex);

        // Select beyond tab count
        dockSystem.SelectTab(tabGroup, 10);
        Assert.Equal(0, tabGroupState.SelectedIndex);
    }

    [Fact]
    public void SelectTab_SameIndex_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 3, SelectedIndex = 1 })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIDockTabChangedEvent>(e => eventFired = true);

        // Select same index
        dockSystem.SelectTab(tabGroup, 1);

        Assert.False(eventFired);
    }

    [Fact]
    public void SelectTab_InvalidTabGroup_DoesNotThrow()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        // Should not throw
        dockSystem.SelectTab(Entity.Null, 0);
    }

    #endregion

    #region FloatPanel Edge Cases

    [Fact]
    public void FloatPanel_InvalidEntity_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        // Should not throw
        dockSystem.FloatPanel(Entity.Null, new Vector2(100, 100));
    }

    [Fact]
    public void FloatPanel_RemovesHiddenTag()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel { CanFloat = true, FloatingSize = new Vector2(300, 200) })
            .With(new UIHiddenTag())
            .Build();

        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        Assert.False(world.Has<UIHiddenTag>(panel));
        ref readonly var element = ref world.Get<UIElement>(panel);
        Assert.True(element.Visible);
    }

    [Fact]
    public void FloatPanel_FromDocked_RemovesFromZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 0, SelectedIndex = -1 })
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone { TabGroup = tabGroup })
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                Title = "Panel",
                FloatingSize = new Vector2(300, 200)
            })
            .Build();

        // Dock first
        dockSystem.DockPanel(panel, DockZone.Center, container);

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);
        Assert.Equal(1, tabGroupState.TabCount);

        // Then float
        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        Assert.Equal(0, tabGroupState.TabCount);
        Assert.Equal(DockState.Floating, world.Get<UIDockPanel>(panel).State);
    }

    #endregion

    #region Click Event Tests

    [Fact]
    public void Click_NonDockTabElement_IsIgnored()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        var clickEvent = new UIClickEvent(button, new Vector2(50, 20), MouseButton.Left);
        world.Send(clickEvent);

        dockSystem.Update(0);

        // Should not throw
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromAllEvents()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        dockSystem.Dispose();

        // Should not throw when sending events after dispose
        world.Send(new UIDockRequestEvent(Entity.Null, DockZone.Center, Entity.Null));
        world.Send(new UIFloatRequestEvent(Entity.Null, Vector2.Zero));
    }

    #endregion

    #region Preview Overlay Tests

    [Fact]
    public void ShowPreviewOverlay_InvalidContainer_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        // Should not throw
        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { CanFloat = true, DockContainer = Entity.Null })
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragStartEvent = new UIDragStartEvent(titleBar, new Vector2(100, 50));
        world.Send(dragStartEvent);

        dockSystem.Update(0);
    }

    [Fact]
    public void DragEnd_HidesPreviewOverlay()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var previewOverlay = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer { PreviewOverlay = previewOverlay })
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel
            {
                CanFloat = true,
                DockContainer = container
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(100, 100));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        ref readonly var element = ref world.Get<UIElement>(previewOverlay);
        Assert.False(element.Visible);
    }

    [Fact]
    public void UpdatePreviewOverlay_PositionsForLeftZone()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var previewOverlay = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer { PreviewOverlay = previewOverlay })
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { DockContainer = container })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // Drag to left edge
        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(50, 300));
        world.Send(dragEvent);

        dockSystem.Update(0);

        ref readonly var previewRect = ref world.Get<UIRect>(previewOverlay);
        Assert.True(previewRect.Size.X.ApproximatelyEquals(400f)); // Half width
        Assert.True(previewRect.Size.Y.ApproximatelyEquals(600f)); // Full height
    }

    #endregion

    #region FloatPanel State Change Events

    [Fact]
    public void FloatPanel_FiresStateChangedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                State = DockState.Docked,
                CurrentZone = DockZone.None,
                FloatingSize = new Vector2(300, 200)
            })
            .Build();

        bool eventFired = false;
        DockState oldState = DockState.Docked;
        DockState newState = DockState.Docked;
        world.Subscribe<UIDockStateChangedEvent>(e =>
        {
            if (e.Panel == panel)
            {
                eventFired = true;
                oldState = e.OldState;
                newState = e.NewState;
            }
        });

        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        Assert.True(eventFired);
        Assert.Equal(DockState.Docked, oldState);
        Assert.Equal(DockState.Floating, newState);
    }

    [Fact]
    public void FloatPanel_AlreadyFloating_DoesNotFireStateChangedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                State = DockState.Floating,
                FloatingSize = new Vector2(300, 200)
            })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIDockStateChangedEvent>(e => eventFired = true);

        dockSystem.FloatPanel(panel, new Vector2(100, 100));

        Assert.False(eventFired);
    }

    #endregion

    #region Remove Panel From Zone Tests

    [Fact]
    public void RemovePanel_AdjustsSelectedIndexWhenNeeded()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 0, SelectedIndex = -1 })
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone { TabGroup = tabGroup })
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel1 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                Title = "Panel 1",
                FloatingSize = new Vector2(300, 200)
            })
            .Build();

        var panel2 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 300, 200))
            .With(new UIDockPanel
            {
                CanFloat = true,
                Title = "Panel 2",
                FloatingSize = new Vector2(300, 200)
            })
            .Build();

        // Dock both panels
        dockSystem.DockPanel(panel1, DockZone.Center, container);
        dockSystem.DockPanel(panel2, DockZone.Center, container);

        // Select the second tab (index 1)
        dockSystem.SelectTab(tabGroup, 1);

        // Float panel 2 (removes from zone)
        dockSystem.FloatPanel(panel2, new Vector2(100, 100));

        ref readonly var tabGroupState = ref world.Get<UIDockTabGroup>(tabGroup);
        Assert.Equal(1, tabGroupState.TabCount);
        Assert.Equal(0, tabGroupState.SelectedIndex); // Should adjust to valid index
    }

    #endregion

    #region Drag Non-Dock Element Tests

    [Fact]
    public void Drag_TitleBarWithNonDockPanel_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var regularWindow = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(regularWindow))
            .Build();

        var dragStartEvent = new UIDragStartEvent(titleBar, new Vector2(100, 50));
        world.Send(dragStartEvent);

        dockSystem.Update(0);

        // Should not add dragging tag to non-dock panel
        Assert.False(world.Has<UIDockDraggingTag>(regularWindow));
    }

    [Fact]
    public void DragEnd_TitleBarWithNonDockPanel_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var regularWindow = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(regularWindow))
            .Build();

        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(200, 150));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        // Should not throw
    }

    [Fact]
    public void Drag_NonTitleBar_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        var dragEvent = new UIDragEvent(button, new Vector2(0, 0), new Vector2(100, 100));
        world.Send(dragEvent);

        dockSystem.Update(0);

        // Should not throw
    }

    [Fact]
    public void DragEnd_NonTitleBar_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        var dragEndEvent = new UIDragEndEvent(button, new Vector2(200, 150));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        // Should not throw
    }

    #endregion

    #region DragEnd Zone Validation Tests

    [Fact]
    public void DragEnd_ToNotAllowedZone_DoesNotDock()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var leftZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.LeftZone = leftZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIDockPanel
            {
                CanDock = true,
                CanFloat = true,
                AllowedZones = DockZone.Right, // Only allow right zone
                State = DockState.Floating,
                DockContainer = container,
                FloatingSize = new Vector2(300, 200)
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // End drag at left edge (not in AllowedZones)
        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(50, 300));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelState.State); // Still floating
    }

    [Fact]
    public void DragEnd_CannotDock_StaysFloating()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(0, 0, 800, 600)
            })
            .With(new UIDockContainer())
            .Build();

        var leftZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.LeftZone = leftZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIDockPanel
            {
                CanDock = false, // Cannot dock
                CanFloat = true,
                AllowedZones = DockZone.Left,
                State = DockState.Floating,
                DockContainer = container,
                FloatingSize = new Vector2(300, 200)
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        // End drag at left edge
        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(50, 300));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        ref readonly var panelState = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelState.State); // Still floating
    }

    #endregion

    #region DockPanel Already Docked Tests

    [Fact]
    public void DockPanel_AlreadyDocked_DoesNotFireStateChangedEvent()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var centerZone = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockZone())
            .Build();

        ref var containerState = ref world.Get<UIDockContainer>(container);
        containerState.CenterZone = centerZone;

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel { State = DockState.Docked })
            .Build();

        // Dock first time
        dockSystem.DockPanel(panel, DockZone.Center, container);

        bool eventFired = false;
        world.Subscribe<UIDockStateChangedEvent>(e => eventFired = true);

        // Dock again to same zone
        dockSystem.DockPanel(panel, DockZone.Center, container);

        Assert.False(eventFired); // Should not fire since state didn't change
    }

    [Fact]
    public void DockPanel_ToNoneZone_DoesNothing()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel())
            .Build();

        bool eventFired = false;
        world.Subscribe<UIDockPanelDockedEvent>(e => eventFired = true);

        // DockZone.None should result in no docking
        dockSystem.DockPanel(panel, DockZone.None, container);

        Assert.False(eventFired); // Zone is None, so docking fails
    }

    #endregion

    #region Drag Without Container Tests

    [Fact]
    public void Drag_WithoutValidContainer_DoesNotUpdatePreview()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel
            {
                DockContainer = Entity.Null // No container
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEvent = new UIDragEvent(titleBar, new Vector2(0, 0), new Vector2(100, 100));
        world.Send(dragEvent);

        dockSystem.Update(0);

        // Should not throw
    }

    [Fact]
    public void DragEnd_WithoutValidContainer_DoesNotDock()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var panel = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockPanel
            {
                CanFloat = true,
                State = DockState.Floating,
                DockContainer = Entity.Null // No container
            })
            .With(new UIDockDraggingTag())
            .Build();

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(panel))
            .Build();

        var dragEndEvent = new UIDragEndEvent(titleBar, new Vector2(100, 100));
        world.Send(dragEndEvent);

        dockSystem.Update(0);

        // Should still remove dragging tag
        Assert.False(world.Has<UIDockDraggingTag>(panel));
    }

    #endregion

    #region Hidden Tag Management Tests

    [Fact]
    public void SelectTab_HidesDeselectedPanel()
    {
        using var world = new World();
        var dockSystem = new UIDockSystem();
        world.AddSystem(dockSystem);

        var tabGroup = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTabGroup { TabCount = 2, SelectedIndex = 0 })
            .Build();

        var panel0 = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var panel1 = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIHiddenTag())
            .Build();

        var tab0 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTab(panel0, tabGroup) { Index = 0 })
            .Build();

        var tab1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIDockTab(panel1, tabGroup) { Index = 1 })
            .Build();

        dockSystem.SelectTab(tabGroup, 1);

        // Panel 0 should now be hidden
        Assert.True(world.Has<UIHiddenTag>(panel0));
        Assert.False(world.Get<UIElement>(panel0).Visible);

        // Panel 1 should now be visible
        Assert.False(world.Has<UIHiddenTag>(panel1));
        Assert.True(world.Get<UIElement>(panel1).Visible);
    }

    #endregion
}
