# Component Bundles Guide

Component bundles group commonly-used components together, reducing boilerplate and improving entity creation ergonomics. This guide covers when and how to use bundles.

## What Are Bundles?

Bundles are compile-time constructs that bundle multiple components into a single reusable group. They're similar to prefabs but operate at the component level rather than the entity level.

```csharp
// Without bundles (verbose)
var enemy = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Rotation { Angle = 0 })
    .With(new Scale { X = 1, Y = 1 })
    .With(new Health { Current = 100, Max = 100 })
    .With(new Damage { Amount = 10 })
    .WithTag<EnemyTag>()
    .Build();

// With bundles (concise)
var enemy = world.Spawn()
    .With(new TransformBundle(x: 0, y: 0))
    .With(new ActorBundle(health: 100, damage: 10))
    .WithTag<EnemyTag>()
    .Build();
```

## Defining Bundles

Use the `[Bundle]` attribute on a partial struct:

```csharp
using KeenEyes.Generators.Attributes;

[Bundle]
public partial struct TransformBundle
{
    public Position Position;
    public Rotation Rotation;
    public Scale Scale;
}

[Bundle]
public partial struct ActorBundle
{
    public Health Health;
    public Damage Damage;
    public Named Name;
}
```

### Bundle Requirements

1. **Struct type** - Bundles must be structs (value types)
2. **Partial** - Must be declared `partial` for source generation
3. **Component fields** - Fields must be components (types implementing `IComponent`)
4. **At least one field** - Empty bundles are not allowed

## Generated Code

The source generator creates several helpers for each bundle:

### 1. Constructor

```csharp
// Generated for TransformBundle
public TransformBundle(Position position, Rotation rotation, Scale scale)
{
    Position = position;
    Rotation = rotation;
    Scale = scale;
}
```

### 2. EntityBuilder Extension

```csharp
// Generated: With(bundle) extension method
public static EntityBuilder With(this EntityBuilder builder, TransformBundle bundle)
{
    return builder
        .With(bundle.Position)
        .With(bundle.Rotation)
        .With(bundle.Scale);
}
```

### 3. World Add/Remove Extensions

```csharp
// Generated: AddTransformBundle extension method
public static void AddTransformBundle(this World world, Entity entity,
    Position position, Rotation rotation, Scale scale)
{
    world.Add(entity, position);
    world.Add(entity, rotation);
    world.Add(entity, scale);
}

// Generated: RemoveTransformBundle extension method
public static void RemoveTransformBundle(this World world, Entity entity)
{
    world.Remove<Position>(entity);
    world.Remove<Rotation>(entity);
    world.Remove<Scale>(entity);
}
```

### 4. Bundle Queries

```csharp
// Query all entities with all bundle components
foreach (var entity in world.Query<TransformBundle>())
{
    ref var transform = ref world.GetBundle(entity, default(TransformBundle));
    // transform.Position, transform.Rotation, transform.Scale are all refs
}
```

### 5. GetBundle Method

```csharp
// Generated: Returns a ref struct with refs to all components
public ref struct TransformBundleRef
{
    public ref Position Position;
    public ref Rotation Rotation;
    public ref Scale Scale;
}

// Zero-copy access to all bundle components
var transform = world.GetBundle(entity, default(TransformBundle));
transform.Position.X += 10;  // Direct modification
```

## Common Bundle Patterns

### Transform Bundle

The most common bundle for spatial data:

```csharp
[Component]
public partial struct Position { public float X, Y; }

[Component]
public partial struct Rotation { public float Angle; }

[Component]
public partial struct Scale { public float X, Y; }

[Bundle]
public partial struct TransformBundle
{
    public Position Position;
    public Rotation Rotation;
    public Scale Scale;
}

// Usage
var entity = world.Spawn()
    .With(new TransformBundle
    {
        Position = new() { X = 100, Y = 50 },
        Rotation = new() { Angle = 0 },
        Scale = new() { X = 1, Y = 1 }
    })
    .Build();
```

### Physics Bundle

Group physics-related components:

