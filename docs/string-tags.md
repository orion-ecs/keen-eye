# String Tags

String tags provide runtime-flexible entity tagging that complements the type-safe tag components. Unlike `ITagComponent` types which require compile-time definitions, string tags can be assigned dynamically at runtime.

## When to Use String Tags

Use string tags for scenarios requiring runtime flexibility:

- **Designer-driven content tagging** - Tags defined in data files or editors
- **Serialization** - Human-readable tags that persist naturally to JSON
- **Dynamic categorization** - Tags determined by game logic at runtime
- **Editor tooling** - Filtering and grouping entities in development tools
- **Debugging** - Quick categorization without creating new types

Use type-safe tag components (`ITagComponent`) when:

- Tags are known at compile time
- You want compile-time type checking
- Tags participate in archetype-based queries for maximum performance

## Basic Usage

### Adding Tags

```csharp
var enemy = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// Add tags after creation
world.AddTag(enemy, "Enemy");
world.AddTag(enemy, "Hostile");
world.AddTag(enemy, "Boss");
```

### Adding Tags During Entity Creation

```csharp
var enemy = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .WithTag("Enemy")      // String tag
    .WithTag("Hostile")
    .WithTag<EnemyTag>()   // Type-safe tag (different overload)
    .Build();
```

### Checking Tags

```csharp
if (world.HasTag(entity, "Boss"))
{
    // Special boss handling
}

// Safe with stale entities - returns false instead of throwing
if (world.HasTag(possiblyDeadEntity, "Player"))
{
    // Only executed if entity is alive AND has the tag
}
```

### Removing Tags

```csharp
// Remove a single tag
world.RemoveTag(entity, "Hostile");

// Tags are automatically removed when entity is despawned
world.Despawn(entity);
```

### Getting All Tags

```csharp
var tags = world.GetTags(entity);
Console.WriteLine($"Entity has {tags.Count} tags:");
foreach (var tag in tags)
{
    Console.WriteLine($"  - {tag}");
}
```

## Querying by Tag

### Simple Tag Query

```csharp
// Get all entities with a specific tag
foreach (var entity in world.QueryByTag("Enemy"))
{
    ref var pos = ref world.Get<Position>(entity);
    // Process enemy
}

// Count entities with a tag
int playerCount = world.QueryByTag("Player").Count();
```

### Combining with Component Queries

String tags integrate with the fluent query API:

```csharp
// Entities with Position AND "Enemy" tag
foreach (var entity in world.Query<Position>().WithTag("Enemy"))
{
    ref var pos = ref world.Get<Position>(entity);
    // Process positioned enemies
}

// Entities with Position, WITHOUT "Frozen" tag
foreach (var entity in world.Query<Position, Velocity>().WithoutTag("Frozen"))
{
    // Process moving entities that aren't frozen
}

// Multiple tag filters
foreach (var entity in world.Query<Position>()
    .WithTag("Enemy")
    .WithTag("Active")
    .WithoutTag("Dead"))
{
    // Process active, living enemies
}
```

## Use Cases

### Dynamic State Tagging

```csharp
public class StatusSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref var health = ref World.Get<Health>(entity);

            // Add/remove tags based on state
            if (health.Current <= health.Max * 0.25f)
            {
                World.AddTag(entity, "LowHealth");
            }
            else
            {
                World.RemoveTag(entity, "LowHealth");
            }
        }
    }
}

// Other systems can react to the tag
public class AISystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Enemies flee when low on health
        foreach (var entity in World.Query<Position, Velocity>()
            .WithTag("Enemy")
            .WithTag("LowHealth"))
        {
            // Flee behavior
        }
    }
}
```

### Data-Driven Tagging

```csharp
// Load tags from configuration
var entityConfig = JsonSerializer.Deserialize<EntityConfig>(json);

var entity = world.Spawn()
    .With(new Position { X = entityConfig.X, Y = entityConfig.Y })
    .Build();

// Apply tags from data
foreach (var tag in entityConfig.Tags)
{
    world.AddTag(entity, tag);
}
```

### Editor Integration

```csharp
// In an editor, allow designers to add arbitrary tags
public void OnTagAdded(Entity entity, string newTag)
{
    world.AddTag(entity, newTag);
    RefreshEntityInspector(entity);
}

// Filter entities in editor by tag
public IEnumerable<Entity> GetEntitiesByEditorFilter(string tagFilter)
{
    return world.QueryByTag(tagFilter);
}
```

### Debugging and Diagnostics

```csharp
// Tag entities during debugging
world.AddTag(suspiciousEntity, "DEBUG_Investigate");

// Find tagged entities later
foreach (var entity in world.QueryByTag("DEBUG_Investigate"))
{
    LogEntityState(entity);
}
```

## Tag Validation

Tags must be non-empty strings:

```csharp
// These throw ArgumentNullException
world.AddTag(entity, null!);

// These throw ArgumentException
world.AddTag(entity, "");
world.AddTag(entity, "   ");
```

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|------------|-------|
| `AddTag` | O(1) | Hash set insertion |
| `RemoveTag` | O(1) | Hash set removal |
| `HasTag` | O(1) | Hash set lookup |
| `GetTags` | O(1) | Returns existing collection |
| `QueryByTag` | O(N) | N = entities with that tag |

String tags use dual indexing:
- **Entity → Tags**: O(1) lookup of tags for an entity
- **Tag → Entities**: O(1) lookup of entities with a tag

### Performance Tips

1. **Prefer type-safe tags for hot paths**: Component-based tags benefit from archetype-based iteration
2. **Reuse tag strings**: Consider using constants for frequently-used tags
3. **Avoid excessive tag changes**: Each add/remove updates two indexes

```csharp
// Good - use constants for common tags
public static class Tags
{
    public const string Enemy = "Enemy";
    public const string Player = "Player";
    public const string Active = "Active";
}

world.AddTag(entity, Tags.Enemy);
```

## Comparison: String Tags vs Component Tags

| Aspect | String Tags | Component Tags |
|--------|-------------|----------------|
| Definition | Runtime strings | Compile-time types |
| Type safety | None | Full |
| Performance | Good (hash-based) | Best (archetype-based) |
| Serialization | Natural | Requires type resolution |
| Dynamic creation | Yes | No (requires code) |
| Query integration | Yes | Yes |
| IDE support | None | Autocomplete, refactoring |

## Example: Hybrid Approach

Combine both tagging systems for flexibility:

```csharp
// Type-safe tags for core game mechanics
[TagComponent]
public partial struct EnemyTag { }

[TagComponent]
public partial struct PlayerTag { }

// String tags for dynamic/editor-driven features
var boss = world.Spawn()
    .With(new Position())
    .With(new Health { Current = 500, Max = 500 })
    .WithTag<EnemyTag>()           // Core gameplay tag
    .WithTag("Boss")                // Difficulty tier
    .WithTag("FireElemental")       // Specific type
    .WithTag("DropsTreasure")       // Loot table flag
    .Build();

// Query using component tag (most efficient)
foreach (var entity in world.Query<Position>().With<EnemyTag>())
{
    // All enemies
}

// Filter further with string tags
foreach (var entity in world.Query<Position>().With<EnemyTag>().WithTag("Boss"))
{
    // Just bosses
}
```
