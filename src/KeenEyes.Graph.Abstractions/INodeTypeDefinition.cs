using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Defines a custom node type for the graph editor.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create custom node types with specific ports,
/// initialization logic, and custom body rendering. Node types are registered
/// with the <c>NodeTypeRegistry</c> and can be instantiated via the graph context.
/// </para>
/// <para>
/// <b>Example:</b>
/// </para>
/// <code>
/// public sealed class AddNode : INodeTypeDefinition
/// {
///     public int TypeId => 101;
///     public string Name => "Add";
///     public string Category => "Math";
///     public IReadOnlyList&lt;PortDefinition&gt; InputPorts => [
///         PortDefinition.Input("A", PortTypeId.Float, 60f),
///         PortDefinition.Input("B", PortTypeId.Float, 90f)
///     ];
///     public IReadOnlyList&lt;PortDefinition&gt; OutputPorts => [
///         PortDefinition.Output("Result", PortTypeId.Float, 75f)
///     ];
///     public bool IsCollapsible => true;
///
///     public void Initialize(Entity node, IWorld world) { }
///
///     public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
///         => 0f; // No custom body content
/// }
/// </code>
/// </remarks>
public interface INodeTypeDefinition
{
    /// <summary>
    /// Gets the unique type identifier for this node type.
    /// </summary>
    /// <remarks>
    /// IDs 1-100 are reserved for built-in node types. User-defined types should use IDs starting at 101.
    /// </remarks>
    int TypeId { get; }

    /// <summary>
    /// Gets the display name of the node type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the category for context menu organization.
    /// </summary>
    /// <remarks>
    /// Common categories include "Math", "Logic", "Flow", "Utility", etc.
    /// The context menu groups node types by category.
    /// </remarks>
    string Category { get; }

    /// <summary>
    /// Gets the input port definitions for this node type.
    /// </summary>
    IReadOnlyList<PortDefinition> InputPorts { get; }

    /// <summary>
    /// Gets the output port definitions for this node type.
    /// </summary>
    IReadOnlyList<PortDefinition> OutputPorts { get; }

    /// <summary>
    /// Gets whether the node supports collapse/expand functionality.
    /// </summary>
    /// <remarks>
    /// When true, a collapse button is shown in the header. Collapsed nodes
    /// display only the title bar with port stubs.
    /// </remarks>
    bool IsCollapsible { get; }

    /// <summary>
    /// Called when a node of this type is created to initialize instance-specific state.
    /// </summary>
    /// <param name="node">The newly created node entity.</param>
    /// <param name="world">The world containing the node.</param>
    /// <remarks>
    /// Use this to add node-specific components (e.g., <c>CommentNodeData</c> for comment nodes).
    /// </remarks>
    void Initialize(Entity node, IWorld world);

    /// <summary>
    /// Renders custom body content for the node.
    /// </summary>
    /// <param name="node">The node entity being rendered.</param>
    /// <param name="world">The world containing the node.</param>
    /// <param name="renderer">The 2D renderer for drawing.</param>
    /// <param name="bodyArea">The available area for body content in screen coordinates.</param>
    /// <returns>The height actually consumed by the body content.</returns>
    /// <remarks>
    /// The body area is below the header and above the bottom padding. Return 0 if no
    /// custom body content is needed. Use <c>NodeWidgets</c> for common UI elements.
    /// </remarks>
    float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea);
}
