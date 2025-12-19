using System.Numerics;

using KeenEyes.Common;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIRadialMenuSystem radial menu management.
/// </summary>
public class UIRadialMenuSystemTests
{
    #region Open/Close Tests

    [Fact]
    public void RadialMenu_OpenRequest_OpensMenuAtPosition()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(new UIRadialMenu { OuterRadius = 100, SliceCount = 4 })
            .Build();

        var requestEvent = new UIRadialMenuRequestEvent(radialMenu, new Vector2(400, 300));
        world.Send(requestEvent);

        radialMenuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);
        ref readonly var element = ref world.Get<UIElement>(radialMenu);

        Assert.True(menu.IsOpen);
        Assert.True(element.Visible);
        Assert.True(menu.CenterPosition.X.ApproximatelyEquals(400f));
        Assert.True(menu.CenterPosition.Y.ApproximatelyEquals(300f));
    }

    [Fact]
    public void RadialMenu_OpenRequest_ClosesOtherMenus()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu1 = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRadialMenu { IsOpen = true })
            .Build();

        var radialMenu2 = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIRadialMenu())
            .Build();

        var requestEvent = new UIRadialMenuRequestEvent(radialMenu2, new Vector2(400, 300));
        world.Send(requestEvent);

        radialMenuSystem.Update(0);

        ref readonly var menu1 = ref world.Get<UIRadialMenu>(radialMenu1);
        ref readonly var menu2 = ref world.Get<UIRadialMenu>(radialMenu2);

        Assert.False(menu1.IsOpen);
        Assert.True(menu2.IsOpen);
    }

    [Fact]
    public void RadialMenu_Close_HidesMenu()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRadialMenu { IsOpen = true })
            .Build();

        radialMenuSystem.CloseRadialMenu(radialMenu);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);
        ref readonly var element = ref world.Get<UIElement>(radialMenu);

        Assert.False(menu.IsOpen);
        Assert.False(element.Visible);
    }

    #endregion

    #region Input Tests

    [Fact]
    public void RadialMenu_UpdateInput_StoresInputState()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu())
            .With(new UIRadialMenuInputState())
            .Build();

        radialMenuSystem.UpdateInput(radialMenu, new Vector2(1, 0), 0.8f);

        ref readonly var inputState = ref world.Get<UIRadialMenuInputState>(radialMenu);

        Assert.True(inputState.InputDirection.X.ApproximatelyEquals(1f));
        Assert.True(inputState.InputMagnitude.ApproximatelyEquals(0.8f));
    }

    [Fact]
    public void RadialMenu_InputAboveDeadZone_SelectsSlice()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SliceCount = 4, StartAngle = 0, SelectedIndex = -1 })
            .With(new UIRadialMenuInputState { InputDirection = new Vector2(1, 0), InputMagnitude = 0.8f })
            .Build();

        radialMenuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.True(menu.SelectedIndex >= 0);
    }

    [Fact]
    public void RadialMenu_InputBelowDeadZone_DeselectsSlice()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SliceCount = 4, SelectedIndex = 0 })
            .With(new UIRadialMenuInputState { InputDirection = new Vector2(0.1f, 0), InputMagnitude = 0.2f })
            .Build();

        radialMenuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.Equal(-1, menu.SelectedIndex);
    }

    #endregion

    #region Slice Selection Tests

    [Fact]
    public void RadialMenu_RightInput_SelectsRightSlice()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SliceCount = 4, StartAngle = 0, SelectedIndex = -1 })
            .With(new UIRadialMenuInputState { InputDirection = new Vector2(1, 0), InputMagnitude = 0.8f })
            .Build();

        radialMenuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.Equal(0, menu.SelectedIndex);
    }

    [Fact]
    public void RadialMenu_UpInput_SelectsTopSlice()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SliceCount = 4, StartAngle = 0 })
            .With(new UIRadialMenuInputState { InputDirection = new Vector2(0, -1), InputMagnitude = 0.8f })
            .Build();

        radialMenuSystem.Update(0);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.True(menu.SelectedIndex >= 0 && menu.SelectedIndex < 4);
    }

    [Fact]
    public void RadialMenu_SelectionChange_UpdatesSliceTags()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SliceCount = 4, StartAngle = 0, SelectedIndex = -1 })
            .With(new UIRadialMenuInputState { InputDirection = new Vector2(1, 0), InputMagnitude = 0.8f })
            .Build();

        var slice0 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialSlice(radialMenu, 0))
            .Build();

        var slice1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialSlice(radialMenu, 1))
            .Build();

        radialMenuSystem.Update(0);

        Assert.True(world.Has<UIRadialSliceSelectedTag>(slice0));
        Assert.False(world.Has<UIRadialSliceSelectedTag>(slice1));
    }

    #endregion

    #region Confirmation Tests

    [Fact]
    public void RadialMenu_ConfirmWithValidSelection_FiresSelectionEvent()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SelectedIndex = 0, SliceCount = 4 })
            .Build();

        var slice = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialSlice(radialMenu, 0) { ItemId = "action1", IsEnabled = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIRadialSliceSelectedEvent>(e =>
        {
            if (e.Slice == slice)
            {
                eventFired = true;
            }
        });

        radialMenuSystem.ConfirmSelection(radialMenu);

        Assert.True(eventFired);
    }

    [Fact]
    public void RadialMenu_ConfirmWithNoSelection_ClosesMenu()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SelectedIndex = -1 })
            .Build();

        radialMenuSystem.ConfirmSelection(radialMenu);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.False(menu.IsOpen);
    }

    [Fact]
    public void RadialMenu_ConfirmDisabledSlice_DoesNotFireEvent()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SelectedIndex = 0, SliceCount = 4 })
            .Build();

        var slice = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialSlice(radialMenu, 0) { IsEnabled = false })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIRadialSliceSelectedEvent>(e => eventFired = true);

        radialMenuSystem.ConfirmSelection(radialMenu);

        Assert.False(eventFired);
    }

    [Fact]
    public void RadialMenu_ConfirmWithSubmenu_OpensSubmenu()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var submenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu())
            .Build();

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu
            {
                IsOpen = true,
                SelectedIndex = 0,
                SliceCount = 4,
                CenterPosition = new Vector2(400, 300),
                OuterRadius = 100
            })
            .Build();

        var slice = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialSlice(radialMenu, 0)
            {
                IsEnabled = true,
                HasSubmenu = true,
                Submenu = submenu,
                StartAngle = 0,
                EndAngle = MathF.PI / 2
            })
            .Build();

        bool submenuOpenRequested = false;
        world.Subscribe<UIRadialMenuRequestEvent>(e =>
        {
            if (e.Menu == submenu)
            {
                submenuOpenRequested = true;
            }
        });

        radialMenuSystem.ConfirmSelection(radialMenu);

        Assert.True(submenuOpenRequested);
    }

    #endregion

    #region Animation Tests

    [Fact]
    public void RadialMenu_OpenAnimation_ProgressesOverTime()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, OpenProgress = 0f })
            .With(new UIRadialMenuInputState())
            .Build();

        radialMenuSystem.Update(0.1f);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.True(menu.OpenProgress > 0f);
    }

    [Fact]
    public void RadialMenu_CloseAnimation_DecreasesOverTime()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = false, OpenProgress = 1f })
            .With(new UIRadialMenuInputState())
            .Build();

        radialMenuSystem.Update(0.05f);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.True(menu.OpenProgress < 1f);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void RadialMenu_Open_FiresOpenedEvent()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(new UIRadialMenu { OuterRadius = 100 })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIRadialMenuOpenedEvent>(e =>
        {
            if (e.Menu == radialMenu)
            {
                eventFired = true;
            }
        });

        var requestEvent = new UIRadialMenuRequestEvent(radialMenu, new Vector2(400, 300));
        world.Send(requestEvent);

        radialMenuSystem.Update(0);

        Assert.True(eventFired);
    }

    [Fact]
    public void RadialMenu_Close_FiresClosedEvent()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRadialMenu { IsOpen = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIRadialMenuClosedEvent>(e =>
        {
            if (e.Menu == radialMenu)
            {
                eventFired = true;
            }
        });

        radialMenuSystem.CloseRadialMenu(radialMenu);

        Assert.True(eventFired);
    }

    [Fact]
    public void RadialMenu_SelectionChange_FiresChangedEvent()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRadialMenu { IsOpen = true, SliceCount = 4, StartAngle = 0, SelectedIndex = -1 })
            .With(new UIRadialMenuInputState { InputDirection = new Vector2(1, 0), InputMagnitude = 0.8f })
            .Build();

        bool eventFired = false;
        int newIndex = -1;
        world.Subscribe<UIRadialSliceChangedEvent>(e =>
        {
            if (e.Menu == radialMenu)
            {
                eventFired = true;
                newIndex = e.NewIndex;
            }
        });

        radialMenuSystem.Update(0);

        Assert.True(eventFired);
        Assert.True(newIndex >= 0);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void RadialMenu_Cancel_ClosesMenu()
    {
        using var world = new World();
        var radialMenuSystem = new UIRadialMenuSystem();
        world.AddSystem(radialMenuSystem);

        var radialMenu = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRadialMenu { IsOpen = true })
            .Build();

        radialMenuSystem.Cancel(radialMenu);

        ref readonly var menu = ref world.Get<UIRadialMenu>(radialMenu);

        Assert.False(menu.IsOpen);
    }

    #endregion
}
