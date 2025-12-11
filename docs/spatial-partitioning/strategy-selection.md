# Spatial Partitioning Strategy Selection

Choosing the right spatial partitioning strategy is crucial for optimal performance. This guide helps you select between Grid, Quadtree, and Octree based on your game's characteristics.

## Strategy Comparison

| Strategy | Best For | Query Performance | Update Performance | Memory Usage |
|----------|----------|-------------------|-------------------|--------------|
| **Grid** | Uniform distributions | O(1) | O(1) | Fixed |
| **Quadtree** (2D) | Clustered 2D scenes | O(log n) avg | O(log n) avg | Adaptive |
| **Octree** (3D) | Clustered 3D scenes | O(log n) avg | O(log n) avg | Adaptive |

## Grid Strategy

### When to Use

Grid partitioning divides space into fixed-size cells. Choose Grid when:

- **Entities are uniformly distributed** across your world
- **Predictable performance** is more important than optimal performance
- **Simplicity** is valued (easiest to understand and tune)
- **Memory usage must be predictable** (no dynamic tree growth)
- **Fast updates** are critical (moving entities just change cell ID)

### Characteristics

**Advantages:**
- O(1) cell lookup - constant time regardless of entity count
- O(1) entity updates - moving entities just rehash position
- Predictable memory footprint
- No tree traversal overhead
- Works well with very large worlds (hash-based infinite grid)

**Disadvantages:**
- May return many false positives if entities cluster in few cells
- Empty cells still consume memory (in bounded grids)
- Not adaptive to entity density

### Ideal Use Cases

```csharp
// Large open world with relatively uniform entity distribution
world.InstallPlugin(new SpatialPlugin(new SpatialConfig
{
    Strategy = SpatialStrategy.Grid,
    Grid = new GridConfig
    {
        CellSize = 50f,  // Approximately 2x average entity size
        WorldMin = new Vector3(-5000, -100, -5000),
        WorldMax = new Vector3(5000, 100, 5000)
    }
}));
```

**Examples:**
- Open-world games (Minecraft-style, survival games)
- Strategy games with units spread across a map
- Large-scale multiplayer with evenly distributed players
- Physics simulations with uniform particle distribution

**See also:** [Collision Detection Sample](../../samples/KeenEyes.Sample.CollisionDetection) demonstrates Grid strategy for uniform entity distribution.

### Configuration Tips

```csharp
// Rule of thumb: CellSize = 2 * average_entity_size
// Too small: many cells to check, overhead
// Too large: too many entities per cell, defeats purpose

var config = new GridConfig
{
    CellSize = 100f,  // For entities averaging 50 units in size
    WorldMin = bounds.Min,
    WorldMax = bounds.Max
};
```

## Quadtree Strategy (2D)

### When to Use

Quadtrees recursively subdivide 2D space into four quadrants. Choose Quadtree when:

- **Entities cluster in certain areas** (cities, battle zones, points of interest)
- **2D gameplay** (top-down, side-scrolling, isometric)
- **Entity density varies significantly** across the map
- **Memory efficiency** is important for sparse areas
- **Logarithmic performance** is acceptable

### Characteristics

**Advantages:**
- Adapts to entity density (fine subdivision where needed)
- Efficient for clustered distributions
- Memory scales with entity count, not world size
- Empty areas consume minimal memory

**Disadvantages:**
- O(log n) query and update time (slower than Grid for uniform distributions)
- Tree traversal overhead
- Subdivision/collapsing adds complexity
- Deep trees for very clustered entities

### Ideal Use Cases

```csharp
// 2D game with clustered entities (RTS, tower defense, etc.)
world.InstallPlugin(new SpatialPlugin(new SpatialConfig
{
    Strategy = SpatialStrategy.Quadtree,
    Quadtree = new QuadtreeConfig
    {
        MaxDepth = 8,                  // 4^8 = 65,536 potential leaf nodes
        MaxEntitiesPerNode = 8,        // Subdivide when > 8 entities
        WorldMin = new Vector3(-1024, 0, -1024),
        WorldMax = new Vector3(1024, 0, 1024),
        UseLooseBounds = false         // Tight bounds for optimal queries
    }
}));
```

**Examples:**
- Real-time strategy (RTS) games with army clustering
- Tower defense games with enemy waves
- Top-down shooters with localized combat
- 2D MMOs with player clustering in cities/dungeons

