# Queries Guide

Queries are how you find and iterate over entities with specific components. This guide covers query patterns and optimization.

## Basic Queries

Query entities by their components:

```csharp
// All entities with Position
foreach (var entity in world.Query<Position>())
{
    ref var pos = ref world.Get<Position>(entity);
    Console.WriteLine($"{entity}: ({pos.X}, {pos.Y})");
}

// Entities with Position AND Velocity
foreach (var entity in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(entity);
    ref readonly var vel = ref world.Get<Velocity>(entity);

    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

## Multi-Component Queries

Query up to 4 component types directly:

```csharp
// Two components
foreach (var entity in world.Query<Position, Velocity>())
{ }

// Three components
foreach (var entity in world.Query<Position, Velocity, Health>())
{ }

// Four components
foreach (var entity in world.Query<Position, Velocity, Health, Damage>())
{ }
```

## Query Filters

### With Filter

Include entities that also have specific components:

```csharp
// Entities with Position AND Velocity AND the Player tag
foreach (var entity in world.Query<Position, Velocity>().With<Player>())
{
    // Only player entities
}

// Chain multiple With filters
foreach (var entity in world.Query<Position>().With<Enemy>().With<Visible>())
{
    // Visible enemies only
}
```

### Without Filter

Exclude entities that have specific components:

```csharp
// Entities with Position but NOT Disabled
foreach (var entity in world.Query<Position>().Without<Disabled>())
{
    // Skip disabled entities
}

// Chain multiple Without filters
foreach (var entity in world.Query<Position>().Without<Disabled>().Without<Dead>())
{
    // Active, living entities only
}
```

### Combining Filters

```csharp
// Complex filter: Enemies that are active (not disabled, not dead)
foreach (var entity in world.Query<Position, Velocity>()
    .With<Enemy>()
    .Without<Disabled>()
    .Without<Dead>())
{
    // Active enemy entities
}
```

## Query Results

Queries return an enumerable of `Entity`:

```csharp
var enemies = world.Query<Position>().With<Enemy>();

// Count matching entities
int count = enemies.Count();

// Convert to list
var list = enemies.ToList();

// Check if any match
bool hasEnemies = enemies.Any();
```

## Component Access in Queries

### Mutable Access

```csharp
foreach (var entity in world.Query<Position, Velocity>())
{
    // Mutable - can modify
    ref var pos = ref world.Get<Position>(entity);
    pos.X += 1;
}
```

### Read-Only Access

```csharp
foreach (var entity in world.Query<Position, Velocity>())
{
    // Read-only - prevents accidental modification
    ref readonly var vel = ref world.Get<Velocity>(entity);
    float speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
}
```

## Query Patterns

### Processing All Entities of a Type

```csharp
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}
```

### Filtering by Tag

```csharp
// Players only
foreach (var entity in World.Query<Position>().With<Player>())
{
    // Handle player movement
}

// Enemies only
foreach (var entity in World.Query<Position>().With<Enemy>())
{
    // Handle enemy AI
}
```

### Skipping Disabled Entities

```csharp
foreach (var entity in World.Query<Position, Velocity>().Without<Disabled>())
{
    // Skip entities marked as disabled
}
```

### Finding Single Entities

```csharp
// Get first matching entity (or Entity.Null if none)
var player = World.Query<Position>().With<Player>().FirstOrDefault();

if (player.IsValid && World.IsAlive(player))
{
    ref var pos = ref World.Get<Position>(player);
}
```

## Query Caching

KeenEyes automatically caches query results for performance:

- First query execution computes matching archetypes
- Subsequent queries with the same filters return cached results
- Cache is automatically invalidated when archetypes change

You don't need to manually manage query caching - it happens automatically.

## Performance Tips

### Prefer Specific Queries

```csharp
// ❌ Less efficient: Query all, then filter manually
foreach (var entity in world.Query<Position>())
{
    if (world.Has<Velocity>(entity) && world.Has<Enemy>(entity))
    {
        // ...
    }
}

// ✅ More efficient: Use query filters
foreach (var entity in world.Query<Position, Velocity>().With<Enemy>())
{
    // ...
}
```

### Avoid Queries in Hot Loops

```csharp
// ❌ Creates query object each frame
public override void Update(float deltaTime)
{
    foreach (var entity in World.Query<Position, Velocity>())
    {
        // Inner loop - avoid querying here
        foreach (var other in World.Query<Position>().With<Enemy>())
        {
            // O(n*m) queries!
        }
    }
}

// ✅ Better: Collect entities first, then process
public override void Update(float deltaTime)
{
    var enemies = World.Query<Position>().With<Enemy>().ToList();

    foreach (var entity in World.Query<Position, Velocity>())
    {
        foreach (var enemy in enemies)
        {
            // Reuse collected list
        }
    }
}
```

### Use Tags for Fast Filtering

Tags are zero-size and don't store data, making them efficient for filtering:

```csharp
// Efficient: Tag-based filtering
foreach (var entity in world.Query<Position>().With<Renderable>())
{
    // Render entity
}
```

## Common Patterns

### Player Input System

```csharp
foreach (var entity in World.Query<Position, Velocity>().With<Player>())
{
    ref var vel = ref World.Get<Velocity>(entity);

    // Handle input - only affects players
    if (Input.IsKeyDown(Keys.Right)) vel.X = 5;
    if (Input.IsKeyDown(Keys.Left)) vel.X = -5;
}
```

### Collision Detection

```csharp
var collidables = World.Query<Position, Collider>().ToList();

for (int i = 0; i < collidables.Count; i++)
{
    for (int j = i + 1; j < collidables.Count; j++)
    {
        var a = collidables[i];
        var b = collidables[j];

        ref readonly var posA = ref World.Get<Position>(a);
        ref readonly var posB = ref World.Get<Position>(b);

        if (CheckCollision(posA, posB))
        {
            // Handle collision
        }
    }
}
```

### Cleanup System

```csharp
var buffer = new CommandBuffer();

foreach (var entity in World.Query<Health>())
{
    ref readonly var health = ref World.Get<Health>(entity);
    if (health.Current <= 0)
    {
        buffer.Despawn(entity);
    }
}

buffer.Flush(World);
```

## Next Steps

- [Systems Guide](systems.md) - Using queries in systems
- [Command Buffer](command-buffer.md) - Safe entity modification during queries
- [Components Guide](components.md) - Component design for efficient queries
