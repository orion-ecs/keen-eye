using KeenEyes;
using KeenEyes.Sample;

// =============================================================================
// KEEN EYE ECS - Source Generator Demo
// =============================================================================
// This sample demonstrates:
// 1. Fluent entity builders with generated WithXxx() methods
// 2. Query API for iterating entities
// 3. System registration and execution
// 4. Multiple isolated worlds
// 5. Plugin system for modular functionality
// 6. WorldBuilder for fluent world configuration
// =============================================================================

Console.WriteLine("KeenEyes ECS - Source Generator Demo");
Console.WriteLine(new string('=', 50));

using var world = new World();

// =============================================================================
// PART 1: Entity Creation with Fluent API
// =============================================================================

Console.WriteLine("\n[1] Creating Entities with Fluent Builders\n");

var player = world.Spawn()
    .WithPosition(x: 100, y: 50)
    .WithVelocity(x: 1, y: 0)
    .WithHealth(current: 100, max: 100)
    .WithSprite(textureId: "player.png", layer: 5)
    .WithPlayer()
    .Build();

Console.WriteLine($"Created player: {player}");

var enemy1 = world.Spawn()
    .WithPosition(x: 200, y: 100)
    .WithVelocity(x: -1, y: 0)
    .WithHealth(current: 50, max: 50)
    .WithSprite(textureId: "enemy.png")
    .WithEnemy()
    .Build();

var enemy2 = world.Spawn()
    .WithPosition(x: 300, y: 150)
    .WithVelocity(x: 0, y: 1)
    .WithHealth(current: 30, max: 30, invulnerable: true)
    .WithSprite(textureId: "boss.png", layer: 10, opacity: 0.9f)
    .WithEnemy()
    .Build();

Console.WriteLine($"Created enemies: {enemy1}, {enemy2}");

var disabledEnemy = world.Spawn()
    .WithPosition(x: 0, y: 0)
    .WithVelocity()
    .WithEnemy()
    .WithDisabled()
    .Build();

Console.WriteLine($"Created disabled enemy: {disabledEnemy}");

// =============================================================================
// PART 2: Query API
// =============================================================================

Console.WriteLine("\n[2] Query API Demo\n");

// Simple query - all entities with Position and Velocity
Console.WriteLine("Entities with Position + Velocity:");
foreach (var entity in world.Query<Position, Velocity>())
{
    Console.WriteLine($"  {entity}");
}

// Filtered query - only players
Console.WriteLine("\nPlayers (Position + Velocity + Player tag):");
foreach (var entity in world.Query<Position, Velocity>().With<Player>())
{
    Console.WriteLine($"  {entity}");
}

// Filtered query - enemies excluding disabled
Console.WriteLine("\nActive enemies (Position + Velocity + Enemy, excluding Disabled):");
foreach (var entity in world.Query<Position, Velocity>().With<Enemy>().Without<Disabled>())
{
    Console.WriteLine($"  {entity}");
}

// =============================================================================
// PART 3: System Registration & Execution
// =============================================================================

Console.WriteLine("\n[3] Systems Demo\n");

// Manual system registration with phases
// Note: MovementSystem uses [RunAfter(typeof(PlayerInputSystem))] for explicit ordering
world
    .AddSystem<PlayerInputSystem>()   // EarlyUpdate phase
    .AddSystem<PhysicsSystem>()       // FixedUpdate phase
    .AddSystem<MovementSystem>()      // Update phase (runs after PlayerInputSystem)
    .AddSystem<EnemyAISystem>()       // Update phase
    .AddSystem<HealthSystem>()        // LateUpdate phase
    .AddSystem<RenderSystem>();       // Render phase

Console.WriteLine("Systems registered. Running one update cycle...\n");

// Simulate one frame
world.Update(deltaTime: 0.016f);

// =============================================================================
// PART 3b: Runtime System Control
// =============================================================================

Console.WriteLine("\n[3b] Runtime System Control Demo\n");

// Get a system by type
var movementSystem = world.GetSystem<MovementSystem>();
Console.WriteLine($"Retrieved MovementSystem: {movementSystem != null}");

// Disable a system at runtime
Console.WriteLine("\nDisabling MovementSystem...");
world.DisableSystem<MovementSystem>();

Console.WriteLine("Running update (MovementSystem disabled)...\n");
world.Update(deltaTime: 0.016f);

// Re-enable the system
Console.WriteLine("\nRe-enabling MovementSystem...");
world.EnableSystem<MovementSystem>();

