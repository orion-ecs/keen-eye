using KeenEyes;
using KeenEyes.Sample.StringTags;

// =============================================================================
// KEEN EYES ECS - String Tags Demo
// =============================================================================
// This sample demonstrates:
// 1. Adding and removing string tags at runtime
// 2. Querying entities by string tags
// 3. Combining string tags with component queries
// 4. Dynamic state management with tags
// 5. Data-driven tagging from configuration
// 6. Hybrid approach: type-safe + string tags
// =============================================================================

Console.WriteLine("KeenEyes ECS - String Tags Demo");
Console.WriteLine(new string('=', 50));

using var world = new World();

// =============================================================================
// PART 1: Basic String Tag Operations
// =============================================================================

Console.WriteLine("\n[1] Basic String Tag Operations\n");

// Create entities with string tags
var goblin = world.Spawn()
    .WithPosition(x: 100, y: 50)
    .WithHealth(current: 30, max: 30)
    .WithTag("Enemy")           // String tag
    .WithTag("Goblin")          // More specific type
    .WithTag("Hostile")
    .Build();

Console.WriteLine($"Created goblin: {goblin}");

// Check tags
Console.WriteLine($"  Has 'Enemy' tag: {world.HasTag(goblin, "Enemy")}");
Console.WriteLine($"  Has 'Goblin' tag: {world.HasTag(goblin, "Goblin")}");
Console.WriteLine($"  Has 'Boss' tag: {world.HasTag(goblin, "Boss")}");

// Get all tags
var goblinTags = world.GetTags(goblin);
Console.WriteLine($"  All tags: [{string.Join(", ", goblinTags)}]");

// Remove a tag
world.RemoveTag(goblin, "Hostile");
Console.WriteLine($"\nRemoved 'Hostile' tag:");
Console.WriteLine($"  Has 'Hostile' tag: {world.HasTag(goblin, "Hostile")}");
Console.WriteLine($"  Current tags: [{string.Join(", ", world.GetTags(goblin))}]");

// =============================================================================
// PART 2: String Tags During Entity Creation
// =============================================================================

Console.WriteLine("\n[2] String Tags in Entity Builder\n");

var orc = world.Spawn()
    .WithPosition(x: 200, y: 100)
    .WithHealth(current: 80, max: 80)
    .WithLootTable(tableId: "orc_drops", dropChance: 0.5f)
    .WithTag("Enemy")
    .WithTag("Orc")
    .WithTag("Hostile")
    .WithTag("MeleeAttacker")
    .Build();

var archer = world.Spawn()
    .WithPosition(x: 250, y: 150)
    .WithHealth(current: 40, max: 40)
    .WithTag("Enemy")
    .WithTag("Goblin")
    .WithTag("Hostile")
    .WithTag("RangedAttacker")
    .Build();

var boss = world.Spawn()
    .WithPosition(x: 300, y: 100)
    .WithHealth(current: 500, max: 500)
    .WithLootTable(tableId: "boss_drops", dropChance: 1.0f)
    .WithTag("Enemy")
    .WithTag("Boss")
    .WithTag("Hostile")
    .WithTag("DropsTreasure")
    .Build();

Console.WriteLine($"Created orc: {orc} - tags: [{string.Join(", ", world.GetTags(orc))}]");
Console.WriteLine($"Created archer: {archer} - tags: [{string.Join(", ", world.GetTags(archer))}]");
Console.WriteLine($"Created boss: {boss} - tags: [{string.Join(", ", world.GetTags(boss))}]");

// =============================================================================
// PART 3: Querying by String Tags
// =============================================================================

Console.WriteLine("\n[3] Querying by String Tags\n");

// Simple tag query
Console.WriteLine("All entities with 'Enemy' tag:");
foreach (var entity in world.QueryByTag("Enemy"))
{
    Console.WriteLine($"  {entity}");
}

// Query by specific enemy type
Console.WriteLine("\nAll 'Goblin' entities:");
foreach (var entity in world.QueryByTag("Goblin"))
{
    Console.WriteLine($"  {entity}");
}

// Query by attack type
Console.WriteLine("\nAll 'RangedAttacker' entities:");
foreach (var entity in world.QueryByTag("RangedAttacker"))
{
    Console.WriteLine($"  {entity}");
}

