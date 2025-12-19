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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(titleBar, new Vector2(150, 150), new Vector2(10, 5));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(titleBar, new Vector2(150, 150), new Vector2(10, 5));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(titleBar, new Vector2(150, 150), new Vector2(10, 5));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(420, 200), new Vector2(20, 0));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(250, 330), new Vector2(0, 30));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(200, 200), new Vector2(-200, 0));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(600, 200), new Vector2(200, 0));
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

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(420, 200), new Vector2(20, 0));
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

        var window = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow())
            .Build();

        windowSystem.ShowWindow(window);

        Assert.True(world.Has<UILayoutDirtyTag>(window));
    }

    #endregion

    #region Minimize Tests

    [Fact]
    public void MinimizeButton_WithMinimizableWindow_MinimizesWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var contentPanel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 30, 300, 170))
            .Build();

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMinimize = true,
                ContentPanel = contentPanel,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        world.SetParent(contentPanel, window);

        var minimizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMinimizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(minimizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        Assert.Equal(WindowState.Minimized, windowComponent.State);
    }

    [Fact]
    public void MinimizeButton_HidesContentPanel()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var contentPanel = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 30, 300, 170))
            .Build();

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMinimize = true,
                ContentPanel = contentPanel
            })
            .Build();

        world.SetParent(contentPanel, window);

        var minimizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMinimizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(minimizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var element = ref world.Get<UIElement>(contentPanel);
        Assert.False(element.Visible);
        Assert.True(world.Has<UIHiddenTag>(contentPanel));
    }

    [Fact]
    public void MinimizeButton_WithNonMinimizableWindow_DoesNotMinimize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test") { CanMinimize = false })
            .Build();

        var minimizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMinimizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(minimizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        Assert.Equal(WindowState.Normal, windowComponent.State);
    }

    [Fact]
    public void MinimizeWindow_FiresMinimizedEvent()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test") { CanMinimize = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIWindowMinimizedEvent>(e =>
        {
            if (e.Window == window)
            {
                eventFired = true;
            }
        });

        windowSystem.MinimizeWindow(window);

        Assert.True(eventFired);
    }

    [Fact]
    public void MinimizeButton_WhenMinimized_RestoresWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMinimize = true,
                State = WindowState.Minimized,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        var minimizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMinimizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(minimizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        Assert.Equal(WindowState.Normal, windowComponent.State);
    }

    #endregion

    #region Maximize Tests

    [Fact]
    public void MaximizeButton_WithMaximizableWindow_MaximizesWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMaximize = true,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        var maximizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMaximizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(maximizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        Assert.Equal(WindowState.Maximized, windowComponent.State);
    }

    [Fact]
    public void MaximizeButton_StretchesWindowToFill()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test") { CanMaximize = true })
            .Build();

        var maximizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMaximizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(maximizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(window);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void MaximizeButton_WithNonMaximizableWindow_DoesNotMaximize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test") { CanMaximize = false })
            .Build();

        var maximizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMaximizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(maximizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        Assert.Equal(WindowState.Normal, windowComponent.State);
    }

    [Fact]
    public void MaximizeWindow_FiresMaximizedEvent()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test") { CanMaximize = true })
            .Build();

        bool eventFired = false;
        world.Subscribe<UIWindowMaximizedEvent>(e =>
        {
            if (e.Window == window)
            {
                eventFired = true;
            }
        });

        windowSystem.MaximizeWindow(window);

        Assert.True(eventFired);
    }

    [Fact]
    public void MaximizeButton_WhenMaximized_RestoresWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIWindow("Test")
            {
                CanMaximize = true,
                State = WindowState.Maximized,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        var maximizeButton = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowMaximizeButton(window))
            .Build();

        var clickEvent = new UIClickEvent(maximizeButton, new Vector2(110, 110), MouseButton.Left);
        world.Send(clickEvent);

        windowSystem.Update(0);

        ref readonly var windowComponent = ref world.Get<UIWindow>(window);
        Assert.Equal(WindowState.Normal, windowComponent.State);
    }

    #endregion

    #region Restore Tests

    [Fact]
    public void RestoreWindow_FromMinimized_RestoresPositionAndSize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMinimize = true,
                State = WindowState.Minimized,
                RestorePosition = new Vector2(150, 150),
                RestoreSize = new Vector2(400, 300)
            })
            .Build();

        windowSystem.RestoreWindow(window);

        ref readonly var rect = ref world.Get<UIRect>(window);
        Assert.True(rect.Offset.Left.ApproximatelyEquals(150f));
        Assert.True(rect.Offset.Top.ApproximatelyEquals(150f));
        Assert.True(rect.Size.X.ApproximatelyEquals(400f));
        Assert.True(rect.Size.Y.ApproximatelyEquals(300f));
    }

    [Fact]
    public void RestoreWindow_FromMaximized_RestoresPositionAndSize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIWindow("Test")
            {
                CanMaximize = true,
                State = WindowState.Maximized,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        windowSystem.RestoreWindow(window);

        ref readonly var rect = ref world.Get<UIRect>(window);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
        Assert.True(rect.Size.X.ApproximatelyEquals(300f));
        Assert.True(rect.Size.Y.ApproximatelyEquals(200f));
    }

    [Fact]
    public void RestoreWindow_FiresRestoredEvent()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMinimize = true,
                State = WindowState.Minimized,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        bool eventFired = false;
        WindowState capturedPreviousState = WindowState.Normal;
        world.Subscribe<UIWindowRestoredEvent>(e =>
        {
            if (e.Window == window)
            {
                eventFired = true;
                capturedPreviousState = e.PreviousState;
            }
        });

        windowSystem.RestoreWindow(window);

        Assert.True(eventFired);
        Assert.Equal(WindowState.Minimized, capturedPreviousState);
    }

    [Fact]
    public void RestoreWindow_ShowsContentPanel()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var contentPanel = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(0, 30, 300, 170))
            .With(new UIHiddenTag())
            .Build();

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanMinimize = true,
                State = WindowState.Minimized,
                ContentPanel = contentPanel,
                RestorePosition = new Vector2(100, 100),
                RestoreSize = new Vector2(300, 200)
            })
            .Build();

        world.SetParent(contentPanel, window);

        windowSystem.RestoreWindow(window);

        ref readonly var element = ref world.Get<UIElement>(contentPanel);
        Assert.True(element.Visible);
        Assert.False(world.Has<UIHiddenTag>(contentPanel));
    }

    #endregion

    #region Maximize/Minimize Drag Prevention Tests

    [Fact]
    public void TitleBarDrag_WhenMaximized_DoesNotMoveWindow()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIWindow("Test")
            {
                CanDrag = true,
                State = WindowState.Maximized
            })
            .Build();
        world.SetParent(window, canvas);

        var titleBar = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowTitleBar(window))
            .Build();
        world.SetParent(titleBar, window);

        layout.Update(0);

        ref readonly var rectBefore = ref world.Get<UIRect>(window);
        var offsetBefore = rectBefore.Offset;

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(titleBar, new Vector2(150, 150), new Vector2(50, 50));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rectAfter = ref world.Get<UIRect>(window);
        Assert.Equal(offsetBefore.Left, rectAfter.Offset.Left);
        Assert.Equal(offsetBefore.Top, rectAfter.Offset.Top);
    }

    [Fact]
    public void ResizeHandle_WhenMaximized_DoesNotResize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIWindow("Test")
            {
                CanResize = true,
                MinSize = new Vector2(100, 100),
                State = WindowState.Maximized
            })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Right))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        ref readonly var rectBefore = ref world.Get<UIRect>(window);
        var sizeBefore = rectBefore.Size;

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(420, 200), new Vector2(20, 0));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rectAfter = ref world.Get<UIRect>(window);
        Assert.Equal(sizeBefore.X, rectAfter.Size.X);
        Assert.Equal(sizeBefore.Y, rectAfter.Size.Y);
    }

    [Fact]
    public void ResizeHandle_WhenMinimized_DoesNotResize()
    {
        using var world = new World();
        var windowSystem = new UIWindowSystem();
        world.AddSystem(windowSystem);

        var (canvas, layout) = SetupLayout(world);

        var window = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 300, 200))
            .With(new UIWindow("Test")
            {
                CanResize = true,
                MinSize = new Vector2(100, 100),
                State = WindowState.Minimized
            })
            .Build();
        world.SetParent(window, canvas);

        var resizeHandle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIWindowResizeHandle(window, ResizeEdge.Right))
            .Build();
        world.SetParent(resizeHandle, window);

        layout.Update(0);

        ref readonly var rectBefore = ref world.Get<UIRect>(window);
        var sizeBefore = rectBefore.Size;

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(resizeHandle, new Vector2(420, 200), new Vector2(20, 0));
        world.Send(dragEvent);

        windowSystem.Update(0);

        ref readonly var rectAfter = ref world.Get<UIRect>(window);
        Assert.True(rectAfter.Size.X.ApproximatelyEquals(sizeBefore.X));
        Assert.True(rectAfter.Size.Y.ApproximatelyEquals(sizeBefore.Y));
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

        return (canvas, layout);
    }

    #endregion
}
