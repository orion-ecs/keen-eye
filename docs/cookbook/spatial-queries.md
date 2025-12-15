# Spatial Queries

## Problem

You need to efficiently find entities near a point, within a radius, or that might collide - without checking every entity against every other entity.

## Solution

### Using Built-in Spatial Partitioning

KeenEyes provides spatial data structures for efficient proximity queries.

```csharp
using KeenEyes.Spatial;

// Create a spatial index
var grid = new SpatialGrid<Entity>(cellSize: 100);

// Or for varying densities
var quadtree = new Quadtree<Entity>(bounds: new Rectangle(0, 0, 1000, 1000));
```

### Syncing Entities with Spatial Index

```csharp
public class SpatialIndexSystem : SystemBase
{
    private SpatialGrid<Entity> spatialGrid = null!;

    public override void Initialize()
    {
        spatialGrid = new SpatialGrid<Entity>(cellSize: 50);
        World.SetSingleton(spatialGrid);
    }

    public override void Update(float deltaTime)
    {
        // Clear and rebuild (simple approach)
        spatialGrid.Clear();

        foreach (var entity in World.Query<Position>().With<Collidable>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            spatialGrid.Insert(entity, pos.X, pos.Y);
        }
    }
}
```

### Radius Query

```csharp
public class ProximitySystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var grid = World.GetSingleton<SpatialGrid<Entity>>();

        foreach (var entity in World.Query<Position, DetectionRadius>().With<Enemy>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var detection = ref World.Get<DetectionRadius>(entity);

            // Find all entities within detection radius
            var nearby = grid.QueryRadius(pos.X, pos.Y, detection.Radius);

            foreach (var other in nearby)
            {
                if (other == entity)
                    continue;

                if (World.Has<Player>(other))
                {
                    // Player detected! Switch to chase state
                    World.Add(entity, new ChaseData { Target = other });
                }
            }
        }
    }
}
```

### Collision Detection

```csharp
[Component]
public partial struct CollisionBox : IComponent
{
    public float Width;
    public float Height;
}

public class CollisionSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var grid = World.GetSingleton<SpatialGrid<Entity>>();
        var buffer = World.GetCommandBuffer();

        // Check projectiles against targets
        foreach (var projectile in World.Query<Position, CollisionBox>().With<Projectile>())
        {
            ref readonly var projPos = ref World.Get<Position>(projectile);
            ref readonly var projBox = ref World.Get<CollisionBox>(projectile);

            // Only check nearby entities
            var maxDim = MathF.Max(projBox.Width, projBox.Height);
            var candidates = grid.QueryRadius(projPos.X, projPos.Y, maxDim * 2);

            foreach (var target in candidates)
            {
                if (target == projectile)
                    continue;

                if (!World.Has<Health>(target))
                    continue;  // Can't damage this

                ref readonly var targetPos = ref World.Get<Position>(target);
                ref readonly var targetBox = ref World.Get<CollisionBox>(target);

                if (BoxesIntersect(projPos, projBox, targetPos, targetBox))
                {
                    // Hit!
                    ref readonly var proj = ref World.Get<Projectile>(projectile);
                    buffer.Add(target, new DamageReceived
                    {
                        Amount = proj.Damage,
                        Source = proj.Owner
                    });

                    // Destroy projectile
                    buffer.Remove<PooledActive>(projectile);
                    break;  // Projectile can only hit once
                }
            }
        }

        buffer.Execute();
    }

    private bool BoxesIntersect(Position posA, CollisionBox boxA, Position posB, CollisionBox boxB)
    {
        return posA.X < posB.X + boxB.Width &&
               posA.X + boxA.Width > posB.X &&
               posA.Y < posB.Y + boxB.Height &&
               posA.Y + boxA.Height > posB.Y;
    }
}
```

### Range Query (Rectangle)

```csharp
public class ViewportCullingSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var grid = World.GetSingleton<SpatialGrid<Entity>>();
        ref readonly var camera = ref World.GetSingleton<Camera>();

        // Get viewport bounds
        var viewportRect = new Rectangle(
            camera.X - camera.ViewWidth / 2,
            camera.Y - camera.ViewHeight / 2,
            camera.ViewWidth,
            camera.ViewHeight
        );

        // Query only entities in viewport
        var visibleEntities = grid.QueryRectangle(viewportRect);

        foreach (var entity in visibleEntities)
        {
            if (!World.Has<Sprite>(entity))
                continue;

            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var sprite = ref World.Get<Sprite>(entity);

            Renderer.Draw(sprite, pos);
        }
    }
}
```

## Why This Works

### O(n) vs O(n²)

**Naive approach** (O(n²)):
```csharp
foreach (var a in entities)
    foreach (var b in entities)
        if (Collides(a, b)) ...
```

With 1000 entities: 1,000,000 checks per frame.

**Spatial partitioning** (O(n)):
```csharp
foreach (var a in entities)
    foreach (var b in grid.QueryNearby(a))  // ~10 entities
        if (Collides(a, b)) ...
```

