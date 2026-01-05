using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that processes input events for graph editing.
/// </summary>
/// <remarks>
/// <para>
/// Handles pan, zoom, node selection, node dragging, and port interaction for all graph canvases.
/// </para>
/// <para>
/// Controls:
/// <list type="bullet">
/// <item><description>Mouse wheel: Zoom (centered on cursor)</description></item>
/// <item><description>Middle mouse drag: Pan</description></item>
/// <item><description>Left click on port: Start connection drag</description></item>
/// <item><description>Left click on node: Select node (Ctrl to add to selection)</description></item>
/// <item><description>Left drag on empty: Box selection</description></item>
/// <item><description>Left drag on node: Move selected nodes</description></item>
/// <item><description>Delete key: Delete selected nodes/connections</description></item>
/// <item><description>ESC/Right click: Cancel connection drag</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class GraphInputSystem : SystemBase
{
    private IInputContext? inputContext;
    private GraphContext? graphContext;
    private PortRegistry? portRegistry;
    private PortPositionCache? portCache;
    private IUndoRedoManager? undoManager;

    // Interaction state
    private Vector2 dragStartScreen;
    private Vector2 dragStartPan;
    private Vector2 lastMousePos;
    private Vector2 selectionBoxStart;
    private bool isDraggingNodes;
    private bool isSelecting;
    private bool isPanning;

    // Undo support for node dragging
    private readonly Dictionary<Entity, Vector2> dragStartPositions = [];

    // Key debouncing
    private readonly HashSet<Key> keysDownLastFrame = [];

    // Connection dragging state
    private Entity connectionSourceNode;
    private int connectionSourcePort;
    private bool connectionFromOutput;
    private PortTypeId connectionSourceType;
    private bool isConnecting;

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

        if (portCache is null && !World.TryGetExtension(out portCache))
        {
            return;
        }

        if (undoManager is null)
        {
            World.TryGetExtension(out undoManager);
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

        // Update key state for next frame
        UpdateKeyState(keyboard);
    }

    private void ProcessCanvas(Entity canvas, IMouse mouse, IKeyboard keyboard, Vector2 mousePos)
    {
        ref var canvasData = ref World.Get<GraphCanvas>(canvas);
        var origin = new Vector2(canvasBounds.X, canvasBounds.Y);

        // Skip input if context menu is open
        if (canvasData.Mode == GraphInteractionMode.ContextMenu)
        {
            return;
        }

        // Handle keyboard shortcuts (before other input)
        HandleKeyboardShortcuts(canvas, ref canvasData, keyboard, mousePos, origin);

        // Always update hovered port
        UpdateHoveredPort(canvas, in canvasData, mousePos, origin);

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

        // Handle connection in progress
        if (isConnecting && canvasData.Mode == GraphInteractionMode.ConnectingPort)
        {
            UpdateConnection(canvas, ref canvasData, mousePos, origin, mouse, keyboard);
            return; // Connection mode takes exclusive control
        }

        // Handle left mouse for selection, dragging, and port interaction
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

        // First check for port hit (ports have priority over node body)
        if (portCache!.HitTestPort(mousePos, canvasData.Pan, canvasData.Zoom, origin,
            out var hitPortNode, out var hitPortDir, out var hitPortIndex))
        {
            // Check if this is an input port with an existing connection (disconnect + reconnect)
            if (hitPortDir == PortDirection.Input)
            {
                var existingConnection = FindConnectionToInput(hitPortNode, hitPortIndex);
                if (existingConnection.IsValid)
                {
                    // Get the source info before deleting
                    ref readonly var conn = ref World.Get<GraphConnection>(existingConnection);
                    var sourceNode = conn.SourceNode;
                    var sourcePort = conn.SourcePortIndex;

                    // Delete the existing connection
                    graphContext!.DeleteConnection(existingConnection);

                    // Start connection from the original source output
                    StartConnection(canvas, ref canvasData, sourceNode, PortDirection.Output, sourcePort, mousePos, origin);
                    return;
                }
            }

            // Start a new connection from this port
            StartConnection(canvas, ref canvasData, hitPortNode, hitPortDir, hitPortIndex, mousePos, origin);
            return;
        }

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

                // Store start positions for undo
                dragStartPositions.Clear();
                foreach (var node in graphContext!.GetSelectedNodes())
                {
                    if (World.IsAlive(node))
                    {
                        ref readonly var nodeData = ref World.Get<GraphNode>(node);
                        dragStartPositions[node] = nodeData.Position;

                        if (!World.Has<GraphNodeDraggingTag>(node))
                        {
                            World.Add(node, new GraphNodeDraggingTag());
                        }
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
        // Create undo command if nodes were actually moved
        if (dragStartPositions.Count > 0)
        {
            var nodePositions = new Dictionary<Entity, (Vector2 OldPosition, Vector2 NewPosition)>();

            foreach (var kvp in dragStartPositions)
            {
                var node = kvp.Key;
                var startPos = kvp.Value;

                if (World.IsAlive(node) && World.Has<GraphNode>(node))
                {
                    ref readonly var nodeData = ref World.Get<GraphNode>(node);
                    if (nodeData.Position != startPos)
                    {
                        nodePositions[node] = (startPos, nodeData.Position);
                    }
                }
            }

            if (nodePositions.Count > 0)
            {
                graphContext!.MoveNodesUndoable(nodePositions);
            }

            dragStartPositions.Clear();
        }

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

        // Use undoable methods
        if (nodesToDelete.Count > 0)
        {
            graphContext!.DeleteNodesUndoable(nodesToDelete);
        }

        foreach (var connection in connectionsToDelete)
        {
            graphContext!.DeleteConnectionUndoable(connection);
        }
    }

    private void HandleKeyboardShortcuts(Entity canvas, ref GraphCanvas canvasData, IKeyboard keyboard, Vector2 mousePos, Vector2 origin)
    {
        var ctrl = (keyboard.Modifiers & KeyModifiers.Control) != 0;

        // Delete/Backspace - delete selected
        if (WasKeyJustPressed(keyboard, Key.Delete) || WasKeyJustPressed(keyboard, Key.Backspace))
        {
            DeleteSelected();
            return;
        }

        // Ctrl+Z - Undo
        if (ctrl && WasKeyJustPressed(keyboard, Key.Z))
        {
            undoManager?.Undo();
            return;
        }

        // Ctrl+Y - Redo
        if (ctrl && WasKeyJustPressed(keyboard, Key.Y))
        {
            undoManager?.Redo();
            return;
        }

        // Ctrl+A - Select All
        if (ctrl && WasKeyJustPressed(keyboard, Key.A))
        {
            graphContext!.SelectAll(canvas);
            return;
        }

        // Ctrl+D - Duplicate
        if (ctrl && WasKeyJustPressed(keyboard, Key.D))
        {
            var selectedNodes = graphContext!.GetSelectedNodes().ToList();
            if (selectedNodes.Count > 0)
            {
                graphContext.DuplicateSelectionUndoable();
            }
            return;
        }

        // F - Frame Selection
        if (WasKeyJustPressed(keyboard, Key.F))
        {
            var selectedNodes = graphContext!.GetSelectedNodes().ToList();
            if (selectedNodes.Count > 0)
            {
                graphContext.FrameSelection(canvas);
            }
            return;
        }

        // Escape - Clear selection
        if (WasKeyJustPressed(keyboard, Key.Escape))
        {
            graphContext!.ClearSelection();
            return;
        }

        // Space - Space+drag panning (handled in mouse input, but we track the state here)
        if (keyboard.IsKeyDown(Key.Space) && !World.Has<GraphSpacePanningTag>(canvas))
        {
            World.Add(canvas, new GraphSpacePanningTag());
        }
        else if (!keyboard.IsKeyDown(Key.Space) && World.Has<GraphSpacePanningTag>(canvas))
        {
            World.Remove<GraphSpacePanningTag>(canvas);
        }

        // Right-click - Open context menu (handled in mouse logic, but check here)
        if (inputContext?.Mouse.IsButtonDown(MouseButton.Right) == true && WasKeyJustPressed(keyboard, Key.Unknown))
        {
            OpenContextMenu(canvas, ref canvasData, mousePos, origin);
        }
    }

    private void OpenContextMenu(Entity canvas, ref GraphCanvas canvasData, Vector2 screenPos, Vector2 origin)
    {
        var canvasPos = GraphTransform.ScreenToCanvas(screenPos, canvasData.Pan, canvasData.Zoom, origin);

        // Determine menu type based on what's under the cursor
        var hitNode = HitTestNodes(canvas, screenPos, canvasData.Pan, canvasData.Zoom, origin);

        ContextMenuType menuType;
        Entity targetEntity;

        if (hitNode.IsValid)
        {
            menuType = ContextMenuType.Node;
            targetEntity = hitNode;
        }
        else
        {
            menuType = ContextMenuType.Canvas;
            targetEntity = Entity.Null;
        }

        World.Add(canvas, new GraphContextMenu
        {
            ScreenPosition = screenPos,
            CanvasPosition = canvasPos,
            MenuType = menuType,
            TargetEntity = targetEntity,
            SearchFilter = string.Empty,
            SelectedIndex = 0
        });

        canvasData.Mode = GraphInteractionMode.ContextMenu;
    }

    private bool WasKeyJustPressed(IKeyboard keyboard, Key key)
    {
        var isDownNow = keyboard.IsKeyDown(key);
        var wasDownLastFrame = keysDownLastFrame.Contains(key);
        return isDownNow && !wasDownLastFrame;
    }

    private void UpdateKeyState(IKeyboard keyboard)
    {
        keysDownLastFrame.Clear();

        // Track all keys that are currently down
        for (var key = Key.Unknown; key <= Key.Slash; key++)
        {
            if (keyboard.IsKeyDown(key))
            {
                keysDownLastFrame.Add(key);
            }
        }
    }

    #region Port Interaction

    private void UpdateHoveredPort(Entity canvas, in GraphCanvas canvasData, Vector2 mousePos, Vector2 origin)
    {
        // Remove existing hover
        if (World.Has<HoveredPort>(canvas))
        {
            World.Remove<HoveredPort>(canvas);
        }

        if (portCache!.HitTestPort(mousePos, canvasData.Pan, canvasData.Zoom, origin,
            out var hitNode, out var hitDir, out var hitIndex))
        {
            // Get port type from registry
            if (!World.IsAlive(hitNode))
            {
                return;
            }

            ref readonly var node = ref World.Get<GraphNode>(hitNode);
            if (!portRegistry!.TryGetNodeType(node.NodeTypeId, out var nodeType))
            {
                return;
            }

            var ports = hitDir == PortDirection.Input ? nodeType.InputPorts : nodeType.OutputPorts;
            if (hitIndex >= ports.Length)
            {
                return;
            }

            var portDef = ports[hitIndex];
            var pos = portCache.GetPortPosition(hitNode, hitDir, hitIndex);

            World.Add(canvas, new HoveredPort
            {
                Node = hitNode,
                Direction = hitDir,
                PortIndex = hitIndex,
                TypeId = portDef.TypeId,
                Position = pos
            });
        }
    }

    private void StartConnection(
        Entity canvas,
        ref GraphCanvas canvasData,
        Entity node,
        PortDirection direction,
        int portIndex,
        Vector2 mousePos,
        Vector2 origin)
    {
        if (!World.IsAlive(node))
        {
            return;
        }

        ref readonly var nodeData = ref World.Get<GraphNode>(node);
        if (!portRegistry!.TryGetNodeType(nodeData.NodeTypeId, out var nodeType))
        {
            return;
        }

        var ports = direction == PortDirection.Input ? nodeType.InputPorts : nodeType.OutputPorts;
        if (portIndex >= ports.Length)
        {
            return;
        }

        var portDef = ports[portIndex];

        connectionSourceNode = node;
        connectionSourcePort = portIndex;
        connectionFromOutput = direction == PortDirection.Output;
        connectionSourceType = portDef.TypeId;
        isConnecting = true;

        canvasData.Mode = GraphInteractionMode.ConnectingPort;

        // Add pending connection component
        var canvasPos = GraphTransform.ScreenToCanvas(mousePos, canvasData.Pan, canvasData.Zoom, origin);
        World.Add(canvas, new PendingConnection
        {
            SourceNode = node,
            SourcePortIndex = portIndex,
            IsFromOutput = connectionFromOutput,
            CurrentPosition = canvasPos,
            SourceType = connectionSourceType
        });
    }

    private void UpdateConnection(
        Entity canvas,
        ref GraphCanvas canvasData,
        Vector2 mousePos,
        Vector2 origin,
        IMouse mouse,
        IKeyboard keyboard)
    {
        // Update preview position
        if (World.Has<PendingConnection>(canvas))
        {
            ref var pending = ref World.Get<PendingConnection>(canvas);
            pending.CurrentPosition = GraphTransform.ScreenToCanvas(
                mousePos, canvasData.Pan, canvasData.Zoom, origin);
        }

        // Cancel on ESC or right-click
        if (keyboard.IsKeyDown(Key.Escape) || mouse.IsButtonDown(MouseButton.Right))
        {
            CancelConnection(canvas, ref canvasData);
            return;
        }

        // Complete on left mouse release
        if (!mouse.IsButtonDown(MouseButton.Left))
        {
            CompleteConnection(canvas, ref canvasData, mousePos, origin);
        }
    }

    private void CompleteConnection(Entity canvas, ref GraphCanvas canvasData, Vector2 mousePos, Vector2 origin)
    {
        if (portCache!.HitTestPort(mousePos, canvasData.Pan, canvasData.Zoom, origin,
            out var targetNode, out var targetDir, out var targetIndex))
        {
            // Validate direction (output to input or input to output)
            var isValidDirection = connectionFromOutput
                ? targetDir == PortDirection.Input
                : targetDir == PortDirection.Output;

            if (isValidDirection && targetNode != connectionSourceNode)
            {
                // Get target port type for validation
                if (World.IsAlive(targetNode))
                {
                    ref readonly var targetNodeData = ref World.Get<GraphNode>(targetNode);
                    if (portRegistry!.TryGetNodeType(targetNodeData.NodeTypeId, out var targetNodeType))
                    {
                        var targetPorts = targetDir == PortDirection.Input
                            ? targetNodeType.InputPorts
                            : targetNodeType.OutputPorts;

                        if (targetIndex < targetPorts.Length)
                        {
                            var targetType = targetPorts[targetIndex].TypeId;

                            // Determine actual source and target based on drag direction
                            PortTypeId srcType, tgtType;
                            Entity srcNode, tgtNode;
                            int srcPort, tgtPort;

                            if (connectionFromOutput)
                            {
                                srcNode = connectionSourceNode;
                                srcPort = connectionSourcePort;
                                srcType = connectionSourceType;
                                tgtNode = targetNode;
                                tgtPort = targetIndex;
                                tgtType = targetType;
                            }
                            else
                            {
                                // Dragging from input - target is actually the source
                                srcNode = targetNode;
                                srcPort = targetIndex;
                                srcType = targetType;
                                tgtNode = connectionSourceNode;
                                tgtPort = connectionSourcePort;
                                tgtType = connectionSourceType;
                            }

                            // Validate type compatibility
                            if (PortTypeCompatibility.CanConnect(srcType, tgtType))
                            {
                                graphContext!.Connect(srcNode, srcPort, tgtNode, tgtPort);
                            }
                        }
                    }
                }
            }
        }

        CancelConnection(canvas, ref canvasData);
    }

    private void CancelConnection(Entity canvas, ref GraphCanvas canvasData)
    {
        isConnecting = false;
        connectionSourceNode = Entity.Null;
        canvasData.Mode = GraphInteractionMode.None;

        if (World.Has<PendingConnection>(canvas))
        {
            World.Remove<PendingConnection>(canvas);
        }
    }

    private Entity FindConnectionToInput(Entity node, int portIndex)
    {
        foreach (var connEntity in World.Query<GraphConnection>())
        {
            ref readonly var conn = ref World.Get<GraphConnection>(connEntity);
            if (conn.TargetNode == node && conn.TargetPortIndex == portIndex)
            {
                return connEntity;
            }
        }

        return Entity.Null;
    }

    #endregion
}
