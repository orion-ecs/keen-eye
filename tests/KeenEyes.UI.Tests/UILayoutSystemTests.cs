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

    #region FitContent Size Mode Tests

    [Fact]
    public void FlexboxLayout_FitContentWidth_WithChildren_SumsChildWidths()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent width
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 100), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Horizontal(10))
            .Build();
        world.SetParent(fitContainer, outerContainer);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, fitContainer);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(75, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Should fit content: 50 + 10 (spacing) + 75 = 135
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(135f));
    }

    [Fact]
    public void FlexboxLayout_FitContentHeight_WithChildren_SumsChildHeights()
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
            .With(UILayout.Vertical(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent height
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 0), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Vertical(5))
            .Build();
        world.SetParent(fitContainer, outerContainer);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, fitContainer);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 40), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Should fit content: 30 + 5 (spacing) + 40 = 75
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(75f));
    }

    [Fact]
    public void FlexboxLayout_FitContentWidth_Horizontal_UsesMaxChildWidth()
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
            .With(UILayout.Vertical(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent width but vertical layout
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 100), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Vertical(5))
            .Build();
        world.SetParent(fitContainer, outerContainer);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, fitContainer);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(80, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Vertical layout with FitContent width = max child width = 80
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(80f));
    }

    [Fact]
    public void FlexboxLayout_FitContentHeight_Horizontal_UsesMaxChildHeight()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent height but horizontal layout
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 0), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(5))
            .Build();
        world.SetParent(fitContainer, outerContainer);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, fitContainer);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 60), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Horizontal layout with FitContent height = max child height = 60
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(60f));
    }

    [Fact]
    public void FlexboxLayout_FitContent_WithPadding_IncludesPadding()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent and padding
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 0), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(0))
            .With(new UIStyle { Padding = new UIEdges(10, 15, 10, 15) }) // left, top, right, bottom
            .Build();
        world.SetParent(fitContainer, outerContainer);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Width = child (50) + padding (10 + 10) = 70
        // Height = child (30) + padding (15 + 15) = 60
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(70f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(60f));
    }

    [Fact]
    public void FlexboxLayout_FitContent_NoChildren_ReturnsPaddingOnly()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent and padding but no children
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 0), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(0))
            .With(new UIStyle { Padding = new UIEdges(20, 25, 20, 25) })
            .Build();
        world.SetParent(fitContainer, outerContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Width = padding only (20 + 20) = 40
        // Height = padding only (25 + 25) = 50
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(40f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(50f));
    }

    #endregion

    #region Vertical Wrap Layout Tests

    [Fact]
    public void FlexboxLayout_VerticalWrap_WrapsToNextColumn()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(400, 150);

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
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        // Create 3 children of 50px height each - only 2 fit per column (50 + 10 + 50 = 110 < 150)
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

        var child3 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child3, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);
        ref readonly var rect3 = ref world.Get<UIRect>(child3);

        // First two in column 1 (x=0), third wraps to column 2 (x = 50 + 10 = 60)
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect3.ComputedBounds.X.ApproximatelyEquals(60f));
    }

    [Fact]
    public void FlexboxLayout_VerticalWrap_WithCenter_CentersEachColumn()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(400, 300);

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
                MainAxisAlign = LayoutAlign.Center,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Single child centered vertically in 300px = (300 - 100) / 2 = 100
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(100f));
    }

    #endregion

    #region Anchor Layout Edge Cases

    [Fact]
    public void AnchorLayout_DifferentAnchors_StretchesBetween()
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

        var stretched = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.25f, 0.25f),
                AnchorMax = new Vector2(0.75f, 0.75f),
                Offset = UIEdges.Zero
            })
            .Build();
        world.SetParent(stretched, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(stretched);

        // Should stretch between 25% and 75% of parent
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(200f)); // 0.25 * 800
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(150f)); // 0.25 * 600
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(400f)); // (0.75 - 0.25) * 800
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(300f)); // (0.75 - 0.25) * 600
    }

    [Fact]
    public void AnchorLayout_WithOffset_AppliesOffsetCorrectly()
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
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 0),
                AnchorMax = new Vector2(1, 1),
                Offset = new UIEdges(20, 30, 20, 30) // left, top, right, bottom
            })
            .Build();
        world.SetParent(element, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(element);

        // Should have offsets applied
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(20f));
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(30f));
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(760f)); // 800 - 20 - 20
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(540f)); // 600 - 30 - 30
    }

    [Fact]
    public void AnchorLayout_WithPivot_OffsetsByPivot()
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

        // Element anchored at center with center pivot
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(100, 50),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .Build();
        world.SetParent(element, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(element);

        // Centered at 400, 300 with size 100, 50
        // With 0.5 pivot: x = 400 - 0.5 * 100 = 350, y = 300 - 0.5 * 50 = 275
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(350f));
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(275f));
    }

    [Fact]
    public void AnchorLayout_TopLeftPivot_PositionsFromTopLeft()
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

        // Element anchored at center with top-left pivot
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0, 0), // Top-left pivot
                Size = new Vector2(100, 50),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .Build();
        world.SetParent(element, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(element);

        // Anchored at 400, 300 with top-left pivot means element starts there
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(400f));
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(300f));
    }

    [Fact]
    public void AnchorLayout_BottomRightPivot_PositionsFromBottomRight()
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

        // Element anchored at center with bottom-right pivot
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(1, 1), // Bottom-right pivot
                Size = new Vector2(100, 50),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .Build();
        world.SetParent(element, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(element);

        // Anchored at 400, 300 with bottom-right pivot
        // x = 400 - 1.0 * 100 = 300, y = 300 - 1.0 * 50 = 250
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(300f));
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(250f));
    }

    #endregion

    #region Menu Exclusion Tests

    [Fact]
    public void FlexboxLayout_FitContent_ExcludesMenusFromMeasurement()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 0), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(fitContainer, outerContainer);

        // Regular child
        var regularChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(regularChild, fitContainer);

        // Menu child (should be excluded from measurement)
        var menuChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 200), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UIMenu()) // Menu component
            .Build();
        world.SetParent(menuChild, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Should only include regular child, not menu
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(50f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(30f));
    }

    #endregion

    #region Hidden Child Tests

    [Fact]
    public void FlexboxLayout_HiddenChildren_ExcludedFromLayout()
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

        var visibleChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(visibleChild, container);

        var hiddenChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(hiddenChild, container);

        var anotherVisibleChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(anotherVisibleChild, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(visibleChild);
        ref readonly var rect2 = ref world.Get<UIRect>(anotherVisibleChild);

        // Hidden child should be skipped, so second visible child at x = 100 + 10 = 110
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(110f));
    }

    [Fact]
    public void FlexboxLayout_FitContent_ExcludesHiddenChildren()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Container with FitContent
        var fitContainer = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 0), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(fitContainer, outerContainer);

        // Visible child
        var visibleChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 30), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(visibleChild, fitContainer);

        // Hidden child
        var hiddenChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 200), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(hiddenChild, fitContainer);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(fitContainer);

        // Should only include visible child
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(50f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(30f));
    }

    #endregion

    #region Fill Size With Multiple Children Tests

    [Fact]
    public void FlexboxLayout_MixedFillAndFixed_DistributesCorrectly()
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

        var fixedChild1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fixedChild1, container);

        var fillChild1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fillChild1, container);

        var fillChild2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fillChild2, container);

        var fixedChild2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(fixedChild2, container);

        layoutSystem.Update(0);

        ref readonly var rectFixed1 = ref world.Get<UIRect>(fixedChild1);
        ref readonly var rectFill1 = ref world.Get<UIRect>(fillChild1);
        ref readonly var rectFill2 = ref world.Get<UIRect>(fillChild2);
        ref readonly var rectFixed2 = ref world.Get<UIRect>(fixedChild2);

        // Fixed = 100 + 100 = 200, remaining = 600 - 200 = 400, each fill = 200
        Assert.True(rectFixed1.ComputedBounds.Width.ApproximatelyEquals(100f));
        Assert.True(rectFill1.ComputedBounds.Width.ApproximatelyEquals(200f));
        Assert.True(rectFill2.ComputedBounds.Width.ApproximatelyEquals(200f));
        Assert.True(rectFixed2.ComputedBounds.Width.ApproximatelyEquals(100f));

        // Positions should be consecutive
        Assert.True(rectFixed1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rectFill1.ComputedBounds.X.ApproximatelyEquals(100f));
        Assert.True(rectFill2.ComputedBounds.X.ApproximatelyEquals(300f));
        Assert.True(rectFixed2.ComputedBounds.X.ApproximatelyEquals(500f));
    }

    [Fact]
    public void FlexboxLayout_FillWithSpacing_AccountsForSpacing()
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
            .With(UILayout.Horizontal(20))
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

        ref readonly var rectFill = ref world.Get<UIRect>(fillChild);

        // Fixed = 100, spacing = 20, remaining = 600 - 100 - 20 = 480
        Assert.True(rectFill.ComputedBounds.Width.ApproximatelyEquals(480f));
    }

    #endregion

    #region Percentage Size Mode Tests

    [Fact]
    public void FlexboxLayout_PercentageWidth_InVerticalLayout_CalculatesFromContainer()
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
            .With(UILayout.Vertical(0))
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 100), WidthMode = UISizeMode.Percentage, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // 50% of 800 = 400
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(400f));
    }

    [Fact]
    public void FlexboxLayout_PercentageHeight_InHorizontalLayout_CalculatesFromContainer()
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
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Percentage })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // 50% of 600 = 300
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(300f));
    }

    #endregion

    #region Children Without UIRect Tests

    [Fact]
    public void AnchorLayout_ChildWithoutUIRect_Skipped()
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

        // Child without UIRect (should be skipped)
        var childNoRect = world.Spawn()
            .With(UIElement.Default)
            .Build();
        world.SetParent(childNoRect, parent);

        // Child with UIRect (should be processed)
        var childWithRect = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(50, 50, 100, 100))
            .Build();
        world.SetParent(childWithRect, parent);

        // Should not throw and should process the valid child
        var exception = Record.Exception(() => layoutSystem.Update(0));
        Assert.Null(exception);

        ref readonly var rect = ref world.Get<UIRect>(childWithRect);
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(100f));
    }

    [Fact]
    public void FlexboxLayout_ChildWithoutUIRect_Skipped()
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

        // Child without UIRect (should be skipped)
        var childNoRect = world.Spawn()
            .With(UIElement.Default)
            .Build();
        world.SetParent(childNoRect, container);

        // Child with UIRect (should be processed)
        var childWithRect = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(childWithRect, container);

        // Should not throw and should process the valid child
        var exception = Record.Exception(() => layoutSystem.Update(0));
        Assert.Null(exception);

        ref readonly var rect = ref world.Get<UIRect>(childWithRect);
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(100f));
    }

    #endregion

    #region Single Child SpaceBetween Tests

    [Fact]
    public void FlexboxLayout_SpaceBetween_SingleChild_StartsAtBeginning()
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

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // With single child, SpaceBetween should place child at start
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(0f));
    }

    [Fact]
    public void FlexboxLayout_SpaceAround_SingleChild_CentersChild()
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

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // With single child, SpaceAround places child with equal space on both sides
        // Total space = 600 - 100 = 500, space per child = 500 / 1 = 500, start at 500/2 = 250
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(250f));
    }

    [Fact]
    public void FlexboxLayout_SpaceEvenly_SingleChild_CentersChild()
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

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // With single child, SpaceEvenly: space = (600 - 100) / 2 = 250, start at 250
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(250f));
    }

    #endregion

    #region Nested FitContent Tests

    [Fact]
    public void FlexboxLayout_NestedFitContent_MeasuresRecursively()
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
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(outerContainer, canvas);

        // Outer FitContent container
        var outerFit = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 0), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(10))
            .Build();
        world.SetParent(outerFit, outerContainer);

        // Inner FitContent container
        var innerFit = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 0), WidthMode = UISizeMode.FitContent, HeightMode = UISizeMode.FitContent })
            .With(UILayout.Horizontal(5))
            .Build();
        world.SetParent(innerFit, outerFit);

        // Innermost children
        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(30, 20), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, innerFit);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(40, 25), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, innerFit);

        layoutSystem.Update(0);

        ref readonly var innerRect = ref world.Get<UIRect>(innerFit);
        ref readonly var outerRect = ref world.Get<UIRect>(outerFit);

        // Inner: 30 + 5 + 40 = 75 width, max(20, 25) = 25 height
        Assert.True(innerRect.ComputedBounds.Width.ApproximatelyEquals(75f));
        Assert.True(innerRect.ComputedBounds.Height.ApproximatelyEquals(25f));

        // Outer contains only inner, so same size (no additional children or spacing)
        Assert.True(outerRect.ComputedBounds.Width.ApproximatelyEquals(75f));
        Assert.True(outerRect.ComputedBounds.Height.ApproximatelyEquals(25f));
    }

    #endregion
}
