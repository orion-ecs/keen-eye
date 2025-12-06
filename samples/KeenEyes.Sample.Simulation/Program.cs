using System.Diagnostics;
using System.Text;
using KeenEyes;
using KeenEyes.Sample.Simulation;

// =============================================================================
// KEEN EYES ECS - Self-Running Simulation
// =============================================================================
// A fully autonomous simulation demonstrating:
// - Continuous game loop with fixed timestep
// - Real-time ASCII visualization
// - Entity spawning/despawning
// - Collision detection and combat
// - Multiple system phases (EarlyUpdate, Update, LateUpdate, Render, PostRender)
// =============================================================================

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

// Configuration
const int WorldWidth = 60;
const int WorldHeight = 20;
const float TargetFps = 30f;
const float FixedDeltaTime = 1f / TargetFps;

// Statistics
int totalKills = 0;
int playerDeaths = 0;
float gameTime = 0;

// Create world
using var world = new World();

// Create player in center
var player = world.Spawn()
    .WithPosition(x: WorldWidth / 2f, y: WorldHeight / 2f)
    .WithVelocity(x: 3, y: 2)
    .WithHealth(current: 5, max: 5)
    .WithCollider(radius: 0.5f)
    .WithRenderable(symbol: '#', color: ConsoleColor.Green)
    .WithCooldown(remaining: 0)
    .WithPlayer()
    .Build();

// Configure and register systems
var movementSystem = new MovementSystem { WorldWidth = WorldWidth, WorldHeight = WorldHeight };
var spawnerSystem = new SpawnerSystem
{
    WorldWidth = WorldWidth,
    WorldHeight = WorldHeight,
    SpawnInterval = 1.5f,
    MaxEnemies = 10
};
var healthSystem = new HealthSystem();
healthSystem.OnEnemyKilled += () => totalKills++;

world
    .AddSystem(spawnerSystem)
    .AddSystem<CooldownSystem>()
    .AddSystem<ShootingSystem>()
    .AddSystem<EnemyShootingSystem>()
    .AddSystem(movementSystem)
    .AddSystem<CollisionSystem>()
    .AddSystem(healthSystem)
    .AddSystem<LifetimeSystem>()
    .AddSystem<CleanupSystem>();

// Frame buffer for double-buffering
var frameBuffer = new char[WorldHeight, WorldWidth];
var colorBuffer = new ConsoleColor[WorldHeight, WorldWidth];

// Clear screen and show header
Console.Clear();
Console.WriteLine("KeenEyes ECS - Self-Running Simulation");
Console.WriteLine(new string('=', WorldWidth));
Console.WriteLine($"# = Player (bounces, auto-fires)  |  o/O/@ = Enemies  |  */. = Projectiles");
Console.WriteLine(new string('-', WorldWidth));

var headerLines = 4;
var statsLine = headerLines + WorldHeight + 1;

// Game loop
var stopwatch = Stopwatch.StartNew();
var lastFrameTime = stopwatch.Elapsed.TotalSeconds;
var accumulator = 0.0;

Console.WriteLine("\nPress Ctrl+C to exit\n");

// Run for a fixed duration or until interrupted
var running = true;
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    running = false;
};

while (running)
{
    var currentTime = stopwatch.Elapsed.TotalSeconds;
    var frameTime = currentTime - lastFrameTime;
    lastFrameTime = currentTime;

    // Cap frame time to prevent spiral of death
    if (frameTime > 0.25)
    {
        frameTime = 0.25;
    }

    accumulator += frameTime;

    // Fixed timestep updates
    while (accumulator >= FixedDeltaTime)
    {
        // Check if player is dead and respawn
        if (!world.IsAlive(player) || (world.Has<Dead>(player)))
        {
            playerDeaths++;

            // Remove old player entity if it still exists
            if (world.IsAlive(player))
            {
                world.Despawn(player);
            }

            // Respawn player
            player = world.Spawn()
                .WithPosition(x: WorldWidth / 2f, y: WorldHeight / 2f)
                .WithVelocity(x: 3, y: 2)
                .WithHealth(current: 5, max: 5)
                .WithCollider(radius: 0.5f)
                .WithRenderable(symbol: '#', color: ConsoleColor.Green)
                .WithCooldown(remaining: 0)
                .WithPlayer()
                .Build();
        }

        world.Update(FixedDeltaTime);
        gameTime += FixedDeltaTime;
        accumulator -= FixedDeltaTime;
    }

    // Render
    RenderFrame();

    // Sleep to cap frame rate
    var elapsed = stopwatch.Elapsed.TotalSeconds - currentTime;
    var sleepTime = (1.0 / TargetFps) - elapsed;
    if (sleepTime > 0)
    {
        Thread.Sleep((int)(sleepTime * 1000));
    }
}

Console.CursorVisible = true;
Console.SetCursorPosition(0, statsLine + 3);
Console.ResetColor();
Console.WriteLine("\nSimulation ended.");
Console.WriteLine($"Final Stats: {totalKills} kills, {playerDeaths} deaths, {gameTime:F1}s played");

// =============================================================================
// RENDERING
// =============================================================================

void RenderFrame()
{
    // Clear buffers
    for (int y = 0; y < WorldHeight; y++)
    {
        for (int x = 0; x < WorldWidth; x++)
        {
            frameBuffer[y, x] = ' ';
            colorBuffer[y, x] = ConsoleColor.Black;
        }
    }

    // Render all entities with Position and Renderable
    foreach (var entity in world.Query<Position, Renderable>().Without<Dead>())
    {
        ref readonly var pos = ref world.Get<Position>(entity);
        ref readonly var render = ref world.Get<Renderable>(entity);

        int x = (int)pos.X;
        int y = (int)pos.Y;

        if (x >= 0 && x < WorldWidth && y >= 0 && y < WorldHeight)
        {
            // Later entities overwrite earlier ones (projectiles on top of enemies)
            frameBuffer[y, x] = render.Symbol;
            colorBuffer[y, x] = render.Color;
        }
    }

    // Draw frame to console
    for (int y = 0; y < WorldHeight; y++)
    {
        Console.SetCursorPosition(0, headerLines + y);
        for (int x = 0; x < WorldWidth; x++)
        {
            var color = colorBuffer[y, x];
            if (color != ConsoleColor.Black)
            {
                Console.ForegroundColor = color;
                Console.Write(frameBuffer[y, x]);
            }
            else
            {
                Console.ResetColor();
                Console.Write('.');
            }
        }
    }

    // Draw stats
    Console.SetCursorPosition(0, statsLine);
    Console.ResetColor();

    // Count entities
    int enemyCount = world.Query<Position>().With<Enemy>().Without<Dead>().Without<Projectile>().Count();
    int projectileCount = world.Query<Position>().With<Projectile>().Without<Dead>().Count();

    // Get player health
    int playerHealth = 0;
    int playerMaxHealth = 0;
    if (world.IsAlive(player) && world.Has<Health>(player))
    {
        ref readonly var health = ref world.Get<Health>(player);
        playerHealth = health.Current;
        playerMaxHealth = health.Max;
    }

    var healthBar = new string('#', playerHealth) + new string('-', playerMaxHealth - playerHealth);

    Console.Write($"HP: [{healthBar}] | Kills: {totalKills,4} | Deaths: {playerDeaths,3} | Enemies: {enemyCount,2} | Time: {gameTime,6:F1}s   ");
}
