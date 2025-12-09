using KeenEyes;
using KeenEyes.Sample.Prefabs;

// =============================================================================
// KEEN EYES ECS - Prefabs Demo
// =============================================================================
// This sample demonstrates:
// 1. Creating and registering prefabs (reusable entity templates)
// 2. Spawning entities from prefabs
// 3. Prefab inheritance with component overrides
// 4. Customizing prefab instances at spawn time
// 5. Named entity spawning from prefabs
// =============================================================================

Console.WriteLine("KeenEyes ECS - Prefabs Demo");
Console.WriteLine(new string('=', 50));

using var world = new World();

// =============================================================================
// PART 1: Basic Prefab Creation and Registration
// =============================================================================

Console.WriteLine("\n[1] Basic Prefab Creation\n");

// Create a basic enemy prefab
var basicEnemyPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 0, Y = 0 })
    .With(new Health { Current = 50, Max = 50 })
    .With(new Sprite { TextureId = "enemy_basic.png", Layer = 1 })
    .WithTag<Enemy>();

world.RegisterPrefab("BasicEnemy", basicEnemyPrefab);
Console.WriteLine("Registered 'BasicEnemy' prefab");

// Create a collectible prefab
var coinPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Sprite { TextureId = "coin.png", Layer = 0 })
    .WithTag<Collectible>();

world.RegisterPrefab("Coin", coinPrefab);
Console.WriteLine("Registered 'Coin' prefab");

// =============================================================================
// PART 2: Spawning Entities from Prefabs
// =============================================================================

Console.WriteLine("\n[2] Spawning from Prefabs\n");

// Spawn multiple enemies from the same prefab
var enemy1 = world.SpawnFromPrefab("BasicEnemy").Build();
var enemy2 = world.SpawnFromPrefab("BasicEnemy").Build();
var enemy3 = world.SpawnFromPrefab("BasicEnemy").Build();

Console.WriteLine($"Spawned enemies: {enemy1}, {enemy2}, {enemy3}");

// Verify they all have the same components
foreach (var entity in world.Query<Position, Health>().With<Enemy>())
{
    ref readonly var health = ref world.Get<Health>(entity);
    Console.WriteLine($"  {entity}: Health = {health.Current}/{health.Max}");
}

// =============================================================================
// PART 3: Customizing Prefab Instances
// =============================================================================

Console.WriteLine("\n[3] Customizing Prefab Instances\n");

// Spawn enemies with custom positions
var enemyAtOrigin = world.SpawnFromPrefab("BasicEnemy")
    .With(new Position { X = 0, Y = 0 })
    .Build();

var enemyOnRight = world.SpawnFromPrefab("BasicEnemy")
    .With(new Position { X = 100, Y = 0 })
    .Build();

var enemyOnTop = world.SpawnFromPrefab("BasicEnemy")
    .With(new Position { X = 0, Y = 100 })
    .Build();

Console.WriteLine("Enemies with custom positions:");
foreach (var entity in new[] { enemyAtOrigin, enemyOnRight, enemyOnTop })
{
    ref readonly var pos = ref world.Get<Position>(entity);
    Console.WriteLine($"  {entity}: Position = ({pos.X}, {pos.Y})");
}

// =============================================================================
// PART 4: Prefab Inheritance
// =============================================================================

Console.WriteLine("\n[4] Prefab Inheritance\n");

// Create a flying enemy that extends basic enemy
var flyingEnemyPrefab = new EntityPrefab()
    .Extends("BasicEnemy")                         // Inherit from BasicEnemy
    .With(new Velocity { X = 0, Y = -2 })          // Add downward velocity
    .With(new AIBehavior { DetectionRange = 150f }) // Add AI
    .WithTag<Flying>();                            // Add Flying tag

world.RegisterPrefab("FlyingEnemy", flyingEnemyPrefab);
Console.WriteLine("Registered 'FlyingEnemy' prefab (extends BasicEnemy)");

// Create a boss enemy that extends basic enemy with overrides
var bossEnemyPrefab = new EntityPrefab()
    .Extends("BasicEnemy")
    .With(new Health { Current = 500, Max = 500 }) // Override health
    .With(new Damage { Amount = 25, Range = 3f }) // Add damage
    .With(new Sprite { TextureId = "boss.png", Layer = 5 }) // Override sprite
    .WithTag<Boss>();

world.RegisterPrefab("BossEnemy", bossEnemyPrefab);
Console.WriteLine("Registered 'BossEnemy' prefab (extends BasicEnemy with overrides)");

// Create a flying boss that extends flying enemy
var flyingBossPrefab = new EntityPrefab()
    .Extends("FlyingEnemy")
    .With(new Health { Current = 750, Max = 750 })
    .With(new Damage { Amount = 40, Range = 5f })
    .WithTag<Boss>();

world.RegisterPrefab("FlyingBoss", flyingBossPrefab);
Console.WriteLine("Registered 'FlyingBoss' prefab (extends FlyingEnemy)");

