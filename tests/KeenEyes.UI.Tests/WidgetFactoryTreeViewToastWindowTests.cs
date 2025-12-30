using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory TreeView, Toast, and Window widget creation methods.
/// </summary>
public class WidgetFactoryTreeViewToastWindowTests
{
    private static readonly FontHandle testFont = new(1);

    #region TreeView Tests

    [Fact]
    public void CreateTreeView_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var treeView = WidgetFactory.CreateTreeView(world, parent);

        Assert.True(world.Has<UIElement>(treeView));
        Assert.True(world.Has<UIRect>(treeView));
        Assert.True(world.Has<UIStyle>(treeView));
        Assert.True(world.Has<UILayout>(treeView));
        Assert.True(world.Has<UITreeView>(treeView));
    }

    [Fact]
    public void CreateTreeView_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var treeView = WidgetFactory.CreateTreeView(world, parent);

        ref readonly var layout = ref world.Get<UILayout>(treeView);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateTreeView_HasNodeContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var treeView = WidgetFactory.CreateTreeView(world, parent);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.True(treeViewData.NodeContainer.IsValid);
        Assert.True(world.IsAlive(treeViewData.NodeContainer));
    }

    [Fact]
    public void CreateTreeView_NodeContainerIsChildOfTreeView()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var treeView = WidgetFactory.CreateTreeView(world, parent);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.Equal(treeView, world.GetParent(treeViewData.NodeContainer));
    }

    [Fact]
    public void CreateTreeView_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new TreeViewConfig(Width: 300, Height: 400, IndentSize: 24f);

        var treeView = WidgetFactory.CreateTreeView(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(treeView);
        Assert.Equal(300, rect.Size.X);
        Assert.Equal(400, rect.Size.Y);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.Equal(24f, treeViewData.IndentSize);
    }

    [Fact]
    public void CreateTreeView_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var treeView = WidgetFactory.CreateTreeView(world, "MyTreeView", parent);

        Assert.Equal("MyTreeView", world.GetName(treeView));
    }

    [Fact]
    public void CreateTreeView_ShowLines_WhenConfigured()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new TreeViewConfig(ShowLines: true);

        var treeView = WidgetFactory.CreateTreeView(world, parent, config);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.True(treeViewData.ShowLines);
    }

    [Fact]
    public void CreateTreeView_AllowMultiSelect_WhenConfigured()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new TreeViewConfig(AllowMultiSelect: true);

        var treeView = WidgetFactory.CreateTreeView(world, parent, config);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.True(treeViewData.AllowMultiSelect);
    }

    [Fact]
    public void CreateTreeView_HasClipChildrenTag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var treeView = WidgetFactory.CreateTreeView(world, parent);

        Assert.True(world.Has<UIClipChildrenTag>(treeView));
    }

    [Fact]
    public void CreateTreeNode_ReturnsValidEntity()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);

        var node = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 1", testFont);

        Assert.True(node.IsValid);
        Assert.True(world.IsAlive(node));
    }

    [Fact]
    public void CreateTreeNode_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);

        var node = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 1", testFont);

        Assert.True(world.Has<UIElement>(node));
        Assert.True(world.Has<UIRect>(node));
        Assert.True(world.Has<UITreeNode>(node));
    }

    [Fact]
    public void CreateTreeNode_IncrementsVisibleNodeCount()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);

        WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 1", testFont);
        WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 2", testFont);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.Equal(2, treeViewData.VisibleNodeCount);
    }

    [Fact]
    public void CreateTreeNode_StoresLabel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);

        var node = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Test Node", testFont);

        ref readonly var nodeData = ref world.Get<UITreeNode>(node);
        Assert.Equal("Test Node", nodeData.Label);
    }

    [Fact]
    public void CreateTreeNode_WithInvalidTreeView_ReturnsNull()
    {
        using var world = new World();

        var node = WidgetFactory.CreateTreeNode(world, Entity.Null, Entity.Null, "Node 1", testFont);

        Assert.Equal(Entity.Null, node);
    }

    [Fact]
    public void CreateTreeNode_ChildNode_HasCorrectDepth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);
        var parentNode = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Parent", testFont);

        var childNode = WidgetFactory.CreateTreeNode(world, treeView, parentNode, "Child", testFont);

        ref readonly var childData = ref world.Get<UITreeNode>(childNode);
        Assert.Equal(1, childData.Depth);
    }

    [Fact]
    public void CreateTreeViewWithNodes_CreatesAllNodes()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var nodes = new[]
        {
            new TreeNodeDef("Node 1"),
            new TreeNodeDef("Node 2"),
            new TreeNodeDef("Node 3")
        };

        var treeView = WidgetFactory.CreateTreeViewWithNodes(world, parent, testFont, nodes);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.Equal(3, treeViewData.VisibleNodeCount);
    }

    [Fact]
    public void CreateTreeViewWithNodes_CreatesNestedNodes()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var nodes = new[]
        {
            new TreeNodeDef("Parent", Children: new[]
            {
                new TreeNodeDef("Child 1"),
                new TreeNodeDef("Child 2")
            })
        };

        var treeView = WidgetFactory.CreateTreeViewWithNodes(world, parent, testFont, nodes);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        // Parent + 2 children = 3 nodes
        Assert.Equal(3, treeViewData.VisibleNodeCount);
    }

    [Fact]
    public void SetTreeNodeExpanded_UpdatesExpandedState()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);
        var parentNode = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Parent", testFont);
        WidgetFactory.CreateTreeNode(world, treeView, parentNode, "Child", testFont);

        // Mark parent as having children
        ref var parentData = ref world.Get<UITreeNode>(parentNode);
        parentData.HasChildren = true;

        WidgetFactory.SetTreeNodeExpanded(world, parentNode, true);

        ref readonly var updatedParentData = ref world.Get<UITreeNode>(parentNode);
        Assert.True(updatedParentData.IsExpanded);
    }

    [Fact]
    public void SelectTreeNode_UpdatesSelectedItem()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);
        var node = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 1", testFont);

        WidgetFactory.SelectTreeNode(world, node);

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        Assert.Equal(node, treeViewData.SelectedItem);

        ref readonly var nodeData = ref world.Get<UITreeNode>(node);
        Assert.True(nodeData.IsSelected);
    }

    [Fact]
    public void SelectTreeNode_DeselectsPreviousNode()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var treeView = WidgetFactory.CreateTreeView(world, parent);
        var node1 = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 1", testFont);
        var node2 = WidgetFactory.CreateTreeNode(world, treeView, Entity.Null, "Node 2", testFont);

        WidgetFactory.SelectTreeNode(world, node1);
        WidgetFactory.SelectTreeNode(world, node2);

        ref readonly var node1Data = ref world.Get<UITreeNode>(node1);
        ref readonly var node2Data = ref world.Get<UITreeNode>(node2);
        Assert.False(node1Data.IsSelected);
        Assert.True(node2Data.IsSelected);
    }

    #endregion

    #region Toast Tests

    [Fact]
    public void CreateToastContainer_HasRequiredComponents()
    {
        using var world = new World();

        var container = WidgetFactory.CreateToastContainer(world);

        Assert.True(world.Has<UIElement>(container));
        Assert.True(world.Has<UIRect>(container));
        Assert.True(world.Has<UIStyle>(container));
        Assert.True(world.Has<UILayout>(container));
        Assert.True(world.Has<UIToastContainer>(container));
    }

    [Fact]
    public void CreateToastContainer_HasVerticalLayout()
    {
        using var world = new World();

        var container = WidgetFactory.CreateToastContainer(world);

        ref readonly var layout = ref world.Get<UILayout>(container);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateToastContainer_AppliesConfig()
    {
        using var world = new World();
        var config = new ToastContainerConfig(
            Position: ToastPosition.TopLeft,
            MaxVisible: 3,
            Spacing: 12f
        );

        var container = WidgetFactory.CreateToastContainer(world, config);

        ref readonly var containerData = ref world.Get<UIToastContainer>(container);
        Assert.Equal(ToastPosition.TopLeft, containerData.Position);
        Assert.Equal(3, containerData.MaxVisible);
        Assert.Equal(12f, containerData.Spacing);
    }

    [Fact]
    public void CreateToastContainer_TopRight_AnchorMin_At_1_0()
    {
        using var world = new World();
        var config = new ToastContainerConfig(Position: ToastPosition.TopRight);

        var container = WidgetFactory.CreateToastContainer(world, config);

        ref readonly var rect = ref world.Get<UIRect>(container);
        Assert.Equal(new Vector2(1, 0), rect.AnchorMin);
    }

    [Fact]
    public void CreateToastContainer_BottomLeft_Position()
    {
        using var world = new World();
        var config = new ToastContainerConfig(Position: ToastPosition.BottomLeft);

        var container = WidgetFactory.CreateToastContainer(world, config);

        ref readonly var containerData = ref world.Get<UIToastContainer>(container);
        Assert.Equal(ToastPosition.BottomLeft, containerData.Position);
    }

    [Fact]
    public void CreateToast_HasRequiredComponents()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);
        var config = new ToastConfig("Test message");

        var toast = WidgetFactory.CreateToast(world, container, config);

        Assert.True(world.Has<UIElement>(toast));
        Assert.True(world.Has<UIRect>(toast));
        Assert.True(world.Has<UIStyle>(toast));
        Assert.True(world.Has<UIToast>(toast));
    }

    [Fact]
    public void CreateToast_StoresMessage()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);
        var config = new ToastConfig("Hello World");

        var toast = WidgetFactory.CreateToast(world, container, config);

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal("Hello World", toastData.Message);
    }

    [Fact]
    public void CreateToast_StoresTitle()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);
        var config = new ToastConfig("Message", Title: "My Title");

        var toast = WidgetFactory.CreateToast(world, container, config);

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal("My Title", toastData.Title);
    }

    [Fact]
    public void CreateToast_StoresType()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);
        var config = new ToastConfig("Message", Type: ToastType.Success);

        var toast = WidgetFactory.CreateToast(world, container, config);

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal(ToastType.Success, toastData.Type);
    }

    [Fact]
    public void CreateToast_StoresDuration()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);
        var config = new ToastConfig("Message", Duration: 5f);

        var toast = WidgetFactory.CreateToast(world, container, config);

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal(5f, toastData.Duration);
    }

    [Fact]
    public void CreateToast_IsChildOfContainer()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);
        var config = new ToastConfig("Test");

        var toast = WidgetFactory.CreateToast(world, container, config);

        Assert.Equal(container, world.GetParent(toast));
    }

    [Fact]
    public void ShowInfoToast_CreatesInfoType()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);

        var toast = WidgetFactory.ShowInfoToast(world, container, "Info message");

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal(ToastType.Info, toastData.Type);
    }

    [Fact]
    public void ShowSuccessToast_CreatesSuccessType()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);

        var toast = WidgetFactory.ShowSuccessToast(world, container, "Success message");

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal(ToastType.Success, toastData.Type);
    }

    [Fact]
    public void ShowWarningToast_CreatesWarningType()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);

        var toast = WidgetFactory.ShowWarningToast(world, container, "Warning message");

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal(ToastType.Warning, toastData.Type);
    }

    [Fact]
    public void ShowErrorToast_CreatesErrorType()
    {
        using var world = new World();
        var container = WidgetFactory.CreateToastContainer(world);

        var toast = WidgetFactory.ShowErrorToast(world, container, "Error message");

        ref readonly var toastData = ref world.Get<UIToast>(toast);
        Assert.Equal(ToastType.Error, toastData.Type);
    }

    #endregion

    #region Window Tests

    [Fact]
    public void CreateWindow_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (window, contentPanel) = WidgetFactory.CreateWindow(world, parent, "Test Window", testFont);

        Assert.True(world.Has<UIElement>(window));
        Assert.True(world.Has<UIRect>(window));
        Assert.True(world.Has<UIStyle>(window));
        Assert.True(world.Has<UILayout>(window));
        Assert.True(world.Has<UIWindow>(window));
    }

    [Fact]
    public void CreateWindow_ReturnsContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (window, contentPanel) = WidgetFactory.CreateWindow(world, parent, "Test Window", testFont);

        Assert.True(contentPanel.IsValid);
        Assert.True(world.IsAlive(contentPanel));
    }

    [Fact]
    public void CreateWindow_StoresTitle()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "My Window", testFont);

        ref readonly var windowData = ref world.Get<UIWindow>(window);
        Assert.Equal("My Window", windowData.Title);
    }

    [Fact]
    public void CreateWindow_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new UIWindowConfig(Width: 400, Height: 300, X: 100, Y: 50);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(window);
        Assert.Equal(new Vector2(400, 300), rect.Size);
    }

    [Fact]
    public void CreateWindow_ContentPanelIsChildOfWindow()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (window, contentPanel) = WidgetFactory.CreateWindow(world, parent, "Test", testFont);

        Assert.Equal(window, world.GetParent(contentPanel));
    }

    [Fact]
    public void CreateWindow_HasTitleBar()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont);

        ref readonly var windowData = ref world.Get<UIWindow>(window);
        Assert.True(windowData.TitleBar.IsValid);
        Assert.True(world.Has<UIWindowTitleBar>(windowData.TitleBar));
    }

    [Fact]
    public void CreateWindow_WithCloseEnabled_HasCloseButton()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new UIWindowConfig(CanClose: true);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont, config);

        var hasCloseButton = FindEntityWithComponent<UIWindowCloseButton>(world, window);
        Assert.True(hasCloseButton);
    }

    [Fact]
    public void CreateWindow_WithMinimizeEnabled_HasMinimizeButton()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new UIWindowConfig(CanMinimize: true);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont, config);

        var hasMinimizeButton = FindEntityWithComponent<UIWindowMinimizeButton>(world, window);
        Assert.True(hasMinimizeButton);
    }

    [Fact]
    public void CreateWindow_WithMaximizeEnabled_HasMaximizeButton()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new UIWindowConfig(CanMaximize: true);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont, config);

        var hasMaximizeButton = FindEntityWithComponent<UIWindowMaximizeButton>(world, window);
        Assert.True(hasMaximizeButton);
    }

    [Fact]
    public void CreateWindow_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "MyWindow", "Test Title", testFont);

        Assert.Equal("MyWindow", world.GetName(window));
    }

    [Fact]
    public void CreateWindow_CanDrag_WhenConfigured()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new UIWindowConfig(CanDrag: true);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont, config);

        ref readonly var windowData = ref world.Get<UIWindow>(window);
        Assert.True(windowData.CanDrag);
    }

    [Fact]
    public void CreateWindow_CanResize_WhenConfigured()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new UIWindowConfig(CanResize: true);

        var (window, _) = WidgetFactory.CreateWindow(world, parent, "Test", testFont, config);

        ref readonly var windowData = ref world.Get<UIWindow>(window);
        Assert.True(windowData.CanResize);
    }

    #endregion

    #region Splitter Tests

    [Fact]
    public void CreateSplitter_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (container, firstPane, secondPane) = WidgetFactory.CreateSplitter(world, parent);

        Assert.True(world.Has<UIElement>(container));
        Assert.True(world.Has<UIRect>(container));
        Assert.True(world.Has<UISplitter>(container));
    }

    [Fact]
    public void CreateSplitter_ReturnsBothPanes()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (container, firstPane, secondPane) = WidgetFactory.CreateSplitter(world, parent);

        Assert.True(firstPane.IsValid);
        Assert.True(secondPane.IsValid);
        Assert.True(world.Has<UISplitterFirstPane>(firstPane));
        Assert.True(world.Has<UISplitterSecondPane>(secondPane));
    }

    [Fact]
    public void CreateSplitter_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new SplitterConfig(
            Orientation: LayoutDirection.Vertical,
            InitialRatio: 0.3f,
            HandleSize: 10f
        );

        var (container, _, _) = WidgetFactory.CreateSplitter(world, parent, config);

        ref readonly var splitter = ref world.Get<UISplitter>(container);
        Assert.Equal(LayoutDirection.Vertical, splitter.Orientation);
        Assert.Equal(0.3f, splitter.SplitRatio);
        Assert.Equal(10f, splitter.HandleSize);
    }

    [Fact]
    public void CreateSplitter_HasHandle()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (container, _, _) = WidgetFactory.CreateSplitter(world, parent);

        var hasHandle = FindEntityWithComponent<UISplitterHandle>(world, container);
        Assert.True(hasHandle);
    }

    [Fact]
    public void CreateSplitter_HandleIsDraggable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (container, _, _) = WidgetFactory.CreateSplitter(world, parent);

        // Find the handle
        var handle = FindEntityWithComponentReturn<UISplitterHandle>(world, container);
        Assert.True(handle.IsValid);

        ref readonly var interactable = ref world.Get<UIInteractable>(handle);
        Assert.True(interactable.CanDrag);
    }

    [Fact]
    public void CreateSplitter_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (container, _, _) = WidgetFactory.CreateSplitter(world, parent, "MySplitter");

        Assert.Equal("MySplitter", world.GetName(container));
    }

    [Fact]
    public void CreateSplitter_PanesAreChildrenOfContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (container, firstPane, secondPane) = WidgetFactory.CreateSplitter(world, parent);

        Assert.Equal(container, world.GetParent(firstPane));
        Assert.Equal(container, world.GetParent(secondPane));
    }

    [Fact]
    public void CreateSplitter_HorizontalOrientation_HasHorizontalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new SplitterConfig(Orientation: LayoutDirection.Horizontal);

        var (container, _, _) = WidgetFactory.CreateSplitter(world, parent, config);

        ref readonly var layout = ref world.Get<UILayout>(container);
        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
    }

    [Fact]
    public void CreateSplitter_VerticalOrientation_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new SplitterConfig(Orientation: LayoutDirection.Vertical);

        var (container, _, _) = WidgetFactory.CreateSplitter(world, parent, config);

        ref readonly var layout = ref world.Get<UILayout>(container);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    #endregion

    #region Helper Methods

    private static Entity CreateRootEntity(World world)
    {
        var root = world.Spawn("Root")
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);
        layout.Update(0);

        return root;
    }

    private static bool FindEntityWithComponent<T>(World world, Entity root) where T : struct, IComponent
    {
        var stack = new Stack<Entity>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (world.Has<T>(current))
            {
                return true;
            }

            foreach (var child in world.GetChildren(current))
            {
                stack.Push(child);
            }
        }

        return false;
    }

    private static Entity FindEntityWithComponentReturn<T>(World world, Entity root) where T : struct, IComponent
    {
        var stack = new Stack<Entity>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (world.Has<T>(current))
            {
                return current;
            }

            foreach (var child in world.GetChildren(current))
            {
                stack.Push(child);
            }
        }

        return Entity.Null;
    }

    #endregion
}
