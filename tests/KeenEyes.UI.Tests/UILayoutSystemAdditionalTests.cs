using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Additional tests for UILayoutSystem to improve coverage.
/// </summary>
public sealed class UILayoutSystemAdditionalTests
{
    #region Wrapping Layout Tests

    [Fact]
    public void FlexboxLayout_WithWrap_WrapsChildrenToNextLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(300, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        // Create 3 children: 100px wide each
        // Container is 300px, so we can fit 2 (100 + 10 + 100 = 210) but 3rd won't fit (210 + 10 + 100 = 320 > 300)
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

        // First two children should be on first line
        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(0f));

        // Third child should wrap to second line
        Assert.True(rect3.ComputedBounds.Y.ApproximatelyEquals(60f)); // 50 height + 10 spacing
        Assert.True(rect3.ComputedBounds.X.ApproximatelyEquals(0f)); // Back to start
    }

    [Fact]
    public void FlexboxLayout_VerticalWrap_WrapsToNextColumn()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(600, 200), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        // Create 3 children: 80px tall each
        // Container is 200px, so we can fit 2 (80 + 10 + 80 = 170) but 3rd won't fit (170 + 10 + 80 = 260 > 200)
        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 80), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 80), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        var child3 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(50, 80), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child3, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);
        ref readonly var rect3 = ref world.Get<UIRect>(child3);

        // First two children should be in first column
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(0f));

        // Third child should wrap to second column
        Assert.True(rect3.ComputedBounds.X.ApproximatelyEquals(60f)); // 50 width + 10 spacing
        Assert.True(rect3.ComputedBounds.Y.ApproximatelyEquals(0f)); // Back to top
    }

    [Fact]
    public void FlexboxLayout_WrapWithCenterAlign_CentersEachLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Center,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        // Two children that fit on one line
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

        // Total width = 100 + 10 + 100 = 210
        // Available = 400, so centered offset = (400 - 210) / 2 = 95
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(95f));
    }

    [Fact]
    public void FlexboxLayout_WrapWithSpaceBetween_DistributesSpaceBetweenItems()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
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

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // SpaceBetween: (400 - 200) / (2 - 1) = 200 spacing
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(0f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(300f)); // 100 + 200
    }

    [Fact]
    public void FlexboxLayout_WrapWithSpaceAround_DistributesSpaceAroundItems()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(500, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceAround,
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

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // SpaceAround: total space = 500 - 200 = 300
        // spacing per item = 300 / 2 = 150
        // first item offset = 150 / 2 = 75
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(75f));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(325f)); // 75 + 100 + 150
    }

    [Fact]
    public void FlexboxLayout_WrapWithSpaceEvenly_DistributesSpaceEvenly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(600, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceEvenly,
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

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // SpaceEvenly: total space = 600 - 200 = 400
        // gaps = 2 + 1 = 3, spacing = 400 / 3 = 133.33
        float expectedSpacing = 400f / 3f;
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(expectedSpacing));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(expectedSpacing + 100f + expectedSpacing));
    }

    [Fact]
    public void FlexboxLayout_WrapWithCrossAxisCenter_CentersItemsInLine()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 10,
                Wrap = true
            })
            .Build();
        world.SetParent(container, canvas);

        // Create children with different heights
        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // Line height is max(50, 100) = 100
        // child1 (50px tall) should be centered in 100px line: (100 - 50) / 2 = 25
        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(25f));
        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(0f));
    }

    [Fact]
    public void FlexboxLayout_WrapWithCrossAxisEnd_AlignsItemsToLineEnd()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                CrossAxisAlign = LayoutAlign.End,
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

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        // Line height is max(50, 100) = 100
        // child1 (50px tall) should be at end: 100 - 50 = 50
        Assert.True(rect1.ComputedBounds.Y.ApproximatelyEquals(50f));
        Assert.True(rect2.ComputedBounds.Y.ApproximatelyEquals(0f));
    }

    #endregion

    #region SpaceBetween/SpaceAround/SpaceEvenly Alignment Tests

    [Fact]
    public void FlexboxLayout_SpaceBetween_WithOneChild_StartsAtBeginning()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

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

        // With only one child, SpaceBetween behaves like Start
        Assert.True(rect.ComputedBounds.X.ApproximatelyEquals(0f));
    }

    [Fact]
    public void FlexboxLayout_SpaceAround_WithNoChildren_DoesNotCrash()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

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
                Spacing = 0
            })
            .Build();
        world.SetParent(container, canvas);

        // Should not throw
        layoutSystem.Update(0);
    }

    [Fact]
    public void FlexboxLayout_SpaceEvenly_DistributesSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(600, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceEvenly,
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

        // Total space = 600 - 200 = 400
        // Gaps = 3 (before, between, after), spacing = 400 / 3 ≈ 133.33
        float spacing = 400f / 3f;
        Assert.True(rect1.ComputedBounds.X.ApproximatelyEquals(spacing));
        Assert.True(rect2.ComputedBounds.X.ApproximatelyEquals(spacing + 100f + spacing));
    }

    #endregion

    #region Fill Size Mode Tests

    [Fact]
    public void FlexboxLayout_FillHorizontal_DistributesRemainingSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(600, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(container, canvas);

        // Fixed width child
        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        // Fill width child
        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        Assert.True(rect1.ComputedBounds.Width.ApproximatelyEquals(100f));
        // Remaining space = 600 - 100 = 500
        Assert.True(rect2.ComputedBounds.Width.ApproximatelyEquals(500f));
    }

    [Fact]
    public void FlexboxLayout_MultipleFillChildren_DividesSpaceEqually()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(600, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child2, container);

        var child3 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(0, 50), WidthMode = UISizeMode.Fill, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child3, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);
        ref readonly var rect3 = ref world.Get<UIRect>(child3);

        // Each should get 600 / 3 = 200
        Assert.True(rect1.ComputedBounds.Width.ApproximatelyEquals(200f));
        Assert.True(rect2.ComputedBounds.Width.ApproximatelyEquals(200f));
        Assert.True(rect3.ComputedBounds.Width.ApproximatelyEquals(200f));
    }

    [Fact]
    public void FlexboxLayout_FillVertical_DistributesRemainingSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Vertical(0))
            .Build();
        world.SetParent(container, canvas);

        var child1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 100), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 0), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fill })
            .Build();
        world.SetParent(child2, container);

        layoutSystem.Update(0);

        ref readonly var rect1 = ref world.Get<UIRect>(child1);
        ref readonly var rect2 = ref world.Get<UIRect>(child2);

        Assert.True(rect1.ComputedBounds.Height.ApproximatelyEquals(100f));
        // Remaining space = 600 - 100 = 500
        Assert.True(rect2.ComputedBounds.Height.ApproximatelyEquals(500f));
    }

    [Fact]
    public void FlexboxLayout_FillCrossAxis_FillsAvailableSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 600), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(UILayout.Horizontal(0))
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 0), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fill })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Cross axis is vertical, should fill container height
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(100f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(600f));
    }

    #endregion

    #region Cross-Axis Alignment Tests

    [Fact]
    public void FlexboxLayout_CrossAxisCenter_CentersChildrenVertically()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 200), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout { Direction = LayoutDirection.Horizontal, CrossAxisAlign = LayoutAlign.Center })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Centered vertically: (200 - 50) / 2 = 75
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(75f));
    }

    [Fact]
    public void FlexboxLayout_CrossAxisEnd_AlignsChildrenToBottom()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var container = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(400, 200), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UILayout { Direction = LayoutDirection.Horizontal, CrossAxisAlign = LayoutAlign.End })
            .Build();
        world.SetParent(container, canvas);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // Aligned to bottom: 200 - 50 = 150
        Assert.True(rect.ComputedBounds.Y.ApproximatelyEquals(150f));
    }

    #endregion

    #region Percentage Size Mode Tests

    [Fact]
    public void FlexboxLayout_PercentageHeight_CalculatesCorrectly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

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
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Percentage })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // 50% of 600 = 300
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(300f));
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(100f));
    }

    [Fact]
    public void FlexboxLayout_PercentageBothDimensions_CalculatesCorrectly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 800);

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
            .With(new UIRect { Size = new Vector2(25, 75), WidthMode = UISizeMode.Percentage, HeightMode = UISizeMode.Percentage })
            .Build();
        world.SetParent(child, container);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(child);

        // 25% of 1000 = 250, 75% of 800 = 600
        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(250f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(600f));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FlexboxLayout_NoChildren_DoesNotCrash()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

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

        // Should not throw with no children
        layoutSystem.Update(0);
    }

    [Fact]
    public void FlexboxLayout_ChildWithoutUIRect_IsIgnored()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

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

        // Child without UIRect component
        var child = world.Spawn()
            .With(UIElement.Default)
            .Build();
        world.SetParent(child, container);

        // Should not throw
        layoutSystem.Update(0);
    }

    [Fact]
    public void AnchorLayout_WithNegativeWidthOrHeight_ClampsToZero()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(100, 100);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        // Create element with offsets that would result in negative size
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Size = new Vector2(200, 200),
                Offset = new UIEdges(0, 0, 150, 150),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .Build();
        world.SetParent(element, canvas);

        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(element);

        // Width and height should be clamped to 0
        Assert.True(rect.ComputedBounds.Width >= 0);
        Assert.True(rect.ComputedBounds.Height >= 0);
    }

    [Fact]
    public void FlexboxLayout_AllChildrenHidden_ProcessesCorrectly()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

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
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(child1, container);

        var child2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(child2, container);

        // Should not throw
        layoutSystem.Update(0);
    }

    [Fact]
    public void RootCanvas_WithHiddenTag_IsNotProcessed()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.SetScreenSize(1000, 600);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .With(new UIHiddenTag())
            .Build();

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(100, 50), WidthMode = UISizeMode.Fixed, HeightMode = UISizeMode.Fixed })
            .Build();
        world.SetParent(child, canvas);

        layoutSystem.Update(0);

        ref readonly var childRect = ref world.Get<UIRect>(child);

        // Child should not have computed bounds since root is hidden
        Assert.True(childRect.ComputedBounds.Width.ApproximatelyEquals(0f));
    }

    #endregion
}
