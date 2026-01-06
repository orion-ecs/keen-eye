using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Nodes;

/// <summary>
/// A group node that contains a subgraph with interface ports.
/// </summary>
/// <remarks>
/// <para>
/// Group nodes encapsulate a collection of child nodes within an internal canvas.
/// Interface ports expose internal connections to the outer graph, allowing the
/// group to act as a single node while containing complex subgraphs.
/// </para>
/// <para>
/// Double-click on a group node to enter and edit the internal canvas.
/// The group's size adjusts based on interface port count.
/// </para>
/// </remarks>
public sealed class GroupNode : INodeTypeDefinition
{
    private static readonly Vector4 GroupBodyColor = new(0.12f, 0.15f, 0.18f, 0.95f);
    private static readonly Vector4 GroupBorderColor = new(0.3f, 0.4f, 0.5f, 1f);
    private static readonly Vector4 GroupLabelColor = new(0.7f, 0.8f, 0.9f, 1f);

    /// <inheritdoc />
    public int TypeId => BuiltInNodeIds.Group;

    /// <inheritdoc />
    public string Name => "Group";

    /// <inheritdoc />
    public string Category => "Utility";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => [];

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        // Create the internal canvas for child nodes
        var internalCanvas = world.Spawn()
            .With(GraphCanvas.Default)
            .With(new GraphCanvasTag())
            .Build();

        // Set internal canvas as child of the group node
        world.SetParent(internalCanvas, node);

        // Add group node data component
        world.Add(node, new GroupNodeData
        {
            InternalCanvas = internalCanvas,
            InterfaceInputs = [],
            InterfaceOutputs = [],
            IsEditing = false
        });

        // Set a larger default width for group nodes
        ref var nodeData = ref world.Get<GraphNode>(node);
        nodeData.Width = 250f;
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Get group data
        if (!world.Has<GroupNodeData>(node))
        {
            return 0f;
        }

        ref readonly var groupData = ref world.Get<GroupNodeData>(node);

        // Draw group body background
        renderer.FillRect(bodyArea.X, bodyArea.Y, bodyArea.Width, bodyArea.Height, GroupBodyColor);

        // Draw a subtle inner border
        renderer.DrawRect(
            bodyArea.X + 4,
            bodyArea.Y + 4,
            bodyArea.Width - 8,
            bodyArea.Height - 8,
            GroupBorderColor,
            1f);

        // Draw interface port labels (when text rendering is available)
        // For now, draw placeholder indicators for interface ports
        DrawInterfacePorts(renderer, bodyArea, groupData);

        // Draw "Double-click to edit" hint in center
        // (Text rendering would go here)

        return bodyArea.Height;
    }

    private static void DrawInterfacePorts(I2DRenderer renderer, Rectangle bodyArea, in GroupNodeData groupData)
    {
        const float portIndicatorSize = 8f;
        const float portSpacing = 20f;

        // Draw input interface indicators on left side
        var startY = bodyArea.Y + 10f;
        for (int i = 0; i < groupData.InterfaceInputs.Count; i++)
        {
            var y = startY + (i * portSpacing);
            renderer.FillCircle(bodyArea.X + 10f, y + (portIndicatorSize / 2f), portIndicatorSize / 2f, GroupLabelColor);
        }

        // Draw output interface indicators on right side
        for (int i = 0; i < groupData.InterfaceOutputs.Count; i++)
        {
            var y = startY + (i * portSpacing);
            renderer.FillCircle(bodyArea.Right - 10f, y + (portIndicatorSize / 2f), portIndicatorSize / 2f, GroupLabelColor);
        }
    }
}
