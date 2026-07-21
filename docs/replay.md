# Replay Recording & Playback

The `KeenEyes.Replay` library records gameplay sessions frame-by-frame and plays them back later, enabling crash reproduction, killcams, demo/attract modes, and racing-style ghosts.

## Overview

Replay recording and playback are split across two plugins:

- **`ReplayPlugin`** installs a `ReplayRecorder` that captures frame boundaries, system execution, entity lifecycle events, and periodic world snapshots while the world runs.
- **`ReplayPlaybackPlugin`** installs a `ReplayPlayer` that loads previously recorded data and drives play/pause/stop/step/seek controls.

A single `World` can have one or the other installed, but not both — `ReplayPlaybackPlugin.Install` throws `InvalidOperationException` if a `ReplayRecorder` extension is already present, since "a world can either record or play back replays, but not both simultaneously."

Recorded sessions are serialized to the `.kreplay` binary container format via `ReplayFileFormat`, which supports GZip/Brotli compression and an optional CRC32 checksum. A lightweight companion feature, **Ghost replays** (`KeenEyes.Replay.Ghost`), extracts just the transform data for one entity out of a full replay for cheap racing-ghost style playback.

A runnable walkthrough of most of the features below lives in `samples/KeenEyes.Sample.Replay`.

## Quick Start

### Installation — Recording

```csharp
using KeenEyes;
using KeenEyes.Generated;
using KeenEyes.Replay;

using var world = new World();

// ReplayPlugin needs a component serializer to build world snapshots.
// The source generator produces a `ComponentSerializer` with a singleton `Instance`.
var replayOptions = new ReplayOptions
{
    SnapshotInterval = TimeSpan.FromSeconds(0.5),
    RecordSystemEvents = true,
    RecordEntityEvents = true,
    RecordComponentEvents = true
};

world.InstallPlugin(new ReplayPlugin(ComponentSerializer.Instance, replayOptions));

// Get the recorder through the world's extension API
var recorder = world.GetExtension<ReplayRecorder>();

recorder.StartRecording("Tutorial Playthrough");

for (int frame = 0; frame < 60; frame++)
{
    world.Update(1f / 60f); // frame boundaries are detected automatically
}

var replayData = recorder.StopRecording();
Console.WriteLine($"Recorded {replayData!.FrameCount} frames over {replayData.Duration.TotalSeconds:F2}s");
```

`ReplayPlugin` requires a concrete `World` (not a mock `IWorld`) and an `ISystemHookCapability` on the plugin context, both of which are present on a normal `World`. On install it registers an internal `ReplayFrameEndSystem` at `SystemPhase.Update` with order `int.MaxValue`, so it always runs after your own systems and can reliably close out the frame.

### Installation — Playback

```csharp
using KeenEyes.Replay;

using var world = new World();
world.InstallPlugin(new ReplayPlaybackPlugin());

var player = world.GetExtension<ReplayPlayer>();
player.LoadReplay("recording.kreplay");
player.Play();

while (player.State == PlaybackState.Playing)
{
    player.Update(deltaTime);

    var frame = player.GetCurrentFrame();
    // ... inspect frame.Events / frame.InputEvents ...
}
```

`ReplayPlayer` can also be constructed and used standalone (without any plugin or `World`) — it is a pure playback engine with no ECS dependency of its own.

## Core Concepts

### ReplayOptions

`ReplayOptions` is an immutable record passed to `ReplayPlugin` that controls what gets recorded:

