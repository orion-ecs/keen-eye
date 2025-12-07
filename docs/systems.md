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
- Lifecycle hooks (`OnInitialize`, `OnBeforeUpdate`, `OnAfterUpdate`, `OnEnabled`, `OnDisabled`)
- `Enabled` property for runtime control
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

### Execution Order

Systems execute based on **phase** and **order**:

1. **Phase** - Which stage of the frame (EarlyUpdate → PostRender)
2. **Order** - Priority within a phase (lower values run first)

```csharp
// Specify phase and order when adding systems
world.AddSystem<InputSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<PhysicsSystem>(SystemPhase.FixedUpdate, order: 0);
world.AddSystem<MovementSystem>(SystemPhase.Update, order: 10);
world.AddSystem<AISystem>(SystemPhase.Update, order: 20);
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);
```

#### System Phases

| Phase | Description |
|-------|-------------|
| `EarlyUpdate` | Start of frame - input polling, time updates |
| `FixedUpdate` | Fixed timestep - physics simulation |
| `Update` | Main game logic (default phase) |
| `LateUpdate` | After main update - cameras, cleanup |
| `Render` | Rendering phase |
| `PostRender` | After rendering - debug overlays, profiling |

#### Using the [System] Attribute

Declare phase and order metadata with the `[System]` attribute:

```csharp
[System(Phase = SystemPhase.EarlyUpdate, Order = 0)]
public partial class InputSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Poll input devices
    }
}

[System(Phase = SystemPhase.Update, Order = 10)]
public partial class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Process movement
    }
}

[System(Phase = SystemPhase.Update, Order = 20, Group = "AI")]
public partial class AISystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // AI decision-making runs after movement
    }
}
```

The source generator creates static metadata properties from the attribute. You can also override the attribute values when registering:

```csharp
// Use attribute defaults
world.AddSystem<MovementSystem>();

// Override at registration time
world.AddSystem<MovementSystem>(SystemPhase.LateUpdate, order: 100);
```

#### System Dependencies with [RunBefore] and [RunAfter]

For explicit ordering constraints between systems, use the `[RunBefore]` and `[RunAfter]` attributes:

```csharp
[System(Phase = SystemPhase.Update)]
[RunAfter(typeof(InputSystem))]
public partial class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Always runs after InputSystem in the Update phase
    }
}

[System(Phase = SystemPhase.Update)]
[RunBefore(typeof(RenderSystem))]
[RunAfter(typeof(MovementSystem))]
public partial class AnimationSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Runs after MovementSystem but before RenderSystem
    }
}
```

You can also specify dependencies at registration time:

```csharp
world.AddSystem<CollisionSystem>(
    SystemPhase.Update,
    order: 0,
    runsBefore: [typeof(DamageSystem)],
    runsAfter: [typeof(MovementSystem)]);
```

**Key points:**
- Dependencies only apply within the same phase
- Cross-phase references are ignored (phase order takes precedence)
- Dependencies override Order values when they conflict
- Systems use topological sorting to resolve the final execution order

#### Cycle Detection

If system dependencies create a cycle, an `InvalidOperationException` is thrown:

```csharp
// This will throw - A runs before B, B runs before A
[System(Phase = SystemPhase.Update)]
[RunBefore(typeof(SystemB))]
public partial class SystemA : SystemBase { }

[System(Phase = SystemPhase.Update)]
[RunBefore(typeof(SystemA))]  // Creates a cycle!
public partial class SystemB : SystemBase { }
```

The exception message indicates which phase contains the cycle and which systems are involved.

### Fixed Update

For physics or other time-sensitive systems, use `World.FixedUpdate()` which only executes systems in the `FixedUpdate` phase:

