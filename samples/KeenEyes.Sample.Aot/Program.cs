// ============================================================================
// KeenEyes ECS - Native AOT Sample
// ============================================================================
// This sample demonstrates KeenEyes ECS running as a Native AOT application.
//
// Key AOT-compatible patterns demonstrated:
// 1. Component definitions as structs implementing IComponent/ITagComponent
// 2. System definitions using SystemBase (no reflection)
// 3. WorldBuilder using factory delegates (no Activator.CreateInstance)
// 4. Query API with compile-time type safety
// 5. Events and singletons without reflection
//
// To publish as native AOT:
//   dotnet publish -c Release
//
// The PublishAot=true setting is already configured in this project.
// ============================================================================

using KeenEyes;

Console.WriteLine("KeenEyes Native AOT Sample");
Console.WriteLine(new string('=', 40));

// Create world using WorldBuilder (uses factory delegates, AOT-safe)
using var world = new WorldBuilder()
    .WithSystem<MovementSystem>()
    .WithSystem<HealthRegenSystem>()
    .Build();

// Register components explicitly
world.Components.Register<Position>();
world.Components.Register<Velocity>();
world.Components.Register<Health>();

// Spawn entities with components
Console.WriteLine("\nSpawning entities...");

var player = world.Spawn("Player")
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { Dx = 1, Dy = 0.5f })
    .With(new Health { Current = 80, Max = 100 })
    .Build();

var enemy1 = world.Spawn("Enemy1")
    .With(new Position { X = 100, Y = 50 })
    .With(new Velocity { Dx = -0.5f, Dy = 0 })
    .With(new Health { Current = 50, Max = 50 })
    .WithTag<EnemyTag>()
    .Build();

var enemy2 = world.Spawn("Enemy2")
    .With(new Position { X = 200, Y = 100 })
    .With(new Velocity { Dx = 0, Dy = -1 })
    .With(new Health { Current = 30, Max = 30 })
    .WithTag<EnemyTag>()
    .Build();

Console.WriteLine($"Created: {world.GetName(player)}, {world.GetName(enemy1)}, {world.GetName(enemy2)}");

// Set up a singleton resource
world.SetSingleton(new GameSettings { TimeScale = 1.0f });

// Set up component events (AOT-safe callbacks)
world.OnComponentAdded<Health>((entity, health) =>
{
    Console.WriteLine($"  [Event] Health added to {world.GetName(entity)}: {health.Current}/{health.Max}");
});

// Add another entity to trigger the event
var latecomer = world.Spawn("Latecomer")
    .With(new Position { X = 50, Y = 50 })
    .With(new Health { Current = 100, Max = 100 })
    .Build();

// Run simulation for a few frames
Console.WriteLine("\nRunning simulation (5 frames)...\n");
for (int frame = 1; frame <= 5; frame++)
{
    Console.WriteLine($"Frame {frame}:");
    world.Update(deltaTime: 0.016f); // ~60 FPS

    // Print entity positions
    foreach (var entity in world.Query<Position, Velocity>())
    {
        ref var pos = ref world.Get<Position>(entity);
        var name = world.GetName(entity);
        Console.WriteLine($"  {name}: ({pos.X:F1}, {pos.Y:F1})");
    }

    Console.WriteLine();
}

// Query demonstration
Console.WriteLine("Query demonstration:");

Console.WriteLine("\nAll entities with Health:");
foreach (var entity in world.Query<Health>())
{
    ref var health = ref world.Get<Health>(entity);
    Console.WriteLine($"  {world.GetName(entity)}: {health.Current}/{health.Max}");
}

Console.WriteLine("\nEnemies only (using With<EnemyTag>):");
foreach (var entity in world.Query<Position, Health>().With<EnemyTag>())
{
    ref var pos = ref world.Get<Position>(entity);
    ref var health = ref world.Get<Health>(entity);
    Console.WriteLine($"  {world.GetName(entity)}: pos=({pos.X:F1}, {pos.Y:F1}), health={health.Current}/{health.Max}");
}

// Singleton access
ref var settings = ref world.GetSingleton<GameSettings>();
Console.WriteLine($"\nGame settings (singleton): TimeScale = {settings.TimeScale}");

Console.WriteLine("\n" + new string('=', 40));
Console.WriteLine("Native AOT sample completed successfully!");

// ============================================================================
// Component Definitions (AOT-compatible)
// ============================================================================

/// <summary>Position component for 2D coordinates.</summary>
public struct Position : IComponent
{
    /// <summary>X coordinate.</summary>
    public float X;

    /// <summary>Y coordinate.</summary>
    public float Y;
}

/// <summary>Velocity component for movement speed.</summary>
public struct Velocity : IComponent
{
    /// <summary>Horizontal velocity.</summary>
    public float Dx;

    /// <summary>Vertical velocity.</summary>
    public float Dy;
}

/// <summary>Health component for entity health tracking.</summary>
public struct Health : IComponent
{
    /// <summary>Current health value.</summary>
    public int Current;

    /// <summary>Maximum health value.</summary>
    public int Max;
}

/// <summary>Tag component marking enemy entities.</summary>
public struct EnemyTag : ITagComponent;

/// <summary>Singleton for game-wide settings.</summary>
public struct GameSettings
{
    /// <summary>Time scale multiplier.</summary>
    public float TimeScale;
}

// ============================================================================
// System Definitions (AOT-compatible)
// ============================================================================

/// <summary>System that updates entity positions based on velocity.</summary>
public class MovementSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);

            pos.X += vel.Dx * deltaTime;
            pos.Y += vel.Dy * deltaTime;
        }
    }
}

/// <summary>System that regenerates health for non-enemy entities.</summary>
public class HealthRegenSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref var health = ref World.Get<Health>(entity);

            // Regenerate 1 health per second for non-enemies
            if (!World.Has<EnemyTag>(entity) && health.Current < health.Max)
            {
                health.Current = Math.Min(health.Current + (int)(10 * deltaTime), health.Max);
            }
        }
    }
}
