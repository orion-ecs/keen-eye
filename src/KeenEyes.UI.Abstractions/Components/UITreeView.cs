namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a tree view container that displays hierarchical data.
/// </summary>
/// <remarks>
/// <para>
/// Tree views display hierarchical data with expandable/collapsible nodes.
/// Common uses include file browsers, scene hierarchies, and outline views.
/// </para>
/// <para>
/// Nodes can be expanded/collapsed by clicking the expand arrow, and
/// selected by clicking the node content. Double-click events can be
/// used to trigger actions like opening files.
/// </para>
/// </remarks>
/// <param name="indentSize">Indentation per depth level.</param>
public struct UITreeView(float indentSize = 20f) : IComponent
{
    /// <summary>
    /// The currently selected node entity.
    /// </summary>
    public Entity SelectedItem = Entity.Null;

    /// <summary>
    /// The indentation size per depth level in pixels.
    /// </summary>
    public float IndentSize = indentSize;

    /// <summary>
    /// Whether to show connecting lines between parent and child nodes.
    /// </summary>
    public bool ShowLines = false;

    /// <summary>
    /// Whether to allow multiple node selection.
    /// </summary>
    public bool AllowMultiSelect = false;

    /// <summary>
    /// The root container for tree nodes.
    /// </summary>
    public Entity NodeContainer = Entity.Null;

    /// <summary>
    /// Total number of visible nodes (expanded parents and their children).
    /// </summary>
    public int VisibleNodeCount = 0;
}

/// <summary>
/// Component for a node within a tree view.
/// </summary>
/// <remarks>
/// <para>
/// Each tree node can have child nodes, creating a hierarchical structure.
/// Nodes with children can be expanded or collapsed.
/// </para>
/// </remarks>
/// <param name="treeView">The owning tree view.</param>
/// <param name="parentNode">The parent node or Entity.Null.</param>
/// <param name="depth">The depth in the hierarchy.</param>
/// <param name="label">The display label.</param>
public struct UITreeNode(Entity treeView, Entity parentNode, int depth, string label) : IComponent
{
    /// <summary>
    /// The tree view this node belongs to.
    /// </summary>
    public Entity TreeView = treeView;

    /// <summary>
    /// The parent node (Entity.Null for root-level nodes).
    /// </summary>
    public Entity ParentNode = parentNode;

    /// <summary>
    /// Depth in the hierarchy (0 = root level).
    /// </summary>
    public int Depth = depth;

    /// <summary>
    /// Whether this node is expanded (showing children).
    /// </summary>
    public bool IsExpanded = false;

    /// <summary>
    /// Whether this node has any children.
    /// </summary>
    public bool HasChildren = false;

    /// <summary>
    /// Whether this node is currently selected.
    /// </summary>
    public bool IsSelected = false;

    /// <summary>
    /// Index of this node among its siblings.
    /// </summary>
    public int SiblingIndex = 0;

    /// <summary>
    /// The label text displayed for this node.
    /// </summary>
    public string Label = label;

    /// <summary>
    /// The expand/collapse arrow entity for this node.
    /// </summary>
    public Entity ExpandArrow = Entity.Null;

    /// <summary>
    /// The content container entity for this node's children.
    /// </summary>
    public Entity ChildContainer = Entity.Null;
}

/// <summary>
/// Tag for nodes that are currently being dragged for reordering.
/// </summary>
public struct UITreeNodeDraggingTag : ITagComponent;

/// <summary>
/// Tag for the expand/collapse arrow element within a tree node.
/// </summary>
public struct UITreeNodeExpandArrowTag : ITagComponent;
