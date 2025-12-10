# Performance Tuning Guide

This guide covers optimization techniques for spatial partitioning performance, debugging tools, and common pitfalls.

## Profiling Spatial Performance

### Measure Before Optimizing

Always profile before making changes. Use these metrics:

```csharp
using System.Diagnostics;

var stopwatch = Stopwatch.StartNew();

// Measure query performance
foreach (var entity in spatial.QueryRadius(center, radius))
{
    ProcessEntity(entity);
}

stopwatch.Stop();
Console.WriteLine($"Query took {stopwatch.Elapsed.TotalMilliseconds:F3}ms");

// Measure index size
Console.WriteLine($"Entities indexed: {spatial.EntityCount}");
```

### What to Measure

1. **Query time** - Time spent in spatial queries per frame
2. **Update time** - Time spent in SpatialUpdateSystem (LateUpdate)
3. **Memory usage** - Index memory footprint
4. **False positive rate** - Broadphase candidates that fail narrowphase
5. **Entity distribution** - Are entities clustered or uniform?

### Profiling Tools

```csharp
// Count false positives
public static class SpatialProfiler
{
    public static (int Total, int FalsePositives) ProfileRadiusQuery(
        SpatialQueryApi spatial,
        IWorld world,
        Vector3 center,
        float radius)
    {
        int total = 0;
        int falsePositives = 0;

        foreach (var entity in spatial.QueryRadius(center, radius))
        {
            total++;

            ref readonly var transform = ref world.Get<Transform3D>(entity);
            float distSq = Vector3.DistanceSquared(center, transform.Position);

            if (distSq > radius * radius)
            {
                falsePositives++;
            }
        }

        float rate = total > 0 ? (falsePositives / (float)total) * 100f : 0f;
        Console.WriteLine($"False positive rate: {rate:F1}% ({falsePositives}/{total})");

        return (total, falsePositives);
    }
}
```

## Grid Tuning

### Cell Size Optimization

Cell size is the most critical parameter for Grid performance:

```csharp
// Rule of thumb: CellSize = 2 * average_entity_size
// Or: CellSize = 2 * average_query_radius

var config = new GridConfig
{
    CellSize = 100f,  // For entities ~50 units in size
};
```

**Too Small:**
- Many cells to check per query
- Overhead from cell iteration
- Wasted memory for empty cells

**Too Large:**
- Too many entities per cell
- High false positive rate
- Defeats purpose of spatial partitioning

**Finding the Sweet Spot:**

```csharp
// Test different cell sizes and measure
foreach (var cellSize in new[] { 25f, 50f, 100f, 200f, 400f })
{
    var config = new GridConfig { CellSize = cellSize };
    world.InstallPlugin(new SpatialPlugin(new SpatialConfig
    {
        Strategy = SpatialStrategy.Grid,
        Grid = config
    }));

    // Run benchmark...
    float avgQueryTime = MeasureAverageQueryTime();
    Console.WriteLine($"CellSize {cellSize}: {avgQueryTime:F3}ms avg");

    world.UninstallPlugin<SpatialPlugin>();
}
```

### World Bounds

Set world bounds to encompass your playable area:

```csharp
var config = new GridConfig
{
    CellSize = 100f,
    // Bounds should match your level/world size
    WorldMin = new Vector3(-2000, -100, -2000),
    WorldMax = new Vector3(2000, 100, 2000)
};
```

**Why it matters:**
- Entities outside bounds still work but may hash less efficiently
- Extremely large coordinates can cause hash collisions
- Memory is allocated proportionally to world size in bounded grids

### Deterministic Mode

Enable for networked games or replays:

```csharp
var config = new GridConfig
{
    DeterministicMode = true  // Results sorted by Entity.Id
};
```

**Cost:** ~10-20% slower queries (sorting overhead)
**Benefit:** Guaranteed reproducible results across clients/replays

## Quadtree Tuning

### Depth vs Entities Per Node

These parameters control tree structure:

