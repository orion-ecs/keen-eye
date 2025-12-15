# Why Native AOT?

KeenEyes is designed to be fully compatible with .NET Native AOT (Ahead-of-Time) compilation. This page explains why this matters and how it influences our design decisions.

## What is Native AOT?

Traditional .NET uses Just-In-Time (JIT) compilation:

```
Your Code (.cs) → IL (.dll) → JIT at Runtime → Machine Code
```

Native AOT compiles directly to machine code:

```
Your Code (.cs) → IL (.dll) → AOT at Build → Native Binary
```

The result is a standalone executable with no runtime dependencies.

## Why Does This Matter?

### 1. Faster Startup

JIT compilation happens at startup. Native AOT has none:

| Metric | JIT | Native AOT |
|--------|-----|------------|
| Cold start | 150-500ms | 10-50ms |
| Memory at start | ~30MB | ~10MB |
| Time to first frame | Variable | Consistent |

For games:
- No "loading JIT" stutter on level load
- Consistent frame times from the first frame
- Faster iteration in development builds

### 2. Smaller Binaries

Native AOT enables aggressive tree shaking:

| Build Type | Size |
|------------|------|
| JIT (self-contained) | ~80MB |
| Native AOT | ~15-30MB |
| Native AOT (trimmed) | ~8-15MB |

Unused code is completely removed, not just "not loaded."

### 3. Required Platforms

Some platforms mandate AOT:

- **iOS**: Apple requires AOT (no JIT allowed)
- **WebAssembly**: WASM doesn't have a JIT
- **Xbox/PlayStation**: Console certification often requires AOT
- **Some Linux distros**: Embedded systems with no JIT runtime

If you want KeenEyes games on these platforms, AOT is required.

### 4. Predictable Performance

JIT has runtime overhead:
- Background compilation affects frame times
- Tiered compilation means performance varies
- First call to a method triggers compilation

Native AOT:
- All code is optimized at build time
- No runtime compilation overhead
- Consistent performance from start to finish

## What Breaks with AOT?

Native AOT can't do anything that requires generating code at runtime:

### Runtime Generic Instantiation

```csharp
// FAILS: Requires runtime code generation
public T Create<T>() where T : new()
{
    return new T();  // AOT can't generate this for unknown T
}

var type = GetSomeRuntimeType();
var method = typeof(Foo).GetMethod("Create").MakeGenericMethod(type);
method.Invoke(foo, null);  // Runtime-determined T - impossible
```

### Dynamic Method Compilation

```csharp
// FAILS: Expression compilation requires JIT
var param = Expression.Parameter(typeof(int));
var body = Expression.Add(param, Expression.Constant(1));
var lambda = Expression.Lambda<Func<int, int>>(body, param);
var compiled = lambda.Compile();  // JIT required
```

### Assembly Loading

```csharp
// FAILS: No runtime assembly loading
var assembly = Assembly.LoadFile("plugin.dll");  // Not supported
var types = assembly.GetTypes();  // Can't enumerate runtime-loaded types
```

### Reflection on Non-Preserved Types

```csharp
// MAY FAIL: Type might be trimmed
var type = Type.GetType("MyGame.SomeComponent");  // null if trimmed
Activator.CreateInstance(type);  // Exception
```

## How KeenEyes Supports AOT

### Pattern 1: Factory Delegates

Instead of `Activator.CreateInstance`:

```csharp
// BAD: Reflection-based creation
public object CreateComponent(Type type)
{
    return Activator.CreateInstance(type);  // AOT fails
}

// GOOD: Factory delegate stored at registration
public delegate IComponent ComponentFactory();

public class ComponentInfo
{
    public ComponentFactory Factory { get; init; }
}

// At registration (compile-time type known)
registry.Register<Position>(() => new Position());

// At runtime (no reflection needed)
var component = componentInfo.Factory();
```

### Pattern 2: Static Abstract Interface Members

Instead of reflection on static fields:

