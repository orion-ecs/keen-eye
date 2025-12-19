using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIToastSystem toast notification management.
/// </summary>
public class UIToastSystemTests
{
    #region Show/Dismiss Tests

    [Fact]
    public void Toast_ShowToast_MakesElementVisible()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIToast("Test message") { Container = container })
            .Build();

        world.Add(toast, new UIHiddenTag());

        toastSystem.ShowToast(toast);

        ref readonly var element = ref world.Get<UIElement>(toast);

        Assert.True(element.Visible);
        Assert.False(world.Has<UIHiddenTag>(toast));
    }

    [Fact]
    public void Toast_ShowToast_ResetsTimer()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIToast("Test message", duration: 5f) { Container = container, TimeRemaining = 1f })
            .Build();

        toastSystem.ShowToast(toast);

        ref readonly var toastComponent = ref world.Get<UIToast>(toast);

        Assert.True(toastComponent.TimeRemaining.ApproximatelyEquals(5f));
        Assert.False(toastComponent.IsClosing);
    }

    [Fact]
    public void Toast_DismissToast_HidesElement()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message") { Container = container })
            .Build();

        toastSystem.DismissToast(toast, wasManual: true);

        // Toast should be despawned after dismissal
        Assert.False(world.IsAlive(toast));
    }

    [Fact]
    public void Toast_DismissToast_FiresDismissedEvent()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message") { Container = container })
            .Build();

        UIToastDismissedEvent? receivedEvent = null;
        var subscription = world.Subscribe<UIToastDismissedEvent>(e => receivedEvent = e);

        toastSystem.DismissToast(toast, wasManual: true);

        Assert.NotNull(receivedEvent);
        Assert.Equal(toast, receivedEvent.Value.Toast);
        Assert.True(receivedEvent.Value.WasManual);

        subscription.Dispose();
    }

    #endregion

    #region Auto-Dismiss Timer Tests

    [Fact]
    public void Toast_Update_DecrementsTimeRemaining()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message", duration: 5f) { Container = container, TimeRemaining = 5f })
            .Build();

        toastSystem.Update(1f);

        ref readonly var toastComponent = ref world.Get<UIToast>(toast);

        Assert.True(toastComponent.TimeRemaining.ApproximatelyEquals(4f));
    }

    [Fact]
    public void Toast_Update_DismissesWhenTimerExpires()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message", duration: 1f) { Container = container, TimeRemaining = 1f })
            .Build();

        UIToastDismissedEvent? receivedEvent = null;
        var subscription = world.Subscribe<UIToastDismissedEvent>(e => receivedEvent = e);

        // Advance time past duration
        toastSystem.Update(1.5f);

        Assert.NotNull(receivedEvent);
        Assert.Equal(toast, receivedEvent.Value.Toast);
        Assert.False(receivedEvent.Value.WasManual);
        Assert.False(world.IsAlive(toast));

        subscription.Dispose();
    }

    [Fact]
    public void Toast_Update_SkipsIndefiniteDuration()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        // Duration of 0 means indefinite (no auto-dismiss)
        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message", duration: 0f) { Container = container, TimeRemaining = 0f })
            .Build();

        // Update multiple times
        toastSystem.Update(10f);
        toastSystem.Update(10f);
        toastSystem.Update(10f);

        // Toast should still be alive
        Assert.True(world.IsAlive(toast));
    }

    [Fact]
    public void Toast_Update_SkipsClosingToasts()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message", duration: 5f) { Container = container, TimeRemaining = 5f, IsClosing = true })
            .Build();

        // Update should skip this toast because it's already closing
        toastSystem.Update(1f);

        ref readonly var toastComponent = ref world.Get<UIToast>(toast);

        // TimeRemaining should not have changed
        Assert.True(toastComponent.TimeRemaining.ApproximatelyEquals(5f));
    }

    #endregion

    #region Click Dismiss Tests

    [Fact]
    public void Toast_ClickOnToast_DismissesWhenCanDismiss()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message") { Container = container, CanDismiss = true })
            .Build();

        UIToastDismissedEvent? receivedEvent = null;
        var subscription = world.Subscribe<UIToastDismissedEvent>(e => receivedEvent = e);

        world.Send(new UIClickEvent(toast, new Vector2(50, 50), MouseButton.Left));

        Assert.NotNull(receivedEvent);
        Assert.True(receivedEvent.Value.WasManual);

        subscription.Dispose();
    }

    [Fact]
    public void Toast_ClickOnToast_IgnoresWhenCannotDismiss()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message") { Container = container, CanDismiss = false })
            .Build();

        UIToastDismissedEvent? receivedEvent = null;
        var subscription = world.Subscribe<UIToastDismissedEvent>(e => receivedEvent = e);

        world.Send(new UIClickEvent(toast, new Vector2(50, 50), MouseButton.Left));

        Assert.Null(receivedEvent);
        Assert.True(world.IsAlive(toast));

        subscription.Dispose();
    }

    [Fact]
    public void Toast_ClickCloseButton_DismissesToast()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Test message") { Container = container })
            .Build();

        var closeButton = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastCloseButton(toast))
            .Build();

        UIToastDismissedEvent? receivedEvent = null;
        var subscription = world.Subscribe<UIToastDismissedEvent>(e => receivedEvent = e);

        world.Send(new UIClickEvent(closeButton, new Vector2(10, 10), MouseButton.Left));

        Assert.NotNull(receivedEvent);
        Assert.True(receivedEvent.Value.WasManual);

        subscription.Dispose();
    }

    #endregion

    #region Container Tests

    [Fact]
    public void Toast_GetVisibleToastCount_ReturnsCorrectCount()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        // Create 3 visible toasts
        for (int i = 0; i < 3; i++)
        {
            world.Spawn()
                .With(new UIElement { Visible = true })
                .With(new UIToast($"Toast {i}") { Container = container, IsClosing = false })
                .Build();
        }

        // Create 1 closing toast (should not be counted)
        world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Closing Toast") { Container = container, IsClosing = true })
            .Build();

        int count = toastSystem.GetVisibleToastCount(container);

        Assert.Equal(3, count);
    }

    [Fact]
    public void Toast_GetVisibleToastCount_OnlyCountsToastsInContainer()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container1 = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var container2 = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        // Create toasts in different containers
        world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Toast 1") { Container = container1 })
            .Build();

        world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Toast 2") { Container = container1 })
            .Build();

        world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToast("Toast 3") { Container = container2 })
            .Build();

        Assert.Equal(2, toastSystem.GetVisibleToastCount(container1));
        Assert.Equal(1, toastSystem.GetVisibleToastCount(container2));
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Toast_ShowToast_FiresShownEvent()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        var container = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIToastContainer())
            .Build();

        var toast = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(new UIToast("Test message") { Container = container })
            .Build();

        UIToastShownEvent? receivedEvent = null;
        var subscription = world.Subscribe<UIToastShownEvent>(e => receivedEvent = e);

        toastSystem.ShowToast(toast);

        Assert.NotNull(receivedEvent);
        Assert.Equal(toast, receivedEvent.Value.Toast);

        subscription.Dispose();
    }

    #endregion

    #region Invalid Entity Tests

    [Fact]
    public void Toast_ShowToast_HandlesInvalidEntity()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        // Should not throw
        toastSystem.ShowToast(Entity.Null);
    }

    [Fact]
    public void Toast_DismissToast_HandlesInvalidEntity()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        // Should not throw
        toastSystem.DismissToast(Entity.Null, wasManual: true);
    }

    [Fact]
    public void Toast_GetVisibleToastCount_HandlesInvalidContainer()
    {
        using var world = new World();
        var toastSystem = new UIToastSystem();
        world.AddSystem(toastSystem);

        int count = toastSystem.GetVisibleToastCount(Entity.Null);

        Assert.Equal(0, count);
    }

    #endregion
}
