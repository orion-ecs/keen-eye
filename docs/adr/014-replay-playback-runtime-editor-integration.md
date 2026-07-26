# ADR-014: Replay Playback Runtime and Editor Integration

**Status:** Amended
**Revision:** v3
**Implementation:** Partial
**First accepted:** 2026-01-02 · **Last amended:** 2026-07-26
**Relates to:** [ADR-001](001-world-manager-architecture.md) (World managers) · [ADR-007](007-capability-based-plugin-architecture.md) (plugin capabilities) · [#83](https://github.com/orion-ecs/keen-eye/issues/83) · [#84](https://github.com/orion-ecs/keen-eye/issues/84)

## Context

The replay recording system (#83) is complete. `ReplayRecorder`, `ReplayPlugin`, and the `.kreplay` file format capture:
- Frame-level events (entity spawns, component changes, system execution)
- Periodic world snapshots for fast seeking
- Compressed binary format with checksums

However, **playback infrastructure is missing**. There's no `ReplayPlayer` or mechanism to replay recorded sessions.

### Two Distinct Playback Contexts

Replay playback serves fundamentally different purposes in runtime vs. editor contexts:

| Context | Primary Use Cases | Characteristics |
|---------|------------------|-----------------|
| **Runtime (Full)** | Demo playback, killcams, tutorials, attract mode | Game owns update loop, real-time playback, minimal UI |
| **Runtime (Ghost)** | Racing ghosts, time trials, leaderboard replays | Parallel to live gameplay, single entity, visual-only |
| **Editor** | Debugging, QA reproduction, frame inspection | Editor owns update loop, stepping, timeline scrubbing, inspection |

These contexts have different requirements:
- **Runtime**: Optimized for performance, integrates with game loop
- **Editor**: Optimized for inspection, integrates with panels and debugging tools

### Current Editor Infrastructure

The editor already has relevant infrastructure:
- `PlayModeManager` with `Playing/Paused/Editing` states
- `SnapshotManager` for state capture/restore
- Shortcut stubs for frame stepping (`Ctrl+Alt+P`)
- Plugin hooks for play mode state changes

However, these are not integrated with replay data.

## Decision

Implement a layered architecture with a **core `ReplayPlayer`** that both runtime and editor integrate with differently.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                 ReplayPlayer (KeenEyes.Replay)               │
│  Core playback engine - no UI, no editor dependencies       │
│  - LoadReplay(path/stream/bytes/data)                       │
│  - Play/Pause/Stop/Step                                     │
│  - SeekToFrame/SeekToTime                                   │
│  - PlaybackSpeed (0.25x - 4x)                               │
│  - State: Playing/Paused/Stopped                            │
└──────────────────────────┬──────────────────────────────────┘
                           │
         ┌─────────────────┴─────────────────┐
         ▼                                   ▼
┌─────────────────────┐           ┌─────────────────────────┐
│   Runtime Usage     │           │   Editor Integration    │
│ (KeenEyes.Replay)   │           │ (KeenEyes.Editor)       │
│                     │           │                         │
│ ReplayPlaybackPlugin│           │ ReplayPlaybackMode      │
│ - Installs player   │           │ - Owns playback world   │
│ - Game calls Update │           │ - Frame inspector sync  │
└─────────────────────┘           └─────────────────────────┘
```

### Core API: ReplayPlayer

The shipped `ReplayPlayer` (`src/KeenEyes.Replay/ReplayPlayer.cs`) is **world-detached**: it is constructed with no arguments and does not own or mutate a world. Consumers pull frame data via `GetCurrentFrame()`/`GetFrame(int)`; a world and serializer are optionally attached via `SetValidationContext` for checksum validation and (with `EnableStateRestoration`) snapshot restoration. This diverges from the originally proposed world-owning constructor — the sketches below reflect the as-built API. `Unload()` shipped as `UnloadReplay()`, `StepBack()` shipped as `Step(int)` accepting negative counts, and the `EventHandler`-based events shipped as `Action`-based events.

```csharp
namespace KeenEyes.Replay;

/// <summary>
/// Core playback engine for replaying recorded sessions.
/// </summary>
public sealed class ReplayPlayer : IDisposable
{
    // Construction - the player is world-detached; no world required
    public ReplayPlayer();

    // Loading
    public void LoadReplay(string path, bool validateChecksum = true);
    public void LoadReplay(Stream stream, bool validateChecksum = true);
    public void LoadReplay(byte[] data, bool validateChecksum = true);
    public void LoadReplay(ReplayData replay);
    public void UnloadReplay();

    // Playback control
    public void Play();
    public void Pause();
    public void Stop();
    public void Step(int frames = 1);   // negative counts step backward

    // Timeline navigation
    public void SeekToFrame(int frameNumber);
    public void SeekToTime(TimeSpan time);
    public SnapshotMarker? GetNearestSnapshot(int targetFrame);

    // Speed control
    public float PlaybackSpeed { get; set; } // clamped 0.25x to 4x via PlaybackSpeeds

    // State
    public PlaybackState State { get; }
    public bool IsLoaded { get; }
    public int CurrentFrame { get; }
    public int TotalFrames { get; }
    public TimeSpan CurrentTime { get; }
    public TimeSpan TotalDuration { get; }

    // Frame advancement (called by game loop or editor)
    public bool Update(float deltaTime);

    // Frame data access - consumers read frames; the player never applies them
    public ReplayFrame? GetCurrentFrame();
    public ReplayFrame GetFrame(int frameIndex);

    // Optional world attachment for validation and snapshot restoration
    public void SetValidationContext(World world, IComponentSerializer serializer);
    public void ClearValidationContext();
    public bool AutoValidate { get; set; }
    public bool EnableStateRestoration { get; set; }
    public bool ValidateCurrentFrame();
    public bool ValidateDeterminism(int iterations = 3);

    // Events (Action-based)
    public event Action? PlaybackStarted;
    public event Action? PlaybackPaused;
    public event Action? PlaybackStopped;
    public event Action? PlaybackEnded;
    public event Action<int>? FrameChanged;
    public event Action<ReplayDesyncException>? DesyncDetected;
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}
```

### Runtime Integration: ReplayPlaybackPlugin

For games that want simple playback without editor. No serialization capability is required at install time, because the player does not apply state to the world:

```csharp
namespace KeenEyes.Replay;

/// <summary>
/// Plugin that enables replay playback in a world.
/// </summary>
public sealed class ReplayPlaybackPlugin : IWorldPlugin
{
    public string Name => "ReplayPlayback";

    private ReplayPlayer? player;

    public void Install(IPluginContext context)
    {
        // Mutual exclusion: a world cannot record and play back simultaneously
        if (context.TryGetExtension<ReplayRecorder>(out _))
        {
            throw new InvalidOperationException(
                "Cannot install ReplayPlaybackPlugin on a world that has ReplayPlugin installed.");
        }

        player = new ReplayPlayer();
        context.SetExtension(player);
    }

    public void Uninstall(IPluginContext context)
    {
        // Stops active playback, unloads the replay, disposes the player,
        // and removes the extension
    }
}
```

**Runtime usage:**
```csharp
// Setup
world.InstallPlugin(new ReplayPlaybackPlugin());
var player = world.GetExtension<ReplayPlayer>();
player.LoadReplay("demo.kreplay");
player.Play();

// In game loop
while (!gameQuit)
{
    if (player.State == PlaybackState.Playing)
    {
        player.Update(deltaTime);
    }
    renderer.Render(world);
}
```

### Runtime Integration: Ghost Mode

For racing/time-trial games that show a "ghost" of a previous run alongside live gameplay. Shipped in the `KeenEyes.Replay.Ghost` namespace (`src/KeenEyes.Replay/Ghost/`).

**Key differences from full replay:**
- Runs **in parallel** with live game, not instead of it
- Only tracks a **single entity** (player character/vehicle)
- **Visual-only** - no collision or physics interaction
- Supports **multiple simultaneous ghosts** (personal best, world record, friend)
- **Lightweight format** - KBs instead of MBs, persisted as `.keghost` files via `GhostFileFormat` (magic `KGHO`), separate from full `.kreplay` replays

**Not yet implemented:** the originally proposed `GhostRecorder` (direct lightweight recording without full replay overhead) was not built. Ghosts are produced only by **extracting** from full replays via `GhostExtractor` or loading previously saved `.keghost` files — a game that wants to capture a ghost must record a full replay first with `ReplayPlugin`.

```csharp
namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Lightweight ghost data extracted from a replay or loaded from a .keghost file.
/// </summary>
public sealed record GhostData
{
    public string? Name { get; init; }
    public string? EntityName { get; init; }
    public required DateTimeOffset RecordingStarted { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int FrameCount { get; init; }
    public required IReadOnlyList<GhostFrame> Frames { get; init; }
    public float TotalDistance { get; }
    public double AverageFrameRate { get; }
}

/// <summary>
/// Extracts ghost data from full replay files.
/// </summary>
public sealed class GhostExtractor
{
    public TimeSpan MinFrameInterval { get; set; }   // downsampling
    public bool CalculateDistance { get; set; }

    public GhostData? ExtractGhost(ReplayData replay, string entityName);
    public GhostData? ExtractGhostById(ReplayData replay, int entityId);
    public Dictionary<string, GhostData> ExtractAllGhosts(ReplayData replay);
}

/// <summary>
/// Plays back a ghost alongside live gameplay.
/// </summary>
public sealed class GhostPlayer : IDisposable
{
    public GhostPlayer();

    public void Load(GhostData ghost);
    public void LoadFromFile(string path, bool validateChecksum = true);
    public void Unload();

    public void Play();
    public void Pause();
    public void Stop();
    public bool Update(float deltaTime);
    public bool UpdateByDistance(float distance);
    public void SeekToTime(TimeSpan time);
    public void SeekToFrame(int frameNumber);

    public GhostPlaybackState State { get; }
    public GhostSyncMode SyncMode { get; set; }  // TimeSynced/FrameSynced/Independent/DistanceSynced
    public float PlaybackSpeed { get; set; }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }
    public float Distance { get; }
}

/// <summary>
/// Manages multiple ghosts for racing scenarios.
/// </summary>
public sealed class GhostManager : IDisposable
{
    public void AddGhost(string id, GhostData data, GhostVisualConfig? config = null);
    public void AddGhostFromFile(string id, string path, GhostVisualConfig? config = null);
    public bool RemoveGhost(string id);

    public void Update(float deltaTime);
    public void UpdateByDistance(float distance);
    public void PlayAll();
    public void PauseAll();
    public void StopAll();
    public void SeekAllToTime(TimeSpan time);
    public void SetAllSyncMode(GhostSyncMode syncMode);
    public void SetAllPlaybackSpeed(float speed);

    // GhostInstance bundles Player + Config (Position/Rotation/Opacity/TintColor/...)
    public IEnumerable<GhostInstance> ActiveGhosts { get; }
}
```

**Ghost mode usage:**
```csharp
// Extract ghost from an existing replay
var (_, replay) = ReplayFileFormat.Read(File.ReadAllBytes("best_lap.kreplay"));
var extractor = new GhostExtractor();
var ghost = extractor.ExtractGhost(replay, "Player")
    ?? throw new InvalidOperationException("Entity not found in replay.");

// Persist it as a lightweight .keghost file for later sessions
GhostFileFormat.WriteToFile("best_lap.keghost", ghost);

// Play ghosts alongside live game
using var ghostManager = new GhostManager();
ghostManager.AddGhost("pb", ghost, new GhostVisualConfig { Opacity = 0.5f });
ghostManager.AddGhostFromFile("wr", "world_record.keghost");

// In game loop - both run in parallel
while (racing)
{
    world.Update(deltaTime);           // Live gameplay
    ghostManager.Update(deltaTime);    // Ghost playback

    renderer.RenderWorld(world);
    foreach (var instance in ghostManager.ActiveGhosts)
    {
        renderer.RenderGhost(instance.Position, instance.Rotation, instance.Opacity);
    }
}
```

**Note:** Ghost playback is separate from `ReplayPlugin` to avoid overhead — games that only *play* ghosts (e.g. from downloaded `.keghost` files) don't need full replay infrastructure. Capturing a new ghost, however, does require full replay recording (see the `GhostRecorder` gap above).

### Editor Integration: ReplayPlaybackMode

The shipped `ReplayPlaybackMode` (`editor/KeenEyes.Editor/PlayMode/ReplayPlaybackMode.cs`) is **decoupled from `PlayModeManager`**, unlike the original proposal: it is constructed with only an `IComponentSerializer` and owns an internally created, dedicated playback `World` (exposed as `PlaybackWorld`). It wraps `ReplayPlayer` with editor-friendly `EventHandler`-based events. The proposed `GetFrameInfos()`/`GetSnapshots()` timeline accessors do not exist; frame data is served through `FrameInspectionData`.

```csharp
namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Editor playback mode wrapping ReplayPlayer with a dedicated playback world.
/// </summary>
public sealed class ReplayPlaybackMode : IDisposable
{
    public ReplayPlaybackMode(IComponentSerializer serializer);

    // Dedicated playback world, created per loaded replay
    public World? PlaybackWorld { get; }

    // Loading
    public void LoadReplay(string path, bool validateChecksum = true);
    public void LoadReplay(Stream stream, bool validateChecksum = true);
    public void LoadReplay(ReplayData replayData);
    public void Unload();

    // Delegates to ReplayPlayer with editor synchronization
    public void Play();
    public void Pause();
    public void Stop();
    public void TogglePlayPause();
    public void StepFrame();
    public void StepFrameBack();
    public void SeekToFrame(int frame);
    public void SeekToTime(TimeSpan time);
    public bool Update(float deltaTime);

    // Current frame details for inspector
    public FrameInspectionData? GetCurrentFrameData();
    public FrameInspectionData GetFrameData(int frameNumber);
    public SnapshotMarker? GetNearestSnapshot(int targetFrame);

    // Events synchronized with editor
    public event EventHandler<FrameChangedEventArgs>? FrameChanged;
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
}
```

**Editor workflow:**
```csharp
// EditorApplication constructs the mode at startup
var replayMode = new ReplayPlaybackMode(serializer);
replayMode.LoadReplay(replayFilePath);

// FrameInspectorPanel is wired in and renders the current frame's data
var frameData = replayMode.GetCurrentFrameData();

// Keyboard shortcuts (EditorShortcuts):
//   Ctrl+Alt+P       -> StepFrame
//   Ctrl+Alt+Shift+P -> StepFrameBack
//   Space            -> TogglePlayPause (during replay mode)
```

## Key Design Decisions

### 1. World Ownership During Playback

**Decision:** Playback operates on a **dedicated playback world**, not the scene being edited.

**Rationale:**
- Prevents losing editor scene state during playback
- Allows comparing playback state to original recording
- Clear separation: editing world vs. playback world

**Implementation (as shipped):** `ReplayPlaybackMode` creates a fresh `World` per loaded replay and restores the replay's initial snapshot into it; the editing world is never touched.

```csharp
public sealed class ReplayPlaybackMode : IDisposable
{
    private World? playbackWorld;   // Dedicated; editing world untouched

    public void LoadReplay(ReplayData replayData)
    {
        DisposePlaybackWorld();          // Tear down any previous playback world
        player.LoadReplay(replayData);
        playbackWorld = new World();     // Fresh world for this replay

        // Restore initial snapshot into the playback world
        SnapshotManager.RestoreSnapshot(playbackWorld, firstSnapshot.Snapshot, serializer);
    }
}
```

**Editor viewport** switches to render `PlaybackWorld` during replay mode.

### 2. Seeking Implementation: Snapshot Restore

**Decision (as proposed):** Seek by restoring nearest snapshot, then replaying events to target frame.

**As shipped, seeking is snapshot-only.** When `EnableStateRestoration` is set (with a validation context attached via `SetValidationContext`), navigation restores the **nearest preceding snapshot** into the attached world, atomically with rollback (`ReplayStateRestorationException` on failure), while `CurrentFrame` moves to the exact target frame. The player documents that it "has no built-in path for re-applying those events to reconstruct exact intermediate state" — the originally proposed event-replay step to reach frame-perfect world state was **not implemented**. Recorded events between snapshots remain available to consumers via `GetFrame(int)`/`GetCurrentFrame()`, but the player does not apply them.

**Performance characteristics:**
- Seek: O(binary search for snapshot) + O(snapshot restore) — `GetNearestSnapshot(int)` finds the largest snapshot frame ≤ target
- Backwards seeking works the same way (restore an earlier snapshot)
- World-state granularity is the snapshot interval (default 1 second @ 60fps), not frame-perfect; the timeline position itself is frame-exact

### 3. Event Application vs. State Comparison

**Decision (as proposed):** Apply recorded events during playback, validate with checksums.

**Options considered:**
1. **State replay**: Restore full world state each frame (expensive, 100% accurate)
2. **Event replay**: Apply recorded events, validate periodically (efficient, requires determinism)
3. **Hybrid**: Restore snapshots at intervals, events between (balanced)

**As shipped, the event-application half of the hybrid was not built.** The player is a timeline/data engine: it exposes per-frame events for consumers (game code, `FrameInspectorPanel`) to read, and its world-facing features are snapshot restoration (Decision 2) and checksum validation. There is no `ApplyFrame`/`ApplyEntityCreated` path that mutates a world.

```csharp
// Consumers read frame events; the player validates rather than applies
var frame = player.GetCurrentFrame();
foreach (var evt in frame.Events)
{
    // consumer-side handling (inspection, visualization, custom logic)
}

// Checksum validation against the attached validation world
player.AutoValidate = true;                       // validate as frames advance
player.DesyncDetected += ex => logger.Error(ex);  // desync diagnostics
var frameOk = player.ValidateCurrentFrame();
var deterministic = player.ValidateDeterminism(iterations: 3);
```

### 4. Input System Integration

**Decision:** Separate input replay from state replay, making input replay optional.

**Rationale:**
- Not all replays need input replay (state-only debugging)
- Input replay enables determinism validation
- Decoupling allows phased implementation

**Shipped architecture ([#410](https://github.com/orion-ecs/keen-eye/issues/410), closed):** the interface shipped as `IInputRecorder` (not the proposed `IInputRecordable`), recording `InputEvent`s into replay frames. On the playback side there is no `InputProvider` property — instead, consumers register handlers and dispatch a frame's inputs explicitly:

```csharp
// Recording side
public interface IInputRecorder
{
    // Records InputEvents (typed via InputEventType) into replay frames
}

// Playback side (ReplayPlayer members)
public void RegisterInputHandler(InputEventType type, Action<InputEvent> handler);
public void RegisterInputHandler<T>(string customType, Action<T> handler);
public void ApplyInputFrame();                 // dispatch current frame's inputs to handlers
public void ApplyInputFrame(int frameIndex);
public IReadOnlyList<InputEvent> GetCurrentInputEvents();
public IReadOnlyList<InputEvent> GetInputEvents(int frameIndex);
```

Both originally planned phases shipped: state playback (#405–#409) and input replay (#410).

### 5. Plugin Architecture: Recording vs. Playback

**Decision:** Separate plugins for recording and playback.

**Rationale:**
- Different lifecycle (recording during live game, playback of historical data)
- Avoids conflicting hooks (can't record while playing back)
- Clearer API for each use case

```csharp
// Recording (existing)
world.InstallPlugin(new ReplayPlugin());
var recorder = world.GetExtension<ReplayRecorder>();

// Playback (new)
world.InstallPlugin(new ReplayPlaybackPlugin());
var player = world.GetExtension<ReplayPlayer>();
```

**Mutual exclusion (as shipped, one-directional):** `ReplayPlaybackPlugin.Install` throws `InvalidOperationException` when a `ReplayRecorder` extension is already present. The reciprocal guard was not implemented — installing `ReplayPlugin` onto a world that already has a playback player is not checked.

## Editor UI Components

### TimelinePanel

Displays replay timeline with frame markers (original design mock):

```
┌─────────────────────────────────────────────────────────────┐
│ ◀ │ ▶ ││ █ │ ⏪ ⏩ │ 0.5x [1x] 2x │  Frame: 1234 / 5000    │
├─────────────────────────────────────────────────────────────┤
│ ░░░░░░░░░░░░░░░░░░░░░░█░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│ 0:00              ↑ 0:42                              1:45  │
│              current position                               │
├─────────────────────────────────────────────────────────────┤
│ Snapshots: ● ─────● ─────● ─────● ─────● ─────● ─────● ─── │
│ Events:    ▲   ▲▲    ▲         ▲▲▲    ▲                    │
│           spawn  components     despawn                     │
└─────────────────────────────────────────────────────────────┘
```

**Status:** `TimelinePanel` is implemented with the full feature set (transport buttons, scrubber, speed control, snapshot/event marker tracks; `editor/KeenEyes.Editor/Panels/TimelinePanel.cs`) and covered by tests, but it is **not yet instantiated by `EditorApplication`** — the frame inspector is the wired-in replay debugging surface today.

### FrameInspectorPanel

`FrameInspectorPanel` is wired into `EditorApplication` and shows details of the current playback frame via `FrameInspectionData` (as shipped — entity references are raw recorded IDs, and change/execution records are dedicated record structs):

```csharp
public sealed class FrameInspectionData
{
    public FrameInspectionData(ReplayFrame frame);

    public int FrameNumber { get; }
    public TimeSpan DeltaTime { get; }
    public TimeSpan ElapsedTime { get; }

    // Events in this frame
    public IReadOnlyList<ReplayEvent> Events { get; }
    public IReadOnlyList<ReplayEvent> CustomEvents { get; }

    // Entities affected (raw entity IDs from the recording)
    public IReadOnlyList<int> CreatedEntities { get; }
    public IReadOnlyList<int> DestroyedEntities { get; }
    public IReadOnlyList<ComponentChange> ComponentChanges { get; }
    public IReadOnlyList<SystemExecution> SystemExecutions { get; }
}
```

**Not yet implemented:** the proposed `WorldDiff? DiffFromPrevious` frame-diffing member — no `WorldDiff` type exists; frame comparison was not built.

## Alternatives Considered

### Option A: Single Plugin with Modes

One `ReplayPlugin` that switches between recording and playback modes.

```csharp
replayPlugin.Mode = ReplayMode.Recording;
// or
replayPlugin.Mode = ReplayMode.Playback;
```

**Rejected because:**
- Conflicting state (recorder has current frame events, player has loaded replay)
- API confusion (which methods work in which mode?)
- Harder to test in isolation

### Option B: Editor-Only Playback

No runtime playback API; only editor can play replays.

**Rejected because:**
- Prevents runtime use cases (demo playback, killcams)
- Forces editor dependency for testing replay determinism
- Limits adoption (not all games have editor integration)

### Option C: Playback Mutates Editing World

Playback directly modifies the scene being edited.

**Rejected because:**
- Risk of losing unsaved work
- No way to compare original vs. playback
- Confusing UX (scene changes unexpectedly)

## Consequences

### Positive

1. **Clear separation** - Core player has no UI dependencies
2. **Reusable** - Same player works in runtime, editor, and tests
3. **Fast seeking** - Snapshots enable sub-100ms seek to any frame (at snapshot-interval state granularity)
4. **Determinism validation** - Checksum comparison catches desyncs (`AutoValidate`, `ValidateDeterminism`, `DesyncDetected`)
5. **Extensible** - Input replay was added (#410) without breaking the core API

### Negative

1. **Memory overhead** - Playback world duplicates state
2. **Complexity** - Two worlds to manage in editor during playback
3. **Event fidelity** - Recorded events are exposed but not re-applied; intermediate world state between snapshots cannot be reconstructed by the player

### Risks

1. **Non-determinism** - Systems with external dependencies (time, random) may desync
2. **Version compatibility** - Old replays on new code versions may fail
3. **Large replays** - Long sessions need streaming playback (future work)

## Implementation Phases

All phase issues (#405–#410) and the runtime/editor integration issues (#691–#695) are closed; the work shipped with the divergences noted per phase.

### Phase 1: Core ReplayPlayer (#405) — shipped
- [x] `ReplayPlayer` class with basic playback control
- [x] Load/unload replay data (`LoadReplay` overloads, `UnloadReplay`)
- [x] Play/pause/stop state machine
- [x] Frame stepping — shipped as `Step(int frames = 1)`, which also steps backward with negative counts
- [x] Events for state changes (`Action`-based)

### Phase 2: Timeline Navigation (#406) — shipped
- [x] `SeekToFrame()` / `SeekToTime()`
- [x] Snapshot-based seeking (`GetNearestSnapshot`, `EnableStateRestoration`)
- [x] Backward stepping via snapshot restore

### Phase 3: Speed Control (#407) — shipped
- [x] `PlaybackSpeed` property (0.25x - 4x, clamped via `PlaybackSpeeds`)
- [x] Delta time scaling in `Update()`

### Phase 4: Event Application (#408) — shipped with a changed model
- [x] Frame events (entity creation/destruction, component changes, system execution markers) exposed to consumers via `GetFrame`/`GetCurrentFrame`
- [ ] Not implemented as designed: the player does not apply recorded events to a world; state reconstruction is snapshot-based only (see Key Design Decisions 2–3)

### Phase 5: Determinism Validation (#409) — shipped
- [x] Checksum calculation (`WorldChecksum`, CRC32)
- [x] Desync detection (`DesyncDetected`, `ReplayDesyncException`)
- [x] Diagnostic validation (`ValidateCurrentFrame`, `ValidateDeterminism`)

### Phase 6: Input Integration (#410) — shipped
- [x] Input recording interface — shipped as `IInputRecorder` (not `IInputRecordable`)
- [x] Input dispatch during playback — `RegisterInputHandler` + `ApplyInputFrame` (no `InputProvider` property)
- [x] Full deterministic replay

### Phase 7: Editor Integration — mostly shipped
- [x] `ReplayPlaybackMode` in editor (serializer-only ctor, owns `PlaybackWorld`)
- [x] Frame inspector (`FrameInspectorPanel` + `FrameInspectionData`, wired into `EditorApplication`)
- [x] Keyboard shortcuts (`Ctrl+Alt+P`, `Ctrl+Alt+Shift+P`, `Space`)
- [ ] TimelinePanel implemented and tested, but not yet wired into `EditorApplication`

Additionally, the proposed `GhostRecorder` (part of the ghost-mode amendment, #695) was not built — see Runtime Integration: Ghost Mode.

## Related

### Core Playback Issues
- [#83](https://github.com/orion-ecs/keen-eye/issues/83) - Replay recording (closed — shipped)
- [#84](https://github.com/orion-ecs/keen-eye/issues/84) - Replay playback (parent issue; closed — shipped)
- [#405](https://github.com/orion-ecs/keen-eye/issues/405) - Core engine API (closed — shipped)
- [#406](https://github.com/orion-ecs/keen-eye/issues/406) - Timeline navigation (closed — shipped)
- [#407](https://github.com/orion-ecs/keen-eye/issues/407) - Speed control (closed — shipped)
- [#408](https://github.com/orion-ecs/keen-eye/issues/408) - Event system (closed — shipped as consumer-facing frame events, not player-applied)
- [#409](https://github.com/orion-ecs/keen-eye/issues/409) - Determinism validation (closed — shipped)
- [#410](https://github.com/orion-ecs/keen-eye/issues/410) - Input integration (closed — shipped)

### Runtime Integration
- [#691](https://github.com/orion-ecs/keen-eye/issues/691) - ReplayPlaybackPlugin (closed — shipped)
- [#695](https://github.com/orion-ecs/keen-eye/issues/695) - Ghost mode system (closed — shipped without `GhostRecorder`)

### Editor Integration
- [#692](https://github.com/orion-ecs/keen-eye/issues/692) - ReplayPlaybackMode (closed — shipped)
- [#693](https://github.com/orion-ecs/keen-eye/issues/693) - TimelinePanel (closed — panel implemented and tested, not yet wired into EditorApplication)
- [#694](https://github.com/orion-ecs/keen-eye/issues/694) - Frame inspector (closed — shipped)

### Related ADRs
- [ADR-001: World Manager Architecture](001-world-manager-architecture.md)
- [ADR-007: Capability-Based Plugin Architecture](007-capability-based-plugin-architecture.md)

---

## Changelog

- **v3 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Amended — all implementation issues (#405–#410, #691–#695) closed and the architecture shipped. Implementation marked Partial: `GhostRecorder` direct recording, player-driven per-frame event application (frame-perfect seek state), TimelinePanel wiring into EditorApplication, and `WorldDiff` frame-diffing did not ship. Body amended to as-built APIs: world-detached parameterless `ReplayPlayer` with `SetValidationContext` and `Action`-based events (`UnloadReplay`, `Step(int)` with negative counts), snapshot-only state restoration in place of the hybrid event-replay seek, `IInputRecorder` + `RegisterInputHandler`/`ApplyInputFrame` instead of `IInputRecordable`/`InputProvider`, one-directional plugin mutual exclusion, extraction-only ghost pipeline with `.keghost` persistence and `GhostInstance`-based `GhostManager`, and serializer-only `ReplayPlaybackMode` owning its own `PlaybackWorld` decoupled from `PlayModeManager`.
- **v2 — 2026-01-03 (58aeaafb):** Amended: added the Ghost Mode runtime-integration section (GhostData/GhostExtractor/GhostRecorder/GhostPlayer/GhostManager sketches) and the runtime/editor integration issue references (#691-#695).
- **v1 — 2026-01-02 (#84 / 3dead626):** Proposed — layered replay playback architecture: a UI-free core ReplayPlayer in KeenEyes.Replay, consumed differently by a runtime ReplayPlaybackPlugin and an editor ReplayPlaybackMode with timeline/frame-inspector panels, using snapshot+event hybrid seeking and checksum-based determinism validation.
