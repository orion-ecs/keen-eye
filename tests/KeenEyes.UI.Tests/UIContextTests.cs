using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIContext focus management.
/// </summary>
public class UIContextTests
{
    #region RequestFocus Tests

    [Fact]
    public void RequestFocus_WithFocusableElement_SetsFocus()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);

        Assert.Equal(button, context.FocusedEntity);
        Assert.True(context.HasFocus);
    }

    [Fact]
    public void RequestFocus_AddsUIFocusedTag()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);

        Assert.True(world.Has<UIFocusedTag>(button));
    }

    [Fact]
    public void RequestFocus_SetsInteractableState()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.True(interactable.IsFocused);
        Assert.True(interactable.HasEvent(UIEventType.FocusGained));
    }

    [Fact]
    public void RequestFocus_WithNonFocusableElement_DoesNotSetFocus()
    {
        using var world = new World();
        var context = new UIContext(world);

        var clickable = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable()) // CanFocus = false
            .Build();

        context.RequestFocus(clickable);

        Assert.Equal(Entity.Null, context.FocusedEntity);
        Assert.False(context.HasFocus);
    }

    [Fact]
    public void RequestFocus_WithoutInteractable_DoesNotSetFocus()
    {
        using var world = new World();
        var context = new UIContext(world);

        var element = world.Spawn()
            .With(UIElement.Default)
            .Build();

        context.RequestFocus(element);

        Assert.Equal(Entity.Null, context.FocusedEntity);
        Assert.False(context.HasFocus);
    }

    [Fact]
    public void RequestFocus_WithDeadEntity_DoesNotSetFocus()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        world.Despawn(button);

        context.RequestFocus(button);

        Assert.Equal(Entity.Null, context.FocusedEntity);
    }

    [Fact]
    public void RequestFocus_ClearsPreviousFocus()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button1 = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        var button2 = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button1);
        context.RequestFocus(button2);

        // Button1 should no longer be focused
        Assert.False(world.Has<UIFocusedTag>(button1));
        ref readonly var interactable1 = ref world.Get<UIInteractable>(button1);
        Assert.False(interactable1.IsFocused);
        Assert.True(interactable1.HasEvent(UIEventType.FocusLost));

        // Button2 should now be focused
        Assert.True(world.Has<UIFocusedTag>(button2));
        Assert.Equal(button2, context.FocusedEntity);
    }

    [Fact]
    public void RequestFocus_SameEntity_DoesNothing()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);

        // Clear pending events for the test
        ref var interactable = ref world.Get<UIInteractable>(button);
        interactable.PendingEvents = UIEventType.None;

        // Focus same button again
        context.RequestFocus(button);

        // Should not trigger focus gained again
        ref readonly var interactable2 = ref world.Get<UIInteractable>(button);
        Assert.False(interactable2.HasEvent(UIEventType.FocusGained));
    }

    #endregion

    #region ClearFocus Tests

    [Fact]
    public void ClearFocus_WithFocusedElement_ClearsFocus()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);
        context.ClearFocus();

        Assert.Equal(Entity.Null, context.FocusedEntity);
        Assert.False(context.HasFocus);
    }

    [Fact]
    public void ClearFocus_RemovesUIFocusedTag()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);
        context.ClearFocus();

        Assert.False(world.Has<UIFocusedTag>(button));
    }

    [Fact]
    public void ClearFocus_ClearsInteractableState()
    {
        using var world = new World();
        var context = new UIContext(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        context.RequestFocus(button);
        context.ClearFocus();

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.False(interactable.IsFocused);
        Assert.True(interactable.HasEvent(UIEventType.FocusLost));
    }

    [Fact]
    public void ClearFocus_WithNoFocus_DoesNothing()
    {
        using var world = new World();
        var context = new UIContext(world);

        // Should not throw
        context.ClearFocus();

        Assert.Equal(Entity.Null, context.FocusedEntity);
        Assert.False(context.HasFocus);
    }

    #endregion

    #region SetLayoutDirty Tests

    [Fact]
    public void SetLayoutDirty_AddsLayoutDirtyTag()
    {
        using var world = new World();
        var context = new UIContext(world);

        var element = world.Spawn()
            .With(UIElement.Default)
            .Build();

        context.SetLayoutDirty(element);

        Assert.True(world.Has<UILayoutDirtyTag>(element));
    }

    [Fact]
    public void SetLayoutDirty_AlreadyDirty_DoesNotAddTwice()
    {
        using var world = new World();
        var context = new UIContext(world);

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UILayoutDirtyTag())
            .Build();

        // Should not throw
        context.SetLayoutDirty(element);

        Assert.True(world.Has<UILayoutDirtyTag>(element));
    }

    [Fact]
    public void SetLayoutDirty_WithDeadEntity_DoesNothing()
    {
        using var world = new World();
        var context = new UIContext(world);

        var element = world.Spawn()
            .With(UIElement.Default)
            .Build();

        world.Despawn(element);

        // Should not throw
        context.SetLayoutDirty(element);
    }

    #endregion

    #region CreateCanvas Tests

    [Fact]
    public void CreateCanvas_CreatesRootEntity()
    {
        using var world = new World();
        var context = new UIContext(world);

        var canvas = context.CreateCanvas();

        Assert.True(world.IsAlive(canvas));
        Assert.True(world.Has<UIElement>(canvas));
        Assert.True(world.Has<UIRect>(canvas));
        Assert.True(world.Has<UIRootTag>(canvas));
    }

    [Fact]
    public void CreateCanvas_WithName_SetsEntityName()
    {
        using var world = new World();
        var context = new UIContext(world);

        var canvas = context.CreateCanvas("MainCanvas");

        var name = world.GetName(canvas);
        Assert.Equal("MainCanvas", name);
    }

    [Fact]
    public void CreateCanvas_HasStretchRect()
    {
        using var world = new World();
        var context = new UIContext(world);

        var canvas = context.CreateCanvas();

        ref readonly var rect = ref world.Get<UIRect>(canvas);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    #endregion
}
