# Math Libraries for Game Development - Research Report

**Date:** December 2024
**Purpose:** Evaluate math libraries for game engine development focusing on graphics-oriented operations (vectors, matrices, quaternions)

## Executive Summary

After evaluating System.Numerics, Silk.NET.Maths, OpenTK.Mathematics, and GlmSharp, **System.Numerics is the recommended primary choice** for a custom game engine due to its built-in SIMD acceleration, zero dependencies, and runtime integration. For features not available in System.Numerics (such as integer vectors or Matrix3x3), **Silk.NET.Maths provides the best supplementary option** given its active maintenance and integration with the Silk.NET graphics ecosystem.

---

## Library Comparison

### System.Numerics

| Attribute | Value |
|-----------|-------|
| **Package** | Built into .NET Runtime |
| **Latest Version** | Ships with .NET 10 |
| **License** | MIT |
| **Dependencies** | None (runtime included) |

**Strengths:**
- **SIMD-accelerated** - Hardware intrinsics for Vector2/3/4, Matrix4x4, Quaternion, Plane
- **Zero allocation overhead** - All types are structs with value semantics
- **No external dependencies** - Part of the runtime, always available
- **JIT-optimized** - Runtime generates optimal code for the target CPU
- **Widely understood** - Standard .NET type, extensive documentation

**Weaknesses:**
- **Limited type coverage** - No Matrix3x3, no integer vectors
- **No Euler angles** - Must use Quaternion or manually implement
- **DirectX-style conventions** - Functions designed for Direct3D clip space
- **Row-vector multiplication** - Uses `v * M` convention, requires attention when interfacing with OpenGL

**Available Types:**
```csharp
System.Numerics.Vector2      // 2D float vector
System.Numerics.Vector3      // 3D float vector
System.Numerics.Vector4      // 4D float vector
System.Numerics.Matrix3x2    // 2D transformation matrix
System.Numerics.Matrix4x4    // 4x4 transformation matrix
System.Numerics.Quaternion   // Rotation quaternion
System.Numerics.Plane        // 3D plane
```

**API Example:**
```csharp
using System.Numerics;

// Create transformation matrices
var translation = Matrix4x4.CreateTranslation(10, 5, 0);
var rotation = Matrix4x4.CreateRotationZ(MathF.PI / 4);
var scale = Matrix4x4.CreateScale(2.0f);

// Compose transformations (row-vector convention: scale * rotate * translate)
var world = scale * rotation * translation;

// Transform a point
var point = new Vector3(1, 0, 0);
var transformed = Vector3.Transform(point, world);

// Create view and projection
var view = Matrix4x4.CreateLookAt(
    cameraPosition: new Vector3(0, 0, 10),
    cameraTarget: Vector3.Zero,
    cameraUpVector: Vector3.UnitY);

var projection = Matrix4x4.CreatePerspectiveFieldOfView(
    fieldOfView: MathF.PI / 4,
    aspectRatio: 16f / 9f,
    nearPlaneDistance: 0.1f,
    farPlaneDistance: 100f);
```

---