```csharp
[Bundle]
public partial struct PhysicsBundle
{
    public Position Position;
    public Velocity Velocity;
    public Collider Collider;
    public RigidBody RigidBody;
}

// Usage
var dynamicObject = world.Spawn()
    .With(new PhysicsBundle
    {
        Position = new() { X = 0, Y = 0 },
        Velocity = new() { X = 10, Y = 0 },
        Collider = new() { Radius = 5 },
        RigidBody = new() { Mass = 1.0f }
    })
    .Build();
```

### Character Bundle

Common components for game characters:

```csharp
[Bundle]
public partial struct CharacterBundle
{
    public Position Position;
    public Health Health;
    public Mana Mana;
    public Inventory Inventory;
}

// Usage
var player = world.Spawn()
    .With(new CharacterBundle
    {
        Position = new() { X = 0, Y = 0 },
        Health = new() { Current = 100, Max = 100 },
        Mana = new() { Current = 50, Max = 50 },
        Inventory = new() { Capacity = 20 }
    })
    .WithTag<PlayerTag>()
    .Build();
```

## Nested Bundles

Bundles can contain other bundles (up to 5 levels deep):

```csharp
[Bundle]
public partial struct EntityBundle
{
    public TransformBundle Transform;  // Nested bundle
    public PhysicsBundle Physics;      // Nested bundle
    public SpriteRenderer Sprite;
}

// Usage
var entity = world.Spawn()
    .With(new EntityBundle
    {
        Transform = new()
        {
            Position = new() { X = 0, Y = 0 },
            Rotation = new() { Angle = 0 },
            Scale = new() { X = 1, Y = 1 }
        },
        Physics = new()
        {
            Position = new() { X = 0, Y = 0 },
            Velocity = new() { X = 5, Y = 0 },
            Collider = new() { Radius = 10 },
            RigidBody = new() { Mass = 2.0f }
        },
        Sprite = new() { TextureId = "hero.png" }
    })
    .Build();
```

**Note**: Nested bundles are flattened into individual components at compile time. There's no runtime overhead.

## Optional Bundle Fields

Mark fields as optional using the `[Optional]` attribute:

```csharp
[Bundle]
public partial struct EnemyBundle
{
    public Position Position;
    public Health Health;
    public Damage Damage;

    [Optional]
    public AI? AI;  // Optional - must be nullable

    [Optional]
    public Loot? Loot;  // Optional - must be nullable
}

// Usage - optional fields can be omitted
var basicEnemy = world.Spawn()
    .With(new EnemyBundle
    {
        Position = new() { X = 50, Y = 0 },
        Health = new() { Current = 50, Max = 50 },
        Damage = new() { Amount = 5 }
        // AI and Loot omitted
    })
    .Build();

var smartEnemy = world.Spawn()
    .With(new EnemyBundle
    {
        Position = new() { X = 100, Y = 0 },
        Health = new() { Current = 100, Max = 100 },
        Damage = new() { Amount = 10 },
        AI = new AI() { BehaviorTree = "smart_enemy" }  // Include AI
    })
    .Build();
```

**Important**: Optional fields must be nullable types (`T?` where `T : struct`). The generator validates this at compile time.

## Working with Bundles

### Adding Bundles to Existing Entities

```csharp
var entity = world.Spawn().Build();

// Add all components from a bundle
world.AddTransformBundle(entity,
    position: new() { X = 0, Y = 0 },
    rotation: new() { Angle = 0 },
    scale: new() { X = 1, Y = 1 }
);
```

### Removing Bundles

```csharp
// Remove all components from a bundle
world.RemoveTransformBundle(entity);

// Entity no longer has Position, Rotation, or Scale
```

### Querying with Bundles

```csharp
// Query all entities with all bundle components
foreach (var entity in world.Query<TransformBundle>())
{
    // This entity has Position, Rotation, and Scale
    var transform = world.GetBundle(entity, default(TransformBundle));

    // Zero-copy access to all components
    transform.Position.X += 10;
    transform.Rotation.Angle += 0.1f;
}

// Mix bundle queries with component queries
foreach (var entity in world.Query<TransformBundle, Velocity>())
{
    var transform = world.GetBundle(entity, default(TransformBundle));
    ref readonly var velocity = ref world.Get<Velocity>(entity);

    transform.Position.X += velocity.X;
    transform.Position.Y += velocity.Y;
}
```

