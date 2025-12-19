using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIWindowSystem window management.
/// </summary>
public class UIWindowSystemTests
{
    #region Window Dragging Tests

    [Fact]
    public void TitleBarDrag_WithDraggableWindow_MovesWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanDrag = true })
            .Build();
        world.SetParent(window, canvas);

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(window))
            .Build();
        world.SetParent(titleBar, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(titleBar, new Vector2(10, 5), new Vector2(150, 150));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Offset.Left.ApproximatelyEquals(110f));
        Assert.True(rect.Offset.Top.ApproximatelyEquals(105f));
    }

    [Fact]
    public void TitleBarDrag_WithNonDraggableWindow_DoesNotMove()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanDrag = false })
            .Build();
        world.SetParent(window, canvas);

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(window))
            .Build();
        world.SetParent(titleBar, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(titleBar, new Vector2(10, 5), new Vector2(150, 150));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Offset.Left.ApproximatelyEquals(100f));
        Assert.True(rect.Offset.Top.ApproximatelyEquals(100f));
    }

    [Fact]
    public void TitleBarDrag_MarksLayoutDirty()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanDrag = true })
            .Build();
        world.SetParent(window, canvas);

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(window))
            .Build();
        world.SetParent(titleBar, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(titleBar, new Vector2(10, 5), new Vector2(150, 150));
        world.Send(dragEvent);

        windowSystem.Update(0);

        Assert.True(world.Has<UILayoutDirtyTag>(window));
    }

    #endregion

    #region Window Close Tests

    [Fact]
    public void CloseButton_WithClosableWindow_HidesWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanClose = true })
            .Build();

        var closeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowCloseButton(window))
            .Build();

        var clickEvent = new UIClickEvent(closeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var element = ref world.Get<UIElement>(window);
        Assert.False(element.Visible);
        Assert.True(world.Has<UIHiddenTag>(window));
    }

    [Fact]
    public void CloseButton_WithNonClosableWindow_DoesNotHide()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanClose = false })
            .Build();

        var closeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowCloseButton(window))
            .Build();

        var clickEvent = new UIClickEvent(closeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var element = ref world.Get<UIElement>(window);
        Assert.True(element.Visible);
        Assert.False(world.Has<UIHiddenTag>(window));
    }

    [Fact]
    public void CloseButton_FiresWindowClosedEvent()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanClose = true })
            .Build();

        var closeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowCloseButton(window))
            .Build();

        bool eventFired = false;
        world.Subscribe<UIWindowClosedEvent>(e =>
        {
            if (e.Window == window)
            {
                eventFired = true;
            }
        });

        var clickEvent = new UIClickEvent(closeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        Assert.True(eventFired);
    }

    #endregion

    #region Window Resize Tests

    [Fact]
    public void ResizeHandle_RightEdge_IncreasesWidth()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanResize = true, MinSize = new Vector2(100, 100) })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Right))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(20, 0), new Vector2(420, 200));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Size.X.ApproximatelyEquals(320f));
        Assert.True(rect.Size.Y.ApproximatelyEquals(200f));
    }

    [Fact]
    public void ResizeHandle_BottomEdge_IncreasesHeight()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanResize = true, MinSize = new Vector2(100, 100) })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Bottom))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(0, 30), new Vector2(250, 330));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Size.X.ApproximatelyEquals(300f));
        Assert.True(rect.Size.Y.ApproximatelyEquals(230f));
    }

    [Fact]
    public void ResizeHandle_RespectsMinSize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanResize = true, MinSize = new Vector2(200, 150) })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Right))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(-200, 0), new Vector2(200, 200));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Size.X.ApproximatelyEquals(200f));
    }

    [Fact]
    public void ResizeHandle_RespectsMaxSize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanResize = true, MinSize = new Vector2(100, 100), MaxSize = new Vector2(400, 300) })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Right))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(200, 0), new Vector2(600, 200));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Size.X.ApproximatelyEquals(400f));
    }

    [Fact]
    public void ResizeHandle_WithNonResizableWindow_DoesNotResize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow { CanResize = false })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Right))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(20, 0), new Vector2(420, 200));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.True(rect.Size.X.ApproximatelyEquals(300f));
    }

    #endregion

    #region Z-Order Tests

    [Fact]
    public void WindowClick_BringsWindowToFront()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window1 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .Build();

        var window2 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(200, 200, 300, 200))
            .With(new UIWindow())
            .Build();

        var clickEvent = new UIClickEvent(window1, new Vector2(150, 150), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var window1Component = ref world.Get<UIWindow>(window1);
        ref readonly var window2Component = ref world.Get<UIWindow>(window2);

        Assert.True(window1Component.ZOrder > window2Component.ZOrder);
    }

    [Fact]
    public void WindowClick_UpdatesLocalZIndex()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .Build();

        var clickEvent = new UIClickEvent(window, new Vector2(150, 150), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        ref readonly var rect = ref world.Get<UIRect>(window);

        Assert.Equal(windowComponent.ZOrder, rect.LocalZIndex);
    }

    [Fact]
    public void ChildElementClick_BringsParentWindowToFront()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .Build();

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 50, 50))
            .Build();
        world.SetParent(child, window);

        ref readonly var windowBefore = ref world.Get<UIWindow>(window);
        var zOrderBefore = windowBefore.ZOrder;

        var clickEvent = new UIClickEvent(child, new Vector2(35, 35), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowAfter = ref world.Get<UIWindow>(window);

        Assert.True(windowAfter.ZOrder >= zOrderBefore);
    }

    #endregion

    #region ShowWindow Tests

    [Fact]
    public void ShowWindow_MakesWindowVisible()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .With(new UIHiddenTag())
            .Build();

        windowSystem.ShowWindow(window);

        ref readonly var element = ref world.Get<UIElement>(window);

        Assert.True(element.Visible);
        Assert.False(world.Has<UIHiddenTag>(window));
    }

    [Fact]
    public void ShowWindow_BringsToFront()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window1 = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .Build();

        var window2 = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(200, 200, 300, 200))
            .With(new UIWindow())
            .Build();

        var clickEvent = new UIClickEvent(window1, new Vector2(150, 150), MouseButton.Left);
        world.Send(clickEvent);
        windowSystem.Update(0);

        windowSystem.ShowWindow(window2);

        ref readonly var window1Component = ref world.Get<UIWindow>(window1);
        ref readonly var window2Component = ref world.Get<UIWindow>(window2);

        Assert.True(window2Component.ZOrder > window1Component.ZOrder);
    }

    [Fact]
    public void ShowWindow_MarksLayoutDirty()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);
        windowSystem.Initialize(world);

        var window = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .Build();

        windowSystem.ShowWindow(window);

        Assert.True(world.Has<UILayoutDirtyTag>(window));
    }

    #endregion

    #region Helper Methods

    private static (Entity Canvas, UILayoutSystem Layout) SetupLayout(World world)
    {
        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);

        return (canvas, layout);
    }

    #endregion
}
