# Troubleshooting

Common issues and their solutions when working with KeenEyes.

## Build Issues

### "Unable to find package" during restore

**Problem:** `dotnet restore` fails with missing package errors.

**Solutions:**

1. Clear the NuGet cache and restore:
   ```bash
   dotnet nuget locals all --clear
   dotnet restore
   ```

2. Verify `nuget.config` includes required feeds:
   ```xml
   <configuration>
     <packageSources>
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     </packageSources>
   </configuration>
   ```

3. Check for proxy issues. If behind a corporate proxy:
   ```bash
   dotnet nuget config set http_proxy http://proxy:port
   ```

### "CS0246: The type or namespace 'IComponent' could not be found"

**Problem:** Missing using directives or package references.

**Solution:** Ensure your project references KeenEyes packages:

```xml
<PackageReference Include="KeenEyes.Core" Version="1.0.0" />
```

And add using directives:
```csharp
using KeenEyes.Core;
using KeenEyes.Abstractions;
```

### Source generator not producing code

**Problem:** `[Component]` attributes aren't generating builder methods.

**Solutions:**

1. Ensure the type is `partial`:
   ```csharp
   [Component]
   public partial struct Position  // Must be partial
   {
       public float X;
       public float Y;
   }
   ```

2. Check generator is referenced:
   ```xml
   <PackageReference Include="KeenEyes.Generators" Version="1.0.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
   ```

3. Rebuild the entire solution (not just the project):
   ```bash
   dotnet build --no-incremental
   ```

4. Check for generator errors in build output. Enable detailed generator diagnostics:
   ```xml
   <PropertyGroup>
     <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   </PropertyGroup>
   ```
   Generated files appear in `obj/Debug/net10.0/generated/`

## Runtime Issues

### "Entity does not exist" exception

**Problem:** Accessing a despawned entity.

**Cause:** Entity was despawned, but you still have a reference.

**Solutions:**

1. Check entity validity before access:
   ```csharp
   if (world.IsAlive(entity))
   {
       ref var pos = ref world.Get<Position>(entity);
   }
   ```

2. Use versioned entity handles - the version number detects stale references:
   ```csharp
   var entity = world.Spawn().Build();  // Version 1
   world.Despawn(entity);
   // Same ID might be reused, but with Version 2
   // world.IsAlive(entity) returns false because version mismatches
   ```

### Query returns no entities when it should

**Problem:** `world.Query<A, B>()` returns empty even with matching entities.

**Causes & Solutions:**

1. **Components not registered:**
   ```csharp
   // Ensure components are registered before creating entities
   world.Components.Register<Position>();
   world.Components.Register<Velocity>();
   ```

2. **Entity doesn't have all required components:**
   ```csharp
   // This entity won't match Query<Position, Velocity>()
   var entity = world.Spawn()
       .With(new Position { X = 0, Y = 0 })
       // Missing Velocity!
       .Build();
   ```

3. **Entity has excluded component:**
   ```csharp
   // This won't match Query<Position>().Without<Disabled>()
   var entity = world.Spawn()
       .With(new Position { X = 0, Y = 0 })
       .WithTag<Disabled>()  // Excluded by Without<>
       .Build();
   ```

### "Collection was modified" during iteration

**Problem:** Modifying entities while iterating throws an exception.

**Cause:** Adding/removing components or spawning/despawning during a foreach loop.

**Solution:** Use a command buffer:

```csharp
// BAD: Modifies during iteration
foreach (var entity in world.Query<Health>())
{
    if (world.Get<Health>(entity).Current <= 0)
        world.Despawn(entity);  // Throws!
}

// GOOD: Deferred modification
var buffer = world.GetCommandBuffer();
foreach (var entity in world.Query<Health>())
{
    if (world.Get<Health>(entity).Current <= 0)
        buffer.Despawn(entity);  // Queued
}
buffer.Execute();  // Applied after iteration
```

### Component data appears corrupt or stale

**Problem:** Reading component returns wrong values.

**Causes & Solutions:**

1. **Not using ref for modification:**
   ```csharp
   // BAD: Copies the struct, changes are lost
   var pos = world.Get<Position>(entity);
   pos.X = 100;  // Modifies the copy!

   // GOOD: Reference to actual data
   ref var pos = ref world.Get<Position>(entity);
   pos.X = 100;  // Modifies actual component
   ```

2. **Using stale entity reference:**
   ```csharp
   var entity = someOldReference;
   // Entity may have been despawned and ID reused
   if (!world.IsAlive(entity))
       return;  // Don't access stale entity
   ```

### System not executing

**Problem:** System's `Update()` method never called.

**Causes & Solutions:**

1. **System not added to world:**
   ```csharp
   world.AddSystem<MovementSystem>();  // Required!
   world.Update(deltaTime);
   ```

2. **System disabled:**
   ```csharp
   var system = world.GetSystem<MovementSystem>();
   system.Enabled = true;  // Ensure enabled
   ```

3. **Wrong phase:**
   ```csharp
   // System only runs in Update phase
   public override SystemPhase Phase => SystemPhase.Update;

   // But you're calling:
   world.FixedUpdate(fixedDeltaTime);  // Wrong phase!
   ```

4. **System order issue - check ordering:**
   ```csharp
   public override int Order => 100;  // Higher = runs later
   ```

## Performance Issues

### Slow query iteration

**Problem:** Queries are taking too long.

**Solutions:**

