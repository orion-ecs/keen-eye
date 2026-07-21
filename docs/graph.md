# Node Graph Editor

The `KeenEyes.Graph` library (built on primitives from `KeenEyes.Graph.Abstractions`) provides a visual, pan/zoom node-graph editor for the ECS: canvases, nodes, ports, and connections, wired up as a plugin.

## Overview

Graphs use a **hybrid data model**:

- **Nodes and connections are entities.** A node has a `GraphNode` component; a connection has a `GraphConnection` component. This gives them independent lifecycle, tagging (selection, dragging, etc.), and normal ECS queries.
- **Ports are not entities.** Each node type declares a fixed set of `PortDefinition`s (name, direction, data type, layout offset). Port metadata lives in a registry and is looked up by index, keyed off the node's `NodeTypeId`.

Custom node types (e.g. math operators, KESL shader nodes) are added by implementing `INodeTypeDefinition` and registering it with the `NodeTypeRegistry` extension.

## Quick Start

### Installation

```csharp
using KeenEyes.Graph;

using var world = new World();

// Install the graph editor plugin
world.InstallPlugin(new GraphPlugin());
```

`GraphPlugin` registers the graph components (`GraphCanvas`, `GraphNode`, `GraphConnection`, and related state/tag components) and the following systems:

| Phase | Order | System | Responsibility |
|-------|-------|--------|-----------------|
| `EarlyUpdate` | -5 | `GraphWidgetSystem` | Process widget input (text editing, slider drag) |
| `EarlyUpdate` | 0 | `GraphInputSystem` | Pan, zoom, select, drag nodes |
| `EarlyUpdate` | 5 | `GraphContextMenuSystem` | Node-creation and context menus |
| `Update` | 0 | `GraphViewAnimationSystem` | Animate pan/zoom transitions (`FrameSelection`) |
| `LateUpdate` | -5 | `GraphLayoutSystem` | Calculate node bounds, port positions |
| `Render` | 90 | `GraphRenderSystem` | Draw grid, nodes, connections |

The plugin also exposes several world extensions, retrievable via `world.GetExtension<T>()`:

- `GraphContext` — graph manipulation API (create/connect/delete/select).
- `PortRegistry` — low-level port metadata storage.
- `NodeTypeRegistry` — `INodeTypeDefinition` registry (wraps `PortRegistry`).
- `PortPositionCache` — cached screen-space port positions used by input/render systems.

