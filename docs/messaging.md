# Inter-System Messaging Guide

Inter-system messaging enables decoupled communication between systems through a typed message bus. This guide covers message patterns, immediate vs deferred delivery, and best practices.

## What is Inter-System Messaging?

Messaging provides a publish-subscribe mechanism for systems to communicate without direct dependencies:

1. **Producers** send messages when events occur (damage dealt, collision detected, etc.)
2. **Consumers** subscribe to message types they care about
3. **Message bus** routes messages to all subscribers

This decouples systems - a `CombatSystem` can send a `DamageMessage` without knowing which systems will handle it.

## Quick Start

```csharp
using KeenEyes;

using var world = new World();

// Define a message type (use struct for zero-allocation)
public readonly record struct DamageMessage(Entity Target, int Amount, Entity Source);

// Subscribe to messages
var subscription = world.Subscribe<DamageMessage>(msg =>
{
    Console.WriteLine($"Entity {msg.Target} took {msg.Amount} damage from {msg.Source}");
});

// Send a message (immediate delivery)
world.Send(new DamageMessage(target, 25, attacker));

// Unsubscribe when done
subscription.Dispose();
```

## Message Types

Messages can be any type, but structs are recommended to minimize allocations:

```csharp
// ✅ Recommended: record struct (immutable, value equality)
public readonly record struct DamageMessage(Entity Target, int Amount, Entity Source);

public readonly record struct CollisionMessage(Entity Entity1, Entity Entity2, float Depth);

public readonly record struct SpawnMessage(string PrefabName, Position Position);

// ✅ Also good: plain struct
public struct HealthChangedMessage
{
    public Entity Entity;
    public int OldHealth;
    public int NewHealth;
}

// ⚠️ Classes work but allocate on each send
public class ExpensiveMessage
{
    public byte[] Data { get; set; } = [];
}
```

## Subscribing to Messages

Use `World.Subscribe<T>()` to register a handler:

```csharp
// Basic subscription
var subscription = world.Subscribe<DamageMessage>(msg =>
{
    ref var health = ref world.Get<Health>(msg.Target);
    health.Current -= msg.Amount;
});

// Multiple subscriptions to same message type
var audioSub = world.Subscribe<DamageMessage>(msg =>
{
    AudioManager.PlaySound("hit");
});

var vfxSub = world.Subscribe<DamageMessage>(msg =>
{
    SpawnDamageNumbers(msg.Target, msg.Amount);
});
```

### Subscription Lifetime

The returned `EventSubscription` must be disposed to unsubscribe:

```csharp
// Option 1: Using statement for scoped lifetime
using (var sub = world.Subscribe<DamageMessage>(HandleDamage))
{
    // Handler active within this scope
}
// Handler automatically unsubscribed

// Option 2: Manual disposal
var sub = world.Subscribe<DamageMessage>(HandleDamage);
// ... later ...
sub.Dispose();

// Option 3: Track subscriptions in systems
public class DamageSystem : SystemBase
{
    private EventSubscription? subscription;

    protected override void OnInitialize()
    {
        subscription = World.Subscribe<DamageMessage>(HandleDamage);
    }

    public override void Dispose()
    {
        subscription?.Dispose();
        base.Dispose();
    }

    private void HandleDamage(DamageMessage msg) { /* ... */ }
}
```

### Safe Unsubscription During Delivery

Handlers can safely unsubscribe themselves during message delivery:

```csharp
EventSubscription? sub = null;
sub = world.Subscribe<DamageMessage>(msg =>
{
    // Handle the message
    ProcessDamage(msg);

    // Self-unsubscribe (safe to do during delivery)
    sub?.Dispose();
});
```

## Sending Messages

### Immediate Delivery

`World.Send<T>()` delivers messages synchronously to all handlers:

```csharp
// Send immediately to all subscribers
world.Send(new DamageMessage(target, 25, attacker));

// Handlers are called synchronously in registration order
// If a handler throws, subsequent handlers are not called
```

### Deferred Delivery (Message Queuing)

Use `World.QueueMessage<T>()` to collect messages and process them later:

```csharp
// Queue messages during system updates
public class CollisionSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var collision in DetectCollisions())
        {
            // Queue instead of immediate send
            World.QueueMessage(new CollisionMessage(
                collision.Entity1,
                collision.Entity2,
                collision.Depth));
        }
    }
}

// Process all queued messages at specific point in frame
public class GameLoop
{
    public void Frame(float deltaTime)
    {
        world.Update(deltaTime);

        // Process all queued messages after systems run
        world.ProcessQueuedMessages();
    }
}
```