```csharp
var config = new QuadtreeConfig
{
    MaxDepth = 8,              // Maximum tree depth
    MaxEntitiesPerNode = 8     // Subdivision threshold
};
```

**MaxDepth:**
- Higher = finer spatial resolution, deeper trees
- Lower = coarser resolution, shallower trees
- Typical range: 6-12
- Depth 8 = 4^8 = 65,536 potential leaf nodes

**MaxEntitiesPerNode:**
- Lower = more subdivisions, more nodes, finer granularity
- Higher = fewer subdivisions, fewer nodes, coarser granularity
- Typical range: 4-16
- Sweet spot: 8-12 for most games

**Finding Optimal Values:**

```csharp
// Test combinations
foreach (var depth in new[] { 6, 8, 10, 12 })
{
    foreach (var maxEntities in new[] { 4, 8, 12, 16 })
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = depth,
            MaxEntitiesPerNode = maxEntities
        };

        // Benchmark...
        Console.WriteLine($"Depth {depth}, MaxEntities {maxEntities}: " +
                          $"{avgQueryTime:F3}ms, {memory:F1}MB");
    }
}
```

### Loose Bounds for Dynamic Entities

Loose bounds reduce update cost for moving entities:

```csharp
var config = new QuadtreeConfig
{
    UseLooseBounds = true,
    LoosenessFactor = 2.0f  // Expand node bounds by 2x
};
```

**When to use:**
- Entities move frequently (every frame or often)
- Movement is small relative to node size
- Update performance > query performance

**Trade-offs:**
- Fewer updates (entities stay in same node longer)
- More false positives (larger node bounds)
- Slightly slower queries

**Tuning LoosenessFactor:**

```csharp
// Factor = 1.0: No expansion (tight bounds)
// Factor = 2.0: Double bounds (1.0 expansion in each direction)
// Factor = 3.0: Triple bounds (2.0 expansion in each direction)

// For fast-moving entities
LoosenessFactor = 3.0f;

// For slow-moving entities
LoosenessFactor = 1.5f;
```

### Node Pooling

Enabled by default, reduces allocation overhead:

```csharp
var config = new QuadtreeConfig
{
    UseNodePooling = true  // Default: true
};
```

**When to disable:**
- Debugging allocation issues
- Profiling memory usage
- Never in production (pooling is always beneficial)

## Octree Tuning

### Depth Considerations

Octrees grow faster than Quadtrees (8^d vs 4^d):

```csharp
var config = new OctreeConfig
{
    MaxDepth = 6,  // Lower than Quadtree due to 8^d growth
    MaxEntitiesPerNode = 8
};
```

**Comparison:**
- Quadtree depth 8 = 4^8 = 65,536 leaf nodes
- Octree depth 6 = 8^6 = 262,144 leaf nodes
- Octree depth 8 = 8^8 = 16,777,216 leaf nodes (usually too deep!)

**Typical Octree Depths:**
- Small worlds: 4-5
- Medium worlds: 6-7
- Large worlds: 7-8
- Extreme worlds: 9+ (rare)

### 3D Movement and Loose Bounds

Loose bounds are especially important for 3D movement:

```csharp
var config = new OctreeConfig
{
    UseLooseBounds = true,
    LoosenessFactor = 2.5f  // Higher for 3D (more degrees of freedom)
};
```

**Why:**
- Entities can move in all three dimensions
- More opportunities to cross node boundaries
- Higher update cost without loose bounds

### Memory vs Performance

Octrees use more memory than Quadtrees:

```csharp
// Each subdivision creates 8 children (vs 4 for Quadtree)
// Memory grows as 8^depth vs 4^depth

// For memory-constrained scenarios:
var config = new OctreeConfig
{
    MaxDepth = 5,              // Limit depth
    MaxEntitiesPerNode = 12,   // Higher threshold (fewer subdivisions)
    UseNodePooling = true      // Always enable pooling
};
```

## Common Optimizations

### 1. Reduce Query Frequency

Don't query every frame if unnecessary:

