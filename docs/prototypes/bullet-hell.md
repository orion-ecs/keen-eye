# Bullet Hell Prototype

A fast-paced shooter demonstrating KeenEyes' ability to handle thousands of entities with high spawn/despawn rates and efficient bulk processing.

---

## Table of Contents

1. [Overview](#overview)
2. [Why This Showcases ECS](#why-this-showcases-ecs)
3. [Gameplay Design](#gameplay-design)
4. [Component Architecture](#component-architecture)
5. [Systems Design](#systems-design)
6. [Bullet Patterns](#bullet-patterns)
7. [Collision Strategy](#collision-strategy)
8. [Visual Effects](#visual-effects)
9. [Implementation Plan](#implementation-plan)

---

## Overview

**Genre:** Top-down arcade shooter / bullet hell
**Target Entity Count:** 5,000-10,000 simultaneous bullets
**Visual Style:** Neon/synthwave with glowing projectiles
**Scope:** Single-screen arena, survive waves of increasingly complex patterns

### Core Loop

1. Player moves and shoots
2. Enemies spawn and fire bullet patterns
3. Player dodges bullets, destroys enemies
4. Survive increasingly difficult waves
5. High score tracking

---

## Why This Showcases ECS

| ECS Strength | How Bullet Hell Demonstrates It |
|--------------|--------------------------------|
| **Bulk Entity Processing** | 10,000 bullets updated per frame |
| **High Spawn/Despawn Rates** | 500+ bullets spawned per second |
| **Component Composition** | Bullets with different behaviors via components |
| **Efficient Queries** | `With<Bullet>().Without<PlayerOwned>()` for collision |
| **System Ordering** | Movement → Collision → Damage → Cleanup |
| **No Inheritance** | All bullet types are component combinations |

---

## Gameplay Design

### Player

- WASD movement (or arrow keys)
- Auto-fire or hold-to-fire
- Small hitbox (only center sprite collides)
- 3 lives, brief invincibility on hit
- Screen-wrap or bounded arena

### Enemies

| Enemy Type | Behavior | Pattern |
|------------|----------|---------|
| **Turret** | Stationary, rotates toward player | Aimed streams |
| **Spinner** | Stationary, constant rotation | Spiral patterns |
| **Chaser** | Follows player slowly | Radial bursts when close |
| **Bomber** | Flies across screen | Drops bombs that explode into bullets |
| **Boss** | Large, multi-phase | Complex overlapping patterns |

### Wave Progression

```
Wave 1-3:   1-2 turrets, simple aimed shots
Wave 4-6:   Add spinners, introduce spirals
Wave 7-10:  Add chasers, increase density
Wave 11-15: Add bombers, overlapping patterns
Wave 16+:   Boss every 5 waves, scaling difficulty
```

---

## Component Architecture

### Core Components

```csharp
/// <summary>
/// Marks an entity as a projectile with lifetime tracking.
/// </summary>
[Component]
public partial struct Bullet
{
    /// <summary>Time remaining before auto-despawn.</summary>
    public float Lifetime;

    /// <summary>Damage dealt on hit.</summary>
    public int Damage;

    /// <summary>Visual size multiplier.</summary>
    public float Size;
}

/// <summary>
/// Standard 2D position component.
/// </summary>
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

/// <summary>
/// Velocity for linear movement.
/// </summary>
[Component]
public partial struct Velocity
{
    public float X;
    public float Y;
}

/// <summary>
/// Circular collision bounds.
/// </summary>
[Component]
public partial struct CircleCollider
{
    /// <summary>Collision radius.</summary>
    public float Radius;
}

/// <summary>
/// Visual tint for rendering.
/// </summary>
[Component]
public partial struct Tint
{
    public float R;
    public float G;
    public float B;
    public float A;
}
```

### Bullet Behavior Components (Tags and Modifiers)

```csharp
/// <summary>
/// Marks bullet as owned by player (won't damage player).
/// </summary>
[TagComponent]
public partial struct PlayerOwned;

/// <summary>
/// Marks bullet as owned by enemy (damages player).
/// </summary>
[TagComponent]
public partial struct EnemyOwned;

/// <summary>
/// Bullet that homes toward a target.
/// </summary>
[Component]
public partial struct Homing
{
    /// <summary>Turn rate in radians per second.</summary>
    public float TurnSpeed;

    /// <summary>Target entity to home toward.</summary>
    public Entity Target;
}

/// <summary>
/// Bullet that accelerates over time.
/// </summary>
[Component]
public partial struct Accelerating
{
    /// <summary>Acceleration per second.</summary>
    public float Rate;

    /// <summary>Maximum speed cap.</summary>
    public float MaxSpeed;
}

/// <summary>
/// Bullet that follows a sine wave path.
/// </summary>
[Component]
public partial struct Wavy
{
    /// <summary>Oscillation amplitude.</summary>
    public float Amplitude;

    /// <summary>Oscillation frequency.</summary>
    public float Frequency;

    /// <summary>Current phase offset.</summary>
    public float Phase;
}

/// <summary>
/// Bullet that splits into multiple bullets on death.
/// </summary>
[Component]
public partial struct Splitting
{
    /// <summary>Number of child bullets.</summary>
    public int Count;

    /// <summary>Spread angle in radians.</summary>
    public float SpreadAngle;

    /// <summary>Child bullet speed.</summary>
    public float ChildSpeed;
}

/// <summary>
/// Bullet that bounces off screen edges.
/// </summary>
[Component]
public partial struct Bouncing
{
    /// <summary>Remaining bounces before despawn.</summary>
    public int BouncesRemaining;
}

/// <summary>
/// Bullet that orbits around its spawn point.
/// </summary>
[Component]
public partial struct Orbiting
{
    /// <summary>Center of orbit.</summary>
    public float CenterX;
    public float CenterY;

    /// <summary>Orbit radius.</summary>
    public float Radius;

    /// <summary>Angular velocity in radians per second.</summary>
    public float AngularSpeed;

    /// <summary>Current angle.</summary>
    public float Angle;
}
```

### Entity Components

```csharp
/// <summary>
/// Player-specific state.
/// </summary>
[Component]
public partial struct Player
{
    public int Lives;
    public float InvincibilityTimer;
    public int Score;
    public float FireCooldown;
}

/// <summary>
/// Enemy with health and point value.
/// </summary>
[Component]
public partial struct Enemy
{
    public int Health;
    public int MaxHealth;
    public int PointValue;
}

/// <summary>
/// Fires bullets in a pattern.
/// </summary>
[Component]
public partial struct BulletEmitter
{
    /// <summary>Pattern identifier.</summary>
    public BulletPatternType Pattern;

    /// <summary>Bullets per second.</summary>
    public float FireRate;

    /// <summary>Time until next shot.</summary>
    public float Cooldown;

    /// <summary>Current pattern phase (for complex patterns).</summary>
    public float Phase;
}

public enum BulletPatternType
{
    Aimed,          // Single shot toward player
    Spread,         // Fan of bullets toward player
    Radial,         // Circle burst
    Spiral,         // Rotating radial
    Stream,         // Rapid-fire line
    Random,         // Random directions
    Targeted,       // Predictive aim
}
```

---

## Systems Design

### System Execution Order

```
Phase: Update
├── PlayerInputSystem      (Order: 0)   - Read input, set velocity
├── EnemyAISystem          (Order: 10)  - Enemy behavior/targeting
├── BulletEmitterSystem    (Order: 20)  - Spawn bullets from emitters
├── HomingSystem           (Order: 30)  - Adjust homing bullet velocity
├── AcceleratingSystem     (Order: 31)  - Apply acceleration
├── WavySystem             (Order: 32)  - Apply sine wave offset
├── OrbitingSystem         (Order: 33)  - Update orbital positions
├── MovementSystem         (Order: 40)  - Apply velocity to position
├── BouncingSystem         (Order: 45)  - Handle screen edge bounces
├── ScreenBoundsSystem     (Order: 50)  - Despawn off-screen bullets
├── LifetimeSystem         (Order: 55)  - Despawn expired bullets
├── CollisionSystem        (Order: 60)  - Detect hits
├── DamageSystem           (Order: 70)  - Apply damage, spawn effects
├── DeathSystem            (Order: 80)  - Handle entity deaths
├── SplittingSystem        (Order: 85)  - Spawn child bullets
├── ScoreSystem            (Order: 90)  - Update score
└── WaveSystem             (Order: 100) - Manage wave progression

Phase: Render
├── BackgroundSystem       (Order: 0)   - Render scrolling background
├── EntityRenderSystem     (Order: 10)  - Render player/enemies
├── BulletRenderSystem     (Order: 20)  - Batch render all bullets
├── EffectRenderSystem     (Order: 30)  - Render explosions/particles
└── UIRenderSystem         (Order: 100) - Score, lives, wave number
```

### Core Systems

```csharp
/// <summary>
/// Moves all entities with Position and Velocity.
/// </summary>
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}

/// <summary>
/// Adjusts velocity of homing bullets toward their target.
/// </summary>
public class HomingSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity, Homing>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var homing = ref World.Get<Homing>(entity);

            if (!World.IsAlive(homing.Target))
                continue;

            ref readonly var targetPos = ref World.Get<Position>(homing.Target);

            float dx = targetPos.X - pos.X;
            float dy = targetPos.Y - pos.Y;
            float targetAngle = MathF.Atan2(dy, dx);
            float currentAngle = MathF.Atan2(vel.Y, vel.X);
            float speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);

            // Rotate toward target
            float angleDiff = NormalizeAngle(targetAngle - currentAngle);
            float maxTurn = homing.TurnSpeed * deltaTime;
            float turn = Math.Clamp(angleDiff, -maxTurn, maxTurn);
            float newAngle = currentAngle + turn;

            vel.X = MathF.Cos(newAngle) * speed;
            vel.Y = MathF.Sin(newAngle) * speed;
        }
    }
}

/// <summary>
/// Applies acceleration to bullets.
/// </summary>
public class AcceleratingSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Velocity, Accelerating>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            ref readonly var accel = ref World.Get<Accelerating>(entity);

            float speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
            if (speed < 0.001f) continue;

            float newSpeed = Math.Min(speed + accel.Rate * deltaTime, accel.MaxSpeed);
            float scale = newSpeed / speed;

            vel.X *= scale;
            vel.Y *= scale;
        }
    }
}

/// <summary>
/// Despawns bullets that exceed their lifetime.
/// </summary>
public class LifetimeSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<Bullet>())
        {
            ref var bullet = ref World.Get<Bullet>(entity);
            bullet.Lifetime -= deltaTime;

            if (bullet.Lifetime <= 0)
            {
                buffer.Despawn(entity);
            }
        }

        buffer.Playback();
    }
}

/// <summary>
/// Spawns bullets from emitters based on their pattern.
/// </summary>
public class BulletEmitterSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        Entity player = FindPlayer();

        foreach (var entity in World.Query<Position, BulletEmitter>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref var emitter = ref World.Get<BulletEmitter>(entity);

            emitter.Cooldown -= deltaTime;
            emitter.Phase += deltaTime;

            if (emitter.Cooldown <= 0)
            {
                emitter.Cooldown = 1f / emitter.FireRate;
                SpawnPattern(pos, emitter, player);
            }
        }
    }

    private void SpawnPattern(in Position origin, in BulletEmitter emitter, Entity target)
    {
        switch (emitter.Pattern)
        {
            case BulletPatternType.Aimed:
                SpawnAimedBullet(origin, target);
                break;

            case BulletPatternType.Radial:
                SpawnRadialBurst(origin, bulletCount: 16);
                break;

            case BulletPatternType.Spiral:
                SpawnSpiralBullet(origin, emitter.Phase);
                break;

            // ... other patterns
        }
    }
}
```

---

## Bullet Patterns

### Pattern Implementations

```csharp
/// <summary>
/// Spawns a single bullet aimed at the player.
/// </summary>
private void SpawnAimedBullet(in Position origin, Entity target)
{
    if (!World.IsAlive(target)) return;

    ref readonly var targetPos = ref World.Get<Position>(target);
    float dx = targetPos.X - origin.X;
    float dy = targetPos.Y - origin.Y;
    float dist = MathF.Sqrt(dx * dx + dy * dy);
    float speed = 200f;

    World.Spawn()
        .With(new Position { X = origin.X, Y = origin.Y })
        .With(new Velocity { X = dx / dist * speed, Y = dy / dist * speed })
        .With(new Bullet { Lifetime = 5f, Damage = 1, Size = 1f })
        .With(new CircleCollider { Radius = 4f })
        .With(new Tint { R = 1f, G = 0.3f, B = 0.3f, A = 1f })
        .WithTag<EnemyOwned>()
        .Build();
}

/// <summary>
/// Spawns bullets in a circle around the origin.
/// </summary>
private void SpawnRadialBurst(in Position origin, int bulletCount, float speed = 150f)
{
    float angleStep = MathF.Tau / bulletCount;

    for (int i = 0; i < bulletCount; i++)
    {
        float angle = i * angleStep;

        World.Spawn()
            .With(new Position { X = origin.X, Y = origin.Y })
            .With(new Velocity
            {
                X = MathF.Cos(angle) * speed,
                Y = MathF.Sin(angle) * speed
            })
            .With(new Bullet { Lifetime = 4f, Damage = 1, Size = 0.8f })
            .With(new CircleCollider { Radius = 3f })
            .With(new Tint { R = 0.3f, G = 0.8f, B = 1f, A = 1f })
            .WithTag<EnemyOwned>()
            .Build();
    }
}

/// <summary>
/// Spawns a single bullet as part of a rotating spiral.
/// </summary>
private void SpawnSpiralBullet(in Position origin, float phase, float speed = 180f)
{
    // Fire 2 bullets opposite each other, rotating over time
    float baseAngle = phase * 3f; // 3 radians per second rotation

    for (int i = 0; i < 2; i++)
    {
        float angle = baseAngle + i * MathF.PI;

        World.Spawn()
            .With(new Position { X = origin.X, Y = origin.Y })
            .With(new Velocity
            {
                X = MathF.Cos(angle) * speed,
                Y = MathF.Sin(angle) * speed
            })
            .With(new Bullet { Lifetime = 5f, Damage = 1, Size = 0.7f })
            .With(new CircleCollider { Radius = 3f })
            .With(new Tint { R = 0.8f, G = 0.3f, B = 1f, A = 1f })
            .WithTag<EnemyOwned>()
            .Build();
    }
}

/// <summary>
/// Spawns a fan of bullets toward the target.
/// </summary>
private void SpawnSpreadShot(in Position origin, Entity target, int bulletCount, float spreadAngle)
{
    if (!World.IsAlive(target)) return;

    ref readonly var targetPos = ref World.Get<Position>(target);
    float dx = targetPos.X - origin.X;
    float dy = targetPos.Y - origin.Y;
    float centerAngle = MathF.Atan2(dy, dx);
    float speed = 180f;

    float angleStep = spreadAngle / (bulletCount - 1);
    float startAngle = centerAngle - spreadAngle / 2;

    for (int i = 0; i < bulletCount; i++)
    {
        float angle = startAngle + i * angleStep;

        World.Spawn()
            .With(new Position { X = origin.X, Y = origin.Y })
            .With(new Velocity
            {
                X = MathF.Cos(angle) * speed,
                Y = MathF.Sin(angle) * speed
            })
            .With(new Bullet { Lifetime = 4f, Damage = 1, Size = 0.9f })
            .With(new CircleCollider { Radius = 4f })
            .With(new Tint { R = 1f, G = 0.6f, B = 0.2f, A = 1f })
            .WithTag<EnemyOwned>()
            .Build();
    }
}
```

### Complex Pattern: Rose Curve

```csharp
/// <summary>
/// Spawns bullets in a mathematical rose pattern.
/// Creates mesmerizing, hard-to-dodge patterns.
/// </summary>
private void SpawnRosePattern(in Position origin, float phase, int petals = 5)
{
    // Rose curve: r = cos(k*theta), k = petals
    int bulletsPerFrame = 3;
    float speed = 120f;

    for (int i = 0; i < bulletsPerFrame; i++)
    {
        float theta = phase * 2f + i * 0.1f;
        float r = MathF.Cos(petals * theta);

        // Convert polar to cartesian for direction
        float angle = theta;
        float dirX = MathF.Cos(angle);
        float dirY = MathF.Sin(angle);

        // Spawn slightly offset based on r
        float offset = r * 20f;

        World.Spawn()
            .With(new Position
            {
                X = origin.X + dirX * offset,
                Y = origin.Y + dirY * offset
            })
            .With(new Velocity
            {
                X = dirX * speed * (0.5f + MathF.Abs(r)),
                Y = dirY * speed * (0.5f + MathF.Abs(r))
            })
            .With(new Bullet { Lifetime = 6f, Damage = 1, Size = 0.6f })
            .With(new CircleCollider { Radius = 3f })
            .With(new Tint
            {
                R = 0.5f + r * 0.5f,
                G = 0.2f,
                B = 0.8f - r * 0.3f,
                A = 1f
            })
            .WithTag<EnemyOwned>()
            .Build();
    }
}
```

---

## Collision Strategy

### Spatial Partitioning

With thousands of bullets, O(n²) collision is unacceptable. Use spatial hashing:

```csharp
/// <summary>
/// Spatial hash grid for efficient collision detection.
/// </summary>
public class SpatialHash
{
    private readonly float cellSize;
    private readonly Dictionary<long, List<Entity>> cells = new();

    public SpatialHash(float cellSize = 50f)
    {
        this.cellSize = cellSize;
    }

    public void Clear() => cells.Clear();

    public void Insert(Entity entity, float x, float y, float radius)
    {
        // Insert into all cells the entity overlaps
        int minX = (int)((x - radius) / cellSize);
        int maxX = (int)((x + radius) / cellSize);
        int minY = (int)((y - radius) / cellSize);
        int maxY = (int)((y + radius) / cellSize);

        for (int cx = minX; cx <= maxX; cx++)
        {
            for (int cy = minY; cy <= maxY; cy++)
            {
                long key = ((long)cx << 32) | (uint)cy;
                if (!cells.TryGetValue(key, out var list))
                {
                    list = new List<Entity>();
                    cells[key] = list;
                }
                list.Add(entity);
            }
        }
    }

    public IEnumerable<Entity> Query(float x, float y, float radius)
    {
        int minX = (int)((x - radius) / cellSize);
        int maxX = (int)((x + radius) / cellSize);
        int minY = (int)((y - radius) / cellSize);
        int maxY = (int)((y + radius) / cellSize);

        for (int cx = minX; cx <= maxX; cx++)
        {
            for (int cy = minY; cy <= maxY; cy++)
            {
                long key = ((long)cx << 32) | (uint)cy;
                if (cells.TryGetValue(key, out var list))
                {
                    foreach (var entity in list)
                        yield return entity;
                }
            }
        }
    }
}
```

### Collision System

```csharp
/// <summary>
/// Detects bullet-player and bullet-enemy collisions.
/// </summary>
public class CollisionSystem : SystemBase
{
    private readonly SpatialHash spatialHash = new(50f);

    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        // Build spatial hash of all enemy bullets
        spatialHash.Clear();
        foreach (var entity in World.Query<Position, CircleCollider, EnemyOwned>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var col = ref World.Get<CircleCollider>(entity);
            spatialHash.Insert(entity, pos.X, pos.Y, col.Radius);
        }

        // Check player against enemy bullets
        foreach (var player in World.Query<Position, CircleCollider, Player>())
        {
            ref readonly var playerPos = ref World.Get<Position>(player);
            ref readonly var playerCol = ref World.Get<CircleCollider>(player);
            ref var playerData = ref World.Get<Player>(player);

            if (playerData.InvincibilityTimer > 0) continue;

            foreach (var bullet in spatialHash.Query(playerPos.X, playerPos.Y, playerCol.Radius + 10f))
            {
                if (!World.IsAlive(bullet)) continue;

                ref readonly var bulletPos = ref World.Get<Position>(bullet);
                ref readonly var bulletCol = ref World.Get<CircleCollider>(bullet);

                float dx = playerPos.X - bulletPos.X;
                float dy = playerPos.Y - bulletPos.Y;
                float distSq = dx * dx + dy * dy;
                float radiiSum = playerCol.Radius + bulletCol.Radius;

                if (distSq < radiiSum * radiiSum)
                {
                    // Hit!
                    playerData.Lives--;
                    playerData.InvincibilityTimer = 2f;
                    buffer.Despawn(bullet);

                    // Spawn hit effect
                    SpawnHitEffect(playerPos);
                    break; // Only one hit per frame
                }
            }
        }

        // Similar logic for player bullets vs enemies...

        buffer.Playback();
    }
}
```

---

## Visual Effects

### Glow Rendering

Bullet hell games need that signature neon glow:

```csharp
/// <summary>
/// Renders bullets with additive glow effect.
/// </summary>
public class BulletRenderSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var graphics = World.GetExtension<GraphicsContext>();
        var spriteBatch = graphics.SpriteBatch;

        // Pass 1: Render bullet cores
        spriteBatch.Begin(BlendMode.Additive);

        foreach (var entity in World.Query<Position, Bullet, Tint>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var bullet = ref World.Get<Bullet>(entity);
            ref readonly var tint = ref World.Get<Tint>(entity);

            float size = bullet.Size * 8f;

            // Core (bright center)
            spriteBatch.Draw(
                BulletTexture,
                new Vector2(pos.X, pos.Y),
                new Color(tint.R, tint.G, tint.B, tint.A),
                size
            );

            // Glow (larger, dimmer)
            spriteBatch.Draw(
                GlowTexture,
                new Vector2(pos.X, pos.Y),
                new Color(tint.R * 0.5f, tint.G * 0.5f, tint.B * 0.5f, 0.3f),
                size * 3f
            );
        }

        spriteBatch.End();
    }
}
```

### Screen Shake

```csharp
[Component]
public partial struct ScreenShake
{
    public float Intensity;
    public float Duration;
    public float Frequency;
}

public class ScreenShakeSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var camera = World.GetSingleton<Camera2D>();

        foreach (var entity in World.Query<ScreenShake>())
        {
            ref var shake = ref World.Get<ScreenShake>(entity);
            shake.Duration -= deltaTime;

            if (shake.Duration <= 0)
            {
                World.Despawn(entity);
                continue;
            }

            float t = shake.Duration * shake.Frequency;
            float offsetX = MathF.Sin(t * 17f) * shake.Intensity;
            float offsetY = MathF.Sin(t * 23f) * shake.Intensity;

            camera.Offset = new Vector2(offsetX, offsetY);
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Core Movement (2-3 files)

1. Position, Velocity, CircleCollider components
2. MovementSystem
3. Basic rendering
4. Player input and movement

**Milestone:** Moving player on screen

### Phase 2: Bullets (3-4 files)

1. Bullet component and lifetime system
2. BulletEmitter component
3. Basic patterns (aimed, radial)
4. Screen bounds despawning

**Milestone:** Stationary turret fires at player

### Phase 3: Collision (2 files)

1. SpatialHash implementation
2. CollisionSystem for player-bullet
3. Damage and invincibility

**Milestone:** Player can be hit by bullets

### Phase 4: Enemies (3 files)

1. Enemy component and spawning
2. Player bullets and enemy collision
3. Score system
4. Basic enemy AI

**Milestone:** Enemies can be destroyed for points

### Phase 5: Patterns (2 files)

1. Spiral, spread, wavy patterns
2. Homing and accelerating bullets
3. Pattern composition

**Milestone:** Diverse bullet patterns

### Phase 6: Polish (2-3 files)

1. Glow rendering
2. Screen shake
3. Particle effects
4. Wave progression
5. Game over / restart

**Milestone:** Complete playable demo

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Bullet count | 10,000 simultaneous |
| Spawn rate | 500/second sustained |
| Frame time | < 8ms at 10k bullets |
| Collision checks | O(n) with spatial hash |
| Memory per bullet | ~100 bytes |

---

## Future Enhancements

- **Scoring combos** - Graze bullets for bonus points
- **Bomb mechanic** - Clear screen of bullets
- **Patterns from data** - Load patterns from JSON/files
- **Replay system** - Record and playback
- **Online leaderboards** - High score tracking
- **Boss patterns** - Multi-phase boss fights
