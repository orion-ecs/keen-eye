using KeenEyes;
using KeenEyes.Generated;
using KeenEyes.Sample.SaveLoad;
using KeenEyes.Serialization;

// =============================================================================
// KEEN EYES ECS - Save/Load Demo
// =============================================================================
// This sample demonstrates:
// 1. Creating world snapshots
// 2. Serializing snapshots to JSON
// 3. Restoring world state from snapshots
// 4. Using metadata for save game info
// 5. Entity ID mapping after restore
// 6. Undo/Redo system pattern
// =============================================================================

Console.WriteLine("KeenEyes ECS - Save/Load Demo");
Console.WriteLine(new string('=', 50));

// =============================================================================
// PART 1: Creating a Game World
// =============================================================================

Console.WriteLine("\n[1] Creating Initial Game World\n");

using var world = new World();

// Create player
var player = world.Spawn("Player")
    .WithPosition(x: 100, y: 50)
    .WithVelocity(x: 0, y: 0)
    .WithHealth(current: 100, max: 100)
    .WithExperience(level: 5, currentXP: 250, xpToNextLevel: 500)
    .WithInventory(slots: 20, usedSlots: 8)
    .WithGold(amount: 1500)
    .WithPlayer()
    .Build();

Console.WriteLine($"Created player: {player}");

// Create some enemies
for (int i = 0; i < 3; i++)
{
    var enemy = world.Spawn()
        .WithPosition(x: 200 + i * 50, y: 100)
        .WithHealth(current: 30 + i * 10, max: 50)
        .WithEnemy()
        .Build();
    Console.WriteLine($"Created enemy {i + 1}: {enemy}");
}

// Create NPCs
var merchant = world.Spawn("Merchant")
    .WithPosition(x: 50, y: 200)
    .WithNPC()
    .Build();

var questGiver = world.Spawn("QuestGiver")
    .WithPosition(x: 150, y: 200)
    .WithNPC()
    .Build();

Console.WriteLine($"Created NPCs: Merchant={merchant}, QuestGiver={questGiver}");

// Show initial state
PrintWorldState(world, "Initial State");

// =============================================================================
// PART 2: Creating a Snapshot
// =============================================================================

Console.WriteLine("\n[2] Creating Snapshot\n");

// Create snapshot with metadata
var metadata = new Dictionary<string, object>
{
    ["SaveSlot"] = 1,
    ["SaveName"] = "Autosave",
    ["PlayTime"] = "02:15:30",
    ["Location"] = "Starting Village",
    ["Chapter"] = 2
};

var snapshot = SnapshotManager.CreateSnapshot(world, ComponentSerializer.Instance, metadata);

Console.WriteLine($"Snapshot created:");
Console.WriteLine($"  Version: {snapshot.Version}");
Console.WriteLine($"  Timestamp: {snapshot.Timestamp}");
Console.WriteLine($"  Entities: {snapshot.Entities.Count}");
Console.WriteLine($"  Singletons: {snapshot.Singletons.Count}");
Console.WriteLine($"  Metadata keys: {string.Join(", ", snapshot.Metadata?.Keys ?? [])}");

// =============================================================================
// PART 3: Serializing to JSON
// =============================================================================

Console.WriteLine("\n[3] Serializing to JSON\n");

var json = SnapshotManager.ToJson(snapshot);

Console.WriteLine($"JSON size: {json.Length} characters");
Console.WriteLine($"First 500 chars:\n{json[..Math.Min(500, json.Length)]}...");

// In a real game, you would save this to a file:
// File.WriteAllText("save1.json", json);

// =============================================================================
// PART 4: Modifying the World (Simulate Gameplay)
// =============================================================================

Console.WriteLine("\n[4] Simulating Gameplay (Modifying World)\n");

// Player takes damage
ref var playerHealth = ref world.Get<Health>(player);
playerHealth.Current = 45;
Console.WriteLine($"Player took damage: {playerHealth.Current}/{playerHealth.Max}");

