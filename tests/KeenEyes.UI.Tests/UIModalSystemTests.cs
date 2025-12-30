using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;
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

    #region Escape Key Tests

    [Fact]
    public void Modal_EscapeKey_ClosesModalWhenAllowed()
    {
        using var world = new World();
        var mockInput = new MockInputContext();
        world.SetExtension<IInputContext>(mockInput);

        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnEscape: true) { IsOpen = true })
            .Build();

        // First update with escape not pressed (to initialize state)
        mockInput.MockKeyboard.SetKeyUp(Key.Escape);
        modalSystem.Update(0);

        // Now press escape
        mockInput.MockKeyboard.SetKeyDown(Key.Escape);
        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);
        Assert.False(modalComponent.IsOpen);
    }

    [Fact]
    public void Modal_EscapeKey_DoesNotCloseWhenDisallowed()
    {
        using var world = new World();
        var mockInput = new MockInputContext();
        world.SetExtension<IInputContext>(mockInput);

        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnEscape: false) { IsOpen = true })
            .Build();

        // First update with escape not pressed (to initialize state)
        mockInput.MockKeyboard.SetKeyUp(Key.Escape);
        modalSystem.Update(0);

        // Now press escape
        mockInput.MockKeyboard.SetKeyDown(Key.Escape);
        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);
        Assert.True(modalComponent.IsOpen); // Still open because closeOnEscape is false
    }

    [Fact]
    public void Modal_EscapeKey_DoesNotCloseWhileHeldDown()
    {
        using var world = new World();
        var mockInput = new MockInputContext();
        world.SetExtension<IInputContext>(mockInput);

        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnEscape: true) { IsOpen = true })
            .Build();

        // First update with escape not pressed (initializes escapeWasDown to false)
        mockInput.MockKeyboard.SetKeyUp(Key.Escape);
        modalSystem.Update(0);

        // Now press escape (transition from up to down) - this closes the modal
        mockInput.MockKeyboard.SetKeyDown(Key.Escape);
        modalSystem.Update(0);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);
        Assert.False(modalComponent.IsOpen); // Modal is now closed

        // Reopen the modal
        modalSystem.OpenModal(modal);
        Assert.True(world.Get<UIModal>(modal).IsOpen);

        // Another update with escape still held down - should NOT close again (no transition)
        modalSystem.Update(0);

        // Modal should still be open because there was no transition
        Assert.True(world.Get<UIModal>(modal).IsOpen);
    }

    [Fact]
    public void Modal_EscapeKey_FiresCancelResult()
    {
        using var world = new World();
        var mockInput = new MockInputContext();
        world.SetExtension<IInputContext>(mockInput);

        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnEscape: true) { IsOpen = true })
            .Build();

        ModalResult? receivedResult = null;
        world.Subscribe<UIModalClosedEvent>(e =>
        {
            if (e.Modal == modal)
            {
                receivedResult = e.Result;
            }
        });

        // First update with escape not pressed
        mockInput.MockKeyboard.SetKeyUp(Key.Escape);
        modalSystem.Update(0);

        // Now press escape
        mockInput.MockKeyboard.SetKeyDown(Key.Escape);
        modalSystem.Update(0);

        Assert.Equal(ModalResult.Cancel, receivedResult);
    }

    #endregion

    #region Invalid Entity Tests

    [Fact]
    public void Modal_OpenWithDeadEntity_NoChange()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .Build();

        // Despawn the entity
        world.Despawn(modal);

        // Should not throw
        modalSystem.OpenModal(modal);
    }

    [Fact]
    public void Modal_OpenWithoutUIModal_NoChange()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        // Entity without UIModal component
        var entity = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        // Should not throw
        modalSystem.OpenModal(entity);
    }

    [Fact]
    public void Modal_CloseWithDeadEntity_NoChange()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true })
            .Build();

        // Despawn the entity
        world.Despawn(modal);

        // Should not throw
        modalSystem.CloseModal(modal, ModalResult.OK);
    }

    [Fact]
    public void Modal_CloseWithoutUIModal_NoChange()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        // Entity without UIModal component
        var entity = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        // Should not throw
        modalSystem.CloseModal(entity, ModalResult.OK);
    }

    [Fact]
    public void Modal_BackdropClickWithDeadModal_NoChange()
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

        // Despawn the modal
        world.Despawn(modal);

        // Should not throw when clicking backdrop
        var clickEvent = new UIClickEvent(backdrop, new Vector2(100, 100), MouseButton.Left);
        world.Send(clickEvent);
        modalSystem.Update(0);
    }

    [Fact]
    public void Modal_CloseButtonClickWithDeadModal_NoChange()
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

        // Despawn the modal
        world.Despawn(modal);

        // Should not throw when clicking close button
        var clickEvent = new UIClickEvent(closeButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);
        modalSystem.Update(0);
    }

    [Fact]
    public void Modal_ActionButtonClickWithDeadModal_NoChange()
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

        // Despawn the modal
        world.Despawn(modal);

        // Should not throw when clicking action button
        var clickEvent = new UIClickEvent(okButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);
        modalSystem.Update(0);
    }

    #endregion

    #region Layout Dirty Tag Tests

    [Fact]
    public void Modal_Open_AddsLayoutDirtyTag()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .Build();

        modalSystem.OpenModal(modal);

        Assert.True(world.Has<UILayoutDirtyTag>(modal));
    }

    [Fact]
    public void Modal_OpenWithExistingDirtyTag_NoDoubleAdd()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false })
            .With(new UILayoutDirtyTag())
            .Build();

        // Should not throw when tag already exists
        modalSystem.OpenModal(modal);

        Assert.True(world.Has<UILayoutDirtyTag>(modal));
    }

    #endregion

    #region Input Context Tests

    [Fact]
    public void Modal_UpdateWithNoInputContext_ReturnsEarly()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnEscape: true) { IsOpen = true })
            .Build();

        // Update with no input context - should not throw
        modalSystem.Update(0);

        // Modal should still be open since no escape key handling is possible
        ref readonly var modalComponent = ref world.Get<UIModal>(modal);
        Assert.True(modalComponent.IsOpen);
    }

    [Fact]
    public void Modal_UpdateRetriesInputContext()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal", closeOnEscape: true) { IsOpen = true })
            .Build();

        // First update - no input context available
        modalSystem.Update(0);

        ref readonly var modalComponent1 = ref world.Get<UIModal>(modal);
        Assert.True(modalComponent1.IsOpen);

        // Now set up input context
        var mockInput = new MockInputContext();
        world.SetExtension<IInputContext>(mockInput);
        mockInput.MockKeyboard.SetKeyUp(Key.Escape);
        modalSystem.Update(0);

        // Press escape
        mockInput.MockKeyboard.SetKeyDown(Key.Escape);
        modalSystem.Update(0);

        ref readonly var modalComponent2 = ref world.Get<UIModal>(modal);
        Assert.False(modalComponent2.IsOpen);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Modal_Dispose_CleansUpSubscription()
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

        // Dispose the system
        modalSystem.Dispose();

        // Click event should not close modal anymore (subscription disposed)
        var clickEvent = new UIClickEvent(closeButton, new Vector2(10, 10), MouseButton.Left);
        world.Send(clickEvent);

        ref readonly var modalComponent = ref world.Get<UIModal>(modal);
        Assert.True(modalComponent.IsOpen);
    }

    #endregion

    #region Backdrop Edge Cases

    [Fact]
    public void Modal_OpenWithInvalidBackdrop_StillOpensModal()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        // Create modal with invalid (default) backdrop entity
        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false, Backdrop = default })
            .Build();

        world.Add(modal, new UIHiddenTag());

        modalSystem.OpenModal(modal);

        ref readonly var element = ref world.Get<UIElement>(modal);
        Assert.True(element.Visible);
    }

    [Fact]
    public void Modal_CloseWithInvalidBackdrop_StillClosesModal()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        // Create modal with invalid (default) backdrop entity
        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true, Backdrop = default })
            .Build();

        modalSystem.CloseModal(modal, ModalResult.OK);

        ref readonly var element = ref world.Get<UIElement>(modal);
        Assert.False(element.Visible);
    }

    [Fact]
    public void Modal_OpenWithDeadBackdrop_StillOpensModal()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var backdrop = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var modal = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIModal("Test Modal") { IsOpen = false, Backdrop = backdrop })
            .Build();

        // Despawn backdrop
        world.Despawn(backdrop);

        modalSystem.OpenModal(modal);

        ref readonly var element = ref world.Get<UIElement>(modal);
        Assert.True(element.Visible);
    }

    [Fact]
    public void Modal_CloseWithDeadBackdrop_StillClosesModal()
    {
        using var world = new World();
        var modalSystem = new UIModalSystem();
        world.AddSystem(modalSystem);

        var backdrop = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var modal = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIModal("Test Modal") { IsOpen = true, Backdrop = backdrop })
            .Build();

        // Despawn backdrop
        world.Despawn(backdrop);

        modalSystem.CloseModal(modal, ModalResult.OK);

        ref readonly var element = ref world.Get<UIElement>(modal);
        Assert.False(element.Visible);
    }

    #endregion
}
