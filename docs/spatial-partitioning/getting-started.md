# Getting Started with Spatial Partitioning

The `KeenEyes.Spatial` plugin provides high-performance spatial queries for collision detection, proximity searches, rendering culling, and AI perception systems.

## What is Spatial Partitioning?

Spatial partitioning optimizes spatial queries by organizing entities into a hierarchical data structure based on their positions. Instead of checking every entity (O(n²) for collision detection), spatial partitioning enables logarithmic or constant-time lookups:

- **Grid**: O(1) cell lookup, best for uniform distributions
- **Quadtree** (2D): O(log n) average, adapts to entity density
- **Octree** (3D): O(log n) average, adapts to entity density

## Installation

### 1. Install the Plugin

```csharp
using KeenEyes;
using KeenEyes.Spatial;
using System.Numerics;

var world = new World();

// Install with default configuration (Grid strategy)
world.InstallPlugin(new SpatialPlugin());

// Or specify a strategy
world.InstallPlugin(new SpatialPlugin(new SpatialConfig
{
    Strategy = SpatialStrategy.Quadtree,
    Quadtree = new QuadtreeConfig
    {
        WorldMin = new Vector3(-1000, 0, -1000),
        WorldMax = new Vector3(1000, 0, 1000),
        MaxDepth = 8,
        MaxEntitiesPerNode = 8
    }
}));
```

### 2. Tag Entities for Indexing

Only entities with the `SpatialIndexed` tag are included in the spatial index:

```csharp
using KeenEyes.Common;

// Create a spatially indexed entity
var entity = world.Spawn()
    .With(new Transform3D(
        position: new Vector3(10, 0, 5),
        rotation: Quaternion.Identity,
        scale: Vector3.One))
    .WithTag<SpatialIndexed>()
    .Build();
```

> **Note**: Entities must have a `Transform3D` component for the spatial system to track their position.

### 3. Perform Queries

Access the query API via `world.GetExtension<SpatialQueryApi>()`:

```csharp
var spatial = world.GetExtension<SpatialQueryApi>();

// Find entities within a radius
foreach (var nearby in spatial.QueryRadius(playerPos, 100f))
{
    // Process nearby entities (broadphase candidates)
    ref readonly var transform = ref world.Get<Transform3D>(nearby);

    // Narrowphase: exact distance check
    float distSq = Vector3.DistanceSquared(playerPos, transform.Position);
    if (distSq <= 100f * 100f)
    {
        // Entity is definitely within radius
        Console.WriteLine($"Entity {nearby.Id} is {MathF.Sqrt(distSq):F2} units away");
    }
}
```

## Query Types

The spatial query API provides several query methods:

### Radius Query

Find entities within a spherical volume:

```csharp
// All entities within radius
foreach (var entity in spatial.QueryRadius(center, radius))
{
    // Process entity
}

// Filter by component type
foreach (var enemy in spatial.QueryRadius<EnemyTag>(playerPos, 50f))
{
    // Only enemies within 50 units
}
```

### Bounds Query

Find entities within an axis-aligned bounding box (AABB):

```csharp
var min = new Vector3(-10, 0, -10);
var max = new Vector3(10, 20, 10);

foreach (var entity in spatial.QueryBounds(min, max))
{
    // Entities in the box
}

// With component filter
foreach (var pickup in spatial.QueryBounds<ItemTag>(min, max))
{
    // Only items in the box
}
```

### Point Query

Find entities at a specific location (useful for mouse picking, raycasts):

```csharp
var point = new Vector3(5, 0, 3);

foreach (var entity in spatial.QueryPoint(point))
{
    // Entities at or near this point
}
```

### Frustum Query

Find entities visible to a camera (rendering culling):

```csharp
var viewProjection = camera.ViewMatrix * camera.ProjectionMatrix;
var frustum = Frustum.FromMatrix(viewProjection);

foreach (var entity in spatial.QueryFrustum(frustum))
{
    // Entities visible to the camera (broadphase)
    ref readonly var transform = ref world.Get<Transform3D>(entity);

    // Narrowphase: exact frustum containment test
    if (frustum.Intersects(transform.Position, entityRadius))
    {
        // Render this entity
    }
}
```

## Broadphase vs Narrowphase

Spatial queries return **broadphase candidates** - entities that may satisfy the query. For exact results, perform a **narrowphase check**:

```csharp
// Broadphase: fast spatial query
foreach (var entity in spatial.QueryRadius(center, radius))
{
    // Narrowphase: exact test
    ref readonly var transform = ref world.Get<Transform3D>(entity);
    float distSq = Vector3.DistanceSquared(center, transform.Position);

    if (distSq <= radius * radius)
    {
        // Entity is DEFINITELY within radius
        HandleCollision(entity);
    }
}
```