// Spawn instances of each
var flyingEnemy = world.SpawnFromPrefab("FlyingEnemy").Build();
var bossEnemy = world.SpawnFromPrefab("BossEnemy").Build();
var flyingBoss = world.SpawnFromPrefab("FlyingBoss").Build();

Console.WriteLine("\nSpawned inherited enemies:");
Console.WriteLine($"  Flying Enemy: {flyingEnemy}");
Console.WriteLine($"  Boss Enemy: {bossEnemy}");
Console.WriteLine($"  Flying Boss: {flyingBoss}");

// Show their health values (demonstrating inheritance)
Console.WriteLine("\nHealth values (showing inheritance):");
Console.WriteLine($"  Flying Enemy: {world.Get<Health>(flyingEnemy).Max} (inherited from BasicEnemy)");
Console.WriteLine($"  Boss Enemy: {world.Get<Health>(bossEnemy).Max} (overridden)");
Console.WriteLine($"  Flying Boss: {world.Get<Health>(flyingBoss).Max} (overridden in FlyingBoss)");

// Check tags
Console.WriteLine("\nTags on Flying Boss:");
Console.WriteLine($"  Has Enemy tag: {world.Has<Enemy>(flyingBoss)}");
Console.WriteLine($"  Has Flying tag: {world.Has<Flying>(flyingBoss)}");
Console.WriteLine($"  Has Boss tag: {world.Has<Boss>(flyingBoss)}");

// =============================================================================
// PART 5: Named Entity Spawning
// =============================================================================

Console.WriteLine("\n[5] Named Entity Spawning\n");

// Spawn named entities from prefabs
var mainBoss = world.SpawnFromPrefab("BossEnemy", "MainBoss").Build();
var miniBoss1 = world.SpawnFromPrefab("BossEnemy", "MiniBoss_Left").Build();
var miniBoss2 = world.SpawnFromPrefab("BossEnemy", "MiniBoss_Right").Build();

Console.WriteLine("Spawned named boss entities:");
Console.WriteLine($"  MainBoss: {mainBoss}");
Console.WriteLine($"  MiniBoss_Left: {miniBoss1}");
Console.WriteLine($"  MiniBoss_Right: {miniBoss2}");

// Retrieve by name later
var foundBoss = world.GetEntityByName("MainBoss");
Console.WriteLine($"\nLooking up 'MainBoss': {foundBoss}");
Console.WriteLine($"  Found: {foundBoss.IsValid}");

// =============================================================================
// PART 6: Prefab Management
// =============================================================================

Console.WriteLine("\n[6] Prefab Management\n");

// List all registered prefabs
Console.WriteLine("Registered prefabs:");
foreach (var prefabName in world.GetAllPrefabNames())
{
    Console.WriteLine($"  - {prefabName}");
}

// Check if a prefab exists
Console.WriteLine($"\nHas 'BasicEnemy': {world.HasPrefab("BasicEnemy")}");
Console.WriteLine($"Has 'Dragon': {world.HasPrefab("Dragon")}");

// Unregister a prefab (entities already spawned are unaffected)
world.UnregisterPrefab("Coin");
Console.WriteLine("\nUnregistered 'Coin' prefab");
Console.WriteLine($"Has 'Coin': {world.HasPrefab("Coin")}");

// =============================================================================
// PART 7: Practical Example - Level Setup
// =============================================================================

Console.WriteLine("\n[7] Practical Example - Level Setup\n");

// Create player prefab
var playerPrefab = new EntityPrefab()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .With(new Sprite { TextureId = "player.png", Layer = 10 })
    .WithTag<Player>();

world.RegisterPrefab("Player", playerPrefab);

// Simulate level data (would normally come from a file)
var levelData = new[]
{
    ("Player", "PlayerEntity", 100f, 50f),
    ("BasicEnemy", null, 200f, 100f),
    ("BasicEnemy", null, 300f, 150f),
    ("FlyingEnemy", null, 250f, 200f),
    ("BossEnemy", "LevelBoss", 400f, 100f),
};

Console.WriteLine("Spawning level entities from prefabs:");
foreach (var (prefabName, entityName, x, y) in levelData)
{
    var entity = world.SpawnFromPrefab(prefabName, entityName)
        .With(new Position { X = x, Y = y })
        .Build();

    var name = entityName ?? "(unnamed)";
    Console.WriteLine($"  {name}: {prefabName} at ({x}, {y}) -> {entity}");
}

// Count entities by type
var playerCount = world.Query<Position>().With<Player>().Count();
var enemyCount = world.Query<Position>().With<Enemy>().Count();
var bossCount = world.Query<Position>().With<Boss>().Count();

Console.WriteLine($"\nLevel summary:");
Console.WriteLine($"  Players: {playerCount}");
Console.WriteLine($"  Enemies: {enemyCount}");
Console.WriteLine($"  Bosses: {bossCount}");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Prefabs demo complete!");
