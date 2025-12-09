# Serialization and Snapshots

KeenEyes provides a snapshot system for saving and restoring complete world state. This enables features like save games, level loading, undo systems, and multiplayer state synchronization.

## Basic Usage

### Creating a Snapshot

Capture the current state of a world:

```csharp
using KeenEyes.Serialization;

// Capture current world state
var snapshot = SnapshotManager.CreateSnapshot(world);
```

### Serializing to JSON

Convert a snapshot to a JSON string for storage:

```csharp
// Serialize to JSON
string json = SnapshotManager.ToJson(snapshot);

// Save to file
File.WriteAllText("save.json", json);
```

### Loading and Restoring

Load a snapshot and restore it to a world:

```csharp
// Load JSON from file
string json = File.ReadAllText("save.json");

// Deserialize snapshot
var snapshot = SnapshotManager.FromJson(json);

// Restore to world (clears existing state first)
var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot);
```

## What Gets Captured

A snapshot captures:

- **All entities** and their IDs
- **All components** attached to each entity
- **Entity names** (if assigned)
- **Parent-child hierarchy** relationships
- **World singletons**
- **Custom metadata** (optional)

## Entity ID Mapping

When restoring, entities get new IDs. The restore method returns a mapping from old to new IDs:

```csharp
var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot);

// Map from snapshot ID to new entity
foreach (var (oldId, newEntity) in entityMap)
{
    Console.WriteLine($"Entity {oldId} is now {newEntity}");
}
```

## Metadata

Include custom metadata in snapshots:

```csharp
var metadata = new Dictionary<string, object>
{
    ["SaveSlot"] = "slot1",
    ["PlayerName"] = "Hero",
    ["PlayTime"] = TimeSpan.FromHours(12.5),
    ["Chapter"] = 3
};

var snapshot = SnapshotManager.CreateSnapshot(world, metadata);

// Access metadata after loading
var loadedSnapshot = SnapshotManager.FromJson(json);
var playerName = loadedSnapshot.Metadata?["PlayerName"];
```

## Type Resolution

By default, `Type.GetType()` resolves component types from their assembly-qualified names. Provide a custom resolver for more control:

```csharp
var entityMap = SnapshotManager.RestoreSnapshot(
    world,
    snapshot,
    typeResolver: typeName =>
    {
        // Custom type resolution logic
        return typeName switch
        {
            "MyGame.OldPosition" => typeof(Position),  // Handle renamed types
            _ => Type.GetType(typeName)
        };
    });
```

## AOT Compatibility

For ahead-of-time compilation scenarios (iOS, WebAssembly, NativeAOT), provide a generated serializer:

```csharp
// Use generated serializer for AOT compatibility
var serializer = new ComponentSerializationRegistry();
var entityMap = SnapshotManager.RestoreSnapshot(
    world,
    snapshot,
    serializer: serializer);
```

To enable generated serializers, mark components with `Serializable = true`:

```csharp
[Component(Serializable = true)]
public partial struct Position
{
    public float X;
    public float Y;
}
```

The source generator creates a `ComponentSerializationRegistry` class that handles serialization without reflection.

## Custom JSON Options

Customize JSON serialization:

```csharp
var options = SnapshotManager.GetDefaultJsonOptions();
options.WriteIndented = false;  // Compact JSON
options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;

var json = SnapshotManager.ToJson(snapshot, options);
var loaded = SnapshotManager.FromJson(json, options);
```

Default options:
- `WriteIndented = true`
- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`
- `IncludeFields = true` (required for struct fields)

## Use Cases

### Save/Load System

```csharp
public class SaveSystem
{
    private readonly World world;
    private readonly string savePath;

    public SaveSystem(World world, string savePath)
    {
        this.world = world;
        this.savePath = savePath;
    }

    public void Save(int slot, string playerName)
    {
        var metadata = new Dictionary<string, object>
        {
            ["Slot"] = slot,
            ["PlayerName"] = playerName,
            ["SaveTime"] = DateTimeOffset.UtcNow
        };

        var snapshot = SnapshotManager.CreateSnapshot(world, metadata);
        var json = SnapshotManager.ToJson(snapshot);

        var path = Path.Combine(savePath, $"save_{slot}.json");
        File.WriteAllText(path, json);
    }