### Bundle-Aware Queries with Filters

```csharp
// Query with bundle + filters
foreach (var entity in world.Query<TransformBundle>()
    .With<EnemyTag>()
    .Without<DisabledTag>())
{
    var transform = world.GetBundle(entity, default(TransformBundle));
    // Process active enemies
}
```

## Component Mixins

Mixins allow component composition at compile time using the `[Mixin]` attribute:

```csharp
[Component]
public partial struct HealthComponent
{
    public int Current;
    public int Max;
}

[Component]
[Mixin(typeof(HealthComponent))]  // Include all fields from HealthComponent
public partial struct RegeneratingHealth
{
    public float RegenRate;
}

// Generated code includes both mixin fields and original fields:
// public partial struct RegeneratingHealth
// {
//     // From HealthComponent mixin
//     public int Current;
//     public int Max;
//
//     // Original field
//     public float RegenRate;
// }

// Usage
var entity = world.Spawn()
    .WithRegeneratingHealth(current: 100, max: 100, regenRate: 5.0f)
    .Build();
```

### Multiple Mixins

```csharp
[Component]
[Mixin(typeof(Position))]
[Mixin(typeof(Rotation))]
public partial struct Transform2D
{
    public float Z;  // Add Z coordinate to Position + Rotation
}

// Generated struct has X, Y (from Position), Angle (from Rotation), and Z
```

### Mixin Validation

The generator validates mixins at compile time:
- Circular mixin references are detected and rejected
- Maximum nesting depth is 5 levels
- Mixins must be valid component types
- Field name conflicts are reported as errors

## Performance Considerations

### Zero Runtime Overhead

Bundles are compile-time constructs. At runtime:
- No bundle objects are created
- No reflection is used
- Components are stored individually in archetypes
- Bundle operations compile to the same code as manual component operations

```csharp
// These two are equivalent at runtime:
world.Spawn().With(new TransformBundle { ... }).Build();
world.Spawn().With(position).With(rotation).With(scale).Build();
```

### Archetype Pre-allocation

Bundles automatically register their component combinations for archetype pre-allocation:

```csharp
// During World initialization, bundles hint the archetype manager
// to pre-allocate storage for common component combinations
// This reduces archetype migrations when spawning entities
```

Pre-allocation happens automatically - no manual configuration needed.

### When to Use Bundles

| Use Bundles When | Use Manual Components When |
|------------------|---------------------------|
| Spawning many entities with the same components | Adding components one-at-a-time based on conditions |
| Common component groups (Transform, Physics) | Rare or unique component combinations |
| You want convenient spawning syntax | Maximum flexibility is needed |
| Building entity templates | Components are added dynamically |

## Best Practices

### Keep Bundles Focused

```csharp
// ✅ Good: Small, focused bundles
[Bundle]
public partial struct TransformBundle
{
    public Position Position;
    public Rotation Rotation;
    public Scale Scale;
}

// ❌ Bad: Bundles with unrelated components
[Bundle]
public partial struct EverythingBundle
{
    public Position Position;
    public Health Health;
    public SpriteRenderer Sprite;
    public InputState Input;
    public NetworkId NetworkId;
    // Too many responsibilities!
}
```

### Use Nested Bundles for Composition

```csharp
// ✅ Good: Compose larger bundles from smaller ones
[Bundle]
public partial struct CharacterBundle
{
    public TransformBundle Transform;  // Reuse existing bundle
    public CombatBundle Combat;        // Reuse existing bundle
    public SpriteRenderer Sprite;
}
```

### Name Bundles Descriptively

```csharp
// ✅ Good: Clear, descriptive names
TransformBundle
PhysicsBundle
CharacterBundle
EnemyBundle

// ❌ Bad: Vague names
DataBundle
EntityStuff
ComponentGroup
```

### Don't Over-Bundle

Not every component combination needs a bundle. Only create bundles for frequently-used patterns.