// Player gains XP
ref var playerXP = ref world.Get<Experience>(player);
playerXP.CurrentXP += 150;
playerXP.Level = 6;
Console.WriteLine($"Player leveled up to {playerXP.Level}!");

// Player moves
ref var playerPos = ref world.Get<Position>(player);
playerPos.X = 300;
playerPos.Y = 175;
Console.WriteLine($"Player moved to ({playerPos.X}, {playerPos.Y})");

// Spend gold
ref var playerGold = ref world.Get<Gold>(player);
playerGold.Amount -= 500;
Console.WriteLine($"Player spent gold, now has: {playerGold.Amount}");

// Kill an enemy
var firstEnemy = world.Query<Health>().With<Enemy>().First();
world.Despawn(firstEnemy);
Console.WriteLine($"Enemy {firstEnemy} was defeated!");

PrintWorldState(world, "After Gameplay");

// =============================================================================
// PART 5: Restoring from Snapshot
// =============================================================================

Console.WriteLine("\n[5] Restoring from Snapshot\n");

Console.WriteLine("Loading saved state...");

// Parse the JSON back to a snapshot
var loadedSnapshot = SnapshotManager.FromJson(json);
if (loadedSnapshot == null)
{
    Console.WriteLine("Failed to load snapshot!");
    return;
}

Console.WriteLine($"Loaded snapshot from: {loadedSnapshot.Timestamp}");
Console.WriteLine($"Save name: {loadedSnapshot.Metadata?["SaveName"]}");

// Restore to world (this clears existing state)
var entityMap = SnapshotManager.RestoreSnapshot(world, loadedSnapshot, ComponentSerializer.Instance);

Console.WriteLine($"\nRestored {entityMap.Count} entities");
Console.WriteLine("Entity ID mapping (old -> new):");
foreach (var (oldId, newEntity) in entityMap.Take(5))
{
    Console.WriteLine($"  {oldId} -> {newEntity}");
}

PrintWorldState(world, "After Restore");

// Find player again (ID may have changed)
var restoredPlayer = world.GetEntityByName("Player");
Console.WriteLine($"\nRestored player entity: {restoredPlayer}");

ref readonly var restoredHealth = ref world.Get<Health>(restoredPlayer);
ref readonly var restoredGold = ref world.Get<Gold>(restoredPlayer);
Console.WriteLine($"Player health: {restoredHealth.Current}/{restoredHealth.Max} (was 45 before restore)");
Console.WriteLine($"Player gold: {restoredGold.Amount} (was 1000 before restore)");

// =============================================================================
// PART 6: Undo/Redo Pattern
// =============================================================================

Console.WriteLine("\n[6] Undo/Redo Pattern Demo\n");

var undoStack = new Stack<WorldSnapshot>();
var redoStack = new Stack<WorldSnapshot>();

void SaveUndoState()
{
    undoStack.Push(SnapshotManager.CreateSnapshot(world, ComponentSerializer.Instance));
    redoStack.Clear();
    Console.WriteLine($"  [Saved undo state, stack size: {undoStack.Count}]");
}

bool Undo()
{
    if (undoStack.Count == 0)
    {
        return false;
    }

    redoStack.Push(SnapshotManager.CreateSnapshot(world, ComponentSerializer.Instance));
    SnapshotManager.RestoreSnapshot(world, undoStack.Pop(), ComponentSerializer.Instance);
    return true;
}

bool Redo()
{
    if (redoStack.Count == 0)
    {
        return false;
    }

    undoStack.Push(SnapshotManager.CreateSnapshot(world, ComponentSerializer.Instance));
    SnapshotManager.RestoreSnapshot(world, redoStack.Pop(), ComponentSerializer.Instance);
    return true;
}

// Initial state
Console.WriteLine("Initial player gold:");
var p = world.GetEntityByName("Player");
Console.WriteLine($"  Gold: {world.Get<Gold>(p).Amount}");

// Make changes and save undo points
SaveUndoState();
world.Get<Gold>(p).Amount += 100;
Console.WriteLine("Added 100 gold:");
Console.WriteLine($"  Gold: {world.Get<Gold>(p).Amount}");

