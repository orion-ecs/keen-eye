using KeenEyes;
using KeenEyes.Sample.Prefabs;

// =============================================================================
// KEEN EYES ECS - Prefabs Demo
// =============================================================================
// This sample demonstrates:
// 1. Source-generated prefabs (recommended approach)
// 2. Spawning entities from .keprefab files
// 3. Legacy runtime prefabs (deprecated, shown for migration reference)
// =============================================================================

Console.WriteLine("KeenEyes ECS - Prefabs Demo");
Console.WriteLine(new string('=', 50));

using var world = new World();

// =============================================================================
// PART 1: Source-Generated Prefabs (Recommended)
// =============================================================================

Console.WriteLine("\n[1] Source-Generated Prefabs (from .keprefab files)\n");

// Prefabs defined in Prefabs/*.keprefab files are automatically compiled
// into type-safe spawn methods in the Scenes class.

// Spawn multiple enemies from the BasicEnemy prefab
var enemy1 = Scenes.SpawnBasicEnemy(world);
var enemy2 = Scenes.SpawnBasicEnemy(world);
var enemy3 = Scenes.SpawnBasicEnemy(world);

Console.WriteLine($"Spawned enemies: {enemy1}, {enemy2}, {enemy3}");

// Verify they all have the same components
foreach (var entity in world.Query<Position, Health>().With<Enemy>())
{
    ref readonly var health = ref world.Get<Health>(entity);
    Console.WriteLine($"  {entity}: Health = {health.Current}/{health.Max}");
}

// =============================================================================
// PART 2: Customizing Spawned Entities
// =============================================================================

Console.WriteLine("\n[2] Customizing Spawned Entities\n");

// Spawned entities can be modified after creation
var enemyAtOrigin = Scenes.SpawnBasicEnemy(world);
var enemyOnRight = Scenes.SpawnBasicEnemy(world);
var enemyOnTop = Scenes.SpawnBasicEnemy(world);

// Modify positions after spawning
world.Get<Position>(enemyOnRight) = new Position { X = 100, Y = 0 };
world.Get<Position>(enemyOnTop) = new Position { X = 0, Y = 100 };

Console.WriteLine("Enemies with custom positions:");
foreach (var entity in new[] { enemyAtOrigin, enemyOnRight, enemyOnTop })
{
    ref readonly var pos = ref world.Get<Position>(entity);
    Console.WriteLine($"  {entity}: Position = ({pos.X}, {pos.Y})");
}

// =============================================================================
// PART 3: All Available Prefabs
// =============================================================================

Console.WriteLine("\n[3] Available Prefab Types\n");

// Spawn one of each prefab type
var flyingEnemy = Scenes.SpawnFlyingEnemy(world);
var bossEnemy = Scenes.SpawnBossEnemy(world);
var flyingBoss = Scenes.SpawnFlyingBoss(world);
var player = Scenes.SpawnPlayer(world);
var coin = Scenes.SpawnCoin(world);

Console.WriteLine("Spawned from .keprefab files:");
Console.WriteLine($"  Flying Enemy: {flyingEnemy}");
Console.WriteLine($"  Boss Enemy: {bossEnemy}");
Console.WriteLine($"  Flying Boss: {flyingBoss}");
Console.WriteLine($"  Player: {player}");
Console.WriteLine($"  Coin: {coin}");

// Show their health values
Console.WriteLine("\nHealth values:");
Console.WriteLine($"  Flying Enemy: {world.Get<Health>(flyingEnemy).Max}");
Console.WriteLine($"  Boss Enemy: {world.Get<Health>(bossEnemy).Max}");
Console.WriteLine($"  Flying Boss: {world.Get<Health>(flyingBoss).Max}");
Console.WriteLine($"  Player: {world.Get<Health>(player).Max}");

// Check tags on Flying Boss
Console.WriteLine("\nTags on Flying Boss:");
Console.WriteLine($"  Has Enemy tag: {world.Has<Enemy>(flyingBoss)}");
Console.WriteLine($"  Has Flying tag: {world.Has<Flying>(flyingBoss)}");
Console.WriteLine($"  Has Boss tag: {world.Has<Boss>(flyingBoss)}");

// =============================================================================
// PART 4: Listing Available Prefabs
// =============================================================================

