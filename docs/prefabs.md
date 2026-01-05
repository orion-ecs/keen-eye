# Prefabs

Prefabs are reusable entity templates that define a set of components. They allow you to define entity archetypes once and instantiate them multiple times with consistent component configurations.

> **Deprecation Notice**: The runtime prefab API (`EntityPrefab`, `world.RegisterPrefab()`, `world.SpawnFromPrefab()`) is deprecated. Use `.keprefab` files with source-generated spawn methods instead. See [Migration Guide](#migration-guide) below.

## Source-Generated Prefabs (Recommended)

The recommended approach is to define prefabs in `.keprefab` JSON files. These are processed at compile-time by the SceneGenerator to produce type-safe spawn methods.

### Creating a .keprefab File

Create a JSON file with the `.keprefab` extension in your project:

```json
// Prefabs/Enemy.keprefab
{
  "name": "Enemy",
  "version": 1,
  "root": {
    "id": "enemy",
    "name": "Enemy",
    "components": {
      "MyGame.Position": { "X": 0, "Y": 0 },
      "MyGame.Health": { "Current": 100, "Max": 100 },
      "MyGame.Sprite": { "TextureId": "enemy.png", "Layer": 1 },
      "MyGame.EnemyTag": {}
    }
  },
  "overridableFields": ["MyGame.Position.X", "MyGame.Position.Y"]
}
```

### Project Configuration

Add the prefab files to your project as `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Prefabs\*.keprefab" />
</ItemGroup>
```

### Using Generated Spawn Methods

The generator creates a `Scenes` class with spawn methods for each prefab:

```csharp
// Spawn with default values
var enemy = Scenes.SpawnEnemy(world);

// Spawn with override parameters (from overridableFields)
var enemy = Scenes.SpawnEnemy(world,
    myGamePositionX: 100,
    myGamePositionY: 50);
```

### Benefits of Source-Generated Prefabs

- **Compile-time validation**: Component types and field names are verified at build time
- **Type-safe API**: IDE autocomplete for spawn methods and parameters
- **Zero runtime overhead**: No registration or string lookups at runtime
- **Overridable fields**: Become typed optional parameters on spawn methods

### Prefab File Schema

```json
{
  "$schema": "https://keeneyes.dev/schemas/prefab-v1.json",
  "name": "PrefabName",
  "version": 1,
  "root": {
    "id": "unique-id",
    "name": "EntityName",
    "components": {
      "Namespace.ComponentType": {
        "FieldName": value
      }
    },
    "children": []
  },
  "children": [],
  "overridableFields": ["Namespace.Component.Field"]
}
```

### Listing Available Prefabs

The generated `Scenes` class provides a list of all prefab names:

```csharp
foreach (var prefabName in Scenes.All)
{
    Console.WriteLine($"Available: {prefabName}");
}
```

---

## Migration Guide

### Step 1: Create .keprefab Files

For each runtime prefab, create a corresponding `.keprefab` file:

**Before (deprecated):**
```csharp
var enemyPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .WithTag<EnemyTag>();

world.RegisterPrefab("Enemy", enemyPrefab);
```

**After (recommended):**
```json
// Prefabs/Enemy.keprefab
{
  "name": "Enemy",
  "root": {
    "id": "enemy",
    "components": {
      "MyGame.Position": { "X": 0, "Y": 0 },
      "MyGame.Health": { "Current": 100, "Max": 100 },
      "MyGame.EnemyTag": {}
    }
  }
}
```

### Step 2: Update Project File

Add the prefab files to your project:

```xml
<ItemGroup>
  <AdditionalFiles Include="Prefabs\*.keprefab" />
</ItemGroup>
```

### Step 3: Replace Spawn Calls

**Before (deprecated):**
```csharp
var enemy = world.SpawnFromPrefab("Enemy").Build();
var enemy = world.SpawnFromPrefab("Enemy")
    .With(new Position { X = 100, Y = 50 })
    .Build();
```

**After (recommended):**
```csharp
var enemy = Scenes.SpawnEnemy(world);
var enemy = Scenes.SpawnEnemy(world,
    myGamePositionX: 100,
    myGamePositionY: 50);
```

### Step 4: Remove Runtime Registration

Delete all `world.RegisterPrefab()` calls and `EntityPrefab` definitions.

### Inheritance Note

Prefab inheritance (`base` field in .keprefab) is not yet implemented. For prefabs that used `Extends()`, flatten the component definitions into each prefab file.

---

## Runtime Prefabs (Deprecated)

> **Warning**: This API is deprecated and will be removed in a future version. Use [Source-Generated Prefabs](#source-generated-prefabs-recommended) instead.

### Defining a Prefab

```csharp
#pragma warning disable CS0618 // Suppress deprecation warning

var enemyPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .With(new Velocity { X = 0, Y = 0 })
    .WithTag<EnemyTag>();
```

### Registering a Prefab

```csharp
world.RegisterPrefab("Enemy", enemyPrefab);
```

### Spawning from a Prefab

```csharp
// Spawn with default prefab values
var enemy1 = world.SpawnFromPrefab("Enemy").Build();

// Spawn with overridden values
var enemy2 = world.SpawnFromPrefab("Enemy")
    .With(new Position { X = 100, Y = 50 })
    .Build();
```

### Prefab Inheritance (Deprecated)

```csharp
// Base enemy prefab
var baseEnemyPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .WithTag<EnemyTag>();

world.RegisterPrefab("Enemy", baseEnemyPrefab);

// Flying enemy extends base enemy
var flyingEnemyPrefab = new EntityPrefab()
    .Extends("Enemy")
    .With(new Velocity { X = 0, Y = -5 })
    .WithTag<FlyingTag>();

world.RegisterPrefab("FlyingEnemy", flyingEnemyPrefab);
```

### Prefab Management (Deprecated)

```csharp
// Check if registered
if (world.HasPrefab("Enemy"))
{
    var enemy = world.SpawnFromPrefab("Enemy").Build();
}

// Unregister
world.UnregisterPrefab("Enemy");

// List all prefabs
foreach (var prefabName in world.GetAllPrefabNames())
{
    Console.WriteLine($"Registered: {prefabName}");
}

#pragma warning restore CS0618
```

## Performance Considerations

**Source-generated prefabs** have zero runtime overhead - all work is done at compile time.

**Runtime prefabs** (deprecated) have the following characteristics:
- Registration: O(1)
- Spawning: O(C * D) where C is total components and D is inheritance depth
- Inheritance resolution happens at spawn time

For performance-critical scenarios, source-generated prefabs are always preferred.
