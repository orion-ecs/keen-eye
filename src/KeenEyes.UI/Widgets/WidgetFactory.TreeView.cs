using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for tree view UI widgets.
/// </summary>
public static partial class WidgetFactory
{
    #region TreeView

    /// <summary>
    /// Creates a tree view widget for displaying hierarchical data.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="config">Optional tree view configuration.</param>
    /// <returns>The created tree view entity.</returns>
    /// <remarks>
    /// <para>
    /// Tree views display hierarchical data with expandable/collapsible nodes.
    /// Use <see cref="CreateTreeNode"/> to add nodes to the tree view.
    /// </para>
    /// </remarks>
    public static Entity CreateTreeView(
        IWorld world,
        Entity parent,
        TreeViewConfig? config = null)
    {
        return CreateTreeView(world, "TreeView", parent, config);
    }

    /// <summary>
    /// Creates a named tree view widget for displaying hierarchical data.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="config">Optional tree view configuration.</param>
    /// <returns>The created tree view entity.</returns>
    public static Entity CreateTreeView(
        IWorld world,
        string name,
        Entity parent,
        TreeViewConfig? config = null)
    {
        config ??= TreeViewConfig.Default;

        var treeView = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width ?? 0, config.Height ?? 0),
                WidthMode = config.Width.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.Height.HasValue ? UISizeMode.Fixed : UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .With(UIInteractable.Button(0))
            .With(new UITreeView(config.IndentSize)
            {
                ShowLines = config.ShowLines,
                AllowMultiSelect = config.AllowMultiSelect
            })
            .WithTag<UIClipChildrenTag>()
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(treeView, parent);
        }

        // Create node container
        var nodeContainer = world.Spawn($"{name}_NodeContainer")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();

        world.SetParent(nodeContainer, treeView);

        // Store node container reference
        ref var treeViewData = ref world.Get<UITreeView>(treeView);
        treeViewData.NodeContainer = nodeContainer;

        return treeView;
    }

    /// <summary>
    /// Creates a tree node within a tree view.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="treeView">The parent tree view entity.</param>
    /// <param name="parentNode">The parent node (Entity.Null for root-level nodes).</param>
    /// <param name="label">The display label for the node.</param>
    /// <param name="font">The font for the label text.</param>
    /// <param name="config">Optional tree node configuration.</param>
    /// <returns>The created tree node entity.</returns>
    /// <remarks>
    /// <para>
    /// Tree nodes can have children, creating a hierarchical structure.
    /// After creating child nodes, call <see cref="UpdateTreeNodeHasChildren"/>
    /// to update the parent node's expand arrow visibility.
    /// </para>
    /// </remarks>
    public static Entity CreateTreeNode(
        IWorld world,
        Entity treeView,
        Entity parentNode,
        string label,
        FontHandle font,
        TreeNodeConfig? config = null)
    {
        if (!world.Has<UITreeView>(treeView))
        {
            return Entity.Null;
        }

        config ??= new TreeNodeConfig();

        ref readonly var treeViewData = ref world.Get<UITreeView>(treeView);
        var treeViewConfig = GetTreeViewConfigFromEntity(world, treeView);

        // Calculate depth
        int depth = 0;
        if (parentNode.IsValid && world.Has<UITreeNode>(parentNode))
        {
            ref readonly var parentNodeData = ref world.Get<UITreeNode>(parentNode);
            depth = parentNodeData.Depth + 1;
        }

        // Calculate sibling index
        int siblingIndex = CountSiblings(world, parentNode, treeView);

        // Create the main node row container
        var nodeRow = world.Spawn($"TreeNode_{label}")
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(0, treeViewConfig.RowHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 4
            })
            .With(UIInteractable.Clickable())
            .With(new UITreeNode(treeView, parentNode, depth, label)
            {
                IsExpanded = config.IsExpanded,
                SiblingIndex = siblingIndex
            })
            .Build();

        // Determine parent container
        Entity parentContainer;
        if (parentNode.IsValid && world.Has<UITreeNode>(parentNode))
        {
            ref readonly var parentNodeData = ref world.Get<UITreeNode>(parentNode);
            parentContainer = parentNodeData.ChildContainer;

            // Create child container for parent if it doesn't exist
            if (!parentContainer.IsValid)
            {
                parentContainer = CreateNodeChildContainer(world, parentNode, treeView, treeViewConfig);
                ref var parentNodeDataMut = ref world.Get<UITreeNode>(parentNode);
                parentNodeDataMut.ChildContainer = parentContainer;
                parentNodeDataMut.HasChildren = true;
            }
        }
        else
        {
            parentContainer = treeViewData.NodeContainer;
        }

        world.SetParent(nodeRow, parentContainer);

        // Create indentation spacer
        float indentWidth = depth * treeViewData.IndentSize;
        if (indentWidth > 0)
        {
            var indent = world.Spawn($"TreeNode_{label}_Indent")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.Zero,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(indentWidth, treeViewConfig.RowHeight),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle())
                .Build();

            world.SetParent(indent, nodeRow);
        }

        // Create expand arrow placeholder (will be shown/hidden based on children)
        var expandArrow = world.Spawn($"TreeNode_{label}_Arrow")
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(16, 16),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle { BackgroundColor = treeViewConfig.GetExpandArrowColor() })
            .With(UIInteractable.Clickable())
            .With(new UITreeNodeExpandArrowTag())
            .Build();

        world.SetParent(expandArrow, nodeRow);

        // Create icon if specified
        if (config.Icon.IsValid)
        {
            var icon = world.Spawn($"TreeNode_{label}_Icon")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.Zero,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(config.IconSize, config.IconSize),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle())
                .With(new UIImage { Texture = config.Icon, Tint = Vector4.One })
                .Build();

            world.SetParent(icon, nodeRow);
        }

        // Create label text
        var labelText = world.Spawn($"TreeNode_{label}_Label")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, treeViewConfig.RowHeight),
                WidthMode = UISizeMode.FitContent,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle())
            .With(new UIText
            {
                Content = label,
                Font = font,
                FontSize = treeViewConfig.FontSize,
                Color = treeViewConfig.GetTextColor(),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelText, nodeRow);

        // Store references in the node
        ref var nodeData = ref world.Get<UITreeNode>(nodeRow);
        nodeData.ExpandArrow = expandArrow;

        // Update visible node count
        ref var treeViewDataMut = ref world.Get<UITreeView>(treeView);
        treeViewDataMut.VisibleNodeCount++;

        return nodeRow;
    }

    /// <summary>
    /// Creates a tree view with nodes from a hierarchical definition.
    /// </summary>
    /// <param name="world">The world to create the entities in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for node labels.</param>
    /// <param name="nodes">The root-level node definitions.</param>
    /// <param name="config">Optional tree view configuration.</param>
    /// <returns>The created tree view entity.</returns>
    public static Entity CreateTreeViewWithNodes(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<TreeNodeDef> nodes,
        TreeViewConfig? config = null)
    {
        return CreateTreeViewWithNodes(world, "TreeView", parent, font, nodes, config);
    }

    /// <summary>
    /// Creates a named tree view with nodes from a hierarchical definition.
    /// </summary>
    /// <param name="world">The world to create the entities in.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for node labels.</param>
    /// <param name="nodes">The root-level node definitions.</param>
    /// <param name="config">Optional tree view configuration.</param>
    /// <returns>The created tree view entity.</returns>
    public static Entity CreateTreeViewWithNodes(
        IWorld world,
        string name,
        Entity parent,
        FontHandle font,
        IEnumerable<TreeNodeDef> nodes,
        TreeViewConfig? config = null)
    {
        var treeView = CreateTreeView(world, name, parent, config);
        CreateNodesRecursive(world, treeView, Entity.Null, font, nodes, config);
        return treeView;
    }

    /// <summary>
    /// Updates the expand arrow visibility based on whether the node has children.
    /// </summary>
    /// <param name="world">The world containing the node.</param>
    /// <param name="node">The tree node entity.</param>
    public static void UpdateTreeNodeHasChildren(IWorld world, Entity node)
    {
        if (!world.Has<UITreeNode>(node))
        {
            return;
        }

        ref readonly var nodeData = ref world.Get<UITreeNode>(node);

        if (nodeData.ExpandArrow.IsValid && world.Has<UIElement>(nodeData.ExpandArrow))
        {
            ref var arrowElement = ref world.Get<UIElement>(nodeData.ExpandArrow);
            arrowElement.Visible = nodeData.HasChildren;
        }
    }

    /// <summary>
    /// Expands or collapses a tree node programmatically.
    /// </summary>
    /// <param name="world">The world containing the node.</param>
    /// <param name="node">The tree node entity.</param>
    /// <param name="expanded">Whether the node should be expanded.</param>
    public static void SetTreeNodeExpanded(IWorld world, Entity node, bool expanded)
    {
        if (!world.Has<UITreeNode>(node))
        {
            return;
        }

        ref var nodeData = ref world.Get<UITreeNode>(node);
        if (!nodeData.HasChildren || nodeData.IsExpanded == expanded)
        {
            return;
        }

        nodeData.IsExpanded = expanded;

        // Update child container visibility
        if (nodeData.ChildContainer.IsValid && world.Has<UIElement>(nodeData.ChildContainer))
        {
            ref var childElement = ref world.Get<UIElement>(nodeData.ChildContainer);
            childElement.Visible = expanded;
        }
    }

    /// <summary>
    /// Selects a tree node programmatically.
    /// </summary>
    /// <param name="world">The world containing the node.</param>
    /// <param name="node">The tree node entity to select.</param>
    public static void SelectTreeNode(IWorld world, Entity node)
    {
        if (!world.Has<UITreeNode>(node))
        {
            return;
        }

        ref var nodeData = ref world.Get<UITreeNode>(node);
        if (!nodeData.TreeView.IsValid || !world.Has<UITreeView>(nodeData.TreeView))
        {
            return;
        }

        ref var treeViewData = ref world.Get<UITreeView>(nodeData.TreeView);

        // Deselect previous
        if (treeViewData.SelectedItem.IsValid && treeViewData.SelectedItem != node)
        {
            if (world.Has<UITreeNode>(treeViewData.SelectedItem))
            {
                ref var prevNode = ref world.Get<UITreeNode>(treeViewData.SelectedItem);
                prevNode.IsSelected = false;
            }
        }

        // Select new
        nodeData.IsSelected = true;
        treeViewData.SelectedItem = node;
    }

    private static void CreateNodesRecursive(
        IWorld world,
        Entity treeView,
        Entity parentNode,
        FontHandle font,
        IEnumerable<TreeNodeDef> nodes,
        TreeViewConfig? treeConfig)
    {
        foreach (var nodeDef in nodes)
        {
            var nodeConfig = new TreeNodeConfig(
                IsExpanded: nodeDef.IsExpanded,
                Icon: nodeDef.Icon);

            var node = CreateTreeNode(world, treeView, parentNode, nodeDef.Label, font, nodeConfig);

            if (nodeDef.Children != null && nodeDef.Children.Any())
            {
                CreateNodesRecursive(world, treeView, node, font, nodeDef.Children, treeConfig);

                // Mark parent as having children and update arrow visibility
                ref var nodeData = ref world.Get<UITreeNode>(node);
                nodeData.HasChildren = true;
                UpdateTreeNodeHasChildren(world, node);

                // Set initial visibility of children based on expanded state
                if (nodeData.ChildContainer.IsValid && world.Has<UIElement>(nodeData.ChildContainer))
                {
                    ref var childElement = ref world.Get<UIElement>(nodeData.ChildContainer);
                    childElement.Visible = nodeDef.IsExpanded;
                }
            }
        }
    }

    private static Entity CreateNodeChildContainer(
        IWorld world,
        Entity parentNode,
        Entity treeView,
        TreeViewConfig config)
    {
        ref readonly var nodeData = ref world.Get<UITreeNode>(parentNode);

        var childContainer = world.Spawn($"TreeNode_{nodeData.Label}_Children")
            .With(new UIElement { Visible = nodeData.IsExpanded, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();

        // Parent to the same container as the node row
        var nodeParent = world.GetParent(parentNode);
        world.SetParent(childContainer, nodeParent);

        return childContainer;
    }

    private static int CountSiblings(IWorld world, Entity parentNode, Entity treeView)
    {
        int count = 0;
        foreach (var entity in world.Query<UITreeNode>())
        {
            ref readonly var node = ref world.Get<UITreeNode>(entity);
            if (node.TreeView == treeView && node.ParentNode == parentNode)
            {
                count++;
            }
        }
        return count;
    }

    private static TreeViewConfig GetTreeViewConfigFromEntity(IWorld world, Entity treeView)
    {
        ref readonly var data = ref world.Get<UITreeView>(treeView);
        return new TreeViewConfig(
            IndentSize: data.IndentSize,
            ShowLines: data.ShowLines,
            AllowMultiSelect: data.AllowMultiSelect);
    }

    #endregion
}
