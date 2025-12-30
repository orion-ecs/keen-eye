using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UILayoutSystem layout calculations.
/// </summary>
public class UILayoutSystemTests
{
    #region Anchor Layout Tests

    [Fact]
    public void AnchorLayout_FixedSizeCentered_ComputesCorrectBounds()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1280, 720);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var centered = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Centered(200, 100))
            .Build();
        world.SetParent(centered, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(centered);

        // Centered in 1280x720 with size 200x100
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(540f)); // (1280 - 200) / 2
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(310f)); // (720 - 100) / 2
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(200f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(100f));
    }

    [Fact]
    public void AnchorLayout_Stretch_FillsParent()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1280, 720);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var stretched = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .Build();
        world.SetParent(stretched, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(stretched);

        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(1280f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(720f));
    }

    [Fact]
    public void AnchorLayout_FixedPosition_UsesOffset()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1280, 720);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var fixedElement = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(50, 75, 200, 100))
            .Build();
        world.SetParent(fixedElement, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fixedElement);

        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(50f));
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(75f));
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(200f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(100f));
    }

    [Fact]
    public void AnchorLayout_HiddenElement_ExcludedFromLayout()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1280, 720);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var hidden = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 100) })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(hidden, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(hidden);

        // Hidden elements should not have their bounds computed
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(0f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(0f));
    }

    #endregion

    #region Flexbox Layout Tests

    [Fact]
    public void FlexboxLayout_HorizontalStart_PositionsChildrenLeftToRight()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1280, 720);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Horizontal(10))
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(150, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect1.ComputedBounds.Width.ApproximatelyEquals(100f));

        // Second child should be after first + spacing
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(110f)); // 100 + 10 spacing
        Assert.True(rect2.ComputedBounds.Width.ApproximatelyEquals(150f));
    }

    [Fact]
    public void FlexboxLayout_VerticalStart_PositionsChildrenTopToBottom()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1280, 720);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Vertical(5))
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 40), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(rect1.ComputedBounds.Height.ApproximatelyEquals(30f));

        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(35f)); // 30 + 5 spacing
        Assert.True(rect2.ComputedBounds.Height.ApproximatelyEquals(40f));
    }

    [Fact]
    public void FlexboxLayout_Center_CentersChildren()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.HorizontalCentered(0))
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Child should be centered horizontally and vertically in 800x600
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(300f)); // (800 - 200) / 2
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(250f)); // (600 - 100) / 2
    }

    [Fact]
    public void FlexboxLayout_End_PositionsChildrenAtEnd()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.End,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Child should be at the right edge
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(700f)); // 800 - 100
    }

    [Fact]
    public void FlexboxLayout_SpaceBetween_DistributesEvenly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(600, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // First at start, second at end (600 - 100 = 500)
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(500f));
    }

    [Fact]
    public void FlexboxLayout_ReverseOrder_ReversesChildren()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 10,
                ReverseOrder = true
            })
            .Build();
        world.SetParent(container, canvas);

        var childA = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(childA, container);

        var childB = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(150, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(childB, container);

        layoutSystem.Update(0);

        ref readonly var rectA = ref world.Get<UIRect>(childA);
        ref readonly var rectB = ref world.Get<UIRect>(childB);

        // With reverse order, B comes first (at x=0), A comes after (at x=160)
        Assert.True(rectB.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rectA.ComputedBounds.X.ApproximatelyEquals(160f)); // 150 + 10
    }

    [Fact]
    public void FlexboxLayout_WithPadding_RespectsStylePadding()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Horizontal(0))
            .With(new UIStyle { Padding = new UIEdges(20, 30, 20, 30) })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Child should start at padding offset
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(20f)); // Left padding
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(30f)); // Top padding
    }

    [Fact]
    public void FlexboxLayout_PercentageWidth_CalculatesCorrectly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 500);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 100), WidthMode = UISizeMode.Percentage, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // 50% of 1000 = 500
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(500f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(100f));
    }

    #endregion

    #region Nested Layout Tests

    [Fact]
    public void NestedLayout_ChildWithOwnChildren_CalculatesRecursively()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var outerContainer = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Vertical(10))
            .Build();
        world.SetParent(outerContainer, canvas);

        var innerContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 200), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Horizontal(5))
            .Build();
        world.SetParent(innerContainer, outerContainer);

        var innerChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(innerChild, innerContainer);

        layoutSystem.Update(0);

        ref readonly var innerContainerRect = ref world.Get<UIRect>(innerContainer);
        ref readonly var innerChildRect = ref world.Get<UIRect>(innerChild);

        // Inner container at (0, 0) with size (400, 200)
        Assert.True(innerContainerRect.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(innerContainerRect.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(innerContainerRect.ComputedBounds.Width.ApproximatelyEquals(400f));

        // Inner child at (0, 0) relative to inner container
        Assert.True(innerChildRect.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(innerChildRect.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(innerChildRect.ComputedBounds.Width.ApproximatelyEquals(100f));
    }

    #endregion

    #region Screen Size Tests

    [Fact]
    public void SetScreenSize_UpdatesRootBounds()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        layoutSystem.SetScreenSize(1920, 1080);
        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(canvas);

        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(1920f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(1080f));
    }

    #endregion

    #region Layout Dirty Tag Tests

    [Fact]
    public void LayoutDirtyTag_ClearedAfterLayout()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 100))
            .With(new UILayoutDirtyTag())
            .Build();
        world.SetParent(element, canvas);

        Assert.True(world.Has<UILayoutDirtyTag>(element));

        layoutSystem.Update(0);

        Assert.False(world.Has<UILayoutDirtyTag>(element));
    }

    #endregion

    #region SpaceAround and SpaceEvenly Tests

    [Fact]
    public void FlexboxLayout_SpaceAround_DistributesWithHalfSpaceAtEdges()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(600, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceAround,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // Space around: total space = 600 - 200 = 400, divided by 2 children = 200 each
        // First child starts at 100 (half of 200), second at 100 + 100 + 200 = 400
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(100f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(400f));
    }

    [Fact]
    public void FlexboxLayout_SpaceEvenly_DistributesEquallyIncludingEdges()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(600, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceEvenly,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // Space evenly: total space = 600 - 200 = 400, divided by (2+1) = 133.33 each
        // First child at ~133, second at ~133 + 100 + 133 = ~366
        Assert.True(Math.Abs(rect1.ComputedBounds.X - 133.33f) < 1f);
        Assert.True(Math.Abs(rect2.ComputedBounds.X - 366.66f) < 1f);
    }

    #endregion

    #region Fill Size Mode Tests

    [Fact]
    public void FlexboxLayout_FillWidth_DistributesRemainingSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(600, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Horizontal(10))
            .Build();
        world.SetParent(container, canvas);

        var fixedChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fixedChild, container);

        var fillChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fillChild, container);

        layoutSystem.Update(0);

        ref readonly var fixedRect = ref world.Get<UIRect>(fixedChild);
        ref readonly var fillRect = ref world.Get<UIRect>(fillChild);

        // Fixed child = 100, spacing = 10, remaining = 600 - 100 - 10 = 490
        Assert.True(fixedRect.ComputedBounds.Width.ApproximatelyEquals(100f));
        Assert.True(fillRect.ComputedBounds.Width.ApproximatelyEquals(490f));
    }

    [Fact]
    public void FlexboxLayout_MultipleFillChildren_SplitsEvenly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(600, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(container, canvas);

        var fill1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fill1, container);

        var fill2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fill2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(fill1);
        ref readonly var rect2 = ref world.Get<UIRect>(fill2);

        // Each fill child gets half of 600 = 300
        Assert.True(rect1.ComputedBounds.Width.ApproximatelyEquals(300f));
        Assert.True(rect2.ComputedBounds.Width.ApproximatelyEquals(300f));
    }

    [Fact]
    public void FlexboxLayout_VerticalFillHeight_DistributesRemainingSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(400, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Vertical(10))
            .Build();
        world.SetParent(container, canvas);

        var fixedChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fixedChild, container);

        var fillChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 0), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fill })
            .Build();
        world.SetParent(fillChild, container);

        layoutSystem.Update(0);

        ref readonly var fillRect = ref world.Get<UIRect>(fillChild);

        // Fixed = 100, spacing = 10, remaining = 600 - 100 - 10 = 490
        Assert.True(fillRect.ComputedBounds.Height.ApproximatelyEquals(490f));
    }

    #endregion

    #region Wrapping Layout Tests

    [Fact]
    public void FlexboxLayout_Wrap_WrapsToNextLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(250, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        // Create 3 children of 100px width each - only 2 fit per line (100 + 10 + 100 = 210 < 250)
        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        var child3 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child3, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);
        ref readonly var rect3 = ref world.Get<UIRect>(child3);

        // First two on line 1, third wraps to line 2
        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(rect3.ComputedBounds.Y.ApproximatelyEquals(60f)); // 50 + 10 spacing
    }

    [Fact]
    public void FlexboxLayout_WrapWithCenter_CentersEachLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Center,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);

        // Single child centered in 300px = (300 - 100) / 2 = 100
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(100f));
    }

    [Fact]
    public void FlexboxLayout_WrapWithEnd_AlignsToEnd()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.End,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);

        // Single child at end = 300 - 100 = 200
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(200f));
    }

    [Fact]
    public void FlexboxLayout_WrapSpaceBetween_SpacesWithinLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // SpaceBetween: first at 0, second at 300 - 50 = 250
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(250f));
    }

    [Fact]
    public void FlexboxLayout_WrapSpaceAround_SpacesWithHalfAtEdges()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceAround,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // SpaceAround: space = (300 - 100) / 2 = 100 each
        // First at 50 (half of 100), second at 50 + 50 + 100 = 200
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(50f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(200f));
    }

    [Fact]
    public void FlexboxLayout_WrapSpaceEvenly_SpacesEquallyIncludingEdges()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceEvenly,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // SpaceEvenly: space = (300 - 100) / 3 = 66.67 each
        // First at ~66.67, second at ~66.67 + 50 + 66.67 = ~183.33
        Assert.True(Math.Abs(rect1.ComputedBounds.X - 66.67f) < 1f);
        Assert.True(Math.Abs(rect2.ComputedBounds.X - 183.33f) < 1f);
    }

    [Fact]
    public void FlexboxLayout_WrapCrossAxisCenter_CentersWithinLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 60), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // Line height is 60 (max of children), child1 (30px) should be centered
        // Center offset = (60 - 30) / 2 = 15
        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(15f));
        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(0f));
    }

    [Fact]
    public void FlexboxLayout_WrapCrossAxisEnd_AlignsToLineBottom()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(300, 400);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 60), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // Line height is 60, child1 (30px) should be at bottom: 60 - 30 = 30
        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(30f));
        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(0f));
    }

    #endregion

    #region CrossAxis Alignment Tests

    [Fact]
    public void FlexboxLayout_CrossAxisEnd_AlignsChildrenToBottom()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Child should be at bottom: 600 - 50 = 550
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(550f));
    }

    [Fact]
    public void FlexboxLayout_VerticalCrossAxisEnd_AlignsChildrenToRight()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Child should be at right: 800 - 100 = 700
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(700f));
    }

    #endregion

    #region Percentage Height Tests

    [Fact]
    public void FlexboxLayout_PercentageHeight_CalculatesCorrectly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 500);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Vertical(0))
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 25), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Percentage })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // 25% of 500 = 125
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(125f));
    }

    #endregion

    #region Hidden Root Canvas Tests

    [Fact]
    public void HiddenRootCanvas_DoesNotProcessChildren()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .With(new UIHiddenTag())
            .Build();

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(50, 50, 100, 100))
            .Build();
        world.SetParent(child, canvas);

        layoutSystem.Update(0);

        ref readonly var canvasRect = ref world.Get<UIRect>(canvas);
        ref readonly var childRect = ref world.Get<UIRect>(child);

        // Hidden canvas should not have its bounds computed
        Assert.True(canvasRect.ComputedBounds.Width.ApproximatelyEquals(0f));
        // Child should also not have bounds computed
        Assert.True(childRect.ComputedBounds.Width.ApproximatelyEquals(0f));
    }

    #endregion

    #region Empty Layout Tests

    [Fact]
    public void FlexboxLayout_EmptyContainer_DoesNotThrow()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(UILayout.Horizontal(10))
            .Build();
        world.SetParent(container, canvas);

        // Should not throw with empty container
        var exception = Record.Exception(() => layoutSystem.Update(0));
        Assert.Null(exception);
    }

    [Fact]
    public void AnchorLayout_NoChildren_DoesNotThrow()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(800, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .Build();
        world.SetParent(parent, canvas);

        // Should not throw with no children
        var exception = Record.Exception(() => layoutSystem.Update(0));
        Assert.Null(exception);
    }

    #endregion
}
