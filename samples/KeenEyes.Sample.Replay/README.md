# KeenEyes.Sample.Replay

This sample demonstrates the **KeenEyes.Replay** recording system, showing how to capture, save, and analyze gameplay for debugging, replays, and crash reports.

## Features Demonstrated

1. **Basic Recording Setup** - Installing the replay plugin with custom options
2. **Recording Gameplay** - Capturing frames, entities, and game events
3. **Custom Events** - Recording game-specific events (player shots, power-ups, etc.)
4. **Analyzing Events** - Inspecting recorded data programmatically
5. **File I/O** - Saving/loading replays with compression options
6. **Ring Buffer Mode** - Continuous recording for crash replays
7. **Snapshot Seeking** - Efficient seeking to arbitrary points in a replay

## Running the Sample

```bash
dotnet run --project samples/KeenEyes.Sample.Replay
```

## Key Concepts

### Recording Setup

```csharp
var replayOptions = new ReplayOptions
{
    SnapshotInterval = TimeSpan.FromSeconds(0.5),
    RecordSystemEvents = true,
    RecordEntityEvents = true,
    RecordComponentEvents = true
};

var replayPlugin = new ReplayPlugin(ComponentSerializer.Instance, replayOptions);
world.InstallPlugin(replayPlugin);
```

### Starting/Stopping Recording

```csharp
var recorder = world.GetExtension<ReplayRecorder>();

recorder.StartRecording("Session Name", metadata);
// ... run game loop with world.Update() ...
var replayData = recorder.StopRecording();
```

### Custom Events

```csharp
recorder.RecordCustomEvent("PlayerShot", new Dictionary<string, object>
{
    ["projectileId"] = projectile.Id,
    ["fromX"] = position.X,
    ["fromY"] = position.Y
});
```

### File Operations

```csharp
// Save with compression
ReplayFileFormat.WriteToFile("replay.kreplay", replayData);

// Load metadata only (fast)
var info = ReplayFileFormat.ReadMetadataFromFile("replay.kreplay");

// Load full data
var (fileInfo, data) = ReplayFileFormat.ReadFromFile("replay.kreplay");
```

### Ring Buffer for Crash Replays

```csharp
var crashOptions = new ReplayOptions
{
    MaxFrames = 30,        // Keep only last 30 frames
    UseRingBuffer = true,  // Overwrite old frames
    RecordSystemEvents = false  // Minimize overhead
};
```

## Output

Running the sample produces output showing:
- Frame and snapshot counts during recording
- Event type distribution analysis
- Compression ratio comparisons (None vs GZip vs Brotli)
- Ring buffer behavior with frame retention
- Snapshot-based seeking calculations

## File Format

The `.kreplay` file format includes:
- **Magic number**: `KRPL` for identification
- **Version**: Format version for compatibility
- **Metadata**: Name, duration, frame count, etc.
- **Compression**: Optional GZip or Brotli compression
- **Checksum**: CRC32 for data integrity validation

## Use Cases

- **Debugging**: Record gameplay leading up to a bug
- **Crash Reports**: Ring buffer captures last N frames before crash
- **Replay System**: Save/load full gameplay recordings
- **Spectator Mode**: Stream replay data for observers
- **Testing**: Record and replay for deterministic testing
