using KeenEyes;
using KeenEyes.Generated;
using KeenEyes.Replay;
using KeenEyes.Sample.Replay;

// =============================================================================
// KEEN EYES ECS - Replay Recording Demo
// =============================================================================
// This sample demonstrates:
// 1. Basic replay recording and data capture
// 2. Custom event recording for game-specific events
// 3. Ring buffer mode for crash replay scenarios
// 4. File save/load with compression options
// 5. Reading replay metadata without loading full data
// =============================================================================

Console.WriteLine("KeenEyes ECS - Replay Recording Demo");
Console.WriteLine(new string('=', 50));

// =============================================================================
// PART 1: Basic Recording Setup
// =============================================================================

Console.WriteLine("\n[1] Basic Recording Setup\n");

using var world = new World();

// Configure replay options
var replayOptions = new ReplayOptions
{
    SnapshotInterval = TimeSpan.FromSeconds(0.5), // Capture snapshot every 0.5s
    RecordSystemEvents = true,
    RecordEntityEvents = true,
    RecordComponentEvents = true
};

// Install the replay plugin
var replayPlugin = new ReplayPlugin(ComponentSerializer.Instance, replayOptions);
world.InstallPlugin(replayPlugin);

Console.WriteLine("Replay plugin installed with options:");
Console.WriteLine($"  Snapshot interval: {replayOptions.SnapshotInterval.TotalSeconds}s");
Console.WriteLine($"  Record system events: {replayOptions.RecordSystemEvents}");
Console.WriteLine($"  Record entity events: {replayOptions.RecordEntityEvents}");

// Add game systems
world.AddSystem(new MovementSystem(), SystemPhase.Update);
world.AddSystem(new CombatSystem(), SystemPhase.Update);

// Get the recorder
var recorder = world.GetExtension<ReplayRecorder>();

// =============================================================================
// PART 2: Recording Gameplay
// =============================================================================

Console.WriteLine("\n[2] Recording Gameplay\n");

// Start recording with metadata
var metadata = new Dictionary<string, object>
{
    ["GameVersion"] = "1.0.0",
    ["LevelName"] = "Tutorial",
    ["Difficulty"] = "Normal"
};

recorder.StartRecording("Tutorial Playthrough", metadata);
Console.WriteLine("Started recording: 'Tutorial Playthrough'");

// Create player
var player = world.Spawn("Player")
    .WithPosition(x: 0, y: 0)
    .WithVelocity(x: 10, y: 5)
    .WithHealth(current: 100, max: 100)
    .WithPlayer()
    .Build();

Console.WriteLine($"Created player: {player}");

// Create some enemies
for (int i = 0; i < 5; i++)
{
    var enemy = world.Spawn($"Enemy_{i}")
        .WithPosition(x: 50 + i * 20, y: 30)
        .WithVelocity(x: -5, y: 0)
        .WithHealth(current: 30, max: 30)
        .WithEnemy()
        .Build();
    Console.WriteLine($"Created enemy: {enemy}");
}

// Simulate gameplay for a few frames
Console.WriteLine("\nSimulating 60 frames of gameplay...");
for (int frame = 0; frame < 60; frame++)
{
    // Simulate player shooting every 20 frames
    if (frame % 20 == 0)
    {
        ref readonly var playerPos = ref world.Get<Position>(player);
        var projectile = world.Spawn()
            .WithPosition(x: playerPos.X, y: playerPos.Y)
            .WithVelocity(x: 50, y: 0)
            .WithProjectile()
            .Build();

        // Record a custom event for the shot
        recorder.RecordCustomEvent("PlayerShot", new Dictionary<string, object>
        {
            ["projectileId"] = projectile.Id,
            ["fromX"] = playerPos.X,
            ["fromY"] = playerPos.Y
        });
    }

    world.Update(1f / 60f); // 60 FPS
}

// Check recording status
Console.WriteLine($"\nRecording status:");
Console.WriteLine($"  Frames recorded: {recorder.RecordedFrameCount}");
Console.WriteLine($"  Snapshots captured: {recorder.SnapshotCount}");
Console.WriteLine($"  Elapsed time: {recorder.ElapsedTime.TotalSeconds:F2}s");

