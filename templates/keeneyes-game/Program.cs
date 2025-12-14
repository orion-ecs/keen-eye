using KeenEyes;
using KeenEyesGame;

// Create the world
using var world = new World();

// Register systems
world
    .AddSystem<MovementSystem>()
    .AddSystem<RenderSystem>();

// Create entities with the fluent builder API
var player = world.Spawn()
    .WithPosition(x: 0, y: 0)
    .WithVelocity(x: 1, y: 0)
    .WithPlayer()
    .Build();

Console.WriteLine($"Created player: {player}");

// Create some enemies
for (int i = 0; i < 3; i++)
{
    world.Spawn()
        .WithPosition(x: i * 10, y: i * 5)
        .WithVelocity(x: -0.5f, y: 0)
        .WithEnemy()
        .Build();
}

Console.WriteLine($"Total entities: {world.EntityCount}");

// Run game loop
const float deltaTime = 1f / 60f;
for (int frame = 0; frame < 5; frame++)
{
    Console.WriteLine($"\n--- Frame {frame + 1} ---");
    world.Update(deltaTime);
}

Console.WriteLine("\nGame finished!");