### Silk.NET.Maths

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [Silk.NET.Maths](https://www.nuget.org/packages/Silk.NET.Maths) |
| **Latest Version** | 2.22.0 (November 2024) |
| **Total Downloads** | 1.5M |
| **License** | MIT |

**Strengths:**
- **Generic types** - `Vector3D<T>`, `Matrix4X4<T>` work with float, double, int, etc.
- **Active development** - Regular releases, .NET Foundation backed
- **Silk.NET integration** - Seamless with Silk.NET.OpenGL bindings
- **Integer support** - `Vector2D<int>` for grid-based systems
- **Mobile support** - iOS and Android compatible

**Weaknesses:**
- **No SIMD optimization** - Maintainers explicitly recommend System.Numerics for performance
- **Supplementary role** - Designed to extend System.Numerics, not replace it
- **Extra dependency** - Adds package reference and transitive dependencies

**Key Insight from Maintainers:**
> "Avoid Silk.NET.Maths if you can avoid it. System.Numerics is faster and will always be faster than non-SIMD Silk.NET.Maths."

**API Example:**
```csharp
using Silk.NET.Maths;

// Generic vectors - useful for integer grid systems
Vector2D<int> gridPos = new(5, 10);
Vector3D<float> position = new(1.0f, 2.0f, 3.0f);

// Convert to System.Numerics for performance-critical operations
System.Numerics.Vector3 sysVec = position.ToSystem();

// Matrix operations
var transform = Matrix4X4.CreateScale(2.0f) *
                Matrix4X4.CreateRotationZ(0.5f) *
                Matrix4X4.CreateTranslation(10f, 0f, 0f);
```

---

### OpenTK.Mathematics

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [OpenTK.Mathematics](https://www.nuget.org/packages/OpenTK.Mathematics) |
| **Latest Version** | 4.9.4 (March 2025) |
| **Total Downloads** | 7.1M |
| **License** | MIT |

**Strengths:**
- **Comprehensive types** - Vector2/3/4, Matrix2/3/4, Quaternion, Box, Color4
- **OpenGL-friendly API** - Methods like `LookAt`, `CreatePerspectiveFieldOfView`
- **Battle-tested** - 15+ years of development, widely used
- **Excellent documentation** - Extensive tutorials and API docs
- **Matrix3 support** - Useful for normal matrix calculations

**Weaknesses:**
- **Desktop only** - No mobile platform support
- **Row naming confusion** - `Row0`-`Row3` fields actually represent columns in OpenGL terms
- **Tightly coupled** - Designed for OpenTK ecosystem
- **Community maintained** - No foundation backing

**Memory Layout Note:**
OpenTK's Matrix4 uses row-major storage with fields named `Row0`, `Row1`, `Row2`, `Row3`. When passing to OpenGL via `GL.UniformMatrix4`, this layout is compatible with OpenGL's column-major expectation when using `transpose: false` because OpenGL interprets the sequential memory as columns.

**API Example:**
```csharp
using OpenTK.Mathematics;

// Create transformation
var model = Matrix4.CreateScale(2.0f) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(45)) *
            Matrix4.CreateTranslation(10, 5, 0);

// Camera setup
var view = Matrix4.LookAt(
    eye: new Vector3(0, 0, 10),
    target: Vector3.Zero,
    up: Vector3.UnitY);

var projection = Matrix4.CreatePerspectiveFieldOfView(
    fovy: MathHelper.DegreesToRadians(45),
    aspect: 16f / 9f,
    depthNear: 0.1f,
    depthFar: 100f);

// Upload to shader (transpose: false for OpenTK matrices)
GL.UniformMatrix4(location, false, ref model);

// Convert to/from System.Numerics
System.Numerics.Matrix4x4 sysMatrix = (System.Numerics.Matrix4x4)model;
Matrix4 opentkMatrix = (Matrix4)sysMatrix;
```

---

### GlmSharp

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [GlmSharp](https://www.nuget.org/packages/GlmSharp) |
| **Latest Version** | 0.9.8 (June 2016) |
| **Total Downloads** | 95.6K |
| **License** | MIT |

**Strengths:**
- **GLM-compatible API** - Familiar to C++ developers using GLM
- **Swizzling support** - `v.swizzle.bgr`, `v.zw = v.yx`
- **Wide type support** - int, uint, long, float, double, decimal, Complex, bool
- **No dependencies** - Pure C# implementation
- **Non-square matrices** - mat2x3, mat3x4, etc.

**Weaknesses:**
- **Not maintained** - Last update June 2016 (8+ years ago)
- **No SIMD** - Pure managed code, no hardware acceleration
- **No modern .NET features** - Targets .NET 2.0, misses modern optimizations
- **Low adoption** - 95K downloads vs millions for alternatives

**Not Recommended** due to maintenance status. Use System.Numerics + Silk.NET.Maths instead.

**API Example (for reference):**
```csharp
using GlmSharp;

vec3 v = new vec3(1, 2, 3);
mat4 view = mat4.LookAt(vec3.Ones, vec3.Zero, vec3.UnitY);

// Swizzling
vec2 xy = v.swizzle.xy;
v.swizzle.xy = v.swizzle.yx;  // Swap x and y
```

---

## Memory Layout and OpenGL Compatibility

### The Row-Major vs Column-Major Question

This is one of the most confusing aspects of graphics math. Here's the practical reality:

| Library | Storage Order | Multiplication Convention | OpenGL Upload |
|---------|---------------|---------------------------|---------------|
| **System.Numerics** | Column-major* | Row-vector (`v * M`) | `transpose: true` |
| **OpenTK.Mathematics** | Row-major | Column-vector (`M * v`)** | `transpose: false` |
| **Silk.NET.Maths** | Matches System.Numerics | Row-vector (`v * M`) | `transpose: true` |
| **GlmSharp** | Column-major | Column-vector (`M * v`) | `transpose: false` |

*System.Numerics stores data for SIMD efficiency in column-major order but uses row-vector multiplication semantics.

**OpenTK uses `model * view * projection` order despite row-major storage.

### Practical OpenGL Integration

**With System.Numerics:**
```csharp
// Option 1: Transpose on upload
GL.UniformMatrix4fv(location, 1, true, ref matrix.M11);

// Option 2: Reverse multiplication in shader
// Instead of: gl_Position = projection * view * model * vec4(position, 1.0);
// Use:        gl_Position = vec4(position, 1.0) * model * view * projection;
```

**With OpenTK.Mathematics:**
```csharp
// Direct upload without transpose
GL.UniformMatrix4(location, false, ref matrix);
// Shader uses standard: gl_Position = projection * view * model * vec4(position, 1.0);
```

### Translation Component Location

When you need to extract or verify translation:

| Library | Translation Access |
|---------|-------------------|
| System.Numerics | `matrix.M41, M42, M43` (row 4) |
| OpenTK.Mathematics | `matrix.Row3.Xyz` (actually column in OpenGL terms) |
| GlmSharp | `matrix.Column3.xyz` |

---

## Performance Considerations

### SIMD Acceleration

Only **System.Numerics** provides true hardware SIMD acceleration in standard .NET:

```
Vector<T>.Count at runtime:
- Vector<float>: 4 (SSE) or 8 (AVX)
- Vector<double>: 2 (SSE) or 4 (AVX)
- Vector<byte>: 16 (SSE) or 32 (AVX)
```

The JIT compiler generates optimal SIMD instructions (SSE2/AVX/AVX2) based on the CPU at runtime.

### Benchmark Expectations

Based on general principles (specific benchmarks vary by workload):

| Operation | System.Numerics | Other Libraries |
|-----------|-----------------|-----------------|
| Matrix4x4 multiply | ~4 SIMD ops | ~64 scalar ops |
| Vector3 normalize | SIMD rsqrt | Scalar sqrt |
| Quaternion slerp | SIMD optimized | Scalar |
| Dot product | Single SIMD op | 3 multiplies + 2 adds |

### Memory Efficiency

All evaluated libraries use value types (structs) with sequential layout:

```csharp
// All are blittable and GPU-upload friendly
sizeof(Vector3) = 12 bytes
sizeof(Vector4) = 16 bytes
sizeof(Matrix4x4) = 64 bytes
sizeof(Quaternion) = 16 bytes
```

---

## API Ergonomics Comparison

### Creating a View-Projection Matrix

**System.Numerics:**
```csharp
var view = Matrix4x4.CreateLookAt(cameraPos, target, Vector3.UnitY);
var proj = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
var viewProj = view * proj;  // Row-vector order
```

**OpenTK.Mathematics:**
```csharp
var view = Matrix4.LookAt(cameraPos, target, Vector3.UnitY);
var proj = Matrix4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
var viewProj = proj * view;  // Column-vector order (reversed)
```

**Silk.NET.Maths:**
```csharp
var view = Matrix4X4.CreateLookAt(cameraPos, target, Vector3D<float>.UnitY);
var proj = Matrix4X4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
var viewProj = view * proj;  // Same as System.Numerics
```

### Rotating a Vector

**System.Numerics:**
```csharp
var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
var rotated = Vector3.Transform(point, rotation);
```

**OpenTK.Mathematics:**
```csharp
var rotation = Quaternion.FromAxisAngle(Vector3.UnitY, angle);
var rotated = Vector3.Transform(point, rotation);
// Or: rotation * point (operator overload)
```

### Missing Features

| Feature | System.Numerics | OpenTK | Silk.NET.Maths | GlmSharp |
|---------|-----------------|--------|----------------|----------|
| Matrix3x3 | ❌ | ✅ | ✅ | ✅ |
| Integer vectors | ❌ | ✅ (Vector2i) | ✅ | ✅ |
| Euler angles | ❌ | ✅ | ✅ | ✅ |
| Swizzling | ❌ | ❌ | ❌ | ✅ |
| Color types | ❌ | ✅ (Color4) | ❌ | ❌ |
| Bounding boxes | ❌ | ✅ (Box2/3) | ✅ | ❌ |

---

## Decision Matrix

| Criteria | Weight | System.Numerics | Silk.NET.Maths | OpenTK.Math | GlmSharp |
|----------|--------|-----------------|----------------|-------------|----------|
| **Performance** | High | 10 | 6 | 7 | 5 |
| **Maintenance** | High | 10 | 9 | 8 | 2 |
| **Type Coverage** | High | 6 | 8 | 9 | 9 |
| **OpenGL Compat** | High | 7 | 7 | 9 | 8 |
| **Dependencies** | Medium | 10 | 7 | 8 | 9 |
| **Documentation** | Medium | 9 | 7 | 9 | 5 |
| **Mobile Support** | Medium | 10 | 9 | 3 | 6 |
| **Weighted Score** | - | **8.7** | **7.5** | **7.6** | **5.9** |

*Scores: 1-10, higher is better*

---

## Recommendations

### Primary Recommendation: System.Numerics + Silk.NET.Maths

For the KeenEyes ECS and custom game engine development:

1. **Use System.Numerics as the primary math library**
   - SIMD acceleration for all hot-path operations
   - Zero dependencies, always available
   - Best performance for bulk ECS operations

2. **Use Silk.NET.Maths for gaps**
   - Integer vectors for grid systems: `Vector2D<int>`
   - Generic math when needed: `Matrix4X4<T>`
   - Seamless integration with Silk.NET graphics

3. **Handle OpenGL integration explicitly**
   ```csharp
   // Create a helper for shader uploads
   public static void UploadMatrix(int location, ref Matrix4x4 matrix)
   {
       GL.UniformMatrix4fv(location, 1, true, ref matrix.M11);  // transpose: true
   }
   ```

### Alternative: OpenTK.Mathematics

Consider **OpenTK.Mathematics** if:
- Desktop-only development is acceptable
- Already using OpenTK for windowing/graphics
- Prefer `Matrix3` and `Color4` included
- Want OpenGL-native conventions without transpose handling

### Avoid: GlmSharp

**Do not use GlmSharp** due to:
- 8+ years without updates
- No SIMD optimization
- Low community adoption
- Better alternatives available

---

## Integration Notes for KeenEyes

### Component Design

```csharp
using System.Numerics;

[Component]
public partial struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}

[Component]
public partial struct Velocity
{
    public Vector3 Linear;
    public Vector3 Angular;
}

// For grid-based systems, use Silk.NET.Maths
using Silk.NET.Maths;

[Component]
public partial struct GridPosition
{
    public Vector2D<int> Cell;
}
```

### System Implementation

```csharp
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform, Velocity>())
        {
            ref var transform = ref World.Get<Transform>(entity);
            ref readonly var velocity = ref World.Get<Velocity>(entity);

            // SIMD-accelerated operations
            transform.Position += velocity.Linear * deltaTime;
            transform.Rotation = Quaternion.Concatenate(
                transform.Rotation,
                Quaternion.CreateFromAxisAngle(
                    Vector3.Normalize(velocity.Angular),
                    velocity.Angular.Length() * deltaTime));
        }
    }
}
```

### Matrix Composition Helper

```csharp
public static class TransformExtensions
{
    public static Matrix4x4 ToMatrix(this Transform transform)
    {
        return Matrix4x4.CreateScale(transform.Scale) *
               Matrix4x4.CreateFromQuaternion(transform.Rotation) *
               Matrix4x4.CreateTranslation(transform.Position);
    }

    public static Matrix4x4 ToNormalMatrix(this Transform transform)
    {
        // For normal transformation, use inverse-transpose of upper-left 3x3
        if (Matrix4x4.Invert(transform.ToMatrix(), out var inverse))
        {
            return Matrix4x4.Transpose(inverse);
        }
        return Matrix4x4.Identity;
    }
}
```

---

## Research Task Checklist

### Completed
- [x] Evaluate System.Numerics capabilities
- [x] Evaluate Silk.NET.Maths
- [x] Evaluate OpenTK.Mathematics
- [x] Evaluate GlmSharp
- [x] Compare memory layouts
- [x] Document OpenGL interop requirements
- [x] Assess SIMD utilization
- [x] Evaluate API ergonomics

### Future Investigation
- [ ] Benchmark common operations (matrix multiply, normalize, cross product)
- [ ] Test actual GPU upload performance with different transpose options
- [ ] Prototype frustum culling with System.Numerics Plane type
- [ ] Evaluate ray-AABB intersection implementations

---

## Sources

- [SIMD-accelerated types in .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/simd)
- [System.Numerics.Matrix4x4 - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.matrix4x4)
- [Silk.NET.Maths NuGet](https://www.nuget.org/packages/Silk.NET.Maths)
- [Silk.NET.Maths vs System.Numerics Discussion](https://github.com/dotnet/Silk.NET/discussions/1602)
- [OpenTK.Mathematics API](https://opentk.net/api/OpenTK.Mathematics.html)
- [OpenTK Matrix4 Documentation](https://opentk.net/api/OpenTK.Mathematics.Matrix4.html)
- [OpenTK Transformations Tutorial](https://opentk.net/learn/chapter1/7-transformations.html)
- [OpenTK Matrix Row/Column Issue](https://github.com/opentk/opentk/issues/512)
- [GlmSharp GitHub](https://github.com/Philip-Trettner/GlmSharp)
- [GlmSharp NuGet](https://www.nuget.org/packages/GlmSharp)
- [OpenGL Matrix Conventions](https://songho.ca/opengl/gl_matrix.html)
- [Sending Matrices to OpenGL](https://austinmorlan.com/posts/opengl_matrices/)
- [System.Numerics Transposed Notation Discussion](https://stackoverflow.com/questions/71588099/why-does-system-numerics-matrix4x4-use-a-transposed-notation)
- [Row-Major vs Column-Major in OpenGL](https://stackoverflow.com/questions/17717600/confusion-between-c-and-opengl-matrix-order-row-major-vs-column-major)
