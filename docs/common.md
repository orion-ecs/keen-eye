# KeenEyes.Common

Common components and utilities shared across KeenEyes ECS plugins.

## Installation

Add a reference to `KeenEyes.Common`:

```xml
<PackageReference Include="KeenEyes.Common" />
```

## Float Extensions

Extension methods for safe floating-point comparisons. Direct equality checks (`==`) with floats are unreliable due to precision limitations.

### Usage

```csharp
using KeenEyes.Common;

// Check if a value is approximately zero
float threshold = 0.0000001f;
if (threshold.IsApproximatelyZero())
{
    // Treat as zero
}

// Compare two floats for near-equality
float a = 1.0f;
float b = 1.0000001f;
if (a.ApproximatelyEquals(b))
{
    // Values are considered equal
}
```

### Methods

| Method | Description |
|--------|-------------|
| `IsApproximatelyZero()` | Returns `true` if the absolute value is less than the default epsilon (1e-6f) |
| `IsApproximatelyZero(float epsilon)` | Returns `true` if the absolute value is less than the specified epsilon |
| `ApproximatelyEquals(float other)` | Returns `true` if the absolute difference is less than the default epsilon |
| `ApproximatelyEquals(float other, float epsilon)` | Returns `true` if the absolute difference is less than the specified epsilon |

### Default Epsilon

The default epsilon value is `1e-6f` (0.000001), which is suitable for most game development scenarios. Access it via:

```csharp
float epsilon = FloatExtensions.DefaultEpsilon; // 1e-6f
```

### Custom Epsilon

For scenarios requiring different precision, use the overloads that accept a custom epsilon:

```csharp
// Physics simulation might need tighter tolerance
const float PhysicsEpsilon = 1e-9f;
if (velocity.IsApproximatelyZero(PhysicsEpsilon))
{
    // Consider at rest
}

// UI positioning might allow looser tolerance
const float UiEpsilon = 0.01f;
if (positionA.ApproximatelyEquals(positionB, UiEpsilon))
{
    // Close enough for UI purposes
}
```

### Common Patterns

**Checking thresholds:**
```csharp
// ❌ BAD: Direct comparison
if (activity.SleepThreshold == 0)

// ✅ GOOD: Tolerance-based
if (activity.SleepThreshold.IsApproximatelyZero())
```

**Comparing calculated values:**
```csharp
// ❌ BAD: May fail due to floating-point errors
float calculated = ComputeValue();
if (calculated == expectedValue)

// ✅ GOOD: Handles floating-point imprecision
if (calculated.ApproximatelyEquals(expectedValue))
```

**Physics systems:**
```csharp
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var velocity = ref World.Get<Velocity>(entity);

            // Check if effectively stopped
            if (velocity.Value.Length().IsApproximatelyZero())
            {
                // Entity is at rest
                continue;
            }

            ref var position = ref World.Get<Position>(entity);
            position.Value += velocity.Value * deltaTime;
        }
    }
}
```

### Edge Cases

The extension methods handle edge cases gracefully:

| Input | `IsApproximatelyZero()` | Notes |
|-------|-------------------------|-------|
| `0f` | `true` | Exact zero |
| `1e-7f` | `true` | Below default epsilon |
| `1e-6f` | `false` | At epsilon boundary (exclusive) |
| `float.NaN` | `false` | NaN comparisons return false |
| `float.PositiveInfinity` | `false` | Infinity is never zero |

## Transform Components

See [Spatial](spatial.md) for 2D and 3D transform components.

## Velocity Components

### Velocity2D

```csharp
using KeenEyes.Common;

var velocity = new Velocity2D(10f, 5f);

// Get speed (magnitude)
float speed = velocity.Magnitude();

// Get squared magnitude (avoids sqrt, good for comparisons)
float speedSquared = velocity.MagnitudeSquared();
```

### Velocity3D

```csharp
using KeenEyes.Common;

var velocity = new Velocity3D(10f, 5f, 3f);

float speed = velocity.Magnitude();
float speedSquared = velocity.MagnitudeSquared();
```

## Spatial Bounds

```csharp
using KeenEyes.Common;

var bounds = new SpatialBounds
{
    Min = new Vector3(-5, -5, -5),
    Max = new Vector3(5, 5, 5)
};

// Check containment, intersection, etc.
```

See [Spatial](spatial.md) and [Spatial Partitioning](spatial-partitioning/getting-started.md) for more details.