// =============================================================================
// PART 4: Combining String Tags with Component Queries
// =============================================================================

Console.WriteLine("\n[4] Combined Tag + Component Queries\n");

// Entities with Health AND "Enemy" tag
Console.WriteLine("Enemies with Health component:");
foreach (var entity in world.Query<Health>().WithTag("Enemy"))
{
    ref readonly var health = ref world.Get<Health>(entity);
    Console.WriteLine($"  {entity}: {health.Current}/{health.Max} HP");
}

// Enemies with loot tables
Console.WriteLine("\nEnemies with LootTable that drop treasure:");
foreach (var entity in world.Query<Health, LootTable>().WithTag("DropsTreasure"))
{
    ref readonly var loot = ref world.Get<LootTable>(entity);
    Console.WriteLine($"  {entity}: {loot.TableId} ({loot.DropChance * 100}% chance)");
}

// Exclude certain tags
Console.WriteLine("\nNon-boss enemies:");
foreach (var entity in world.Query<Health>().WithTag("Enemy").WithoutTag("Boss"))
{
    ref readonly var health = ref world.Get<Health>(entity);
    Console.WriteLine($"  {entity}: {health.Current}/{health.Max} HP");
}

// Multiple tag filters
Console.WriteLine("\nHostile ranged attackers:");
foreach (var entity in world.Query<Position>().WithTag("Hostile").WithTag("RangedAttacker"))
{
    ref readonly var pos = ref world.Get<Position>(entity);
    Console.WriteLine($"  {entity} at ({pos.X}, {pos.Y})");
}

// =============================================================================
// PART 5: Dynamic State Management
// =============================================================================

Console.WriteLine("\n[5] Dynamic State Management with Tags\n");

// Create an entity with AI state tracking via tags
var patrollingGuard = world.Spawn()
    .WithPosition(x: 0, y: 0)
    .WithHealth(current: 100, max: 100)
    .WithAIBehavior(state: "Patrol", timer: 0)
    .WithTag("Guard")
    .WithTag("State:Patrol")
    .Build();

Console.WriteLine($"Created guard: {patrollingGuard}");
Console.WriteLine($"  Initial state: [{string.Join(", ", world.GetTags(patrollingGuard))}]");

// Simulate state transitions
void TransitionTo(Entity entity, string newState)
{
    // Remove old state tag
    foreach (var tag in world.GetTags(entity).ToList())
    {
        if (tag.StartsWith("State:"))
        {
            world.RemoveTag(entity, tag);
        }
    }

    // Add new state tag
    world.AddTag(entity, $"State:{newState}");
    Console.WriteLine($"  Guard transitioned to: {newState}");
}

TransitionTo(patrollingGuard, "Alert");
Console.WriteLine($"  Current tags: [{string.Join(", ", world.GetTags(patrollingGuard))}]");

TransitionTo(patrollingGuard, "Chase");
Console.WriteLine($"  Current tags: [{string.Join(", ", world.GetTags(patrollingGuard))}]");

TransitionTo(patrollingGuard, "Attack");
Console.WriteLine($"  Current tags: [{string.Join(", ", world.GetTags(patrollingGuard))}]");

// Query by state
Console.WriteLine("\nEntities in 'Attack' state:");
foreach (var entity in world.QueryByTag("State:Attack"))
{
    Console.WriteLine($"  {entity}");
}

// =============================================================================
// PART 6: Data-Driven Tagging
// =============================================================================

Console.WriteLine("\n[6] Data-Driven Tagging\n");

// Simulate loading entity definitions from a data file (JSON, XML, etc.)
var entityDefinitions = new[]
{
    new {
        Name = "FireElemental",
        X = 400f, Y = 100f,
        Health = 120,
        Tags = new[] { "Enemy", "Elemental", "Fire", "ImmuneToFire", "Hostile" }
    },
    new {
        Name = "IceElemental",
        X = 450f, Y = 100f,
        Health = 100,
        Tags = new[] { "Enemy", "Elemental", "Ice", "ImmuneToIce", "Hostile" }
    },
    new {
        Name = "NeutralMerchant",
        X = 500f, Y = 200f,
        Health = 50,
        Tags = new[] { "NPC", "Merchant", "Friendly", "CanTrade" }
    }
};