Console.WriteLine("Running update (MovementSystem enabled)...\n");
world.Update(deltaTime: 0.016f);

// =============================================================================
// PART 3c: Fixed Update Demo
// =============================================================================

Console.WriteLine("\n[3c] Fixed Update Demo\n");

Console.WriteLine("World.FixedUpdate() runs ONLY FixedUpdate phase systems (e.g., PhysicsSystem)");
Console.WriteLine("Calling FixedUpdate twice at fixed timestep...\n");

world.FixedUpdate(fixedDeltaTime: 1f / 60f);
world.FixedUpdate(fixedDeltaTime: 1f / 60f);

Console.WriteLine("\nNote: PhysicsSystem ran twice, other systems were not executed.");

// =============================================================================
// PART 4: Component Registry (per-World)
// =============================================================================

Console.WriteLine("\n[4] Component Registry\n");

foreach (var info in world.Components.All)
{
    var tagMarker = info.IsTag ? " [TAG]" : "";
    Console.WriteLine($"  {info.Name}: Id={info.Id.Value}, Size={info.Size}{tagMarker}");
}

Console.WriteLine($"\nTotal: {world.Components.Count} component types");

// =============================================================================
// PART 5: Multiple Isolated Worlds
// =============================================================================

Console.WriteLine("\n[5] Multiple Worlds (Isolation Demo)\n");

using var world2 = new World();

world2.Spawn().WithPosition(x: 0, y: 0).WithPlayer().Build();
world2.Spawn().WithPosition(x: 10, y: 10).WithVelocity().Build();

Console.WriteLine($"World 1: {world.Components.Count} component types");
Console.WriteLine($"World 2: {world2.Components.Count} component types");
Console.WriteLine("Each world has independent component IDs - no shared static state!");

// =============================================================================
// PART 6: Plugin System
// =============================================================================

Console.WriteLine("\n[6] Plugin System Demo\n");

// Create a world using WorldBuilder with plugins
using var pluginWorld = new WorldBuilder()
    .WithPlugin<DebugPlugin>()        // Installs debug statistics
    .WithSystem<MovementSystem>()      // Add systems after plugins
    .Build();

// Create an entity to process
pluginWorld.Spawn()
    .WithPosition(x: 0, y: 0)
    .WithVelocity(x: 1, y: 0)
    .Build();

Console.WriteLine("\nRunning update with DebugPlugin installed...");
pluginWorld.Update(0.016f);

// Access the extension using the generated typed property (C# 13 extension members)
// Instead of: var stats = pluginWorld.GetExtension<DebugStats>();
// We can now use: pluginWorld.DebugStats
Console.WriteLine($"\nDebug stats from plugin: {pluginWorld.DebugStats.EntitiesProcessed} entities processed");

// Query installed plugins
Console.WriteLine($"\nInstalled plugins:");
foreach (var installedPlugin in pluginWorld.GetPlugins())
{
    Console.WriteLine($"  - {installedPlugin.Name}");
}

// Uninstall a plugin at runtime
Console.WriteLine("\nUninstalling DebugPlugin...");
pluginWorld.UninstallPlugin<DebugPlugin>();
Console.WriteLine($"DebugPlugin still installed? {pluginWorld.HasPlugin<DebugPlugin>()}");

// =============================================================================
// PART 7: WorldBuilder for Complex Setup
// =============================================================================

Console.WriteLine("\n[7] WorldBuilder Demo\n");

// WorldBuilder provides a fluent API for complex world configuration
using var complexWorld = new WorldBuilder()
    .WithPlugin<CombatPlugin>()                              // Install combat plugin
    .WithSystem<PlayerInputSystem>(SystemPhase.EarlyUpdate)  // Add input first
    .WithSystem<MovementSystem>(SystemPhase.Update)          // Then movement
    .WithSystem<RenderSystem>(SystemPhase.Render)            // Then render
    .Build();

Console.WriteLine("Created world with WorldBuilder:");
Console.WriteLine($"  - {complexWorld.GetPlugins().Count()} plugin(s) installed");
Console.WriteLine($"  - Combat plugin: {complexWorld.HasPlugin<CombatPlugin>()}");

// Add entities and run
complexWorld.Spawn()
    .WithPosition(x: 100, y: 50)
    .WithVelocity(x: 1, y: 0)
    .WithPlayer()
    .WithHealth(current: 100, max: 100)
    .Build();

Console.WriteLine("\nRunning WorldBuilder-created world update...");
complexWorld.Update(0.016f);

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Demo complete!");
