# ADR-004: Reflection Elimination for AOT Compatibility

**Status:** Proposed
**Date:** 2025-12-11

## Context

The KeenEyes runtime uses reflection in **5 production files** with ~34 distinct reflection operations:

| File | Reflection Pattern | Usage |
|------|-------------------|-------|
| `ArchetypeChunk.cs` | `MakeGenericType` + `Activator.CreateInstance` | Create `FixedComponentArray<T>` at runtime |
| `MessageManager.cs` | `MakeGenericMethod` + `Invoke` | Process untyped message queues |
| `ComponentValidationManager.cs` | `GetCustomAttributes`, `MakeGenericMethod`, assembly scanning | Read validation attributes, invoke validators |
| `PrefabManager.cs` | `MakeGenericMethod` + `Invoke` | Apply prefab components to entity builders |
| `SnapshotManager.cs` | `MakeGenericMethod` + `Invoke`, `Type.GetType` | Register components and set singletons during deserialization |

### Problems with Reflection

1. **Native AOT Incompatibility**
   - `MakeGenericMethod()` requires runtime code generation
   - `Activator.CreateInstance()` requires runtime type instantiation
   - `MethodInfo.Invoke()` uses dynamic dispatch
   - Native AOT cannot generate code at runtime; these patterns fail

2. **Performance Overhead**
   - Reflection is 10-100x slower than direct calls
   - `GetMethod()` involves string parsing and type lookups
   - `Invoke()` boxes arguments and has dispatch overhead
   - While not in hot paths today, this limits future optimization

3. **Trimming Issues**
   - IL Linker cannot statically analyze reflection calls
   - Requires `DynamicDependency` attributes or trimmer warnings
   - Risk of runtime failures in trimmed applications

4. **Debugging Difficulty**
   - Reflection calls have poor stack traces
   - No compile-time type checking
   - Errors surface at runtime rather than build time

### Current State

The codebase already has patterns to avoid reflection:
- Source generators exist for components, queries, systems, serialization, and validation
- `IComponentSerializer` interface provides AOT-compatible deserialization path
- `IComponentArray` interface enables type-erased component storage

However, reflection remains as fallback paths or in areas not yet addressed by generators.

## Decision

Eliminate all reflection from production code using these patterns:

### Pattern 1: Factory Delegate Registration (ArchetypeChunk)

Store factory delegates at component registration time when the generic type is known:

```csharp
// In ComponentInfo
public Func<int, IComponentArray> ArrayFactory { get; }

// At registration (type is known)
public ComponentInfo Register<T>(bool isTag = false) where T : struct, IComponent
{
    return new ComponentInfo
    {
        ArrayFactory = capacity => new FixedComponentArray<T>(capacity),
        // ...
    };
}

// Usage - no reflection
var array = componentInfo.ArrayFactory(capacity);
```

### Pattern 2: Typed Wrapper Interface (MessageManager)

Replace `Dictionary<Type, object>` with typed wrappers that implement a common interface:

```csharp
internal interface IMessageQueue
{
    void Process(object handlers);
    void Clear();
    int Count { get; }
}

internal sealed class MessageQueue<T> : IMessageQueue
{
    private readonly Queue<T> queue = new();

    public void Process(object handlers)
    {
        var handlerList = (List<Action<T>>)handlers;
        while (queue.Count > 0)
        {
            var msg = queue.Dequeue();
            foreach (var handler in handlerList)
                handler(msg);
        }
    }
    // ...
}
```

### Pattern 3: Stored Invokers (ComponentValidationManager)

Cache typed invoker delegates at registration time:

```csharp
private readonly Dictionary<Type, Func<Entity, object, Delegate, bool>> validatorInvokers = [];

public void RegisterValidator<T>(ComponentValidator<T> validator) where T : struct, IComponent
{
    customValidators[typeof(T)] = validator;
    validatorInvokers[typeof(T)] = (entity, data, del) =>
    {
        var typed = (ComponentValidator<T>)del;
        return typed(world, entity, (T)data);
    };
}
```

### Pattern 4: Applicator Delegates (PrefabManager)

Store typed applicators in component definitions:

