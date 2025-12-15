# Timers & Cooldowns

## Problem

You need time-based mechanics: ability cooldowns, buff durations, delayed actions, or periodic effects.

## Solution

### Basic Timer Component

```csharp
[Component]
public partial struct Timer : IComponent
{
    public float Duration;
    public float Elapsed;
    public bool Loop;

    public readonly float Remaining => Duration - Elapsed;
    public readonly float Progress => Duration > 0 ? Elapsed / Duration : 1f;
    public readonly bool IsComplete => Elapsed >= Duration;
}
```

### Ability Cooldowns

```csharp
[Component]
public partial struct AbilityCooldown : IComponent
{
    public float FireballCooldown;
    public float DashCooldown;
    public float ShieldCooldown;
}

public class AbilitySystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Reduce all cooldowns
        foreach (var entity in World.Query<AbilityCooldown>())
        {
            ref var cd = ref World.Get<AbilityCooldown>(entity);

            cd.FireballCooldown = MathF.Max(0, cd.FireballCooldown - deltaTime);
            cd.DashCooldown = MathF.Max(0, cd.DashCooldown - deltaTime);
            cd.ShieldCooldown = MathF.Max(0, cd.ShieldCooldown - deltaTime);
        }
    }

    public bool TryCastFireball(Entity caster)
    {
        ref var cd = ref World.Get<AbilityCooldown>(caster);

        if (cd.FireballCooldown > 0)
            return false;  // Still on cooldown

        // Cast the spell
        SpawnFireball(caster);
        cd.FireballCooldown = 5f;  // 5 second cooldown
        return true;
    }
}
```

### Buff/Debuff System

```csharp
[Component]
public partial struct SpeedBuff : IComponent
{
    public float Multiplier;
    public float RemainingDuration;
}

[Component]
public partial struct PoisonDebuff : IComponent
{
    public int DamagePerTick;
    public float TickInterval;
    public float NextTickIn;
    public float RemainingDuration;
}

public class BuffSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        // Process speed buffs
        foreach (var entity in World.Query<SpeedBuff>())
        {
            ref var buff = ref World.Get<SpeedBuff>(entity);
            buff.RemainingDuration -= deltaTime;

            if (buff.RemainingDuration <= 0)
            {
                buffer.Remove<SpeedBuff>(entity);
            }
        }

        // Process poison (with periodic damage)
        foreach (var entity in World.Query<PoisonDebuff>())
        {
            ref var poison = ref World.Get<PoisonDebuff>(entity);
            poison.RemainingDuration -= deltaTime;
            poison.NextTickIn -= deltaTime;

            // Apply damage on tick
            if (poison.NextTickIn <= 0)
            {
                buffer.Add(entity, new DamageReceived { Amount = poison.DamagePerTick });
                poison.NextTickIn = poison.TickInterval;
            }

            // Remove expired debuff
            if (poison.RemainingDuration <= 0)
            {
                buffer.Remove<PoisonDebuff>(entity);
            }
        }

        buffer.Execute();
    }
}

// Usage
public void ApplySpeedBoost(Entity target)
{
    world.Add(target, new SpeedBuff
    {
        Multiplier = 1.5f,
        RemainingDuration = 10f  // 10 second boost
    });
}

public void ApplyPoison(Entity target, Entity source)
{
    world.Add(target, new PoisonDebuff
    {
        DamagePerTick = 5,
        TickInterval = 1f,
        NextTickIn = 1f,
        RemainingDuration = 8f  // 8 seconds of poison
    });
}
```

### Delayed Actions

```csharp
[Component]
public partial struct DelayedAction : IComponent
{
    public float Delay;
    public ActionType Type;
    public Entity Target;
}

public enum ActionType
{
    Explode,
    Spawn,
    Despawn,
    Heal
}

public class DelayedActionSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<DelayedAction>())
        {
            ref var action = ref World.Get<DelayedAction>(entity);
            action.Delay -= deltaTime;

            if (action.Delay <= 0)
            {
                ExecuteAction(buffer, entity, action);
                buffer.Despawn(entity);  // Action entity served its purpose
            }
        }

        buffer.Execute();
    }

    private void ExecuteAction(ICommandBuffer buffer, Entity actionEntity, DelayedAction action)
    {
        switch (action.Type)
        {
            case ActionType.Explode:
                CreateExplosion(buffer, World.Get<Position>(actionEntity));
                break;

            case ActionType.Spawn:
                // Spawn logic
                break;

            case ActionType.Despawn:
                if (World.IsAlive(action.Target))
                    buffer.Despawn(action.Target);
                break;

            case ActionType.Heal:
                if (World.IsAlive(action.Target))
                    buffer.Add(action.Target, new HealReceived { Amount = 50 });
                break;
        }
    }
}

// Usage: Grenade explodes after 3 seconds
var grenade = world.Spawn()
    .With(new Position { X = 100, Y = 100 })
    .With(new DelayedAction { Delay = 3f, Type = ActionType.Explode })
    .Build();
```