| Property | Default | Purpose |
|---|---|---|
| `SnapshotInterval` | 1 second | How often a full `WorldSnapshot` is captured for fast seeking. `TimeSpan.Zero` disables automatic snapshots. |
| `RecordSystemEvents` | `true` | Records `SystemStart`/`SystemEnd` events for each system execution. |
| `RecordEntityEvents` | `true` | Records `EntityCreated`/`EntityDestroyed` events. |
| `RecordComponentEvents` | `true` | Records `ComponentAdded`/`ComponentRemoved`/`ComponentChanged` events. |
| `SystemEventPhase` | `null` | Optional `SystemPhase` filter — when set, only that phase's systems are recorded. |
| `MaxFrames` / `MaxDuration` | `null` | Automatic recording limits. |
| `UseRingBuffer` | `false` | When combined with `MaxFrames`, overwrites the oldest frame instead of stopping — ideal for "last N seconds before a crash" buffers. |
| `DefaultRecordingName` | `null` | Fallback name used when `StartRecording` is called without one. |
| `RecordChecksums` | `false` | Calculates a `WorldChecksum` per frame/snapshot for desync detection (~1ms/frame cost). |

```csharp
var crashOptions = new ReplayOptions
{
    SnapshotInterval = TimeSpan.FromSeconds(1),
    MaxFrames = 30,
    UseRingBuffer = true,
    RecordSystemEvents = false
};

var crashPlugin = new ReplayPlugin(ComponentSerializer.Instance, crashOptions);
crashWorld.InstallPlugin(crashPlugin);
```

### ReplayRecorder

`ReplayRecorder` is the extension installed by `ReplayPlugin`. Beyond `StartRecording`/`StopRecording`/`CancelRecording`, it exposes:

- `RecordEvent(ReplayEvent)` and `RecordCustomEvent(string customType, IReadOnlyDictionary<string, object>? data = null)` for application-defined events.
- `CaptureSnapshot()` to force an out-of-band snapshot (e.g. on a significant game event) in addition to the automatic interval.
- `IsRecording`, `CurrentFrameNumber`, `ElapsedTime`, `RecordedFrameCount`, `SnapshotCount` for status reporting.
- The `IInputRecorder` interface (`RecordKeyDown`, `RecordMouseMove`, `RecordGamepadAxis`, `RecordCustomInput<T>`, etc.) for capturing per-frame input alongside world events.

```csharp
recorder.RecordCustomEvent("PlayerShot", new Dictionary<string, object>
{
    ["projectileId"] = projectile.Id,
    ["fromX"] = playerPos.X,
    ["fromY"] = playerPos.Y
});

recorder.RecordKeyDown("Space");
```

### ReplayData, ReplayFrame, and ReplayEvent

`StopRecording()` returns a `ReplayData` record — the root container for a recording:

- `Frames`: an `IReadOnlyList<ReplayFrame>`, each with `FrameNumber`, `DeltaTime`, `ElapsedTime`, `Events`, `InputEvents`, an optional `PrecedingSnapshotIndex`, and an optional `Checksum`.
- `Snapshots`: an `IReadOnlyList<SnapshotMarker>`, each pairing a `FrameNumber`/`ElapsedTime` with a full `WorldSnapshot` (and optional `Checksum`) for restoration.
- `Name`, `RecordingStarted`, `RecordingEnded`, `Duration`, `FrameCount`, `Metadata`, and the computed `AverageFrameRate`.

Each `ReplayEvent` has a `Type` (`ReplayEventType`: `Custom`, `FrameStart`, `FrameEnd`, `SystemStart`, `SystemEnd`, `EntityCreated`, `EntityDestroyed`, `ComponentAdded`, `ComponentRemoved`, `ComponentChanged`, `Snapshot`), a `Timestamp` relative to the frame start, and optional `EntityId`/`SystemTypeName`/`ComponentTypeName`/`CustomType`/`Data` depending on the type.

```csharp
var eventCounts = new Dictionary<ReplayEventType, int>();
foreach (var frame in replayData.Frames)
{
    foreach (var evt in frame.Events)
    {
        eventCounts.TryGetValue(evt.Type, out var count);
        eventCounts[evt.Type] = count + 1;
    }
}
```

### Saving and loading — ReplayFileFormat

