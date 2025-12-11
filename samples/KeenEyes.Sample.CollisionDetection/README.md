# Collision Detection Sample

Demonstrates broadphase/narrowphase collision detection using spatial partitioning and compares performance against a naive O(n²) approach.

## What This Sample Shows

- **Broadphase vs Narrowphase**: How spatial queries provide broadphase candidates and narrowphase tests confirm actual collisions
- **Performance Comparison**: Grid vs Quadtree vs Naive O(n²) approaches
- **Spatial Query Usage**: Using `QueryRadius()` for efficient proximity checks
- **Component Filtering**: Querying entities with specific components
- **Statistics Tracking**: Measuring false positive rates and performance

## Running the Sample

### Build and Run

```bash
# From repository root
cd samples/KeenEyes.Sample.CollisionDetection

# Restore dependencies (first time only)
dotnet restore

# Build the sample
dotnet build

# Run the sample
dotnet run
```

Or from the repository root:

```bash
dotnet run --project samples/KeenEyes.Sample.CollisionDetection
```

## Expected Output

The sample runs three scenarios with 1,000 entities:

1. **Grid Strategy** - Fixed-size cells for uniform distributions
2. **Quadtree Strategy** - Adaptive tree for clustered distributions
3. **Naive O(n²)** - Checks every entity pair (no spatial partitioning)

Example output:

```
=== Collision Detection Sample ===

Simulating 1000 entities in 1000x1000 world
Entity radius: 5 units
Running 100 frames...

--- Grid Strategy ---
Total time: 245ms
Average frame time: 2.45ms
Total collisions detected: 1234
Average collisions/frame: 12.3
Broadphase candidates: 8542
Narrowphase checks: 8542
False positive rate: 15.2%

--- Quadtree Strategy ---
Total time: 312ms
Average frame time: 3.12ms
Total collisions detected: 1234
Average collisions/frame: 12.3
Broadphase candidates: 7891
Narrowphase checks: 7891
False positive rate: 12.8%

--- Naive O(n²) Approach ---
Total time: 1847ms
Average frame time: 18.47ms
Total collisions detected: 1234
Average collisions/frame: 12.3
Total entity pair checks: 499500
```

## Key Observations

### Performance

- **Grid** is fastest for uniform distributions (O(1) cell lookup)
- **Quadtree** adapts to clustering but has tree traversal overhead
- **Naive** is dramatically slower (~7-10x) as entity count grows

### Scalability

The naive approach checks n*(n-1)/2 pairs:
- 100 entities = 4,950 checks
- 1,000 entities = 499,500 checks
- 10,000 entities = 49,995,000 checks (impractical!)

Spatial partitioning reduces checks to O(n log n) or O(n):
- Only checks entities in nearby cells/nodes
- Scales much better as entity count increases

### False Positives

Spatial queries return **broadphase candidates** - entities that may collide. Not all candidates will actually collide:

- Grid/Quadtree return entities in the same or adjacent cells/nodes
- Entities at cell/node edges may be far apart
- **Narrowphase** (exact distance test) confirms actual collisions

## Code Structure

### Components

- `Transform3D` - Position, rotation, scale (from KeenEyes.Common)
- `Velocity` - Movement velocity vector
- `CollisionRadius` - Sphere collision radius
- `SpatialIndexed` - Tag for spatial index inclusion

### Systems

- `MovementSystem` - Updates entity positions based on velocity
- `SpatialCollisionSystem` - Detects collisions using spatial queries
- `NaiveCollisionSystem` - Detects collisions using O(n²) brute force
- `StatsSystem` - Tracks and reports collision statistics

### Collision Detection Flow

```csharp
// For each entity:
foreach (var entity in World.Query<Transform3D, CollisionRadius>())
{
    // 1. BROADPHASE: Query spatial index for nearby entities
    foreach (var other in spatial.QueryRadius(position, radius * 2))
    {
        // 2. NARROWPHASE: Exact distance test
        float distSq = Vector3.DistanceSquared(pos1, pos2);
        if (distSq <= combinedRadiusSq)
        {
            // Confirmed collision!
        }
    }
}
```

## Tuning for Your Game

### Cell Size (Grid)

```csharp
Grid = new GridConfig
{
    CellSize = EntityRadius * 4f  // Rule of thumb: 2-4x average entity size
}
```

**Too small**: Many cells to check, overhead
**Too large**: Too many entities per cell, defeats purpose

### Tree Configuration (Quadtree)

```csharp
Quadtree = new QuadtreeConfig
{
    MaxDepth = 8,              // Depth 8 = 65,536 potential leaf nodes
    MaxEntitiesPerNode = 8     // Subdivide when > 8 entities in node
}
```

**Lower MaxEntitiesPerNode**: Finer subdivision, more memory
**Higher MaxDepth**: More spatial resolution, deeper trees

## Integration Tips

### Component Filtering

Query only entities of specific types:

```csharp
// Only check enemies for collision
foreach (var enemy in spatial.QueryRadius<EnemyTag>(playerPos, attackRange))
{
    // All results are enemies
}
```

### Query Optimization

Cache query results when possible:

```csharp
// Instead of querying every frame:
private List<Entity> nearbyEnemies = new();

if (Time.time >= lastQueryTime + 0.5f)  // Query every 0.5s
{
    nearbyEnemies.Clear();
    nearbyEnemies.AddRange(spatial.QueryRadius<EnemyTag>(pos, range));
    lastQueryTime = Time.time;
}
```

### Narrowphase Optimization

Always use squared distances (avoids expensive sqrt):

```csharp
float radiusSq = radius * radius;  // Pre-compute
float distSq = Vector3.DistanceSquared(a, b);  // No sqrt!
if (distSq <= radiusSq)
{
    // Collision
}
```

## Documentation

Learn more about spatial partitioning:

- **[Getting Started](../../docs/spatial-partitioning/getting-started.md)** - Installation, query types, and basic patterns
- **[Strategy Selection](../../docs/spatial-partitioning/strategy-selection.md)** - Choosing between Grid, Quadtree, and Octree
- **[Performance Tuning](../../docs/spatial-partitioning/performance-tuning.md)** - Optimization techniques and profiling

## Next Steps

- Try different strategies: Grid vs Quadtree
- Experiment with entity counts: 100, 1000, 10000
- Add physics response to collisions
- Visualize the spatial structure (debug rendering)

## Related Samples

- [Rendering Culling](../KeenEyes.Sample.RenderingCulling) - Frustum culling for 3D rendering
- [AI Proximity](../KeenEyes.Sample.AIProximity) - Vision/hearing detection for AI agents