```csharp
// If you only spawn one or two entities with these components,
// a bundle might be overkill - just use .With() directly
```

## Bundles vs Prefabs

Bundles and prefabs serve different purposes:

| Bundles | Prefabs |
|---------|---------|
| Compile-time component grouping | Runtime entity templates |
| Type-safe component combinations | Data-driven entity spawning |
| No default values | Can have default component values |
| Generates code | Registered at runtime |
| Use for common patterns | Use for content creation |

You can combine both:

```csharp
[Prefab("Characters/Goblin")]
[Bundle]
public partial struct GoblinPrefab
{
    public TransformBundle Transform;
    public EnemyBundle Enemy;
    public AI AI;
}
```

## Common Patterns

### Factory Methods with Bundles

```csharp
public static class EntityFactory
{
    public static Entity SpawnEnemy(World world, float x, float y, int health)
    {
        return world.Spawn()
            .With(new TransformBundle
            {
                Position = new() { X = x, Y = y },
                Rotation = new() { Angle = 0 },
                Scale = new() { X = 1, Y = 1 }
            })
            .With(new ActorBundle
            {
                Health = new() { Current = health, Max = health },
                Damage = new() { Amount = 10 },
                Name = new() { Value = "Enemy" }
            })
            .WithTag<EnemyTag>()
            .Build();
    }
}
```

### Bundle-Based Systems

```csharp
public class TransformSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Process all entities with TransformBundle
        foreach (var entity in World.Query<TransformBundle>())
        {
            var transform = World.GetBundle(entity, default(TransformBundle));

            // Apply transformations
            transform.Position.X += transform.Rotation.Angle * deltaTime;
        }
    }
}
```

### Conditional Bundle Fields

```csharp
[Bundle]
public partial struct SpawnableBundle
{
    public Position Position;

    [Optional]
    public Velocity? Velocity;  // Only if entity should move

    [Optional]
    public Health? Health;  // Only if entity is damageable
}

// Spawn static decoration (no velocity or health)
world.Spawn().With(new SpawnableBundle
{
    Position = new() { X = 0, Y = 0 }
}).Build();

// Spawn moving, damageable entity
world.Spawn().With(new SpawnableBundle
{
    Position = new() { X = 0, Y = 0 },
    Velocity = new Velocity() { X = 5, Y = 0 },
    Health = new Health() { Current = 100, Max = 100 }
}).Build();
```

## Troubleshooting

### Compiler Error: "Bundle must be a struct"

```csharp
// ❌ Error
[Bundle]
public partial class TransformBundle { }  // Classes not allowed

// ✅ Fix
[Bundle]
public partial struct TransformBundle { }
```

### Compiler Error: "Bundle field must be a component type"

```csharp
// ❌ Error
[Bundle]
public partial struct MyBundle
{
    public string Name;  // string is not IComponent
}

// ✅ Fix: Use a component wrapper
[Component]
public partial struct Named { public string Value; }

[Bundle]
public partial struct MyBundle
{
    public Named Name;  // Now it's a component
}
```

### Compiler Error: "Circular bundle reference detected"

```csharp
// ❌ Error
[Bundle]
public partial struct BundleA
{
    public BundleB B;
}

[Bundle]
public partial struct BundleB
{
    public BundleA A;  // Circular reference!
}

// ✅ Fix: Remove circular dependency
[Bundle]
public partial struct BundleA
{
    public Position Position;
}

[Bundle]
public partial struct BundleB
{
    public BundleA A;  // One-way reference is fine
    public Velocity Velocity;
}
```

### Compiler Error: "Optional field must be nullable"

```csharp
// ❌ Error
[Bundle]
public partial struct MyBundle
{
    [Optional]
    public Position Position;  // Not nullable
}

// ✅ Fix
[Bundle]
public partial struct MyBundle
{
    [Optional]
    public Position? Position;  // Nullable struct
}
```

## Next Steps

- [Components Guide](components.md) - Component design patterns
- [Prefabs Guide](prefabs.md) - Entity templates
- [Queries Guide](queries.md) - Querying entities
- [Getting Started](getting-started.md) - Basic ECS usage
