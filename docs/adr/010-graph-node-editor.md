# ADR-010: Graph Node Editor Architecture

**Status:** Accepted
**Date:** 2024-12-31

## Context

KeenEyes needs visual editing capabilities for:
- **KESL Compute Shaders** - Visual composition of GPU compute kernels
- **Future graph-based systems** - Behavior trees, state machines, dialogue, VFX

The existing UI system (ECS-based, retained mode, 40+ widgets) provides a solid foundation but lacks graph-specific primitives: nodes, connections, ports, pan/zoom canvas.

With the KESL shader language prototype complete (ADR-009), we need a visual frontend that:
1. Allows non-programmers to compose compute shaders
2. Provides real-time validation feedback
3. Generates KESL source that compiles via existing pipeline
4. Is extensible for other graph-based domains

## Decision

Implement a **generic graph node editor framework** with KESL-specific node types as the first domain implementation.

### Architecture Overview

```
KeenEyes.Graph.Abstractions/     # Generic graph primitives
├── Components (GraphCanvas, GraphNode, GraphConnection)
├── Ports (PortDefinition, PortTypeId, PortDirection)
└── Interfaces (INodeTypeDefinition, IGraphRenderer)

KeenEyes.Graph/                  # Core editing infrastructure
├── Systems (Input, Layout, Render)
├── GraphContext extension
└── Registries (Port, NodeType)

KeenEyes.Graph.Kesl/             # KESL-specific nodes
├── Node definitions
├── KeslGraphCompiler (Graph → AST)
└── KeslGraphValidator
```

### Data Model

**Hybrid approach**: Nodes and connections are entities; ports are structured data in a registry.

```csharp
// Nodes are entities
public struct GraphNode : IComponent
{
    public Vector2 Position;      // Canvas coordinates
    public float Width;
    public int NodeTypeId;
    public bool IsSelected;
    public Entity Canvas;
}

// Connections are entities
public struct GraphConnection : IComponent
{
    public Entity SourceNode;
    public int SourcePortIndex;
    public Entity TargetNode;
    public int TargetPortIndex;
    public Entity Canvas;
}

// Ports are NOT entities - stored in PortRegistry
public readonly record struct PortDefinition(
    string Name,
    PortDirection Direction,
    PortTypeId TypeId,
    Vector2 LocalOffset,
    bool AllowMultiple = false
);
```

**Rationale for hybrid**:
- Nodes need independent lifecycle, components, queries → entities
- Connections need metadata, can be selected → entities
- Ports don't have independent lifecycle, positions derived from node → registry

### Port Type System

Types support **implicit widening only**:

| Source | Allowed Targets |
|--------|-----------------|
| `float` | `float2`, `float3`, `float4` |
| `float2` | `float3`, `float4` |
| `float3` | `float4` |
| `int` | `float` |

No narrowing conversions (lossy). Connection validation:

```csharp
public static bool CanConnect(PortTypeId source, PortTypeId target)
{
    if (source == target) return true;
    if (target == PortTypeId.Any) return true;

    return (source, target) switch
    {
        (PortTypeId.Float, PortTypeId.Float2 or PortTypeId.Float3 or PortTypeId.Float4) => true,
        (PortTypeId.Float2, PortTypeId.Float3 or PortTypeId.Float4) => true,
        (PortTypeId.Float3, PortTypeId.Float4) => true,
        (PortTypeId.Int, PortTypeId.Float) => true,
        _ => false
    };
}
```

Visual feedback: Connection shows conversion indicator when implicit conversion occurs.

### Canvas Coordinate System

```
Screen Position = (Canvas Position - Pan) * Zoom + CanvasOrigin
Canvas Position = (Screen Position - CanvasOrigin) / Zoom + Pan
```

The `GraphCanvas` component stores pan/zoom state:

```csharp
public struct GraphCanvas : IComponent
{
    public Vector2 Pan;
    public float Zoom;           // 1.0 = 100%
    public float MinZoom;        // e.g., 0.1
    public float MaxZoom;        // e.g., 4.0
    public float GridSize;       // Snap grid
    public bool SnapToGrid;
    public GraphInteractionMode Mode;
}
```

### Connection Rendering

Dedicated `IGraphRenderer` interface (not extending I2DRenderer):

```csharp
public interface IGraphRenderer
{
    void DrawConnection(Vector2 start, Vector2 end, PortTypeId type,
                       ConnectionStyle style, bool isSelected);
    void DrawGrid(Rectangle visibleArea, float gridSize, float zoom);
    void DrawSelectionBox(Rectangle bounds);
    void DrawPortHighlight(Vector2 position, PortTypeId type, bool isValid);
}
```

Bezier curves tessellated to line strips for I2DRenderer compatibility.

### Node Type Extensibility

Node types registered via interface:

