# Prefabs

Prefabs are reusable entity templates that define a set of components. They allow you to define entity archetypes once and instantiate them multiple times with consistent component configurations.

Prefabs are defined in `.keprefab` JSON files and processed at compile-time by the SceneGenerator to produce type-safe, source-generated spawn methods.

## Creating a .keprefab File

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

## Project Configuration

Add the prefab files to your project as `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Prefabs\*.keprefab" />
</ItemGroup>
```

## Using Generated Spawn Methods

The generator creates a `Scenes` class with spawn methods for each prefab:

```csharp
// Spawn with default values
var enemy = Scenes.SpawnEnemy(world);

// Spawn with override parameters (from overridableFields)
var enemy = Scenes.SpawnEnemy(world,
    myGamePositionX: 100,
    myGamePositionY: 50);
```

## Benefits of Source-Generated Prefabs

- **Compile-time validation**: Component types and field names are verified at build time
- **Type-safe API**: IDE autocomplete for spawn methods and parameters
- **Zero runtime overhead**: No registration or string lookups at runtime
- **Overridable fields**: Become typed optional parameters on spawn methods

## Prefab File Schema

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

## Listing Available Prefabs

The generated `Scenes` class provides a list of all prefab names:

```csharp
foreach (var prefabName in Scenes.All)
{
    Console.WriteLine($"Available: {prefabName}");
}
```

## Inheritance Note

Prefab inheritance (`base` field in .keprefab) is not yet implemented. Flatten the component definitions into each prefab file.

## Performance Considerations

Source-generated prefabs have zero runtime overhead — all work is done at compile time. There is no registration step and no string lookups when spawning.
