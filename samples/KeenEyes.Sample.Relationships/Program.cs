using KeenEyes;
using KeenEyes.Sample.Relationships;

// =============================================================================
// KEEN EYES ECS - Relationships & Reactivity Demo (v0.3.0 Features)
// =============================================================================
// This sample demonstrates:
// 1. Entity hierarchy (parent-child relationships)
// 2. Event system (component and entity lifecycle events)
// 3. Change tracking (dirty entity detection)
// =============================================================================

Console.WriteLine("KeenEyes ECS - Relationships & Reactivity Demo (v0.3.0)");
Console.WriteLine(new string('=', 60));

using var world = new World();

// =============================================================================
// PART 1: Entity Hierarchy
// =============================================================================

Console.WriteLine("\n[1] Entity Hierarchy Demo\n");

// Create a scene root
var sceneRoot = world.Spawn("SceneRoot")
    .WithPosition(0, 0)
    .WithSceneRoot()
    .Build();

Console.WriteLine($"Created scene root: {sceneRoot}");

// Create a player as child of scene
var player = world.Spawn("Player")
    .WithPosition(100, 50)
    .WithLocalOffset(0, 0)
    .WithHealth(100, 100)
    .Build();

world.SetParent(player, sceneRoot);
Console.WriteLine($"Created player: {player} (child of scene root)");

// Create a weapon as child of player
var weapon = world.Spawn("Sword")
    .WithLocalOffset(10, 0)
    .WithDamage(25)
    .Build();

world.SetParent(weapon, player);
Console.WriteLine($"Created weapon: {weapon} (child of player)");

// Create a shield as another child of player
var shield = world.Spawn("Shield")
    .WithLocalOffset(-10, 0)
    .Build();

world.AddChild(player, shield);  // Alternative syntax
Console.WriteLine($"Created shield: {shield} (child of player)");

// Demonstrate hierarchy traversal
Console.WriteLine("\nHierarchy from player's perspective:");
Console.WriteLine($"  Parent: {world.GetParent(player)} ({world.GetName(world.GetParent(player))})");
Console.WriteLine($"  Children: {string.Join(", ", world.GetChildren(player).Select(c => world.GetName(c)))}");

Console.WriteLine("\nAll descendants of scene root:");
foreach (var descendant in world.GetDescendants(sceneRoot))
{
    var name = world.GetName(descendant) ?? "unnamed";
    var parent = world.GetParent(descendant);
    var parentName = parent.IsValid ? world.GetName(parent) : "none";
    Console.WriteLine($"  {name} (parent: {parentName})");
}

// Find root from any entity
Console.WriteLine($"\nRoot of weapon: {world.GetName(world.GetRoot(weapon))}");

// =============================================================================
// PART 2: Event System
// =============================================================================

Console.WriteLine("\n[2] Event System Demo\n");

// Track event counts
var createdCount = 0;
var destroyedCount = 0;
var damageEventCount = 0;

// Subscribe to entity lifecycle events
var createdSub = world.OnEntityCreated((entity, name) =>
{
    createdCount++;
    Console.WriteLine($"  [EVENT] Entity created: {name ?? "unnamed"} ({entity})");
});

var destroyedSub = world.OnEntityDestroyed(entity =>
{
    destroyedCount++;
    var name = world.GetName(entity) ?? "unnamed";
    Console.WriteLine($"  [EVENT] Entity destroyed: {name} ({entity})");
});

// Subscribe to component events
var healthAddedSub = world.OnComponentAdded<Health>((entity, health) =>
{
    var name = world.GetName(entity) ?? "unnamed";
    Console.WriteLine($"  [EVENT] Health added to {name}: {health.Current}/{health.Max}");
});

var healthChangedSub = world.OnComponentChanged<Health>((entity, oldVal, newVal) =>
{
    var name = world.GetName(entity) ?? "unnamed";
    Console.WriteLine($"  [EVENT] Health changed for {name}: {oldVal.Current} -> {newVal.Current}");
});

// Subscribe to custom events
var damageSub = world.Events.Subscribe<DamageEvent>(evt =>
{
    damageEventCount++;
    var targetName = world.GetName(evt.Target) ?? "unnamed";
    Console.WriteLine($"  [EVENT] Damage event: {targetName} took {evt.Amount} damage");
});

var deathSub = world.Events.Subscribe<DeathEvent>(evt =>
{
    var name = world.GetName(evt.Entity) ?? "unnamed";
    Console.WriteLine($"  [EVENT] Death event: {name} died ({evt.Cause})");
});

Console.WriteLine("Creating entities (watch for events):");

// Create some enemies
var enemy1 = world.Spawn("Goblin")
    .WithPosition(200, 100)
    .WithHealth(30, 30)
    .Build();

_ = world.Spawn("Orc")
    .WithPosition(250, 100)
    .WithHealth(50, 50)
    .Build();

// Simulate combat - change health via Set() to trigger events
Console.WriteLine("\nSimulating combat:");

// Player attacks goblin
ref readonly var weaponDamage = ref world.Get<Damage>(weapon);
var goblinHealth = world.Get<Health>(enemy1);
goblinHealth.Current -= weaponDamage.Amount;
world.Set(enemy1, goblinHealth);  // Triggers OnComponentChanged

