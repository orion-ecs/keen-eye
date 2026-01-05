using KeenEyes;
using KeenEyes.Generated;
using KeenEyes.Sample.SchemaMigration;
using KeenEyes.Serialization;

// =============================================================================
// KEEN EYES ECS - Schema Migration Demo
// =============================================================================
// This sample demonstrates:
// 1. Loading save files from older component versions (v1, v2)
// 2. Automatic migration chains (v1 → v2 → v3)
// 3. Using [DefaultValue] for auto-generated migrations
// 4. Explicit [MigrateFrom] migrations for complex transformations
// 5. Migration diagnostics and troubleshooting
// =============================================================================

Console.WriteLine("KeenEyes ECS - Schema Migration Demo");
Console.WriteLine(new string('=', 60));

// =============================================================================
// PART 1: Show Current Component Versions
// =============================================================================

Console.WriteLine("\n[1] Current Component Versions\n");

// The source generator creates ComponentMigrationMetadata with version info
Console.WriteLine("Component versions (from source generator):");
Console.WriteLine($"  PlayerStats: v{ComponentMigrationMetadata.GetVersion<PlayerStats>()}");
Console.WriteLine($"  Equipment:   v{ComponentMigrationMetadata.GetVersion<Equipment>()}");
Console.WriteLine($"  Currency:    v{ComponentMigrationMetadata.GetVersion<Currency>()}");
Console.WriteLine($"  Position:    v{ComponentMigrationMetadata.GetVersion<Position>()}");

// =============================================================================
// PART 2: Load a v1 Save File
// =============================================================================

Console.WriteLine("\n[2] Loading v1 Save File\n");

var v1SavePath = Path.Combine(AppContext.BaseDirectory, "TestData", "save_v1.json");
if (!File.Exists(v1SavePath))
{
    Console.WriteLine($"  Creating v1 test save file: {v1SavePath}");
    CreateV1SaveFile(v1SavePath);
}

Console.WriteLine($"  Loading: {v1SavePath}");
var v1Json = File.ReadAllText(v1SavePath);
Console.WriteLine($"  Size: {v1Json.Length} characters");

// Parse the snapshot
var v1Snapshot = SnapshotManager.FromJson(v1Json);
if (v1Snapshot == null)
{
    Console.WriteLine("  ERROR: Failed to parse v1 snapshot");
    return;
}

Console.WriteLine($"  Entities in save: {v1Snapshot.Entities.Count}");
Console.WriteLine($"  Timestamp: {v1Snapshot.Timestamp}");

// Show what versions are in the save
Console.WriteLine("\n  Component versions in save file:");
foreach (var entity in v1Snapshot.Entities)
{
    foreach (var comp in entity.Components)
    {
        var shortName = comp.TypeName.Split('.').Last().Split(',').First();
        Console.WriteLine($"    {entity.Name ?? $"Entity{entity.Id}"}.{shortName}: v{comp.Version}");
    }
}

// Restore to world - migrations run automatically!
using var world1 = new World();
Console.WriteLine("\n  Restoring to world (migrations run automatically)...");
var entityMap1 = SnapshotManager.RestoreSnapshot(world1, v1Snapshot, ComponentSerializer.Instance);
Console.WriteLine($"  Restored {entityMap1.Count} entities");

// Verify migrated data
var player1 = world1.GetEntityByName("Hero");
if (player1.IsValid)
{
    var stats = world1.Get<PlayerStats>(player1);
    var equip = world1.Get<Equipment>(player1);
    var currency = world1.Get<Currency>(player1);

    Console.WriteLine("\n  Migrated Player Data:");
    Console.WriteLine($"    PlayerStats: Health={stats.Health}, MaxHealth={stats.MaxHealth}, " +
                     $"Shield={stats.Shield} (added), Armor={stats.Armor} (added), MagicResist={stats.MagicResist} (added)");
    Console.WriteLine($"    Equipment: {equip.WeaponType}, Damage={equip.Damage}, " +
                     $"Durability={equip.Durability} (added), IsMagical={equip.IsMagical} (added)");
    Console.WriteLine($"    Currency: Gold={currency.Gold}, Silver={currency.Silver} (added), Gems={currency.Gems} (added)");
}

// =============================================================================
// PART 3: Load a v2 Save File
// =============================================================================

Console.WriteLine("\n[3] Loading v2 Save File\n");

var v2SavePath = Path.Combine(AppContext.BaseDirectory, "TestData", "save_v2.json");
if (!File.Exists(v2SavePath))
{
    Console.WriteLine($"  Creating v2 test save file: {v2SavePath}");
    CreateV2SaveFile(v2SavePath);
}

