# Prefabs

Prefabs are reusable entity templates that define a set of components. They allow you to define entity archetypes once and instantiate them multiple times with consistent component configurations.

## Basic Usage

### Defining a Prefab

Create a prefab using the fluent builder pattern:

```csharp
var enemyPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .With(new Velocity { X = 0, Y = 0 })
    .WithTag<EnemyTag>();
```

### Registering a Prefab

Register prefabs with the world using a unique name:

```csharp
world.RegisterPrefab("Enemy", enemyPrefab);
```

### Spawning from a Prefab

Create entities from registered prefabs:

```csharp
// Spawn with default prefab values
var enemy1 = world.SpawnFromPrefab("Enemy").Build();

// Spawn multiple instances
var enemy2 = world.SpawnFromPrefab("Enemy").Build();
var enemy3 = world.SpawnFromPrefab("Enemy").Build();
```

### Customizing Spawned Entities

Override prefab components when spawning:

```csharp
// Spawn with custom position
var enemy = world.SpawnFromPrefab("Enemy")
    .With(new Position { X = 100, Y = 50 })
    .Build();

// Add additional components not in the prefab
var bossEnemy = world.SpawnFromPrefab("Enemy")
    .With(new BossMarker())
    .With(new Health { Current = 500, Max = 500 })  // Override health
    .Build();
```

## Prefab Inheritance

Prefabs support inheritance, allowing derived prefabs to extend or override base prefab components.

### Creating Derived Prefabs

```csharp
// Base enemy prefab
var baseEnemyPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .WithTag<EnemyTag>();

world.RegisterPrefab("Enemy", baseEnemyPrefab);

// Flying enemy extends base enemy
var flyingEnemyPrefab = new EntityPrefab()
    .Extends("Enemy")                              // Inherit from base
    .With(new Velocity { X = 0, Y = -5 })          // Add new component
    .WithTag<FlyingTag>();                         // Add new tag

world.RegisterPrefab("FlyingEnemy", flyingEnemyPrefab);

// Boss enemy extends base with overridden health
var bossEnemyPrefab = new EntityPrefab()
    .Extends("Enemy")
    .With(new Health { Current = 500, Max = 500 }) // Override base health
    .WithTag<BossTag>();

world.RegisterPrefab("BossEnemy", bossEnemyPrefab);
```

### Inheritance Rules

1. **Component Merging**: Derived prefabs include all components from the base prefab
2. **Component Override**: Components of the same type in the derived prefab replace base components
3. **Tag Accumulation**: Tags from both base and derived prefabs are included
4. **Resolution Order**: Base prefabs are processed first, then derived prefab components are applied

## Named Entity Spawning

Spawn named entities from prefabs for later retrieval:

```csharp
// Spawn a named entity from a prefab
var player = world.SpawnFromPrefab("Player", "MainPlayer").Build();

// Later, retrieve by name
var foundPlayer = world.GetEntityByName("MainPlayer");
if (foundPlayer.IsValid)
{
    ref var pos = ref world.Get<Position>(foundPlayer);
    // Work with the player entity
}
```

## Prefab Management

### Checking Prefab Registration

```csharp
if (world.HasPrefab("Enemy"))
{
    var enemy = world.SpawnFromPrefab("Enemy").Build();
}
```

### Unregistering Prefabs

```csharp
// Remove a prefab (existing entities are unaffected)
bool removed = world.UnregisterPrefab("Enemy");
```

### Listing All Prefabs

```csharp
foreach (var prefabName in world.GetAllPrefabNames())
{
    Console.WriteLine($"Registered prefab: {prefabName}");
}
```

## Use Cases

### Game Entity Templates

```csharp
// Define common game entities
var playerPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .With(new Inventory { Slots = 20 })
    .WithTag<PlayerTag>();

var itemPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new ItemData { Value = 10 })
    .WithTag<PickupableTag>();

world.RegisterPrefab("Player", playerPrefab);
world.RegisterPrefab("Item", itemPrefab);
```

### Level Loading

```csharp
// Load level data and spawn entities from prefabs
foreach (var entityData in levelData.Entities)
{
    world.SpawnFromPrefab(entityData.PrefabName)
        .With(new Position { X = entityData.X, Y = entityData.Y })
        .Build();
}
```

### Pooling Patterns

```csharp
// Pre-register prefabs at startup
void InitializePrefabs(World world)
{
    world.RegisterPrefab("Bullet", new EntityPrefab()
        .With(new Position())
        .With(new Velocity())
        .With(new Damage { Amount = 10 })
        .WithTag<BulletTag>());

    world.RegisterPrefab("Explosion", new EntityPrefab()
        .With(new Position())
        .With(new ParticleEffect { Type = EffectType.Explosion })
        .With(new Lifetime { Remaining = 1.0f }));
}

// Spawn from prefabs during gameplay
Entity SpawnBullet(World world, Position pos, Velocity vel)
{
    return world.SpawnFromPrefab("Bullet")
        .With(pos)
        .With(vel)
        .Build();
}
```

## Error Handling

### Missing Prefab

```csharp
try
{
    var entity = world.SpawnFromPrefab("NonExistent").Build();
}
catch (InvalidOperationException ex)
{
    // "No prefab registered with name 'NonExistent'."
}
```

### Circular Inheritance

```csharp
// This will throw when spawning
var prefabA = new EntityPrefab().Extends("B");
var prefabB = new EntityPrefab().Extends("A");

world.RegisterPrefab("A", prefabA);
world.RegisterPrefab("B", prefabB);

try
{
    world.SpawnFromPrefab("A").Build(); // Throws InvalidOperationException
}
catch (InvalidOperationException ex)
{
    // "Circular inheritance detected in prefab 'A'..."
}
```

## Performance Considerations

- **Registration**: O(1) - Prefabs are stored by name in a dictionary
- **Spawning**: O(C * D) where C is total components and D is inheritance depth
- **Inheritance Resolution**: Resolved at spawn time, not registration time

Prefabs are designed for convenience and maintainability rather than hot-path performance. For performance-critical spawning scenarios with thousands of entities per frame, consider direct entity creation with `world.Spawn()`.
