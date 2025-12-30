using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIScrollbarSystem scrollbar thumb drag handling.
/// </summary>
public class UIScrollbarSystemTests
{
    #region Vertical Scrollbar Tests

    [Fact]
    public void VerticalScrollbar_Drag_UpdatesScrollPosition()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Drag thumb down by 50 pixels
        SimulateDrag(world, thumb, new Vector2(0, 50));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        // With 800 content height in 400 viewport, scroll range is 400
        // 50 pixel drag * (800 / 400) ratio = 100 pixel scroll
        Assert.True(scrollable.ScrollPosition.Y.ApproximatelyEquals(100f));
    }

    [Fact]
    public void VerticalScrollbar_DragDown_IncreasesScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        var initialScroll = world.Get<UIScrollable>(scrollView).ScrollPosition.Y;

        // Drag down
        SimulateDrag(world, thumb, new Vector2(0, 20));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.True(scrollable.ScrollPosition.Y > initialScroll);
    }

    [Fact]
    public void VerticalScrollbar_DragUp_DecreasesScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Set initial scroll position
        ref var scrollable = ref world.Get<UIScrollable>(scrollView);
        scrollable.ScrollPosition = new Vector2(0, 200);

        // Drag up (negative delta)
        SimulateDrag(world, thumb, new Vector2(0, -20));
        system.Update(0);

        Assert.True(scrollable.ScrollPosition.Y < 200f);
    }

    [Fact]
    public void VerticalScrollbar_Drag_ClampsToMaxScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Drag way down (should clamp to max)
        SimulateDrag(world, thumb, new Vector2(0, 500));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        var maxScroll = scrollable.GetMaxScroll(new Vector2(200, 400));
        Assert.True(scrollable.ScrollPosition.Y.ApproximatelyEquals(maxScroll.Y));
    }

    #endregion

    #region Horizontal Scrollbar Tests

    [Fact]
    public void HorizontalScrollbar_Drag_UpdatesScrollPosition()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateHorizontalScrollView(world, 400, 200, 800);
        layout.Update(0);

        // Drag thumb right by 50 pixels
        SimulateDrag(world, thumb, new Vector2(50, 0));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        // With 800 content width in 400 viewport, scroll range is 400
        // 50 pixel drag * (800 / 400) ratio = 100 pixel scroll
        Assert.True(scrollable.ScrollPosition.X.ApproximatelyEquals(100f));
    }

    [Fact]
    public void HorizontalScrollbar_DragRight_IncreasesScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateHorizontalScrollView(world, 400, 200, 800);
        layout.Update(0);

        var initialScroll = world.Get<UIScrollable>(scrollView).ScrollPosition.X;

        // Drag right
        SimulateDrag(world, thumb, new Vector2(20, 0));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.True(scrollable.ScrollPosition.X > initialScroll);
    }

    [Fact]
    public void HorizontalScrollbar_DragLeft_DecreasesScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateHorizontalScrollView(world, 400, 200, 800);
        layout.Update(0);

        // Set initial scroll position
        ref var scrollable = ref world.Get<UIScrollable>(scrollView);
        scrollable.ScrollPosition = new Vector2(200, 0);

        // Drag left (negative delta)
        SimulateDrag(world, thumb, new Vector2(-20, 0));
        system.Update(0);

        Assert.True(scrollable.ScrollPosition.X < 200f);
    }

    [Fact]
    public void HorizontalScrollbar_Drag_ClampsToMaxScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateHorizontalScrollView(world, 400, 200, 800);
        layout.Update(0);

        // Drag way right (should clamp to max)
        SimulateDrag(world, thumb, new Vector2(500, 0));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        var maxScroll = scrollable.GetMaxScroll(new Vector2(400, 200));
        Assert.True(scrollable.ScrollPosition.X.ApproximatelyEquals(maxScroll.X));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Scrollbar_DragWithDeletedThumb_DoesNotCrash()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Delete thumb entity
        world.Despawn(thumb);

        // Create drag event for dead entity - should not crash
        var dragEvent = new UIDragEvent(thumb, Vector2.Zero, new Vector2(0, 50));
        world.Send(dragEvent);
        system.Update(0);
    }

    [Fact]
    public void Scrollbar_DragElementWithoutScrollbarThumb_IsIgnored()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);

        // Create an element without UIScrollbarThumb component
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 100))
            .Build();

        // Should not crash or throw
        var dragEvent = new UIDragEvent(element, Vector2.Zero, new Vector2(0, 50));
        world.Send(dragEvent);
        system.Update(0);
    }

    [Fact]
    public void Scrollbar_ScrollViewWithoutUIRect_IsIgnored()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        // Create scroll view without UIRect
        var scrollView = world.Spawn()
            .With(UIElement.Default)
            .With(new UIScrollable
            {
                VerticalScroll = true,
                ContentSize = new Vector2(200, 800),
                ScrollPosition = Vector2.Zero
            })
            // No UIRect!
            .Build();

        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 20, 40))
            .With(new UIScrollbarThumb(scrollView, isVertical: true))
            .Build();

        // Should not crash
        var dragEvent = new UIDragEvent(thumb, Vector2.Zero, new Vector2(0, 50));
        world.Send(dragEvent);
        system.Update(0);
    }

    [Fact]
    public void Scrollbar_ScrollViewWithoutUIScrollable_IsIgnored()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        // Create scroll view without UIScrollable
        var scrollView = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 400))
            // No UIScrollable!
            .Build();

        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 20, 40))
            .With(new UIScrollbarThumb(scrollView, isVertical: true))
            .Build();

        // Should not crash
        var dragEvent = new UIDragEvent(thumb, Vector2.Zero, new Vector2(0, 50));
        world.Send(dragEvent);
        system.Update(0);
    }

    [Fact]
    public void Scrollbar_Dispose_CleansUpSubscriptions()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Dispose should not throw
        system.Dispose();

        // After dispose, events should not be processed
        var dragEvent = new UIDragEvent(thumb, Vector2.Zero, new Vector2(0, 50));
        world.Send(dragEvent);

        // Scroll position should still be 0 since event was not processed
        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.True(scrollable.ScrollPosition.Y.ApproximatelyEquals(0f));
    }

    [Fact]
    public void VerticalScrollbar_ZeroViewportHeight_DoesNotScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        // Create scroll view with zero viewport height
        var scrollView = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(0, 0, 200, 0) // Zero height
            })
            .With(new UIScrollable
            {
                VerticalScroll = true,
                ContentSize = new Vector2(200, 800),
                ScrollPosition = Vector2.Zero
            })
            .Build();

        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 20, 40))
            .With(new UIScrollbarThumb(scrollView, isVertical: true))
            .Build();

        // Should not crash or scroll
        var dragEvent = new UIDragEvent(thumb, Vector2.Zero, new Vector2(0, 50));
        world.Send(dragEvent);
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.True(scrollable.ScrollPosition.Y.ApproximatelyEquals(0f));
    }

    [Fact]
    public void HorizontalScrollbar_ZeroViewportWidth_DoesNotScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        // Create scroll view with zero viewport width
        var scrollView = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                ComputedBounds = new Rectangle(0, 0, 0, 200) // Zero width
            })
            .With(new UIScrollable
            {
                HorizontalScroll = true,
                ContentSize = new Vector2(800, 200),
                ScrollPosition = Vector2.Zero
            })
            .Build();

        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 40, 20))
            .With(new UIScrollbarThumb(scrollView, isVertical: false))
            .Build();

        // Should not crash or scroll
        var dragEvent = new UIDragEvent(thumb, Vector2.Zero, new Vector2(50, 0));
        world.Send(dragEvent);
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.True(scrollable.ScrollPosition.X.ApproximatelyEquals(0f));
    }

    [Fact]
    public void Scrollbar_DragClampsToZero()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Set scroll position to some value
        ref var scrollable = ref world.Get<UIScrollable>(scrollView);
        scrollable.ScrollPosition = new Vector2(0, 100);

        // Drag up way past the beginning
        SimulateDrag(world, thumb, new Vector2(0, -200));
        system.Update(0);

        // Should clamp to 0
        Assert.True(scrollable.ScrollPosition.Y.ApproximatelyEquals(0f));
    }

    [Fact]
    public void HorizontalScrollbar_DragClampsToZero()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateHorizontalScrollView(world, 400, 200, 800);
        layout.Update(0);

        // Set scroll position to some value
        ref var scrollable = ref world.Get<UIScrollable>(scrollView);
        scrollable.ScrollPosition = new Vector2(100, 0);

        // Drag left way past the beginning
        SimulateDrag(world, thumb, new Vector2(-200, 0));
        system.Update(0);

        // Should clamp to 0
        Assert.True(scrollable.ScrollPosition.X.ApproximatelyEquals(0f));
    }

    [Fact]
    public void Scrollbar_DragWithDeletedScrollView_DoesNotCrash()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 800);
        layout.Update(0);

        // Delete scroll view
        world.Despawn(scrollView);

        // Drag should not crash
        SimulateDrag(world, thumb, new Vector2(0, 10));
        system.Update(0);
    }

    [Fact]
    public void Scrollbar_DragWithNoContentOverflow_DoesNotScroll()
    {
        using var world = new World();
        var system = new UIScrollbarSystem();
        world.AddSystem(system);

        // Content fits within viewport (no overflow)
        var layout = SetupLayout(world);
        var (scrollView, thumb) = CreateVerticalScrollView(world, 200, 400, 300);
        layout.Update(0);

        SimulateDrag(world, thumb, new Vector2(0, 50));
        system.Update(0);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        // Max scroll should be 0, so position stays at 0
        Assert.True(scrollable.ScrollPosition.Y.ApproximatelyEquals(0f));
    }

    #endregion

    #region Helper Methods

    private static UILayoutSystem SetupLayout(World world)
    {
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.Initialize(world);
        layoutSystem.SetScreenSize(800, 600);
        return layoutSystem;
    }

    private static (Entity ScrollView, Entity Thumb) CreateVerticalScrollView(
        World world, float width, float height, float contentHeight)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create scroll view with scrollable component
        var scrollView = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, width, height))
            .With(new UIScrollable
            {
                VerticalScroll = true,
                HorizontalScroll = false,
                ContentSize = new Vector2(width, contentHeight),
                ScrollPosition = Vector2.Zero
            })
            .Build();

        // Create scrollbar thumb
        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 20, 40))
            .With(new UIScrollbarThumb(scrollView, isVertical: true))
            .With(UIInteractable.Draggable())
            .Build();

        world.SetParent(scrollView, canvas);
        world.SetParent(thumb, canvas);

        return (scrollView, thumb);
    }

    private static (Entity ScrollView, Entity Thumb) CreateHorizontalScrollView(
        World world, float width, float height, float contentWidth)
    {
        // Create canvas root
        if (!world.TryGetExtension<UIContext>(out var uiContext))
        {
            uiContext = new UIContext(world);
            world.SetExtension(uiContext);
        }
        var canvas = uiContext.CreateCanvas();

        // Create scroll view with scrollable component
        var scrollView = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, width, height))
            .With(new UIScrollable
            {
                VerticalScroll = false,
                HorizontalScroll = true,
                ContentSize = new Vector2(contentWidth, height),
                ScrollPosition = Vector2.Zero
            })
            .Build();

        // Create scrollbar thumb
        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 40, 20))
            .With(new UIScrollbarThumb(scrollView, isVertical: false))
            .With(UIInteractable.Draggable())
            .Build();

        world.SetParent(scrollView, canvas);
        world.SetParent(thumb, canvas);

        return (scrollView, thumb);
    }

    private static void SimulateDrag(World world, Entity entity, Vector2 delta)
    {
        var dragEvent = new UIDragEvent(entity, Vector2.Zero, delta);
        world.Send(dragEvent);
    }

    #endregion
}