Console.WriteLine($"  Loading: {v2SavePath}");
var v2Json = File.ReadAllText(v2SavePath);
var v2Snapshot = SnapshotManager.FromJson(v2Json)!;

// Show what versions are in the save
Console.WriteLine("\n  Component versions in save file:");
foreach (var entity in v2Snapshot.Entities)
{
    foreach (var comp in entity.Components)
    {
        var shortName = comp.TypeName.Split('.').Last().Split(',').First();
        Console.WriteLine($"    {entity.Name ?? $"Entity{entity.Id}"}.{shortName}: v{comp.Version}");
    }
}

// Restore to world
using var world2 = new World();
Console.WriteLine("\n  Restoring to world (v2 → v3 migrations run)...");
var entityMap2 = SnapshotManager.RestoreSnapshot(world2, v2Snapshot, ComponentSerializer.Instance);

var player2 = world2.GetEntityByName("Mage");
if (player2.IsValid)
{
    var stats = world2.Get<PlayerStats>(player2);
    var equip = world2.Get<Equipment>(player2);
    var currency = world2.Get<Currency>(player2);

    Console.WriteLine("\n  Migrated Mage Data:");
    Console.WriteLine($"    PlayerStats: Health={stats.Health}, Shield={stats.Shield} (preserved), " +
                     $"Armor={stats.Armor} (added), MagicResist={stats.MagicResist} (added)");
    Console.WriteLine($"    Equipment: {equip.WeaponType}, Damage={equip.Damage} (preserved), " +
                     $"IsMagical={equip.IsMagical} (preserved), EnchantmentLevel={equip.EnchantmentLevel} (computed)");
    Console.WriteLine($"    Currency: Gold={currency.Gold}, Silver={currency.Silver} (preserved), " +
                     $"Gems={currency.Gems} (added via [DefaultValue])");
}

// =============================================================================
// PART 4: Migration Diagnostics
// =============================================================================

Console.WriteLine("\n[4] Migration Diagnostics\n");

var diagnostics = ComponentSerializer.Instance as IMigrationDiagnostics;
if (diagnostics != null)
{
    Console.WriteLine("  Components with migrations:");
    foreach (var component in diagnostics.GetComponentsWithMigrations())
    {
        var shortName = component.Split('.').Last();
        var versions = diagnostics.GetMigrationVersions(component);
        Console.WriteLine($"    {shortName}: migrates from versions [{string.Join(", ", versions)}]");
    }

    // Get migration graph for PlayerStats
    var graph = diagnostics.GetMigrationGraph(typeof(PlayerStats));
    if (graph != null)
    {
        Console.WriteLine($"\n  Migration Graph for PlayerStats:");
        Console.WriteLine($"    Current version: {graph.CurrentVersion}");
        Console.WriteLine($"    Edge count: {graph.EdgeCount}");
        Console.WriteLine($"    Source versions: {string.Join(", ", graph.SourceVersions)}");

        // Check for gaps
        var gaps = graph.FindGaps();
        if (gaps.Count > 0)
        {
            Console.WriteLine($"    GAPS: {string.Join(", ", gaps)} (would prevent loading old saves!)");
        }
        else
        {
            Console.WriteLine("    No gaps - all versions can be migrated");
        }
    }

    // Check for any gaps across all components
    Console.WriteLine("\n  Checking all components for migration gaps...");
    var allGaps = diagnostics.FindAllMigrationGaps();
    if (allGaps.Count == 0)
    {
        Console.WriteLine("    All migration chains are complete!");
    }
    else
    {
        foreach (var (component, gapVersions) in allGaps)
        {
            Console.WriteLine($"    WARNING: {component} missing migrations from: {string.Join(", ", gapVersions)}");
        }
    }
}
else
{
    Console.WriteLine("  Migration diagnostics not available.");
}

// =============================================================================
// PART 5: Create and Save Current Version
// =============================================================================

Console.WriteLine("\n[5] Creating and Saving Current Version (v3)\n");

using var world3 = new World();

var warrior = world3.Spawn("Warrior")
    .WithPosition(x: 100, y: 200)
    .WithPlayerStats(health: 150, maxHealth: 150, shield: 50, armor: 20, magicResist: 10)
    .WithEquipment(weaponType: "Greatsword", damage: 45, durability: 95, isMagical: true,
                   bonusDamage: 15, enchantmentLevel: 3)
    .WithCurrency(gold: 5000, silver: 2500, gems: 10)
    .WithPlayer()
    .Build();

Console.WriteLine($"  Created: {warrior} (Warrior)");

