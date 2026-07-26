# ADR-011: Unified Scene Model

**Status:** Accepted
**Revision:** v2
**Implementation:** Partial
**First accepted:** 2026-01-01 · **Last amended:** 2026-07-26
**Relates to:** [ADR-001](001-world-manager-architecture.md) (World managers) · [#431](https://github.com/orion-ecs/keen-eye/issues/431) · [#1079](https://github.com/orion-ecs/keen-eye/issues/1079)

## Context

Issue #431 raised fundamental questions about scene management:

1. Should scenes be separate `World` instances or entity subsets within a single World?
2. How do persistent entities survive scene transitions?
3. How does streaming work?
4. How do scenes integrate with the editor?

Through discussion, we identified that KeenEyes currently has two similar but separate concepts:

| Concept | File Format | Generator | Runtime |
|---------|-------------|-----------|---------|
| Prefab | `.keprefab` | `PrefabGenerator` | `PrefabManager` |
| Scene | `.kescene` | `SceneGenerator` | `SceneSerializer` |

Both represent hierarchies of entities with components. The distinction is artificial:
- A prefab is "a template to spawn multiple times" (player, enemy)
- A scene is "a template to load as a level" (forest, dungeon)

This mirrors Godot's elegant approach where **everything is a scene** (`.tscn`). A player is a scene, a level is a scene. The difference is how you *use* it, not what it *is*.

## Decision

**Unify prefabs and scenes into a single concept: Scenes.**

### Model

Scenes are entity hierarchies. A scene definition produces a root entity with descendants. How you use it determines whether it behaves like a "prefab" or a "level".

As built, the runtime splits this across two cooperating pieces:

- **Generated static spawn methods** — the unified generator turns each `.kescene`/`.keprefab` file into a `Scenes.SpawnX(world, ...)` method that instantiates the file-defined entity hierarchy.
- **`world.Scenes` (`SceneManager`)** — manages lifecycle. `Spawn(string name)` creates a scene root entity (`SceneRootTag` + `SceneMetadata`), `AddToScene` associates spawned entities with that root, and `Unload`/`TransitionEntity`/`MarkPersistent` handle reference counting and persistence.

```csharp
// Instantiate file-defined hierarchies (prefab usage) via generated spawn methods
var player = Scenes.SpawnPlayer(world);
var enemy1 = Scenes.SpawnEnemy(world);
var enemy2 = Scenes.SpawnEnemy(world);

// Create a scene root for lifecycle tracking (level usage)
var level = world.Scenes.Spawn("ForestLevel");
world.Scenes.AddToScene(Scenes.SpawnForestLevel(world), level);

// Unload when transitioning
world.Scenes.Unload(level);
```

### Components

All scene-related components live in `KeenEyes.Abstractions` (namespace `KeenEyes.Scenes`):

```csharp
/// <summary>
/// Marks an entity as the root of a spawned scene.
/// </summary>
[TagComponent]
public partial struct SceneRootTag;

/// <summary>
/// Marks an entity as persistent across scene unloads.
/// </summary>
[TagComponent]
public partial struct PersistentTag;

/// <summary>
/// Metadata for a scene root entity.
/// </summary>
[Component]
public partial struct SceneMetadata
{
    public required string Name;
    public Guid SceneId;
    public SceneState State;
}

/// <summary>
/// Tracks which scene an entity belongs to and reference count.
/// </summary>
[Component]
public partial struct SceneMembership
{
    public Entity OriginScene;
    public int ReferenceCount;
}

public enum SceneState
{
    Loaded,
    Unloading
}
```

### Runtime API

`SceneManager` is an internal manager in `World`, accessed via `world.Scenes`:

```csharp
public partial class World
{
    private SceneManager? sceneManager;

    /// <summary>
    /// Gets the scene manager for spawning and managing scenes.
    /// </summary>
    public SceneManager Scenes => sceneManager ??= new SceneManager(this);
}
```

**SceneManager API (as built):**

| Method | Description |
|--------|-------------|
| `Spawn(string name)` | Create an empty scene root entity (`SceneRootTag` + `SceneMetadata`) for lifecycle tracking |
| `AddToScene(Entity entity, Entity scene)` | Associate an entity with a scene root (increments reference count) |
| `RemoveFromScene(Entity entity, Entity scene)` | Remove an entity from a scene (decrements reference count) |
| `Unload(Entity sceneRoot)` | Unload scene, respecting persistence and reference counts |
| `MarkPersistent(Entity entity)` | Mark entity to survive scene unloads |
| `TransitionEntity(Entity entity, Entity toScene)` | Move entity to another scene (increments ref count) |
| `GetLoaded()` | Get all currently loaded scene roots |
| `GetScene(string name)` | Get loaded scene by name |
| `IsLoaded(string name)` | Check whether a scene with the given name is loaded |
| `LoadedCount` | Number of currently loaded scenes |

Note that `SceneManager` does not instantiate file-defined content — the generated static `Scenes.SpawnX` methods do that. There is also no `Spawn(string name, Vector3 position)` overload; per-instance overrides are typed optional parameters on the generated spawn methods, derived from each file's `overridableFields` list.

### Scene Transitions and Persistence

**Reference counting** handles entities that span scenes:

```csharp
// NPC spawns in village (RefCount = 1)
var village = world.Scenes.Spawn("Village");
var npc = world.Spawn().Build();
world.Scenes.AddToScene(npc, village);

// NPC follows player to forest (RefCount = 2)
var forest = world.Scenes.Spawn("Forest");
world.Scenes.TransitionEntity(npc, forest);

// Unload village - NPC survives (RefCount = 1)
world.Scenes.Unload(village);

// Unload forest - NPC despawns (RefCount = 0)
world.Scenes.Unload(forest);
```

**Persistent entities** are never despawned by scene unloads:

```csharp
var player = world.Spawn().Build();
world.Scenes.MarkPersistent(player);  // Player survives all scene transitions
```

### File Format

The `.kescene` format remains unchanged. The existing JSON schema works for both "prefab" and "scene" usage:

```json
{
  "$schema": "../schemas/kescene.schema.json",
  "name": "Player",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Player",
      "components": {
        "Transform3D": { "position": [0, 0, 0] },
        "Health": { "current": 100, "max": 100 }
      }
    },
    {
      "id": "camera",
      "name": "Camera",
      "parent": "root",
      "components": {
        "Transform3D": { "position": [0, 2, -5] },
        "Camera": { "fov": 60 }
      }
    }
  ]
}
```

### Generator

One unified generator — `SceneGenerator` (`editor/KeenEyes.Generators/SceneGenerator.cs`) — processes both `.kescene` and `.keprefab` AdditionalFiles and produces spawn methods. It emits a `Scenes` class with an `All` list and one spawn method per asset; each method's optional parameters come from that file's `overridableFields` list (there is no fixed position parameter):

```csharp
// Generated code — optional parameters derive from each file's overridableFields
public static partial class Scenes
{
    public static IReadOnlyList<string> All { get; } = ["Player", "Enemy", "ForestLevel"];

    public static Entity SpawnPlayer(World world, /* overridable-field parameters */) { ... }
    public static Entity SpawnEnemy(World world, /* overridable-field parameters */) { ... }
    public static Entity SpawnForestLevel(World world) { ... }
}

// Usage: overrides are typed named arguments
var enemy = Scenes.SpawnEnemy(world, myGamePositionX: 100, myGamePositionY: 50);
```

### Systems Do Not Load/Unload with Scenes

Systems are registered on the World and query for matching entities. When scene entities spawn, systems automatically process them. When entities despawn, systems stop processing them. No explicit system loading/unloading is needed.

## Consequences

### Positive

- **Simpler mental model** - One concept instead of two
- **Matches Godot's proven approach** - Everything is a scene
- **Less code duplication** - One generator, one manager
- **Flexible usage** - Same file can be instanced many times or loaded as a level
- **Clean API** - Generated `Scenes.SpawnX` methods plus `world.Scenes` lifecycle management for everything

### Negative

- **Migration** - Superseded: the anticipated migration of existing `.keprefab` files to `.kescene` never happened and is not planned. Both extensions remain first-class inputs to the single unified `SceneGenerator` (the SDK auto-includes `**/*.keprefab`, samples still use it, and `docs/prefabs.md` documents the workflow as current).
- **Naming** - "Scene" for a player entity may feel odd initially

### Neutral

- **Deprecation path** - Completed and exceeded: `PrefabGenerator` was merged into `SceneGenerator`, and the entire runtime prefab API (`PrefabManager`, `EntityPrefab`, `IPrefabCapability`, `World.Prefabs`) was deprecated and then removed outright in July 2026 ([#1079](https://github.com/orion-ecs/keen-eye/issues/1079)). The unified model is the only prefab/scene mechanism.

## Implementation

- [x] Add scene components to `KeenEyes.Abstractions` (`KeenEyes.Scenes` namespace)
- [x] Add `SceneManager` to `KeenEyes.Core` with `world.Scenes` accessor
- [x] Update `SceneGenerator` to handle all use cases — unified over `.kescene` and `.keprefab`
- [x] Deprecate `PrefabManager` and `PrefabGenerator` — exceeded: `PrefabGenerator` merged into `SceneGenerator`, and the runtime prefab API was subsequently removed entirely ([#1079](https://github.com/orion-ecs/keen-eye/issues/1079))
- [x] Update editor to use unified model (`EditorWorldManager`)
- [ ] ~~Migrate existing `.keprefab` files to `.kescene`~~ — superseded: both extensions remain supported by the unified generator; no migration planned

## References

- [Issue #431: Scene Management Research](https://github.com/orion-ecs/keen-eye/issues/431)
- [Issue #1079: Remove deprecated runtime prefab API](https://github.com/orion-ecs/keen-eye/issues/1079)
- [Godot Scene System](https://docs.godotengine.org/en/stable/getting_started/step_by_step/scenes_and_nodes.html)
- [ADR-001: World Manager Architecture](001-world-manager-architecture.md)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Implementation marked Partial: `SceneManager.Spawn(name)` creates only an empty scene root — file-defined hierarchies spawn via generated static `Scenes.SpawnX` methods with `overridableFields` parameters, and the documented `Spawn(name, Vector3)` overload was never built; the planned `.keprefab`→`.kescene` migration was superseded (both extensions stay first-class). Decision/Consequences/Implementation amended to the as-built split (generated spawn methods vs. `world.Scenes` lifecycle), full SceneManager API table, and the complete removal of the runtime prefab API (#1079).
- **v1 — 2026-01-01 (#431):** Accepted — Unify prefabs and scenes into a single scene concept (one file model, one generator, one runtime lifecycle manager), resolving issue #431's scene-management questions.
