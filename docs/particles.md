# Particles

The `KeenEyes.Particles` library provides a high-performance particle system for visual effects such as fire, smoke, explosions, and magic effects, installed into a `World` via `ParticlesPlugin`.

## Overview

Particles are **not** individual ECS entities. An emitter is an entity carrying a `ParticleEmitter` component (and, optionally, a `ParticleEmitterModifiers` component), while the actual particles it spawns are pooled data managed internally by `ParticleManager`. This keeps thousands of particles cheap to spawn and update, since they never go through archetype storage or component lookups - they live in a `ParticlePool` using a Structure-of-Arrays (SOA) layout indexed by a free list.

An emitter's world position is read from the entity's `Transform2D` component (or `Transform3D`, which is projected to 2D) each frame.

## Quick Start

### Installation

```csharp
using KeenEyes.Particles;

using var world = new World();

// Install with default configuration
world.InstallPlugin(new ParticlesPlugin());

// Or with custom configuration
world.InstallPlugin(new ParticlesPlugin(new ParticlesConfig
{
    MaxParticlesPerEmitter = 5000,
    MaxEmitters = 50
}));
```

`ParticlesPlugin` registers two components (`ParticleEmitter`, `ParticleEmitterModifiers`) and three systems:

| System | Phase | Order | Responsibility |
|--------|-------|-------|-----------------|
| `ParticleSpawnSystem` | `SystemPhase.Update` | 100 | Spawns new particles from continuous emission and bursts |
| `ParticleUpdateSystem` | `SystemPhase.Update` | 101 | Ages particles, applies modifiers, integrates position/rotation, releases dead particles |
| `ParticleRenderSystem` | `SystemPhase.Render` | 100 | Batches active particles by `BlendMode` and draws them via `I2DRenderer` |

It also exposes a `ParticleManager` as a world extension, retrievable with `world.GetExtension<ParticleManager>()`.

### Creating an Emitter

```csharp
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

var fire = world.Spawn()
    .With(new Transform2D(new Vector2(400, 300), 0, Vector2.One))
    .With(ParticleEmitter.Default with
    {
        EmissionRate = 100,
        StartColor = new Vector4(1, 0.5f, 0, 1),
        BlendMode = BlendMode.Additive
    })
    .With(ParticleEmitterModifiers.WithFadeOut(new Vector4(1, 0.3f, 0, 1)))
    .Build();
```

Adding a `ParticleEmitter` component fires `World.OnComponentAdded<ParticleEmitter>`, which `ParticlesPlugin` uses to register the entity with `ParticleManager` and allocate its `ParticlePool`. Removing the component, or destroying the entity, unregisters it and disposes the pool.

## Core Concepts

### `ParticleEmitter`

`ParticleEmitter` is a `[Component]` struct describing how an emitter spawns particles:

