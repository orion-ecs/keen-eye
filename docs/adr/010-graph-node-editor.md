# ADR-010: Graph Node Editor Architecture

**Status:** Accepted
**Revision:** v2
**Implementation:** Shipped
**First accepted:** 2025-12-31 · **Last amended:** 2026-07-26
**Relates to:** [ADR-009](009-kesl-shader-language.md) (KESL)

## Context

KeenEyes needs visual editing capabilities for:
- **KESL Compute Shaders** - Visual composition of GPU compute kernels
- **Future graph-based systems** - Behavior trees, state machines, dialogue, VFX

The existing UI system (ECS-based, retained mode, 40+ widgets) provides a solid foundation but lacks graph-specific primitives: nodes, connections, ports, pan/zoom canvas.

With the KESL shader language prototype complete ([ADR-009](009-kesl-shader-language.md)), we need a visual frontend that:
1. Allows non-programmers to compose compute shaders
2. Provides real-time validation feedback
3. Generates KESL source that compiles via existing pipeline
4. Is extensible for other graph-based domains

## Decision

Implement a **generic graph node editor framework** with KESL-specific node types as the first domain implementation.

### Architecture Overview

```
src/KeenEyes.Graph.Abstractions/  # Generic graph primitives
├── Components (GraphCanvas, GraphNode, GraphConnection)
├── Ports (PortDefinition, PortTypeId, PortDirection)
└── Interfaces (INodeTypeDefinition, IGraphRenderer)

src/KeenEyes.Graph/               # Core editing infrastructure
├── Systems (Input, Layout, Render, ContextMenu, Widgets)
├── GraphContext extension
├── Registries (Port, NodeType)
├── Built-in nodes (Comment, Group, Reroute)
└── Commands (undo/redo command objects)

editor/KeenEyes.Graph.Kesl/       # KESL-specific tooling (editor tier)
├── Nodes (Flow, Logic, Math, Shader, Vector libraries)
├── Compiler (KeslGraphCompiler: Graph → AST)
├── Validation (KeslGraphValidator + rules)
├── Editing (KeslGraphParser, KeslGraphExporter, source mapping)
└── Preview (ShaderPreviewPanel, ShaderExecutor)
```

The KESL package lives in the editor tier (`editor/KeenEyes.Graph.Kesl`) rather than
`src/` — it is editor tooling, not a runtime library — and grew bidirectional `.kesl`
editing (parse/export with source mapping) and a CPU-interpreted component preview
alongside the compiler and validator. The core `KeenEyes.Graph` package additionally
ships built-in Comment/Group/Reroute nodes and an in-node widget system
(`GraphWidgetSystem`, `NodeWidgets`).

### Data Model

**Hybrid approach**: Nodes and connections are entities; ports are structured data in a registry.

