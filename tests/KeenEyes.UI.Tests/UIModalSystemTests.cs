using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIModalSystem modal dialog management.
/// </summary>
public class UIModalSystemTests
{
    #region Open/Close Tests

    [Fact]
    public void Modal_OpenModal_SetsIsOpenTrue()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .Build();

        modalSystem.OpenModal(modal);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);

        Assert.True(modalComponent.IsOpen);
    }

    [Fact]
    public void Modal_OpenModal_MakesElementVisible()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .Build();

        world.Add(modal, new UIHiddenTag());

        modalSystem.OpenModal(modal);

        ref readonly var element = ref world.Get<UIElement>(modal);

        Assert.True(element.Visible);
        Assert.False(world.Has<UIHiddenTag>(modal));
    }

    [Fact]
    public void Modal_CloseModal_SetsIsOpenFalse()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        modalSystem.CloseModal(modal, ModalResult.OK);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);

        Assert.False(modalComponent.IsOpen);
    }

    [Fact]
    public void Modal_CloseModal_HidesElement()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        modalSystem.CloseModal(modal, ModalResult.OK);

        ref readonly var element = ref world.Get<UIElement>(modal);

        Assert.False(element.Visible);
        Assert.True(world.Has<UIHiddenTag>(modal));
    }

    #endregion

    #region Backdrop Tests

    [Fact]
    public void Modal_OpenWithBackdrop_ShowsBackdrop()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var backdrop = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        world.Add(backdrop, new UIHiddenTag());

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { Backdrop = backdrop })
            .Build();

        world.Add(modal, new UIHiddenTag());

        modalSystem.OpenModal(modal);

        ref readonly var backdropElement = ref world.Get<UIElement>(backdrop);

        Assert.True(backdropElement.Visible);
        Assert.False(world.Has<UIHiddenTag>(backdrop));
    }

    [Fact]
    public void Modal_CloseWithBackdrop_HidesBackdrop()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var backdrop = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        // Create UIModal explicitly to ensure all fields are set
        var modalComponent = new UIModal("Test Modal")
        {
            IsOpen = true,
            Backdrop = backdrop
        };

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(modalComponent)
            .Build();

        modalSystem.CloseModal(modal, ModalResult.OK);

        ref readonly var backdropElement = ref world.Get<UIElement>(backdrop);

        Assert.False(backdropElement.Visible);
        Assert.True(world.Has<UIHiddenTag>(backdrop));
    }

    [Fact]
    public void Modal_BackdropClick_ClosesWhenAllowed()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var backdrop = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnBackdropClick: true) { IsOpen = true, Backdrop = backdrop })
            .Build();

        world.Add(backdrop, new UIModalBackdrop(modal));

        var clickEvent = new UIClickEvent(backdrop, new Vector2(100, 100), MouseButton.Left);
        world.Send(clickEvent);

        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);

        Assert.False(modalComponent.IsOpen);
    }

    [Fact]
    public void Modal_BackdropClick_DoesNotCloseWhenDisallowed()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var backdrop = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnBackdropClick: false) { IsOpen = true, Backdrop = backdrop })
            .Build();

        world.Add(backdrop, new UIModalBackdrop(modal));

        var clickEvent = new UIClickEvent(backdrop, new Vector2(100, 100), MouseButton.Left);
        world.Send(clickEvent);

        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);

        Assert.True(modalComponent.IsOpen);
    }

    #endregion

    #region Close Button Tests

    [Fact]
    public void Modal_CloseButtonClick_ClosesModal()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        var closeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIModalCloseButton(modal))
            .Build();

        var clickEvent = new UIClickEvent(closeButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);

        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);

        Assert.False(modalComponent.IsOpen);
    }

    #endregion

    #region Action Button Tests

    [Fact]
    public void Modal_ActionButtonClick_ClosesModal()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        var okButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIModalButton(modal, ModalResult.OK))
            .Build();

        var clickEvent = new UIClickEvent(okButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);

        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);

        Assert.False(modalComponent.IsOpen);
    }

    [Fact]
    public void Modal_ActionButtonClick_FiresResultEvent()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        var okButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIModalButton(modal, ModalResult.OK))
            .Build();

        bool eventFired = false;
        ModalResult receivedResult = ModalResult.None;
        world.Subscribe<UIModalResultEvent>(e =>
        {
            if (e.Modal == modal)
            {
                eventFired = true;
                receivedResult = e.Result;
            }
        });

        var clickEvent = new UIClickEvent(okButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);

        modalSystem.Update(0);

        Assert.True(eventFired);
        Assert.Equal(ModalResult.OK, receivedResult);
    }

    [Fact]
    public void Modal_CancelButtonClick_FiresCancelResult()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        var cancelButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIModalButton(modal, ModalResult.Cancel))
            .Build();

        bool eventFired = false;
        ModalResult receivedResult = ModalResult.None;
        world.Subscribe<UIModalResultEvent>(e =>
        {
            if (e.Modal == modal)
            {
                eventFired = true;
                receivedResult = e.Result;
            }
        });

        var clickEvent = new UIClickEvent(cancelButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);

        modalSystem.Update(0);

        Assert.True(eventFired);
        Assert.Equal(ModalResult.Cancel, receivedResult);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Modal_Open_FiresOpenedEvent()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIModalOpenedEvent>(e =>
        {
            if (e.Modal == modal)
            {
                eventFired = true;
            }
        });

        modalSystem.OpenModal(modal);

        Assert.True(eventFired);
    }

    [Fact]
    public void Modal_Close_FiresClosedEvent()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        bool eventFired = false;
        ModalResult receivedResult = ModalResult.None;
        world.Subscribe<UIModalClosedEvent>(e =>
        {
            if (e.Modal == modal)
            {
                eventFired = true;
                receivedResult = e.Result;
            }
        });

        modalSystem.CloseModal(modal, ModalResult.OK);

        Assert.True(eventFired);
        Assert.Equal(ModalResult.OK, receivedResult);
    }

    #endregion

    #region Already Open/Closed Tests

    [Fact]
    public void Modal_OpenAlreadyOpen_NoChange()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        int eventCount = 0;
        world.Subscribe<UIModalOpenedEvent>(_ => eventCount++);

        modalSystem.OpenModal(modal);

        Assert.Equal(0, eventCount); // No event should fire since already open
    }

    [Fact]
    public void Modal_CloseAlreadyClosed_NoChange()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .Build();

        int eventCount = 0;
        world.Subscribe<UIModalClosedEvent>(_ => eventCount++);

        modalSystem.CloseModal(modal, ModalResult.Cancel);

        Assert.Equal(0, eventCount); // No event should fire since already closed
    }

    #endregion
}