```csharp
const float fixedDeltaTime = 1f / 60f;  // 60 Hz
float accumulator = 0f;

while (running)
{
    float deltaTime = CalculateDeltaTime();
    accumulator += deltaTime;

    // Run physics at fixed timestep
    while (accumulator >= fixedDeltaTime)
    {
        world.FixedUpdate(fixedDeltaTime);  // Only FixedUpdate phase systems
        accumulator -= fixedDeltaTime;
    }

    // Run all other systems at variable rate
    world.Update(deltaTime);  // Runs all phases including FixedUpdate
}
```

**Note:** `World.Update()` runs ALL phases (including FixedUpdate). Use `World.FixedUpdate()` when you need to run physics separately at a fixed timestep.

## Lifecycle Hooks

`SystemBase` provides lifecycle hooks for fine-grained control over system execution:

| Hook | When Called |
|------|-------------|
| `OnInitialize()` | After system is added to a world |
| `OnBeforeUpdate(deltaTime)` | Before each `Update()` call |
| `OnAfterUpdate(deltaTime)` | After each `Update()` call |
| `OnEnabled()` | When system transitions from disabled to enabled |
| `OnDisabled()` | When system transitions from enabled to disabled |

### Example: Fixed Timestep Physics

```csharp
public class PhysicsSystem : SystemBase
{
    private float accumulator;
    private const float FixedTimeStep = 1f / 60f;

    protected override void OnBeforeUpdate(float deltaTime)
    {
        // Accumulate time for fixed timestep simulation
        accumulator += deltaTime;
    }

    public override void Update(float deltaTime)
    {
        // Run physics at fixed timestep
        while (accumulator >= FixedTimeStep)
        {
            SimulatePhysics(FixedTimeStep);
            accumulator -= FixedTimeStep;
        }
    }

    private void SimulatePhysics(float dt)
    {
        foreach (var entity in World.Query<Position, Velocity>().Without<Static>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * dt;
            pos.Y += vel.Y * dt;
        }
    }
}
```

### Example: Resource Management

```csharp
public class AudioSystem : SystemBase
{
    private AudioEngine? engine;

    protected override void OnInitialize()
    {
        engine = new AudioEngine();
    }

    protected override void OnEnabled()
    {
        engine?.Resume();
    }

    protected override void OnDisabled()
    {
        engine?.Pause();
    }

    public override void Update(float deltaTime)
    {
        engine?.Update(deltaTime);
    }

    public override void Dispose()
    {
        engine?.Dispose();
        base.Dispose();
    }
}
```

## Runtime Control

Enable or disable systems at runtime for performance optimization or game state management.

### Enabled Property

```csharp
var system = world.GetSystem<AISystem>();
system.Enabled = false;  // Pauses AI processing

// Later...
system.Enabled = true;   // Resumes AI processing
```

### World API

```csharp
// Get a system by type
var physics = world.GetSystem<PhysicsSystem>();

// Enable/disable by type
world.DisableSystem<RenderSystem>();  // Returns true if found
world.EnableSystem<RenderSystem>();

// Works with nested SystemGroups
world.GetSystem<AISystem>();  // Searches all groups recursively
```

### Pausing Game Systems

```csharp
public class GamePauseManager
{
    private readonly World world;
    private readonly List<Type> pausableSystems =
    [
        typeof(PhysicsSystem),
        typeof(AISystem),
        typeof(AnimationSystem)
    ];

    public void Pause()
    {
        world.DisableSystem<PhysicsSystem>();
        world.DisableSystem<AISystem>();
        world.DisableSystem<AnimationSystem>();
    }

    public void Resume()
    {
        world.EnableSystem<PhysicsSystem>();
        world.EnableSystem<AISystem>();
        world.EnableSystem<AnimationSystem>();
    }
}
```

### SystemGroup Control

Disable an entire group to pause all contained systems:

```csharp
var combatGroup = new SystemGroup("Combat")
    .Add<TargetingSystem>()
    .Add<DamageSystem>()
    .Add<HealthSystem>();

world.AddSystem(combatGroup);

// Disable all combat systems at once
combatGroup.Enabled = false;
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