**See also:**
- [Collision Detection Sample](../../samples/KeenEyes.Sample.CollisionDetection) compares Grid vs Quadtree performance
- [AI Proximity Sample](../../samples/KeenEyes.Sample.AIProximity) uses Grid for 2D AI sensory detection

### Configuration Tips

```csharp
// MaxDepth: Higher = finer subdivision, more memory, deeper traversal
// Typical range: 6-12
// Depth 8 = 4^8 = 65,536 leaf nodes (usually sufficient)

// MaxEntitiesPerNode: Lower = more subdivision, higher = flatter tree
// Typical range: 4-16
// Sweet spot: 8-12 for most games

var config = new QuadtreeConfig
{
    MaxDepth = 10,                     // For large worlds or precise queries
    MaxEntitiesPerNode = 6,            // More subdivision for tight queries
    UseLooseBounds = true,             // Enable for dynamic entities
    LoosenessFactor = 2.0f             // Double node bounds (reduces updates)
};
```

### Loose vs Tight Bounds

**Tight Bounds** (UseLooseBounds = false):
- Node bounds exactly match their spatial region
- Entities at node edges may move between nodes frequently
- Best query performance
- Higher update cost for moving entities

**Loose Bounds** (UseLooseBounds = true):
- Node bounds are expanded by `LoosenessFactor`
- Entities can move within expanded region without changing nodes
- Reduced update cost for moving entities
- Slightly more false positives in queries

```csharp
// Choose based on your entity movement patterns
var config = new QuadtreeConfig
{
    // Static or slow-moving entities: tight bounds
    UseLooseBounds = false,

    // Or for fast-moving entities: loose bounds
    UseLooseBounds = true,
    LoosenessFactor = 2.5f  // Expand bounds by 2.5x (1.25x in each direction)
};
```

## Octree Strategy (3D)

### When to Use

Octrees recursively subdivide 3D space into eight octants. Choose Octree when:

- **Entities cluster in 3D space** (buildings, terrain features, aerial combat)
- **True 3D gameplay** (flight simulators, space games, voxel worlds)
- **Entity density varies in all three dimensions**
- **Adaptive memory usage** is important
- **Logarithmic performance** is acceptable

### Characteristics

**Advantages:**
- Adapts to 3D entity density
- Efficient for clustered 3D distributions
- Scales memory with entity count
- Empty 3D regions consume minimal memory

**Disadvantages:**
- O(log n) query and update time
- Higher memory overhead than Quadtree (8 children vs 4)
- Tree traversal in 3D is more complex
- Deeper trees grow faster (8^depth vs 4^depth)

### Ideal Use Cases

```csharp
// 3D game with clustered entities
world.InstallPlugin(new SpatialPlugin(new SpatialConfig
{
    Strategy = SpatialStrategy.Octree,
    Octree = new OctreeConfig
    {
        MaxDepth = 6,                  // 8^6 = 262,144 potential leaf nodes
        MaxEntitiesPerNode = 8,
        WorldMin = new Vector3(-512, -512, -512),
        WorldMax = new Vector3(512, 512, 512),
        UseLooseBounds = false
    }
}));
```

**Examples:**
- Flight simulators with aerial and ground entities
- Space games with 3D positioning
- Voxel-based games (though Grid often better for uniform voxels)
- 3D physics simulations with volumetric clustering
- Underwater games with depth-based gameplay

**See also:** [Rendering Culling Sample](../../samples/KeenEyes.Sample.RenderingCulling) demonstrates Octree strategy for 3D frustum culling.

### Configuration Tips

```csharp
// MaxDepth: Lower than Quadtree due to exponential growth (8^d vs 4^d)
// Typical range: 4-8
// Depth 6 = 8^6 = 262,144 leaf nodes (often sufficient for 3D)
// Depth 8 = 8^8 = 16,777,216 leaf nodes (very large worlds)

// MaxEntitiesPerNode: Similar to Quadtree
// Typical range: 4-16

var config = new OctreeConfig
{
    MaxDepth = 6,                      // Lower than Quadtree due to 8^d growth
    MaxEntitiesPerNode = 8,
    UseLooseBounds = true,             // Recommended for moving 3D entities
    LoosenessFactor = 2.0f
};
```

## Decision Tree

Use this flowchart to select your strategy:

```
Are your entities in true 3D space (all axes matter)?
├─ No → Are entities uniformly distributed?
│   ├─ Yes → Use Grid
│   └─ No (clustered) → Use Quadtree
│
└─ Yes (3D) → Are entities uniformly distributed in 3D?
    ├─ Yes → Use Grid (or Octree for adaptive memory)
    └─ No (clustered in 3D) → Use Octree
```

