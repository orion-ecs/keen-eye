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
        dockSystem.Initialize(world);

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
}