Console.WriteLine("Spawning entities from data definitions:");
foreach (var def in entityDefinitions)
{
    var builder = world.Spawn()
        .WithPosition(x: def.X, y: def.Y)
        .WithHealth(current: def.Health, max: def.Health);

    foreach (var tag in def.Tags)
    {
        builder = builder.WithTag(tag);
    }

    var entity = builder.Build();
    Console.WriteLine($"  {def.Name}: {entity}");
    Console.WriteLine($"    Tags: [{string.Join(", ", world.GetTags(entity))}]");
}

// Query data-driven entities
Console.WriteLine("\nAll Elemental enemies:");
foreach (var entity in world.Query<Health>().WithTag("Elemental"))
{
    var tags = world.GetTags(entity);
    var element = tags.FirstOrDefault(t => t is "Fire" or "Ice" or "Earth" or "Air") ?? "Unknown";
    Console.WriteLine($"  {entity}: {element} elemental");
}

Console.WriteLine("\nFire-immune entities:");
foreach (var entity in world.QueryByTag("ImmuneToFire"))
{
    Console.WriteLine($"  {entity}");
}

// =============================================================================
// PART 7: Hybrid Approach - Type-Safe + String Tags
// =============================================================================

Console.WriteLine("\n[7] Hybrid Approach: Type-Safe + String Tags\n");

// Use type-safe tag for core gameplay mechanics
var hybridEnemy = world.Spawn()
    .WithPosition(x: 600, y: 100)
    .WithHealth(current: 75, max: 75)
    .WithEnemy()                    // Type-safe tag for queries
    .WithTag("Undead")              // String tag for specific type
    .WithTag("ResistPhysical")      // String tag for resistances
    .WithTag("WeakToHoly")          // String tag for weaknesses
    .WithTag("NightSpawned")        // String tag for spawn condition
    .Build();

Console.WriteLine($"Created hybrid enemy: {hybridEnemy}");
Console.WriteLine($"  Has Enemy component (type-safe): {world.Has<Enemy>(hybridEnemy)}");
Console.WriteLine($"  String tags: [{string.Join(", ", world.GetTags(hybridEnemy))}]");

// Use type-safe query for performance-critical paths
Console.WriteLine("\nType-safe Enemy query (fastest for hot paths):");
foreach (var entity in world.Query<Health>().With<Enemy>())
{
    Console.WriteLine($"  {entity}");
}

// Use string tags for flexible filtering
Console.WriteLine("\nEnemies weak to holy damage:");
foreach (var entity in world.Query<Health>().With<Enemy>().WithTag("WeakToHoly"))
{
    Console.WriteLine($"  {entity}");
}

// =============================================================================
// PART 8: Tag Constants Pattern
// =============================================================================

Console.WriteLine("\n[8] Tag Constants Pattern\n");

// Tag constants are defined in TagConstants.cs for consistency
// This provides IDE autocomplete and prevents typos

var dragon = world.Spawn()
    .WithPosition(x: 700, y: 50)
    .WithHealth(current: 1000, max: 1000)
    .WithTag(Tags.Enemy)
    .WithTag(Tags.Boss)
    .WithTag(Tags.Elements.Fire)
    .WithTag(Tags.Resistances.ImmuneToFire)
    .Build();

Console.WriteLine($"Created dragon using tag constants: {dragon}");
Console.WriteLine($"  Is boss: {world.HasTag(dragon, Tags.Boss)}");
Console.WriteLine($"  Is fire element: {world.HasTag(dragon, Tags.Elements.Fire)}");
Console.WriteLine($"  Immune to fire: {world.HasTag(dragon, Tags.Resistances.ImmuneToFire)}");

// Summary
Console.WriteLine("\n--- Entity Summary ---");
Console.WriteLine($"Total entities with 'Enemy' tag: {world.QueryByTag("Enemy").Count()}");
Console.WriteLine($"Total entities with 'Boss' tag: {world.QueryByTag("Boss").Count()}");
Console.WriteLine($"Total entities with 'Hostile' tag: {world.QueryByTag("Hostile").Count()}");
Console.WriteLine($"Total entities with 'Friendly' tag: {world.QueryByTag("Friendly").Count()}");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("String Tags demo complete!");