With 1000 entities: ~10,000 checks per frame.

### Cell Size Tuning

The optimal cell size depends on:
- **Entity density**: Higher density → smaller cells
- **Query radius**: Cell size ≈ average query radius
- **Movement speed**: Very fast entities may skip cells

Rule of thumb: Cell size = 2× average entity size.

### When to Rebuild

Options for keeping spatial index current:

1. **Full rebuild each frame** (simplest):
   ```csharp
   grid.Clear();
   foreach (var e in entities) grid.Insert(e, pos);
   ```

2. **Update only moved entities**:
   ```csharp
   foreach (var e in movedThisFrame)
   {
       grid.Remove(e);
       grid.Insert(e, newPos);
   }
   ```

3. **Lazy update** (check position on query):
   ```csharp
   // Grid stores last known position
   // On query, verify entity is still in range
   ```

## Variations

### Quadtree for Non-Uniform Distribution

```csharp
// Better when entities cluster in certain areas
var quadtree = new Quadtree<Entity>(
    bounds: new Rectangle(0, 0, worldWidth, worldHeight),
    maxDepth: 8,
    maxEntitiesPerNode: 16
);

// Automatically subdivides dense areas
foreach (var entity in World.Query<Position>())
{
    ref readonly var pos = ref World.Get<Position>(entity);
    quadtree.Insert(entity, pos.X, pos.Y);
}
```

### Octree for 3D

```csharp
var octree = new Octree<Entity>(
    bounds: new BoundingBox(Vector3.Zero, new Vector3(1000, 1000, 1000))
);

foreach (var entity in World.Query<Transform3D>())
{
    ref readonly var transform = ref World.Get<Transform3D>(entity);
    octree.Insert(entity, transform.Position);
}

// 3D radius query
var nearbyIn3D = octree.QuerySphere(center, radius);
```

### Raycast

```csharp
public Entity? Raycast(Vector2 origin, Vector2 direction, float maxDistance)
{
    var grid = World.GetSingleton<SpatialGrid<Entity>>();

    // Step along ray
    float stepSize = grid.CellSize / 2;
    var current = origin;
    float traveled = 0;

    while (traveled < maxDistance)
    {
        var candidates = grid.QueryCell((int)(current.X / grid.CellSize),
                                         (int)(current.Y / grid.CellSize));

        foreach (var entity in candidates)
        {
            if (IntersectsRay(entity, origin, direction, out float dist))
            {
                if (dist <= maxDistance)
                    return entity;
            }
        }

        current += direction * stepSize;
        traveled += stepSize;
    }

    return null;
}
```

### K-Nearest Neighbors

```csharp
public List<Entity> GetKNearest(Position origin, int k)
{
    var grid = World.GetSingleton<SpatialGrid<Entity>>();
    var result = new List<(Entity entity, float distSq)>();

    // Start with small radius, expand until we have k
    float radius = grid.CellSize;

    while (result.Count < k && radius < 10000)
    {
        result.Clear();
        var candidates = grid.QueryRadius(origin.X, origin.Y, radius);

        foreach (var entity in candidates)
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            float distSq = (pos.X - origin.X) * (pos.X - origin.X) +
                           (pos.Y - origin.Y) * (pos.Y - origin.Y);
            result.Add((entity, distSq));
        }

        radius *= 2;
    }

    return result
        .OrderBy(x => x.distSq)
        .Take(k)
        .Select(x => x.entity)
        .ToList();
}
```

### Collision Layers

```csharp
[Flags]
public enum CollisionLayer
{
    None = 0,
    Player = 1 << 0,
    Enemy = 1 << 1,
    Projectile = 1 << 2,
    Environment = 1 << 3,
    Pickup = 1 << 4
}

[Component]
public partial struct CollisionMask : IComponent
{
    public CollisionLayer Layer;      // What I am
    public CollisionLayer ColidesWith; // What I hit
}

// In collision system:
foreach (var a in World.Query<Position, CollisionMask>())
{
    ref readonly var maskA = ref World.Get<CollisionMask>(a);

    foreach (var b in grid.QueryRadius(posA.X, posA.Y, radius))
    {
        ref readonly var maskB = ref World.Get<CollisionMask>(b);

        // Check if layers match
        if ((maskA.ColidesWith & maskB.Layer) == 0)
            continue;

        // These can collide
        CheckCollision(a, b);
    }
}
```

## Performance Tips

1. **Don't over-query**: Cache results when checking multiple times
2. **Use appropriate structure**: Grid for uniform distribution, Quadtree for clustered
3. **Size cells properly**: Too small = many cells to check; too large = many entities per cell
4. **Consider update frequency**: Static objects can use separate, non-updating index

## See Also

- [Spatial Partitioning Guide](../spatial-partitioning/getting-started.md) - Full spatial documentation
- [Strategy Selection](../spatial-partitioning/strategy-selection.md) - Choose the right structure
- [Performance Tuning](../spatial-partitioning/performance-tuning.md) - Optimization techniques