Console.WriteLine("\n[4] Available Prefabs (from Scenes.All)\n");

// The generated Scenes class provides a list of all prefab names
Console.WriteLine("Available prefabs:");
foreach (var prefabName in Scenes.All)
{
    Console.WriteLine($"  - {prefabName}");
}

// =============================================================================
// PART 5: Practical Example - Level Setup
// =============================================================================

Console.WriteLine("\n[5] Practical Example - Level Setup\n");

// Simulate level data (would normally come from a file)
var levelData = new[]
{
    ("Player", 100f, 50f),
    ("BasicEnemy", 200f, 100f),
    ("BasicEnemy", 300f, 150f),
    ("FlyingEnemy", 250f, 200f),
    ("BossEnemy", 400f, 100f),
};

Console.WriteLine("Spawning level entities:");
foreach (var (prefabName, x, y) in levelData)
{
    // Spawn the prefab
    Entity entity = prefabName switch
    {
        "Player" => Scenes.SpawnPlayer(world),
        "BasicEnemy" => Scenes.SpawnBasicEnemy(world),
        "FlyingEnemy" => Scenes.SpawnFlyingEnemy(world),
        "BossEnemy" => Scenes.SpawnBossEnemy(world),
        _ => Entity.Null
    };

    // Set the position after spawning
    if (entity.IsValid)
    {
        world.Get<Position>(entity) = new Position { X = x, Y = y };
    }

    Console.WriteLine($"  {prefabName} at ({x}, {y}) -> {entity}");
}

// Count entities by type
var playerCount = world.Query<Position>().With<Player>().Count();
var enemyCount = world.Query<Position>().With<Enemy>().Count();
var bossCount = world.Query<Position>().With<Boss>().Count();

Console.WriteLine($"\nLevel summary:");
Console.WriteLine($"  Players: {playerCount}");
Console.WriteLine($"  Enemies: {enemyCount}");
Console.WriteLine($"  Bosses: {bossCount}");

// =============================================================================
// PART 6: Legacy Runtime Prefabs (Deprecated)
// =============================================================================

Console.WriteLine("\n[6] Legacy Runtime Prefabs (Deprecated)\n");
Console.WriteLine("The following demonstrates the deprecated runtime prefab API.");
Console.WriteLine("This is shown for migration reference - prefer .keprefab files.\n");

#pragma warning disable CS0618 // Type or member is obsolete

// Create a runtime prefab (deprecated)
var customPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 999, Max = 999 })
    .WithTag<Enemy>();

world.RegisterPrefab("CustomEnemy", customPrefab);
Console.WriteLine("Registered 'CustomEnemy' runtime prefab (deprecated)");

// Spawn from runtime prefab
var customEnemy = world.SpawnFromPrefab("CustomEnemy").Build();
Console.WriteLine($"Spawned from runtime prefab: {customEnemy}");
Console.WriteLine($"  Health: {world.Get<Health>(customEnemy).Max}");

// Named entity spawning from runtime prefab
var namedBoss = world.SpawnFromPrefab("CustomEnemy", "TheCustomBoss").Build();
Console.WriteLine($"Spawned named entity: {namedBoss}");

// Retrieve by name
var foundBoss = world.GetEntityByName("TheCustomBoss");
Console.WriteLine($"Found by name: {foundBoss} (valid: {foundBoss.IsValid})");

// Runtime prefab management
Console.WriteLine($"\nHas 'CustomEnemy': {world.HasPrefab("CustomEnemy")}");
Console.WriteLine($"Has 'NonExistent': {world.HasPrefab("NonExistent")}");

Console.WriteLine("\nAll runtime prefabs:");
foreach (var prefabName in world.GetAllPrefabNames())
{
    Console.WriteLine($"  - {prefabName}");
}

// Unregister
world.UnregisterPrefab("CustomEnemy");
Console.WriteLine("\nUnregistered 'CustomEnemy'");
Console.WriteLine($"Has 'CustomEnemy': {world.HasPrefab("CustomEnemy")}");

#pragma warning restore CS0618

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Prefabs demo complete!");
Console.WriteLine("\nMigration tip: Replace runtime prefabs with .keprefab files");
Console.WriteLine("for compile-time validation and type-safe spawn methods.");
