using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that renders graph canvases, nodes, and connections.
/// </summary>
/// <remarks>
/// <para>
/// Renders the following elements:
/// <list type="bullet">
/// <item><description>Grid lines in the visible canvas area</description></item>
/// <item><description>Connections as straight lines between ports</description></item>
/// <item><description>Nodes as rounded rectangles with headers and ports</description></item>
/// <item><description>Selection highlight on selected nodes</description></item>
/// <item><description>Selection box during box-select interaction</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class GraphRenderSystem : SystemBase
{
    private I2DRenderer? renderer;
    private ITextRenderer? textRenderer;
    private PortRegistry? portRegistry;

    // Canvas screen bounds
    private Rectangle canvasBounds = new(0, 0, 1280, 720);

    // Colors
    private static readonly Vector4 GridColorMajor = new(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 GridColorMinor = new(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Vector4 NodeBodyColor = new(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Vector4 NodeHeaderColor = new(0.25f, 0.35f, 0.5f, 1f);
    private static readonly Vector4 NodeBorderColor = new(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Vector4 NodeSelectedBorderColor = new(0.3f, 0.6f, 1f, 1f);
    private static readonly Vector4 PortColor = new(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Vector4 ConnectionColor = new(0.6f, 0.6f, 0.6f, 1f);
    // Note: SelectionBoxColor, SelectionBoxBorderColor, and TextColor will be used
    // when selection box drawing and text rendering are implemented in later phases

    private const float NodeBorderRadius = 6f;
    private const float BorderThickness = 2f;
    private const float ConnectionThickness = 2f;

    /// <summary>
    /// Sets the canvas screen bounds.
    /// </summary>
    /// <param name="bounds">The screen rectangle of the canvas area.</param>
    public void SetCanvasBounds(Rectangle bounds)
    {
        canvasBounds = bounds;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Get renderer
        if (renderer is null && !World.TryGetExtension(out renderer))
        {
            return;
        }

        // Text renderer is optional
        if (textRenderer is null)
        {
            World.TryGetExtension(out textRenderer);
        }

        // Get port registry
        if (portRegistry is null && !World.TryGetExtension(out portRegistry))
        {
            return;
        }

        // Render each canvas
        foreach (var canvas in World.Query<GraphCanvas, GraphCanvasTag>())
        {
            RenderCanvas(canvas);
        }
    }

    private void RenderCanvas(Entity canvas)
    {
        ref readonly var canvasData = ref World.Get<GraphCanvas>(canvas);
        var origin = new Vector2(canvasBounds.X, canvasBounds.Y);

        renderer!.Begin();

        try
        {
            // Clip to canvas bounds
            renderer.PushClip(canvasBounds);

            // Draw grid
            DrawGrid(canvasData, origin);

            // Draw connections
            DrawConnections(canvas, canvasData, origin);

            // Draw nodes
            DrawNodes(canvas, canvasData, origin);

            // Draw selection box if selecting
            if (canvasData.Mode == GraphInteractionMode.Selecting)
            {
                DrawSelectionBox(canvasData);
            }

            renderer.PopClip();
        }
        finally
        {
            renderer.End();
        }
    }

    private void DrawGrid(in GraphCanvas canvasData, Vector2 origin)
    {
        var visibleArea = GraphTransform.GetVisibleArea(canvasBounds, canvasData.Pan, canvasData.Zoom);
        var gridSize = canvasData.GridSize;

        if (gridSize <= 0)
        {
            return;
        }

        // Calculate grid line range
        var startX = MathF.Floor(visibleArea.X / gridSize) * gridSize;
        var startY = MathF.Floor(visibleArea.Y / gridSize) * gridSize;
        var endX = visibleArea.X + visibleArea.Width;
        var endY = visibleArea.Y + visibleArea.Height;

        // Draw minor grid lines (every grid unit)
        for (var x = startX; x <= endX; x += gridSize)
        {
            var screenStart = GraphTransform.CanvasToScreen(new Vector2(x, visibleArea.Y), canvasData.Pan, canvasData.Zoom, origin);
            var screenEnd = GraphTransform.CanvasToScreen(new Vector2(x, visibleArea.Y + visibleArea.Height), canvasData.Pan, canvasData.Zoom, origin);

            // Major lines every 5 grid units
            var isMajor = MathF.Abs(x % (gridSize * 5)) < 0.1f;
            var color = isMajor ? GridColorMajor : GridColorMinor;

            renderer!.DrawLine(screenStart, screenEnd, color, 1f);
        }

        for (var y = startY; y <= endY; y += gridSize)
        {
            var screenStart = GraphTransform.CanvasToScreen(new Vector2(visibleArea.X, y), canvasData.Pan, canvasData.Zoom, origin);
            var screenEnd = GraphTransform.CanvasToScreen(new Vector2(visibleArea.X + visibleArea.Width, y), canvasData.Pan, canvasData.Zoom, origin);

            var isMajor = MathF.Abs(y % (gridSize * 5)) < 0.1f;
            var color = isMajor ? GridColorMajor : GridColorMinor;

            renderer!.DrawLine(screenStart, screenEnd, color, 1f);
        }
    }

    private void DrawConnections(Entity canvas, in GraphCanvas canvasData, Vector2 origin)
    {
        foreach (var connectionEntity in World.Query<GraphConnection>())
        {
            ref readonly var connection = ref World.Get<GraphConnection>(connectionEntity);
            if (connection.Canvas != canvas)
            {
                continue;
            }

            // Get source and target node positions
            if (!World.IsAlive(connection.SourceNode) || !World.IsAlive(connection.TargetNode))
            {
                continue;
            }

            ref readonly var sourceNode = ref World.Get<GraphNode>(connection.SourceNode);
            ref readonly var targetNode = ref World.Get<GraphNode>(connection.TargetNode);

            // Calculate port positions
            var sourcePortY = GraphLayoutSystem.HeaderHeight + (connection.SourcePortIndex * GraphLayoutSystem.PortRowHeight) + (GraphLayoutSystem.PortRowHeight / 2);
            var targetPortY = GraphLayoutSystem.HeaderHeight + (connection.TargetPortIndex * GraphLayoutSystem.PortRowHeight) + (GraphLayoutSystem.PortRowHeight / 2);

            var sourcePos = sourceNode.Position + new Vector2(sourceNode.Width, sourcePortY);
            var targetPos = targetNode.Position + new Vector2(0, targetPortY);

            // Convert to screen coordinates
            var screenStart = GraphTransform.CanvasToScreen(sourcePos, canvasData.Pan, canvasData.Zoom, origin);
            var screenEnd = GraphTransform.CanvasToScreen(targetPos, canvasData.Pan, canvasData.Zoom, origin);

            // Draw connection line
            var isSelected = World.Has<GraphConnectionSelectedTag>(connectionEntity);
            var color = isSelected ? NodeSelectedBorderColor : ConnectionColor;
            renderer!.DrawLine(screenStart, screenEnd, color, ConnectionThickness);
        }
    }

    private void DrawNodes(Entity canvas, in GraphCanvas canvasData, Vector2 origin)
    {
        foreach (var node in World.Query<GraphNode>())
        {
            ref readonly var nodeData = ref World.Get<GraphNode>(node);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            DrawNode(node, in nodeData, canvasData, origin);
        }
    }

    private void DrawNode(Entity node, ref readonly GraphNode nodeData, in GraphCanvas canvasData, Vector2 origin)
    {
        var nodeRect = new Rectangle(nodeData.Position.X, nodeData.Position.Y, nodeData.Width, nodeData.Height);
        var screenRect = GraphTransform.CanvasToScreen(nodeRect, canvasData.Pan, canvasData.Zoom, origin);

        var isSelected = World.Has<GraphNodeSelectedTag>(node);
        var borderColor = isSelected ? NodeSelectedBorderColor : NodeBorderColor;
        var scaledRadius = NodeBorderRadius * canvasData.Zoom;

        // Draw node body
        renderer!.FillRoundedRect(screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height, scaledRadius, NodeBodyColor);

        // Draw header
        var headerHeight = GraphLayoutSystem.HeaderHeight * canvasData.Zoom;
        renderer.FillRoundedRect(screenRect.X, screenRect.Y, screenRect.Width, headerHeight, scaledRadius, NodeHeaderColor);

        // Draw node border
        renderer.DrawRoundedRect(screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height, scaledRadius, borderColor, BorderThickness);

        // Note: Text rendering for node name would go here if textRenderer is available
        // For Phase 1, we skip text since it requires font setup

        // Draw ports
        DrawPorts(node, in nodeData, canvasData, origin);
    }

    private void DrawPorts(Entity node, ref readonly GraphNode nodeData, in GraphCanvas canvasData, Vector2 origin)
    {
        if (!portRegistry!.TryGetNodeType(nodeData.NodeTypeId, out var nodeType))
        {
            return;
        }

        var portRadius = GraphLayoutSystem.PortRadius * canvasData.Zoom;

        // Draw input ports (left edge)
        for (int i = 0; i < nodeType.InputPorts.Length; i++)
        {
            var yOffset = GraphLayoutSystem.HeaderHeight + (i * GraphLayoutSystem.PortRowHeight) + (GraphLayoutSystem.PortRowHeight / 2);
            var canvasPos = nodeData.Position + new Vector2(0, yOffset);
            var screenPos = GraphTransform.CanvasToScreen(canvasPos, canvasData.Pan, canvasData.Zoom, origin);

            var port = nodeType.InputPorts[i];
            var portColor = GetPortColor(port.TypeId);
            renderer!.FillCircle(screenPos.X, screenPos.Y, portRadius, portColor);
        }

        // Draw output ports (right edge)
        for (int i = 0; i < nodeType.OutputPorts.Length; i++)
        {
            var yOffset = GraphLayoutSystem.HeaderHeight + (i * GraphLayoutSystem.PortRowHeight) + (GraphLayoutSystem.PortRowHeight / 2);
            var canvasPos = nodeData.Position + new Vector2(nodeData.Width, yOffset);
            var screenPos = GraphTransform.CanvasToScreen(canvasPos, canvasData.Pan, canvasData.Zoom, origin);

            var port = nodeType.OutputPorts[i];
            var portColor = GetPortColor(port.TypeId);
            renderer!.FillCircle(screenPos.X, screenPos.Y, portRadius, portColor);
        }
    }

    private void DrawSelectionBox(in GraphCanvas canvasData)
    {
        // The selection box coordinates would be provided by the input system
        // For now, this is a placeholder - the actual implementation needs
        // state from GraphInputSystem
    }

    private static Vector4 GetPortColor(PortTypeId typeId)
    {
        return typeId switch
        {
            PortTypeId.Float or PortTypeId.Float2 or PortTypeId.Float3 or PortTypeId.Float4 => new Vector4(0.4f, 0.8f, 0.4f, 1f),
            PortTypeId.Int or PortTypeId.Int2 or PortTypeId.Int3 or PortTypeId.Int4 => new Vector4(0.3f, 0.5f, 0.8f, 1f),
            PortTypeId.Bool => new Vector4(0.8f, 0.3f, 0.3f, 1f),
            PortTypeId.Entity => new Vector4(0.8f, 0.6f, 0.2f, 1f),
            PortTypeId.Flow => new Vector4(1f, 1f, 1f, 1f),
            _ => PortColor
        };
    }
}