This two-phase approach provides the best balance:
- **Broadphase**: Eliminate most entities quickly using spatial structure
- **Narrowphase**: Precise test on remaining candidates

## Example: Collision Detection

```csharp
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Spatial;
using System.Numerics;

public class CollisionSystem : SystemBase
{
    private SpatialQueryApi? spatial;

    public override void Initialize()
    {
        spatial = World.GetExtension<SpatialQueryApi>();
    }

    public override void Update(float deltaTime)
    {
        // Check collisions for all entities with collision radius
        foreach (var entity in World.Query<Transform3D, CollisionRadius>())
        {
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            ref readonly var radius = ref World.Get<CollisionRadius>(entity);

            // Broadphase: query nearby entities
            foreach (var other in spatial!.QueryRadius(transform.Position, radius.Value))
            {
                if (other.Id == entity.Id) continue; // Skip self

                // Narrowphase: exact sphere-sphere collision
                ref readonly var otherTransform = ref World.Get<Transform3D>(other);
                ref readonly var otherRadius = ref World.Get<CollisionRadius>(other);

                float combinedRadius = radius.Value + otherRadius.Value;
                float distSq = Vector3.DistanceSquared(
                    transform.Position,
                    otherTransform.Position);

                if (distSq <= combinedRadius * combinedRadius)
                {
                    // Collision detected!
                    HandleCollision(entity, other);
                }
            }
        }
    }

    private void HandleCollision(Entity a, Entity b)
    {
        // Trigger events, apply physics, etc.
    }
}

[Component]
public partial struct CollisionRadius
{
    public float Value;
}
```

## Automatic Index Updates

The spatial index automatically updates when entities move. The `SpatialUpdateSystem` (runs in `LateUpdate` phase) tracks changes to `Transform3D` positions and updates the index:

```csharp
// Movement happens in Update phase
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var velocity = ref World.Get<Velocity>(entity);

            transform.Position += velocity.Value * deltaTime;
            // Position changed - will be indexed in LateUpdate
        }
    }
}

// Spatial index updates automatically in LateUpdate (order: -100)
// Queries in LateUpdate or later will see updated positions
```

## Common Patterns

### Adding/Removing from Index

Entities are added to the index when they gain the `SpatialIndexed` tag:

```csharp
// Add to index
world.Set<SpatialIndexed>(entity);

// Remove from index
world.Remove<SpatialIndexed>(entity);
```

### Querying Specific Entity Types

Use component filtering for type-specific queries:

```csharp
// Find nearby enemies
foreach (var enemy in spatial.QueryRadius<EnemyTag>(playerPos, 50f))
{
    // All enemies within 50 units
}

// Find nearby pickups
foreach (var item in spatial.QueryBounds<ItemTag>(min, max))
{
    // All items in the box
}
```

### Persistent Query Results

Spatial queries return `IEnumerable<Entity>`, which is lazily evaluated. To persist results:

```csharp
// Store results in a list
var nearbyEnemies = spatial
    .QueryRadius<EnemyTag>(playerPos, 100f)
    .ToList();

// Reuse the list
foreach (var enemy in nearbyEnemies)
{
    ProcessEnemy(enemy);
}
```

## Performance Considerations

### Entity Count in Index

Check how many entities are indexed:

```csharp
int indexedCount = spatial.EntityCount;
Console.WriteLine($"{indexedCount} entities in spatial index");
```

### Query Frequency

Spatial queries are fast (O(log n) or O(1)), but avoid unnecessary queries:

```csharp
// Bad: Query every frame for every entity
foreach (var entity in allEntities)
{
    foreach (var nearby in spatial.QueryRadius(entity.Position, 100f))
    {
        // O(n * log n) per frame - expensive!
    }
}

// Good: Query only when needed
foreach (var entity in entitiesThatNeedQueries)
{
    if (shouldCheckProximity)
    {
        foreach (var nearby in spatial.QueryRadius(entity.Position, 100f))
        {
            // Only query when necessary
        }
    }
}
```

### Narrowphase Tests

Always perform narrowphase tests for exact results:

```csharp
// Broadphase may return false positives
foreach (var entity in spatial.QueryRadius(center, radius))
{
    // REQUIRED: Exact distance test
    ref readonly var transform = ref world.Get<Transform3D>(entity);
    float distSq = Vector3.DistanceSquared(center, transform.Position);

    if (distSq <= radius * radius)
    {
        // Confirmed hit
    }
}
```

## Next Steps

- [Strategy Selection](strategy-selection.md) - Choose the right partitioning strategy
- [Performance Tuning](performance-tuning.md) - Optimize configuration for your use case
- [Samples](../../samples/) - Complete examples of collision detection, rendering culling, and AI
