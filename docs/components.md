# Components Guide

Components are pure data containers that attach to entities. This guide covers component design patterns and best practices.

## Defining Components

Components are structs that implement `IComponent`:

```csharp
using KeenEyes;

public struct Position : IComponent
{
    public float X;
    public float Y;
}

public struct Velocity : IComponent
{
    public float X;
    public float Y;
}
```

### Why Structs?

Components must be structs (value types) because:

1. **Cache-friendly**: Structs are stored contiguously in memory
2. **No heap allocation**: Creating components doesn't allocate
3. **Value semantics**: Clear ownership, no reference sharing bugs
4. **Blittable**: Can be copied efficiently with memcpy

## Tag Components

Tag components have no data - they just mark entities:

```csharp
public struct Player : ITagComponent { }
public struct Enemy : ITagComponent { }
public struct Disabled : ITagComponent { }
public struct Poisoned : ITagComponent { }
```

Use tags for:
- Entity categorization (`Player`, `Enemy`, `NPC`)
- State flags (`Disabled`, `Invulnerable`, `Dead`)
- Query filtering (`Renderable`, `PhysicsEnabled`)

```csharp
// Add a tag
world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .WithTag<Player>()
    .Build();

// Query with tags
foreach (var entity in world.Query<Position>().With<Player>())
{
    // Only player entities
}
```

## Component Design Patterns

### Small and Focused

Each component should have one responsibility:

```csharp
// ❌ Bad: God component with everything
public struct Actor : IComponent
{
    public float X, Y;
    public float VelX, VelY;
    public int Health, MaxHealth;
    public string Name;
    public int Gold;
    public int Experience;
    public int Level;
}

// ✅ Good: Small, focused components
public struct Position : IComponent { public float X, Y; }
public struct Velocity : IComponent { public float X, Y; }
public struct Health : IComponent { public int Current, Max; }
public struct Named : IComponent { public string Name; }
public struct Wallet : IComponent { public int Gold; }
public struct Experience : IComponent { public int Xp, Level; }
```

### Data Only, No Logic

Components should never contain methods:

```csharp
// ❌ Bad: Logic in component
public struct Health : IComponent
{
    public int Current;
    public int Max;

    public void TakeDamage(int amount) => Current -= amount;  // NO!
    public bool IsDead => Current <= 0;  // Computed properties are okay
}

// ✅ Good: Pure data
public struct Health : IComponent
{
    public int Current;
    public int Max;
}

// Logic belongs in systems
public class DamageSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Process damage events and modify Health components
    }
}
```

### Prefer Primitives

Use simple types when possible:

```csharp
// ❌ Avoid: Nested complex types
public struct Transform : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}

// ✅ Better: Flat structure
public struct Position : IComponent { public float X, Y, Z; }
public struct Rotation : IComponent { public float X, Y, Z, W; }
public struct Scale : IComponent { public float X, Y, Z; }
```

### Entity References

Store entity references for relationships:

```csharp
public struct Parent : IComponent
{
    public Entity Entity;
}

public struct Target : IComponent
{
    public Entity Entity;
}

// Usage
var child = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Parent { Entity = parentEntity })
    .Build();

// Always validate references before use
if (world.IsAlive(target.Entity))
{
    ref var targetPos = ref world.Get<Position>(target.Entity);
}
```

## Source Generator Attributes

Use `[Component]` to generate fluent builder methods:

```csharp
using KeenEyes.Generators.Attributes;

[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

[Component]
public partial struct Health
{
    public int Current;
    public int Max;
    public bool Invulnerable;
}
```

Generated methods:

```csharp
// Generated: WithPosition(float x, float y)
world.Spawn()
    .WithPosition(x: 10, y: 20)
    .Build();

// Generated: WithHealth(int current, int max, bool invulnerable = false)
world.Spawn()
    .WithHealth(current: 100, max: 100)
    .WithHealth(current: 50, max: 50, invulnerable: true)
    .Build();
```

### Tag Component Attribute

```csharp
[TagComponent]
public partial struct Player { }

// Generated: WithPlayer()
world.Spawn().WithPlayer().Build();
```

## Component Access Patterns

### Mutable Access

```csharp
ref var pos = ref world.Get<Position>(entity);
pos.X += 10;
pos.Y += 5;
```

### Read-Only Access

```csharp
ref readonly var vel = ref world.Get<Velocity>(entity);
float speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
// vel.X = 0;  // Compile error - readonly
```

### Safe Access

```csharp
if (world.Has<Velocity>(entity))
{
    ref var vel = ref world.Get<Velocity>(entity);
    // Use velocity...
}
```

## Component Lifecycle

### Adding Components

```csharp
// At spawn time
var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// After spawn
world.Add(entity, new Velocity { X = 1, Y = 0 });
```

### Modifying Components

```csharp
// Direct modification via ref
ref var pos = ref world.Get<Position>(entity);
pos.X = 100;

// Replace entire component
world.Set(entity, new Position { X = 100, Y = 200 });
```

### Removing Components

```csharp
world.Remove<Velocity>(entity);

// Check before accessing
if (!world.Has<Velocity>(entity))
{
    // Component was removed
}
```

## Archetype Implications

Adding or removing components changes an entity's archetype:

```csharp
// Entity moves from [Position] archetype to [Position, Velocity]
world.Add(entity, new Velocity { X = 1, Y = 0 });

// Entity moves from [Position, Velocity] archetype to [Position]
world.Remove<Velocity>(entity);
```

**Performance note**: Archetype changes involve copying component data. Avoid frequent add/remove in hot paths. Consider using tag components for state that changes often.

## Best Practices Summary

| Do | Don't |
|---|---|
| Keep components small and focused | Create "god components" with many fields |
| Use structs for components | Use classes for components |
| Store pure data | Put logic in components |
| Use tags for categorization | Use boolean flags in components |
| Use entity references for relationships | Store object references |
| Validate entity references | Assume references are valid |

## Next Steps

- [Queries Guide](queries.md) - Filtering entities by components
- [Systems Guide](systems.md) - Processing component data
- [Entities Guide](entities.md) - Entity lifecycle