### Process Specific Message Types

Process only certain message types while leaving others queued:

```csharp
// Process physics messages first
world.ProcessQueuedMessages<CollisionMessage>();

// Then process gameplay messages
world.ProcessQueuedMessages<DamageMessage>();

// Other message types remain queued
```

### Clear Queued Messages

Discard queued messages without processing:

```csharp
// Clear all queued messages (e.g., on level reset)
world.ClearQueuedMessages();

// Clear specific type only
world.ClearQueuedMessages<CollisionMessage>();
```

## Checking Subscribers

### Existence Check

Skip expensive message creation when no one is listening:

```csharp
// Optimization: Only create message if someone cares
if (world.HasMessageSubscribers<ExpensiveMessage>())
{
    var data = ComputeExpensiveData(); // Only compute if needed
    world.Send(new ExpensiveMessage(data));
}
```

### Subscriber Count

Get the number of handlers (useful for debugging):

```csharp
int count = world.GetMessageSubscriberCount<DamageMessage>();
Console.WriteLine($"{count} systems are listening for damage events");
```

### Queue Statistics

Monitor queued message counts:

```csharp
// Count for specific type
int damageQueued = world.GetQueuedMessageCount<DamageMessage>();

// Total across all types
int totalQueued = world.GetTotalQueuedMessageCount();
```

## Patterns and Best Practices

### Command Pattern

Use messages to implement command patterns:

```csharp
// Command messages
public readonly record struct MoveCommand(Entity Entity, float X, float Y);
public readonly record struct AttackCommand(Entity Attacker, Entity Target);
public readonly record struct UseAbilityCommand(Entity Caster, int AbilityId, Entity? Target);

// Command handler system
public class CommandSystem : SystemBase
{
    private EventSubscription? moveSub;
    private EventSubscription? attackSub;

    protected override void OnInitialize()
    {
        moveSub = World.Subscribe<MoveCommand>(HandleMove);
        attackSub = World.Subscribe<AttackCommand>(HandleAttack);
    }

    private void HandleMove(MoveCommand cmd)
    {
        ref var pos = ref World.Get<Position>(cmd.Entity);
        pos.X = cmd.X;
        pos.Y = cmd.Y;
    }

    private void HandleAttack(AttackCommand cmd)
    {
        ref readonly var damage = ref World.Get<Damage>(cmd.Attacker);
        World.Send(new DamageMessage(cmd.Target, damage.Amount, cmd.Attacker));
    }
}
```

### Event Sourcing

Track all game events for replay or debugging:

```csharp
public class EventRecorder : SystemBase
{
    private readonly List<object> events = [];
    private EventSubscription? damageSub;
    private EventSubscription? spawnSub;
    private EventSubscription? deathSub;

    protected override void OnInitialize()
    {
        damageSub = World.Subscribe<DamageMessage>(msg => events.Add(msg));
        spawnSub = World.Subscribe<SpawnMessage>(msg => events.Add(msg));
        deathSub = World.Subscribe<DeathMessage>(msg => events.Add(msg));
    }

    public IReadOnlyList<object> GetEvents() => events;
}
```

### Decoupled System Communication

Systems communicate without direct dependencies:

```csharp
// Combat system sends damage
public class CombatSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Attack, Target>())
        {
            ref readonly var attack = ref World.Get<Attack>(entity);
            ref readonly var target = ref World.Get<Target>(entity);

            // Send message - don't care who handles it
            World.Send(new DamageMessage(target.Entity, attack.Damage, entity));
        }
    }
}

// Health system responds to damage
public class HealthSystem : SystemBase
{
    private EventSubscription? sub;

    protected override void OnInitialize()
    {
        sub = World.Subscribe<DamageMessage>(HandleDamage);
    }

    private void HandleDamage(DamageMessage msg)
    {
        if (!World.Has<Health>(msg.Target)) return;

        ref var health = ref World.Get<Health>(msg.Target);
        health.Current = Math.Max(0, health.Current - msg.Amount);

        if (health.Current <= 0)
        {
            World.Send(new DeathMessage(msg.Target, msg.Source));
        }
    }
}

// Audio system plays sounds
public class AudioSystem : SystemBase
{
    private EventSubscription? damageSub;
    private EventSubscription? deathSub;

    protected override void OnInitialize()
    {
        damageSub = World.Subscribe<DamageMessage>(_ => PlaySound("hit"));
        deathSub = World.Subscribe<DeathMessage>(_ => PlaySound("death"));
    }
}

// UI system shows damage numbers
public class UISystem : SystemBase
{
    private EventSubscription? sub;

    protected override void OnInitialize()
    {
        sub = World.Subscribe<DamageMessage>(msg =>
        {
            ShowDamageNumber(msg.Target, msg.Amount);
        });
    }
}
```

