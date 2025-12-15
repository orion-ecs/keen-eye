# Events Guide

KeenEyes provides a comprehensive event system for reacting to entity and component lifecycle changes. This guide covers all event types and usage patterns.

## Overview

The event system includes:

- **Component events** - `OnComponentAdded`, `OnComponentRemoved`, `OnComponentChanged`
- **Entity events** - `OnEntityCreated`, `OnEntityDestroyed`
- **Custom events** - Generic pub/sub via `EventBus`
- **Automatic cleanup** - `IDisposable` subscriptions

## Component Events

### OnComponentAdded

Fires when a component is added to an entity:

```csharp
using var world = new World();

// Subscribe to Health component additions
var subscription = world.OnComponentAdded<Health>((entity, health) =>
{
    Console.WriteLine($"Entity {entity} gained health: {health.Current}/{health.Max}");
});

// This triggers the event
var entity = world.Spawn()
    .With(new Health { Current = 100, Max = 100 })
    .Build();

// Output: Entity (0v1) gained health: 100/100

// Cleanup when done
subscription.Dispose();
```

### OnComponentRemoved

Fires when a component is removed from an entity:

```csharp
var subscription = world.OnComponentRemoved<Health>(entity =>
{
    Console.WriteLine($"Entity {entity} lost health component");
});

// Triggers when explicitly removed
world.Remove<Health>(entity);

// Also triggers on despawn
world.Despawn(entity);
```

Note: The handler receives only the entity, not the component value, because the data may already be overwritten.

### OnComponentChanged

Fires when a component is modified via `World.Set()`:

```csharp
var subscription = world.OnComponentChanged<Health>((entity, oldValue, newValue) =>
{
    Console.WriteLine($"Entity {entity} health changed: {oldValue.Current} -> {newValue.Current}");
});

// Create entity
var entity = world.Spawn()
    .With(new Health { Current = 100, Max = 100 })
    .Build();

// This triggers the change event
world.Set(entity, new Health { Current = 75, Max = 100 });

// Output: Entity (0v1) health changed: 100 -> 75
```

**Important**: Direct modifications via `ref` do not trigger change events:

```csharp
// This does NOT trigger OnComponentChanged
ref var health = ref world.Get<Health>(entity);
health.Current = 50;

// Use Set() to trigger the event
world.Set(entity, health with { Current = 50 });
```

## Entity Events

### OnEntityCreated

Fires when an entity is spawned:

```csharp
var subscription = world.OnEntityCreated((entity, name) =>
{
    if (name != null)
    {
        Console.WriteLine($"Created named entity: {name} ({entity})");
    }
    else
    {
        Console.WriteLine($"Created entity: {entity}");
    }
});

world.Spawn().Build();                    // Created entity: (0v1)
world.Spawn("Player").Build();            // Created named entity: Player ((1v1))
```

### OnEntityDestroyed

Fires when an entity is despawned:

```csharp
var subscription = world.OnEntityDestroyed(entity =>
{
    Console.WriteLine($"Entity {entity} destroyed");
});

var entity = world.Spawn().Build();
world.Despawn(entity);  // Entity (0v1) destroyed
```

The handler is called before the entity is fully removed, so the entity handle is still valid during the callback.

## Custom Events with EventBus

For application-specific events, use the generic `EventBus`:

### Defining Events

```csharp
// Define event as readonly record struct
public readonly record struct DamageEvent(Entity Target, int Amount, Entity? Source);

public readonly record struct LevelUpEvent(Entity Player, int NewLevel);

public readonly record struct GameOverEvent(string Reason, int FinalScore);
```

### Subscribing

```csharp
// Access via world.Events
var damageSubscription = world.Events.Subscribe<DamageEvent>(evt =>
{
    Console.WriteLine($"Entity {evt.Target} took {evt.Amount} damage");
});

var levelUpSubscription = world.Events.Subscribe<LevelUpEvent>(evt =>
{
    Console.WriteLine($"Player {evt.Player} reached level {evt.NewLevel}!");
});
```

### Publishing

```csharp
// Publish events from systems
world.Events.Publish(new DamageEvent(targetEntity, 25, attackerEntity));
world.Events.Publish(new LevelUpEvent(playerEntity, 5));
```

### Checking for Handlers

Skip expensive event creation when no one is listening:

```csharp
if (world.Events.HasHandlers<ExpensiveEvent>())
{
    var eventData = CreateExpensiveEventData();
    world.Events.Publish(eventData);
}
```

## Subscription Management

### IDisposable Pattern

All event subscriptions return `EventSubscription` which implements `IDisposable`:

```csharp
var subscription = world.OnEntityCreated((e, n) => { });

// Later, unsubscribe
subscription.Dispose();
```

### Using Statement

For scoped subscriptions:

```csharp
using (var subscription = world.OnComponentAdded<Health>((e, h) =>
{
    Console.WriteLine($"Health added to {e}");
}))
{
    // Events fire within this scope
    world.Spawn().With(new Health { Current = 100, Max = 100 }).Build();
}
// Automatically unsubscribed
```