// Stop recording and get data
var replayData = recorder.StopRecording();
Console.WriteLine($"\nRecording complete!");
Console.WriteLine($"  Total frames: {replayData!.FrameCount}");
Console.WriteLine($"  Total snapshots: {replayData.Snapshots.Count}");
Console.WriteLine($"  Duration: {replayData.Duration.TotalSeconds:F2}s");
Console.WriteLine($"  Average FPS: {replayData.AverageFrameRate:F1}");

// =============================================================================
// PART 3: Analyzing Recorded Events
// =============================================================================

Console.WriteLine("\n[3] Analyzing Recorded Events\n");

// Count event types
var eventCounts = new Dictionary<ReplayEventType, int>();
foreach (var frame in replayData.Frames)
{
    foreach (var evt in frame.Events)
    {
        eventCounts.TryGetValue(evt.Type, out var count);
        eventCounts[evt.Type] = count + 1;
    }
}

Console.WriteLine("Event counts by type:");
foreach (var (type, count) in eventCounts.OrderByDescending(kv => kv.Value))
{
    Console.WriteLine($"  {type}: {count}");
}

// Find custom events
var customEvents = replayData.Frames
    .SelectMany(f => f.Events)
    .Where(e => e.Type == ReplayEventType.Custom)
    .ToList();

Console.WriteLine($"\nCustom events ({customEvents.Count}):");
foreach (var evt in customEvents)
{
    Console.WriteLine($"  {evt.CustomType}: {string.Join(", ", evt.Data?.Select(kv => $"{kv.Key}={kv.Value}") ?? [])}");
}

// =============================================================================
// PART 4: Saving to File
// =============================================================================

Console.WriteLine("\n[4] Saving to File\n");

var tempPath = Path.Combine(Path.GetTempPath(), "replay_demo.kreplay");

// Save with default compression (GZip)
ReplayFileFormat.WriteToFile(tempPath, replayData);
var fileInfo = new FileInfo(tempPath);
Console.WriteLine($"Saved replay to: {tempPath}");
Console.WriteLine($"  File size: {fileInfo.Length:N0} bytes");

// Compare compression modes
Console.WriteLine("\nCompression comparison:");

var noCompression = ReplayFileFormat.Write(replayData, new ReplayFileOptions
{
    Compression = CompressionMode.None
});
Console.WriteLine($"  No compression: {noCompression.Length:N0} bytes");

var gzipCompression = ReplayFileFormat.Write(replayData, new ReplayFileOptions
{
    Compression = CompressionMode.GZip
});
Console.WriteLine($"  GZip compression: {gzipCompression.Length:N0} bytes ({100.0 * gzipCompression.Length / noCompression.Length:F1}%)");

var brotliCompression = ReplayFileFormat.Write(replayData, new ReplayFileOptions
{
    Compression = CompressionMode.Brotli
});
Console.WriteLine($"  Brotli compression: {brotliCompression.Length:N0} bytes ({100.0 * brotliCompression.Length / noCompression.Length:F1}%)");

// =============================================================================
// PART 5: Loading from File
// =============================================================================

Console.WriteLine("\n[5] Loading from File\n");

// Read just metadata (fast, doesn't load full data)
var loadedInfo = ReplayFileFormat.ReadMetadataFromFile(tempPath);
Console.WriteLine("Metadata (quick read):");
Console.WriteLine($"  Name: {loadedInfo.Name}");
Console.WriteLine($"  Duration: {loadedInfo.Duration.TotalSeconds:F2}s");
Console.WriteLine($"  Frames: {loadedInfo.FrameCount}");
Console.WriteLine($"  Snapshots: {loadedInfo.SnapshotCount}");
Console.WriteLine($"  Compression: {loadedInfo.Compression}");
Console.WriteLine($"  Compression ratio: {loadedInfo.CompressionRatio:P1}");