```csharp
// BAD: Reflection to access static field
var field = typeof(TBundle).GetField("ComponentTypes");
var types = (Type[])field.GetValue(null);

// GOOD: Static abstract interface member
public interface IBundle
{
    static abstract Type[] ComponentTypes { get; }
}

// Usage - direct access, no reflection
var types = TBundle.ComponentTypes;
```

### Pattern 3: Source Generators

Instead of runtime attribute reading:

```csharp
// BAD: Runtime attribute discovery
var attrs = componentType.GetCustomAttributes<RequiresAttribute>();

// GOOD: Generated lookup table
// Generator reads attributes at compile time
// Generates switch statement or dictionary
public static ValidationConstraints? GetConstraints(Type type)
{
    return type.FullName switch
    {
        "MyGame.Health" => new ValidationConstraints(...),
        "MyGame.Armor" => new ValidationConstraints(...),
        _ => null
    };
}
```

### Pattern 4: Explicit Registration

Instead of assembly scanning:

```csharp
// BAD: Runtime type discovery
var components = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(t => typeof(IComponent).IsAssignableFrom(t));

// GOOD: Explicit registration
world.Components.Register<Position>();
world.Components.Register<Velocity>();
world.Components.Register<Health>();

// Or generated registry
ComponentRegistry.RegisterAll(world);  // Generated from [Component] attributes
```

## Testing AOT Compatibility

Enable AOT analysis in your project:

```xml
<PropertyGroup>
    <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

This generates warnings for AOT-incompatible patterns:

```
warning IL3050: Using 'System.Type.MakeGenericType' requires all generic
arguments to be statically known at compile time.
```

Build with AOT to verify:

```bash
dotnet publish -c Release -r linux-x64 /p:PublishAot=true
```

## Performance Comparison

Benchmarks from a real game scenario:

| Metric | JIT | Native AOT | Improvement |
|--------|-----|------------|-------------|
| Startup time | 450ms | 35ms | 13x faster |
| Binary size | 85MB | 22MB | 4x smaller |
| Memory (idle) | 45MB | 18MB | 2.5x less |
| First frame | 32ms | 16ms | 2x faster |
| P99 frame time | 18ms | 14ms | 22% better |

*Results from a game with 50,000 entities on Linux x64*

## When JIT is Still Useful

Native AOT isn't always better:

1. **Development builds**: JIT is faster to compile
2. **Debugging**: Better debug experience with JIT
3. **Plugin systems**: If you need runtime assembly loading
4. **Scripting**: If you need to compile code at runtime

KeenEyes supports both:
- Development: Use JIT for fast iteration
- Release: Use Native AOT for deployment

## Tradeoffs

### Longer Build Times

AOT compilation is slower:

| Build | JIT | Native AOT |
|-------|-----|------------|
| Debug | 5s | 30s |
| Release | 15s | 90s |

Mitigated by only using AOT for release builds.

### Platform-Specific Binaries

JIT: One DLL runs anywhere .NET runs
AOT: One binary per target platform

You need separate builds for:
- Windows x64
- Linux x64
- macOS ARM64
- etc.

### Larger Initial Download

While AOT binaries are smaller than self-contained JIT, they're larger than framework-dependent JIT (which shares the runtime).

## Adopting AOT in Your Project

1. **Enable analysis**:
   ```xml
   <IsAotCompatible>true</IsAotCompatible>
   ```

2. **Fix warnings**: Each IL3050/IL2050 warning indicates an AOT issue

3. **Test with AOT build**:
   ```bash
   dotnet publish -c Release -r win-x64 /p:PublishAot=true
   ```

4. **Profile both**: Compare JIT and AOT performance for your use case

## See Also

- [.NET Native AOT Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [ADR-004: Reflection Elimination](../adr/004-reflection-elimination.md) - Technical implementation
- [Why Source Generators?](source-generators.md) - How generators enable AOT
- [AOT Deployment Guide](../aot-deployment.md) - Deploying with AOT
