# Engine Systems Research Roadmap

> **Note**: This document was originally created for planning. Many systems have since been implemented.
> See `ROADMAP.md` for current status.

This document outlines game engine systems that require further research and architectural planning before implementation. Each section describes the system's purpose, why it matters, potential approaches, and open questions to resolve.

---

## Table of Contents

1. [Pathfinding & Navigation](#pathfinding--navigation)
2. [Scene Management](#scene-management)
3. [Localization](#localization)
4. [Networking](#networking)
5. [Priority Matrix](#priority-matrix)

---

## Pathfinding & Navigation

### Why It Matters

The AI system (planned in #426-427) handles *decision making* - choosing what to do. But AI agents also need to *move through the world*, which requires:

- Finding paths around obstacles
- Navigating complex 3D geometry
- Handling dynamic obstacles (doors, moving platforms)
- Supporting different movement capabilities (flying, swimming, climbing)

Without pathfinding, AI is limited to:
- Direct movement toward targets (gets stuck on walls)
- Pre-scripted patrol routes
- Simple steering behaviors

### Current State

- **KeenEyes.Spatial** provides broadphase queries (nearby entities)
- **KeenEyes.Physics** provides raycasting and collision
- **No navigation mesh or pathfinding algorithm exists**

### Potential Approaches

#### Option A: Grid-Based A*
**Pros:** Simple, works well for 2D and tile-based games
**Cons:** Memory-intensive for large worlds, not ideal for 3D

```
KeenEyes.Navigation/
├── Grid/
│   ├── NavigationGrid.cs      # 2D/3D grid representation
│   ├── AStarPathfinder.cs     # A* implementation
│   └── GridObstacle.cs        # Component to mark blocked cells
```

#### Option B: Navigation Mesh (NavMesh)
**Pros:** Industry standard, efficient for 3D, supports agent sizes
**Cons:** Complex to generate, requires baking step

```
KeenEyes.Navigation/
├── NavMesh/
│   ├── NavigationMesh.cs      # Polygon mesh for walkable areas
│   ├── NavMeshAgent.cs        # Component for pathfinding entities
│   ├── NavMeshObstacle.cs     # Dynamic obstacles
│   └── NavMeshBuilder.cs      # Mesh generation from geometry
├── Pathfinding/
│   ├── AStarOnNavMesh.cs      # A* over navmesh polygons
│   └── FunnelAlgorithm.cs     # Path smoothing
```

#### Option C: Hierarchical Pathfinding (HPA*)
**Pros:** Scales to very large worlds, fast long-distance paths
**Cons:** More complex, requires preprocessing

#### Option D: Flow Fields
**Pros:** Excellent for many units going to same destination (RTS)
**Cons:** Not ideal for individual agent paths

### Integration with AI

```csharp
// AI decides to move to target
public class ChaseAction : IAIAction
{
    public BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        var target = bb.Get<Entity>("Target");
        var targetPos = world.Get<Transform3D>(target).Position;

        // Request path from navigation system
        var nav = world.GetExtension<INavigationContext>();
        var path = nav.FindPath(entity, targetPos);

        if (path == null) return BTNodeState.Failure;

        // Store path for movement system to follow
        bb.Set("CurrentPath", path);
        return BTNodeState.Success;
    }
}
```

### Open Questions

1. **2D vs 3D focus?** Should we support both, or prioritize one?
2. **NavMesh generation** - Runtime or editor-time baking?
3. **Dynamic obstacles** - How to handle doors, destructibles?
4. **Agent variety** - Different sizes, flying units, vehicles?
5. **Integration pattern** - Standalone plugin or part of AI?
6. **Recast/Detour** - Use established library or build custom?

### Recommendation

Start with **NavMesh approach** as it's the industry standard and works for most 3D games. Consider wrapping [Recast Navigation](https://github.com/recastnavigation/recastnavigation) for the heavy lifting, with KeenEyes-native components and systems on top.

**See:** [Pathfinding & Navigation Research](pathfinding-navigation.md) for detailed architecture and implementation plan.

---

## Scene Management

### Why It Matters

Currently, a KeenEyes `World` contains all entities. This works for small games, but larger games need:

- **Level transitions** - Moving between distinct areas
- **Additive loading** - Loading additional content without destroying current
- **Streaming** - Loading/unloading chunks as player moves
- **Prefab instantiation** - Spawning pre-designed entity groups

### Current State

- **KeenEyes.Persistence** handles save/load of world state
- **Prefabs sample** demonstrates entity templates
- **No scene abstraction or loading system exists**

### What is a "Scene"?

This is the key architectural question. Options:

#### Option A: Scene = World
Each scene is a separate World instance. Transitioning means disposing old World, creating new.

**Pros:** Clean isolation, simple mental model
**Cons:** Can't share entities between scenes, expensive transitions

#### Option B: Scene = Entity Subset
Scenes are collections of entities within a single World. Loading adds entities, unloading removes them.

**Pros:** Entities can persist across scenes (player), additive loading natural
**Cons:** More complex tracking, potential for leaks

#### Option C: Scene = Serialized Snapshot
Scenes are data files (like prefabs but larger). Loading deserializes into current World.

**Pros:** Works with existing persistence, clear data format
**Cons:** Similar to Option B complexity

### Potential Structure

```
KeenEyes.Scenes/
├── Core/
│   ├── Scene.cs               # Scene definition (entity list or snapshot)
│   ├── SceneHandle.cs         # Reference to loaded scene
│   └── SceneState.cs          # Loading, Loaded, Unloading
├── Loading/
│   ├── ISceneLoader.cs        # Load scene data
│   ├── SceneLoadRequest.cs    # Async load request
│   └── SceneManager.cs        # Orchestrates loading/unloading
├── Streaming/
│   ├── StreamingVolume.cs     # Component triggering load/unload
│   ├── StreamingSystem.cs     # Monitor player position
│   └── ChunkManager.cs        # Grid-based streaming
└── Transitions/
    ├── SceneTransition.cs     # Fade, crossfade, etc.
    └── LoadingScreen.cs       # UI during loads
```

### Integration with Assets

Scene loading should use the Asset Management system:

```csharp
var assets = world.GetExtension<AssetManager>();
var sceneHandle = await assets.LoadAsync<SceneAsset>("levels/forest.scene");

var scenes = world.GetExtension<SceneManager>();
await scenes.LoadSceneAsync(sceneHandle, LoadMode.Additive);
```

### Open Questions

1. **Scene = World or Scene = Entity subset?** Fundamental architecture decision
2. **Persistent entities** - How does the player survive scene transitions?
3. **Scene dependencies** - Scene A requires assets X, Y, Z
4. **Streaming granularity** - Chunks? Rooms? Distance-based?
5. **Editor integration** - How are scenes authored?
6. **Multi-scene editing** - Edit multiple scenes simultaneously?

### Recommendation

Start with **Scene = Serialized Snapshot** approach, building on existing Persistence system. This provides:
- Clear data format (can inspect/edit scene files)
- Additive loading by deserializing into current World
- Entity tagging to track "which scene owns this entity"
- Unloading by despawning tagged entities

---

## Localization

### Why It Matters

Any game targeting multiple regions needs:

- **Translated text** - UI, dialogue, item names
- **Formatted values** - Numbers, dates, currencies
- **Right-to-left support** - Arabic, Hebrew
- **Asset variants** - Different images/audio per language

### Current State

- **No localization system exists**
- UI system (planned) will need localized text

### Potential Approaches

#### Option A: Simple Key-Value
```csharp
// strings/en.json
{ "menu.start": "Start Game", "menu.quit": "Quit" }

// strings/es.json
{ "menu.start": "Iniciar Juego", "menu.quit": "Salir" }

// Usage
var loc = world.GetExtension<ILocalization>();
string text = loc.Get("menu.start");  // Returns based on current language
```

**Pros:** Simple, sufficient for most games
**Cons:** No pluralization, no formatting

#### Option B: ICU Message Format
```csharp
// Supports pluralization, gender, etc.
{ "items.count": "{count, plural, =0 {No items} =1 {One item} other {# items}}" }

string text = loc.Format("items.count", new { count = 5 });  // "5 items"
```

**Pros:** Industry standard, handles complex cases
**Cons:** More complex syntax, parser needed

#### Option C: Fluent (Mozilla)
```ftl
# messages.ftl
hello = Hello, { $name }!
items = { $count ->
    [0] No items
    [one] One item
   *[other] { $count } items
}
```

**Pros:** Very expressive, good tooling
**Cons:** Custom syntax, another dependency

### Potential Structure

```
KeenEyes.Localization/
├── Core/
│   ├── ILocalization.cs       # Main API
│   ├── Locale.cs              # Language + region (en-US, es-MX)
│   ├── LocalizationConfig.cs  # Default locale, fallback chain
│   └── LocalizedString.cs     # Component-friendly reference
├── Sources/
│   ├── IStringSource.cs       # Where strings come from
│   ├── JsonStringSource.cs    # Load from JSON files
│   └── CsvStringSource.cs     # Load from CSV (translator-friendly)
├── Formatting/
│   ├── IMessageFormatter.cs   # Format with parameters
│   └── IcuFormatter.cs        # ICU message format support
└── Components/
    ├── LocalizedText.cs       # Component for UI text
    └── LocalizedAsset.cs      # Component for localized assets
```

### Integration with UI

```csharp
// UI text component uses localization key
[Component]
public partial struct UIText
{
    public string Content;           // Direct text OR
    public string LocalizationKey;   // Key to look up
    // ... other fields
}

// UIRenderSystem checks for key
if (!string.IsNullOrEmpty(text.LocalizationKey))
{
    var loc = World.GetExtension<ILocalization>();
    displayText = loc.Get(text.LocalizationKey);
}
```

### Open Questions

1. **Format complexity** - Simple key-value vs ICU vs Fluent?
2. **Asset localization** - Different textures/audio per language?
3. **Hot-reload** - Change language without restart?
4. **Fallback chain** - en-US → en → default?
5. **Tooling** - How do translators work with the files?
6. **Font support** - CJK characters, Arabic shaping?

### Recommendation

Start with **simple key-value JSON** with parameter substitution. This covers 90% of use cases. Add ICU formatting later if needed. The key architectural decision is the `LocalizedString` pattern - using keys in components rather than raw text.

---

## Networking

### Why It Matters

Multiplayer games need:

- **State synchronization** - Keep all clients' worlds consistent
- **Input handling** - Send player actions to server
- **Latency compensation** - Hide network delay from players
- **Authority** - Who decides what's "true"?

### Current State

- **Networking plugin is being discussed separately**
- This section captures ECS-specific considerations

### ECS Networking Challenges

#### Challenge 1: Entity Identity
Entities are local IDs `(int Id, int Version)`. How do we reference entities across network?

```csharp
// Option A: Network ID component
[Component]
public partial struct NetworkIdentity
{
    public uint NetworkId;      // Globally unique across all clients
    public bool IsOwner;        // Does this client control this entity?
}

// Option B: Deterministic spawning
// Same spawn order = same entity IDs (requires lockstep)
```

#### Challenge 2: Component Replication
Which components sync across network? How often?

```csharp
// Mark components for replication
[Component]
[Replicated(Frequency = ReplicationFrequency.EveryFrame)]
public partial struct Transform3D { ... }

[Component]
[Replicated(Frequency = ReplicationFrequency.OnChange)]
public partial struct Health { ... }

[Component]  // Not replicated - client-only visual
public partial struct ParticleEmitter { ... }
```

#### Challenge 3: Authority Model

| Model | Description | Use Case |
|-------|-------------|----------|
| **Server Authoritative** | Server owns all state, clients are dumb | Competitive, anti-cheat |
| **Client Authoritative** | Clients own their entities | Cooperative, trust players |
| **Distributed** | Different entities owned by different peers | P2P games |

#### Challenge 4: Prediction & Rollback
Client predicts locally, server corrects. How to rollback ECS state?

```csharp
// Snapshot system for rollback
public class NetworkRollbackSystem : SystemBase
{
    private readonly RingBuffer<WorldSnapshot> history;

    public void Rollback(int serverTick)
    {
        var snapshot = history.Get(serverTick);
        // Restore world state to snapshot
        // Re-simulate from serverTick to now
    }
}
```

### Potential Structure

```
KeenEyes.Networking/
├── Core/
│   ├── INetworkTransport.cs   # UDP, WebSocket, etc.
│   ├── NetworkManager.cs      # Connection management
│   └── NetworkConfig.cs       # Tick rate, interpolation settings
├── Identity/
│   ├── NetworkIdentity.cs     # Component for networked entities
│   ├── NetworkSpawner.cs      # Spawn entities across network
│   └── OwnershipManager.cs    # Track who owns what
├── Replication/
│   ├── ReplicatedAttribute.cs # Mark components for sync
│   ├── DeltaCompressor.cs     # Only send changes
│   └── ReplicationSystem.cs   # Serialize and send state
├── Prediction/
│   ├── InputBuffer.cs         # Queue inputs for server
│   ├── PredictionSystem.cs    # Client-side prediction
│   └── ReconciliationSystem.cs # Handle server corrections
└── Transport/
    ├── UdpTransport.cs        # Raw UDP
    ├── LiteNetLibTransport.cs # Reliable UDP library
    └── WebSocketTransport.cs  # Browser support
```

### Open Questions

1. **Authority model** - Server authoritative default?
2. **Transport** - UDP, WebSocket, or pluggable?
3. **Tick rate** - Fixed tick for determinism?
4. **Interpolation** - How to smooth entity movement?
5. **Interest management** - Only replicate nearby entities?
6. **Snapshot vs delta** - Full state or changes only?
7. **P2P support** - Or server-only architecture?

### Recommendation

Networking is complex enough to warrant its own deep-dive planning session. Key decisions:
1. Pick an authority model (recommend server-authoritative)
2. Define replication attributes via source generator
3. Use existing Persistence serialization for snapshots
4. Consider wrapping a proven library (LiteNetLib, ENet)

---

## Priority Matrix

| System | Complexity | Impact | Status |
|--------|------------|--------|--------|
| **Pathfinding** | Medium | High (completes AI) | ✅ **COMPLETE** - DotRecast NavMesh + Grid A* |
| **Scene Management** | Medium | Medium (large games only) | ✅ **COMPLETE** - SceneManager in Core |
| **Localization** | Low | Medium (release requirement) | ✅ **COMPLETE** - JSON + ICU + RTL |
| **Networking** | High | High (if multiplayer) | ✅ **COMPLETE** - KeenEyes.Network |

### Implementation Summary

All four systems have been implemented:

1. **Pathfinding** - `KeenEyes.Navigation` with DotRecast NavMesh and grid-based A*
2. **Scene Management** - `SceneManager` in Core, unified scene model
3. **Localization** - `LocalizationPlugin` with JSON sources, ICU MessageFormat, RTL support
4. **Networking** - `KeenEyes.Network` with replication, prediction, reconciliation

---

## Summary

> **Update (2026)**: All four systems researched in this document have been implemented.

These four systems represented the remaining "big picture" items for KeenEyes to become a complete game engine. All have now been addressed:

- **Pathfinding** - Implemented with DotRecast NavMesh for 3D and grid-based A* for 2D
- **Scene Management** - Implemented with SceneManager, scene serialization, and editor integration
- **Localization** - Implemented with JSON/CSV sources, ICU MessageFormat, and RTL layout support
- **Networking** - Implemented with entity replication, client prediction, and server reconciliation

The existing foundation (ECS, Physics, Persistence, Parallelism) combined with these new systems provides a comprehensive base for game development. See `ROADMAP.md` for remaining work on UI, Audio, Particles, Animation, and Asset Management.