`ReplayFileFormat` reads and writes the `.kreplay` container: a header (magic bytes `"KRPL"`, version, flags), JSON-encoded `ReplayFileInfo` metadata, compressed `ReplayData`, and an optional CRC32 checksum.

```csharp
using KeenEyes.Replay;

// Save
ReplayFileFormat.WriteToFile("replay_demo.kreplay", replayData);

// Compare compression modes
var noCompression = ReplayFileFormat.Write(replayData, new ReplayFileOptions { Compression = CompressionMode.None });
var gzip = ReplayFileFormat.Write(replayData, new ReplayFileOptions { Compression = CompressionMode.GZip });
var brotli = ReplayFileFormat.Write(replayData, new ReplayFileOptions { Compression = CompressionMode.Brotli });

// Read just the metadata (fast — doesn't decompress/deserialize frame data)
var info = ReplayFileFormat.ReadMetadataFromFile("replay_demo.kreplay");
Console.WriteLine($"{info.Name}: {info.FrameCount} frames, {info.CompressionRatio:P1} of original size");

// Read the full replay
var (fileInfo, loadedData) = ReplayFileFormat.ReadFromFile("replay_demo.kreplay");
```

`ReplayFileOptions` controls `Compression` (`CompressionMode.None`/`GZip`/`Brotli`, default `GZip`), `CompressionLevel`, and `IncludeChecksum` (default `true`).

### ReplayPlayer

`ReplayPlayer` (installed as an extension by `ReplayPlaybackPlugin`, or constructed directly) loads a replay via `LoadReplay(string path, bool validateChecksum = true)`, `LoadReplay(Stream, bool)`, `LoadReplay(byte[], bool)`, or `LoadReplay(ReplayData)`, then exposes:

- Transport controls: `Play()`, `Pause()`, `Stop()`, `Step(int frames = 1)`.
- Timeline navigation: `SeekToFrame(int)`, `SeekToTime(TimeSpan)`, `GetNearestSnapshot(int targetFrame)` (binary search for the nearest preceding `SnapshotMarker`).
- Status: `State` (`PlaybackState.Stopped`/`Playing`/`Paused`), `CurrentFrame`, `TotalFrames`, `CurrentTime`, `TotalDuration`, `IsLoaded`, `LoadedReplay`, `FileInfo`.
- `PlaybackSpeed` — a `float` clamped to `PlaybackSpeeds.MinSpeed`..`PlaybackSpeeds.MaxSpeed` (0.25x–4x); `PlaybackSpeeds` also defines `QuarterSpeed`, `HalfSpeed`, `NormalSpeed`, `DoubleSpeed`, `QuadrupleSpeed` constants.
- Events: `PlaybackStarted`, `PlaybackPaused`, `PlaybackStopped`, `PlaybackEnded`, `FrameChanged`, `DesyncDetected`.

`Update(float deltaTime)` accumulates time scaled by `PlaybackSpeed` and advances `CurrentFrame` accordingly, firing `FrameChanged` for each frame that advances and `PlaybackEnded` (transitioning to `PlaybackState.Stopped`) when the replay completes.

```csharp
var player = new ReplayPlayer();
player.LoadReplay("recording.kreplay");
player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;

player.FrameChanged += frame => Console.WriteLine($"Now at frame {frame}");
player.PlaybackEnded += () => Console.WriteLine("Replay finished");

player.Play();
```

### Replaying input

Register handlers with `RegisterInputHandler(InputEventType, Action<InputEvent>)` for built-in input types, or `RegisterInputHandler<T>(string customType, Action<T> handler)` for `InputEventType.Custom` events, then call `ApplyInputFrame()` after each `Update` that advances a frame:

```csharp
player.RegisterInputHandler(InputEventType.KeyDown, input =>
    inputSystem.SimulateKeyDown(input.Key));

player.Play();

while (player.State == PlaybackState.Playing)
{
    if (player.Update(deltaTime))
    {
        player.ApplyInputFrame();
    }
}
```

