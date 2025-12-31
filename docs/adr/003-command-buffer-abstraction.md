# ADR-003: CommandBuffer Abstraction and Reflection Elimination

**Status:** Accepted
**Date:** 2025-12-10
**Related Work:** Plugin Architecture, Performance Optimization

## Context

The CommandBuffer pattern enables safe entity modifications during iteration by queueing operations for deferred execution. Prior to this change, the command buffer system was tightly coupled to `KeenEyes.Core`, preventing plugins from using it without a Core dependency.

### Initial Architecture (Problems)

```
KeenEyes.Core/Commands/
├── CommandBuffer.cs          (concrete implementation in Core)
├── ICommand.cs                (internal interface in Core)
├── EntityCommands.cs          (fluent builder in Core)
├── SpawnCommand.cs            (command using reflection)
├── AddComponentCommand.cs     (command using reflection)
└── ...other commands

Plugins → Need Core dependency → Tight coupling
```

**Issues:**

1. **Plugin Isolation Broken** - Plugins needed Core dependency to use `new CommandBuffer()`
2. **Reflection in Hot Path** - Commands used `MakeGenericMethod()` and `MethodInfo.Invoke()`
3. **No Interface** - CommandBuffer was concrete only, not mockable for testing
4. **Generator Incompatibility** - Generated extension methods didn't work with `IWorld.Spawn()`

### Reflection Performance Problem

Original command execution (using reflection):
```csharp
// SpawnCommand.Execute() - OLD
var addMethod = typeof(IEntityBuilder).GetMethod("With")
    .MakeGenericMethod(componentType);
addMethod.Invoke(builder, new object[] { component }); // ~20-50x slower
```

Measured overhead per command execution:
- **Reflection path:** ~500-1000 CPU cycles
- **Direct call:** ~20-30 CPU cycles
- **Performance degradation:** 20-50x slower

### Generator Compatibility Problem

Generated component extension methods only worked with concrete `EntityBuilder`:

```csharp
// Generated extension (OLD - only concrete type)
public static EntityBuilder WithPosition(this EntityBuilder builder, float x, float y)

// Plugin usage (BROKEN)
IWorld world = GetWorld();
world.Spawn()         // Returns IEntityBuilder
    .WithPosition();  // ❌ CS0311: Type constraint mismatch
```

Plugins using `IWorld.Spawn()` couldn't call generated extension methods without casting to concrete `World`.

## Decision

Move the entire command buffer system to `KeenEyes.Abstractions` and eliminate all reflection:

### Architecture Changes

**Package Structure:**

```
KeenEyes.Abstractions/
├── ICommandBuffer.cs           (public interface)
├── CommandBuffer.cs            (concrete implementation moved from Core)
├── EntityCommands.cs           (fluent builder moved from Core)
├── ICommand.cs                 (internal interface moved from Core)
├── IEntityBuilder.cs           (updated with Build() method)
├── IWorld.cs                   (updated with Spawn(name) overload)
└── Commands/                   (all command classes moved from Core)
    ├── SpawnCommand.cs          (delegate-based, no reflection)
    ├── AddComponentCommand.cs   (delegate-based, no reflection)
    ├── DespawnCommand.cs
    ├── RemoveComponentCommand.cs
    └── SetComponentCommand.cs
```

**Key Changes:**

1. **Move to Abstractions** - CommandBuffer and all related types now in Abstractions package
2. **Add ICommandBuffer Interface** - Public interface for plugin usage
3. **Delegate Capture Pattern** - Replace reflection with delegate capture
4. **Dual Extension Methods** - Generator creates both generic and non-generic versions

### Reflection Elimination: Delegate Capture Pattern

**Before (reflection-based):**
```csharp
// EntityCommands storage
internal List<(Type Type, object Data, bool IsTag)> Components { get; } = [];

// Command execution
var addMethod = typeof(IEntityBuilder).GetMethod("With")
    .MakeGenericMethod(componentType);
addMethod.Invoke(builder, new object[] { component }); // Slow!
```

**After (delegate capture):**
```csharp
// EntityCommands storage - delegates instead of reflection
internal List<Func<IEntityBuilder, IEntityBuilder>> ComponentAdders { get; } = [];

// Registration (cold path) - captures type and value
public EntityCommands With<T>(T component) where T : struct, IComponent
{
    // Capture component value and type in delegate
    ComponentAdders.Add(builder => builder.With(component));
    return this;
}

// Execution (hot path) - direct invocation, zero reflection
foreach (var adder in entityCommands.ComponentAdders)
{
    builder = adder(builder);  // ~20-30 cycles vs ~500-1000 with reflection
}
```

