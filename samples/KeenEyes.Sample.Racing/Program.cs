using KeenEyes.Replay.Ghost;
using KeenEyes.Sample.Racing;

// =============================================================================
// KEEN EYES ECS - Racing Ghost Mode Demo
// =============================================================================
// This sample drives a car around a circular time-trial track three times,
// building up ghosts from earlier laps and racing against them:
//
//   1. Record via ReplayPlugin + ReplayRecorder.
//   2. Extract a lightweight ghost from the replay with GhostExtractor.
//   3. Save/load it as a .keghost file with GhostFileFormat.
//   4. Race against it through GhostManager in DistanceSynced mode, printing the
//      live time gap (ahead/behind) at the same point on the track.
//   5. On the final lap, race two ghosts at once (best lap + previous lap).
//
// Everything runs unattended and headless - the "player" is a deterministic
// scripted throttle - so the demo is reproducible and CI-friendly.
// =============================================================================

Console.WriteLine("KeenEyes ECS - Racing Ghost Mode Demo");
Console.WriteLine(new string('=', 60));

var track = new Track(radius: 50f);
var renderer = new TrackRenderer(track);
var raceManager = new RaceManager(track, renderer);
var serializer = new RacingComponentSerializer();

Console.WriteLine($"Track: circular, radius {track.Radius:F0}, lap length {track.Length:F1} units.\n");

var ghostDirectory = Path.Combine(Path.GetTempPath(), "keeneyes-racing-ghosts");
Directory.CreateDirectory(ghostDirectory);
var lap1Path = Path.Combine(ghostDirectory, "lap1.keghost");
var lap2Path = Path.Combine(ghostDirectory, "lap2.keghost");

try
{
    // -------------------------------------------------------------------------
    // LAP 1 - solo recording lap. No ghosts to chase yet.
    // -------------------------------------------------------------------------
    Console.WriteLine("[Lap 1] Solo hot lap - recording (throttle 0.80)\n");
    var lap1 = raceManager.RunLap(serializer, "Lap 1", targetThrottle: 0.80f, ghosts: null);

    var lap1Ghost = GhostSetup.ExtractAndSave(lap1.Replay, lap1Path);
    Console.WriteLine($"\n  Lap 1 time: {lap1.LapSeconds:F2}s");
    Console.WriteLine(
        $"  Recorded {lap1.Replay.Snapshots.Count} snapshots -> extracted {lap1Ghost.FrameCount} ghost frames " +
        $"({new FileInfo(lap1Path).Length:N0} bytes on disk).\n");

    // -------------------------------------------------------------------------
    // LAP 2 - race against lap 1 (the previous lap), loaded from memory.
    // -------------------------------------------------------------------------
    Console.WriteLine(new string('-', 60));
    Console.WriteLine("[Lap 2] Racing the previous lap's ghost (throttle 0.60)\n");

    LapResult lap2;
    using (var lap2Ghosts = new GhostManager { DefaultSyncMode = GhostSyncMode.DistanceSynced })
    {
        lap2Ghosts.AddGhost("previous", lap1Ghost, GhostSetup.PreviousLap);
        lap2 = raceManager.RunLap(serializer, "Lap 2", targetThrottle: 0.60f, ghosts: lap2Ghosts);
    }

    GhostSetup.ExtractAndSave(lap2.Replay, lap2Path);
    Console.WriteLine($"\n  Lap 2 time: {lap2.LapSeconds:F2}s");
    Console.WriteLine($"  {Compare("Lap 2", lap2.LapSeconds, "Lap 1", lap1.LapSeconds)}\n");

    // -------------------------------------------------------------------------
    // LAP 3 - race two ghosts at once: best lap + previous lap, both loaded
    // from their .keghost files.
    // -------------------------------------------------------------------------
    Console.WriteLine(new string('-', 60));
    Console.WriteLine("[Lap 3] Racing two ghosts: best lap + previous lap (throttle 0.95)\n");

    // The previous lap is always lap 2; the best lap is whichever prior lap was fastest.
    var bestIsLap1 = lap1.LapSeconds <= lap2.LapSeconds;
    var bestPath = bestIsLap1 ? lap1Path : lap2Path;
    var bestLabel = bestIsLap1 ? "Lap 1" : "Lap 2";
    var bestTime = bestIsLap1 ? lap1.LapSeconds : lap2.LapSeconds;

    LapResult lap3;
    using (var lap3Ghosts = new GhostManager { DefaultSyncMode = GhostSyncMode.DistanceSynced })
    {
        lap3Ghosts.AddGhostFromFile("best", bestPath, GhostSetup.BestLap);
        lap3Ghosts.AddGhostFromFile("previous", lap2Path, GhostSetup.PreviousLap);
        lap3 = raceManager.RunLap(serializer, "Lap 3", targetThrottle: 0.95f, ghosts: lap3Ghosts);
    }

    Console.WriteLine($"\n  Lap 3 time: {lap3.LapSeconds:F2}s");
    Console.WriteLine($"  {Compare("Lap 3", lap3.LapSeconds, $"best ({bestLabel})", bestTime)}");
    Console.WriteLine($"  {Compare("Lap 3", lap3.LapSeconds, "previous (Lap 2)", lap2.LapSeconds)}\n");

    // -------------------------------------------------------------------------
    // Final standings.
    // -------------------------------------------------------------------------
    Console.WriteLine(new string('=', 60));
    Console.WriteLine("Final standings\n");

    var times = new (string Label, float Seconds)[]
    {
        ("Lap 1", lap1.LapSeconds),
        ("Lap 2", lap2.LapSeconds),
        ("Lap 3", lap3.LapSeconds),
    };

    var fastest = times[0];
    foreach (var entry in times)
    {
        if (entry.Seconds < fastest.Seconds)
        {
            fastest = entry;
        }
    }

    foreach (var entry in times)
    {
        var marker = entry.Label == fastest.Label ? "  <- fastest" : string.Empty;
        Console.WriteLine($"  {entry.Label}: {entry.Seconds,6:F2}s{marker}");
    }

    Console.WriteLine("\nDemo complete.");
}
finally
{
    if (Directory.Exists(ghostDirectory))
    {
        Directory.Delete(ghostDirectory, recursive: true);
    }
}

// Formats a comparison between a lap time and a reference time.
static string Compare(string lapLabel, float lapSeconds, string referenceLabel, float referenceSeconds)
{
    var delta = lapSeconds - referenceSeconds;
    var verdict = delta < 0f ? "faster" : "slower";
    return $"{lapLabel} is {Math.Abs(delta):F2}s {verdict} than {referenceLabel} ({delta:+0.00;-0.00}s).";
}
