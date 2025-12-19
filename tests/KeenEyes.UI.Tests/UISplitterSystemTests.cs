using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UISplitterSystem splitter drag operations.
/// </summary>
public class UISplitterSystemTests
{
    #region Horizontal Splitter Tests

    [Fact]
    public void HorizontalSplitter_DragRight_IncreasesRatio()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(handle, new Vector2(250, 150), new Vector2(50, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);

        Assert.True(splitter.SplitRatio > 0.5f);
    }

    [Fact]
    public void HorizontalSplitter_DragLeft_DecreasesRatio()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(handle, new Vector2(150, 150), new Vector2(-50, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);

        Assert.True(splitter.SplitRatio < 0.5f);
    }

    #endregion

    #region Vertical Splitter Tests

    [Fact]
    public void VerticalSplitter_DragDown_IncreasesRatio()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Vertical,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(handle, new Vector2(200, 190), new Vector2(0, 40));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);

        Assert.True(splitter.SplitRatio > 0.5f);
    }

    [Fact]
    public void VerticalSplitter_DragUp_DecreasesRatio()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Vertical,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(handle, new Vector2(200, 110), new Vector2(0, -40));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);

        Assert.True(splitter.SplitRatio < 0.5f);
    }

    #endregion

    #region Minimum Size Constraint Tests

    [Fact]
    public void Splitter_RespectsMinFirstPane()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.3f,
                HandleSize = 10,
                MinFirstPane = 100,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(handle, new Vector2(0, 150), new Vector2(-200, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);
        float totalWidth = 400 - 10;
        float firstPaneWidth = totalWidth * splitter.SplitRatio;

        Assert.True(firstPaneWidth >= 100f);
    }

    [Fact]
    public void Splitter_RespectsMinSecondPane()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.7f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 100
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        // UIDragEvent(Element, Position, Delta) - Delta is the movement amount
        var dragEvent = new UIDragEvent(handle, new Vector2(500, 150), new Vector2(200, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);
        float totalWidth = 400 - 10;
        float secondPaneWidth = totalWidth * (1f - splitter.SplitRatio);

        // Use epsilon tolerance for float comparison (MinSecondPane = 100)
        Assert.True(secondPaneWidth >= 100f - 0.01f);
    }

    #endregion

    #region Ratio Bounds Tests

    [Fact]
    public void Splitter_RatioClampsToZero()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.1f,
                HandleSize = 10,
                MinFirstPane = 0,
                MinSecondPane = 0
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        var dragEvent = new UIDragEvent(handle, new Vector2(-100, 150), new Vector2(-500, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);

        Assert.True(splitter.SplitRatio >= 0f);
    }

    [Fact]
    public void Splitter_RatioClampsToOne()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.9f,
                HandleSize = 10,
                MinFirstPane = 0,
                MinSecondPane = 0
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        var dragEvent = new UIDragEvent(handle, new Vector2(800, 150), new Vector2(500, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        ref readonly var splitter = ref world.Get<UISplitter>(container);

        Assert.True(splitter.SplitRatio <= 1f);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Splitter_FiresSplitterChangedEvent()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        bool eventFired = false;
        float oldRatio = 0f;
        float newRatio = 0f;

        world.Subscribe<UISplitterChangedEvent>(e =>
        {
            if (e.Splitter == container)
            {
                eventFired = true;
                oldRatio = e.OldRatio;
                newRatio = e.NewRatio;
            }
        });

        var dragEvent = new UIDragEvent(handle, new Vector2(250, 150), new Vector2(50, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        Assert.True(eventFired);
        Assert.True(oldRatio.ApproximatelyEquals(0.5f));
        Assert.True(newRatio > 0.5f);
    }

    [Fact]
    public void Splitter_MarksLayoutDirty()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        var dragEvent = new UIDragEvent(handle, new Vector2(250, 150), new Vector2(50, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        Assert.True(world.Has<UILayoutDirtyTag>(container));
    }

    #endregion

    #region No-Op Tests

    [Fact]
    public void Splitter_SmallDragChange_DoesNotFireEvent()
    {
        using var world = new World();
        var splitterSystem = new UISplitterSystem();
        world.AddSystem(splitterSystem);

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 400, 300) })
            .With(new UISplitter
            {
                Orientation = LayoutDirection.Horizontal,
                SplitRatio = 0.5f,
                HandleSize = 10,
                MinFirstPane = 50,
                MinSecondPane = 50
            })
            .Build();

        var handle = world.Spawn()
            .With(UIElement.Default)
            .With(new UISplitterHandle(container))
            .Build();

        bool eventFired = false;
        world.Subscribe<UISplitterChangedEvent>(e => eventFired = true);

        var dragEvent = new UIDragEvent(handle, new Vector2(200, 150), new Vector2(0.00001f, 0));
        world.Send(dragEvent);

        splitterSystem.Update(0);

        Assert.False(eventFired);
    }

    #endregion
}
