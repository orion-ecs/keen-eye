using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UITreeViewSystem node expand/collapse and selection handling.
/// </summary>
public class UITreeViewSystemTests
{
    #region Expand/Collapse Tests

    [Fact]
    public void TreeNodeArrow_Click_ExpandsNode()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Click expand arrow
        SimulateClick(world, arrows[0]);
        system.Update(0);

        ref readonly var node = ref world.Get<UITreeNode>(nodes[0]);
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void TreeNodeArrow_ClickExpanded_CollapsesNode()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Expand first
        SimulateClick(world, arrows[0]);
        system.Update(0);

        // Click again to collapse
        SimulateClick(world, arrows[0]);
        system.Update(0);

        ref readonly var node = ref world.Get<UITreeNode>(nodes[0]);
        Assert.False(node.IsExpanded);
    }

    [Fact]
    public void TreeNodeArrow_ClickExpand_ShowsChildren()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Click to expand
        SimulateClick(world, arrows[0]);
        system.Update(0);

        var childContainer = world.Get<UITreeNode>(nodes[0]).ChildContainer;
        ref readonly var containerElement = ref world.Get<UIElement>(childContainer);
        Assert.True(containerElement.Visible);
    }

    [Fact]
    public void TreeNodeArrow_ClickCollapse_HidesChildren()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Expand then collapse
        SimulateClick(world, arrows[0]);
        system.Update(0);
        SimulateClick(world, arrows[0]);
        system.Update(0);

        var childContainer = world.Get<UITreeNode>(nodes[0]).ChildContainer;
        ref readonly var containerElement = ref world.Get<UIElement>(childContainer);
        Assert.False(containerElement.Visible);
    }

    [Fact]
    public void TreeNodeArrow_ClickExpand_UpdatesArrowVisual()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Click to expand
        SimulateClick(world, arrows[0]);
        system.Update(0);

        ref readonly var arrowText = ref world.Get<UIText>(arrows[0]);
        Assert.Equal("▼", arrowText.Content);
    }

    [Fact]
    public void TreeNodeArrow_ClickCollapse_UpdatesArrowVisual()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Expand then collapse
        SimulateClick(world, arrows[0]);
        system.Update(0);
        SimulateClick(world, arrows[0]);
        system.Update(0);

        ref readonly var arrowText = ref world.Get<UIText>(arrows[0]);
        Assert.Equal("▶", arrowText.Content);
    }

    [Fact]
    public void TreeNodeArrow_ClickOnNodeWithoutChildren_DoesNothing()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        // Click arrow on node without children
        SimulateClick(world, arrows[0]);
        system.Update(0);

        ref readonly var node = ref world.Get<UITreeNode>(nodes[0]);
        Assert.False(node.IsExpanded);
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void TreeNode_Click_SelectsNode()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        // Click node to select
        SimulateClick(world, nodes[0]);
        system.Update(0);

        ref readonly var node = ref world.Get<UITreeNode>(nodes[0]);
        Assert.True(node.IsSelected);
    }

    [Fact]
    public void TreeNode_Click_UpdatesTreeViewSelectedItem()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        // Click node to select
        SimulateClick(world, nodes[0]);
        system.Update(0);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.Equal(nodes[0], treeViewData.SelectedItem);
    }

    [Fact]
    public void TreeNode_ClickDifferentNode_DeselectsPrevious()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        // Select first node
        SimulateClick(world, nodes[0]);
        system.Update(0);

        // Select second node
        SimulateClick(world, nodes[1]);
        system.Update(0);

        ref readonly var node0 = ref world.Get<UITreeNode>(nodes[0]);
        ref readonly var node1 = ref world.Get<UITreeNode>(nodes[1]);

        Assert.False(node0.IsSelected);
        Assert.True(node1.IsSelected);
    }

    [Fact]
    public void TreeNode_Click_UpdatesSelectionVisual()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        // Click to select
        SimulateClick(world, nodes[0]);
        system.Update(0);

        ref readonly var style = ref world.Get<UIStyle>(nodes[0]);
        var expectedSelectedColor = new Vector4(0.3f, 0.5f, 0.7f, 1f);
        Assert.Equal(expectedSelectedColor, style.BackgroundColor);
    }

    [Fact]
    public void TreeNode_Deselect_ClearsVisual()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        // Select first then second (deselects first)
        SimulateClick(world, nodes[0]);
        system.Update(0);
        SimulateClick(world, nodes[1]);
        system.Update(0);

        ref readonly var style = ref world.Get<UIStyle>(nodes[0]);
        Assert.Equal(Vector4.Zero, style.BackgroundColor);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void TreeNodeArrow_ClickToExpand_FiresExpandedEvent()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        UITreeNodeExpandedEvent? receivedEvent = null;
        world.Subscribe<UITreeNodeExpandedEvent>(e => receivedEvent = e);

        SimulateClick(world, arrows[0]);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(nodes[0], receivedEvent.Value.Node);
        Assert.Equal(treeView, receivedEvent.Value.TreeView);
    }

    [Fact]
    public void TreeNodeArrow_ClickToCollapse_FiresCollapsedEvent()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: true);

        // Expand first
        SimulateClick(world, arrows[0]);
        system.Update(0);

        UITreeNodeCollapsedEvent? receivedEvent = null;
        world.Subscribe<UITreeNodeCollapsedEvent>(e => receivedEvent = e);

        // Collapse
        SimulateClick(world, arrows[0]);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(nodes[0], receivedEvent.Value.Node);
        Assert.Equal(treeView, receivedEvent.Value.TreeView);
    }

    [Fact]
    public void TreeNode_Click_FiresSelectedEvent()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        UITreeNodeSelectedEvent? receivedEvent = null;
        world.Subscribe<UITreeNodeSelectedEvent>(e => receivedEvent = e);

        SimulateClick(world, nodes[0]);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(nodes[0], receivedEvent.Value.Node);
        Assert.Equal(treeView, receivedEvent.Value.TreeView);
    }

    [Fact]
    public void TreeNode_DoubleClick_FiresDoubleClickEvent()
    {
        using var world = new World();
        var system = new UITreeViewSystem();
        world.AddSystem(system);

        var (treeView, nodes, arrows) = CreateTreeView(world, hasChildren: false);

        UITreeNodeDoubleClickedEvent? receivedEvent = null;
        world.Subscribe<UITreeNodeDoubleClickedEvent>(e => receivedEvent = e);

        // Simulate double-click
        ref var interactable = ref world.Get<UIInteractable>(nodes[0]);
        interactable.PendingEvents |= UIEventFlags.Click | UIEventFlags.DoubleClick;

        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(nodes[0], receivedEvent.Value.Node);
        Assert.Equal(treeView, receivedEvent.Value.TreeView);
    }

    #endregion

    #region Helper Methods

    private static (Entity TreeView, Entity[] Nodes, Entity[] Arrows) CreateTreeView(
        World world, bool hasChildren, int nodeCount = 2)
    {
        // Create tree view container
        var treeView = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UITreeView())
            .Build();

        var nodes = new Entity[nodeCount];
        var arrows = new Entity[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            // Create expand arrow
            arrows[i] = world.Spawn()
                .With(UIElement.Default)
                .With(new UIText { Content = "▶" })
                .With(new UITreeNodeExpandArrowTag())
                .With(UIInteractable.Clickable())
                .Build();

            // Create child container (for nodes with children)
            Entity childContainer = Entity.Null;
            if (hasChildren)
            {
                childContainer = world.Spawn()
                    .With(new UIElement { Visible = false })
                    .With(UIRect.Fixed(20, 0, 280, 100))
                    .Build();
            }

            // Create node
            nodes[i] = world.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Fixed(0, i * 30, 300, 30))
                .With(new UITreeNode(treeView, Entity.Null, 0, $"Node {i + 1}")
                {
                    HasChildren = hasChildren,
                    IsExpanded = false,
                    IsSelected = false,
                    ExpandArrow = arrows[i],
                    ChildContainer = childContainer
                })
                .With(UIInteractable.Clickable())
                .With(new UIStyle())
                .Build();

            world.SetParent(arrows[i], nodes[i]);
            if (hasChildren)
            {
                world.SetParent(childContainer, nodes[i]);
            }
            world.SetParent(nodes[i], treeView);
        }

        return (treeView, nodes, arrows);
    }

    private static void SimulateClick(World world, Entity entity)
    {
        ref var interactable = ref world.Get<UIInteractable>(entity);
        interactable.PendingEvents |= UIEventFlags.Click;
    }

    #endregion
}
