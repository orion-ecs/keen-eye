# Why Source Generators?

KeenEyes uses Roslyn source generators to create efficient, type-safe code at compile time. This page explains why we chose this approach over alternatives.

## The Problem: Boilerplate vs Performance

ECS requires repetitive patterns:

```csharp
// Without any automation - lots of boilerplate
public struct Position : IComponent
{
    public float X;
    public float Y;
}

// Registration (every component, every world)
world.Components.Register<Position>();
world.Components.Register<Velocity>();
world.Components.Register<Health>();
// ... hundreds more

// Query iteration (manual pattern)
foreach (var entity in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(entity);
    ref readonly var vel = ref world.Get<Velocity>(entity);
    // ...
}
```

Common solutions:

1. **Manual code** - Error-prone, tedious
2. **Runtime reflection** - Convenient but slow, no AOT support
3. **Source generators** - Best of both worlds

## How Source Generators Work

Source generators run at compile time:

```
Your Code → Roslyn Compiler → Generator → Additional Code → Compilation
```

They see your source code and generate additional C# code that compiles alongside yours.

### Example: Component Generation

You write:
```csharp
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}
```

Generator produces:
```csharp
// Auto-generated - do not edit
public partial struct Position : IComponent
{
    // Fluent builder method
    public static EntityBuilder With(EntityBuilder builder, Position component)
        => builder.With(component);
}

public static class PositionBuilderExtensions
{
    public static EntityBuilder WithPosition(this EntityBuilder builder, float x, float y)
        => builder.With(new Position { X = x, Y = y });
}
```

You can now write:
```csharp
var entity = world.Spawn()
    .WithPosition(10, 20)  // Generated method
    .WithVelocity(1, 0)    // Generated method
    .Build();
```

## Why Not Runtime Reflection?

Reflection reads type metadata at runtime:

```csharp
// Reflection-based approach
public void RegisterAllComponents(Assembly assembly)
{
    foreach (var type in assembly.GetTypes())
    {
        if (typeof(IComponent).IsAssignableFrom(type))
        {
            // Use reflection to get constructor, fields, etc.
            var method = typeof(ComponentRegistry)
                .GetMethod("Register")
                .MakeGenericMethod(type);
            method.Invoke(registry, null);
        }
    }
}
```

### Problem 1: Performance

Reflection is 10-100x slower than direct calls:

| Operation | Direct Call | Reflection |
|-----------|-------------|------------|
| Method invoke | ~1ns | ~100ns |
| Field access | ~0.5ns | ~50ns |
| Type lookup | ~0.1ns | ~10ns |

Not dramatic for one-time setup, but problematic if used in hot paths.

### Problem 2: No Native AOT Support

Native AOT compiles directly to machine code - no JIT, no runtime type generation:

```csharp
// This FAILS with Native AOT
var method = genericMethod.MakeGenericMethod(runtimeType);  // Requires JIT
method.Invoke(target, args);  // Dynamic dispatch
```

Native AOT is increasingly important for:
- Mobile games (iOS requires AOT)
- WebAssembly (WASM has no JIT)
- Cloud functions (faster cold starts)
- Console games (fixed hardware)

### Problem 3: Trimming Issues

The IL Linker removes unused code for smaller binaries. It can't trace reflection calls:

```csharp
// Linker can't see this uses MyComponent
var type = Type.GetType("MyGame.MyComponent");
Activator.CreateInstance(type);  // May be trimmed away!
```

You need `[DynamicDependency]` attributes everywhere, which is fragile.

### Problem 4: No Compile-Time Validation

Reflection errors surface at runtime:

```csharp
// Typo - won't error until runtime
var type = Type.GetType("MyGame.Positoin");  // Misspelled
registry.Register(type);  // null type - runtime exception
```

With source generators, errors appear in your IDE as you type.

## Source Generator Benefits

### Benefit 1: Zero Runtime Cost

Generated code is identical to hand-written code:

```csharp
// Generated (compiles to same IL as hand-written)
public static EntityBuilder WithPosition(this EntityBuilder builder, float x, float y)
    => builder.With(new Position { X = x, Y = y });
```

No reflection, no boxing, no dynamic dispatch.

### Benefit 2: Native AOT Compatible

All code exists at compile time:

```bash
dotnet publish -c Release -r linux-x64 --self-contained /p:PublishAot=true
```

Works perfectly because there's no runtime code generation.

### Benefit 3: Trimming Safe

The linker sees all code paths:

```csharp
// Generated code - linker sees the direct reference
builder.With(new Position { X = x, Y = y });
// Position type won't be trimmed
```

### Benefit 4: Compile-Time Errors

IDE shows problems immediately:

```csharp
[Component]
public partial struct Position
{
    // Error: 'X' is not a valid field name (if you had a typo)
}

// If you forget [Component]:
world.Spawn().WithPosition(10, 20);  // Error: WithPosition doesn't exist
```

### Benefit 5: IDE Support

Generated methods appear in IntelliSense:

```
world.Spawn().With  →  [Autocomplete dropdown]
                       WithPosition(float x, float y)
                       WithVelocity(float x, float y)
                       WithHealth(int current, int max)
```

## What KeenEyes Generates

### Component Extensions

```csharp
[Component]
public partial struct Health
{
    public int Current;
    public int Max;
}

// Generates:
// - WithHealth(int current, int max) builder extension
// - Implements IComponent interface
```

### Tag Components

```csharp
[TagComponent]
public partial struct Enemy { }

// Generates:
// - WithTag<Enemy>() builder extension
// - Implements ITagComponent interface
// - Zero-size component optimization
```

### System Metadata

```csharp
[System(Phase = SystemPhase.Update, Order = 100)]
public partial class MovementSystem : SystemBase
{
    // Generates:
    // - Phase property override
    // - Order property override
    // - Registration helpers
}
```

### Query Iterators

```csharp
// With generators, queries are optimized
foreach (var entity in world.Query<Position, Velocity>())
{
    // Generated iterator avoids allocations
    // Direct archetype chunk access
}
```

### Serialization

```csharp
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

// Generates:
// - IComponentSerializer implementation
// - Binary serialization methods
// - JSON serialization support
```

## Tradeoffs

### Longer Compile Times

Generators add compilation overhead:
- Initial compile: +10-30%
- Incremental compile: Minimal impact

Mitigated by incremental generation - only regenerates when source changes.

### Generated Code Complexity

Sometimes generated code is hard to debug:

```csharp
// Error in generated code?
// Check: obj/Debug/net10.0/generated/KeenEyes.Generators/...
```

Mitigated by clear generation patterns and good error messages.

### Attribute Requirements

You must mark types with attributes:

```csharp
[Component]  // Required for generation
public partial struct Position { }
```

A small cost for the benefits gained.

## Comparison Table

| Feature | Reflection | Source Generators |
|---------|------------|-------------------|
| Runtime performance | Slow | Fast |
| Native AOT | No | Yes |
| Trimming | Fragile | Safe |
| Compile-time errors | No | Yes |
| IDE support | Limited | Full |
| Compile time | Fast | Slightly slower |
| Complexity | Simple | More setup |

## See Also

- [ADR-004: Reflection Elimination](../adr/004-reflection-elimination.md) - Full technical analysis
- [Why Native AOT?](native-aot.md) - AOT compilation benefits
- [Components Guide](../components.md) - Using generated component code