// Load full data
var (fullInfo, loadedData) = ReplayFileFormat.ReadFromFile(tempPath);
Console.WriteLine($"\nFull data loaded: {loadedData.Frames.Count} frames, {loadedData.Snapshots.Count} snapshots");

// Verify checksum
Console.WriteLine($"Checksum valid: {fullInfo.IsValid}");

// Clean up temp file
File.Delete(tempPath);

// =============================================================================
// PART 6: Ring Buffer Mode (Crash Replay)
// =============================================================================

Console.WriteLine("\n[6] Ring Buffer Mode (Crash Replay)\n");

// Create a new world for ring buffer demo
using var crashWorld = new World();

var crashOptions = new ReplayOptions
{
    SnapshotInterval = TimeSpan.FromSeconds(1),
    MaxFrames = 30, // Keep only last 30 frames
    UseRingBuffer = true,
    RecordSystemEvents = false, // Minimize overhead
    RecordEntityEvents = true
};

var crashPlugin = new ReplayPlugin(ComponentSerializer.Instance, crashOptions);
crashWorld.InstallPlugin(crashPlugin);
crashWorld.AddSystem(new MovementSystem(), SystemPhase.Update);

var crashRecorder = crashWorld.GetExtension<ReplayRecorder>();

Console.WriteLine("Ring buffer mode enabled:");
Console.WriteLine($"  Max frames: {crashOptions.MaxFrames}");
Console.WriteLine($"  Use ring buffer: {crashOptions.UseRingBuffer}");

// Start recording (always on in crash replay mode)
crashRecorder.StartRecording("Crash Buffer");

// Create an entity
crashWorld.Spawn("CrashTestEntity")
    .WithPosition(x: 0, y: 0)
    .WithVelocity(x: 1, y: 0)
    .Build();

// Simulate 100 frames (more than ring buffer size)
Console.WriteLine("\nSimulating 100 frames (buffer size is 30)...");
for (int frame = 0; frame < 100; frame++)
{
    crashWorld.Update(1f / 60f);
}

// Simulate crash - get the buffer contents
var crashData = crashRecorder.StopRecording();
Console.WriteLine($"\nRing buffer captured:");
Console.WriteLine($"  Frames in buffer: {crashData!.FrameCount}");
Console.WriteLine($"  First frame number: {crashData.Frames[0].FrameNumber}");
Console.WriteLine($"  Last frame number: {crashData.Frames[^1].FrameNumber}");
Console.WriteLine($"  Buffer duration: {crashData.Duration.TotalSeconds:F2}s");

// This would be saved to a crash report
Console.WriteLine("\n  [Crash data could be saved for debugging]");

// =============================================================================
// PART 7: Seeking with Snapshots
// =============================================================================

Console.WriteLine("\n[7] Seeking with Snapshots\n");

Console.WriteLine($"Original replay has {replayData.Snapshots.Count} snapshots:");
for (int i = 0; i < replayData.Snapshots.Count; i++)
{
    var snapshot = replayData.Snapshots[i];
    Console.WriteLine($"  Snapshot {i}: Frame {snapshot.FrameNumber}, Time {snapshot.ElapsedTime.TotalSeconds:F2}s");
}

// Find snapshot nearest to a target time
var targetTime = TimeSpan.FromSeconds(0.75);
var nearestSnapshot = replayData.Snapshots
    .Where(s => s.ElapsedTime <= targetTime)
    .OrderByDescending(s => s.ElapsedTime)
    .FirstOrDefault();

if (nearestSnapshot != null)
{
    Console.WriteLine($"\nTo seek to {targetTime.TotalSeconds:F2}s:");
    Console.WriteLine($"  1. Restore snapshot at frame {nearestSnapshot.FrameNumber} ({nearestSnapshot.ElapsedTime.TotalSeconds:F2}s)");

    var framesToReplay = replayData.Frames
        .Count(f => f.FrameNumber > nearestSnapshot.FrameNumber &&
                    f.ElapsedTime <= targetTime);
    Console.WriteLine($"  2. Replay {framesToReplay} frames to reach target");
}

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("Replay recording demo complete!");
