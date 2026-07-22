using System;
using System.Collections.Generic;
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Replay;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// The outcome of running one lap.
/// </summary>
/// <param name="Replay">The full replay recording of the lap.</param>
/// <param name="LapSeconds">The final lap time, in seconds.</param>
public readonly record struct LapResult(ReplayData Replay, float LapSeconds);

/// <summary>
/// Runs a single lap in an isolated world: sets up recording, drives the car with
/// scripted throttle, optionally races it against a set of ghosts, and prints
/// progress and live time gaps.
/// </summary>
public sealed class RaceManager
{
    // 20 Hz snapshots (every 0.05s) with the default delta compression left on. The ghost
    // extractor reconstructs state at delta markers, so every snapshot feeds the ghost and
    // the recording stays compact - see the README for the full rationale.
    private static readonly ReplayOptions recordingOptions = new()
    {
        SnapshotInterval = TimeSpan.FromSeconds(0.05),
        RecordSystemEvents = false,
        RecordComponentEvents = false,
        RecordEntityEvents = false,
    };

    private const float DeltaTime = 1f / 60f;
    private const int SafetyFrameCap = 60 * 60; // 60 seconds of simulation.

    private readonly Track track;
    private readonly TrackRenderer renderer;

    // Reused across checkpoints so reading a ghost's trail allocates nothing per call.
    // Sized to comfortably hold the sample's configured trail lengths.
    private readonly Vector3[] trailBuffer = new Vector3[128];

    /// <summary>
    /// Initializes a new instance of the <see cref="RaceManager"/> class.
    /// </summary>
    /// <param name="track">The track to race on.</param>
    /// <param name="renderer">The console renderer for the top-down view.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public RaceManager(Track track, TrackRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(renderer);
        this.track = track;
        this.renderer = renderer;
    }

    /// <summary>
    /// Runs one lap and returns its recording and final time.
    /// </summary>
    /// <param name="serializer">The serializer used to capture snapshots (must handle Transform3D).</param>
    /// <param name="recordingName">A name for the replay recording.</param>
    /// <param name="targetThrottle">The scripted throttle held for the whole lap (0..1).</param>
    /// <param name="ghosts">Ghosts to race against, or null for a solo recording lap.</param>
    /// <returns>The lap result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public LapResult RunLap(
        RacingComponentSerializer serializer,
        string recordingName,
        float targetThrottle,
        GhostManager? ghosts)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(recordingName);

        using var world = new World();
        world.InstallPlugin(new ReplayPlugin(serializer, recordingOptions));
        world.AddSystem(new VehicleMovementSystem(track), SystemPhase.Update);
        world.AddSystem(new LapTrackingSystem(track.Length), SystemPhase.Update);

        var car = world.Spawn(GhostSetup.CarEntityName)
            .With(new Vehicle { MaxSpeed = 60f, Acceleration = 45f })
            .With(new TrackPosition())
            .With(new LapTimer())
            .With(new Transform3D(
                track.PositionAt(0f),
                Quaternion.CreateFromYawPitchRoll(track.HeadingAt(0f), 0f, 0f),
                Vector3.One))
            .Build();

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording(recordingName);
        ghosts?.PlayAll();

        var nextCheckpoint = 0.25f;
        var frame = 0;

        while (true)
        {
            // The scripted "driver" writes this frame's input, then systems run.
            world.Get<Vehicle>(car).Throttle = targetThrottle;
            world.Update(DeltaTime);

            var distance = world.Get<TrackPosition>(car).Distance;
            var elapsed = world.Get<LapTimer>(car).ElapsedSeconds;
            var finished = world.Get<LapTimer>(car).Finished;

            // Distance-synced ghosts advance to wherever they were at the same distance.
            ghosts?.UpdateByDistance(distance);

            var fraction = Math.Clamp(distance / track.Length, 0f, 1f);
            if (fraction >= nextCheckpoint && nextCheckpoint < 1f)
            {
                ReportCheckpoint(world, car, ghosts, fraction, elapsed);
                nextCheckpoint += 0.25f;
            }

            if (finished || ++frame >= SafetyFrameCap)
            {
                break;
            }
        }

        var replay = recorder.StopRecording()
            ?? throw new InvalidOperationException("Recording produced no data.");
        ghosts?.StopAll();

        var lapSeconds = world.Get<LapTimer>(car).FinishedSeconds;
        return new LapResult(replay, lapSeconds);
    }

    private void ReportCheckpoint(
        World world,
        Entity car,
        GhostManager? ghosts,
        float fraction,
        float elapsed)
    {
        Console.WriteLine($"  [{fraction * 100,3:F0}%]  t = {elapsed,6:F2}s");

        if (ghosts is null)
        {
            return;
        }

        var markers = new List<TrackMarker>
        {
            new('C', "Your Car", world.Get<Transform3D>(car).Position),
        };
        var trails = new List<TrackTrail>();

        foreach (var ghost in ghosts.ActiveGhosts)
        {
            // Positive gap: the live car reached this distance later than the ghost
            // did, so the car is behind. Negative gap means the car is ahead.
            var gap = elapsed - (float)ghost.Player.CurrentTime.TotalSeconds;
            var status = gap <= 0f ? "ahead" : "behind";
            var label = ghost.Label ?? ghost.Id;
            var symbol = char.ToUpperInvariant(label[0]);

            Console.WriteLine($"          vs {label,-13} {gap:+0.00;-0.00}s ({status})");
            markers.Add(new TrackMarker(symbol, label, ghost.Position));

            // When the ghost opts into a trail, read its recent path from the
            // data-only provider. GetTrailPoints writes into our reusable buffer with
            // no per-call allocation; we then snapshot just the points it wrote.
            if (ghost.ShowTrail)
            {
                var length = Math.Min(ghost.Config.TrailLength, trailBuffer.Length);
                var count = ghost.GetTrailPoints(trailBuffer.AsSpan(0, length));
                if (count > 0)
                {
                    trails.Add(new TrackTrail(
                        trailBuffer.AsSpan(0, count).ToArray(),
                        ghost.Config.TrailFadeStart,
                        ghost.Config.TrailStyle));
                }
            }
        }

        Console.WriteLine(renderer.Render(markers, trails));
    }
}
