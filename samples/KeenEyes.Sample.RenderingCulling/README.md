# Rendering Culling Sample

Demonstrates frustum culling for 3D rendering optimization using spatial queries. Shows performance gains from culling off-screen entities.

## What This Sample Shows

- **Frustum Culling**: Using `QueryFrustum()` to find entities visible to camera
- **View-Projection Matrix**: Building camera matrices for frustum extraction
- **Broadphase/Narrowphase**: Spatial query followed by exact frustum tests
- **Performance Comparison**: Rendering with/without culling
- **3D Spatial Indexing**: Octree for 3D scene management
- **Camera Movement**: Orbiting camera to demonstrate dynamic culling

## Running the Sample

### Build and Run

```bash
# From repository root
cd samples/KeenEyes.Sample.RenderingCulling

# Restore dependencies (first time only)
dotnet restore

# Build the sample
dotnet build

# Run the sample
dotnet run
```

Or from the repository root:

```bash
dotnet run --project samples/KeenEyes.Sample.RenderingCulling
```

## Expected Output

The sample runs two scenarios with 5,000 entities spread across 3D space:

1. **With Frustum Culling** - Only renders entities visible to camera
2. **Without Frustum Culling** - Renders all entities (naive approach)

Example output:

```
=== Rendering Culling Sample ===

Simulating 5000 entities in 2000x2000x2000 world
Camera FOV: 60°, Aspect: 1.78, Far: 1000
Running 100 frames...

--- With Frustum Culling (Octree) ---
..
Total time: 234ms
Average frame time: 2.34ms
Total entities rendered: 45230
Average entities/frame: 452.3
Total culled: 454770
Average culled/frame: 4547.7
Culling efficiency: 90.9%
Broadphase candidates: 48512
False positive rate: 6.8%

--- Without Frustum Culling (Naive) ---
..
Total time: 1847ms
Average frame time: 18.47ms
Total entities rendered: 500000
Average entities/frame: 5000.0
```

## Key Observations

### Culling Efficiency

With frustum culling:
- ~450 entities rendered per frame (9% visible)
- ~4,550 entities culled per frame (91% culled)
- **90%+ culling efficiency** saves significant GPU time

### Performance Impact

- **With culling**: 2-3ms per frame
- **Without culling**: 18-25ms per frame
- **~8x faster rendering** with frustum culling

### False Positives

Spatial queries return broadphase candidates:
- ~485 candidates per frame
- ~450 actually visible (after narrowphase)
- **~7% false positive rate** (acceptable overhead)

## How Frustum Culling Works

### 1. Build View-Projection Matrix

```csharp
var viewMatrix = Matrix4x4.CreateLookAt(cameraPos, target, up);
var projMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
var viewProj = viewMatrix * projMatrix;
```

### 2. Extract Frustum Planes

```csharp
var frustum = Frustum.FromMatrix(viewProj);
// Frustum has 6 planes: Near, Far, Left, Right, Top, Bottom
```

### 3. Broadphase: Query Spatial Index

```csharp
foreach (var entity in spatial.QueryFrustum<Renderable>(frustum))
{
    // Entity is potentially visible (broadphase candidate)
}
```

### 4. Narrowphase: Exact Frustum Test

```csharp
if (frustum.Contains(entityPosition))
{
    // Entity is definitely visible
    RenderEntity(entity);
}

// Or for bounding volumes:
if (frustum.Intersects(boundsMin, boundsMax))
{
    RenderEntity(entity);
}
```

## Code Structure

### Components

- `Transform3D` - Position, rotation, scale (from KeenEyes.Common)
- `Renderable` - Marks entity as renderable, stores mesh ID
- `Camera` - Camera component (placeholder for real engine)
- `SpatialIndexed` - Tag for spatial index inclusion

### Systems

- `CameraOrbitSystem` - Orbits camera around origin
- `FrustumCullingRenderSystem` - Renders visible entities using frustum culling
- `NaiveRenderSystem` - Renders all entities (no culling)

### Rendering Pipeline

```csharp
// 1. Update camera position (orbit)
CameraOrbitSystem.Update()

// 2. Build frustum from camera
var viewProj = viewMatrix * projMatrix;
var frustum = Frustum.FromMatrix(viewProj);

// 3. Query visible entities
foreach (var entity in spatial.QueryFrustum<Renderable>(frustum))
{
    // 4. Exact visibility test
    if (frustum.Contains(position))
    {
        // 5. Render entity
        RenderEntity(entity);
    }
}
```

## Integration with Real Rendering

### Bounding Volumes

In a real engine, use bounding volumes for narrowphase:

```csharp
// Store AABB with renderable
[Component]
public partial struct Renderable
{
    public int MeshId;
    public Vector3 BoundsMin;
    public Vector3 BoundsMax;
}

// Narrowphase with AABB
if (frustum.Intersects(renderable.BoundsMin, renderable.BoundsMax))
{
    RenderEntity(entity);
}
```

### Graphics API Integration