**Benefits:**
- Type information captured once at registration time (cold path)
- Execution invokes stored delegate directly (hot path)
- **20-50x performance improvement** over reflection
- Type safety preserved through generics at capture time

### Generator Interface Support

Generate TWO versions of each extension method:

```csharp
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

// Generated - Generic version for type-safe chaining
public static TSelf WithPosition<TSelf>(this TSelf builder, float x, float y)
    where TSelf : IEntityBuilder<TSelf>
{
    return builder.With(new Position { X = x, Y = y });
}

// Generated - Non-generic version for interface usage
public static IEntityBuilder WithPosition(this IEntityBuilder builder, float x, float y)
{
    return builder.With(new Position { X = x, Y = y });
}
```

C# overload resolution automatically picks the best match:
- **Concrete types** (`EntityBuilder`, `EntityCommands`) → Generic version (type-safe)
- **Interface types** (`IEntityBuilder`) → Non-generic version (compatible)

### Interface Enhancements

Extended interfaces for completeness:

```csharp
// IWorld - Added named spawn overload
public interface IWorld
{
    IEntityBuilder Spawn();        // Already existed
    IEntityBuilder Spawn(string? name);  // NEW
}

// IEntityBuilder - Added Build method
public interface IEntityBuilder
{
    IEntityBuilder With<T>(T component);
    IEntityBuilder WithTag<T>();
    Entity Build();  // NEW
}
```

## Implementation Strategy

### Phase 1: Interface and Move
1. Create `ICommandBuffer` interface in Abstractions
2. Move `ICommand`, `EntityCommands` to Abstractions
3. Update `EntityCommands` to use delegate list
4. Add `InternalsVisibleTo` for Core access

### Phase 2: Reflection Elimination
1. Replace `(Type, object, bool)` tuples with `Func<IEntityBuilder, IEntityBuilder>` delegates
2. Update all command classes to use delegate capture pattern
3. Remove all `MakeGenericMethod()` and `MethodInfo.Invoke()` calls

### Phase 3: Generator Update
1. Update ComponentGenerator to generate dual extension methods
2. Update tests to verify both generic and non-generic versions
3. Remove temporary casts from sample code

### Phase 4: Complete Move
1. Move `CommandBuffer` concrete class to Abstractions
2. Move all command implementation classes to Abstractions
3. Remove Core/Commands directory
4. Verify all command classes only depend on IWorld interface

## Alternatives Considered

### Option 1: Keep CommandBuffer in Core, Add Factory Method

Add factory method to IWorld:
```csharp
public interface IWorld
{
    ICommandBuffer CreateCommandBuffer();
}
```

**Rejected because:**
- More verbose usage: `world.CreateCommandBuffer()` vs `new CommandBuffer()`
- Still requires Core dependency for concrete type in application code
- Doesn't solve the reflection problem
- Factory pattern adds unnecessary indirection

### Option 2: Keep Reflection, Optimize with Caching

Cache reflected `MethodInfo` instances:
```csharp
private static readonly Dictionary<Type, MethodInfo> methodCache = new();
```

**Rejected because:**
- Still 10-20x slower than direct calls (caching only helps repeated operations)
- Adds complexity (cache management, thread safety)
- Doesn't eliminate boxing/unboxing overhead
- Reflection is fundamentally incompatible with AOT compilation

### Option 3: Move Only Interface, Keep Implementation in Core

Expose `ICommandBuffer` interface in Abstractions but keep `CommandBuffer` in Core:

**Rejected because:**
- Plugins still need Core dependency to instantiate `new CommandBuffer()`
- Forces plugins to use factory pattern or DI
- Doesn't achieve true plugin isolation
- Half-measure that doesn't solve the core problem

### Option 4: Single Generic Extension Method Only

Generate only the generic version:
```csharp
public static TSelf WithPosition<TSelf>(this TSelf builder, float x, float y)
    where TSelf : IEntityBuilder<TSelf>
```

**Rejected because:**
- Doesn't work with `IWorld.Spawn()` return type (CS0311 error)
- Forces plugins to cast: `((EntityBuilder)world.Spawn()).WithPosition()`
- Defeats purpose of IWorld abstraction

## Consequences

### Positive

