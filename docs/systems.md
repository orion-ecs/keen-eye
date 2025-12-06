# Systems Guide

Systems contain the logic that processes entities. This guide covers system design patterns and best practices.

## What is a System?

A system is a class that:

1. Queries entities with specific components
2. Processes those entities each frame/tick
3. May modify component data or trigger side effects

Systems implement `ISystem` or extend `SystemBase`:

```csharp
public interface ISystem : IDisposable
{
    void Initialize(World world);
    void Update(float deltaTime);
}
```

## Creating Systems

### Using SystemBase

The recommended way to create systems:

```csharp
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
```

`SystemBase` provides:
- `World` property for accessing the world
- `OnInitialize()` hook for setup
- `Dispose()` for cleanup

### Implementing ISystem

For more control, implement `ISystem` directly:

```csharp
public class CustomSystem : ISystem
{
    private World? world;

    public void Initialize(World world)
    {
        this.world = world;
        // Setup code
    }

    public void Update(float deltaTime)
    {
        // Update code
    }

    public void Dispose()
    {
        // Cleanup code
    }
}
```

## Registering Systems

Register systems with the world:

```csharp
using var world = new World();

// Register individual systems
world.AddSystem<InputSystem>();
world.AddSystem<MovementSystem>();
world.AddSystem<RenderSystem>();

// Or chain registrations
world
    .AddSystem<InputSystem>()
    .AddSystem<PhysicsSystem>()
    .AddSystem<MovementSystem>()
    .AddSystem<AnimationSystem>()
    .AddSystem<RenderSystem>();
```

## System Execution

### Update Loop

Call `World.Update()` to run all systems:

```csharp
// Game loop
while (running)
{
    float deltaTime = CalculateDeltaTime();
    world.Update(deltaTime);
}
```

Systems execute in registration order.

### Fixed Update

For physics or other time-sensitive systems:

```csharp
const float fixedDeltaTime = 1f / 60f;  // 60 Hz
float accumulator = 0f;

while (running)
{
    float deltaTime = CalculateDeltaTime();
    accumulator += deltaTime;

    // Fixed update for physics
    while (accumulator >= fixedDeltaTime)
    {
        world.Update(fixedDeltaTime);
        accumulator -= fixedDeltaTime;
    }
}
```

## System Patterns

### Query-Based Processing

The most common pattern:

```csharp
public class GravitySystem : SystemBase
{
    private const float Gravity = -9.81f;

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Velocity>().Without<Static>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            vel.Y += Gravity * deltaTime;
        }
    }
}
```

### Using CommandBuffer

For spawning/despawning during iteration:

```csharp
public class DeathSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref readonly var health = ref World.Get<Health>(entity);
            if (health.Current <= 0)
            {
                buffer.Despawn(entity);
            }
        }

        buffer.Flush(World);
    }
}
```

### Using Singletons

Access world-level data:

```csharp
public class TimeSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref var time = ref World.GetSingleton<GameTime>();
        time.DeltaTime = deltaTime;
        time.TotalTime += deltaTime;
        time.FrameCount++;
    }
}

public class AnimationSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref readonly var time = ref World.GetSingleton<GameTime>();

        foreach (var entity in World.Query<Animation>())
        {
            ref var anim = ref World.Get<Animation>(entity);
            anim.CurrentFrame = (int)(time.TotalTime * anim.FrameRate) % anim.FrameCount;
        }
    }
}
```

### System Initialization

Use `OnInitialize()` for one-time setup:

```csharp
public class AISystem : SystemBase
{
    private NavMesh? navMesh;

    protected override void OnInitialize()
    {
        // Load resources, build data structures
        navMesh = NavMesh.LoadFrom("level.nav");

        // Set up singletons
        World.SetSingleton(new AIConfig { UpdateRate = 0.1f });
    }

    public override void Update(float deltaTime)
    {
        // Use initialized resources
    }

    public override void Dispose()
    {
        // Cleanup
        navMesh?.Dispose();
    }
}
```

### Inter-System Communication

Systems can communicate through:

1. **Singletons** - World-level shared state:

```csharp
// InputSystem writes
World.SetSingleton(new InputState { MoveDirection = dir });

// MovementSystem reads
ref readonly var input = ref World.GetSingleton<InputState>();
```

2. **Components** - Entity-level data:

```csharp
// DamageSystem adds damage events
World.Add(entity, new DamageEvent { Amount = 10, Source = attacker });

// HealthSystem processes and removes
foreach (var entity in World.Query<Health, DamageEvent>())
{
    ref var health = ref World.Get<Health>(entity);
    ref readonly var damage = ref World.Get<DamageEvent>(entity);
    health.Current -= damage.Amount;
    World.Remove<DamageEvent>(entity);
}
```

3. **Tags** - State flags:

```csharp
// CombatSystem marks entity
World.Add(entity, default(InCombat));

// AISystem checks flag
foreach (var entity in World.Query<AI>().With<InCombat>())
{
    // Combat AI behavior
}
```

## System Organization

### By Feature

Group related functionality:

```csharp
// Movement feature
world.AddSystem<InputSystem>();
world.AddSystem<PhysicsSystem>();
world.AddSystem<MovementSystem>();

// Combat feature
world.AddSystem<TargetingSystem>();
world.AddSystem<CombatSystem>();
world.AddSystem<DamageSystem>();

// Rendering feature
world.AddSystem<AnimationSystem>();
world.AddSystem<RenderSystem>();
```

### By Update Phase

```csharp
// Early update - input and AI
world.AddSystem<InputSystem>();
world.AddSystem<AISystem>();

// Main update - game logic
world.AddSystem<MovementSystem>();
world.AddSystem<CombatSystem>();
world.AddSystem<PhysicsSystem>();

// Late update - cleanup and rendering
world.AddSystem<DeathSystem>();
world.AddSystem<RenderSystem>();
```

## Best Practices

### Do: Single Responsibility

```csharp
// ✅ Good: Focused systems
public class MovementSystem : SystemBase { /* movement only */ }
public class GravitySystem : SystemBase { /* gravity only */ }
public class CollisionSystem : SystemBase { /* collision only */ }

// ❌ Bad: God system
public class EverythingSystem : SystemBase { /* movement + gravity + collision + ... */ }
```

### Do: Use ref for Component Access

```csharp
// ✅ Good: Zero-copy access
ref var pos = ref World.Get<Position>(entity);
pos.X += 1;

// ❌ Bad: Copies data unnecessarily
var pos = World.Get<Position>(entity);  // Creates a copy
pos.X += 1;  // Modifies the copy, not the original!
```

### Don't: Modify Entities During Iteration

```csharp
// ❌ Bad: Modifying during iteration
foreach (var entity in World.Query<Health>())
{
    if (health.Current <= 0)
        World.Despawn(entity);  // Invalidates iterator!
}

// ✅ Good: Use CommandBuffer
var buffer = new CommandBuffer();
foreach (var entity in World.Query<Health>())
{
    if (health.Current <= 0)
        buffer.Despawn(entity);
}
buffer.Flush(World);
```

### Do: Keep Systems Stateless When Possible

```csharp
// ✅ Good: Stateless system
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // All state is in components/singletons
    }
}

// Be careful with: Stateful systems
public class TimerSystem : SystemBase
{
    private float elapsed;  // State in system - harder to serialize/debug

    public override void Update(float deltaTime)
    {
        elapsed += deltaTime;
    }
}
```

## Next Steps

- [Queries Guide](queries.md) - Query patterns for systems
- [Command Buffer](command-buffer.md) - Deferred entity operations
- [Singletons Guide](singletons.md) - World-level resources
