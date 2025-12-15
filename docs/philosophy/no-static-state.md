# Why No Static State?

KeenEyes enforces a strict rule: **no static state**. Every `World` is completely isolated, with its own component registry, entity storage, and systems. This page explains why.

## The Problem with Static State

Many ECS frameworks use static registries:

```csharp
// Common pattern in other frameworks (NOT KeenEyes)
public static class ComponentRegistry
{
    private static readonly Dictionary<Type, int> ids = new();
    private static int nextId = 0;

    public static int GetId<T>() => ids.GetOrAdd(typeof(T), _ => nextId++);
}
```

This seems convenient until you encounter these scenarios:

### Problem 1: Test Isolation

```csharp
[Fact]
public void Test1()
{
    // Registers Position as ID 0
    var world1 = new World();
    world1.Components.Register<Position>();
}

[Fact]
public void Test2()
{
    // Expects Position to be ID 0, but it's already registered!
    var world2 = new World();
    // Test may pass or fail depending on execution order
}
```

With static state, test order matters. Running tests in parallel becomes impossible.

### Problem 2: Multiple Worlds

```csharp
var simulationWorld = new World();  // AI simulation
var renderWorld = new World();      // Render state
var physicsWorld = new World();     // Physics preview

// With static state, all share the same component IDs
// Can't have different components registered per world
```

You can't have:
- A lightweight debug world without heavy components
- Multiple simulations with different configurations
- A "replay" world that uses only serializable components

### Problem 3: Library Conflicts

```csharp
// Library A registers Position as ID 0
LibraryA.Initialize();

// Library B also uses Position - conflict!
LibraryB.Initialize();

// Or worse: both libraries use different Position types
// with the same name but different layouts
```

Static state creates implicit dependencies between unrelated code.

### Problem 4: Hidden Initialization Order

```csharp
// What happens if you create an entity before registering components?
var entity = world.Spawn()
    .With(new Position { X = 10, Y = 20 })  // Is Position registered?
    .Build();

// With static state, it depends on what code ran before this
// The answer varies by execution path
```

Debugging becomes archaeology - tracing what ran first.

## KeenEyes' Solution: Per-World State

Each `World` maintains its own:

```csharp
public sealed class World : IDisposable
{
    // Per-world component registry
    public ComponentRegistry Components { get; } = new();

    // Per-world entity storage
    private readonly ArchetypeManager archetypes = new();

    // Per-world systems
    private readonly SystemManager systems = new();

    // Per-world singletons
    private readonly SingletonManager singletons = new();

    // No static fields anywhere
}
```

### Benefit 1: Perfect Test Isolation

```csharp
[Fact]
public void Test1()
{
    using var world = new World();
    // Completely isolated - registers fresh
    world.Components.Register<Position>();
    // ... test logic
}

[Fact]
public void Test2()
{
    using var world = new World();
    // Also completely isolated - no state from Test1
    world.Components.Register<Position>();
    // ... test logic
}
```

Tests can run in parallel without any setup or teardown.

### Benefit 2: Multiple Independent Worlds

```csharp
// Lightweight debug world
var debugWorld = new World();
debugWorld.Components.Register<Position>();
debugWorld.Components.Register<DebugInfo>();

// Full game world
var gameWorld = new World();
gameWorld.Components.Register<Position>();
gameWorld.Components.Register<Health>();
gameWorld.Components.Register<Inventory>();
gameWorld.Components.Register<AIState>();
// ... many more components

// Render preview world (minimal)
var previewWorld = new World();
previewWorld.Components.Register<Transform3D>();
previewWorld.Components.Register<Mesh>();
```

Each world only has what it needs. Component IDs are optimized for each world's usage pattern.

### Benefit 3: No Hidden Dependencies

```csharp
// Everything is explicit and traceable
var world = new World();

// Register components
world.Components.Register<Position>();
world.Components.Register<Velocity>();

// Add systems
world.AddSystem<MovementSystem>();
world.AddSystem<RenderSystem>();

// Create entities
var player = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// Every piece of state is visible and owned by this world
```

No surprises from other code affecting your world.

### Benefit 4: Predictable Behavior

```csharp
// Order doesn't matter for component registration
var world = new World();
world.Components.Register<Health>();
world.Components.Register<Position>();

// Same behavior if you swap the order
var world2 = new World();
world2.Components.Register<Position>();
world2.Components.Register<Health>();

// Both worlds work correctly, just with different internal IDs
```

The API is order-independent because there's no shared state to race for.

## Common Concerns

### "But registration is tedious!"

Source generators handle common patterns:

```csharp
// Attribute marks component
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

// Generator creates registration helpers
// You can batch register in application startup
```

### "What about shared configuration?"

Use explicit configuration passing:

```csharp
var config = new GameConfig { MaxEntities = 10000 };

var world1 = new World(config);
var world2 = new World(config);

// Both configured the same way, but still isolated
```

### "What about component type IDs across worlds?"

Each world has its own IDs. If you need cross-world references:

```csharp
// Use component type, not ID
world1.Has<Position>(entity);  // Type-safe, works across worlds

// Or serialize by type name
var snapshot = Serializer.Save(world1);
Serializer.Load(world2, snapshot);  // Deserializes by type name
```

### "Isn't this slower?"

The overhead of per-world registration is:
- One-time cost at startup
- O(1) lookup during runtime (cached IDs)
- Negligible compared to actual work

The benefits (testability, isolation, predictability) far outweigh this minimal cost.

## When Static State Might Be OK

In practice, static state is acceptable for:

1. **Truly global constants** (not state):
   ```csharp
   public static class MathConstants
   {
       public const float Pi = 3.14159f;  // OK - never changes
   }
   ```

2. **Thread-local caches** (not shared):
   ```csharp
   [ThreadStatic]
   private static StringBuilder? cachedBuilder;  // OK - per-thread
   ```

3. **Process-wide services** (like logging):
   ```csharp
   public static ILogger Log { get; set; }  // OK - configured once at startup
   ```

But for ECS data (component registries, entity storage, system state) - always instance-based.

## The Philosophical Argument

Beyond practical benefits, instance-based design reflects a cleaner mental model:

- **Objects represent things** - A `World` represents a simulation
- **State belongs to objects** - The simulation's state lives in the `World`
- **No spooky action at distance** - Changing one world can't affect another

Static state breaks this model by introducing invisible shared state that multiple objects secretly depend on.

## See Also

- [Core Concepts](../concepts.md) - How World isolation works in practice
- [ADR-001: World Manager Architecture](../adr/001-world-manager-architecture.md) - Internal world structure
- [Why Source Generators?](source-generators.md) - Reducing registration boilerplate
