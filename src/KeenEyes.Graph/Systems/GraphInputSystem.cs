using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that processes input events for graph editing.
/// </summary>
/// <remarks>
/// <para>
/// Handles pan, zoom, node selection, and node dragging for all graph canvases.
/// </para>
/// <para>
/// Controls:
/// <list type="bullet">
/// <item><description>Mouse wheel: Zoom (centered on cursor)</description></item>
/// <item><description>Middle mouse drag: Pan</description></item>
/// <item><description>Left click: Select node (Ctrl to add to selection)</description></item>
/// <item><description>Left drag on empty: Box selection</description></item>
/// <item><description>Left drag on node: Move selected nodes</description></item>
/// <item><description>Delete key: Delete selected nodes/connections</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class GraphInputSystem : SystemBase
{
    private IInputContext? inputContext;
    private GraphContext? graphContext;
    private PortRegistry? portRegistry;

    // Interaction state
    private Vector2 dragStartScreen;
    private Vector2 dragStartPan;
    private Vector2 lastMousePos;
    private Vector2 selectionBoxStart;
    private bool isDraggingNodes;
    private bool isSelecting;
    private bool isPanning;

    // Canvas bounds cache (updated by layout system)
    private Rectangle canvasBounds = new(0, 0, 1280, 720);

    private const float ZoomSpeed = 0.1f;
    private const float DragThreshold = 5f;

    /// <summary>
    /// Sets the canvas screen bounds for hit testing.
    /// </summary>
    /// <param name="bounds">The screen rectangle of the canvas area.</param>
    public void SetCanvasBounds(Rectangle bounds)
    {
        canvasBounds = bounds;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization
        if (inputContext is null && !World.TryGetExtension(out inputContext))
        {
            return;
        }

        if (graphContext is null && !World.TryGetExtension(out graphContext))
        {
            return;
        }

        if (portRegistry is null && !World.TryGetExtension(out portRegistry))
        {
            return;
        }

        var mouse = inputContext!.Mouse;
        var keyboard = inputContext.Keyboard;
        var mousePos = mouse.Position;

        // Process each canvas
        foreach (var canvas in World.Query<GraphCanvas, GraphCanvasTag>())
        {
            ProcessCanvas(canvas, mouse, keyboard, mousePos);
        }

        lastMousePos = mousePos;
    }

    private void ProcessCanvas(Entity canvas, IMouse mouse, IKeyboard keyboard, Vector2 mousePos)
    {
        ref var canvasData = ref World.Get<GraphCanvas>(canvas);
        var origin = new Vector2(canvasBounds.X, canvasBounds.Y);

        // Handle zoom
        var scrollDelta = mouse.GetState().ScrollDelta.Y;
        if (!scrollDelta.IsApproximatelyZero())
        {
            ProcessZoom(ref canvasData, scrollDelta, mousePos, origin);
        }

        // Handle middle mouse panning
        if (mouse.IsButtonDown(MouseButton.Middle))
        {
            if (!isPanning)
            {
                StartPan(ref canvasData, mousePos);
            }
            else
            {
                UpdatePan(ref canvasData, mousePos, origin);
            }
        }
        else if (isPanning)
        {
            EndPan(ref canvasData);
        }

        // Handle left mouse for selection and dragging
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            if (!isDraggingNodes && !isSelecting && canvasData.Mode == GraphInteractionMode.None)
            {
                StartLeftMouseAction(canvas, ref canvasData, mousePos, origin, keyboard);
            }
            else if (isDraggingNodes)
            {
                UpdateNodeDrag(canvas, ref canvasData, mousePos, origin);
            }
            else if (isSelecting)
            {
                UpdateSelection(canvas, ref canvasData, mousePos, origin);
            }
        }
        else
        {
            if (isDraggingNodes)
            {
                EndNodeDrag(ref canvasData);
            }

            if (isSelecting)
            {
                EndSelection(canvas, ref canvasData, origin);
            }
        }

        // Handle delete key
        if (keyboard.IsKeyDown(Key.Delete) || keyboard.IsKeyDown(Key.Backspace))
        {
            DeleteSelected();
        }
    }

    private void ProcessZoom(ref GraphCanvas canvasData, float scrollDelta, Vector2 mousePos, Vector2 origin)
    {
        var zoomFactor = 1f + (scrollDelta > 0 ? ZoomSpeed : -ZoomSpeed);
        var newZoom = Math.Clamp(canvasData.Zoom * zoomFactor, canvasData.MinZoom, canvasData.MaxZoom);

        if (Math.Abs(newZoom - canvasData.Zoom) > 0.001f)
        {
            canvasData.Pan = GraphTransform.ZoomToPoint(canvasData.Pan, canvasData.Zoom, newZoom, mousePos, origin);
            canvasData.Zoom = newZoom;
        }
    }

    private void StartPan(ref GraphCanvas canvasData, Vector2 mousePos)
    {
        isPanning = true;
        dragStartScreen = mousePos;
        dragStartPan = canvasData.Pan;
        canvasData.Mode = GraphInteractionMode.Panning;
    }

    private void UpdatePan(ref GraphCanvas canvasData, Vector2 mousePos, Vector2 origin)
    {
        var screenDelta = mousePos - dragStartScreen;
        var canvasDelta = screenDelta / canvasData.Zoom;
        canvasData.Pan = dragStartPan - canvasDelta;
    }

    private void EndPan(ref GraphCanvas canvasData)
    {
        isPanning = false;
        canvasData.Mode = GraphInteractionMode.None;
    }

    private void StartLeftMouseAction(Entity canvas, ref GraphCanvas canvasData, Vector2 mousePos, Vector2 origin, IKeyboard keyboard)
    {
        dragStartScreen = mousePos;

        // Hit test nodes
        var hitNode = HitTestNodes(canvas, mousePos, canvasData.Pan, canvasData.Zoom, origin);

        if (hitNode.IsValid)
        {
            // Clicked on a node
            var addToSelection = (keyboard.Modifiers & KeyModifiers.Control) != 0;

            if (!World.Has<GraphNodeSelectedTag>(hitNode))
            {
                graphContext!.SelectNode(hitNode, addToSelection);
            }
            else if (addToSelection)
            {
                // Toggle selection if Ctrl-clicking already selected node
                graphContext!.DeselectNode(hitNode);
            }

            // Prepare for potential drag
            isDraggingNodes = false; // Wait for movement threshold
        }
        else
        {
            // Clicked on empty space - start box selection
            selectionBoxStart = mousePos;
            isSelecting = false; // Wait for movement threshold
            canvasData.Mode = GraphInteractionMode.Selecting;

            if ((keyboard.Modifiers & KeyModifiers.Control) == 0)
            {
                graphContext!.ClearSelection();
            }
        }
    }

    private void UpdateNodeDrag(Entity canvas, ref GraphCanvas canvasData, Vector2 mousePos, Vector2 origin)
    {
        if (!isDraggingNodes)
        {
            // Check drag threshold
            var distance = Vector2.Distance(mousePos, dragStartScreen);
            if (distance >= DragThreshold)
            {
                isDraggingNodes = true;
                canvasData.Mode = GraphInteractionMode.DraggingNode;

                // Add dragging tag to selected nodes
                foreach (var node in graphContext!.GetSelectedNodes())
                {
                    if (!World.Has<GraphNodeDraggingTag>(node))
                    {
                        World.Add(node, new GraphNodeDraggingTag());
                    }
                }
            }
        }

        if (isDraggingNodes)
        {
            // Move all selected nodes
            var screenDelta = mousePos - lastMousePos;
            var canvasDelta = screenDelta / canvasData.Zoom;

            foreach (var node in graphContext!.GetSelectedNodes())
            {
                ref var nodeData = ref World.Get<GraphNode>(node);
                nodeData.Position += canvasDelta;

                if (canvasData.SnapToGrid)
                {
                    nodeData.Position = GraphTransform.SnapToGrid(nodeData.Position, canvasData.GridSize);
                }
            }
        }
    }

    private void EndNodeDrag(ref GraphCanvas canvasData)
    {
        isDraggingNodes = false;
        canvasData.Mode = GraphInteractionMode.None;

        // Remove dragging tags
        foreach (var entity in World.Query<GraphNodeDraggingTag>())
        {
            World.Remove<GraphNodeDraggingTag>(entity);
        }
    }

    private void UpdateSelection(Entity canvas, ref GraphCanvas canvasData, Vector2 mousePos, Vector2 origin)
    {
        if (!isSelecting)
        {
            var distance = Vector2.Distance(mousePos, selectionBoxStart);
            if (distance >= DragThreshold)
            {
                isSelecting = true;
            }
        }
    }

    private void EndSelection(Entity canvas, ref GraphCanvas canvasData, Vector2 origin)
    {
        if (isSelecting)
        {
            // Select all nodes within the selection box
            var selectionBox = GraphTransform.CreateSelectionBox(selectionBoxStart, lastMousePos);

            foreach (var node in World.Query<GraphNode>())
            {
                ref readonly var nodeData = ref World.Get<GraphNode>(node);
                if (nodeData.Canvas != canvas)
                {
                    continue;
                }

                var nodeRect = new Rectangle(nodeData.Position.X, nodeData.Position.Y, nodeData.Width, nodeData.Height);
                var screenRect = GraphTransform.CanvasToScreen(nodeRect, canvasData.Pan, canvasData.Zoom, origin);

                if (selectionBox.Intersects(screenRect))
                {
                    if (!World.Has<GraphNodeSelectedTag>(node))
                    {
                        World.Add(node, new GraphNodeSelectedTag());
                    }
                }
            }
        }

        isSelecting = false;
        canvasData.Mode = GraphInteractionMode.None;
    }

    private Entity HitTestNodes(Entity canvas, Vector2 screenPos, Vector2 pan, float zoom, Vector2 origin)
    {
        // Test in reverse order (top-most first, assuming last added is on top)
        Entity hitNode = Entity.Null;

        foreach (var node in World.Query<GraphNode>())
        {
            ref readonly var nodeData = ref World.Get<GraphNode>(node);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            var nodeRect = new Rectangle(nodeData.Position.X, nodeData.Position.Y, nodeData.Width, nodeData.Height);
            if (GraphTransform.HitTest(screenPos, nodeRect, pan, zoom, origin))
            {
                hitNode = node;
                // Don't break - continue to find the "top-most" (last) node
            }
        }

        return hitNode;
    }

    private void DeleteSelected()
    {
        // Collect entities to delete (can't modify during iteration)
        var nodesToDelete = new List<Entity>();
        var connectionsToDelete = new List<Entity>();

        foreach (var node in World.Query<GraphNode, GraphNodeSelectedTag>())
        {
            nodesToDelete.Add(node);
        }

        foreach (var connection in World.Query<GraphConnection, GraphConnectionSelectedTag>())
        {
            connectionsToDelete.Add(connection);
        }

        // Delete connections first
        foreach (var connection in connectionsToDelete)
        {
            graphContext!.DeleteConnection(connection);
        }

        // Delete nodes (this also deletes their connections)
        foreach (var node in nodesToDelete)
        {
            graphContext!.DeleteNode(node);
        }
    }
}