- **Continuous emission** - `EmissionRate` (particles per second); set to `0` to disable.
- **Burst emission** - `BurstCount` particles every `BurstInterval` seconds; `BurstInterval = 0` fires a single one-shot burst instead of repeating.
- **Spawn ranges** - `LifetimeMin`/`LifetimeMax`, `StartSizeMin`/`StartSizeMax`, `StartSpeedMin`/`StartSpeedMax`, `StartRotationMin`/`StartRotationMax` are all sampled uniformly at random per particle.
- **Shape** - `Shape` (an `EmissionShape`) controls where particles spawn and their initial direction.
- **Space** - `Space` (a `ParticleSpace`) selects world- or local-space simulation (see [Simulation Space](#simulation-space-world-vs-local) below); defaults to `ParticleSpace.World`.
- **Visuals** - `Texture` (a `TextureHandle`; particles render as filled circles when it's not valid), `StartColor`, `BlendMode`, and `TextureSheetColumns`/`TextureSheetRows` for sprite-sheet animation (see [Texture Sheet Animation](#texture-sheet-animation) below).
- **Playback** - `IsPlaying` toggles emission on and off without removing the component.

`ParticleEmitter.Default` provides sensible starting values, and `ParticleEmitter.Burst(count, lifetime)` / `ParticleEmitter.Continuous(rate, lifetime)` are convenience factories for the two emission modes.

`EmissionShape` supports these shapes via static factories:

- `EmissionShape.Point` - all particles emit from the emitter origin.
- `EmissionShape.Sphere(radius)` - a filled disc (the sphere flattened to 2D); direction is radial outward.
- `EmissionShape.Cone(radius, angle)` / `EmissionShape.Cone(radius, angle, direction)` - a spread arc around `direction`.
- `EmissionShape.Box(width, height)` - a filled rectangle; direction is random.
- `EmissionShape.Hemisphere(radius)` / `EmissionShape.Hemisphere(radius, direction)` - **2D interpretation:** a filled half-disc. Positions and initial directions span the 180-degree arc centered on `direction` (default `Vector2.UnitY`), mirroring how `Sphere` flattens a sphere to a disc.
- `EmissionShape.Edge(length)` / `EmissionShape.Edge(extent)` - a straight line segment centered on the emitter (spanning `-extent/2` to `+extent/2`; `Edge(length)` lies along the X axis). Direction is random. The extent is stored in `Size`.
- `EmissionShape.Circle(radius)` - the **perimeter** of a ring (not a filled disc): positions lie exactly at distance `radius`, with an outward radial direction.

#### Simulation Space (World vs Local)

`ParticleEmitter.Space` chooses the coordinate space particles are simulated in:

- `ParticleSpace.World` (default) - particles spawn at the emitter's current world position and are stored in world coordinates. Once spawned they are independent, so moving the emitter afterwards leaves existing particles where they are. This is the original behavior.
- `ParticleSpace.Local` - particles are stored relative to the emitter. Their world position is resolved each frame by adding the emitter's current position, so moving the emitter carries all of its live particles with it. Velocity is still integrated in the emitter's local frame.

#### Texture Sheet Animation

Set `TextureSheetColumns` and `TextureSheetRows` to treat `Texture` as a grid of animation frames laid out left-to-right, top-to-bottom. When `TextureSheetColumns * TextureSheetRows` is greater than 1, `ParticleRenderSystem` selects the frame from each particle's normalized age: frame `0` at spawn advancing to the final frame at end of life (`frame = clamp((int)(normalizedAge * frameCount), 0, frameCount - 1)`), and draws only that frame's UV sub-rectangle. A value of `0` or `1` for either dimension disables sheet animation and draws the whole texture.

```csharp
var explosion = ParticleEffects.Explosion() with
{
    Texture = explosionSheet, // e.g. a 4x4 grid of 16 frames
    TextureSheetColumns = 4,
    TextureSheetRows = 4
};
```

### `ParticleEmitterModifiers`

`ParticleEmitterModifiers` is an optional `[Component]` struct that changes particle properties over their lifetime. Each group of fields is gated by a `bool` flag so it only runs when explicitly enabled. During `ParticleUpdateSystem.Update`, modifiers are applied in this order: gravity → velocity over lifetime → size over lifetime → color over lifetime → rotation over lifetime.

```csharp
var modifiers = new ParticleEmitterModifiers
{
    HasGravity = true,
    GravityX = 0f,
    GravityY = 98f, // Downward gravity
    Drag = 0.3f,
    HasColorOverLifetime = true,
    ColorGradient = ParticleGradient.FadeOut(new Vector4(1, 0.5f, 0, 1))
};
```

`ParticleEmitterModifiers.None` is an all-disabled baseline, and `WithGravity(gravityY, drag)` / `WithFadeOut(startColor)` build on top of it for common cases.

Curves and gradients (`VelocityCurve`, `SizeCurve`, `RotationCurve`, `ColorGradient`) are evaluated with a normalized age in `[0, 1]`:

- `ParticleCurve` - a 64-sample lookup table with factories `Constant(value)`, `LinearFadeIn()`, `LinearFadeOut()`, `EaseIn()`, `EaseOut()`, and `FromPoints(points)` for arbitrary control points.
- `ParticleGradient` - the same idea for `Vector4` colors, with `Constant(color)`, `FadeIn(color)`, `FadeOut(color)`, `TwoColor(start, end)`, and `FromPoints(points)`.

Both are pre-sampled into fixed-size arrays at construction time so `Evaluate(t)` is a cheap linear interpolation - no allocations or reflection on the hot path.

### `ParticleEffects`

`ParticleEffects` is a static factory class with ready-made emitter/modifier pairs for common effects: `Fire()`/`FireModifiers()`, `Smoke()`/`SmokeModifiers()`, `Explosion()`/`ExplosionModifiers()`, `MagicSparkles()`/`MagicSparklesModifiers()`, `BloodSplatter()`/`BloodSplatterModifiers()`, and `Rain()`/`RainModifiers()`, `Snow()`/`SnowModifiers()`. Use these as starting points and customize with `with` expressions:

```csharp
using KeenEyes.Particles;

var entity = world.Spawn()
    .With(new Transform2D(position, 0, Vector2.One))
    .With(ParticleEffects.Fire())
    .With(ParticleEffects.FireModifiers())
    .Build();
```

### `ParticleManager`

`ParticleManager` is the world extension that owns every emitter's `ParticlePool`:

- `EmitterCount` - number of currently registered emitters.
- `TotalActiveParticles` - sum of `ActiveCount` across all pools.
- `GetPool(entity)` - returns the `ParticlePool?` for an emitter entity, or `null` if it isn't registered.
- `HasPool(entity)` - checks registration without allocating.
- `GetAllPools()` - enumerates `(Entity, ParticlePool)` pairs for every active emitter.
- `ClearAll()` - clears every pool's particles without removing the emitters themselves.
- `Config` - the `ParticlesConfig` the manager was created with.

```csharp
var particles = world.GetExtension<ParticleManager>();
int totalActive = particles.TotalActiveParticles;
```

### `ParticlesConfig`

`ParticlesConfig` caps resource usage across the whole world:

| Property | Default | Effect |
|----------|---------|--------|
| `MaxParticlesPerEmitter` | 1000 | Once a pool hits this size, new particles aren't spawned until existing ones expire |
| `MaxEmitters` | 100 | Additional emitters beyond this limit are silently ignored |
| `InitialPoolCapacity` | 256 | Starting array size per pool; grows dynamically (doubling, via `ParticlePool.Grow`) up to `MaxParticlesPerEmitter` |

`ParticlesConfig.Default`, `ParticlesConfig.HighPerformance` (5000/50/1024), and `ParticlesConfig.LowMemory` (200/20/64) cover common presets. `Validate()` returns a descriptive error string (or `null`) and is called automatically by the `ParticlesPlugin(ParticlesConfig)` constructor, which throws `ArgumentException` for an invalid configuration.

## Performance

- Particles live in `ParticlePool`'s parallel arrays (`PositionsX`/`PositionsY`, `VelocitiesX`/`VelocitiesY`, `ColorsR/G/B/A`, `Sizes`, `Rotations`, `Ages`, `Lifetimes`, etc.), not as ECS entities, so spawning and updating thousands of particles avoids archetype moves and per-particle component lookups entirely.
- Allocation and release use an O(1) free list (`ParticlePool.Allocate`/`Release`); pools grow by doubling capacity on demand rather than reallocating per particle.
- `ParticleRenderSystem` groups active emitters by `BlendMode` before rendering, issuing one `I2DRenderer.Begin()`/`End()` batch per blend mode (rendered in multiply → transparent → premultiplied → additive order) instead of one draw call per particle.
- `ParticleCurve` and `ParticleGradient` pre-sample 64 points at construction, so evaluating them during `ParticleUpdateSystem` is a constant-time lookup and lerp with no per-frame allocation.

## Next Steps

- [Plugins Guide](plugins.md) - How `IWorldPlugin` and `IPluginContext` work in general
- [Systems Guide](systems.md) - System phases, ordering, and execution
- [Components Guide](components.md) - The `[Component]` attribute and generated builder methods
- [Graphics Guide](graphics.md) - `I2DRenderer`, `TextureHandle`, and rendering primitives used by `ParticleRenderSystem`
- [Particle System Design](research/particle-system.md) - Original design document (note: some sections describe an aspirational GPU-instanced/module-based design that differs from the current CPU-pooled implementation described above)