```csharp
// Bad: Query every frame
public override void Update(float deltaTime)
{
    foreach (var entity in world.Query<Transform3D>())
    {
        var nearby = spatial.QueryRadius(position, 100f).ToList();
        // ...
    }
}

// Good: Query every N frames or on event
private int frameCounter = 0;
private const int QueryInterval = 10;  // Every 10 frames

public override void Update(float deltaTime)
{
    frameCounter++;
    if (frameCounter >= QueryInterval)
    {
        frameCounter = 0;
        UpdateNearbyCache();
    }
}
```

### 2. Cache Query Results

Reuse query results when possible:

```csharp
// Cache results and update periodically
private List<Entity> nearbyEnemies = new();
private float lastQueryTime = 0f;
private const float QueryCooldown = 0.5f;  // Every 0.5 seconds

public override void Update(float deltaTime)
{
    lastQueryTime += deltaTime;

    if (lastQueryTime >= QueryCooldown)
    {
        lastQueryTime = 0f;
        nearbyEnemies.Clear();
        nearbyEnemies.AddRange(spatial.QueryRadius<EnemyTag>(playerPos, 100f));
    }

    // Use cached results
    foreach (var enemy in nearbyEnemies)
    {
        ProcessEnemy(enemy);
    }
}
```

### 3. Optimize Narrowphase Tests

Use squared distances to avoid sqrt:

```csharp
// Bad: Square root for every entity
foreach (var entity in spatial.QueryRadius(center, radius))
{
    float distance = Vector3.Distance(center, transform.Position);
    if (distance <= radius)  // sqrt is expensive!
    {
        Process(entity);
    }
}

// Good: Squared distance (no sqrt)
float radiusSq = radius * radius;
foreach (var entity in spatial.QueryRadius(center, radius))
{
    float distSq = Vector3.DistanceSquared(center, transform.Position);
    if (distSq <= radiusSq)
    {
        Process(entity);
    }
}
```

### 4. Filter by Component Before Query

If possible, filter entities before spatial query:

```csharp
// Bad: Query all entities, then filter
foreach (var entity in spatial.QueryRadius(center, radius))
{
    if (world.Has<EnemyTag>(entity))  // Expensive check for every result
    {
        ProcessEnemy(entity);
    }
}

// Good: Use typed query (filters during query)
foreach (var enemy in spatial.QueryRadius<EnemyTag>(center, radius))
{
    ProcessEnemy(enemy);  // Already filtered
}
```

### 5. Avoid Nested Queries

Nested queries are O(n²) - avoid if possible:

```csharp
// Bad: O(n²) - query for every entity
foreach (var entity in world.Query<Transform3D>())
{
    var nearby = spatial.QueryRadius(entity.Position, 50f);
    foreach (var other in nearby)
    {
        // n * log(n) queries per frame!
    }
}

// Good: Query once, check pairs
var allEntities = world.Query<Transform3D>().ToList();
for (int i = 0; i < allEntities.Count; i++)
{
    var entity = allEntities[i];
    var nearby = spatial.QueryRadius(entity.Position, 50f);

    foreach (var other in nearby)
    {
        if (other.Id <= entity.Id) continue;  // Avoid duplicate pairs
        CheckInteraction(entity, other);
    }
}
```

## Debugging Tools

### Visualize Spatial Structure

```csharp
// For Quadtree/Octree: Visualize nodes
public static class SpatialDebugger
{
    public static void DrawQuadtreeNodes(QuadtreePartitioner tree)
    {
        // Recursively draw node bounds
        DrawNode(tree.Root, 0);
    }

    private static void DrawNode(QuadtreeNode node, int depth)
    {
        // Draw this node's bounds (use debug rendering or gizmos)
        DrawBounds(node.Bounds, GetDepthColor(depth));

        if (node.Children != null)
        {
            for (int i = 0; i < 4; i++)
            {
                DrawNode(node.Children[i], depth + 1);
            }
        }
    }

    private static Color GetDepthColor(int depth)
    {
        // Different color per depth level
        return depth switch
        {
            0 => Color.Red,
            1 => Color.Orange,
            2 => Color.Yellow,
            3 => Color.Green,
            4 => Color.Blue,
            _ => Color.Purple
        };
    }
}
```

