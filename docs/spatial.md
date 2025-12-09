# Spatial Components

The `KeenEyes.Common` library provides common 3D spatial components using `System.Numerics` for SIMD-accelerated math operations.

> **Note**: Transform3D was moved from `KeenEyes.Spatial` to `KeenEyes.Common` to avoid forcing dependencies on spatial partitioning features. Update your using statements to `using KeenEyes.Common;`.

## Transform3D

The primary component for 3D positioning, rotation, and scaling:

```csharp
using KeenEyes.Common;
using System.Numerics;

// Create with explicit values
var transform = new Transform3D(
    position: new Vector3(10, 5, 0),
    rotation: Quaternion.Identity,
    scale: Vector3.One
);

// Use identity (origin, no rotation, unit scale)
var transform = Transform3D.Identity;
```

### Properties

```csharp
public struct Transform3D : IComponent
{
    public Vector3 Position;      // World position
    public Quaternion Rotation;   // Rotation as quaternion
    public Vector3 Scale;         // Scale factors
}
```

### Direction Vectors

Get orientation-relative direction vectors:

```csharp
ref var transform = ref world.Get<Transform3D>(entity);

Vector3 forward = transform.Forward;  // -Z axis after rotation
Vector3 right = transform.Right;      // +X axis after rotation
Vector3 up = transform.Up;            // +Y axis after rotation
```

### Transformation Matrix

Compute the 4x4 world matrix (Scale * Rotation * Translation):

```csharp
Matrix4x4 worldMatrix = transform.ToMatrix();
```

## Usage with ECS

### Entity Creation

```csharp
using KeenEyes;
using KeenEyes.Common;

var entity = world.Spawn()
    .With(new Transform3D(
        new Vector3(0, 0, 0),
        Quaternion.Identity,
        Vector3.One))
    .Build();
```

### Movement System

```csharp
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var velocity = ref World.Get<Velocity>(entity);

            transform.Position += velocity.Value * deltaTime;
        }
    }
}
```

### Rotation

```csharp
// Rotate around Y axis
ref var transform = ref world.Get<Transform3D>(entity);
transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

// Look at a target
var direction = Vector3.Normalize(target - transform.Position);
transform.Rotation = Quaternion.CreateFromRotationMatrix(
    Matrix4x4.CreateLookAt(Vector3.Zero, direction, Vector3.UnitY));

// Euler angles (yaw, pitch, roll)
transform.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
```

### Scaling

```csharp
ref var transform = ref world.Get<Transform3D>(entity);

// Uniform scale
transform.Scale = new Vector3(2, 2, 2);

// Non-uniform scale
transform.Scale = new Vector3(1, 2, 0.5f);
```

## Integration with Graphics

Transform3D is used by the graphics plugin for positioning cameras, lights, and renderables:

```csharp
using KeenEyes.Graphics;
using KeenEyes.Common;

// Camera with transform
world.Spawn()
    .With(new Transform3D(new Vector3(0, 5, 10), Quaternion.Identity, Vector3.One))
    .With(Camera.CreatePerspective(60f, 16f/9f, 0.1f, 1000f))
    .WithTag<MainCameraTag>()
    .Build();

// Renderable object with transform
world.Spawn()
    .With(Transform3D.Identity)
    .With(new Renderable(meshId, 0))
    .With(Material.Default)
    .Build();

// Light with transform (direction from rotation)
world.Spawn()
    .With(new Transform3D(Vector3.Zero,
        Quaternion.CreateFromYawPitchRoll(0, -0.5f, 0), Vector3.One))
    .With(Light.Directional(new Vector3(1, 1, 1), 1f))
    .Build();
```

## Performance Notes

- Uses `System.Numerics` types for SIMD acceleration
- `ToMatrix()` computes the matrix on-demand (not cached)
- For hot paths accessing only position, consider separate `Position` and `Rotation` components

### Component Splitting

If you frequently access only subsets of transform data, consider splitting:

```csharp
// Separate components for cache efficiency
public struct Position3D : IComponent
{
    public Vector3 Value;
}

public struct Rotation3D : IComponent
{
    public Quaternion Value;
}

public struct Scale3D : IComponent
{
    public Vector3 Value;
}

// Query only what you need
foreach (var entity in world.Query<Position3D, Velocity>())
{
    // Only Position3D and Velocity in cache
}
```

## System.Numerics Types

Transform3D uses these SIMD-accelerated types:

| Type | Description |
|------|-------------|
| `Vector3` | 3D position/direction/scale |
| `Quaternion` | Rotation (avoids gimbal lock) |
| `Matrix4x4` | 4x4 transformation matrix |

Common operations:

```csharp
// Vector math
var sum = v1 + v2;
var scaled = v * 2f;
var normalized = Vector3.Normalize(v);
var dot = Vector3.Dot(v1, v2);
var cross = Vector3.Cross(v1, v2);
var distance = Vector3.Distance(a, b);
var lerped = Vector3.Lerp(a, b, t);

// Quaternion operations
var combined = q1 * q2;
var inverted = Quaternion.Inverse(q);
var slerped = Quaternion.Slerp(a, b, t);
var fromAxis = Quaternion.CreateFromAxisAngle(axis, angle);
var fromEuler = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);

// Transform a point by rotation
var rotated = Vector3.Transform(point, rotation);
```