### Request-Response Pattern

Implement request-response with paired messages:

```csharp
// Request
public readonly record struct QueryHealthRequest(Entity Entity, int RequestId);

// Response
public readonly record struct QueryHealthResponse(int RequestId, int Current, int Max);

// Requester
public class UIHealthBar : SystemBase
{
    private EventSubscription? responseSub;
    private int nextRequestId;
    private readonly Dictionary<int, Action<int, int>> callbacks = [];

    protected override void OnInitialize()
    {
        responseSub = World.Subscribe<QueryHealthResponse>(HandleResponse);
    }

    public void RequestHealth(Entity entity, Action<int, int> callback)
    {
        var requestId = nextRequestId++;
        callbacks[requestId] = callback;
        World.Send(new QueryHealthRequest(entity, requestId));
    }

    private void HandleResponse(QueryHealthResponse response)
    {
        if (callbacks.TryGetValue(response.RequestId, out var callback))
        {
            callback(response.Current, response.Max);
            callbacks.Remove(response.RequestId);
        }
    }
}

// Responder
public class HealthQuerySystem : SystemBase
{
    private EventSubscription? sub;

    protected override void OnInitialize()
    {
        sub = World.Subscribe<QueryHealthRequest>(HandleRequest);
    }

    private void HandleRequest(QueryHealthRequest request)
    {
        if (!World.Has<Health>(request.Entity)) return;

        ref readonly var health = ref World.Get<Health>(request.Entity);
        World.Send(new QueryHealthResponse(request.RequestId, health.Current, health.Max));
    }
}
```

## Messaging vs Events vs Components

KeenEyes offers multiple communication mechanisms:

| Mechanism | Use Case | Delivery |
|-----------|----------|----------|
| **Messaging** (`Send`/`Subscribe`) | Decoupled system communication | Immediate or queued |
| **Events** (`OnComponentAdded`, etc.) | React to ECS lifecycle changes | Immediate |
| **EventBus** (`Events.Publish`) | Custom game events with entity context | Immediate |
| **Components** | Persistent state on entities | Via queries |
| **Singletons** | Global game state | Via `GetSingleton<T>()` |

### When to Use Messaging

- Systems need to communicate without direct references
- Multiple systems should react to the same event
- You want to decouple producers from consumers
- Implementing command patterns or event sourcing

### When to Use Events

- Reacting to entity/component lifecycle changes
- Immediate response to structural changes
- Integration with the ECS lifecycle

### When to Use Components

- State that persists across frames
- Data that should be queried with other components
- Information that belongs to a specific entity

## Performance Considerations

1. **Use structs** - Minimize allocations on hot paths
2. **Check subscribers** - Skip work when no one listens
3. **Queue for batching** - Collect messages and process together
4. **Unsubscribe promptly** - Dispose subscriptions when systems are disabled

```csharp
// ✅ Good: Check before expensive work
if (world.HasMessageSubscribers<AnalyticsMessage>())
{
    var data = GatherAnalytics(); // Skip if no one cares
    world.Send(new AnalyticsMessage(data));
}

// ✅ Good: Queue for batch processing
foreach (var collision in collisions)
{
    world.QueueMessage(new CollisionMessage(collision));
}
world.ProcessQueuedMessages<CollisionMessage>();

// ❌ Bad: Send in tight loop without checking
foreach (var entity in world.Query<Position>())
{
    world.Send(new PositionUpdatedMessage(entity)); // May be wasteful
}
```

## Next Steps

- [Systems Guide](systems.md) - System design patterns
- [Events Guide](events.md) - Component and entity lifecycle events
- [Command Buffer](command-buffer.md) - Deferred entity operations