It also registers three built-in node types: `CommentNode`, `RerouteNode`, and `GroupNode` (type IDs 1–3, see [Node Types](#node-types) below).

### Your First Graph

```csharp
using KeenEyes.Graph;

var graph = world.GetExtension<GraphContext>();
var registry = world.GetExtension<NodeTypeRegistry>();

// Register a custom node type before creating nodes of that type
// (AddNode.TypeId is 102 - see the custom node type example below)
registry.Register<AddNode>();

// Create a canvas and two nodes
var canvas = graph.CreateCanvas("MyGraph");
var nodeA = graph.CreateNode(canvas, 102, new Vector2(0, 0));
var nodeB = graph.CreateNode(canvas, 102, new Vector2(300, 0));

// Connect output port 0 on nodeA to input port 0 on nodeB
graph.Connect(nodeA, 0, nodeB, 0);
```

`GraphContext.CreateNode` validates the canvas and node type but does not call `INodeTypeDefinition.Initialize`; use `NodeTypeRegistry.CreateNode` (or the `*Undoable` methods below) when a node type needs to add its own components on creation, as `CommentNode` does for `CommentNodeData`.

## Core Concepts

### GraphCanvas

`GraphCanvas` is the root component for a graph editor surface. It stores pan/zoom state, grid settings, and the current `GraphInteractionMode`. All nodes and connections are parented (via `world.SetParent`) to a canvas entity.

```csharp
public struct GraphCanvas : IComponent
{
    public Vector2 Pan;
    public float Zoom;
    public float MinZoom;
    public float MaxZoom;
    public float GridSize;
    public bool SnapToGrid;
    public GraphInteractionMode Mode;
}
```

`GraphCanvas.Default` gives `Zoom = 1.0f`, `MinZoom = 0.25f`, `MaxZoom = 4.0f`, `GridSize = 20f`, `SnapToGrid = false`. `GraphContext.CreateCanvas` builds an entity with this default plus a `GraphCanvasTag`.

### GraphNode and GraphConnection

```csharp
public struct GraphNode : IComponent
{
    public Vector2 Position;   // canvas coordinates, top-left
    public float Width;
    public float Height;       // calculated by GraphLayoutSystem
    public int NodeTypeId;
    public Entity Canvas;
    public string? DisplayName;
}

public struct GraphConnection : IComponent
{
    public Entity SourceNode;
    public int SourcePortIndex;
    public Entity TargetNode;
    public int TargetPortIndex;
    public Entity Canvas;
}
```

Connections reference ports by index into the source/target node type's `InputPorts`/`OutputPorts` arrays — ports themselves have no entity identity.

### Ports and Type Compatibility

A `PortDefinition` describes one input or output slot on a node type:

```csharp
public readonly record struct PortDefinition(
    string Name,
    PortDirection Direction,
    PortTypeId TypeId,
    Vector2 LocalOffset,
    bool AllowMultiple = false);
```

Use the `PortDefinition.Input(...)` / `PortDefinition.Output(...)` factory methods rather than the constructor directly:

```csharp
PortDefinition.Input("A", PortTypeId.Float, yOffset: 60f);
PortDefinition.Output("Result", PortTypeId.Float, yOffset: 75f);
```

`PortTypeId` covers `Any`, `Float`/`Float2`/`Float3`/`Float4`, `Int`/`Int2`/`Int3`/`Int4`, `Bool`, `Entity`, and `Flow` (execution flow for control-flow nodes). `PortTypeCompatibility.CanConnect(source, target)` allows exact matches, anything into `Any`, and implicit widening conversions (`float`→`float2/3/4`, `int`→`int2/3/4`, `int`→`float`); `Flow` only connects to `Flow`, and narrowing conversions are always rejected. `GraphContext.Connect` calls this before creating a `GraphConnection`.

### Defining a Custom Node Type

Implement `INodeTypeDefinition` and register it with `NodeTypeRegistry`. Type IDs 1–100 are reserved for built-ins (`BuiltInNodeIds`); user-defined types must use IDs starting at `BuiltInNodeIds.UserDefinedStart` (101):

```csharp
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

public sealed class AddNode : INodeTypeDefinition
{
    public int TypeId => 102;
    public string Name => "Add";
    public string Category => "Math";

    public IReadOnlyList<PortDefinition> InputPorts { get; } =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 90f)
    ];

    public IReadOnlyList<PortDefinition> OutputPorts { get; } =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 75f)
    ];

    public bool IsCollapsible => true;

    public void Initialize(Entity node, IWorld world) { }

    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
        => 0f; // no custom body content, ports render by default
}
```

Register it during setup, then create instances through the registry (which calls `Initialize` for you) or `GraphContext`:

```csharp
var registry = world.GetExtension<NodeTypeRegistry>();
registry.Register<AddNode>();

var node = registry.CreateNode(canvas, 102, new Vector2(100, 100), world);
```

This mirrors how the `KeenEyes.Sample.UI` sample registers its own math nodes (`samples/KeenEyes.Sample.UI/Nodes/MathNodes.cs`) after installing `GraphPlugin`:

```csharp
world.InstallPlugin(new GraphPlugin());
// ...
var nodeTypeRegistry = world.GetExtension<NodeTypeRegistry>();
nodeTypeRegistry.Register<NumberNode>();
nodeTypeRegistry.Register<AddNode>();
nodeTypeRegistry.Register<MultiplyNode>();
```

### Node Types

`GraphPlugin` registers three built-in node types (`BuiltInNodeIds.Comment` = 1, `.Reroute` = 2, `.Group` = 3):

- **`CommentNode`** — no ports; adds a `CommentNodeData` component (`Text`, `FontScale`) for a free-form text annotation on the canvas.
- **`RerouteNode`** — a single `Any`-typed input and output, used to visually bend connections without affecting data flow. Not collapsible, renders at a minimal 60-unit width.
- **`GroupNode`** — a subgraph container; adds `GroupNodeData` (`InternalCanvas`, `InterfaceInputs`/`InterfaceOutputs` lists of `InterfacePort`, `IsEditing`) so an internal canvas of nodes can present a bridged set of ports to the outer graph.

### Node State and Interaction Components

Beyond `GraphNode`/`GraphConnection`, the plugin registers several supporting components:

- `GraphNodeCollapsed` — added to a node to collapse it to just its header; stores `ExpandedHeight` so it can be restored.
- `PendingConnection`, `HoveredPort` — added to the **canvas** entity while a connection drag or port hover is in progress; read by `GraphRenderSystem` to draw previews/highlights.
- `GraphContextMenu` — canvas-entity state for the node-creation/context menu (`ScreenPosition`, `CanvasPosition`, `MenuType`, `TargetEntity`, `SearchFilter`, `SelectedIndex`).
- `GraphViewAnimation` — canvas-entity state driving a pan/zoom transition; exposes `IsComplete`, `Progress` (eased), `CurrentPan`, and `CurrentZoom`. Added by `GraphContext.FrameSelection`.
- `WidgetFocus` — canvas-entity state for whichever node body widget (from `NodeWidgets`, e.g. `FloatField`) currently has input focus.

Selection and interaction state use tag components rather than fields on `GraphNode`: `GraphNodeSelectedTag`, `GraphConnectionSelectedTag`, `GraphNodeDraggingTag`, `GraphNodeGhostTag` (duplication preview), and `GraphCanvasTag`/`GraphSpacePanningTag`.

### GraphContext API

`GraphContext` (a `[PluginExtension("Graph")]`) is the main manipulation surface, retrieved with `world.GetExtension<GraphContext>()`. Key members observed on the type:

- `Entity CreateCanvas(string? name = null)`
- `Entity CreateNode(Entity canvas, int nodeTypeId, Vector2 position, string? displayName = null)`
- `Entity Connect(Entity sourceNode, int sourcePortIndex, Entity targetNode, int targetPortIndex)` — returns `Entity.Null` if the entities, canvas, ports, or port types are invalid/incompatible.
- `void DeleteNode(Entity node)` — also removes connections referencing the node.
- `void DeleteConnection(Entity connection)`
- `void SelectNode(Entity node, bool addToSelection = false)`, `void DeselectNode(Entity node)`, `void ClearSelection()`, `IEnumerable<Entity> GetSelectedNodes()`, `void SelectAll(Entity canvas)`
- `Rectangle? GetSelectionBounds()`, `void FrameSelection(Entity canvas, float duration = 0.2f)` — animates pan/zoom to fit the current selection via `GraphViewAnimation`.
- `Vector2 ScreenToCanvas(...)` / `Vector2 CanvasToScreen(...)` — coordinate conversion using the canvas's `Pan`/`Zoom`.
- Undoable variants — `CreateNodeUndoable`, `DeleteNodesUndoable`, `MoveNodesUndoable`, `DuplicateSelectionUndoable`, `ConnectUndoable`, `DeleteConnectionUndoable` — route through `KeenEyes.Editor.Abstractions.IUndoRedoManager` when one has been registered as a world extension (`GraphPlugin.Install` wires it up automatically via `TryGetExtension<IUndoRedoManager>`), otherwise they execute immediately.

### Rendering

`GraphRenderSystem` implements `IGraphRenderer` and draws the canvas grid, node bodies/headers/ports, and connections each `Render` phase. Connections currently render as `ConnectionStyle.Bezier` curves (the enum also defines `Straight` and `Stepped` for future use); `IGraphRenderer` exposes `DrawConnection`, `DrawGrid`, `DrawSelectionBox`, `DrawPortHighlight`, and `DrawConnectionPreview` if you need to drive the same primitives from custom code.

Custom node body content is drawn through `INodeTypeDefinition.RenderBody`, which can call into the static helpers on `NodeWidgets` (`FloatField`, and other widgets keyed by `WidgetType`) to render editable fields that participate in the same focus/input handling as built-in widgets.

## Performance

- Port lookups are index-based array access against the registry's `NodeTypeInfo`, not per-frame allocation.
- `PortPositionCache` (a world extension registered by `GraphPlugin`) caches computed screen-space port positions so `GraphInputSystem` and `GraphRenderSystem` don't recompute node layout every frame for hit-testing.
- Selection and drag state use tag components (`GraphNodeSelectedTag`, `GraphNodeDraggingTag`, etc.) so `world.Query<...>()` can filter selected/dragging nodes without scanning a boolean field on every `GraphNode`.

## Next Steps

- [Plugins Guide](plugins.md) - How plugins work
- [Systems Guide](systems.md) - System design patterns
- [UI System Guide](ui.md) - The retained-mode UI system graphs render alongside
- [ADR-010: Graph Node Editor Architecture](adr/010-graph-node-editor.md) - Original design document (note: aspirational in places — e.g. it shows `GraphNode.IsSelected` as a bool field, but the shipped implementation uses `GraphNodeSelectedTag` instead)