### Log Query Statistics

```csharp
public class SpatialQueryStats
{
    public int TotalQueries { get; private set; }
    public int TotalCandidates { get; private set; }
    public int TotalFalsePositives { get; private set; }
    public float AvgCandidatesPerQuery => TotalQueries > 0
        ? TotalCandidates / (float)TotalQueries
        : 0f;
    public float FalsePositiveRate => TotalCandidates > 0
        ? (TotalFalsePositives / (float)TotalCandidates) * 100f
        : 0f;

    public void RecordQuery(int candidates, int falsePositives)
    {
        TotalQueries++;
        TotalCandidates += candidates;
        TotalFalsePositives += falsePositives;
    }

    public void Reset()
    {
        TotalQueries = 0;
        TotalCandidates = 0;
        TotalFalsePositives = 0;
    }

    public void Print()
    {
        Console.WriteLine($"Spatial Query Stats:");
        Console.WriteLine($"  Total queries: {TotalQueries}");
        Console.WriteLine($"  Avg candidates/query: {AvgCandidatesPerQuery:F1}");
        Console.WriteLine($"  False positive rate: {FalsePositiveRate:F1}%");
    }
}
```

## Performance Targets

### Expected Performance

**Grid:**
- Query time: < 0.1ms for most queries (O(1) lookup)
- Update time: < 0.05ms per entity (simple rehash)
- Memory: Predictable, ~10-100KB for typical games

**Quadtree:**
- Query time: 0.1-1.0ms for most queries (depends on depth)
- Update time: 0.1-0.5ms per entity (tree traversal + potential subdivision)
- Memory: Adaptive, ~1-10MB for typical games

**Octree:**
- Query time: 0.2-2.0ms for most queries (3D traversal)
- Update time: 0.2-1.0ms per entity
- Memory: Adaptive, ~2-20MB for typical games

### When to Worry

**Red flags:**
- Query time > 5ms consistently
- Update time > 2ms per frame
- False positive rate > 50%
- Memory usage growing unbounded
- Deep trees (depth > 12 for Quadtree, > 9 for Octree)

## Troubleshooting

### Slow Queries

**Symptoms:** Queries take too long (> 2ms)

**Possible causes:**
1. Too many entities in index
2. Poor configuration (cell size, depth, max entities)
3. Excessive false positives
4. Too many queries per frame

**Solutions:**
- Profile false positive rate
- Adjust configuration (see tuning sections above)
- Cache query results
- Reduce query frequency

### Slow Updates

**Symptoms:** SpatialUpdateSystem takes too long (> 5ms)

**Possible causes:**
1. Many entities moving every frame
2. Entities frequently crossing node boundaries
3. Frequent subdivision/collapsing

**Solutions:**
- Enable loose bounds for dynamic entities
- Increase LoosenessFactor
- Reduce MaxDepth (fewer subdivisions)
- Increase MaxEntitiesPerNode (less subdivision)

### High Memory Usage

**Symptoms:** Spatial index uses too much memory (> 100MB)

**Possible causes:**
1. Tree too deep (many nodes allocated)
2. MaxEntitiesPerNode too low (excessive subdivision)
3. Node pooling disabled

**Solutions:**
- Reduce MaxDepth
- Increase MaxEntitiesPerNode
- Enable UseNodePooling
- Consider switching to Grid

### Determinism Issues

**Symptoms:** Query results differ across clients/replays

**Possible causes:**
1. DeterministicMode disabled
2. Hash collisions (Grid)
3. Floating-point precision differences

**Solutions:**
- Enable DeterministicMode
- Use fixed-point math for positions (if necessary)
- Ensure consistent rounding

## Next Steps

- [Strategy Selection](strategy-selection.md) - Choose the right strategy
- [Getting Started](getting-started.md) - Basic usage and API reference
- [Benchmarks](../../benchmarks/KeenEyes.Spatial.Benchmarks/) - Measure performance