```csharp
public sealed class ComponentDefinition
{
    public Action<EntityBuilder>? Applicator { get; init; }
}

public static ComponentDefinition Create<T>(T component) where T : struct, IComponent
{
    return new ComponentDefinition
    {
        Type = typeof(T),
        Data = component,
        Applicator = builder => builder.With(component)
    };
}
```

### Pattern 5: Extended Serializer Interface (SnapshotManager)

Extend `IComponentSerializer` to handle registration and singleton operations:

```csharp
public interface IComponentSerializer
{
    // Existing...

    ComponentInfo? Register(ComponentRegistry registry, string typeName, bool isTag);
    bool SetSingleton(World world, string typeName, object value);
}

// Generated implementation uses switch on type names
public ComponentInfo? Register(ComponentRegistry registry, string typeName, bool isTag)
{
    return typeName switch
    {
        "MyGame.Position" => registry.Register<Position>(isTag),
        "MyGame.Velocity" => registry.Register<Velocity>(isTag),
        _ => null
    };
}
```

### Implementation Order

| Priority | File | Effort | Justification |
|----------|------|--------|---------------|
| 1 | ArchetypeChunk.cs | Low | Called during archetype creation; simple factory pattern |
| 2 | MessageManager.cs | Medium | Called every `ProcessQueuedMessages()`; typed wrapper is clean |
| 3 | PrefabManager.cs | Medium | Only at prefab spawn; store applicators at definition time |
| 4 | SnapshotManager.cs | Medium | Only at save/load; extend existing `IComponentSerializer` |
| 5 | ComponentValidationManager.cs | Low | Already has generated path; enhance with stored invokers |

## Alternatives Considered

### Option 1: Use `[DynamicDependency]` Attributes

Annotate reflection targets to preserve them during trimming:

```csharp
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(MessageManager))]
private static void ProcessTypedQueue(...) { }
```

**Rejected because:**
- Does not solve Native AOT incompatibility (still uses runtime codegen)
- Requires manual maintenance as code evolves
- Only addresses trimming, not performance

### Option 2: `RuntimeHelpers.CreateSpan` + Unsafe Casting

Use unsafe memory operations to avoid generic instantiation:

**Rejected because:**
- Introduces unsafe code throughout
- Harder to reason about correctness
- Still requires reflection for method dispatch

### Option 3: Expression Trees

Build and compile expression trees instead of reflection:

```csharp
var param = Expression.Parameter(typeof(object));
var cast = Expression.Convert(param, componentType);
var lambda = Expression.Lambda<Func<object, IComponent>>(cast, param).Compile();
```

**Rejected because:**
- `Expression.Compile()` still requires JIT (fails in AOT)
- More complex than delegate caching
- Similar performance to reflection when not cached

### Option 4: Accept Reflection (Document as Limitation)

Keep reflection and document that Native AOT is not supported.

**Rejected because:**
- Native AOT is increasingly important (mobile, WASM, cloud functions)
- Game engines are a primary AOT target
- Limits adoption for performance-sensitive scenarios
- Other ECS frameworks (Arch, Flecs.NET) support AOT

## Consequences

### Positive

- **Native AOT Compatible**: All production code will work with `PublishAot=true`
- **Trimming Safe**: No runtime type discovery; IL Linker can safely trim
- **Better Performance**: Delegate calls are ~100x faster than `MethodInfo.Invoke`
- **Compile-Time Safety**: Type errors caught at build time, not runtime
- **Improved Debugging**: Clear stack traces without reflection frames
- **Reduced Memory**: No `MethodInfo` caching or reflection metadata

### Negative

- **Increased Code Complexity**: Interfaces and delegates add indirection
- **Migration Effort**: Each file requires careful refactoring
- **More Generated Code**: Source generators produce more output
- **Boxed Fallback Path**: Unknown types still require `object` boxing

### Neutral

- **Public API Unchanged**: All changes are internal
- **Fallback Preserved**: Reflection can remain for edge cases with clear documentation
- **Testing Required**: Each refactored area needs additional unit tests

## References

- [.NET Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trimming and Native AOT Warnings](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/fixing-warnings)
- [Source Generators Overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