```csharp
// Nodes are entities
public struct GraphNode : IComponent
{
    public Vector2 Position;      // Canvas coordinates
    public float Width;
    public float Height;          // Calculated by the layout system
    public int NodeTypeId;
    public Entity Canvas;
    public string? DisplayName;   // Null = use the node type's name
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

Selection state lives in tag components (`GraphNodeSelectedTag`, `GraphConnectionSelectedTag`)
rather than a bool field on the component, keeping selection queryable via archetypes.

### Port Type System

Types support **implicit widening only**:

| Source | Allowed Targets |
|--------|-----------------|
| `float` | `float2`, `float3`, `float4` |
| `float2` | `float3`, `float4` |
| `float3` | `float4` |
| `int` | `float`, `int2`, `int3`, `int4` |
| `int2` | `int3`, `int4` |
| `int3` | `int4` |

No narrowing conversions (lossy). `PortTypeId.Flow` is connectable only to `Flow` —
execution-order ports never mix with data ports. Connection validation:

```csharp
public static bool CanConnect(PortTypeId source, PortTypeId target)
{
    if (source == target) return true;
    if (target == PortTypeId.Any) return true;

    // Flow can only connect to flow
    if (source == PortTypeId.Flow || target == PortTypeId.Flow) return false;

    return (source, target) switch
    {
        (PortTypeId.Float, PortTypeId.Float2 or PortTypeId.Float3 or PortTypeId.Float4) => true,
        (PortTypeId.Float2, PortTypeId.Float3 or PortTypeId.Float4) => true,
        (PortTypeId.Float3, PortTypeId.Float4) => true,
        (PortTypeId.Int, PortTypeId.Float) => true,
        (PortTypeId.Int, PortTypeId.Int2 or PortTypeId.Int3 or PortTypeId.Int4) => true,
        (PortTypeId.Int2, PortTypeId.Int3 or PortTypeId.Int4) => true,
        (PortTypeId.Int3, PortTypeId.Int4) => true,
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
    IReadOnlyList<PortDefinition> InputPorts { get; }
    IReadOnlyList<PortDefinition> OutputPorts { get; }
    bool IsCollapsible { get; }

    void Initialize(Entity node, IWorld world);
    float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea);
}
```

`RenderBody` receives the available body area and returns the height it actually
consumed, letting the layout system size nodes around custom content.

Source generator support (future — not yet implemented):

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
- [x] `GraphCanvas`, `GraphNode`, `GraphConnection` components
- [x] `GraphContext` extension with CreateCanvas, CreateNode, Connect
- [x] Basic rendering (rectangles for nodes, lines for connections)
- [x] Pan/zoom/drag nodes

### Phase 2: Connections
- [x] Bezier curve rendering
- [x] Port type system with validation
- [x] Connection creation via drag-from-port
- [x] Port highlighting on hover

### Phase 3: Interaction Polish
- [x] Multi-select with box selection
- [x] Undo/redo via explicit command objects and the `IUndoRedoManager` extension (not ChangeTracker as originally planned)
- [x] Context menu for node creation
- [x] Keyboard shortcuts (delete, duplicate, select all)

### Phase 4: Node System
- [x] `INodeTypeDefinition` interface
- [x] `NodeTypeRegistry`
- [x] Custom node body rendering
- [ ] Source generator for `[GraphNode]` — not yet implemented; remains future work

### Phase 5: KESL Integration
- [x] KESL-specific node library
- [x] `KeslGraphCompiler` (graph → AST)
- [x] Validation — `KeslGraphValidator` ships with four rules (no-cycles, required-inputs, single-root, type-compatibility); the in-editor error-highlighting UI integration is not yet wired up
- [x] Component preview panel
- [x] Bidirectional: parse .kesl files into graph

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
- Undo/redo uses explicit command objects (create/delete/move/duplicate node and create/delete connection) pushed to the world's `IUndoRedoManager` extension when present; `GraphContext` exposes `*Undoable` variants of each mutating operation
- UI plugin required as dependency

## References

- [ADR-009](009-kesl-shader-language.md) — KESL shader language; graphs compile to its AST
- [Graph Node Editor documentation](../graph.md) — user-facing guide, including the as-built divergences from this ADR

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Implementation marked Shipped — all five phases landed (`src/KeenEyes.Graph`, `src/KeenEyes.Graph.Abstractions`, `editor/KeenEyes.Graph.Kesl`); named gaps: `[GraphNode]` source generator (still future) and in-editor error-highlighting UI for validation. Body amended to as-built reality: selection moved from a `GraphNode.IsSelected` bool to `GraphNodeSelectedTag`/`GraphConnectionSelectedTag` tag components (and `GraphNode` gained `Height`/`DisplayName`); undo/redo uses command objects + `IUndoRedoManager` instead of ChangeTracker; `INodeTypeDefinition` updated to shipped signatures (`IReadOnlyList` ports, height-returning `RenderBody`); port widening table extended with int-vector rows and the Flow-only-to-Flow rule; architecture diagram updated to place KeenEyes.Graph.Kesl in the editor tier with its Editing/Preview subsystems. First-accepted date corrected 2024-12-31 → 2025-12-31 (git).
- **v1 — 2025-12-31 (c8a3690a):** Accepted — generic graph node editor framework (KeenEyes.Graph + Abstractions) with KESL shader nodes as the first domain: hybrid entity/registry data model, implicit-widening port types, IGraphRenderer, and graph-to-KESL-AST compilation.
