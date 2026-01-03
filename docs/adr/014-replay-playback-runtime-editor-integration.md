# ADR-014: Replay Playback Runtime and Editor Integration

**Status:** Proposed
**Date:** 2026-01-02
**Issue:** [#84](https://github.com/orion-ecs/keen-eye/issues/84)

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
│  - LoadReplay(path/data)                                    │
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
│ - Installs player   │           │ - Extends PlayModeManager│
│ - Game calls Update │           │ - Timeline panel sync   │
│                     │           │ - Inspector integration │
└─────────────────────┘           └─────────────────────────┘
```

### Core API: ReplayPlayer

```csharp
namespace KeenEyes.Replay;

/// <summary>
/// Core playback engine for replaying recorded sessions.
/// </summary>
public sealed class ReplayPlayer
{
    // Construction
    public ReplayPlayer(IWorld world, IComponentSerializer serializer);

    // Loading
    public void LoadReplay(string path);
    public void LoadReplay(ReplayData data);
    public void Unload();

    // Playback control
    public void Play();
    public void Pause();
    public void Stop();
    public void Step(int frames = 1);
    public void StepBack(int frames = 1);

    // Timeline navigation
    public void SeekToFrame(int frameNumber);
    public void SeekToTime(TimeSpan time);

    // Speed control
    public float PlaybackSpeed { get; set; } // 0.25x to 4x, default 1.0

    // State
    public PlaybackState State { get; }
    public bool IsLoaded { get; }
    public int CurrentFrame { get; }
    public int TotalFrames { get; }
    public TimeSpan CurrentTime { get; }
    public TimeSpan TotalDuration { get; }

    // Frame advancement (called by game loop or editor)
    public void Update(float deltaTime);

    // Events
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<FrameChangedEventArgs>? FrameChanged;
    public event EventHandler? PlaybackCompleted;
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}
```

### Runtime Integration: ReplayPlaybackPlugin

For games that want simple playback without editor:

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
        var serializer = context.GetCapability<ISerializationCapability>()
            .ComponentRegistry.CreateSerializer();
        player = new ReplayPlayer(context.World, serializer);
        context.RegisterExtension(player);
    }

    public void Uninstall(IPluginContext context)
    {
        player?.Stop();
        player?.Unload();
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

For racing/time-trial games that show a "ghost" of a previous run alongside live gameplay.

**Key differences from full replay:**
- Runs **in parallel** with live game, not instead of it
- Only tracks a **single entity** (player character/vehicle)
- **Visual-only** - no collision or physics interaction
- Supports **multiple simultaneous ghosts** (personal best, world record, friend)
- **Lightweight format** - KBs instead of MBs

```csharp
namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Lightweight ghost data extracted from a replay or recorded directly.
/// </summary>
public sealed class GhostData
{
    public string Name { get; }
    public int TotalFrames { get; }
    public TimeSpan Duration { get; }
    public IReadOnlyList<GhostFrame> Frames { get; }
}

/// <summary>
/// Extracts ghost data from full replay files.
/// </summary>
public sealed class GhostExtractor
{
    public GhostData ExtractGhost(ReplayData replay, string entityName);
    public GhostData ExtractGhost(ReplayData replay, Predicate<Entity> selector);
}

/// <summary>
/// Records ghost data directly (without full replay overhead).
/// </summary>
public sealed class GhostRecorder
{
    public GhostRecorder(IWorld world, string entityName);

    public void StartRecording(string name);
    public void Update(float deltaTime);
    public GhostData StopRecording();
}

/// <summary>
/// Plays back a ghost alongside live gameplay.
/// </summary>
public sealed class GhostPlayer
{
    public GhostPlayer(GhostData ghost);

    public void Play();
    public void Pause();
    public void Reset();
    public void Update(float deltaTime);

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public bool IsComplete { get; }
}

/// <summary>
/// Manages multiple ghosts for racing scenarios.
/// </summary>
public sealed class GhostManager
{
    public void AddGhost(string id, GhostData data, GhostVisualConfig config);
    public void RemoveGhost(string id);
    public void Update(float deltaTime);
    public IEnumerable<(string Id, GhostPlayer Player)> ActiveGhosts { get; }
}
```

**Ghost mode usage:**
```csharp
// Extract ghost from existing replay
var replay = ReplayFileFormat.Load("best_lap.kreplay");
var ghost = GhostExtractor.ExtractGhost(replay, "Player");

// Or record ghost directly (lightweight)
var recorder = new GhostRecorder(world, "Player");
recorder.StartRecording("Time Trial");
// ... race happens ...
var ghost = recorder.StopRecording();

// Play ghost alongside live game
var ghostManager = new GhostManager();
ghostManager.AddGhost("pb", ghost, new GhostVisualConfig { Opacity = 0.5f });

// In game loop - both run in parallel
while (racing)
{
    world.Update(deltaTime);           // Live gameplay
    ghostManager.Update(deltaTime);    // Ghost playback

    renderer.RenderWorld(world);
    renderer.RenderGhosts(ghostManager);
}
```

**Note:** Ghost recording is separate from `ReplayPlugin` to avoid overhead. Games that only need ghost mode don't need full replay infrastructure.

### Editor Integration: ReplayPlaybackMode

Extends existing `PlayModeManager` with replay capabilities:

```csharp
namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Extends PlayModeManager to support replay file playback.
/// </summary>
public sealed class ReplayPlaybackMode : IDisposable
{
    private readonly PlayModeManager playModeManager;
    private readonly IWorld world;
    private readonly ReplayPlayer player;

    public ReplayPlaybackMode(PlayModeManager playModeManager, IWorld world);

    // Loading
    public void LoadReplay(string path);
    public void LoadReplay(ReplayData data);

    // Delegates to ReplayPlayer with editor synchronization
    public void Play();
    public void Pause();
    public void Stop();
    public void StepFrame();
    public void StepFrameBack();
    public void SeekToFrame(int frame);

    // Timeline data for UI
    public IReadOnlyList<FrameInfo> GetFrameInfos();
    public IReadOnlyList<SnapshotMarker> GetSnapshots();

    // Current frame details for inspector
    public FrameInspectionData GetCurrentFrameData();

    // Events synchronized with editor
    public event EventHandler<FrameChangedEventArgs>? FrameChanged;
}
```

**Editor workflow:**
```csharp
// In EditorApplication when user opens a .kreplay file
var replayMode = new ReplayPlaybackMode(playModeManager, currentWorld);
replayMode.LoadReplay(replayFilePath);

// TimelinePanel subscribes to events
replayMode.FrameChanged += (s, e) => timelinePanel.UpdatePosition(e.Frame);

// Inspector shows frame data
var frameData = replayMode.GetCurrentFrameData();
inspectorPanel.ShowReplayFrame(frameData);

// Keyboard shortcuts
shortcutManager.Register("Ctrl+Alt+P", () => replayMode.StepFrame());
shortcutManager.Register("Ctrl+Alt+Shift+P", () => replayMode.StepFrameBack());
```

## Key Design Decisions

### 1. World Ownership During Playback

**Decision:** Playback operates on a **dedicated playback world**, not the scene being edited.

**Rationale:**
- Prevents losing editor scene state during playback
- Allows comparing playback state to original recording
- Clear separation: editing world vs. playback world

**Implementation:**
```csharp
public sealed class ReplayPlaybackMode
{
    private readonly IWorld editingWorld;   // Preserved
    private readonly IWorld playbackWorld;  // Created for playback

    public void LoadReplay(ReplayData data)
    {
        // Create fresh world for playback
        playbackWorld = new World();
        player = new ReplayPlayer(playbackWorld, serializer);
        player.LoadReplay(data);

        // Restore initial snapshot to playback world
        var initialSnapshot = data.Snapshots[0].WorldSnapshot;
        SnapshotManager.RestoreSnapshot(playbackWorld, initialSnapshot, serializer);
    }
}
```

**Editor viewport** switches to render `playbackWorld` during replay mode.

### 2. Seeking Implementation: Snapshot + Event Replay

**Decision:** Seek by restoring nearest snapshot, then replaying events to target frame.

**Rationale:**
- Fast seeking via snapshots (O(1) restore)
- Frame-perfect accuracy via event replay
- Backwards seeking possible (restore earlier snapshot)

**Implementation:**
```csharp
public void SeekToFrame(int targetFrame)
{
    // Find nearest snapshot at or before target
    var snapshot = FindNearestSnapshot(targetFrame);

    // Restore world to snapshot state
    SnapshotManager.RestoreSnapshot(world, snapshot.WorldSnapshot, serializer);
    currentFrame = snapshot.Frame;

    // Replay events from snapshot to target
    while (currentFrame < targetFrame)
    {
        ApplyFrame(replayData.Frames[currentFrame]);
        currentFrame++;
    }
}

private SnapshotMarker FindNearestSnapshot(int targetFrame)
{
    // Binary search for largest snapshot.Frame <= targetFrame
    return snapshots
        .Where(s => s.Frame <= targetFrame)
        .OrderByDescending(s => s.Frame)
        .First();
}
```

**Performance characteristics:**
- Seek within same snapshot interval: O(frames between)
- Seek across snapshots: O(snapshot restore) + O(frames to target)
- Default snapshot interval (1 second @ 60fps): Max 60 frames to replay

### 3. Event Application vs. State Comparison

**Decision:** Apply recorded events during playback, validate with checksums.

**Options considered:**
1. **State replay**: Restore full world state each frame (expensive, 100% accurate)
2. **Event replay**: Apply recorded events, validate periodically (efficient, requires determinism)
3. **Hybrid**: Restore snapshots at intervals, events between (balanced)

**Chosen:** Option 3 (Hybrid) with checksum validation.

```csharp
private void ApplyFrame(ReplayFrame frame)
{
    foreach (var evt in frame.Events)
    {
        switch (evt.EventType)
        {
            case ReplayEventType.EntityCreated:
                ApplyEntityCreated(evt);
                break;
            case ReplayEventType.ComponentChanged:
                ApplyComponentChanged(evt);
                break;
            // ... other event types
        }
    }

    // Periodic checksum validation
    if (options.ValidateChecksums && frame.Checksum.HasValue)
    {
        var actualChecksum = CalculateWorldChecksum();
        if (actualChecksum != frame.Checksum.Value)
        {
            OnDesyncDetected(frame.FrameNumber, frame.Checksum.Value, actualChecksum);
        }
    }
}
```

### 4. Input System Integration

**Decision:** Separate input replay from state replay, making input replay optional.

**Rationale:**
- Not all replays need input replay (state-only debugging)
- Input replay enables determinism validation
- Decoupling allows phased implementation

**Architecture:**
```csharp
public interface IInputRecordable
{
    void RecordInput(InputFrame frame);
    InputFrame GetRecordedInput(int frameNumber);
}

public sealed class ReplayPlayer
{
    // Optional input replay
    public IInputRecordable? InputProvider { get; set; }

    private void ApplyFrame(ReplayFrame frame)
    {
        // If input provider set, inject recorded inputs
        if (InputProvider != null && frame.InputData != null)
        {
            InputProvider.InjectInput(frame.InputData);
        }

        // Then run systems normally (they read injected input)
        world.Update(frame.DeltaTime);
    }
}
```

**Phase 1 (this ADR):** State replay only - apply recorded events directly.
**Phase 2 (#410):** Input replay - inject inputs and let systems run.

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

**Mutual exclusion:** Installing both plugins on the same world throws `InvalidOperationException`. A world is either recording OR playing back, never both.

## Editor UI Components

### TimelinePanel (New)

Displays replay timeline with frame markers:

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

### FrameInspectorPanel (New or Inspector Extension)

Shows details of current playback frame:

```csharp
public sealed class FrameInspectionData
{
    public int FrameNumber { get; }
    public float DeltaTime { get; }
    public TimeSpan ElapsedTime { get; }

    // Events in this frame
    public IReadOnlyList<ReplayEvent> Events { get; }

    // Entities affected
    public IReadOnlyList<Entity> CreatedEntities { get; }
    public IReadOnlyList<Entity> DestroyedEntities { get; }
    public IReadOnlyList<(Entity, Type)> ComponentChanges { get; }

    // Comparison to previous frame
    public WorldDiff? DiffFromPrevious { get; }
}
```

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
3. **Fast seeking** - Snapshots enable sub-100ms seek to any frame
4. **Determinism validation** - Checksum comparison catches desyncs
5. **Extensible** - Input replay can be added later without API changes

### Negative

1. **Memory overhead** - Playback world duplicates state
2. **Complexity** - Two worlds to manage in editor during playback
3. **Event fidelity** - Some events may be hard to replay exactly (external state)

### Risks

1. **Non-determinism** - Systems with external dependencies (time, random) may desync
2. **Version compatibility** - Old replays on new code versions may fail
3. **Large replays** - Long sessions need streaming playback (future work)

## Implementation Phases

### Phase 1: Core ReplayPlayer (#405)
- `ReplayPlayer` class with basic playback control
- Load/unload replay data
- Play/pause/stop state machine
- Frame stepping (forward only)
- Events for state changes

### Phase 2: Timeline Navigation (#406)
- `SeekToFrame()` / `SeekToTime()`
- Snapshot-based seeking
- Backward stepping via snapshot restore

### Phase 3: Speed Control (#407)
- `PlaybackSpeed` property (0.25x - 4x)
- Delta time scaling in `Update()`

### Phase 4: Event Application (#408)
- Apply recorded events to world
- Entity creation/destruction
- Component changes
- System execution markers

### Phase 5: Determinism Validation (#409)
- Checksum calculation
- Desync detection
- Diagnostic events for debugging

### Phase 6: Input Integration (#410)
- `IInputRecordable` interface
- Input injection during playback
- Full deterministic replay

### Phase 7: Editor Integration
- `ReplayPlaybackMode` in editor
- TimelinePanel implementation
- Frame inspector extensions
- Keyboard shortcuts

## Related

### Core Playback Issues
- [#83](https://github.com/orion-ecs/keen-eye/issues/83) - Replay recording (complete)
- [#84](https://github.com/orion-ecs/keen-eye/issues/84) - Replay playback (parent issue)
- [#405](https://github.com/orion-ecs/keen-eye/issues/405) - Core engine API
- [#406](https://github.com/orion-ecs/keen-eye/issues/406) - Timeline navigation
- [#407](https://github.com/orion-ecs/keen-eye/issues/407) - Speed control
- [#408](https://github.com/orion-ecs/keen-eye/issues/408) - Event system
- [#409](https://github.com/orion-ecs/keen-eye/issues/409) - Determinism validation
- [#410](https://github.com/orion-ecs/keen-eye/issues/410) - Input integration

### Runtime Integration
- [#691](https://github.com/orion-ecs/keen-eye/issues/691) - ReplayPlaybackPlugin
- [#695](https://github.com/orion-ecs/keen-eye/issues/695) - Ghost mode system

### Editor Integration
- [#692](https://github.com/orion-ecs/keen-eye/issues/692) - ReplayPlaybackMode
- [#693](https://github.com/orion-ecs/keen-eye/issues/693) - TimelinePanel
- [#694](https://github.com/orion-ecs/keen-eye/issues/694) - Frame inspector

### Related ADRs
- ADR-001: World Manager Architecture
- ADR-007: Capability-Based Plugin Architecture
