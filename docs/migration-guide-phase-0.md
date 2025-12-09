# Migration Guide: Phase 0 - KeenEyes.Common

## Overview

Phase 0 introduces `KeenEyes.Common`, a new lightweight package containing shared components used across multiple plugins. This change improves dependency architecture by separating common components from optional plugin features.

## Breaking Changes

### Transform3D Moved to KeenEyes.Common

`Transform3D` has been moved from `KeenEyes.Spatial` to `KeenEyes.Common`.

**Why?** This prevents forcing all packages to depend on spatial partitioning logic when they only need basic transform components. Graphics and Physics plugins need `Transform3D` but shouldn't depend on optional spatial partitioning features.

## Migration Steps

### 1. Update Package References

If you're consuming KeenEyes as NuGet packages:

```xml
<!-- Before -->
<PackageReference Include="KeenEyes.Spatial" Version="*" />

<!-- After -->
<PackageReference Include="KeenEyes.Common" Version="*" />
<!-- Only add Spatial if you need spatial partitioning features (Phase 1+) -->
<PackageReference Include="KeenEyes.Spatial" Version="*" />
```

### 2. Update Using Statements

Change all `using KeenEyes.Spatial;` statements that reference `Transform3D` to `using KeenEyes.Common;`:

```csharp
// Before
using KeenEyes.Spatial;

var entity = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .Build();

// After
using KeenEyes.Common;

var entity = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .Build();
```

### 3. Update Project References (For Source)

If you're building from source or referencing KeenEyes projects directly:

```xml
<!-- Before -->
<ProjectReference Include="path/to/KeenEyes.Spatial/KeenEyes.Spatial.csproj" />

<!-- After -->
<ProjectReference Include="path/to/KeenEyes.Common/KeenEyes.Common.csproj" />
```

## New Components in KeenEyes.Common

Phase 0 also introduces new common components:

### Transform2D

2D transformation for 2D games:

```csharp
using KeenEyes.Common;
using System.Numerics;

var entity = world.Spawn()
    .With(new Transform2D(
        position: new Vector2(100, 50),
        rotation: 0f,  // Radians
        scale: Vector2.One))
    .Build();

// Or use identity
var entity = world.Spawn()
    .With(Transform2D.Identity)
    .Build();
```

### SpatialBounds

Axis-aligned bounding box (AABB) for spatial queries and collision detection:

```csharp
using KeenEyes.Common;

var entity = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(new SpatialBounds(
        min: new Vector3(-1, -1, -1),
        max: new Vector3(1, 1, 1)))
    .Build();

// Or create from center and extents
var bounds = SpatialBounds.FromCenterAndExtents(
    center: new Vector3(0, 5, 0),
    extents: new Vector3(2, 2, 2));  // Half-size in each dimension

// Check containment
bool contains = bounds.Contains(point);
bool intersects = bounds.Intersects(otherBounds);
```

### Velocity2D and Velocity3D

Linear velocity components for movement systems:

```csharp
using KeenEyes.Common;

// 2D velocity
var entity2D = world.Spawn()
    .With(new Transform2D(Vector2.Zero, 0f, Vector2.One))
    .With(new Velocity2D(new Vector2(100f, 0f)))  // 100 units/sec right
    .Build();

// 3D velocity
var entity3D = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(new Velocity3D(new Vector3(0f, 10f, 0f)))  // 10 units/sec up
    .Build();

// In a movement system
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity3D>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var velocity = ref World.Get<Velocity3D>(entity);

            transform.Position += velocity.Value * deltaTime;
        }
    }
}
```

## Compatibility

- ✅ **Binary compatible**: No changes to `Transform3D` structure or behavior
- ✅ **API compatible**: All `Transform3D` methods and properties unchanged
- ⚠️ **Namespace change required**: Update `using` statements from `KeenEyes.Spatial` to `KeenEyes.Common`

## Dependency Graph

The new architecture provides cleaner separation:

```
Before:
KeenEyes.Core
    ↑
KeenEyes.Spatial (Transform3D + future spatial partitioning)
    ↑
KeenEyes.Graphics (forced to depend on spatial partitioning)

After:
KeenEyes.Abstractions
    ↑
KeenEyes.Common (Transform3D, Transform2D, SpatialBounds, Velocity)
    ↑
    ├── KeenEyes.Spatial (optional spatial partitioning features)
    ├── KeenEyes.Graphics (no forced spatial partitioning dependency)
    └── KeenEyes.Physics (future, no forced spatial partitioning dependency)

Note: KeenEyes.Common only depends on Abstractions (IComponent interface),
making it as lightweight as possible.
```

## FAQ

### Q: Do I need to reference both KeenEyes.Common and KeenEyes.Spatial?

**A:** Only if you use both `Transform3D` (from Common) and spatial partitioning features (from Spatial, Phase 1+). Most projects will only need `KeenEyes.Common`.

### Q: Will my code break?

**A:** The only breaking change is the namespace. If you update your `using` statements from `KeenEyes.Spatial` to `KeenEyes.Common`, everything will work.

### Q: What happened to KeenEyes.Spatial?

**A:** `KeenEyes.Spatial` still exists but no longer contains `Transform3D`. It will be used for spatial partitioning features (grid, quadtree, octree) in Phase 1+.

### Q: Should I use Transform2D or Transform3D?

**A:** Use `Transform2D` for 2D games where you only need X, Y, and rotation. Use `Transform3D` for 3D games or 2D games that need Z-layering.

## Need Help?

If you encounter issues during migration:

1. Check that all `using KeenEyes.Spatial;` statements referencing `Transform3D` are changed to `using KeenEyes.Common;`
2. Verify your project references `KeenEyes.Common` (via NuGet or project reference)
3. Report issues at: https://github.com/orion-ecs/keen-eye/issues

## What's Next?

Phase 1 will introduce spatial partitioning features (grid-based, quadtree, octree) in the `KeenEyes.Spatial` package. These features will be completely optional and won't affect projects that only use `Transform3D`.