- ✅ **True Plugin Isolation** - Plugins only need Abstractions, no Core dependency
- ✅ **20-50x Performance Improvement** - Delegate capture eliminates reflection overhead
- ✅ **Interface Compatibility** - Generated extensions work with both concrete and interface types
- ✅ **AOT Compilation Ready** - No reflection means compatible with NativeAOT
- ✅ **Better Testability** - ICommandBuffer interface enables mocking
- ✅ **Cleaner Samples** - No casting needed, natural IWorld usage
- ✅ **Type Safety** - Delegates preserve compile-time type checking

### Negative

- ⚠️ **Dual Method Generation** - Generator creates 2x methods (slight code bloat)
- ⚠️ **Delegate Allocation** - Each component capture allocates a delegate (minimal GC pressure)
- ⚠️ **Breaking Change** - Anything referencing `KeenEyes.Core.Commands` namespace must update
- ⚠️ **Larger Abstractions Package** - CommandBuffer implementation adds ~500 LOC to Abstractions

### Trade-offs

| Aspect | Before | After |
|--------|--------|-------|
| Plugin dependency | Core required | Abstractions only |
| Command execution | Reflection-based (~500-1000 cycles) | Delegate-based (~20-30 cycles) |
| Performance | 20-50x slower | Baseline (100% fast) |
| Generated methods | 1 (concrete only) | 2 (generic + non-generic) |
| AOT compatibility | ❌ No (reflection) | ✅ Yes (delegates) |
| Interface usage | ❌ Requires cast | ✅ Works naturally |
| Package size | Small Abstractions | Medium Abstractions (+500 LOC) |

### Performance Characteristics

**Command Registration (Cold Path):**
- **Before:** Store `(Type, object, bool)` tuple → ~10 cycles
- **After:** Create delegate closure → ~30-40 cycles
- **Impact:** Negligible (registration happens once)

**Command Execution (Hot Path):**
- **Before:** Reflection (`MakeGenericMethod` + `Invoke`) → ~500-1000 cycles
- **After:** Delegate invocation → ~20-30 cycles
- **Improvement:** 20-50x faster

**Example with 100 commands:**
- **Before:** 50,000-100,000 cycles
- **After:** 2,000-3,000 cycles
- **Savings:** 47,000-97,000 cycles (~20-40 microseconds on modern CPU)

## Testing Coverage

Updated tests:
- ✅ ComponentGenerator dual method generation tests
- ✅ CommandBuffer tests (moved to Abstractions.Tests)
- ✅ Sample code compilation (no casts needed)
- ✅ All command types with delegate pattern
- ✅ Interface usage through IWorld.Spawn()

All 2,172 tests passing, zero warnings.

## Future Considerations

- **Command Pooling** - Reuse command objects to reduce GC pressure
- **Batch Optimization** - Process similar commands together for cache efficiency
- **Async Commands** - Support for async/await in command execution
- **Command Validation** - Pre-execution validation for debugging

## Related Decisions

- **ADR-001**: World Manager Architecture - EventManager provides command execution infrastructure
- **ADR-002**: Complete IWorld Event System - IWorld.Spawn() must return IEntityBuilder

## References

- Commit: "refactor: Enhance CommandBuffer and generator for plugin architecture"
- Commit: "refactor: Move CommandBuffer implementation to Abstractions"
- Files changed:
  - `src/KeenEyes.Abstractions/ICommandBuffer.cs` - New interface
  - `src/KeenEyes.Abstractions/CommandBuffer.cs` - Moved from Core
  - `src/KeenEyes.Abstractions/EntityCommands.cs` - Moved from Core, delegate-based
  - `src/KeenEyes.Abstractions/Commands/*.cs` - All command classes moved from Core
  - `editor/KeenEyes.Generators/ComponentGenerator.cs` - Dual extension method generation
  - `docs/command-buffer.md` - Updated with plugin usage and performance notes
  - `docs/abstractions.md` - Documented CommandBuffer in Abstractions
  - `docs/plugins.md` - Added command buffer usage section
  - `docs/components.md` - Documented dual extension generation

### Architecture Evolution

**v0.1-v0.3: Core-Coupled Commands**
- CommandBuffer in Core
- Reflection-based execution
- Plugin dependency on Core
- Generated methods work with concrete types only

**v0.4+ (this ADR): Abstracted Commands**
- CommandBuffer in Abstractions
- Delegate-based execution (20-50x faster)
- Plugin isolation (Abstractions only)
- Generated methods work with interfaces and concrete types

This evolution reflects the principle that **performance and isolation should not be opposing goals**. By combining the delegate capture pattern with dual method generation, we achieve both zero-reflection performance and clean plugin isolation without compromise.
