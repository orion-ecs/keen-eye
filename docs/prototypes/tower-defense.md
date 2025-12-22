# Tower Defense Prototype

A strategic tower defense game demonstrating component composition, system ordering, and query filtering with `With<>`/`Without<>` patterns.

---

## Table of Contents

1. [Overview](#overview)
2. [Why This Showcases ECS](#why-this-showcases-ecs)
3. [Gameplay Design](#gameplay-design)
4. [Component Architecture](#component-architecture)
5. [Systems Design](#systems-design)
6. [Tower Mechanics](#tower-mechanics)
7. [Enemy Pathfinding](#enemy-pathfinding)
8. [Status Effects](#status-effects)
9. [Implementation Plan](#implementation-plan)

---

## Overview

**Genre:** Classic tower defense with lane-based enemies
**Target Entity Count:** 100-500 enemies, 20-50 towers, 200+ projectiles
**Visual Style:** Clean 2D with clear visual feedback
**Scope:** Single map, 20+ waves, 8-10 tower types

### Core Loop

1. Enemies spawn in waves, follow paths to goal
2. Player places towers using currency
3. Towers auto-target and fire at enemies
4. Killing enemies earns currency
5. Survive all waves or lose when enemies reach goal

---

## Why This Showcases ECS

| ECS Strength | How Tower Defense Demonstrates It |
|--------------|----------------------------------|
| **Composition** | Tower = Targeting + DamageDealer + SlowAura + ... |
| **Query Filters** | `With<Enemy>().Without<Dead>()` for targeting |
| **System Ordering** | PathFollow → Targeting → Firing → Projectile → Damage |
| **Shared Components** | Status effects work on any entity with Health |
| **Entity Relationships** | Projectiles track targets, towers have range entities |
| **Tag Components** | `Flying`, `Armored`, `Boss` for enemy types |

---

## Gameplay Design

### Map Layout

```
┌─────────────────────────────────────────┐
│  [S]═══╗                                │
│        ║     ┌───┐         ┌───┐        │
│        ╚═════│   │═════════│   │════╗   │
│              └───┘         └───┘    ║   │
│    ┌───┐                   ┌───┐    ║   │
│ ╔══│   │═══════════════════│   │════╝   │
│ ║  └───┘                   └───┘        │
│ ╚═══════════════════════════════════[G] │
└─────────────────────────────────────────┘

[S] = Spawn point
[G] = Goal (player base)
═══ = Enemy path
┌─┐ = Buildable areas
```

### Resources

| Resource | Source | Use |
|----------|--------|-----|
| **Gold** | Killing enemies | Building/upgrading towers |
| **Lives** | Start with 20 | Lost when enemies reach goal |
| **Wave** | Increments each wave | Determines enemy composition |

### Tower Types

| Tower | Cost | Damage | Range | Special |
|-------|------|--------|-------|---------|
| **Arrow** | 100 | 10 | Medium | Fast attack speed |
| **Cannon** | 200 | 50 | Short | Splash damage |
| **Frost** | 150 | 5 | Medium | Slows enemies |
| **Tesla** | 300 | 30 | Medium | Chain lightning |
| **Sniper** | 250 | 100 | Long | Slow, ignores armor |
| **Flame** | 200 | 15/s | Short | Damage over time |
| **Support** | 175 | 0 | Medium | Buffs nearby towers |
| **Missile** | 350 | 80 | Long | Homing, anti-air |

### Enemy Types

| Enemy | Health | Speed | Armor | Special |
|-------|--------|-------|-------|---------|
| **Grunt** | 50 | Normal | 0 | Basic enemy |
| **Runner** | 30 | Fast | 0 | Low HP, high speed |
| **Tank** | 200 | Slow | 5 | High HP, damage reduction |
| **Flyer** | 60 | Normal | 0 | Ignores ground towers, flying |
| **Healer** | 80 | Normal | 0 | Heals nearby enemies |
| **Shield** | 100 | Slow | 10 | Blocks projectiles for others |
| **Splitter** | 40 | Normal | 0 | Spawns 2 mini enemies on death |
| **Boss** | 1000 | Slow | 8 | Every 5 waves |

---

## Component Architecture

### Core Components

```csharp
/// <summary>
/// Position in 2D world space.
/// </summary>
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

/// <summary>
/// Entity health pool.
/// </summary>
[Component]
public partial struct Health
{
    public int Current;
    public int Max;

    public float Percentage => (float)Current / Max;
}

/// <summary>
/// Damage reduction from attacks.
/// </summary>
[Component]
public partial struct Armor
{
    /// <summary>Flat damage reduction.</summary>
    public int Value;
}

/// <summary>
/// Movement speed modifier.
/// </summary>
[Component]
public partial struct MoveSpeed
{
    /// <summary>Base speed in units per second.</summary>
    public float Base;

    /// <summary>Current multiplier (1.0 = normal).</summary>
    public float Multiplier;

    public float Current => Base * Multiplier;
}
```

### Enemy Components

```csharp
/// <summary>
/// Marks entity as an enemy.
/// </summary>
[TagComponent]
public partial struct Enemy;

/// <summary>
/// Enemy follows a path to the goal.
/// </summary>
[Component]
public partial struct PathFollower
{
    /// <summary>Current waypoint index.</summary>
    public int WaypointIndex;

    /// <summary>Progress to next waypoint (0-1).</summary>
    public float Progress;

    /// <summary>Path ID this enemy follows.</summary>
    public int PathId;
}

/// <summary>
/// Currency reward when killed.
/// </summary>
[Component]
public partial struct Bounty
{
    public int Gold;
}

/// <summary>
/// Lives lost when reaching goal.
/// </summary>
[Component]
public partial struct LivesCost
{
    public int Lives;
}

// Enemy type tags
[TagComponent]
public partial struct Flying;

[TagComponent]
public partial struct Armored;

[TagComponent]
public partial struct Boss;

/// <summary>
/// Spawns child entities on death.
/// </summary>
[Component]
public partial struct SpawnsOnDeath
{
    /// <summary>Prefab ID to spawn.</summary>
    public int PrefabId;

    /// <summary>Number of children.</summary>
    public int Count;
}

/// <summary>
/// Heals nearby allies.
/// </summary>
[Component]
public partial struct HealerAura
{
    /// <summary>Healing range.</summary>
    public float Radius;

    /// <summary>HP healed per second.</summary>
    public float HealRate;
}
```

### Tower Components

```csharp
/// <summary>
/// Marks entity as a tower.
/// </summary>
[TagComponent]
public partial struct Tower;

/// <summary>
/// Tower can attack enemies.
/// </summary>
[Component]
public partial struct Targeting
{
    /// <summary>Attack range in units.</summary>
    public float Range;

    /// <summary>Current target entity.</summary>
    public Entity Target;

    /// <summary>How to select targets.</summary>
    public TargetingMode Mode;
}

public enum TargetingMode
{
    First,      // Furthest along path
    Last,       // Most recent spawn
    Closest,    // Nearest to tower
    Strongest,  // Highest HP
    Weakest,    // Lowest HP
}

/// <summary>
/// Deals damage to target.
/// </summary>
[Component]
public partial struct DamageDealer
{
    /// <summary>Damage per hit.</summary>
    public int Damage;

    /// <summary>Attacks per second.</summary>
    public float AttackSpeed;

    /// <summary>Time until next attack.</summary>
    public float Cooldown;
}

/// <summary>
/// Fires projectiles at target.
/// </summary>
[Component]
public partial struct ProjectileFirer
{
    /// <summary>Projectile prefab ID.</summary>
    public int ProjectilePrefab;

    /// <summary>Projectile speed.</summary>
    public float ProjectileSpeed;
}

/// <summary>
/// Deals damage in an area.
/// </summary>
[Component]
public partial struct SplashDamage
{
    /// <summary>Splash radius.</summary>
    public float Radius;

    /// <summary>Damage falloff at edge (0-1).</summary>
    public float Falloff;
}

/// <summary>
/// Slows enemies in range.
/// </summary>
[Component]
public partial struct SlowAura
{
    /// <summary>Slow radius.</summary>
    public float Radius;

    /// <summary>Speed multiplier (0.5 = 50% slower).</summary>
    public float SlowAmount;
}

/// <summary>
/// Chains to additional targets.
/// </summary>
[Component]
public partial struct ChainLightning
{
    /// <summary>Maximum chain jumps.</summary>
    public int MaxChains;

    /// <summary>Range for chain jumps.</summary>
    public float ChainRange;

    /// <summary>Damage multiplier per jump.</summary>
    public float DamageDecay;
}

/// <summary>
/// Ignores target's armor.
/// </summary>
[TagComponent]
public partial struct ArmorPiercing;

/// <summary>
/// Can target flying enemies.
/// </summary>
[TagComponent]
public partial struct AntiAir;

/// <summary>
/// Cannot target flying enemies.
/// </summary>
[TagComponent]
public partial struct GroundOnly;

/// <summary>
/// Buffs nearby towers.
/// </summary>
[Component]
public partial struct SupportAura
{
    /// <summary>Buff radius.</summary>
    public float Radius;

    /// <summary>Damage increase multiplier.</summary>
    public float DamageBonus;

    /// <summary>Attack speed increase multiplier.</summary>
    public float AttackSpeedBonus;
}

/// <summary>
/// Tower upgrade level.
/// </summary>
[Component]
public partial struct Upgradeable
{
    /// <summary>Current upgrade level (1-3).</summary>
    public int Level;

    /// <summary>Cost for next upgrade.</summary>
    public int UpgradeCost;
}
```

### Projectile Components

```csharp
/// <summary>
/// Marks entity as a projectile.
/// </summary>
[TagComponent]
public partial struct Projectile;

/// <summary>
/// Projectile moves toward a target entity.
/// </summary>
[Component]
public partial struct HomingProjectile
{
    /// <summary>Target entity to hit.</summary>
    public Entity Target;

    /// <summary>Movement speed.</summary>
    public float Speed;

    /// <summary>Damage on hit.</summary>
    public int Damage;
}

/// <summary>
/// Source tower for damage attribution.
/// </summary>
[Component]
public partial struct DamageSource
{
    public Entity Tower;
}
```

### Status Effect Components

```csharp
/// <summary>
/// Slowed movement.
/// </summary>
[Component]
public partial struct Slowed
{
    /// <summary>Speed multiplier (0.5 = 50% speed).</summary>
    public float Multiplier;

    /// <summary>Remaining duration.</summary>
    public float Duration;

    /// <summary>Source of the slow.</summary>
    public Entity Source;
}

/// <summary>
/// Taking damage over time.
/// </summary>
[Component]
public partial struct Burning
{
    /// <summary>Damage per second.</summary>
    public float DamagePerSecond;

    /// <summary>Remaining duration.</summary>
    public float Duration;

    /// <summary>Source of the burn.</summary>
    public Entity Source;
}

/// <summary>
/// Cannot attack.
/// </summary>
[Component]
public partial struct Stunned
{
    /// <summary>Remaining duration.</summary>
    public float Duration;
}

/// <summary>
/// Marked for bonus damage.
/// </summary>
[Component]
public partial struct Marked
{
    /// <summary>Damage multiplier.</summary>
    public float DamageMultiplier;

    /// <summary>Remaining duration.</summary>
    public float Duration;
}
```

---

## Systems Design

### System Execution Order

```
Phase: Update
├── WaveSpawnSystem        (Order: 0)   - Spawn enemies each wave
├── PathFollowSystem       (Order: 10)  - Move enemies along path
├── HealerAuraSystem       (Order: 15)  - Apply healing to nearby
├── StatusEffectSystem     (Order: 20)  - Tick and expire effects
├── SlowAuraSystem         (Order: 25)  - Apply slow to enemies in range
├── SupportAuraSystem      (Order: 26)  - Buff towers in range
├── TargetingSystem        (Order: 30)  - Acquire targets for towers
├── AttackSystem           (Order: 40)  - Fire at targets
├── ProjectileSystem       (Order: 50)  - Move projectiles
├── DamageSystem           (Order: 60)  - Apply damage, check kills
├── SplashDamageSystem     (Order: 65)  - Apply AoE damage
├── ChainLightningSystem   (Order: 66)  - Process chain jumps
├── DeathSystem            (Order: 70)  - Handle enemy deaths
├── SpawnOnDeathSystem     (Order: 75)  - Spawn children from splitters
├── GoalReachedSystem      (Order: 80)  - Check enemies at goal
├── EconomySystem          (Order: 90)  - Update gold, check game over
└── UIUpdateSystem         (Order: 100) - Update UI elements

Phase: Render
├── MapRenderSystem        (Order: 0)   - Render map tiles
├── TowerRenderSystem      (Order: 10)  - Render towers
├── EnemyRenderSystem      (Order: 20)  - Render enemies + HP bars
├── ProjectileRenderSystem (Order: 30)  - Render projectiles
├── EffectRenderSystem     (Order: 40)  - Render effects/particles
├── RangeIndicatorSystem   (Order: 50)  - Show selected tower range
└── UIRenderSystem         (Order: 100) - HUD, menus
```

### Core Systems

```csharp
/// <summary>
/// Moves enemies along their designated path.
/// </summary>
public class PathFollowSystem : SystemBase
{
    private readonly PathData[] paths;

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, PathFollower, MoveSpeed>()
            .With<Enemy>()
            .Without<Stunned>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var follower = ref World.Get<PathFollower>(entity);
            ref readonly var speed = ref World.Get<MoveSpeed>(entity);

            var path = paths[follower.PathId];
            if (follower.WaypointIndex >= path.Waypoints.Length)
                continue; // Already at goal

            var target = path.Waypoints[follower.WaypointIndex];
            float dx = target.X - pos.X;
            float dy = target.Y - pos.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < 1f)
            {
                // Reached waypoint, move to next
                follower.WaypointIndex++;
                follower.Progress = 0;
            }
            else
            {
                // Move toward waypoint
                float move = speed.Current * deltaTime;
                pos.X += dx / dist * move;
                pos.Y += dy / dist * move;
                follower.Progress = 1f - (dist / path.SegmentLengths[follower.WaypointIndex]);
            }
        }
    }
}

/// <summary>
/// Acquires targets for towers based on targeting mode.
/// </summary>
public class TargetingSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var tower in World.Query<Position, Targeting>().With<Tower>())
        {
            ref readonly var towerPos = ref World.Get<Position>(tower);
            ref var targeting = ref World.Get<Targeting>(tower);

            // Check if current target still valid
            if (World.IsAlive(targeting.Target) && IsInRange(tower, targeting.Target))
                continue;

            // Find new target
            targeting.Target = FindBestTarget(tower, towerPos, targeting);
        }
    }

    private Entity FindBestTarget(Entity tower, in Position towerPos, in Targeting targeting)
    {
        bool canTargetFlying = World.Has<AntiAir>(tower);
        bool groundOnly = World.Has<GroundOnly>(tower);

        Entity best = Entity.Invalid;
        float bestScore = float.MinValue;

        foreach (var enemy in World.Query<Position, PathFollower, Health>()
            .With<Enemy>()
            .Without<Dead>())
        {
            // Check flying restriction
            bool isFlying = World.Has<Flying>(enemy);
            if (isFlying && groundOnly) continue;
            if (isFlying && !canTargetFlying) continue;

            ref readonly var enemyPos = ref World.Get<Position>(enemy);

            // Check range
            float dx = enemyPos.X - towerPos.X;
            float dy = enemyPos.Y - towerPos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq > targeting.Range * targeting.Range)
                continue;

            // Score based on targeting mode
            float score = targeting.Mode switch
            {
                TargetingMode.First => World.Get<PathFollower>(enemy).WaypointIndex * 1000
                                     + World.Get<PathFollower>(enemy).Progress,
                TargetingMode.Last => -(World.Get<PathFollower>(enemy).WaypointIndex * 1000
                                      + World.Get<PathFollower>(enemy).Progress),
                TargetingMode.Closest => -distSq,
                TargetingMode.Strongest => World.Get<Health>(enemy).Current,
                TargetingMode.Weakest => -World.Get<Health>(enemy).Current,
                _ => 0
            };

            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }
}

/// <summary>
/// Fires at targets when cooldown allows.
/// </summary>
public class AttackSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var tower in World.Query<Position, Targeting, DamageDealer>()
            .With<Tower>())
        {
            ref readonly var towerPos = ref World.Get<Position>(tower);
            ref readonly var targeting = ref World.Get<Targeting>(tower);
            ref var damage = ref World.Get<DamageDealer>(tower);

            damage.Cooldown -= deltaTime;

            if (damage.Cooldown > 0 || !World.IsAlive(targeting.Target))
                continue;

            // Reset cooldown
            damage.Cooldown = 1f / damage.AttackSpeed;

            // Fire projectile or instant hit?
            if (World.Has<ProjectileFirer>(tower))
            {
                SpawnProjectile(tower, targeting.Target);
            }
            else
            {
                // Instant damage (e.g., laser)
                ApplyDamage(targeting.Target, damage.Damage, tower);
            }
        }
    }

    private void SpawnProjectile(Entity tower, Entity target)
    {
        ref readonly var towerPos = ref World.Get<Position>(tower);
        ref readonly var firer = ref World.Get<ProjectileFirer>(tower);
        ref readonly var damage = ref World.Get<DamageDealer>(tower);

        World.Spawn()
            .With(new Position { X = towerPos.X, Y = towerPos.Y })
            .With(new HomingProjectile
            {
                Target = target,
                Speed = firer.ProjectileSpeed,
                Damage = damage.Damage
            })
            .With(new DamageSource { Tower = tower })
            .WithTag<Projectile>()
            .Build();
    }
}

/// <summary>
/// Applies slow to enemies in tower's aura range.
/// </summary>
public class SlowAuraSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // First, clear expired slows and reset speed
        foreach (var entity in World.Query<MoveSpeed, Slowed>())
        {
            ref var slow = ref World.Get<Slowed>(entity);
            slow.Duration -= deltaTime;

            if (slow.Duration <= 0)
            {
                World.Remove<Slowed>(entity);
                ref var speed = ref World.Get<MoveSpeed>(entity);
                speed.Multiplier = 1f;
            }
        }

        // Apply slows from frost towers
        foreach (var tower in World.Query<Position, SlowAura>().With<Tower>())
        {
            ref readonly var towerPos = ref World.Get<Position>(tower);
            ref readonly var aura = ref World.Get<SlowAura>(tower);

            foreach (var enemy in World.Query<Position, MoveSpeed>().With<Enemy>())
            {
                ref readonly var enemyPos = ref World.Get<Position>(enemy);

                float dx = enemyPos.X - towerPos.X;
                float dy = enemyPos.Y - towerPos.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq > aura.Radius * aura.Radius)
                    continue;

                // Apply or refresh slow
                ref var speed = ref World.Get<MoveSpeed>(enemy);

                if (World.Has<Slowed>(enemy))
                {
                    ref var slow = ref World.Get<Slowed>(enemy);
                    // Take strongest slow
                    slow.Multiplier = MathF.Min(slow.Multiplier, aura.SlowAmount);
                    slow.Duration = 0.5f; // Refresh
                }
                else
                {
                    World.Add(enemy, new Slowed
                    {
                        Multiplier = aura.SlowAmount,
                        Duration = 0.5f,
                        Source = tower
                    });
                }

                speed.Multiplier = World.Get<Slowed>(enemy).Multiplier;
            }
        }
    }
}
```

---

## Tower Mechanics

### Tower Composition Examples

```csharp
/// <summary>
/// Tower factory methods demonstrating composition.
/// </summary>
public static class TowerFactory
{
    /// <summary>
    /// Basic arrow tower - fast attacks, moderate damage.
    /// </summary>
    public static Entity CreateArrowTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 150, Mode = TargetingMode.First })
            .With(new DamageDealer { Damage = 10, AttackSpeed = 2f })
            .With(new ProjectileFirer { ProjectileSpeed = 400 })
            .With(new Upgradeable { Level = 1, UpgradeCost = 75 })
            .WithTag<Tower>()
            .WithTag<GroundOnly>()
            .Build();
    }

    /// <summary>
    /// Cannon tower - slow, splash damage.
    /// </summary>
    public static Entity CreateCannonTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 100, Mode = TargetingMode.Strongest })
            .With(new DamageDealer { Damage = 50, AttackSpeed = 0.5f })
            .With(new ProjectileFirer { ProjectileSpeed = 200 })
            .With(new SplashDamage { Radius = 50, Falloff = 0.5f })
            .With(new Upgradeable { Level = 1, UpgradeCost = 150 })
            .WithTag<Tower>()
            .WithTag<GroundOnly>()
            .Build();
    }

    /// <summary>
    /// Frost tower - slows enemies in range.
    /// </summary>
    public static Entity CreateFrostTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 120, Mode = TargetingMode.First })
            .With(new DamageDealer { Damage = 5, AttackSpeed = 1f })
            .With(new ProjectileFirer { ProjectileSpeed = 300 })
            .With(new SlowAura { Radius = 120, SlowAmount = 0.5f })
            .With(new Upgradeable { Level = 1, UpgradeCost = 100 })
            .WithTag<Tower>()
            .Build();
    }

    /// <summary>
    /// Tesla tower - chains to multiple targets.
    /// </summary>
    public static Entity CreateTeslaTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 130, Mode = TargetingMode.Closest })
            .With(new DamageDealer { Damage = 30, AttackSpeed = 0.8f })
            .With(new ChainLightning { MaxChains = 3, ChainRange = 80, DamageDecay = 0.7f })
            .With(new Upgradeable { Level = 1, UpgradeCost = 200 })
            .WithTag<Tower>()
            .Build();
    }

    /// <summary>
    /// Sniper tower - long range, armor piercing.
    /// </summary>
    public static Entity CreateSniperTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 300, Mode = TargetingMode.Strongest })
            .With(new DamageDealer { Damage = 100, AttackSpeed = 0.3f })
            .With(new ProjectileFirer { ProjectileSpeed = 800 })
            .With(new Upgradeable { Level = 1, UpgradeCost = 175 })
            .WithTag<Tower>()
            .WithTag<ArmorPiercing>()
            .Build();
    }

    /// <summary>
    /// Flame tower - damage over time.
    /// </summary>
    public static Entity CreateFlameTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 80, Mode = TargetingMode.First })
            .With(new DamageDealer { Damage = 5, AttackSpeed = 4f })
            .With(new AppliesBurning { DamagePerSecond = 10, Duration = 3f })
            .With(new Upgradeable { Level = 1, UpgradeCost = 150 })
            .WithTag<Tower>()
            .WithTag<GroundOnly>()
            .Build();
    }

    /// <summary>
    /// Support tower - buffs nearby towers.
    /// </summary>
    public static Entity CreateSupportTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new SupportAura { Radius = 150, DamageBonus = 0.25f, AttackSpeedBonus = 0.15f })
            .With(new Upgradeable { Level = 1, UpgradeCost = 125 })
            .WithTag<Tower>()
            .Build();
    }

    /// <summary>
    /// Missile tower - homing, anti-air.
    /// </summary>
    public static Entity CreateMissileTower(World world, float x, float y)
    {
        return world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new Targeting { Range = 200, Mode = TargetingMode.First })
            .With(new DamageDealer { Damage = 80, AttackSpeed = 0.6f })
            .With(new ProjectileFirer { ProjectileSpeed = 250 })
            .With(new SplashDamage { Radius = 30, Falloff = 0.3f })
            .With(new Upgradeable { Level = 1, UpgradeCost = 250 })
            .WithTag<Tower>()
            .WithTag<AntiAir>()
            .Build();
    }
}
```

### Upgrade System

```csharp
/// <summary>
/// Applies upgrades to tower components.
/// </summary>
public static class TowerUpgrades
{
    public static void UpgradeTower(World world, Entity tower)
    {
        ref var upgrade = ref world.Get<Upgradeable>(tower);

        if (upgrade.Level >= 3) return; // Max level

        upgrade.Level++;

        // Scale damage
        if (world.Has<DamageDealer>(tower))
        {
            ref var damage = ref world.Get<DamageDealer>(tower);
            damage.Damage = (int)(damage.Damage * 1.4f);
            damage.AttackSpeed *= 1.1f;
        }

        // Scale range
        if (world.Has<Targeting>(tower))
        {
            ref var targeting = ref world.Get<Targeting>(tower);
            targeting.Range *= 1.15f;
        }

        // Scale auras
        if (world.Has<SlowAura>(tower))
        {
            ref var aura = ref world.Get<SlowAura>(tower);
            aura.SlowAmount *= 0.85f; // Stronger slow (lower multiplier)
            aura.Radius *= 1.1f;
        }

        // Update cost for next upgrade
        upgrade.UpgradeCost = (int)(upgrade.UpgradeCost * 1.8f);
    }
}
```

---

## Enemy Pathfinding

### Path Data Structure

```csharp
/// <summary>
/// Pre-computed path for enemies to follow.
/// </summary>
public readonly struct PathData
{
    /// <summary>Ordered waypoints from spawn to goal.</summary>
    public readonly Vector2[] Waypoints;

    /// <summary>Distance of each segment for progress calculation.</summary>
    public readonly float[] SegmentLengths;

    /// <summary>Total path length.</summary>
    public readonly float TotalLength;

    public PathData(Vector2[] waypoints)
    {
        Waypoints = waypoints;
        SegmentLengths = new float[waypoints.Length];
        TotalLength = 0;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            float dx = waypoints[i + 1].X - waypoints[i].X;
            float dy = waypoints[i + 1].Y - waypoints[i].Y;
            SegmentLengths[i] = MathF.Sqrt(dx * dx + dy * dy);
            TotalLength += SegmentLengths[i];
        }
    }
}

/// <summary>
/// Singleton storing all paths.
/// </summary>
public class PathManager
{
    private readonly PathData[] paths;

    public PathData GetPath(int pathId) => paths[pathId];

    /// <summary>
    /// Calculate how far along the total path an enemy is (0-1).
    /// Used for "First" targeting priority.
    /// </summary>
    public float GetPathProgress(in PathFollower follower)
    {
        var path = paths[follower.PathId];
        float traveled = 0;

        for (int i = 0; i < follower.WaypointIndex; i++)
        {
            traveled += path.SegmentLengths[i];
        }

        if (follower.WaypointIndex < path.SegmentLengths.Length)
        {
            traveled += path.SegmentLengths[follower.WaypointIndex] * follower.Progress;
        }

        return traveled / path.TotalLength;
    }
}
```

---

## Status Effects

### Effect Application

```csharp
/// <summary>
/// Component indicating this tower applies burning on hit.
/// </summary>
[Component]
public partial struct AppliesBurning
{
    public float DamagePerSecond;
    public float Duration;
}

/// <summary>
/// Processes burning damage over time.
/// </summary>
public class BurningSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<Health, Burning>())
        {
            ref var health = ref World.Get<Health>(entity);
            ref var burning = ref World.Get<Burning>(entity);

            // Apply damage
            int damage = (int)(burning.DamagePerSecond * deltaTime);
            health.Current -= damage;

            // Tick duration
            burning.Duration -= deltaTime;

            if (burning.Duration <= 0)
            {
                buffer.Remove<Burning>(entity);
            }
        }

        buffer.Playback();
    }
}

/// <summary>
/// Applies burning when projectile hits.
/// </summary>
public class ApplyBurningOnHitSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // When a tower with AppliesBurning deals damage, add Burning to target
        // This would be called from the damage system
    }

    public static void ApplyBurning(World world, Entity target, Entity tower)
    {
        if (!world.Has<AppliesBurning>(tower)) return;

        ref readonly var applies = ref world.Get<AppliesBurning>(tower);

        if (world.Has<Burning>(target))
        {
            // Refresh duration
            ref var burning = ref world.Get<Burning>(target);
            burning.Duration = applies.Duration;
            burning.DamagePerSecond = MathF.Max(burning.DamagePerSecond, applies.DamagePerSecond);
        }
        else
        {
            world.Add(target, new Burning
            {
                DamagePerSecond = applies.DamagePerSecond,
                Duration = applies.Duration,
                Source = tower
            });
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Core Loop (3-4 files)

1. Position, Health, PathFollower components
2. PathFollowSystem
3. Basic enemy spawning
4. Goal reached detection

**Milestone:** Enemies walk path to goal

### Phase 2: Towers (4-5 files)

1. Tower, Targeting, DamageDealer components
2. TargetingSystem
3. AttackSystem
4. Projectile movement
5. Basic damage application

**Milestone:** Towers shoot and kill enemies

### Phase 3: Economy (2-3 files)

1. Gold tracking singleton
2. Bounty rewards on kill
3. Tower placement with cost
4. Lives tracking

**Milestone:** Playable economy loop

### Phase 4: Tower Variety (3-4 files)

1. SplashDamage component and system
2. SlowAura component and system
3. ChainLightning system
4. Support aura buffs

**Milestone:** Diverse tower types

### Phase 5: Enemy Variety (2-3 files)

1. Flying tag and targeting rules
2. Armor and ArmorPiercing
3. SpawnsOnDeath (splitters)
4. HealerAura

**Milestone:** Diverse enemy types

### Phase 6: Polish (3-4 files)

1. Wave progression system
2. Upgrade system
3. UI for tower selection/upgrade
4. Visual effects
5. Sound effects

**Milestone:** Complete playable demo

---

## Performance Considerations

| Aspect | Approach |
|--------|----------|
| Targeting | Spatial hash for range queries |
| Path progress | Pre-computed segment lengths |
| Status effects | Component-based, tick each frame |
| Projectiles | Simple homing, despawn on miss |
| Rendering | Batch by tower/enemy type |

---

## Query Examples

The tower defense prototype showcases KeenEyes' query system:

```csharp
// Find all living ground enemies
World.Query<Position, Health>()
    .With<Enemy>()
    .Without<Dead>()
    .Without<Flying>()

// Find all towers that can hit air
World.Query<Position, Targeting>()
    .With<Tower>()
    .With<AntiAir>()

// Find all slowed enemies
World.Query<MoveSpeed>()
    .With<Enemy>()
    .With<Slowed>()

// Find buffable towers in range (not self)
World.Query<Position, DamageDealer>()
    .With<Tower>()
    .Without<SupportAura>()  // Support towers don't buff themselves
```

---

## Future Enhancements

- **Multiple paths** - Enemies split across routes
- **Maze building** - Place walls to create path
- **Hero units** - Controllable units with abilities
- **Elemental system** - Fire/ice/lightning interactions
- **Endless mode** - Infinite scaling waves
- **Map editor** - Create custom maps
- **Multiplayer** - Competitive send-enemies mode
