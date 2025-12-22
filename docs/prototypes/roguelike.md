# Roguelike Prototype

A turn-based dungeon crawler demonstrating deep component composition, where items, abilities, and effects are all entity combinations.

---

## Table of Contents

1. [Overview](#overview)
2. [Why This Showcases ECS](#why-this-showcases-ecs)
3. [Gameplay Design](#gameplay-design)
4. [Component Architecture](#component-architecture)
5. [Systems Design](#systems-design)
6. [Item System](#item-system)
7. [Combat System](#combat-system)
8. [Procedural Generation](#procedural-generation)
9. [Implementation Plan](#implementation-plan)

---

## Overview

**Genre:** Turn-based roguelike dungeon crawler
**Visual Style:** ASCII or simple tile-based
**Scope:** 5-10 dungeon floors, permadeath, item collection

### Core Loop

1. Explore procedurally generated dungeon floors
2. Fight enemies in turn-based combat
3. Collect items (weapons, armor, potions, scrolls)
4. Level up and gain abilities
5. Descend to next floor
6. Die and start over (roguelike permadeath)

---

## Why This Showcases ECS

| ECS Strength | How Roguelike Demonstrates It |
|--------------|------------------------------|
| **Deep Composition** | Sword = Item + Equipable + MeleeDamage + Durability |
| **Everything is Entities** | Items, effects, abilities are all entities |
| **Flexible Queries** | `With<Equipable>().With<InInventory>()` |
| **Runtime Modification** | Add/remove components to change behavior |
| **No Inheritance** | Potion of Fire = Consumable + AppliesBurning |
| **Entity Relationships** | Equipment → Owner, Effect → Target |

---

## Gameplay Design

### Turn Structure

```
Player Turn:
├── Move (WASD/Arrow keys)
├── Attack (bump into enemy)
├── Use Item (inventory)
├── Use Ability (if learned)
├── Wait (skip turn)
└── Interact (doors, stairs, chests)

Enemy Turn:
├── AI decides action
├── Move toward player (if aggressive)
├── Attack if adjacent
└── Use abilities (if any)

End Turn:
├── Process status effects
├── Tick cooldowns
├── Regenerate resources
└── Check win/lose conditions
```

### Dungeon Structure

```
Floor 1-2:   Tutorial, weak enemies, basic items
Floor 3-4:   Normal difficulty, varied enemies
Floor 5-6:   Challenging, special enemies
Floor 7-8:   Hard, rare items
Floor 9:     Pre-boss gauntlet
Floor 10:    Final boss
```

### Stats

| Stat | Description |
|------|-------------|
| **HP** | Health points, die at 0 |
| **Attack** | Base melee damage |
| **Defense** | Damage reduction |
| **Speed** | Turn order priority |
| **Accuracy** | Hit chance modifier |
| **Evasion** | Dodge chance |

---

## Component Architecture

### Core Components

```csharp
/// <summary>
/// Position on the dungeon grid.
/// </summary>
[Component]
public partial struct GridPosition
{
    public int X;
    public int Y;
}

/// <summary>
/// Entity occupies space and blocks movement.
/// </summary>
[TagComponent]
public partial struct BlocksMovement;

/// <summary>
/// Entity blocks line of sight.
/// </summary>
[TagComponent]
public partial struct BlocksSight;

/// <summary>
/// Entity's name for display.
/// </summary>
[Component]
public partial struct Named
{
    public string Name;
}

/// <summary>
/// Visual representation.
/// </summary>
[Component]
public partial struct Renderable
{
    /// <summary>ASCII character or sprite ID.</summary>
    public char Glyph;

    /// <summary>Foreground color.</summary>
    public Color ForegroundColor;

    /// <summary>Background color (optional).</summary>
    public Color BackgroundColor;

    /// <summary>Render layer (higher = on top).</summary>
    public int Layer;
}

/// <summary>
/// Health and death tracking.
/// </summary>
[Component]
public partial struct Health
{
    public int Current;
    public int Max;

    public bool IsAlive => Current > 0;
    public float Percentage => (float)Current / Max;
}

/// <summary>
/// Combat statistics.
/// </summary>
[Component]
public partial struct CombatStats
{
    public int Attack;
    public int Defense;
    public int Speed;
    public int Accuracy;
    public int Evasion;
}

/// <summary>
/// Experience and leveling.
/// </summary>
[Component]
public partial struct Experience
{
    public int Level;
    public int Current;
    public int ToNextLevel;
}

/// <summary>
/// XP reward when killed.
/// </summary>
[Component]
public partial struct ExperienceReward
{
    public int Amount;
}
```

### Actor Components

```csharp
/// <summary>
/// Marks entity as the player.
/// </summary>
[TagComponent]
public partial struct Player;

/// <summary>
/// Marks entity as an enemy.
/// </summary>
[TagComponent]
public partial struct Enemy;

/// <summary>
/// Entity can take turns.
/// </summary>
[Component]
public partial struct TurnActor
{
    /// <summary>Energy for turn system (acts when >= 100).</summary>
    public int Energy;

    /// <summary>Energy gained per tick (based on speed).</summary>
    public int EnergyPerTick;
}

/// <summary>
/// AI behavior type.
/// </summary>
[Component]
public partial struct AIBehavior
{
    public AIType Type;
    public Entity Target;
}

public enum AIType
{
    Aggressive,     // Chases and attacks player
    Passive,        // Ignores unless attacked
    Cowardly,       // Runs away when hurt
    Stationary,     // Doesn't move
    Patrol,         // Follows a path
}

/// <summary>
/// Field of view data.
/// </summary>
[Component]
public partial struct FieldOfView
{
    public int Range;
    public HashSet<(int X, int Y)> VisibleTiles;
    public bool IsDirty;
}
```

### Item Components

```csharp
/// <summary>
/// Marks entity as an item.
/// </summary>
[TagComponent]
public partial struct Item;

/// <summary>
/// Item is in an inventory.
/// </summary>
[Component]
public partial struct InInventory
{
    /// <summary>Owner entity.</summary>
    public Entity Owner;
}

/// <summary>
/// Item is on the ground.
/// </summary>
[TagComponent]
public partial struct OnGround;

/// <summary>
/// Item can be picked up.
/// </summary>
[TagComponent]
public partial struct Pickupable;

/// <summary>
/// Item can be equipped.
/// </summary>
[Component]
public partial struct Equipable
{
    public EquipmentSlot Slot;
}

public enum EquipmentSlot
{
    MainHand,
    OffHand,
    Head,
    Body,
    Feet,
    Ring1,
    Ring2,
    Amulet,
}

/// <summary>
/// Item is currently equipped.
/// </summary>
[Component]
public partial struct Equipped
{
    /// <summary>Who has this equipped.</summary>
    public Entity Owner;

    /// <summary>Which slot it's in.</summary>
    public EquipmentSlot Slot;
}

/// <summary>
/// Item can be consumed (potion, food, scroll).
/// </summary>
[TagComponent]
public partial struct Consumable;

/// <summary>
/// Limited uses before breaking.
/// </summary>
[Component]
public partial struct Durability
{
    public int Current;
    public int Max;
}

/// <summary>
/// Item value for shops.
/// </summary>
[Component]
public partial struct Value
{
    public int Gold;
}

/// <summary>
/// Item rarity affects stats and color.
/// </summary>
[Component]
public partial struct Rarity
{
    public ItemRarity Tier;
}

public enum ItemRarity
{
    Common,     // White
    Uncommon,   // Green
    Rare,       // Blue
    Epic,       // Purple
    Legendary,  // Orange
}
```

### Damage & Effect Components

```csharp
/// <summary>
/// Weapon deals melee damage.
/// </summary>
[Component]
public partial struct MeleeDamage
{
    public int MinDamage;
    public int MaxDamage;
}

/// <summary>
/// Weapon deals ranged damage.
/// </summary>
[Component]
public partial struct RangedDamage
{
    public int MinDamage;
    public int MaxDamage;
    public int Range;
}

/// <summary>
/// Adds elemental damage.
/// </summary>
[Component]
public partial struct ElementalDamage
{
    public DamageType Type;
    public int MinDamage;
    public int MaxDamage;
}

public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Poison,
    Holy,
    Dark,
}

/// <summary>
/// Provides armor rating.
/// </summary>
[Component]
public partial struct ArmorRating
{
    public int Value;
}

/// <summary>
/// Provides stat bonuses when equipped.
/// </summary>
[Component]
public partial struct StatBonus
{
    public int Attack;
    public int Defense;
    public int Speed;
    public int MaxHealth;
}

/// <summary>
/// Provides elemental resistance.
/// </summary>
[Component]
public partial struct Resistance
{
    public DamageType Type;
    /// <summary>Percentage reduction (0-100).</summary>
    public int Percentage;
}

/// <summary>
/// When consumed, heals the user.
/// </summary>
[Component]
public partial struct HealsOnUse
{
    public int Amount;
}

/// <summary>
/// When consumed, applies a status effect.
/// </summary>
[Component]
public partial struct AppliesEffectOnUse
{
    public StatusEffectType Effect;
    public int Duration;
    public int Potency;
}

/// <summary>
/// When consumed, deals damage to target.
/// </summary>
[Component]
public partial struct DamagesOnUse
{
    public DamageType Type;
    public int MinDamage;
    public int MaxDamage;
    public int Range;
    public bool RequiresTarget;
}

/// <summary>
/// When consumed, teleports user.
/// </summary>
[Component]
public partial struct TeleportsOnUse
{
    public TeleportType Type;
}

public enum TeleportType
{
    Random,         // Random position on floor
    Blink,          // Short range targeted
    Stairs,         // To nearest stairs
}

/// <summary>
/// When consumed, reveals map.
/// </summary>
[TagComponent]
public partial struct RevealsMapOnUse;

/// <summary>
/// Chance to apply effect on hit.
/// </summary>
[Component]
public partial struct OnHitEffect
{
    public StatusEffectType Effect;
    /// <summary>Chance 0-100.</summary>
    public int Chance;
    public int Duration;
    public int Potency;
}
```

### Status Effect Components

```csharp
public enum StatusEffectType
{
    Poisoned,
    Burning,
    Frozen,
    Stunned,
    Bleeding,
    Regenerating,
    Strengthened,
    Weakened,
    Hasted,
    Slowed,
    Invisible,
    Confused,
}

/// <summary>
/// Taking poison damage over time.
/// </summary>
[Component]
public partial struct Poisoned
{
    public int DamagePerTurn;
    public int TurnsRemaining;
    public Entity Source;
}

/// <summary>
/// Taking fire damage over time.
/// </summary>
[Component]
public partial struct Burning
{
    public int DamagePerTurn;
    public int TurnsRemaining;
}

/// <summary>
/// Cannot act.
/// </summary>
[Component]
public partial struct Stunned
{
    public int TurnsRemaining;
}

/// <summary>
/// Movement speed reduced.
/// </summary>
[Component]
public partial struct Slowed
{
    /// <summary>Speed multiplier (0.5 = half speed).</summary>
    public float Multiplier;
    public int TurnsRemaining;
}

/// <summary>
/// Movement speed increased.
/// </summary>
[Component]
public partial struct Hasted
{
    /// <summary>Speed multiplier (2.0 = double speed).</summary>
    public float Multiplier;
    public int TurnsRemaining;
}

/// <summary>
/// Healing over time.
/// </summary>
[Component]
public partial struct Regenerating
{
    public int HealPerTurn;
    public int TurnsRemaining;
}

/// <summary>
/// Attack increased.
/// </summary>
[Component]
public partial struct Strengthened
{
    public int BonusDamage;
    public int TurnsRemaining;
}

/// <summary>
/// Attack decreased.
/// </summary>
[Component]
public partial struct Weakened
{
    /// <summary>Damage multiplier (0.5 = half damage).</summary>
    public float Multiplier;
    public int TurnsRemaining;
}

/// <summary>
/// Cannot be seen by enemies.
/// </summary>
[Component]
public partial struct Invisible
{
    public int TurnsRemaining;
}

/// <summary>
/// Random movement direction.
/// </summary>
[Component]
public partial struct Confused
{
    public int TurnsRemaining;
}
```

### Environment Components

```csharp
/// <summary>
/// Marks a tile as a wall.
/// </summary>
[TagComponent]
public partial struct Wall;

/// <summary>
/// Marks a tile as floor.
/// </summary>
[TagComponent]
public partial struct Floor;

/// <summary>
/// Stairs to next/previous floor.
/// </summary>
[Component]
public partial struct Stairs
{
    public StairsDirection Direction;
}

public enum StairsDirection
{
    Down,
    Up,
}

/// <summary>
/// A door that can be opened/closed.
/// </summary>
[Component]
public partial struct Door
{
    public bool IsOpen;
    public bool IsLocked;
    public int KeyId;
}

/// <summary>
/// A chest containing items.
/// </summary>
[Component]
public partial struct Container
{
    public bool IsOpen;
    public bool IsLocked;
    public int KeyId;
}

/// <summary>
/// A trap that triggers on step.
/// </summary>
[Component]
public partial struct Trap
{
    public TrapType Type;
    public int Damage;
    public bool IsVisible;
    public bool IsTriggered;
}

public enum TrapType
{
    Spike,
    Fire,
    Poison,
    Teleport,
    Alarm,
}

/// <summary>
/// Spawns enemies when triggered.
/// </summary>
[Component]
public partial struct Spawner
{
    public int EnemyPrefabId;
    public int SpawnCount;
    public int TurnsUntilSpawn;
    public int MaxSpawned;
    public int CurrentlySpawned;
}
```

---

## Systems Design

### System Execution Order

```
Phase: TurnStart
├── EnergySystem           (Order: 0)   - Grant energy to actors
├── TurnOrderSystem        (Order: 10)  - Determine who acts

Phase: PlayerTurn (only if player has enough energy)
├── PlayerInputSystem      (Order: 0)   - Wait for player input
├── MovementSystem         (Order: 10)  - Process movement
├── AttackSystem           (Order: 20)  - Process attacks
├── ItemUseSystem          (Order: 30)  - Process item usage
├── InteractionSystem      (Order: 40)  - Doors, stairs, etc.

Phase: EnemyTurn (for each enemy with enough energy)
├── AISystem               (Order: 0)   - Decide action
├── MovementSystem         (Order: 10)  - Process movement
├── AttackSystem           (Order: 20)  - Process attacks

Phase: TurnEnd
├── StatusEffectSystem     (Order: 0)   - Tick effects, apply damage
├── DeathSystem            (Order: 10)  - Handle deaths
├── ExperienceSystem       (Order: 20)  - Grant XP, level up
├── PickupSystem           (Order: 30)  - Auto-pickup gold
├── FieldOfViewSystem      (Order: 40)  - Recalculate visibility
├── SpawnerSystem          (Order: 50)  - Spawn new enemies

Phase: Render
├── MapRenderSystem        (Order: 0)   - Render tiles
├── EntityRenderSystem     (Order: 10)  - Render entities
├── UIRenderSystem         (Order: 20)  - Health bar, inventory
├── MessageLogSystem       (Order: 30)  - Combat messages
```

### Core Systems

```csharp
/// <summary>
/// Manages energy-based turn order.
/// Entities act when they reach 100 energy.
/// </summary>
public class EnergySystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<TurnActor, CombatStats>())
        {
            ref var actor = ref World.Get<TurnActor>(entity);
            ref readonly var stats = ref World.Get<CombatStats>(entity);

            // Speed affects energy gain
            actor.EnergyPerTick = 10 + stats.Speed;
            actor.Energy += actor.EnergyPerTick;
        }
    }
}

/// <summary>
/// Processes movement on the grid.
/// </summary>
public class MovementSystem : SystemBase
{
    public void TryMove(Entity entity, int dx, int dy)
    {
        ref readonly var pos = ref World.Get<GridPosition>(entity);
        int newX = pos.X + dx;
        int newY = pos.Y + dy;

        // Check bounds
        var map = World.GetSingleton<DungeonMap>();
        if (!map.IsInBounds(newX, newY))
            return;

        // Check for blockers
        foreach (var blocker in World.Query<GridPosition>().With<BlocksMovement>())
        {
            ref readonly var blockerPos = ref World.Get<GridPosition>(blocker);
            if (blockerPos.X == newX && blockerPos.Y == newY)
            {
                // Bump attack if enemy
                if (World.Has<Enemy>(blocker) && World.Has<Player>(entity))
                {
                    World.Send(new AttackEvent { Attacker = entity, Defender = blocker });
                }
                else if (World.Has<Player>(blocker) && World.Has<Enemy>(entity))
                {
                    World.Send(new AttackEvent { Attacker = entity, Defender = blocker });
                }
                return;
            }
        }

        // Check for doors
        foreach (var door in World.Query<GridPosition, Door>())
        {
            ref readonly var doorPos = ref World.Get<GridPosition>(door);
            if (doorPos.X == newX && doorPos.Y == newY)
            {
                ref var doorData = ref World.Get<Door>(door);
                if (!doorData.IsOpen)
                {
                    if (doorData.IsLocked)
                    {
                        // Check for key
                        if (HasKey(entity, doorData.KeyId))
                        {
                            doorData.IsLocked = false;
                            doorData.IsOpen = true;
                            World.Remove<BlocksMovement>(door);
                            World.Remove<BlocksSight>(door);
                        }
                        return;
                    }
                    doorData.IsOpen = true;
                    World.Remove<BlocksMovement>(door);
                    World.Remove<BlocksSight>(door);
                }
            }
        }

        // Move
        ref var position = ref World.Get<GridPosition>(entity);
        position.X = newX;
        position.Y = newY;

        // Mark FOV dirty
        if (World.Has<FieldOfView>(entity))
        {
            ref var fov = ref World.Get<FieldOfView>(entity);
            fov.IsDirty = true;
        }

        // Check for traps
        foreach (var trap in World.Query<GridPosition, Trap>())
        {
            ref readonly var trapPos = ref World.Get<GridPosition>(trap);
            if (trapPos.X == newX && trapPos.Y == newY)
            {
                TriggerTrap(entity, trap);
            }
        }
    }
}

/// <summary>
/// Calculates field of view using shadowcasting.
/// </summary>
public class FieldOfViewSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<GridPosition, FieldOfView>())
        {
            ref var fov = ref World.Get<FieldOfView>(entity);
            if (!fov.IsDirty) continue;

            ref readonly var pos = ref World.Get<GridPosition>(entity);
            fov.VisibleTiles.Clear();

            // Shadowcasting algorithm
            ComputeFov(pos.X, pos.Y, fov.Range, fov.VisibleTiles);
            fov.IsDirty = false;
        }
    }

    private void ComputeFov(int originX, int originY, int range, HashSet<(int, int)> visible)
    {
        // Implementation of recursive shadowcasting
        // See: http://www.roguebasin.com/index.php/FOV_using_recursive_shadowcasting
        visible.Add((originX, originY));

        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(originX, originY, range, 1, 1.0f, 0.0f,
                      Multipliers[0, octant], Multipliers[1, octant],
                      Multipliers[2, octant], Multipliers[3, octant],
                      visible);
        }
    }
}

/// <summary>
/// Processes status effects each turn.
/// </summary>
public class StatusEffectSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        // Poison damage
        foreach (var entity in World.Query<Health, Poisoned>())
        {
            ref var health = ref World.Get<Health>(entity);
            ref var poison = ref World.Get<Poisoned>(entity);

            health.Current -= poison.DamagePerTurn;
            poison.TurnsRemaining--;

            if (poison.TurnsRemaining <= 0)
                buffer.Remove<Poisoned>(entity);
        }

        // Burning damage
        foreach (var entity in World.Query<Health, Burning>())
        {
            ref var health = ref World.Get<Health>(entity);
            ref var burning = ref World.Get<Burning>(entity);

            health.Current -= burning.DamagePerTurn;
            burning.TurnsRemaining--;

            if (burning.TurnsRemaining <= 0)
                buffer.Remove<Burning>(entity);
        }

        // Regeneration
        foreach (var entity in World.Query<Health, Regenerating>())
        {
            ref var health = ref World.Get<Health>(entity);
            ref var regen = ref World.Get<Regenerating>(entity);

            health.Current = Math.Min(health.Current + regen.HealPerTurn, health.Max);
            regen.TurnsRemaining--;

            if (regen.TurnsRemaining <= 0)
                buffer.Remove<Regenerating>(entity);
        }

        // Tick durations on other effects
        TickDuration<Stunned>(buffer);
        TickDuration<Slowed>(buffer);
        TickDuration<Hasted>(buffer);
        TickDuration<Strengthened>(buffer);
        TickDuration<Weakened>(buffer);
        TickDuration<Invisible>(buffer);
        TickDuration<Confused>(buffer);

        buffer.Playback();
    }

    private void TickDuration<T>(CommandBuffer buffer) where T : struct
    {
        // Generic duration tick - would use reflection or source gen
    }
}
```

---

## Item System

### Item Composition Examples

```csharp
/// <summary>
/// Item factory demonstrating composition.
/// </summary>
public static class ItemFactory
{
    /// <summary>
    /// Basic iron sword.
    /// </summary>
    public static Entity CreateIronSword(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Iron Sword" })
            .With(new Renderable { Glyph = '/', ForegroundColor = Color.Gray, Layer = 1 })
            .With(new Equipable { Slot = EquipmentSlot.MainHand })
            .With(new MeleeDamage { MinDamage = 3, MaxDamage = 8 })
            .With(new Durability { Current = 50, Max = 50 })
            .With(new Value { Gold = 50 })
            .With(new Rarity { Tier = ItemRarity.Common })
            .WithTag<Item>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }

    /// <summary>
    /// Flaming sword - iron sword + fire damage + burn on hit.
    /// </summary>
    public static Entity CreateFlamingSword(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Flaming Sword" })
            .With(new Renderable { Glyph = '/', ForegroundColor = Color.Orange, Layer = 1 })
            .With(new Equipable { Slot = EquipmentSlot.MainHand })
            .With(new MeleeDamage { MinDamage = 4, MaxDamage = 10 })
            .With(new ElementalDamage { Type = DamageType.Fire, MinDamage = 2, MaxDamage = 5 })
            .With(new OnHitEffect
            {
                Effect = StatusEffectType.Burning,
                Chance = 30,
                Duration = 3,
                Potency = 2
            })
            .With(new Durability { Current = 40, Max = 40 })
            .With(new Value { Gold = 200 })
            .With(new Rarity { Tier = ItemRarity.Rare })
            .WithTag<Item>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }

    /// <summary>
    /// Health potion - consumable healing.
    /// </summary>
    public static Entity CreateHealthPotion(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Health Potion" })
            .With(new Renderable { Glyph = '!', ForegroundColor = Color.Red, Layer = 1 })
            .With(new HealsOnUse { Amount = 25 })
            .With(new Value { Gold = 30 })
            .With(new Rarity { Tier = ItemRarity.Common })
            .WithTag<Item>()
            .WithTag<Consumable>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }

    /// <summary>
    /// Poison potion - throwable damage + poison.
    /// </summary>
    public static Entity CreatePoisonPotion(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Poison Flask" })
            .With(new Renderable { Glyph = '!', ForegroundColor = Color.Green, Layer = 1 })
            .With(new DamagesOnUse
            {
                Type = DamageType.Poison,
                MinDamage = 5,
                MaxDamage = 10,
                Range = 5,
                RequiresTarget = true
            })
            .With(new AppliesEffectOnUse
            {
                Effect = StatusEffectType.Poisoned,
                Duration = 5,
                Potency = 3
            })
            .With(new Value { Gold = 40 })
            .With(new Rarity { Tier = ItemRarity.Uncommon })
            .WithTag<Item>()
            .WithTag<Consumable>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }

    /// <summary>
    /// Scroll of teleportation.
    /// </summary>
    public static Entity CreateTeleportScroll(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Scroll of Teleportation" })
            .With(new Renderable { Glyph = '?', ForegroundColor = Color.Cyan, Layer = 1 })
            .With(new TeleportsOnUse { Type = TeleportType.Random })
            .With(new Value { Gold = 75 })
            .With(new Rarity { Tier = ItemRarity.Uncommon })
            .WithTag<Item>()
            .WithTag<Consumable>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }

    /// <summary>
    /// Leather armor.
    /// </summary>
    public static Entity CreateLeatherArmor(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Leather Armor" })
            .With(new Renderable { Glyph = '[', ForegroundColor = Color.Brown, Layer = 1 })
            .With(new Equipable { Slot = EquipmentSlot.Body })
            .With(new ArmorRating { Value = 3 })
            .With(new StatBonus { Defense = 2 })
            .With(new Durability { Current = 30, Max = 30 })
            .With(new Value { Gold = 60 })
            .With(new Rarity { Tier = ItemRarity.Common })
            .WithTag<Item>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }

    /// <summary>
    /// Ring of fire resistance.
    /// </summary>
    public static Entity CreateFireResistRing(World world, int x, int y)
    {
        return world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Named { Name = "Ring of Fire Resistance" })
            .With(new Renderable { Glyph = '=', ForegroundColor = Color.Red, Layer = 1 })
            .With(new Equipable { Slot = EquipmentSlot.Ring1 })
            .With(new Resistance { Type = DamageType.Fire, Percentage = 50 })
            .With(new Value { Gold = 150 })
            .With(new Rarity { Tier = ItemRarity.Rare })
            .WithTag<Item>()
            .WithTag<OnGround>()
            .WithTag<Pickupable>()
            .Build();
    }
}
```

### Equipment System

```csharp
/// <summary>
/// Handles equipping and unequipping items.
/// </summary>
public class EquipmentSystem : SystemBase
{
    public void Equip(Entity owner, Entity item)
    {
        if (!World.Has<Equipable>(item))
            return;

        ref readonly var equipable = ref World.Get<Equipable>(item);
        var slot = equipable.Slot;

        // Unequip existing item in slot
        foreach (var equipped in World.Query<Equipped>().With<Item>())
        {
            ref readonly var eq = ref World.Get<Equipped>(equipped);
            if (eq.Owner == owner && eq.Slot == slot)
            {
                Unequip(owner, equipped);
                break;
            }
        }

        // Move from inventory to equipped
        if (World.Has<InInventory>(item))
            World.Remove<InInventory>(item);

        World.Add(item, new Equipped { Owner = owner, Slot = slot });

        // Apply stat bonuses
        ApplyItemBonuses(owner, item);
    }

    public void Unequip(Entity owner, Entity item)
    {
        if (!World.Has<Equipped>(item))
            return;

        // Remove stat bonuses
        RemoveItemBonuses(owner, item);

        // Move to inventory
        World.Remove<Equipped>(item);
        World.Add(item, new InInventory { Owner = owner });
    }

    private void ApplyItemBonuses(Entity owner, Entity item)
    {
        if (!World.Has<CombatStats>(owner))
            return;

        ref var stats = ref World.Get<CombatStats>(owner);

        if (World.Has<StatBonus>(item))
        {
            ref readonly var bonus = ref World.Get<StatBonus>(item);
            stats.Attack += bonus.Attack;
            stats.Defense += bonus.Defense;
            stats.Speed += bonus.Speed;
        }

        if (World.Has<Health>(owner) && World.Has<StatBonus>(item))
        {
            ref var health = ref World.Get<Health>(owner);
            ref readonly var bonus = ref World.Get<StatBonus>(item);
            health.Max += bonus.MaxHealth;
            health.Current += bonus.MaxHealth;
        }
    }
}

/// <summary>
/// Calculates total combat stats including equipment.
/// </summary>
public static class CombatCalculator
{
    public static int CalculateTotalDamage(World world, Entity attacker)
    {
        int damage = 0;

        // Base stats
        if (world.Has<CombatStats>(attacker))
        {
            damage += world.Get<CombatStats>(attacker).Attack;
        }

        // Weapon damage
        foreach (var item in world.Query<Equipped, MeleeDamage>())
        {
            ref readonly var eq = ref world.Get<Equipped>(item);
            if (eq.Owner != attacker) continue;

            ref readonly var weapon = ref world.Get<MeleeDamage>(item);
            damage += Random.Shared.Next(weapon.MinDamage, weapon.MaxDamage + 1);
        }

        // Strength buff
        if (world.Has<Strengthened>(attacker))
        {
            damage += world.Get<Strengthened>(attacker).BonusDamage;
        }

        // Weakness debuff
        if (world.Has<Weakened>(attacker))
        {
            damage = (int)(damage * world.Get<Weakened>(attacker).Multiplier);
        }

        return damage;
    }

    public static int CalculateTotalDefense(World world, Entity defender)
    {
        int defense = 0;

        // Base stats
        if (world.Has<CombatStats>(defender))
        {
            defense += world.Get<CombatStats>(defender).Defense;
        }

        // Armor from equipment
        foreach (var item in world.Query<Equipped, ArmorRating>())
        {
            ref readonly var eq = ref world.Get<Equipped>(item);
            if (eq.Owner != defender) continue;

            defense += world.Get<ArmorRating>(item).Value;
        }

        return defense;
    }
}
```

---

## Combat System

### Attack Resolution

```csharp
/// <summary>
/// Processes attack events.
/// </summary>
public class AttackSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var attackEvent in World.Receive<AttackEvent>())
        {
            ProcessAttack(attackEvent.Attacker, attackEvent.Defender);
        }
    }

    private void ProcessAttack(Entity attacker, Entity defender)
    {
        if (!World.IsAlive(attacker) || !World.IsAlive(defender))
            return;

        // Calculate hit chance
        int accuracy = World.Has<CombatStats>(attacker)
            ? World.Get<CombatStats>(attacker).Accuracy
            : 80;
        int evasion = World.Has<CombatStats>(defender)
            ? World.Get<CombatStats>(defender).Evasion
            : 0;

        int hitChance = Math.Clamp(accuracy - evasion + 50, 5, 95);

        if (Random.Shared.Next(100) >= hitChance)
        {
            // Miss!
            LogMessage($"{GetName(attacker)} misses {GetName(defender)}!");
            return;
        }

        // Calculate damage
        int damage = CombatCalculator.CalculateTotalDamage(World, attacker);
        int defense = CombatCalculator.CalculateTotalDefense(World, defender);
        int finalDamage = Math.Max(1, damage - defense);

        // Apply damage
        ref var health = ref World.Get<Health>(defender);
        health.Current -= finalDamage;

        LogMessage($"{GetName(attacker)} hits {GetName(defender)} for {finalDamage} damage!");

        // Process on-hit effects from weapons
        ProcessOnHitEffects(attacker, defender);

        // Apply elemental damage
        ProcessElementalDamage(attacker, defender);

        // Reduce weapon durability
        ReduceWeaponDurability(attacker);
    }

    private void ProcessOnHitEffects(Entity attacker, Entity defender)
    {
        foreach (var item in World.Query<Equipped, OnHitEffect>())
        {
            ref readonly var eq = ref World.Get<Equipped>(item);
            if (eq.Owner != attacker) continue;

            ref readonly var effect = ref World.Get<OnHitEffect>(item);

            if (Random.Shared.Next(100) < effect.Chance)
            {
                ApplyStatusEffect(defender, effect.Effect, effect.Duration, effect.Potency);
            }
        }
    }

    private void ProcessElementalDamage(Entity attacker, Entity defender)
    {
        foreach (var item in World.Query<Equipped, ElementalDamage>())
        {
            ref readonly var eq = ref World.Get<Equipped>(item);
            if (eq.Owner != attacker) continue;

            ref readonly var elem = ref World.Get<ElementalDamage>(item);
            int elemDamage = Random.Shared.Next(elem.MinDamage, elem.MaxDamage + 1);

            // Check resistance
            int resistance = GetResistance(defender, elem.Type);
            elemDamage = (int)(elemDamage * (100 - resistance) / 100f);

            if (elemDamage > 0)
            {
                ref var health = ref World.Get<Health>(defender);
                health.Current -= elemDamage;

                LogMessage($"{GetName(defender)} takes {elemDamage} {elem.Type} damage!");
            }
        }
    }
}
```

---

## Procedural Generation

### Dungeon Generator

```csharp
/// <summary>
/// Generates dungeon floors using BSP.
/// </summary>
public class DungeonGenerator
{
    private readonly World world;
    private readonly int width;
    private readonly int height;

    public DungeonMap Generate(int floor)
    {
        var map = new DungeonMap(width, height);

        // BSP room generation
        var rooms = GenerateRooms(floor);

        // Carve rooms
        foreach (var room in rooms)
        {
            CarveRoom(map, room);
        }

        // Connect rooms with corridors
        ConnectRooms(map, rooms);

        // Place stairs
        PlaceStairs(map, rooms, floor);

        // Spawn enemies
        SpawnEnemies(map, rooms, floor);

        // Place items
        PlaceItems(map, rooms, floor);

        // Place traps
        PlaceTraps(map, rooms, floor);

        return map;
    }

    private List<Room> GenerateRooms(int floor)
    {
        var rooms = new List<Room>();
        int roomCount = 8 + floor; // More rooms on deeper floors

        for (int i = 0; i < roomCount * 3; i++) // Try extra times
        {
            if (rooms.Count >= roomCount) break;

            int roomWidth = Random.Shared.Next(5, 12);
            int roomHeight = Random.Shared.Next(5, 10);
            int x = Random.Shared.Next(1, width - roomWidth - 1);
            int y = Random.Shared.Next(1, height - roomHeight - 1);

            var room = new Room(x, y, roomWidth, roomHeight);

            // Check overlap
            bool overlaps = rooms.Any(r => r.Intersects(room, padding: 2));
            if (!overlaps)
            {
                rooms.Add(room);
            }
        }

        return rooms;
    }

    private void SpawnEnemies(DungeonMap map, List<Room> rooms, int floor)
    {
        foreach (var room in rooms.Skip(1)) // Skip first room (player spawn)
        {
            int enemyCount = Random.Shared.Next(0, 3 + floor / 3);

            for (int i = 0; i < enemyCount; i++)
            {
                var pos = room.GetRandomPosition();
                if (!map.IsBlocked(pos.X, pos.Y))
                {
                    SpawnEnemy(pos.X, pos.Y, floor);
                }
            }
        }

        // Boss on every 5th floor
        if (floor % 5 == 0)
        {
            var bossRoom = rooms[^1];
            var pos = bossRoom.Center;
            SpawnBoss(pos.X, pos.Y, floor);
        }
    }

    private void SpawnEnemy(int x, int y, int floor)
    {
        var enemyType = ChooseEnemyType(floor);

        var builder = world.Spawn()
            .With(new GridPosition { X = x, Y = y })
            .With(new Health { Current = 20 + floor * 5, Max = 20 + floor * 5 })
            .With(new CombatStats
            {
                Attack = 5 + floor,
                Defense = 2 + floor / 2,
                Speed = 8,
                Accuracy = 75,
                Evasion = 5
            })
            .With(new TurnActor { Energy = 0, EnergyPerTick = 10 })
            .With(new AIBehavior { Type = AIType.Aggressive })
            .With(new ExperienceReward { Amount = 10 + floor * 3 })
            .With(new FieldOfView { Range = 8 })
            .WithTag<Enemy>()
            .WithTag<BlocksMovement>();

        switch (enemyType)
        {
            case EnemyType.Goblin:
                builder
                    .With(new Named { Name = "Goblin" })
                    .With(new Renderable { Glyph = 'g', ForegroundColor = Color.Green, Layer = 2 });
                break;

            case EnemyType.Orc:
                builder
                    .With(new Named { Name = "Orc" })
                    .With(new Renderable { Glyph = 'o', ForegroundColor = Color.DarkGreen, Layer = 2 })
                    .With(new Health { Current = 40 + floor * 8, Max = 40 + floor * 8 });
                break;

            case EnemyType.Skeleton:
                builder
                    .With(new Named { Name = "Skeleton" })
                    .With(new Renderable { Glyph = 's', ForegroundColor = Color.White, Layer = 2 })
                    .With(new Resistance { Type = DamageType.Poison, Percentage = 100 });
                break;

            case EnemyType.FireImp:
                builder
                    .With(new Named { Name = "Fire Imp" })
                    .With(new Renderable { Glyph = 'i', ForegroundColor = Color.Orange, Layer = 2 })
                    .With(new Resistance { Type = DamageType.Fire, Percentage = 100 })
                    .With(new ElementalDamage { Type = DamageType.Fire, MinDamage = 2, MaxDamage = 4 });
                break;
        }

        builder.Build();
    }
}
```

---

## Implementation Plan

### Phase 1: Core (4-5 files)

1. GridPosition, Renderable, Named components
2. DungeonMap data structure
3. Basic dungeon generation (rooms + corridors)
4. Player movement on grid
5. Simple ASCII rendering

**Milestone:** Walking in a dungeon

### Phase 2: Combat (3-4 files)

1. Health, CombatStats components
2. Enemy spawning
3. Bump-to-attack combat
4. Death handling
5. Turn/energy system

**Milestone:** Fighting enemies

### Phase 3: Items (4-5 files)

1. Item component hierarchy
2. Inventory system (InInventory)
3. Equipment system (Equipped)
4. Item pickup
5. Basic weapon effects

**Milestone:** Equipping weapons

### Phase 4: Effects (3 files)

1. Status effect components
2. StatusEffectSystem
3. On-hit effect processing
4. Potions and consumables

**Milestone:** Status effects working

### Phase 5: FOV & AI (2-3 files)

1. Field of view calculation
2. Map memory (explored tiles)
3. Basic enemy AI
4. Pathfinding

**Milestone:** Tactical gameplay

### Phase 6: Polish (3-4 files)

1. Multiple floors
2. Experience and leveling
3. Traps and doors
4. Better procedural generation
5. Win/lose conditions

**Milestone:** Complete roguelike

---

## Query Examples

The roguelike showcases KeenEyes' query flexibility:

```csharp
// Find all items in player's inventory
World.Query<InInventory>()
    .With<Item>()
    .Where(e => World.Get<InInventory>(e).Owner == player)

// Find all equipped weapons
World.Query<Equipped, MeleeDamage>()
    .With<Item>()

// Find visible enemies
World.Query<GridPosition, Health>()
    .With<Enemy>()
    .Where(e => playerFov.VisibleTiles.Contains(GetPos(e)))

// Find consumable healing items
World.Query<HealsOnUse>()
    .With<Item>()
    .With<Consumable>()
    .With<InInventory>()

// Find all poisoned entities
World.Query<Health, Poisoned>()
```

---

## Design Principles

### Everything is an Entity

| Concept | Implementation |
|---------|----------------|
| Player | Entity with Player tag |
| Enemy | Entity with Enemy tag |
| Item | Entity with Item tag |
| Effect | Component on affected entity |
| Ability | Entity with ability components |

### Composition over Configuration

Instead of:
```csharp
new Sword(damage: 5, element: Fire, onHit: Burn)
```

Use:
```csharp
entity
    .With(MeleeDamage { ... })
    .With(ElementalDamage { Type = Fire, ... })
    .With(OnHitEffect { Effect = Burning, ... })
```

### Runtime Flexibility

```csharp
// Cursed item adds debuff when equipped
World.Add(player, new Weakened { ... });

// Blessing removes curse
World.Remove<Cursed>(item);

// Enchanting adds new properties
World.Add(sword, new ElementalDamage { Type = Lightning, ... });
```

---

## Future Enhancements

- **Character classes** - Different starting stats/abilities
- **Skill trees** - Unlock abilities as you level
- **Shops** - Buy/sell items
- **Quests** - Objectives beyond survival
- **Companions** - AI allies
- **Ranged combat** - Bows, thrown weapons, spells
- **Crafting** - Combine items
- **Achievements** - Meta progression
- **Daily runs** - Seeded dungeons
