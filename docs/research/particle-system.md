# Particle System Architecture

This document outlines the architecture for a high-performance particle system in KeenEyes, optimized for rendering thousands of particles efficiently.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Design Philosophy](#design-philosophy)
3. [Architecture Overview](#architecture-overview)
4. [Particle Data Structure](#particle-data-structure)
5. [Emitter Configuration](#emitter-configuration)
6. [Rendering Strategy](#rendering-strategy)
7. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes Particles uses a **data-oriented design** where particles are NOT entities. Instead:
- **Emitters** are entities with configuration components
- **Particles** are pooled data managed by a dedicated system
- **Rendering** uses GPU instancing for maximum throughput

This approach enables 10,000+ particles at 60fps while keeping the ECS clean.

**Key Decision:** Particles are managed data, not ECS entities.

---

## Design Philosophy

### Why NOT Make Particles Entities?

| Aspect | Particles as Entities | Particles as Data |
|--------|----------------------|-------------------|
| Overhead | ~100-200 bytes per entity | ~48-64 bytes per particle |
| Spawn rate | ~1,000/frame max | ~100,000/frame |
| Query cost | O(n) archetype iteration | O(1) array access |
| Memory layout | Scattered archetypes | Contiguous arrays |
| Rendering | Individual draw calls | Batched instancing |

ECS entities have overhead for versioning, component lookup, archetype membership, etc. Particles need to be lightweight and numerous.

### Hybrid Approach

```
ECS World                          Particle System
┌─────────────────┐               ┌─────────────────┐
│ ParticleEmitter │──references──▶│ ParticlePool    │
│ (Entity)        │               │ (Managed Data)  │
│                 │               │                 │
│ - EmitterConfig │               │ - Particle[]    │
│ - Transform     │               │ - ActiveCount   │
│ - EmitterState  │               │ - FreeList      │
└─────────────────┘               └─────────────────┘
```

---

## Architecture Overview

### Project Structure

```
KeenEyes.Particles/
├── KeenEyes.Particles.csproj
├── ParticlePlugin.cs              # IWorldPlugin entry point
│
├── Core/
│   ├── Particle.cs               # Single particle data
│   ├── ParticlePool.cs           # Pool of particles
│   ├── ParticleModule.cs         # Behavior module interface
│   └── ParticleContext.cs        # Extension API
│
├── Components/
│   ├── ParticleEmitter.cs        # Entity emitter marker
│   ├── EmitterConfig.cs          # Emission settings
│   ├── EmitterState.cs           # Runtime state
│   └── EmitterModules.cs         # Attached modules
│
├── Modules/
│   ├── EmissionModule.cs         # Spawn rate/bursts
│   ├── ShapeModule.cs            # Spawn shape (cone, sphere, etc.)
│   ├── VelocityModule.cs         # Initial velocity
│   ├── ColorModule.cs            # Color over lifetime
│   ├── SizeModule.cs             # Size over lifetime
│   ├── RotationModule.cs         # Rotation over lifetime
│   ├── GravityModule.cs          # Gravity/forces
│   ├── NoiseModule.cs            # Turbulence
│   ├── CollisionModule.cs        # World collision
│   └── SubEmitterModule.cs       # Spawn on events
│
├── Systems/
│   ├── ParticleSpawnSystem.cs    # Creates new particles
│   ├── ParticleUpdateSystem.cs   # Updates particle state
│   ├── ParticleRenderSystem.cs   # Submits to renderer
│   └── ParticleCleanupSystem.cs  # Removes dead particles
│
└── Rendering/
    ├── ParticleBatch.cs          # GPU instance batch
    ├── ParticleShader.cs         # Particle shader
    └── ParticleAtlas.cs          # Sprite sheet management
```

---

## Particle Data Structure

### Particle Struct

```csharp
/// <summary>
/// Single particle instance. Kept minimal for cache efficiency.
/// Size: 64 bytes (fits in one cache line)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Particle
{
    // Position and velocity (24 bytes)
    public Vector3 Position;
    public Vector3 Velocity;

    // Visual properties (16 bytes)
    public Color Color;
    public float Size;
    public float Rotation;
    public float RotationSpeed;

    // Lifetime (8 bytes)
    public float Age;
    public float Lifetime;

    // Texture (8 bytes)
    public ushort SpriteIndex;
    public ushort Flags;
    public uint CustomData;        // User-defined

    // State
    public bool IsAlive => Age < Lifetime;
    public float NormalizedAge => Age / Lifetime;
}
```

### Particle Pool

```csharp
public sealed class ParticlePool : IDisposable
{
    private Particle[] particles;
    private int activeCount;
    private int capacity;

    // Separate arrays for better cache performance during updates
    private Vector3[] positions;
    private Vector3[] velocities;
    private float[] ages;

    public ParticlePool(int initialCapacity = 10000)
    {
        capacity = initialCapacity;
        particles = new Particle[capacity];
        positions = new Vector3[capacity];
        velocities = new Vector3[capacity];
        ages = new float[capacity];
    }

    public int Spawn(in ParticleSpawnParams spawn)
    {
        if (activeCount >= capacity)
        {
            // Either grow or reject
            if (!TryGrow())
                return -1;
        }

        int index = activeCount++;

        particles[index] = new Particle
        {
            Position = spawn.Position,
            Velocity = spawn.Velocity,
            Color = spawn.Color,
            Size = spawn.Size,
            Rotation = spawn.Rotation,
            RotationSpeed = spawn.RotationSpeed,
            Age = 0,
            Lifetime = spawn.Lifetime,
            SpriteIndex = spawn.SpriteIndex
        };

        return index;
    }

    public void Update(float deltaTime)
    {
        // Update ages and compact dead particles
        int writeIndex = 0;

        for (int i = 0; i < activeCount; i++)
        {
            particles[i].Age += deltaTime;

            if (particles[i].IsAlive)
            {
                if (writeIndex != i)
                {
                    particles[writeIndex] = particles[i];
                }
                writeIndex++;
            }
        }

        activeCount = writeIndex;
    }

    public ReadOnlySpan<Particle> ActiveParticles => particles.AsSpan(0, activeCount);
}
```

---

## Emitter Configuration

### ParticleEmitter Component

```csharp
[Component]
public partial struct ParticleEmitter
{
    public bool Enabled;
    public bool Playing;
    public ParticleBlendMode BlendMode;
    public int MaxParticles;

    // Pool reference (managed by system)
    internal int PoolIndex;
}

public enum ParticleBlendMode
{
    Alpha,
    Additive,
    Multiply,
    Premultiplied
}
```

### EmitterConfig Component

```csharp
[Component]
public partial struct EmitterConfig
{
    // Lifetime
    public RangeFloat Lifetime;

    // Emission
    public float EmissionRate;        // Particles per second
    public BurstConfig[] Bursts;      // Timed bursts

    // Initial values
    public RangeFloat StartSpeed;
    public RangeFloat StartSize;
    public RangeFloat StartRotation;
    public RangeColor StartColor;

    // Simulation
    public SimulationSpace Space;
    public float GravityMultiplier;
}

public enum SimulationSpace
{
    Local,      // Particles move with emitter
    World       // Particles independent of emitter
}

public readonly record struct RangeFloat(float Min, float Max)
{
    public float Random() => Min + (Max - Min) * RandomF();
    public float Lerp(float t) => Min + (Max - Min) * t;
}

public readonly record struct RangeColor(Color Min, Color Max)
{
    public Color Random() => Color.Lerp(Min, Max, RandomF());
    public Color Lerp(float t) => Color.Lerp(Min, Max, t);
}
```

### Shape Module

```csharp
[Component]
public partial struct EmitterShape
{
    public EmitterShapeType Type;

    // Shape-specific parameters
    public float Radius;
    public float Arc;              // Degrees for cone/arc
    public Vector3 BoxSize;
    public float EdgeThickness;    // For hollow shapes

    // Direction
    public bool RandomDirection;
    public Vector3 Direction;
}

public enum EmitterShapeType
{
    Point,
    Sphere,
    Hemisphere,
    Cone,
    Box,
    Circle,
    Edge,
    Mesh              // Emit from mesh surface
}
```

### Modules Over Lifetime

```csharp
[Component]
public partial struct ColorOverLifetime
{
    public Gradient Gradient;
}

[Component]
public partial struct SizeOverLifetime
{
    public AnimationCurve Curve;
    public float Multiplier;
}

[Component]
public partial struct VelocityOverLifetime
{
    public AnimationCurve SpeedCurve;
    public Vector3 LinearVelocity;
    public Vector3 OrbitalVelocity;
}

[Component]
public partial struct RotationOverLifetime
{
    public AnimationCurve Curve;
    public float Speed;
}
```

### Animation Curves & Gradients

```csharp
public readonly struct AnimationCurve
{
    private readonly Keyframe[] keyframes;

    public float Evaluate(float t)
    {
        // Binary search for keyframes, interpolate
        // ...
    }
}

public readonly struct Gradient
{
    private readonly GradientKey[] colorKeys;
    private readonly GradientKey[] alphaKeys;

    public Color Evaluate(float t)
    {
        // Interpolate color and alpha separately
        // ...
    }
}
```

---

## Rendering Strategy

### GPU Instancing

```csharp
public class ParticleBatch
{
    private readonly IGraphicsBackend graphics;

    // Instance data buffer
    private ParticleInstanceData[] instanceData;
    private nint instanceBuffer;

    // Shared quad mesh
    private static readonly float[] QuadVertices = {
        -0.5f, -0.5f, 0, 0,  // position, uv
         0.5f, -0.5f, 1, 0,
         0.5f,  0.5f, 1, 1,
        -0.5f,  0.5f, 0, 1
    };

    public void Render(ReadOnlySpan<Particle> particles, Matrix4x4 viewProjection)
    {
        if (particles.Length == 0) return;

        // Convert particles to instance data
        for (int i = 0; i < particles.Length; i++)
        {
            ref readonly var p = ref particles[i];
            instanceData[i] = new ParticleInstanceData
            {
                Position = p.Position,
                Size = p.Size,
                Rotation = p.Rotation,
                Color = p.Color,
                UVOffset = GetSpriteUV(p.SpriteIndex)
            };
        }

        // Upload to GPU
        graphics.UpdateBuffer(instanceBuffer, instanceData.AsSpan(0, particles.Length));

        // Draw instanced
        graphics.DrawInstanced(QuadMesh, particles.Length);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct ParticleInstanceData
{
    public Vector3 Position;
    public float Size;
    public float Rotation;
    public Color Color;
    public Vector4 UVOffset;    // x, y, width, height in atlas
}
```

### Particle Shader (GLSL)

```glsl
// Vertex Shader
#version 450

// Per-vertex
layout(location = 0) in vec2 a_Position;
layout(location = 1) in vec2 a_TexCoord;

// Per-instance
layout(location = 2) in vec3 i_Position;
layout(location = 3) in float i_Size;
layout(location = 4) in float i_Rotation;
layout(location = 5) in vec4 i_Color;
layout(location = 6) in vec4 i_UVOffset;

uniform mat4 u_ViewProjection;

out vec2 v_TexCoord;
out vec4 v_Color;

void main()
{
    // Billboard rotation
    float c = cos(i_Rotation);
    float s = sin(i_Rotation);
    vec2 rotated = vec2(
        a_Position.x * c - a_Position.y * s,
        a_Position.x * s + a_Position.y * c
    );

    vec3 worldPos = i_Position + vec3(rotated * i_Size, 0.0);
    gl_Position = u_ViewProjection * vec4(worldPos, 1.0);

    // UV from atlas
    v_TexCoord = i_UVOffset.xy + a_TexCoord * i_UVOffset.zw;
    v_Color = i_Color;
}

// Fragment Shader
#version 450

in vec2 v_TexCoord;
in vec4 v_Color;

uniform sampler2D u_Texture;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(u_Texture, v_TexCoord);
    FragColor = texColor * v_Color;
}
```

---

## Systems

### ParticleSpawnSystem

```csharp
public class ParticleSpawnSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var context = World.GetExtension<ParticleContext>();

        foreach (var entity in World.Query<ParticleEmitter, EmitterConfig, EmitterState, Transform3D>())
        {
            ref readonly var emitter = ref World.Get<ParticleEmitter>(entity);
            if (!emitter.Enabled || !emitter.Playing) continue;

            ref readonly var config = ref World.Get<EmitterConfig>(entity);
            ref var state = ref World.Get<EmitterState>(entity);
            ref readonly var transform = ref World.Get<Transform3D>(entity);

            var pool = context.GetPool(emitter.PoolIndex);

            // Rate-based emission
            state.EmissionAccumulator += config.EmissionRate * deltaTime;

            while (state.EmissionAccumulator >= 1f)
            {
                state.EmissionAccumulator -= 1f;
                SpawnParticle(pool, config, transform, entity);
            }

            // Burst emission
            foreach (var burst in config.Bursts)
            {
                if (state.Time >= burst.Time && state.Time - deltaTime < burst.Time)
                {
                    for (int i = 0; i < burst.Count; i++)
                    {
                        SpawnParticle(pool, config, transform, entity);
                    }
                }
            }

            state.Time += deltaTime;
        }
    }

    private void SpawnParticle(ParticlePool pool, in EmitterConfig config,
                               in Transform3D transform, Entity emitter)
    {
        var spawn = new ParticleSpawnParams
        {
            Position = GetSpawnPosition(emitter, transform),
            Velocity = GetSpawnVelocity(emitter, transform) * config.StartSpeed.Random(),
            Color = config.StartColor.Random(),
            Size = config.StartSize.Random(),
            Rotation = config.StartRotation.Random(),
            RotationSpeed = 0,
            Lifetime = config.Lifetime.Random()
        };

        pool.Spawn(spawn);
    }
}
```

### ParticleUpdateSystem

```csharp
public class ParticleUpdateSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var context = World.GetExtension<ParticleContext>();

        // Process each emitter's particles
        foreach (var entity in World.Query<ParticleEmitter, EmitterConfig>())
        {
            ref readonly var emitter = ref World.Get<ParticleEmitter>(entity);
            var pool = context.GetPool(emitter.PoolIndex);

            ref readonly var config = ref World.Get<EmitterConfig>(entity);

            // Update particles
            var particles = pool.GetParticlesForWrite();
            var gravity = Vector3.UnitY * -9.8f * config.GravityMultiplier;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                if (!p.IsAlive) continue;

                // Physics
                p.Velocity += gravity * deltaTime;
                p.Position += p.Velocity * deltaTime;
                p.Rotation += p.RotationSpeed * deltaTime;
                p.Age += deltaTime;

                // Modules
                ApplyModules(ref p, entity);
            }

            // Compact dead particles
            pool.Compact();
        }
    }

    private void ApplyModules(ref Particle p, Entity emitter)
    {
        float t = p.NormalizedAge;

        if (World.Has<ColorOverLifetime>(emitter))
        {
            ref readonly var module = ref World.Get<ColorOverLifetime>(emitter);
            p.Color = module.Gradient.Evaluate(t);
        }

        if (World.Has<SizeOverLifetime>(emitter))
        {
            ref readonly var module = ref World.Get<SizeOverLifetime>(emitter);
            p.Size *= module.Curve.Evaluate(t) * module.Multiplier;
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure

1. Create `KeenEyes.Particles` project
2. Implement Particle struct and ParticlePool
3. Basic emitter component
4. Simple spawn system

**Milestone:** Spawn particles from entity

### Phase 2: Rendering

1. Implement GPU instancing batch
2. Particle shader
3. ParticleRenderSystem
4. Blend modes (additive, alpha)

**Milestone:** Visible particles on screen

### Phase 3: Emitter Shapes

1. Point, sphere, cone shapes
2. Box, circle, edge shapes
3. Random direction modes
4. Shape gizmos for debugging

**Milestone:** Diverse emission patterns

### Phase 4: Lifetime Modules

1. Color over lifetime
2. Size over lifetime
3. Velocity over lifetime
4. Rotation over lifetime

**Milestone:** Dynamic particle appearance

### Phase 5: Advanced Features

1. Noise/turbulence module
2. Collision module
3. Sub-emitters
4. Texture animation

**Milestone:** Production-ready particles

---

## Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Active particles | 100,000 | With instancing |
| Spawn rate | 50,000/frame | Burst capability |
| Update cost | < 1ms | For 10k particles |
| Memory per particle | 64 bytes | Cache-line aligned |
| Draw calls | 1 per emitter | Batched instancing |

---

## Open Questions

1. **GPU Compute** - Use compute shaders for simulation?
2. **LOD** - Reduce particles at distance?
3. **Culling** - Frustum cull individual particles?
4. **Trails** - Ribbon/trail rendering?
5. **Mesh Particles** - 3D mesh instead of billboards?
6. **Physics Integration** - Interact with KeenEyes.Physics?

---

## Related Issues

- Milestone #18: Particle System
- Issue #422: Create KeenEyes.Particles with core pool and rendering
- Issue #423: Implement particle emitter components and modules