## Hybrid Approaches

### Multiple Worlds with Different Strategies

For complex games, use multiple worlds with different strategies:

```csharp
// Ground entities: Quadtree (2D-ish movement)
var groundWorld = new World();
groundWorld.InstallPlugin(new SpatialPlugin(new SpatialConfig
{
    Strategy = SpatialStrategy.Quadtree,
    Quadtree = new QuadtreeConfig { MaxDepth = 8 }
}));

// Aerial entities: Octree (true 3D movement)
var aerialWorld = new World();
aerialWorld.InstallPlugin(new SpatialPlugin(new SpatialConfig
{
    Strategy = SpatialStrategy.Octree,
    Octree = new OctreeConfig { MaxDepth = 6 }
}));
```

### Strategy Migration

Start with Grid for simplicity, then migrate to Quadtree/Octree if profiling shows benefit:

```csharp
// Phase 1: Prototype with Grid
var config = new SpatialConfig { Strategy = SpatialStrategy.Grid };

// Phase 2: Profile shows clustering - migrate to Quadtree
var config = new SpatialConfig
{
    Strategy = SpatialStrategy.Quadtree,
    Quadtree = new QuadtreeConfig { MaxDepth = 8, MaxEntitiesPerNode = 8 }
};

// No code changes needed - just configuration!
```

## Performance Benchmarks

See [benchmarks/KeenEyes.Spatial.Benchmarks](../../benchmarks/KeenEyes.Spatial.Benchmarks/) for comparative performance data.

### When to Benchmark

- After initial implementation with default settings
- When entity count exceeds 1,000
- When clustering patterns change (more/less uniform)
- When performance issues arise

### What to Measure

1. **Query time** - How long do spatial queries take?
2. **Update time** - How long does SpatialUpdateSystem take?
3. **Memory usage** - How much memory does the index consume?
4. **False positive rate** - How many broadphase candidates fail narrowphase?

## Configuration Examples

### Small Arena Game (< 500 entities)

```csharp
// Grid works well - entities rarely exceed a few hundred
new SpatialConfig
{
    Strategy = SpatialStrategy.Grid,
    Grid = new GridConfig
    {
        CellSize = 25f,
        WorldMin = new Vector3(-100, 0, -100),
        WorldMax = new Vector3(100, 50, 100)
    }
}
```

### Medium RTS (500-5000 entities)

```csharp
// Quadtree adapts to army clustering
new SpatialConfig
{
    Strategy = SpatialStrategy.Quadtree,
    Quadtree = new QuadtreeConfig
    {
        MaxDepth = 8,
        MaxEntitiesPerNode = 10,
        WorldMin = new Vector3(-2048, 0, -2048),
        WorldMax = new Vector3(2048, 0, 2048),
        UseLooseBounds = true,
        LoosenessFactor = 2.0f
    }
}
```

### Large Open World (5000+ entities)

```csharp
// Grid for uniform distribution, or Octree if clustered
new SpatialConfig
{
    Strategy = SpatialStrategy.Grid,
    Grid = new GridConfig
    {
        CellSize = 100f,
        WorldMin = new Vector3(-10000, -100, -10000),
        WorldMax = new Vector3(10000, 100, 10000),
        DeterministicMode = true  // For networked/replay consistency
    }
}
```

### Space Sim (3D clustering)

```csharp
// Octree for clustered 3D combat zones
new SpatialConfig
{
    Strategy = SpatialStrategy.Octree,
    Octree = new OctreeConfig
    {
        MaxDepth = 6,
        MaxEntitiesPerNode = 6,
        WorldMin = new Vector3(-5000, -5000, -5000),
        WorldMax = new Vector3(5000, 5000, 5000),
        UseLooseBounds = true,
        LoosenessFactor = 3.0f  // High-speed 3D movement
    }
}
```

## See Also

### Documentation
- [Getting Started](getting-started.md) - Basic usage and API reference
- [Performance Tuning](performance-tuning.md) - Optimize your selected strategy

### Sample Projects
- [Collision Detection Sample](../../samples/KeenEyes.Sample.CollisionDetection) - Grid vs Quadtree performance comparison for collision detection
- [Rendering Culling Sample](../../samples/KeenEyes.Sample.RenderingCulling) - Octree strategy for 3D frustum culling
- [AI Proximity Sample](../../samples/KeenEyes.Sample.AIProximity) - Grid strategy for multi-sensory AI detection