var snapshot3 = SnapshotManager.CreateSnapshot(world3, ComponentSerializer.Instance,
    new Dictionary<string, object>
    {
        ["Version"] = "3.0.0",
        ["SaveName"] = "v3 Save Demo"
    });

var v3SavePath = Path.Combine(AppContext.BaseDirectory, "TestData", "save_v3.json");
var v3Json = SnapshotManager.ToJson(snapshot3);
File.WriteAllText(v3SavePath, v3Json);
Console.WriteLine($"  Saved to: {v3SavePath}");
Console.WriteLine($"  Size: {v3Json.Length} characters");

// Show the component versions in the new save
Console.WriteLine("\n  Component versions in new save:");
foreach (var entity in snapshot3.Entities)
{
    foreach (var comp in entity.Components)
    {
        var shortName = comp.TypeName.Split('.').Last().Split(',').First();
        Console.WriteLine($"    {entity.Name ?? $"Entity{entity.Id}"}.{shortName}: v{comp.Version}");
    }
}

// =============================================================================
// PART 6: Summary
// =============================================================================

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("Migration Demo Complete!");
Console.WriteLine(new string('=', 60));
Console.WriteLine(@"
Key Takeaways:
1. Old saves (v1, v2) loaded successfully into current (v3) world
2. Migrations ran automatically during RestoreSnapshot
3. [MigrateFrom] handles complex transformations (field renames, computed values)
4. [DefaultValue] auto-generates simple field additions
5. IMigrationDiagnostics helps debug migration issues

Test Data Files Created:
  - TestData/save_v1.json  (PlayerStats v1, Equipment v1, Currency v1)
  - TestData/save_v2.json  (PlayerStats v2, Equipment v2, Currency v2)
  - TestData/save_v3.json  (PlayerStats v3, Equipment v3, Currency v3)
");

// =============================================================================
// Helper Functions to Create Test Save Files
// =============================================================================

// Creates a v1 save file simulating an old save before any schema updates.
static void CreateV1SaveFile(string path)
{
    // This JSON represents what a save file looked like at v1
    // - PlayerStats: only Health and MaxHealth
    // - Equipment: only WeaponType and Damage
    // - Currency: only Gold
    var v1Json = """
    {
      "version": 1,
      "timestamp": "2025-01-01T12:00:00Z",
      "entities": [
        {
          "id": 1,
          "name": "Hero",
          "components": [
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.PlayerStats",
              "version": 1,
              "isTag": false,
              "data": {
                "health": 80,
                "maxHealth": 100
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Equipment",
              "version": 1,
              "isTag": false,
              "data": {
                "weaponType": "Sword",
                "damage": 25
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Currency",
              "version": 1,
              "isTag": false,
              "data": {
                "gold": 1000
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Position",
              "version": 1,
              "isTag": false,
              "data": {
                "x": 50.0,
                "y": 75.0
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Player",
              "version": 1,
              "isTag": true,
              "data": {}
            }
          ]
        }
      ],
      "singletons": [],
      "metadata": {
        "SaveName": "Original v1 Save",
        "GameVersion": "1.0.0"
      }
    }
    """;

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, v1Json);
}

// Creates a v2 save file simulating a save from the first schema update.
static void CreateV2SaveFile(string path)
{
    // This JSON represents what a save file looked like at v2
    // - PlayerStats: Health, MaxHealth, Shield
    // - Equipment: WeaponType, Damage, Durability, IsMagical
    // - Currency: Gold, Silver
    var v2Json = """
    {
      "version": 1,
      "timestamp": "2025-06-01T12:00:00Z",
      "entities": [
        {
          "id": 1,
          "name": "Mage",
          "components": [
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.PlayerStats",
              "version": 2,
              "isTag": false,
              "data": {
                "health": 60,
                "maxHealth": 80,
                "shield": 40
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Equipment",
              "version": 2,
              "isTag": false,
              "data": {
                "weaponType": "Staff",
                "damage": 15,
                "durability": 85,
                "isMagical": true
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Currency",
              "version": 2,
              "isTag": false,
              "data": {
                "gold": 2500,
                "silver": 500
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Position",
              "version": 1,
              "isTag": false,
              "data": {
                "x": 150.0,
                "y": 200.0
              }
            },
            {
              "typeName": "KeenEyes.Sample.SchemaMigration.Player",
              "version": 1,
              "isTag": true,
              "data": {}
            }
          ]
        }
      ],
      "singletons": [],
      "metadata": {
        "SaveName": "v2 Save with Shield",
        "GameVersion": "2.0.0"
      }
    }
    """;

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, v2Json);
}