SaveUndoState();
world.Get<Gold>(p).Amount += 200;
Console.WriteLine("Added 200 more gold:");
Console.WriteLine($"  Gold: {world.Get<Gold>(p).Amount}");

SaveUndoState();
world.Get<Gold>(p).Amount -= 50;
Console.WriteLine("Spent 50 gold:");
Console.WriteLine($"  Gold: {world.Get<Gold>(p).Amount}");

// Undo changes
Console.WriteLine("\nUndoing...");
Undo();
p = world.GetEntityByName("Player");
Console.WriteLine($"  Gold after undo: {world.Get<Gold>(p).Amount}");

Undo();
p = world.GetEntityByName("Player");
Console.WriteLine($"  Gold after undo: {world.Get<Gold>(p).Amount}");

// Redo
Console.WriteLine("\nRedoing...");
Redo();
p = world.GetEntityByName("Player");
Console.WriteLine($"  Gold after redo: {world.Get<Gold>(p).Amount}");

// =============================================================================
// PART 7: Multiple Save Slots
// =============================================================================

Console.WriteLine("\n[7] Multiple Save Slots Pattern\n");

var saveSlots = new Dictionary<int, string>();

void SaveToSlot(int slot, string saveName)
{
    var meta = new Dictionary<string, object>
    {
        ["SaveSlot"] = slot,
        ["SaveName"] = saveName,
        ["SaveTime"] = DateTimeOffset.UtcNow
    };
    var snap = SnapshotManager.CreateSnapshot(world, ComponentSerializer.Instance, meta);
    saveSlots[slot] = SnapshotManager.ToJson(snap);
    Console.WriteLine($"Saved to slot {slot}: '{saveName}'");
}

void LoadFromSlot(int slot)
{
    if (!saveSlots.TryGetValue(slot, out var slotJson))
    {
        Console.WriteLine($"Slot {slot} is empty!");
        return;
    }

    var snap = SnapshotManager.FromJson(slotJson);
    if (snap != null)
    {
        SnapshotManager.RestoreSnapshot(world, snap, ComponentSerializer.Instance);
        Console.WriteLine($"Loaded from slot {slot}: '{snap.Metadata?["SaveName"]}'");
    }
}

// Save to different slots
SaveToSlot(1, "Beginning of Chapter 2");
world.Get<Gold>(world.GetEntityByName("Player")).Amount = 9999;
SaveToSlot(2, "Rich Player Save");
SaveToSlot(3, "Quick Save");

Console.WriteLine($"\nAvailable saves: {saveSlots.Count}");

// Load from slot 1
Console.WriteLine("\nLoading slot 1...");
LoadFromSlot(1);
Console.WriteLine($"Gold: {world.Get<Gold>(world.GetEntityByName("Player")).Amount}");

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Save/Load demo complete!");

// =============================================================================
// Helper Methods
// =============================================================================

static void PrintWorldState(World world, string label)
{
    Console.WriteLine($"\n--- {label} ---");

    var playerCount = world.Query<Position>().With<Player>().Count();
    var enemyCount = world.Query<Position>().With<Enemy>().Count();
    var npcCount = world.Query<Position>().With<NPC>().Count();

    Console.WriteLine($"Entities: {playerCount} players, {enemyCount} enemies, {npcCount} NPCs");

    var player = world.GetEntityByName("Player");
    if (player.IsValid &&
        world.Has<Position>(player) &&
        world.Has<Health>(player) &&
        world.Has<Experience>(player) &&
        world.Has<Gold>(player))
    {
        ref readonly var pos = ref world.Get<Position>(player);
        ref readonly var health = ref world.Get<Health>(player);
        ref readonly var xp = ref world.Get<Experience>(player);
        ref readonly var gold = ref world.Get<Gold>(player);

        Console.WriteLine($"Player: Pos=({pos.X},{pos.Y}), HP={health.Current}/{health.Max}, Lv={xp.Level}, Gold={gold.Amount}");
    }
}
