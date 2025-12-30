using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIHitTester class.
/// </summary>
public class UIHitTesterTests
{
    #region HitTest Tests

    [Fact]
    public void HitTest_WithNoElements_ReturnsNull()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var result = hitTester.HitTest(new Vector2(50, 50));

        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void HitTest_WithPositionInsideElement_ReturnsElement()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // Create a UI root with an element
        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 50) })
            .Build();
        world.SetParent(button, root);

        var result = hitTester.HitTest(new Vector2(150, 125));

        Assert.Equal(button, result);
    }

    [Fact]
    public void HitTest_WithPositionOutsideElement_ReturnsRoot()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 50) })
            .Build();
        world.SetParent(button, root);

        // Click outside button but inside root
        var result = hitTester.HitTest(new Vector2(50, 50));

        Assert.Equal(root, result);
    }

    [Fact]
    public void HitTest_WithNonRaycastTarget_SkipsElement()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var overlay = world.Spawn()
            .With(UIElement.NonInteractive) // RaycastTarget = false
            .With(new UIRect { ComputedBounds = new Rectangle(50, 50, 300, 300) })
            .Build();
        world.SetParent(overlay, root);

        var result = hitTester.HitTest(new Vector2(100, 100));

        // Should return root, not overlay
        Assert.Equal(root, result);
    }

    [Fact]
    public void HitTest_WithHiddenElement_SkipsElement()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var hidden = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(50, 50, 100, 100) })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(hidden, root);

        var result = hitTester.HitTest(new Vector2(75, 75));

        // Should return root, not hidden element
        Assert.Equal(root, result);
    }

    [Fact]
    public void HitTest_WithInvisibleElement_SkipsElement()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var invisible = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect { ComputedBounds = new Rectangle(50, 50, 100, 100) })
            .Build();
        world.SetParent(invisible, root);

        var result = hitTester.HitTest(new Vector2(75, 75));

        // Should return root, not invisible element
        Assert.Equal(root, result);
    }

    [Fact]
    public void HitTest_ChildOnTop_ReturnsChild()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 400, 300) })
            .Build();
        world.SetParent(parent, root);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(150, 150, 100, 50) })
            .Build();
        world.SetParent(child, parent);

        var result = hitTester.HitTest(new Vector2(175, 175));

        // Child should be on top
        Assert.Equal(child, result);
    }

    [Fact]
    public void HitTest_ZIndex_HigherValueOnTop()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var behind = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200), LocalZIndex = 0 })
            .Build();
        world.SetParent(behind, root);

        var inFront = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(150, 150, 100, 100), LocalZIndex = 10 })
            .Build();
        world.SetParent(inFront, root);

        var result = hitTester.HitTest(new Vector2(175, 175));

        // Higher z-index should be on top
        Assert.Equal(inFront, result);
    }

    #endregion

    #region HitTestAll Tests

    [Fact]
    public void HitTestAll_WithNoElements_ReturnsEmptyList()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var results = hitTester.HitTestAll(new Vector2(50, 50));

        Assert.Empty(results);
    }

    [Fact]
    public void HitTestAll_WithOverlappingElements_ReturnsAllSortedByDepth()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var layer1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(50, 50, 200, 200), LocalZIndex = 0 })
            .Build();
        world.SetParent(layer1, root);

        var layer2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200), LocalZIndex = 5 })
            .Build();
        world.SetParent(layer2, root);

        var results = hitTester.HitTestAll(new Vector2(150, 150));

        // Should contain all 3 elements, sorted by depth (topmost first)
        Assert.Equal(3, results.Count);
        Assert.Equal(layer2, results[0]); // Higher z-index first
        Assert.Equal(layer1, results[1]);
        Assert.Equal(root, results[2]);
    }

    [Fact]
    public void HitTestAll_WithHierarchy_ReturnsChildFirst()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 300, 300) })
            .Build();
        world.SetParent(parent, root);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(150, 150, 100, 100) })
            .Build();
        world.SetParent(child, parent);

        var results = hitTester.HitTestAll(new Vector2(175, 175));

        // Child should be first (higher depth)
        Assert.Equal(3, results.Count);
        Assert.Equal(child, results[0]);
        Assert.Equal(parent, results[1]);
        Assert.Equal(root, results[2]);
    }

    #endregion

    #region Root Canvas Edge Cases

    [Fact]
    public void HitTest_WithInvisibleRoot_SkipsRoot()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // Create an invisible root canvas
        var invisibleRoot = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var result = hitTester.HitTest(new Vector2(100, 100));

        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void HitTest_WithHiddenRoot_SkipsRoot()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // Create a hidden root canvas
        var hiddenRoot = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .With(new UIHiddenTag())
            .Build();

        var result = hitTester.HitTest(new Vector2(100, 100));

        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void HitTestAll_WithInvisibleRoot_SkipsRoot()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // Create an invisible root canvas
        var invisibleRoot = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        var results = hitTester.HitTestAll(new Vector2(100, 100));

        Assert.Empty(results);
    }

    [Fact]
    public void HitTestAll_WithHiddenRoot_SkipsRoot()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // Create a hidden root canvas
        var hiddenRoot = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .With(new UIHiddenTag())
            .Build();

        var results = hitTester.HitTestAll(new Vector2(100, 100));

        Assert.Empty(results);
    }

    #endregion

    #region Child Entity Edge Cases

    [Fact]
    public void HitTest_WithChildWithoutUIElement_SkipsChild()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        // Child without UIElement component (just UIRect)
        var nonUIChild = world.Spawn()
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200) })
            .Build();
        world.SetParent(nonUIChild, root);

        var result = hitTester.HitTest(new Vector2(150, 150));

        // Should hit the root, not the child without UIElement
        Assert.Equal(root, result);
    }

    [Fact]
    public void HitTest_WithChildWithoutUIRect_SkipsChild()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        // Child without UIRect component (just UIElement)
        var nonRectChild = world.Spawn()
            .With(UIElement.Default)
            .Build();
        world.SetParent(nonRectChild, root);

        var result = hitTester.HitTest(new Vector2(150, 150));

        // Should hit the root, not the child without UIRect
        Assert.Equal(root, result);
    }

    [Fact]
    public void HitTestAll_WithChildWithoutUIElement_SkipsChild()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        // Child without UIElement
        var nonUIChild = world.Spawn()
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200) })
            .Build();
        world.SetParent(nonUIChild, root);

        var results = hitTester.HitTestAll(new Vector2(150, 150));

        // Should only contain root
        Assert.Single(results);
        Assert.Equal(root, results[0]);
    }

    [Fact]
    public void HitTestAll_WithHiddenChild_SkipsChild()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        // Hidden child
        var hiddenChild = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200) })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(hiddenChild, root);

        var results = hitTester.HitTestAll(new Vector2(150, 150));

        // Should only contain root
        Assert.Single(results);
        Assert.Equal(root, results[0]);
    }

    [Fact]
    public void HitTestAll_WithInvisibleChild_SkipsChild()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        // Invisible child
        var invisibleChild = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200) })
            .Build();
        world.SetParent(invisibleChild, root);

        var results = hitTester.HitTestAll(new Vector2(150, 150));

        // Should only contain root
        Assert.Single(results);
        Assert.Equal(root, results[0]);
    }

    [Fact]
    public void HitTestAll_WithNonRaycastChild_ExcludesFromResults()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 1280, 720) })
            .With(new UIRootTag())
            .Build();

        // Non-raycast target child
        var nonRaycastChild = world.Spawn()
            .With(UIElement.NonInteractive)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200) })
            .Build();
        world.SetParent(nonRaycastChild, root);

        var results = hitTester.HitTestAll(new Vector2(150, 150));

        // Should only contain root
        Assert.Single(results);
        Assert.Equal(root, results[0]);
    }

    #endregion

    #region Multiple Root Canvas Tests

    [Fact]
    public void HitTest_WithMultipleRoots_ReturnsTopmostHit()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // First root
        var root1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 500, 500), LocalZIndex = 0 })
            .With(new UIRootTag())
            .Build();

        // Second root with higher z-index
        var root2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(200, 200, 500, 500), LocalZIndex = 10 })
            .With(new UIRootTag())
            .Build();

        var result = hitTester.HitTest(new Vector2(300, 300));

        // Should return root2 (higher z-index)
        Assert.Equal(root2, result);
    }

    [Fact]
    public void HitTestAll_WithMultipleRoots_ReturnsAllSorted()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        // First root
        var root1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(0, 0, 500, 500), LocalZIndex = 0 })
            .With(new UIRootTag())
            .Build();

        // Second root with higher z-index
        var root2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(200, 200, 500, 500), LocalZIndex = 10 })
            .With(new UIRootTag())
            .Build();

        var results = hitTester.HitTestAll(new Vector2(300, 300));

        // Should contain both roots, sorted by z-index (root2 first)
        Assert.Equal(2, results.Count);
        Assert.Equal(root2, results[0]);
        Assert.Equal(root1, results[1]);
    }

    #endregion

    #region Position Outside All Elements

    [Fact]
    public void HitTest_WithPositionCompletelyOutside_ReturnsNull()
    {
        using var world = new World();
        var hitTester = new UIHitTester(world);

        var root = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { ComputedBounds = new Rectangle(100, 100, 200, 200) })
            .With(new UIRootTag())
            .Build();

        // Test position outside root bounds
        var result = hitTester.HitTest(new Vector2(50, 50));

        Assert.Equal(Entity.Null, result);
    }

    #endregion
}