    public bool Load(int slot)
    {
        var path = Path.Combine(savePath, $"save_{slot}.json");
        if (!File.Exists(path))
        {
            return false;
        }

        var json = File.ReadAllText(path);
        var snapshot = SnapshotManager.FromJson(json);

        if (snapshot != null)
        {
            SnapshotManager.RestoreSnapshot(world, snapshot);
            return true;
        }

        return false;
    }
}
```

### Undo System

```csharp
public class UndoSystem
{
    private readonly World world;
    private readonly Stack<WorldSnapshot> undoStack = new();
    private readonly Stack<WorldSnapshot> redoStack = new();

    public UndoSystem(World world)
    {
        this.world = world;
    }

    public void SaveState()
    {
        undoStack.Push(SnapshotManager.CreateSnapshot(world));
        redoStack.Clear();
    }

    public bool Undo()
    {
        if (undoStack.Count == 0) return false;

        redoStack.Push(SnapshotManager.CreateSnapshot(world));
        var snapshot = undoStack.Pop();
        SnapshotManager.RestoreSnapshot(world, snapshot);
        return true;
    }

    public bool Redo()
    {
        if (redoStack.Count == 0) return false;

        undoStack.Push(SnapshotManager.CreateSnapshot(world));
        var snapshot = redoStack.Pop();
        SnapshotManager.RestoreSnapshot(world, snapshot);
        return true;
    }
}
```

### Level Transitions

```csharp
public class LevelManager
{
    private readonly Dictionary<string, WorldSnapshot> levelSnapshots = new();

    public void SaveLevelState(World world, string levelName)
    {
        levelSnapshots[levelName] = SnapshotManager.CreateSnapshot(world);
    }

    public void LoadLevel(World world, string levelName)
    {
        if (levelSnapshots.TryGetValue(levelName, out var snapshot))
        {
            SnapshotManager.RestoreSnapshot(world, snapshot);
        }
    }

    public void ExportLevel(string levelName, string filePath)
    {
        if (levelSnapshots.TryGetValue(levelName, out var snapshot))
        {
            var json = SnapshotManager.ToJson(snapshot);
            File.WriteAllText(filePath, json);
        }
    }
}
```

### Multiplayer State Sync

```csharp
public class NetworkSync
{
    public byte[] CreateStateUpdate(World world)
    {
        var snapshot = SnapshotManager.CreateSnapshot(world);
        var json = SnapshotManager.ToJson(snapshot);
        return Encoding.UTF8.GetBytes(json);
    }

    public void ApplyStateUpdate(World world, byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        var snapshot = SnapshotManager.FromJson(json);

        if (snapshot != null)
        {
            SnapshotManager.RestoreSnapshot(world, snapshot);
        }
    }
}
```

## WorldSnapshot Structure

The `WorldSnapshot` record contains:

```csharp
public sealed record WorldSnapshot
{
    public int Version { get; init; } = 1;              // Format version
    public DateTimeOffset Timestamp { get; init; }       // Creation time
    public IReadOnlyList<SerializedEntity> Entities { get; init; }
    public IReadOnlyList<SerializedSingleton> Singletons { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

## Performance Considerations

- **Snapshot creation**: O(E * C) where E = entities, C = average components per entity
- **Component boxing**: Components are boxed for serialization
- **JSON serialization**: Uses System.Text.Json for efficiency
- **Not for hot paths**: Designed for save/load, not real-time synchronization

For high-frequency state sync, consider delta compression or binary formats.

## Error Handling

```csharp
try
{
    var snapshot = SnapshotManager.FromJson(json);
    if (snapshot == null)
    {
        Console.WriteLine("Invalid snapshot data");
        return;
    }

    SnapshotManager.RestoreSnapshot(world, snapshot);
}
catch (JsonException ex)
{
    Console.WriteLine($"Failed to parse snapshot: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Failed to restore snapshot: {ex.Message}");
}
```

## Best Practices

1. **Save metadata** - Include save slot, timestamps, player info for UI
2. **Handle missing types** - Provide type resolvers for backwards compatibility
3. **Test restore paths** - Ensure snapshots from old versions still load
4. **Consider file format versioning** - The `Version` property enables migration
5. **Use AOT serializers** - Required for iOS/WASM, faster everywhere else
