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
using KeenEyes.Sample.Aot;

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
_ = world.Spawn("Latecomer")
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