### Multiple Handlers

Multiple handlers can subscribe to the same event type:

```csharp
var sub1 = world.OnEntityCreated((e, n) => Console.WriteLine($"Handler 1: {e}"));
var sub2 = world.OnEntityCreated((e, n) => Console.WriteLine($"Handler 2: {e}"));

world.Spawn().Build();
// Handler 1: (0v1)
// Handler 2: (0v1)
```

## Use Cases

### Logging System

```csharp
public class EventLogger
{
    private readonly List<EventSubscription> subscriptions = [];

    public void AttachTo(World world)
    {
        subscriptions.Add(world.OnEntityCreated((e, n) =>
            Console.WriteLine($"[LOG] Entity created: {e} (name: {n ?? "unnamed"})")));

        subscriptions.Add(world.OnEntityDestroyed(e =>
            Console.WriteLine($"[LOG] Entity destroyed: {e}")));
    }

    public void Detach()
    {
        foreach (var sub in subscriptions)
            sub.Dispose();
        subscriptions.Clear();
    }
}
```

### Reactive UI

```csharp
// Update UI when player health changes
world.OnComponentChanged<Health>((entity, oldHealth, newHealth) =>
{
    if (world.Has<Player>(entity))
    {
        UpdateHealthBar(newHealth.Current, newHealth.Max);

        if (newHealth.Current < oldHealth.Current)
        {
            PlayDamageAnimation();
        }
    }
});
```

### Achievement System

```csharp
world.Events.Subscribe<DamageEvent>(evt =>
{
    totalDamageDealt += evt.Amount;

    if (totalDamageDealt >= 10000)
    {
        UnlockAchievement("Damage Dealer");
    }
});

world.OnEntityDestroyed(entity =>
{
    if (world.Has<Enemy>(entity))
    {
        enemiesDefeated++;

        if (enemiesDefeated >= 100)
        {
            UnlockAchievement("Monster Slayer");
        }
    }
});
```

### Cleanup Chains

```csharp
// Clean up related resources when entities are destroyed
world.OnEntityDestroyed(entity =>
{
    // Clean up physics body
    if (world.Has<PhysicsBody>(entity))
    {
        physicsWorld.RemoveBody(entity);
    }

    // Clean up audio sources
    if (world.Has<AudioSource>(entity))
    {
        audioManager.StopAll(entity);
    }
});
```

## Performance Considerations

### Minimal Overhead When Unused

When no handlers are registered for an event type, firing the event has minimal cost (a dictionary lookup returning false):

```csharp
// No OnComponentAdded<Health> handlers registered
world.Spawn().With(new Health { Current = 100, Max = 100 }).Build();
// Very low overhead - just a dictionary check
```

### Handler Execution Order

Handlers are invoked in reverse registration order (LIFO). This allows handlers to safely unsubscribe during iteration.

### Exception Handling

If a handler throws an exception, subsequent handlers are not invoked:

```csharp
world.OnEntityCreated((e, n) => Console.WriteLine("Handler 1"));
world.OnEntityCreated((e, n) => throw new Exception("Oops!"));
world.OnEntityCreated((e, n) => Console.WriteLine("Handler 3")); // Not called!

try
{
    world.Spawn().Build();
}
catch (Exception)
{
    // Handler 1 ran, handler 2 threw, handler 3 skipped
}
```

Wrap handlers in try-catch if fault tolerance is needed.

## Best Practices

### Do: Dispose Subscriptions

```csharp
// Store subscriptions for later cleanup
private List<EventSubscription> subscriptions = [];

public void Initialize(IWorld world)
{
    subscriptions.Add(world.OnEntityCreated((e, n) => { }));
    subscriptions.Add(world.OnComponentAdded<Health>((e, h) => { }));
}

public void Cleanup()
{
    foreach (var sub in subscriptions)
        sub.Dispose();
    subscriptions.Clear();
}
```

### Do: Use Set() for Change Detection

```csharp
// Use Set() to trigger OnComponentChanged
world.Set(entity, new Health { Current = newValue, Max = maxValue });
```

### Don't: Modify World During Events (Without Care)

```csharp
// Dangerous: Spawning during spawn event can cause issues
world.OnEntityCreated((entity, name) =>
{
    // Be careful! This triggers another OnEntityCreated
    world.Spawn().Build();
});
```

### Don't: Keep Long-Lived References

```csharp
// Bad: Captured entity may become stale
Entity capturedEntity;
world.OnEntityCreated((e, n) => capturedEntity = e);
// Much later...
world.Get<Position>(capturedEntity); // May throw!

// Good: Validate before use
if (world.IsAlive(capturedEntity))
{
    ref var pos = ref world.Get<Position>(capturedEntity);
}
```

## Next Steps

- [Change Tracking](change-tracking.md) - Track component modifications
- [Relationships](relationships.md) - Parent-child hierarchies
- [Systems Guide](systems.md) - Processing entities
