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
}