1. **Avoid Query allocation in hot path:**
   ```csharp
   // BAD: Creates query object each frame
   foreach (var e in world.Query<Position, Velocity>())

   // GOOD: Cache the query
   private QueryDescription<Position, Velocity> movingQuery;

   public override void Initialize()
   {
       movingQuery = world.Query<Position, Velocity>();
   }

   public override void Update(float dt)
   {
       foreach (var e in movingQuery)  // Reuses query
   ```

2. **Use `ref readonly` for read-only access:**
   ```csharp
   // Tells compiler component won't be modified
   ref readonly var vel = ref world.Get<Velocity>(entity);
   ```

3. **Avoid large components:**
   ```csharp
   // BAD: Large component = cache misses
   public struct BigComponent : IComponent
   {
       public byte[] Data;  // 1KB array
   }

   // GOOD: Keep components small
   public struct DataRef : IComponent
   {
       public int DataIndex;  // Reference to external storage
   }
   ```

### High memory usage

**Problem:** World uses excessive memory.

**Solutions:**

1. **Despawn unused entities:**
   ```csharp
   // Don't just disable - actually remove
   world.Despawn(unusedEntity);
   ```

2. **Use entity pooling for frequently spawned/despawned entities:**
   See [Entity Pooling](cookbook/entity-pooling.md)

3. **Avoid component bloat:**
   ```csharp
   // BAD: Entity has many unused components
   var entity = world.Spawn()
       .With(new Position())
       .With(new Velocity())
       .With(new Health())       // Not all entities need all these
       .With(new Inventory())
       .With(new AIState())
       .Build();

   // GOOD: Only add what's needed
   var bullet = world.Spawn()
       .With(new Position())
       .With(new Velocity())
       .Build();
   ```

### Frame time spikes

**Problem:** Occasional long frames.

**Solutions:**

1. **Spread work across frames:**
   ```csharp
   // BAD: Process all at once
   foreach (var entity in world.Query<AIState>())
       UpdateAI(entity);  // 1000 entities = 1000 updates

   // GOOD: Process in batches
   private int batchIndex = 0;
   private const int BatchSize = 100;

   public override void Update(float dt)
   {
       var entities = world.Query<AIState>().ToArray();
       int start = batchIndex * BatchSize;
       int end = Math.Min(start + BatchSize, entities.Length);

       for (int i = start; i < end; i++)
           UpdateAI(entities[i]);

       batchIndex = (batchIndex + 1) % ((entities.Length / BatchSize) + 1);
   }
   ```

2. **Use parallel execution for independent systems:**
   See [Parallelism Guide](parallelism.md)

## Serialization Issues

### "Unknown component type" during deserialization

**Problem:** Loading a save file fails with unknown type error.

**Cause:** Component type not registered in target world.

**Solution:** Register all component types before loading:

```csharp
// Ensure same components are registered
world.Components.Register<Position>();
world.Components.Register<Velocity>();
world.Components.Register<Health>();

// Then load
Serializer.Load(world, saveData);
```

Or use a serializer with type metadata:
```csharp
var serializer = new JsonComponentSerializer();
// Serializer stores type names, not IDs
```

### Entity references invalid after load

**Problem:** `Entity` fields in components point to wrong entities after loading.

**Cause:** Entity IDs are reassigned during load.

**Solution:** Use entity names or stable identifiers:

```csharp
// BAD: Raw entity reference
[Component]
public struct FollowTarget
{
    public Entity Target;  // ID changes on load
}

// GOOD: Named reference
[Component]
public struct FollowTarget
{
    public string TargetName;  // Stable across save/load
}

// Resolve after load
var target = world.GetEntityByName(component.TargetName);
```

## Native AOT Issues

### "RuntimeTypeHandle" exception with AOT

**Problem:** Code fails when published with Native AOT.

**Cause:** Reflection used at runtime.

**Solutions:**

1. Use source-generated code instead of reflection
2. Check for these patterns:
   ```csharp
   // BAD: Runtime generic instantiation
   method.MakeGenericMethod(runtimeType);

   // BAD: Dynamic activation
   Activator.CreateInstance(type);

   // BAD: Assembly scanning
   assembly.GetTypes().Where(...)
   ```

3. Enable AOT analysis during development:
   ```xml
   <PropertyGroup>
     <IsAotCompatible>true</IsAotCompatible>
   </PropertyGroup>
   ```

See [Why Native AOT?](philosophy/native-aot.md) for details.

## Common Mistakes

### Using `==` for float comparison

**Problem:** Float comparisons fail due to precision.

```csharp
// BAD: May fail due to floating-point precision
if (velocity.X == 0)

// GOOD: Use tolerance-based comparison
using KeenEyes.Common;
if (velocity.X.IsApproximatelyZero())
```

### Forgetting to call `Build()`

**Problem:** Entity spawning doesn't work.

```csharp
// BAD: Missing Build() - entity not created
world.Spawn()
    .With(new Position { X = 0, Y = 0 });

// GOOD: Build() creates the entity
var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();
```

### Not disposing World

**Problem:** Memory leaks.

```csharp
// BAD: Never disposed
var world = new World();
// ... use world
// world stays in memory

// GOOD: Using statement ensures disposal
using var world = new World();
// ... use world
// Automatically disposed at end of scope
```

## Getting More Help

If your issue isn't covered here:

1. Check the [API Documentation](../api/index.md) for detailed method documentation
2. Review [Architecture Decisions](adr/001-world-manager-architecture.md) for design rationale
3. Search [GitHub Issues](https://github.com/orion-ecs/keen-eye/issues)
4. Open a new issue with:
   - KeenEyes version
   - .NET version
   - Minimal reproduction code
   - Expected vs actual behavior