### Periodic Events

```csharp
[Component]
public partial struct PeriodicSpawner : IComponent
{
    public float Interval;
    public float Timer;
}

public class PeriodicSpawnerSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<PeriodicSpawner, Position>())
        {
            ref var spawner = ref World.Get<PeriodicSpawner>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);

            spawner.Timer += deltaTime;

            while (spawner.Timer >= spawner.Interval)
            {
                spawner.Timer -= spawner.Interval;

                // Spawn something
                buffer.Spawn()
                    .With(new Position { X = pos.X, Y = pos.Y })
                    .WithTag<SpawnedEntity>();
            }
        }

        buffer.Execute();
    }
}
```

## Why This Works

### Timers as Components

Making timers components means:
- They're updated automatically by systems
- They're serializable for save/load
- They're queryable: "Find all entities with active buffs"
- No hidden `Update()` callbacks or coroutines

### Separation of Timer and Effect

The timer tracks when; the system decides what:
- `SpeedBuff` has duration but no movement logic
- Movement system reads `SpeedBuff.Multiplier` when active
- `BuffSystem` handles expiration

### Accumulator Pattern for Periodic Effects

Using a timer that accumulates and subtracts intervals:
```csharp
while (spawner.Timer >= spawner.Interval)
{
    spawner.Timer -= spawner.Interval;
    // ...
}
```

This handles:
- Large delta times (multiple triggers in one frame)
- Consistent timing regardless of frame rate
- No drift over time

## Variations

### Generic Timer Entity

```csharp
[Component]
public partial struct TimerCallback : IComponent
{
    public Entity Target;
    public int CallbackId;  // Identifies which action to take
}

// Use an entity just for timing
var timer = world.Spawn()
    .With(new Timer { Duration = 5f })
    .With(new TimerCallback { Target = player, CallbackId = 1 })
    .Build();
```

### Buff Stacking

```csharp
[Component]
public partial struct StackingBuff : IComponent
{
    public int Stacks;
    public int MaxStacks;
    public float DurationPerStack;
    public float RemainingDuration;
}

public void AddStack(Entity target)
{
    if (World.TryGet<StackingBuff>(target, out var buff))
    {
        // Refresh or add stack
        buff.Stacks = Math.Min(buff.Stacks + 1, buff.MaxStacks);
        buff.RemainingDuration = buff.DurationPerStack * buff.Stacks;
        World.Set(target, buff);
    }
    else
    {
        // First stack
        World.Add(target, new StackingBuff
        {
            Stacks = 1,
            MaxStacks = 5,
            DurationPerStack = 3f,
            RemainingDuration = 3f
        });
    }
}
```

### Cooldown Reduction

```csharp
[Component]
public partial struct CooldownReduction : IComponent
{
    public float Percentage;  // 0.2 = 20% CDR
}

public class AbilitySystem : SystemBase
{
    public bool TryCastAbility(Entity caster, ref float cooldown, float baseCooldown)
    {
        if (cooldown > 0)
            return false;

        // Apply cooldown reduction
        float finalCooldown = baseCooldown;
        if (World.TryGet<CooldownReduction>(caster, out var cdr))
        {
            finalCooldown *= (1f - cdr.Percentage);
        }

        cooldown = finalCooldown;
        return true;
    }
}
```

### Visual Feedback Integration

```csharp
[Component]
public partial struct CooldownUI : IComponent
{
    public int AbilitySlot;
}

public class CooldownUISystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AbilityCooldown, CooldownUI>().With<Player>())
        {
            ref readonly var cd = ref World.Get<AbilityCooldown>(entity);
            ref readonly var ui = ref World.Get<CooldownUI>(entity);

            // Update UI with cooldown progress
            UIManager.SetCooldownProgress(0, cd.FireballCooldown / 5f);  // 5s max
            UIManager.SetCooldownProgress(1, cd.DashCooldown / 3f);      // 3s max
            UIManager.SetCooldownProgress(2, cd.ShieldCooldown / 15f);   // 15s max
        }
    }
}
```

## See Also

- [Health & Damage](health-damage.md) - Combine with damage-over-time
- [State Machines](state-machines.md) - Time-based state transitions
- [Systems Guide](../systems.md) - System execution timing
