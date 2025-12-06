# Singletons Guide

Singletons are world-level resources not tied to any entity. This guide covers when and how to use them.

## What are Singletons?

Singletons are global data within a world:

- **Not attached to entities** - Exist at the world level
- **One per type** - Only one instance of each type per world
- **World-isolated** - Different worlds have different singletons

Common uses:
- Game time / delta time
- Input state
- Configuration settings
- Random number generators
- Asset references

## Basic Usage

### Setting a Singleton

```csharp
world.SetSingleton(new GameTime
{
    DeltaTime = 0.016f,
    TotalTime = 0f,
    FrameCount = 0
});
```

### Getting a Singleton

```csharp
// Get by reference (zero-copy)
ref var time = ref world.GetSingleton<GameTime>();
Console.WriteLine($"Delta: {time.DeltaTime}");

// Modify directly
time.TotalTime += time.DeltaTime;
time.FrameCount++;
```

### Read-Only Access

```csharp
ref readonly var time = ref world.GetSingleton<GameTime>();
float delta = time.DeltaTime;
// time.DeltaTime = 0;  // Compile error - readonly
```

### Checking Existence

```csharp
if (world.HasSingleton<GameConfig>())
{
    ref var config = ref world.GetSingleton<GameConfig>();
    // Use config...
}
```

### Try-Get Pattern

```csharp
if (world.TryGetSingleton<InputState>(out var input))
{
    // Use input (this is a copy, not a reference)
    Console.WriteLine($"Move: ({input.MoveX}, {input.MoveY})");
}
```

### Removing Singletons

```csharp
bool removed = world.RemoveSingleton<DebugConfig>();
```

## Common Singleton Types

### Game Time

```csharp
public struct GameTime
{
    public float DeltaTime;
    public float TotalTime;
    public long FrameCount;
    public float TimeScale;
}

// TimeSystem updates it each frame
public class TimeSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref var time = ref World.GetSingleton<GameTime>();
        time.DeltaTime = deltaTime * time.TimeScale;
        time.TotalTime += time.DeltaTime;
        time.FrameCount++;
    }
}
```

### Input State

```csharp
public struct InputState
{
    public float MoveX;
    public float MoveY;
    public bool Jump;
    public bool Attack;
    public float MouseX;
    public float MouseY;
}

// InputSystem reads raw input and populates singleton
public class InputSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        World.SetSingleton(new InputState
        {
            MoveX = GetAxis("Horizontal"),
            MoveY = GetAxis("Vertical"),
            Jump = IsKeyPressed(Keys.Space),
            Attack = IsMouseButtonPressed(0)
        });
    }
}

// Other systems read it
public class PlayerControlSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref readonly var input = ref World.GetSingleton<InputState>();

        foreach (var entity in World.Query<Velocity>().With<Player>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            vel.X = input.MoveX * 5f;
            vel.Y = input.MoveY * 5f;
        }
    }
}
```

### Game Configuration

```csharp
public struct GameConfig
{
    public int Difficulty;
    public float MasterVolume;
    public float MusicVolume;
    public float SfxVolume;
    public bool Fullscreen;
    public int ResolutionWidth;
    public int ResolutionHeight;
}

// Set during game initialization
world.SetSingleton(new GameConfig
{
    Difficulty = 1,
    MasterVolume = 0.8f,
    MusicVolume = 0.7f,
    SfxVolume = 1.0f,
    Fullscreen = true,
    ResolutionWidth = 1920,
    ResolutionHeight = 1080
});
```

### Random Number Generator

```csharp
public struct GameRandom
{
    public int Seed;
    public Random Instance;  // Note: Reference type inside struct
}

// Initialize once
world.SetSingleton(new GameRandom
{
    Seed = 12345,
    Instance = new Random(12345)
});

// Use in systems
public class SpawnSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref var rng = ref World.GetSingleton<GameRandom>();

        foreach (var entity in World.Query<Spawner>())
        {
            float x = rng.Instance.NextSingle() * 100f;
            float y = rng.Instance.NextSingle() * 100f;
            // Spawn at random position...
        }
    }
}
```

### Camera Data

```csharp
public struct CameraData
{
    public float X;
    public float Y;
    public float Zoom;
    public float Rotation;
    public Entity Target;
}

// CameraSystem updates singleton
public class CameraSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref var cam = ref World.GetSingleton<CameraData>();

        if (cam.Target.IsValid && World.IsAlive(cam.Target))
        {
            ref readonly var targetPos = ref World.Get<Position>(cam.Target);
            cam.X = Lerp(cam.X, targetPos.X, deltaTime * 5f);
            cam.Y = Lerp(cam.Y, targetPos.Y, deltaTime * 5f);
        }
    }
}

// RenderSystem reads it
public class RenderSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref readonly var cam = ref World.GetSingleton<CameraData>();

        // Apply camera transform
        BeginCamera(cam.X, cam.Y, cam.Zoom, cam.Rotation);

        foreach (var entity in World.Query<Position, Sprite>())
        {
            // Render entities...
        }

        EndCamera();
    }
}
```

## Singletons vs Components

| Use Singleton | Use Component |
|---------------|---------------|
| One instance in the world | Multiple instances (per entity) |
| Global state | Entity-specific state |
| Systems share data | Entity owns data |
| Time, input, config | Position, health, velocity |

## Patterns

### Lazy Initialization

```csharp
public class AudioSystem : SystemBase
{
    protected override void OnInitialize()
    {
        if (!World.HasSingleton<AudioState>())
        {
            World.SetSingleton(new AudioState { MasterVolume = 1.0f });
        }
    }
}
```

### Default Values

```csharp
public class PhysicsSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Use TryGet with defaults
        if (!World.TryGetSingleton<PhysicsConfig>(out var config))
        {
            config = new PhysicsConfig { Gravity = -9.81f };
        }

        foreach (var entity in World.Query<Velocity>().Without<Static>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            vel.Y += config.Gravity * deltaTime;
        }
    }
}
```

### System Communication

```csharp
// Combat system writes damage results
public class CombatSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        int totalDamage = 0;
        // Calculate damage...

        World.SetSingleton(new CombatResults
        {
            TotalDamageDealt = totalDamage,
            EntitiesKilled = killCount
        });
    }
}

// UI system reads results
public class UISystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        if (World.TryGetSingleton<CombatResults>(out var results))
        {
            DisplayDamageNumbers(results.TotalDamageDealt);
        }
    }
}
```

## Best Practices

### Do: Use Structs

```csharp
// ✅ Good: Value type
public struct GameTime
{
    public float DeltaTime;
    public float TotalTime;
}
```

### Do: Keep Singletons Focused

```csharp
// ✅ Good: Separate concerns
public struct InputState { /* input only */ }
public struct GameTime { /* time only */ }
public struct AudioConfig { /* audio only */ }

// ❌ Bad: Kitchen sink singleton
public struct GameGlobals
{
    public float DeltaTime;
    public float MoveX, MoveY;
    public float MasterVolume;
    public int Score;
    // ... everything else
}
```

### Don't: Use Singletons for Entity-Specific Data

```csharp
// ❌ Bad: Should be a component on an entity
public struct PlayerHealth
{
    public int Current;
    public int Max;
}

// ✅ Good: Use components
public struct Health : IComponent
{
    public int Current;
    public int Max;
}
```

## Next Steps

- [Systems Guide](systems.md) - Using singletons in systems
- [Components Guide](components.md) - Entity-level data
- [Core Concepts](concepts.md) - ECS fundamentals
