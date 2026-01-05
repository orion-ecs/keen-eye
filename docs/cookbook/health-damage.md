# Health & Damage System

## Problem

You want entities to have health, take damage, heal, and die when health reaches zero.

## Solution

### Components

```csharp
[Component]
public partial struct Health
{
    public int Current;
    public int Max;
}

[Component]
public partial struct DamageReceived
{
    public int Amount;
    public Entity Source;  // Who dealt the damage
}

[Component]
public partial struct HealReceived
{
    public int Amount;
}

[TagComponent]
public partial struct Dead { }

[TagComponent]
public partial struct Invulnerable { }
```

### Damage Processing System

```csharp
public class DamageSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.Update;
    public override int Order => 100;  // Process after gameplay systems

    public override void Update(float deltaTime)
    {
        var buffer = new CommandBuffer();

        // Process damage
        foreach (var entity in World.Query<Health, DamageReceived>().Without<Invulnerable>())
        {
            ref var health = ref World.Get<Health>(entity);
            ref readonly var damage = ref World.Get<DamageReceived>(entity);

            health.Current -= damage.Amount;

            // Clamp to zero
            if (health.Current < 0)
                health.Current = 0;

            // Remove the damage component (it's been processed)
            buffer.RemoveComponent<DamageReceived>(entity);

            // Check for death
            if (health.Current == 0)
            {
                buffer.AddComponent(entity, default(Dead));
            }
        }

        // Process healing
        foreach (var entity in World.Query<Health, HealReceived>().Without<Dead>())
        {
            ref var health = ref World.Get<Health>(entity);
            ref readonly var heal = ref World.Get<HealReceived>(entity);

            health.Current += heal.Amount;

            // Clamp to max
            if (health.Current > health.Max)
                health.Current = health.Max;

            buffer.RemoveComponent<HealReceived>(entity);
        }

        buffer.Flush(World);
    }
}
```

### Death Handling System

```csharp
public class DeathSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.LateUpdate;

    public override void Update(float deltaTime)
    {
        var buffer = new CommandBuffer();

        foreach (var entity in World.Query<Dead>())
        {
            // Option 1: Destroy immediately
            buffer.Despawn(entity);

            // Option 2: Keep for death animation (see variations)
            // buffer.AddComponent(entity, new DeathAnimation { Timer = 2f });
        }

        buffer.Flush(World);
    }
}
```

### Usage

```csharp
using var world = new World();
world.AddSystem<DamageSystem>();
world.AddSystem<DeathSystem>();

// Create an entity with health
var enemy = world.Spawn()
    .With(new Health { Current = 100, Max = 100 })
    .Build();

// Deal damage by adding a component
world.Add(enemy, new DamageReceived { Amount = 30, Source = player });

// Heal by adding a component
world.Add(enemy, new HealReceived { Amount = 20 });

// Make temporarily invulnerable
world.Add<Invulnerable>(enemy);

// Update processes all damage/healing
world.Update(deltaTime);
```

## Why This Works

### Event-Driven via Components

Instead of calling `entity.TakeDamage(30)`, you add a `DamageReceived` component. Benefits:

1. **Decoupling**: Damage source doesn't need to know about health systems
2. **Batching**: All damage processed together in one system
3. **Queryable**: Can find "all entities that took damage this frame"
4. **Auditable**: The `Source` field tracks who dealt damage

### One Frame Lifecycle

Damage/heal components are:
1. Added by gameplay code
2. Processed by the damage system
3. Removed in the same frame

This prevents double-processing and keeps the component as a "this frame event."

### Invulnerability via Tag

Using `Without<Invulnerable>` in the query means:
- Invulnerable entities are skipped entirely
- No conditional logic in the damage loop
- Easy to add/remove invulnerability frames

### Death as State

The `Dead` tag serves multiple purposes:
- Prevents further healing (`Without<Dead>`)
- Queryable for death effects
- Can be checked by other systems (AI, rendering)

## Variations

### Damage Types

```csharp
public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Poison
}

[Component]
public partial struct DamageReceived
{
    public int Amount;
    public DamageType Type;
    public Entity Source;
}

[Component]
public partial struct DamageResistance
{
    public float Physical;  // 0 = no resistance, 1 = immune
    public float Fire;
    public float Ice;
    public float Poison;
}

// In system:
float resistance = type switch
{
    DamageType.Physical => res.Physical,
    DamageType.Fire => res.Fire,
    // ...
};
int finalDamage = (int)(damage.Amount * (1f - resistance));
```

### Damage Over Time

```csharp
[Component]
public partial struct PoisonEffect
{
    public int DamagePerSecond;
    public float RemainingDuration;
    public Entity Source;
}

public class PoisonSystem : SystemBase
{
    private float tickTimer = 0f;
    private const float TickInterval = 1f;

    public override void Update(float deltaTime)
    {
        tickTimer += deltaTime;

        foreach (var entity in World.Query<PoisonEffect>())
        {
            ref var poison = ref World.Get<PoisonEffect>(entity);
            poison.RemainingDuration -= deltaTime;

            // Apply damage every tick
            if (tickTimer >= TickInterval)
            {
                World.Add(entity, new DamageReceived
                {
                    Amount = poison.DamagePerSecond,
                    Source = poison.Source
                });
            }

            // Remove expired poison
            if (poison.RemainingDuration <= 0)
            {
                World.Remove<PoisonEffect>(entity);
            }
        }

        if (tickTimer >= TickInterval)
            tickTimer = 0f;
    }
}
```

### Death Animation

```csharp
[Component]
public partial struct DeathAnimation
{
    public float Timer;
}

public class DeathAnimationSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = new CommandBuffer();

        foreach (var entity in World.Query<Dead, DeathAnimation>())
        {
            ref var anim = ref World.Get<DeathAnimation>(entity);
            anim.Timer -= deltaTime;

            if (anim.Timer <= 0)
            {
                buffer.Despawn(entity);
            }
        }

        buffer.Flush(World);
    }
}
```

### Shields Before Health

```csharp
[Component]
public partial struct Shield
{
    public int Current;
    public int Max;
}

// In damage system, check shield first:
foreach (var entity in World.Query<Health, Shield, DamageReceived>())
{
    ref var health = ref World.Get<Health>(entity);
    ref var shield = ref World.Get<Shield>(entity);
    ref readonly var damage = ref World.Get<DamageReceived>(entity);

    var remaining = damage.Amount;

    // Absorb with shield first
    if (shield.Current > 0)
    {
        var absorbed = Math.Min(shield.Current, remaining);
        shield.Current -= absorbed;
        remaining -= absorbed;
    }

    // Apply remaining to health
    health.Current -= remaining;
    // ...
}
```

## See Also

- [Events Guide](../events.md) - Entity lifecycle events
- [Command Buffer](../command-buffer.md) - Safe entity modification
- [State Machines](state-machines.md) - For complex AI death states