```csharp
public interface INodeTypeDefinition
{
    int TypeId { get; }
    string Name { get; }
    string Category { get; }
    PortDefinition[] Inputs { get; }
    PortDefinition[] Outputs { get; }

    void Initialize(Entity node, IWorld world);
    void RenderBody(Entity node, IWorld world, I2DRenderer renderer);
}
```

Source generator support (future):

```csharp
[GraphNode("Add", Category = "Math")]
public partial struct AddNode
{
    [Input] public float A;
    [Input] public float B;
    [Output] public float Result => A + B;
}
```

### KESL Integration

Graph compiles to KESL AST, then through existing pipeline:

```
Visual Graph
    ↓
KeslGraphCompiler.ToAst(graphEntity)
    ↓
ComputeShaderDeclaration (existing AST)
    ↓
┌──────────────────┬──────────────────┐
│ GlslGenerator    │ CSharpBinding    │
│ (existing)       │ Generator        │
└──────────────────┴──────────────────┘
```

### Component Preview

For KESL graphs, show before/after values for sample entities:

```
┌─ Position Preview ──────────┐
│ Before: (10.5, 20.3, 0.0)   │
│ After:  (10.7, 20.1, 0.0)   │
│ Delta:  (+0.2, -0.2, 0.0)   │
└─────────────────────────────┘
```

Run shader on small sample (1-10 entities), display delta.

### Deferred Features

**Subgraphs** (reusable node groups): Deferred to later phase. V1 supports visual grouping only (collapse/expand), not interface ports or saved templates.

**Multi-backend**: Graph editor is backend-agnostic. GLSL only for now; HLSL/SPIR-V backends can be added without graph changes.

**Runtime execution**: Compile-time only for KESL. Graph data model supports future interpreted execution for other domains.

## Implementation Phases

### Phase 1: Foundation
- [ ] `GraphCanvas`, `GraphNode`, `GraphConnection` components
- [ ] `GraphContext` extension with CreateCanvas, CreateNode, Connect
- [ ] Basic rendering (rectangles for nodes, lines for connections)
- [ ] Pan/zoom/drag nodes

### Phase 2: Connections
- [ ] Bezier curve rendering
- [ ] Port type system with validation
- [ ] Connection creation via drag-from-port
- [ ] Port highlighting on hover

### Phase 3: Interaction Polish
- [ ] Multi-select with box selection
- [ ] Undo/redo integration via ChangeTracker
- [ ] Context menu for node creation
- [ ] Keyboard shortcuts (delete, duplicate, select all)

### Phase 4: Node System
- [ ] `INodeTypeDefinition` interface
- [ ] `NodeTypeRegistry`
- [ ] Custom node body rendering
- [ ] Source generator for `[GraphNode]` (future)

### Phase 5: KESL Integration
- [ ] KESL-specific node library
- [ ] `KeslGraphCompiler` (graph → AST)
- [ ] Real-time validation with error highlighting
- [ ] Component preview panel
- [ ] Bidirectional: parse .kesl files into graph

## Alternatives Considered

### Option 1: Extend I2DRenderer with Bezier

Add bezier curves directly to `I2DRenderer`:

```csharp
void DrawBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Vector4 color, float thickness);
```

**Rejected because:**
- Bezier curves are graph-specific, not general 2D
- IGraphRenderer encapsulates visual style (type colors, connection styles)
- Keeps I2DRenderer lean and focused

### Option 2: Ports as Entities

Make every port a child entity of its node:

```
Node Entity
├── Port Entity (Input 1)
├── Port Entity (Input 2)
└── Port Entity (Output 1)
```

**Rejected because:**
- Many entities for complex graphs (500+ for 100 nodes)
- Ports don't need independent lifecycle
- Position is always derived from node
- Registry lookup is simpler and faster

### Option 3: Runtime KESL Interpretation

Execute KESL graphs at runtime without code generation:

**Rejected because:**
- KESL already has compile-time model (source generator)
- Performance would suffer vs compiled shaders
- Adds complexity with minimal benefit
- Other graph types (behavior trees) may use interpretation

## Consequences

### Positive

- **Unified architecture**: Same graph primitives for shaders, behavior trees, etc.
- **KESL integration**: Reuses existing compiler pipeline
- **Extensible**: New node types via `INodeTypeDefinition`
- **ECS consistency**: Follows existing plugin/extension patterns
- **Native AOT**: No reflection, source generators for metadata

### Negative

- **Bezier performance**: Tessellation has CPU cost (mitigated by batching)
- **Learning curve**: New concepts for node type authors
- **Complexity**: Graph editing is inherently complex

### Neutral

- Graph data model is serializable via existing WorldSnapshot
- Integrates with existing undo/redo (ChangeTracker)
- UI plugin required as dependency
