using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Nodes;

/// <summary>
/// A comment node that displays editable text annotation.
/// </summary>
/// <remarks>
/// <para>
/// Comment nodes have no ports and serve purely as documentation on the graph canvas.
/// They can be resized and edited by double-clicking.
/// </para>
/// </remarks>
public sealed class CommentNode : INodeTypeDefinition
{
    private static readonly Vector4 commentBackgroundColor = new(0.2f, 0.2f, 0.15f, 0.9f);

    /// <inheritdoc />
    public int TypeId => BuiltInNodeIds.Comment;

    /// <inheritdoc />
    public string Name => "Comment";

    /// <inheritdoc />
    public string Category => "Utility";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => [];

    /// <inheritdoc />
    public bool IsCollapsible => false;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        // Add the comment data component with default text
        world.Add(node, CommentNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Get comment data
        if (!world.Has<CommentNodeData>(node))
        {
            return 0f;
        }

        ref readonly var commentData = ref world.Get<CommentNodeData>(node);

        // Draw comment background (slightly different from node body)
        renderer.FillRect(bodyArea.X, bodyArea.Y, bodyArea.Width, bodyArea.Height, commentBackgroundColor);

        // Note: Text rendering would go here when ITextRenderer is available
        // For now, the comment displays as a colored area indicating editable content

        return bodyArea.Height;
    }
}
