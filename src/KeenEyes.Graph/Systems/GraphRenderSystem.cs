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
/// <item><description>Connections as bezier curves between ports</description></item>
/// <item><description>Nodes as rounded rectangles with headers and ports</description></item>
/// <item><description>Selection highlight on selected nodes</description></item>
/// <item><description>Selection box during box-select interaction</description></item>
/// <item><description>Port hover highlights during connection creation</description></item>
/// <item><description>Connection preview while dragging from a port</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class GraphRenderSystem : SystemBase, IGraphRenderer
{
    private I2DRenderer? renderer;
    private ITextRenderer? textRenderer;
    private PortRegistry? portRegistry;
    private PortPositionCache? portCache;

    // Canvas screen bounds
    private Rectangle canvasBounds = new(0, 0, 1280, 720);

    // Current rendering context (set per-frame)
    private Vector2 currentOrigin;
    private float currentZoom = 1f;
    private Vector2 currentPan;

    // Colors
    private static readonly Vector4 GridColorMajor = new(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 GridColorMinor = new(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Vector4 NodeBodyColor = new(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Vector4 NodeHeaderColor = new(0.25f, 0.35f, 0.5f, 1f);
    private static readonly Vector4 NodeBorderColor = new(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Vector4 NodeSelectedBorderColor = new(0.3f, 0.6f, 1f, 1f);
    private static readonly Vector4 PortColor = new(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Vector4 ConnectionColor = new(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Vector4 SelectionBoxFillColor = new(0.3f, 0.6f, 1f, 0.15f);
    private static readonly Vector4 SelectionBoxBorderColor = new(0.3f, 0.6f, 1f, 0.8f);
    private static readonly Vector4 PortHighlightValidColor = new(0.3f, 0.9f, 0.3f, 1f);
    private static readonly Vector4 PortHighlightInvalidColor = new(0.9f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 ConnectionPreviewColor = new(0.7f, 0.7f, 0.7f, 0.6f);
    private static readonly Vector4 ConversionIndicatorColor = new(1f, 0.8f, 0.2f, 1f);

    private const float NodeBorderRadius = 6f;
    private const float BorderThickness = 2f;
    private const float ConnectionThickness = 2f;
    private const float PortHighlightRadius = 10f;
    private const float PortHighlightThickness = 3f;
    private const float ConversionIndicatorRadius = 4f;

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

        // Get port position cache
        if (portCache is null && !World.TryGetExtension(out portCache))
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

        // Store current rendering context
        currentOrigin = origin;
        currentZoom = canvasData.Zoom;
        currentPan = canvasData.Pan;

        renderer!.Begin();

        try
        {
            // Clip to canvas bounds
            renderer.PushClip(canvasBounds);

            // Draw grid
            DrawGrid(canvasData, origin);

            // Draw connections
            DrawConnections(canvas, canvasData, origin);

            // Draw pending connection preview
            DrawPendingConnection(canvas, canvasData, origin);

            // Draw nodes
            DrawNodes(canvas, canvasData, origin);

            // Draw port highlights
            DrawHoveredPort(canvas, canvasData, origin);

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

            // Look up port types from the registry
            var sourceType = PortTypeId.Any;
            var targetType = PortTypeId.Any;
            if (portRegistry!.TryGetNodeType(sourceNode.NodeTypeId, out var sourceNodeType) &&
                connection.SourcePortIndex < sourceNodeType.OutputPorts.Length)
            {
                sourceType = sourceNodeType.OutputPorts[connection.SourcePortIndex].TypeId;
            }
            if (portRegistry.TryGetNodeType(targetNode.NodeTypeId, out var targetNodeType) &&
                connection.TargetPortIndex < targetNodeType.InputPorts.Length)
            {
                targetType = targetNodeType.InputPorts[connection.TargetPortIndex].TypeId;
            }

            // Check if type conversion is required
            var requiresConversion = sourceType != targetType
                && sourceType != PortTypeId.Any
                && targetType != PortTypeId.Any;

            // Draw connection curve
            var isSelected = World.Has<GraphConnectionSelectedTag>(connectionEntity);
            DrawConnection(screenStart, screenEnd, sourceType, ConnectionStyle.Bezier, isSelected, requiresConversion);
        }
    }

    private void DrawPendingConnection(Entity canvas, in GraphCanvas canvasData, Vector2 origin)
    {
        if (!World.Has<PendingConnection>(canvas))
        {
            return;
        }

        ref readonly var pending = ref World.Get<PendingConnection>(canvas);

        // Get source port position from cache
        var sourceDirection = pending.IsFromOutput ? PortDirection.Output : PortDirection.Input;
        if (portCache?.TryGetPortPosition(pending.SourceNode, sourceDirection, pending.SourcePortIndex, out var sourceCanvasPos) != true)
        {
            return;
        }

        var screenStart = GraphTransform.CanvasToScreen(sourceCanvasPos, canvasData.Pan, canvasData.Zoom, origin);
        var screenEnd = GraphTransform.CanvasToScreen(pending.CurrentPosition, canvasData.Pan, canvasData.Zoom, origin);

        // If dragging from input, swap start and end
        if (!pending.IsFromOutput)
        {
            (screenStart, screenEnd) = (screenEnd, screenStart);
        }

        // Check for hovered target port type
        PortTypeId? targetType = null;
        if (World.Has<HoveredPort>(canvas))
        {
            ref readonly var hovered = ref World.Get<HoveredPort>(canvas);
            targetType = hovered.TypeId;
        }

        DrawConnectionPreview(screenStart, screenEnd, pending.SourceType, targetType, ConnectionStyle.Bezier);
    }

    private void DrawHoveredPort(Entity canvas, in GraphCanvas canvasData, Vector2 origin)
    {
        if (!World.Has<HoveredPort>(canvas))
        {
            return;
        }

        ref readonly var hovered = ref World.Get<HoveredPort>(canvas);
        var screenPos = GraphTransform.CanvasToScreen(hovered.Position, canvasData.Pan, canvasData.Zoom, origin);

        // Determine if this is a valid target when a connection is being created
        var isValidTarget = true;
        if (World.Has<PendingConnection>(canvas))
        {
            ref readonly var pending = ref World.Get<PendingConnection>(canvas);

            // Invalid if same direction (output to output or input to input)
            var targetDirection = hovered.Direction;
            var sourceDirection = pending.IsFromOutput ? PortDirection.Output : PortDirection.Input;
            if (targetDirection == sourceDirection)
            {
                isValidTarget = false;
            }
            // Invalid if same node
            else if (hovered.Node == pending.SourceNode)
            {
                isValidTarget = false;
            }
            // Check type compatibility
            else
            {
                isValidTarget = PortTypeCompatibility.CanConnect(pending.SourceType, hovered.TypeId);
            }
        }

        DrawPortHighlight(screenPos, hovered.TypeId, isValidTarget);
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
        // Selection box bounds would come from GraphInputSystem
        // For now, draw using canvas selection start/current position if available
        // This will be fully wired when input system exposes selection bounds
    }

    #region IGraphRenderer Implementation

    /// <inheritdoc />
    public void DrawConnection(
        Vector2 start,
        Vector2 end,
        PortTypeId type,
        ConnectionStyle style,
        bool isSelected,
        bool requiresConversion)
    {
        var color = isSelected ? NodeSelectedBorderColor : GetPortColor(type);

        switch (style)
        {
            case ConnectionStyle.Bezier:
                DrawBezierConnection(start, end, color);
                break;
            case ConnectionStyle.Straight:
                renderer!.DrawLine(start, end, color, ConnectionThickness);
                break;
            case ConnectionStyle.Stepped:
                DrawSteppedConnection(start, end, color);
                break;
        }

        // Draw conversion indicator at midpoint if required
        if (requiresConversion)
        {
            var midpoint = (start + end) / 2f;
            renderer!.FillCircle(midpoint.X, midpoint.Y, ConversionIndicatorRadius * currentZoom, ConversionIndicatorColor);
        }
    }

    /// <inheritdoc />
    public void DrawGrid(Rectangle visibleArea, float gridSize, float zoom)
    {
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
            var screenStart = GraphTransform.CanvasToScreen(new Vector2(x, visibleArea.Y), currentPan, zoom, currentOrigin);
            var screenEnd = GraphTransform.CanvasToScreen(new Vector2(x, visibleArea.Y + visibleArea.Height), currentPan, zoom, currentOrigin);

            // Major lines every 5 grid units
            var isMajor = MathF.Abs(x % (gridSize * 5)) < 0.1f;
            var color = isMajor ? GridColorMajor : GridColorMinor;

            renderer!.DrawLine(screenStart, screenEnd, color, 1f);
        }

        for (var y = startY; y <= endY; y += gridSize)
        {
            var screenStart = GraphTransform.CanvasToScreen(new Vector2(visibleArea.X, y), currentPan, zoom, currentOrigin);
            var screenEnd = GraphTransform.CanvasToScreen(new Vector2(visibleArea.X + visibleArea.Width, y), currentPan, zoom, currentOrigin);

            var isMajor = MathF.Abs(y % (gridSize * 5)) < 0.1f;
            var color = isMajor ? GridColorMajor : GridColorMinor;

            renderer!.DrawLine(screenStart, screenEnd, color, 1f);
        }
    }

    /// <inheritdoc />
    public void DrawSelectionBox(Rectangle bounds)
    {
        renderer!.FillRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, SelectionBoxFillColor);
        renderer.DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, SelectionBoxBorderColor, 1f);
    }

    /// <inheritdoc />
    public void DrawPortHighlight(Vector2 position, PortTypeId type, bool isValidTarget)
    {
        var color = isValidTarget ? PortHighlightValidColor : PortHighlightInvalidColor;
        var radius = PortHighlightRadius * currentZoom;

        // Draw highlight ring around port
        renderer!.DrawCircle(position.X, position.Y, radius, color, PortHighlightThickness);
    }

    /// <inheritdoc />
    public void DrawConnectionPreview(
        Vector2 start,
        Vector2 end,
        PortTypeId sourceType,
        PortTypeId? targetType,
        ConnectionStyle style)
    {
        var color = ConnectionPreviewColor;

        // If over a valid target, use the source type color with transparency
        if (targetType.HasValue && PortTypeCompatibility.CanConnect(sourceType, targetType.Value))
        {
            var typeColor = GetPortColor(sourceType);
            color = new Vector4(typeColor.X, typeColor.Y, typeColor.Z, 0.6f);
        }

        switch (style)
        {
            case ConnectionStyle.Bezier:
                DrawBezierConnection(start, end, color);
                break;
            case ConnectionStyle.Straight:
                renderer!.DrawLine(start, end, color, ConnectionThickness);
                break;
            case ConnectionStyle.Stepped:
                DrawSteppedConnection(start, end, color);
                break;
        }
    }

    #endregion

    private void DrawBezierConnection(Vector2 start, Vector2 end, Vector4 color)
    {
        // Calculate control points for horizontal S-curve
        var (cp1, cp2) = BezierCurve.CalculateControlPoints(start, end);

        // Calculate adaptive segment count based on screen distance
        var segments = BezierCurve.CalculateSegmentCount(start, end, currentZoom);

        // Tessellate the bezier curve
        Span<Vector2> points = stackalloc Vector2[segments + 1];
        BezierCurve.TessellateInto(start, cp1, cp2, end, points);

        // Draw line strip
        renderer!.DrawLineStrip(points, color, ConnectionThickness);
    }

    private void DrawSteppedConnection(Vector2 start, Vector2 end, Vector4 color)
    {
        // Horizontal-vertical-horizontal stepped path
        var midX = (start.X + end.X) / 2f;

        Span<Vector2> points = stackalloc Vector2[4];
        points[0] = start;
        points[1] = new Vector2(midX, start.Y);
        points[2] = new Vector2(midX, end.Y);
        points[3] = end;

        renderer!.DrawLineStrip(points, color, ConnectionThickness);
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
