# ADR-011: Unified Scene Model

**Status:** Accepted
**Date:** 2026-01-01
**Issue:** [#431](https://github.com/orion-ecs/keen-eye/issues/431)

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

Scenes are entity hierarchies. A scene definition produces a root entity with descendants. How you use it determines whether it behaves like a "prefab" or a "level":

```csharp
// Spawn multiple instances (prefab usage)
var player = world.Scenes.Spawn("Player");
var enemy1 = world.Scenes.Spawn("Enemy");
var enemy2 = world.Scenes.Spawn("Enemy");

// Spawn as current level (scene usage)
var level = world.Scenes.Spawn("ForestLevel");

// Unload when transitioning
world.Scenes.Unload(level);
```

### Components

All scene-related components live in `KeenEyes.Abstractions`:

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

**SceneManager API:**

| Method | Description |
|--------|-------------|
| `Spawn(string name)` | Spawn a scene by name, returns root entity |
| `Spawn(string name, Vector3 position)` | Spawn with position override |
| `Unload(Entity sceneRoot)` | Unload scene, respecting persistence and reference counts |
| `MarkPersistent(Entity entity)` | Mark entity to survive scene unloads |
| `TransitionEntity(Entity entity, Entity toScene)` | Move entity to another scene (increments ref count) |
| `GetLoaded()` | Get all currently loaded scene roots |
| `GetScene(string name)` | Get loaded scene by name |

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

One unified generator produces spawn methods:

```csharp
// Generated code
public static partial class Scenes
{
    public static IReadOnlyList<string> All { get; } = ["Player", "Enemy", "ForestLevel"];

    public static Entity SpawnPlayer(World world, Vector3? position = null) { ... }
    public static Entity SpawnEnemy(World world, Vector3? position = null) { ... }
    public static Entity SpawnForestLevel(World world) { ... }
}
```

### Systems Do Not Load/Unload with Scenes

Systems are registered on the World and query for matching entities. When scene entities spawn, systems automatically process them. When entities despawn, systems stop processing them. No explicit system loading/unloading is needed.

## Consequences

### Positive

- **Simpler mental model** - One concept instead of two
- **Matches Godot's proven approach** - Everything is a scene
- **Less code duplication** - One generator, one manager
- **Flexible usage** - Same file can be instanced many times or loaded as a level
- **Clean API** - `world.Scenes.Spawn("Player")` for everything

### Negative

- **Migration** - Existing `.keprefab` files need migration to `.kescene`
- **Naming** - "Scene" for a player entity may feel odd initially

### Neutral

- **Deprecation path** - `PrefabManager` and `PrefabGenerator` will be deprecated in favor of the unified model

## Implementation

1. Add scene components to `KeenEyes.Abstractions`
2. Add `SceneManager` to `KeenEyes.Core` with `world.Scenes` accessor
3. Update `SceneGenerator` to handle all use cases
4. Deprecate `PrefabManager` and `PrefabGenerator`
5. Update editor to use unified model
6. Migrate existing `.keprefab` files to `.kescene`

## References

- [Issue #431: Scene Management Research](https://github.com/orion-ecs/keen-eye/issues/431)
- [Godot Scene System](https://docs.godotengine.org/en/stable/getting_started/step_by_step/scenes_and_nodes.html)
- [ADR-001: World Manager Architecture](./001-world-manager-architecture.md)