```csharp
private void RenderEntity(Entity entity)
{
    ref readonly var transform = ref World.Get<Transform3D>(entity);
    ref readonly var renderable = ref World.Get<Renderable>(entity);

    // Update world matrix uniform
    var worldMatrix = transform.ToMatrix();
    shader.SetUniform("uWorld", worldMatrix);

    // Bind mesh and materials
    meshes[renderable.MeshId].Bind();
    materials[renderable.MaterialId].Bind();

    // Submit draw call
    graphicsDevice.DrawIndexed(mesh.IndexCount);
}
```

### Occlusion Culling

Combine frustum culling with occlusion queries:

```csharp
foreach (var entity in spatial.QueryFrustum<Renderable>(frustum))
{
    // Frustum test
    if (!frustum.Contains(position)) continue;

    // Occlusion query (GPU-based)
    if (OcclusionQuery(entity))
    {
        RenderEntity(entity);
    }
}
```

## Tuning for Your Game

### Octree Configuration

```csharp
Octree = new OctreeConfig
{
    MaxDepth = 6,              // Adjust based on world size
    MaxEntitiesPerNode = 8,    // Higher = fewer subdivisions
    WorldMin = sceneBounds.Min,
    WorldMax = sceneBounds.Max
}
```

**Depth 6** = 8^6 = 262,144 potential nodes (good for large scenes)
**Depth 8** = 8^8 = 16,777,216 nodes (very large worlds)

### Camera Settings

```csharp
// Wide FOV = more visible entities
const float CameraFov = 90f;  // Wide angle

// Narrow FOV = fewer visible entities (telephoto)
const float CameraFov = 30f;  // Telephoto

// Far plane affects culling volume
const float CameraFar = 1000f;  // Standard
const float CameraFar = 10000f; // Large open world
```

### Loose Bounds for Moving Objects

```csharp
Octree = new OctreeConfig
{
    UseLooseBounds = true,     // Reduce updates for moving entities
    LoosenessFactor = 2.0f     // 2x bounds = less movement updates
}
```

## Performance Tips

### 1. Minimize Broadphase Candidates

Use tighter octree configuration:

```csharp
MaxDepth = 8,              // Finer subdivision
MaxEntitiesPerNode = 6     // More granular nodes
```

### 2. Cache Frustum Calculation

Don't rebuild frustum every frame if camera is static:

```csharp
private Frustum? cachedFrustum;
private Vector3 lastCameraPos;

if (cameraTransform.Position != lastCameraPos)
{
    cachedFrustum = Frustum.FromMatrix(viewProj);
    lastCameraPos = cameraTransform.Position;
}
```

### 3. LOD (Level of Detail)

Render distant objects with simpler meshes:

```csharp
float distSq = Vector3.DistanceSquared(cameraPos, entityPos);
if (distSq > 1000f * 1000f)
{
    RenderWithLOD(entity, LODLevel.Low);
}
else if (distSq > 100f * 100f)
{
    RenderWithLOD(entity, LODLevel.Medium);
}
else
{
    RenderWithLOD(entity, LODLevel.High);
}
```

### 4. Batching

Group visible entities by mesh/material:

```csharp
var visibleByMesh = new Dictionary<int, List<Entity>>();
foreach (var entity in spatial.QueryFrustum<Renderable>(frustum))
{
    var meshId = World.Get<Renderable>(entity).MeshId;
    if (!visibleByMesh.TryGetValue(meshId, out var list))
    {
        list = new List<Entity>();
        visibleByMesh[meshId] = list;
    }
    list.Add(entity);
}

// Batch render by mesh
foreach (var (meshId, entities) in visibleByMesh)
{
    RenderBatch(meshId, entities);
}
```

## Common Scenarios

### First-Person Game

```csharp
// Typical FPS settings
CameraFov = 90f;        // Wide FOV
CameraFar = 500f;       // Medium draw distance
MaxEntitiesPerNode = 8; // Balanced
```

### Third-Person Game

```csharp
// Over-the-shoulder camera
CameraFov = 60f;        // Standard FOV
CameraFar = 1000f;      // Larger draw distance
MaxEntitiesPerNode = 10;
```

### Open World Game

```csharp
// Large seamless world
CameraFov = 70f;
CameraFar = 5000f;      // Very far draw distance
MaxDepth = 8;           // Deep octree for large world
UseLooseBounds = true;  // Many moving entities
```

### Indoor Game

```csharp
// Tight spaces, less culling benefit
CameraFov = 75f;
CameraFar = 200f;       // Closer far plane
MaxDepth = 5;           // Shallower tree
MaxEntitiesPerNode = 12;
```

## Documentation

Learn more about spatial partitioning:

- **[Getting Started](../../docs/spatial-partitioning/getting-started.md)** - Installation, query types, and basic patterns
- **[Strategy Selection](../../docs/spatial-partitioning/strategy-selection.md)** - Choosing between Grid, Quadtree, and Octree
- **[Performance Tuning](../../docs/spatial-partitioning/performance-tuning.md)** - Optimization techniques and profiling

## Next Steps

- Try different octree configurations
- Experiment with camera settings (FOV, far plane)
- Add LOD system based on distance
- Implement occlusion culling
- Profile with real rendering workload

## Related Samples

- [Collision Detection](../KeenEyes.Sample.CollisionDetection) - Broadphase/narrowphase collision detection
- [AI Proximity](../KeenEyes.Sample.AIProximity) - Vision/hearing detection for AI
