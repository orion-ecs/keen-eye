# ADR-004: Reflection Elimination for AOT Compatibility

**Status:** Accepted
**Revision:** v2
**Implementation:** Shipped
**First accepted:** 2025-12-11 · **Last amended:** 2026-07-26
**Relates to:** [#1079](https://github.com/orion-ecs/keen-eye/issues/1079)

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

Eliminate all reflection from production code using these patterns (all five shipped; the code below reflects the as-built implementation):

### Pattern 1: Factory Delegate Registration (ArchetypeChunk)

`ComponentInfo` carries a `CreateComponentArray` factory delegate, assigned at component registration time when the generic type is known:

```csharp
// In ComponentInfo
internal Func<int, IComponentArray>? CreateComponentArray { get; set; }

// Assigned in ComponentRegistry.Register<T> (type is known)
CreateComponentArray = capacity => new FixedComponentArray<T>(capacity),

// Usage in the ArchetypeChunk constructor - no reflection
var array = info.CreateComponentArray!(capacity);
```

No `Activator.CreateInstance` or `MakeGenericType` remains anywhere in `KeenEyes.Core`.

### Pattern 2: Typed Wrapper Interface (MessageManager)

`MessageManager` stores queues as `Dictionary<Type, IMessageQueueWrapper>`, where the wrapper is a private nested typed class implementing a type-erased interface:

```csharp
private interface IMessageQueueWrapper
{
    int Count { get; }
    void Clear();
    void Process(object handlersObj);
}

private sealed class MessageQueueWrapper<T> : IMessageQueueWrapper
{
    public Queue<T> Queue { get; } = new();

    public int Count => Queue.Count;

    public void Clear() => Queue.Clear();

    public void Process(object handlersObj)
    {
        var handlerList = (List<Action<T>>)handlersObj;

        // Snapshot the handler list so a handler can safely unsubscribe mid-dispatch
        var snapshot = handlerList.ToArray();

        while (Queue.Count > 0)
        {
            var message = Queue.Dequeue();

            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i](message);
            }
        }
    }
}
```

Processing dispatches through the wrapper with no `MakeGenericMethod`/`Invoke`.

### Pattern 3: Stored Invokers (ComponentValidationManager)

Validator invocation is AOT-safe via `ComponentInfo.InvokeValidator`, a typed delegate captured once at component registration (consolidated onto `ComponentInfo` rather than a separate invoker dictionary):

```csharp
// In ComponentInfo
internal Func<World, Entity, object, Delegate, bool>? InvokeValidator { get; set; }

// Assigned in ComponentRegistry.Register<T>
InvokeValidator = (world, entity, data, validator) =>
{
    var typedValidator = (ComponentValidator<T>)validator;
    var component = (T)data;
    return typedValidator(world, entity, component);
}
```

`ComponentValidationManager` calls this delegate to invoke custom validators without reflection. No `GetCustomAttributes` or assembly scanning remains in `KeenEyes.Core`.

### Pattern 4: Applicator Delegates (PrefabManager)

The applicator-delegate pattern shipped as `ComponentInfo.ApplyToBuilder` / `ApplyTagToBuilder`, assigned at component registration:

```csharp
// In ComponentInfo
internal Action<EntityBuilder, object>? ApplyToBuilder { get; set; }
internal Action<EntityBuilder>? ApplyTagToBuilder { get; set; }

// Assigned in ComponentRegistry.Register<T>
ApplyToBuilder = (builder, boxedValue) => builder.With((T)boxedValue),
```

The original consumer — the runtime prefab API (`PrefabManager`, `EntityPrefab`, `ComponentDefinition`) — was removed entirely in July 2026 ([#1079](https://github.com/orion-ecs/keen-eye/issues/1079)); prefabs are now `.keprefab` assets with source-generated spawn methods, which never used reflection. The applicator delegates remain in use for boxed component application generally.

### Pattern 5: Extended Serializer Interface (SnapshotManager)

`IComponentSerializer` exposes registration and singleton operations over `ISerializationCapability`:

```csharp
public interface IComponentSerializer
{
    // Existing...

    ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag);
    bool SetSingleton(ISerializationCapability serialization, string typeName, object value);
}

// SerializationGenerator emits switch-on-type-name implementations
public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)
{
    return typeName switch
    {
        "MyGame.Position" => serialization.Components.Register<Position>(isTag),
        "MyGame.Velocity" => serialization.Components.Register<Velocity>(isTag),
        _ => null
    };
}
```

`SnapshotManager` and `DeltaRestorer` route all deserialization-time component registration and singleton restoration through these methods — no `MakeGenericMethod` or `Type.GetType`.

### Implementation Order

All five refactors were implemented on 2025-12-11, the same day this ADR was written (commit e823210c eliminated the reflection, fd61ece6 removed the remaining fallback paths). The priority table below is preserved as the original plan; the `PrefabManager.cs` row is now moot since the runtime prefab API was removed entirely in [#1079](https://github.com/orion-ecs/keen-eye/issues/1079).

| Priority | File | Effort | Justification |
|----------|------|--------|---------------|
| 1 | ArchetypeChunk.cs | Low | Called during archetype creation; simple factory pattern |
| 2 | MessageManager.cs | Medium | Called every `ProcessQueuedMessages()`; typed wrapper is clean |
| 3 | PrefabManager.cs | Medium | Only at prefab spawn; store applicators at definition time |
| 4 | SnapshotManager.cs | Medium | Only at save/load; extend existing `IComponentSerializer` |
| 5 | ComponentValidationManager.cs | Low | Already has generated path; enhance with stored invokers |

Compliance is continuously enforced: the [aot-compatibility workflow](../../.github/workflows/aot-compatibility.yml) gates every PR with an AOT-analyzer build, then natively publishes and runs `tests/KeenEyes.AotCompatibility.Tests` and [`samples/KeenEyes.Sample.Aot`](../../samples/KeenEyes.Sample.Aot/) on linux-x64, win-x64, and osx-arm64 with `-warnaserror`.

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

- **Native AOT Compatible**: All production code publishes and runs under `PublishAot=true`, verified on every PR by the aot-compatibility workflow (analyzer build plus native publish and execution of an AOT test project and sample on linux-x64, win-x64, and osx-arm64 with zero trimming warnings)
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
- **Fallbacks Removed**: No reflection fallbacks remain in production code — commit fd61ece6 removed them, and the repo-wide policy (CLAUDE.md "No Reflection in Production Code") plus the AOT CI gate prevent reintroduction. Test code and `#if DEBUG` diagnostics are the only sanctioned exceptions
- **Testing Required**: Each refactored area needs additional unit tests

## References

- [.NET Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trimming and Native AOT Warnings](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/fixing-warnings)
- [Source Generators Overview](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [AOT compatibility CI workflow](../../.github/workflows/aot-compatibility.yml)
- [AOT sample project](../../samples/KeenEyes.Sample.Aot/)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Accepted — all five reflection eliminations shipped 2025-12-11 (commits e823210c, fd61ece6) and Implementation is Shipped, gated by the three-OS AOT CI workflow. Decision patterns amended to as-built names and signatures (`ArrayFactory` → `ComponentInfo.CreateComponentArray`; `IMessageQueue`/`MessageQueue<T>` → private nested `IMessageQueueWrapper`/`MessageQueueWrapper<T>`; `validatorInvokers` dictionary → `ComponentInfo.InvokeValidator`; serializer `Register`/`SetSingleton` → `RegisterComponent`/`SetSingleton` over `ISerializationCapability`). Noted that the runtime prefab API targeted by Pattern 4 was deleted in #1079 (replaced by `.keprefab` assets with source-generated spawn methods, applicator delegates retained as `ComponentInfo.ApplyToBuilder`/`ApplyTagToBuilder`). Consequences amended: reflection fallbacks were removed outright rather than preserved, and AOT compatibility is now continuously CI-verified.
- **v1 — 2025-12-11 (b714ad14):** Proposed — eliminate all reflection (~34 operations across 5 KeenEyes.Core files) via factory delegates, typed queue wrappers, stored invokers, applicator delegates, and an extended IComponentSerializer, to make the runtime Native-AOT compatible. Implementation landed in code the same day (commits e823210c, fd61ece6) without further edits to the ADR itself.