### Desync detection

When `ReplayOptions.RecordChecksums` was enabled during recording, `WorldChecksum.Calculate(World, IComponentSerializer)` (an FNV-1a hash over entity IDs/versions, component data, and singletons in deterministic order) is stored per frame and snapshot. During playback, call `player.SetValidationContext(world, serializer)` and either poll `player.ValidateCurrentFrame()` or set `player.AutoValidate = true` to have `Update` validate automatically. A mismatch raises `DesyncDetected` with a `ReplayDesyncException` carrying `Frame`, `ExpectedChecksum`, and `ActualChecksum`.

```csharp
player.SetValidationContext(world, ComponentSerializer.Instance);
player.AutoValidate = true;
player.DesyncDetected += ex =>
    Console.WriteLine($"Desync at frame {ex.Frame}: expected 0x{ex.ExpectedChecksum:X8}, got 0x{ex.ActualChecksum:X8}");
```

## Ghost Replays

`KeenEyes.Replay.Ghost` extracts a lightweight `GhostData` (just position/rotation/scale per frame, orders of magnitude smaller than a full `ReplayData`) from an existing recording using `GhostExtractor.ExtractGhost(ReplayData replay, string entityName)`. Ghosts are saved/loaded with `GhostFileFormat` (`.keghost` files) and played back independently with `GhostPlayer`, which supports four `GhostSyncMode` values (`TimeSynced`, `FrameSynced`, `DistanceSynced`, `Independent`) for racing-ghost style comparisons against live gameplay. `GhostManager` coordinates several simultaneous ghosts (e.g. "Personal Best" and "World Record") for rendering.

```csharp
using KeenEyes.Replay.Ghost;

var extractor = new GhostExtractor();
var ghostData = extractor.ExtractGhost(replayData, "Player");

if (ghostData is not null)
{
    GhostFileFormat.WriteToFile("personal_best.keghost", ghostData);
}

using var ghostPlayer = new GhostPlayer();
ghostPlayer.Load(ghostData!);
ghostPlayer.SyncMode = GhostSyncMode.TimeSynced;
ghostPlayer.Play();
```

## Performance

- Recording has effectively zero overhead when `IsRecording` is `false` — the plugin's system hooks and event subscriptions short-circuit immediately.
- `WorldChecksum.Calculate` targets sub-millisecond performance for typical world sizes; enabling `RecordChecksums` (recording) or `AutoValidate` (playback) adds roughly 1ms per frame, so enable it for determinism testing rather than always-on production recording.
- `ReplayOptions.SnapshotInterval` trades file size against seek speed: more frequent snapshots make `SeekToFrame`/`SeekToTime` cheaper (less forward replay needed after restoring the nearest snapshot) at the cost of a larger `.kreplay` file.
- `ReplayFileOptions.Compression` (`GZip` vs `Brotli`) trades save/load speed against file size; `CompressionMode.None` is useful for debugging the raw JSON payload.

## Next Steps

- [Plugins Guide](plugins.md) - How `IWorldPlugin`, `IPluginContext`, and the world extension API used by `ReplayPlugin`/`ReplayPlaybackPlugin` work
- [Systems Guide](systems.md) - System phases and ordering, relevant to the internal `ReplayFrameEndSystem` and `SystemEventPhase` filtering
- [Serialization Guide](serialization.md) - `IComponentSerializer`, `WorldSnapshot`, and `SnapshotManager`, which underpin replay snapshots
- `samples/KeenEyes.Sample.Replay` - Runnable sample covering recording, custom events, ring-buffer crash replay, file save/load, and snapshot-based seeking
- [ADR-014: Replay Playback Runtime and Editor Integration](adr/014-replay-playback-runtime-editor-integration.md) - Original design document (note: proposed/aspirational; some API shapes in the ADR differ from the shipped implementation described above)