// Publish custom damage event
world.Events.Publish(new DamageEvent(enemy1, weaponDamage.Amount, player));

// Check if goblin died
if (goblinHealth.Current <= 0)
{
    world.Events.Publish(new DeathEvent(enemy1, "slain by player"));
    world.Despawn(enemy1);  // Triggers OnEntityDestroyed
}

Console.WriteLine($"\nEvent statistics:");
Console.WriteLine($"  Entities created: {createdCount}");
Console.WriteLine($"  Entities destroyed: {destroyedCount}");
Console.WriteLine($"  Damage events: {damageEventCount}");

// Cleanup subscriptions
createdSub.Dispose();
destroyedSub.Dispose();
healthAddedSub.Dispose();
healthChangedSub.Dispose();
damageSub.Dispose();
deathSub.Dispose();

// =============================================================================
// PART 3: Change Tracking
// =============================================================================

Console.WriteLine("\n[3] Change Tracking Demo\n");

// Enable auto-tracking for Position
world.EnableAutoTracking<Position>();
Console.WriteLine("Auto-tracking enabled for Position component");

// Create more entities for tracking demo
var npc1 = world.Spawn("NPC1")
    .WithPosition(50, 50)
    .Build();

var npc2 = world.Spawn("NPC2")
    .WithPosition(60, 60)
    .Build();

var npc3 = world.Spawn("NPC3")
    .WithPosition(70, 70)
    .Build();

// Modify some positions using Set() (auto-tracked)
Console.WriteLine("\nModifying positions (auto-tracked via Set):");
world.Set(npc1, new Position { X = 55, Y = 55 });
world.Set(npc2, new Position { X = 65, Y = 65 });

Console.WriteLine($"  Dirty entities for Position: {world.GetDirtyCount<Position>()}");

// Manually mark player as dirty
world.MarkDirty<Position>(player);
Console.WriteLine($"  After manual mark on player: {world.GetDirtyCount<Position>()}");

// Process dirty entities (simulating network sync)
Console.WriteLine("\nProcessing dirty entities (network sync simulation):");
foreach (var entity in world.GetDirtyEntities<Position>())
{
    var name = world.GetName(entity) ?? "unnamed";
    ref readonly var pos = ref world.Get<Position>(entity);
    Console.WriteLine($"  Syncing {name} position: ({pos.X}, {pos.Y})");
}

// Clear dirty flags after processing
world.ClearDirtyFlags<Position>();
Console.WriteLine($"\nAfter clearing: {world.GetDirtyCount<Position>()} dirty entities");

// Check individual entity dirty status
Console.WriteLine($"Is NPC1 dirty? {world.IsDirty<Position>(npc1)}");

// Modify via ref (not auto-tracked) - must manually mark
Console.WriteLine("\nModifying via ref (requires manual marking):");
ref var npc3Pos = ref world.Get<Position>(npc3);
npc3Pos.X = 75;
npc3Pos.Y = 75;
// Not dirty yet - need to mark manually
Console.WriteLine($"  NPC3 dirty after ref modify? {world.IsDirty<Position>(npc3)}");
world.MarkDirty<Position>(npc3);
Console.WriteLine($"  NPC3 dirty after manual mark? {world.IsDirty<Position>(npc3)}");

// Disable auto-tracking
world.DisableAutoTracking<Position>();
Console.WriteLine($"\nAuto-tracking enabled? {world.IsAutoTrackingEnabled<Position>()}");

// =============================================================================
// PART 4: Cascading Despawn
// =============================================================================

Console.WriteLine("\n[4] Cascading Despawn Demo\n");

// Show current hierarchy
Console.WriteLine("Before DespawnRecursive:");
Console.WriteLine($"  Scene entities: {world.ArchetypeManager.EntityCount}");
Console.WriteLine($"  Player children: {world.GetChildren(player).Count()}");

// Despawn player and all children (weapon, shield)
Console.WriteLine("\nDespawning player recursively...");
var despawnedCount = world.DespawnRecursive(player);
Console.WriteLine($"  Entities despawned: {despawnedCount}");

Console.WriteLine($"\nAfter DespawnRecursive:");
Console.WriteLine($"  Scene entities: {world.ArchetypeManager.EntityCount}");
Console.WriteLine($"  Is player alive? {world.IsAlive(player)}");
Console.WriteLine($"  Is weapon alive? {world.IsAlive(weapon)}");
Console.WriteLine($"  Is shield alive? {world.IsAlive(shield)}");
Console.WriteLine($"  Is scene root alive? {world.IsAlive(sceneRoot)}");

// =============================================================================
// Summary
// =============================================================================

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("v0.3.0 Features Summary:");
Console.WriteLine("  - Entity Hierarchy: SetParent, GetChildren, GetDescendants, DespawnRecursive");
Console.WriteLine("  - Events: OnEntityCreated, OnComponentChanged, custom EventBus");
Console.WriteLine("  - Change Tracking: MarkDirty, GetDirtyEntities, EnableAutoTracking");
Console.WriteLine("\nDemo complete!");
