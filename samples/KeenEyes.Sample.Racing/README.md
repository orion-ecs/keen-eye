# KeenEyes.Sample.Racing

This sample demonstrates the **Ghost Mode** pipeline in `KeenEyes.Replay` in a
time-trial racing context: record a lap, distil it into a lightweight *ghost*, save
it to disk, then race against it - and against multiple ghosts at once - while
showing the live time gap.

It is the first end-to-end consumer of the ghost pipeline, so it also shows the one
piece of integration glue a real game needs: making the engine's `Transform3D`
component serializable into replay snapshots.

## What it demonstrates

1. **Recording** a lap with `ReplayPlugin` + `ReplayRecorder`.
2. **Extracting** a ghost from the replay with `GhostExtractor.ExtractGhost`.
3. **Saving / loading** ghosts as `.keghost` files via `GhostFileFormat`.
4. **Racing** against ghosts through `GhostManager` in `GhostSyncMode.DistanceSynced`,
   printing the live **time gap** (ahead / behind) at the same point on the track.
5. **Multiple simultaneous ghosts** - the final lap races the *best* lap and the
   *previous* lap at the same time, each with its own `GhostVisualConfig`
   (tint, opacity, label).

Everything runs **headless and unattended**. The "player" is a deterministic
scripted throttle, so the demo is reproducible and CI-friendly - no window, GPU, or
input required.

## Running the sample

```bash
dotnet run --project samples/KeenEyes.Sample.Racing -c Release
```

The demo drives three laps of a circular track:

| Lap | Throttle | Role |
|-----|----------|------|
| 1 | 0.80 | Solo hot lap - recorded, becomes the *best* lap |
| 2 | 0.60 | Slower lap, raced against lap 1 (the *previous* lap) |
| 3 | 0.95 | Fastest lap, raced against **two** ghosts: best (lap 1) + previous (lap 2) |

It prints per-quarter progress, a top-down ASCII view of the track, the live time
gap to each ghost, and a final standings table.

## Project layout

```
KeenEyes.Sample.Racing/
├── Program.cs                         # Orchestrates the three-lap demo
├── Components/
│   ├── Vehicle.cs                     # Speed / steering state (pure data)
│   ├── TrackPosition.cs               # Distance travelled along the track
│   └── LapTimer.cs                    # Elapsed / final lap time
├── Systems/
│   ├── VehicleMovementSystem.cs       # Drives the car along the racing line
│   └── LapTrackingSystem.cs           # Lap timing and completion
└── Game/
    ├── Track.cs                       # Parametric circular racing line
    ├── RaceManager.cs                 # Runs one lap: record + race + report
    ├── GhostSetup.cs                  # Ghost extraction + visual configs
    ├── TrackRenderer.cs               # Top-down ASCII track view
    └── RacingComponentSerializer.cs   # Makes Transform3D serializable
```

## Key concepts

### Making `Transform3D` recordable

The ghost extractor reads each entity's `KeenEyes.Common.Transform3D` out of replay
snapshots. But the source-generated `ComponentSerializer` only knows about components
declared with `[Component(Serializable = true)]` **in this project** - it never sees
engine types from `KeenEyes.Common`.

`RacingComponentSerializer` closes that gap. It wraps the generated serializer and
intercepts only `Transform3D`, emitting the exact JSON shape the extractor expects
(`Position` / `Rotation` / `Scale` with `X/Y/Z/W` members). Everything else is
delegated straight through. This is the pattern any game will use to record engine
components for ghosts.

```csharp
var serializer = new RacingComponentSerializer();
world.InstallPlugin(new ReplayPlugin(serializer, recordingOptions));
```

### Recording settings chosen for smooth ghosts

```csharp
new ReplayOptions
{
    SnapshotInterval = TimeSpan.FromSeconds(0.05), // ~20 Hz snapshots
    KeyframeInterval = 1,                          // every snapshot is a full keyframe
    RecordSystemEvents = false,
    RecordComponentEvents = false,
    RecordEntityEvents = false,
}
```

Two settings matter for ghost fidelity, and they interact:

- **`SnapshotInterval`** controls how often the world state is captured. Ghost frames
  come from snapshots, so a short interval means a smoother ghost.
- **`KeyframeInterval`** controls how many snapshots are stored as full *keyframes*
  versus space-saving *deltas*. **The ghost extractor only reads keyframes** - delta
  markers carry no snapshot. With the default `KeyframeInterval = 10`, only every
  tenth snapshot would be usable and the ghost would be jerky. Setting it to `1`
  makes every snapshot a keyframe, so all ~100+ frames per lap feed the ghost.

The trade-off: `KeyframeInterval = 1` disables delta compression, so the replay is
larger. That is the right call here - a single-car time-trial recording is tiny
(a few kilobytes) and fidelity is what matters for a good ghost.

### Distance-synced comparison

Ghosts play back in `DistanceSynced` mode: each frame the manager is told how far the
car has travelled, and it positions each ghost at wherever *it* was after the same
distance.

```csharp
using var ghosts = new GhostManager { DefaultSyncMode = GhostSyncMode.DistanceSynced };
ghosts.AddGhostFromFile("best", bestPath, GhostSetup.BestLap);
ghosts.AddGhostFromFile("previous", lap2Path, GhostSetup.PreviousLap);
ghosts.PlayAll();

// each frame:
ghosts.UpdateByDistance(car.TrackPosition.Distance);
foreach (var ghost in ghosts.ActiveGhosts)
{
    var gap = carElapsedSeconds - ghost.Player.CurrentTime.TotalSeconds;
    // gap < 0 => the car reached this distance sooner => it is ahead
}
```

Because the comparison is at *equal distance*, the ghost sits at the same point on the
track as the car - the difference between them is **time**, not position. That is why
the ASCII map shows the car lapping while the head-to-head result is reported as a
live time gap rather than as separated dots on the track.

### Ghost visual configuration

`GhostVisualConfig` is metadata a renderer consumes; the ghost system never draws
anything itself. This sample uses it to label and color each ghost distinctly:

```csharp
public static GhostVisualConfig BestLap { get; } = new()
{
    TintColor = new Vector4(1f, 0.84f, 0f, 1f), // gold
    Opacity = 0.4f,
    Label = "Best Lap",
};
```

## Sample output (excerpt)

```
[Lap 3] Racing two ghosts: best lap + previous lap (throttle 0.95)

  [ 50%]  t =   3.38s
          vs Best Lap      -0.42s (ahead)
          vs Previous Lap  -1.38s (ahead)

  Lap 3 time: 6.15s
  Lap 3 is 0.93s faster than best (Lap 1) (-0.93s).
  Lap 3 is 2.98s faster than previous (Lap 2) (-2.98s).

Final standings

  Lap 1:   7.08s
  Lap 2:   9.13s
  Lap 3:   6.15s  <- fastest
```
