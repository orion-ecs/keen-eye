using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// A component used by the snapshot restoration tests.
/// </summary>
public struct RestorableHealth : IComponent
{
    /// <summary>The current health value.</summary>
    public int Current;

    /// <summary>The maximum health value.</summary>
    public int Max;
}

/// <summary>
/// Integration tests for world-state restoration during replay navigation
/// (see <see cref="ReplayPlayer.EnableStateRestoration"/>).
/// </summary>
public class SnapshotRestorationTests
{
    private const float FrameDelta = 0.1f;

    #region EnableStateRestoration Flag Tests

    [Fact]
    public void EnableStateRestoration_Default_IsFalse()
    {
        using var player = new ReplayPlayer();

        Assert.False(player.EnableStateRestoration);
    }

    [Fact]
    public void EnableStateRestoration_SetToTrue_ReturnsTrue()
    {
        using var player = new ReplayPlayer();

        player.EnableStateRestoration = true;

        Assert.True(player.EnableStateRestoration);
    }

    #endregion

    #region Restore-On-Seek Tests

    [Fact]
    public void SeekToFrame_WithRestorationEnabled_RestoresNearestSnapshotState()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer);

        using var playbackWorld = new World();
        // Populate the world with unrelated state that restoration must wipe.
        for (int i = 0; i < 5; i++)
        {
            playbackWorld.Spawn().With(new RestorableHealth { Current = 999, Max = 999 }).Build();
        }

        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        // Frame 7's nearest preceding snapshot is frame 6 (Current == 106).
        player.SeekToFrame(7);

        var snapshotAt6 = replay.Snapshots.Single(s => s.FrameNumber == 6);

        // Timeline position advances to the exact target frame.
        Assert.Equal(7, player.CurrentFrame);

        // World state lands on the nearest preceding snapshot's state.
        var restored = playbackWorld.Query<RestorableHealth>().ToList();
        Assert.Equal(3, restored.Count);
        Assert.All(restored, e => Assert.Equal(106, playbackWorld.Get<RestorableHealth>(e).Current));

        // Checksum-verified: restored state matches the recorded snapshot exactly.
        Assert.Equal(snapshotAt6.Checksum, WorldChecksum.Calculate(playbackWorld, serializer));
    }

    [Fact]
    public void SeekToFrame_TargetBetweenSnapshots_LandsOnEarlierSnapshotState()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer);

        using var playbackWorld = new World();
        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        // Frame 8 lies between snapshots at frames 6 and 9; its true per-frame value
        // would be 108, but restoration lands on the earlier snapshot (frame 6 -> 106).
        player.SeekToFrame(8);

        Assert.Equal(8, player.CurrentFrame);
        var entity = playbackWorld.Query<RestorableHealth>().First();
        Assert.Equal(106, playbackWorld.Get<RestorableHealth>(entity).Current);
    }

    [Fact]
    public void SeekToTime_WithRestorationEnabled_RestoresNearestSnapshotState()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer);

        using var playbackWorld = new World();
        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        // Frame 3 is at elapsed time 3 * FrameDelta; seek slightly past it.
        player.SeekToTime(TimeSpan.FromSeconds(3.5 * FrameDelta));

        var entity = playbackWorld.Query<RestorableHealth>().First();
        Assert.Equal(103, playbackWorld.Get<RestorableHealth>(entity).Current);
    }

    [Fact]
    public void Step_BackwardWithRestorationEnabled_RestoresNearestSnapshotState()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer);

        using var playbackWorld = new World();
        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        // Move forward to frame 9 first (snapshot at 9 -> 109).
        player.SeekToFrame(9);
        var forward = playbackWorld.Query<RestorableHealth>().First();
        Assert.Equal(109, playbackWorld.Get<RestorableHealth>(forward).Current);

        // Step backward to frame 4; nearest preceding snapshot is frame 3 -> 103.
        player.Step(-5);

        Assert.Equal(4, player.CurrentFrame);
        var back = playbackWorld.Query<RestorableHealth>().First();
        Assert.Equal(103, playbackWorld.Get<RestorableHealth>(back).Current);
    }

    #endregion

    #region Disabled Flag Tests

    [Fact]
    public void SeekToFrame_WithRestorationDisabled_LeavesWorldUntouched()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer);

        using var playbackWorld = new World();
        playbackWorld.Spawn("Untouched").With(new RestorableHealth { Current = 555, Max = 555 }).Build();
        playbackWorld.Spawn("AlsoUntouched").With(new RestorableHealth { Current = 555, Max = 555 }).Build();

        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        // EnableStateRestoration deliberately left at its default (false).

        var checksumBefore = WorldChecksum.Calculate(playbackWorld, serializer);
        var countBefore = playbackWorld.GetAllEntities().Count();

        player.SeekToFrame(6);

        // Timeline still moves, but the world is not mutated.
        Assert.Equal(6, player.CurrentFrame);
        Assert.Equal(countBefore, playbackWorld.GetAllEntities().Count());
        Assert.Equal(checksumBefore, WorldChecksum.Calculate(playbackWorld, serializer));
        Assert.All(
            playbackWorld.Query<RestorableHealth>(),
            e => Assert.Equal(555, playbackWorld.Get<RestorableHealth>(e).Current));
    }

    [Fact]
    public void SeekToFrame_WithRestorationEnabledButNoContext_LeavesWorldUntouched()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer);

        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.EnableStateRestoration = true;
        // No validation/restore context set: restoration is silently skipped.

        player.SeekToFrame(6);

        Assert.Equal(6, player.CurrentFrame);
    }

    [Fact]
    public void SeekToFrame_WithRestorationEnabledButNoSnapshots_LeavesWorldUntouched()
    {
        var serializer = CreateSerializer();

        using var playbackWorld = new World();
        playbackWorld.Spawn().With(new RestorableHealth { Current = 7, Max = 7 }).Build();

        using var player = new ReplayPlayer();
        player.LoadReplay(CreateReplayWithoutSnapshots());
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        var checksumBefore = WorldChecksum.Calculate(playbackWorld, serializer);

        player.SeekToFrame(2);

        Assert.Equal(2, player.CurrentFrame);
        Assert.Equal(checksumBefore, WorldChecksum.Calculate(playbackWorld, serializer));
    }

    #endregion

    #region Corrupt Snapshot Safety Tests

    [Fact]
    public void SeekToFrame_WithCorruptSnapshot_LeavesWorldIntactAndThrows()
    {
        var serializer = CreateSerializer();
        var replay = CreateReplayWithCorruptSnapshot();

        using var playbackWorld = new World();
        playbackWorld.Spawn("Survivor").With(new RestorableHealth { Current = 42, Max = 42 }).Build();
        playbackWorld.Spawn("AlsoSurvivor").With(new RestorableHealth { Current = 42, Max = 42 }).Build();

        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        var checksumBefore = WorldChecksum.Calculate(playbackWorld, serializer);
        var countBefore = playbackWorld.GetAllEntities().Count();
        var frameBefore = player.CurrentFrame;

        // The nearest snapshot for frame 1 is the corrupt one at frame 0.
        var ex = Assert.Throws<ReplayStateRestorationException>(() => player.SeekToFrame(1));

        // Meaningful error referencing the frame numbers involved.
        Assert.Contains("frame 0", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("frame 1", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ex.FrameNumber);
        Assert.Equal(0, ex.SnapshotFrameNumber);

        // World is rolled back to its pre-seek state - not left half-mutated.
        Assert.Equal(countBefore, playbackWorld.GetAllEntities().Count());
        Assert.Equal(checksumBefore, WorldChecksum.Calculate(playbackWorld, serializer));
        Assert.All(
            playbackWorld.Query<RestorableHealth>(),
            e => Assert.Equal(42, playbackWorld.Get<RestorableHealth>(e).Current));

        // Timeline position is unchanged because the frame index only advances after
        // a successful restore.
        Assert.Equal(frameBefore, player.CurrentFrame);
    }

    #endregion

    #region Helpers

    private static RestorationTestSerializer CreateSerializer()
        => new RestorationTestSerializer().WithComponent<RestorableHealth>();

    /// <summary>
    /// Records a 10-frame session with three named entities whose health is mutated
    /// each frame, capturing snapshots at frames 0, 3, 6, and 9. The value at frame f
    /// is <c>100 + f</c>.
    /// </summary>
    private static ReplayData RecordSession(RestorationTestSerializer serializer)
    {
        using var recordingWorld = new World();
        var entities = new[]
        {
            recordingWorld.Spawn("Player").With(new RestorableHealth { Current = 100, Max = 100 }).Build(),
            recordingWorld.Spawn("Enemy1").With(new RestorableHealth { Current = 100, Max = 100 }).Build(),
            recordingWorld.Spawn("Enemy2").With(new RestorableHealth { Current = 100, Max = 100 }).Build(),
        };

        var recorder = new ReplayRecorder(
            recordingWorld,
            serializer,
            new ReplayOptions
            {
                RecordChecksums = true,
                SnapshotInterval = TimeSpan.Zero, // Snapshots are captured explicitly below.
            });

        recorder.StartRecording("Restoration Session");

        for (int frame = 0; frame < 10; frame++)
        {
            recorder.BeginFrame(FrameDelta);

            foreach (var entity in entities)
            {
                ref var health = ref recordingWorld.Get<RestorableHealth>(entity);
                health.Current = 100 + frame;
            }

            if (frame > 0 && frame % 3 == 0)
            {
                recorder.CaptureSnapshot();
            }

            recorder.EndFrame(FrameDelta);
        }

        var data = recorder.StopRecording();
        Assert.NotNull(data);
        return data!;
    }

    private static ReplayData CreateReplayWithoutSnapshots()
    {
        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(3 * FrameDelta),
            FrameCount = 3,
            Frames =
            [
                new ReplayFrame { FrameNumber = 0, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.Zero, Events = [] },
                new ReplayFrame { FrameNumber = 1, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.FromSeconds(FrameDelta), Events = [] },
                new ReplayFrame { FrameNumber = 2, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.FromSeconds(2 * FrameDelta), Events = [] },
            ],
            Snapshots = []
        };
    }

    /// <summary>
    /// Builds replay data whose only snapshot (at frame 0) contains a component with a
    /// schema version far newer than the serializer supports, which forces
    /// <see cref="SnapshotManager.RestoreSnapshot"/> to throw during restoration.
    /// </summary>
    private static ReplayData CreateReplayWithCorruptSnapshot()
    {
        var badSnapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity
                {
                    Id = 0,
                    Name = "Corrupt",
                    Components =
                    [
                        new SerializedComponent
                        {
                            TypeName = typeof(RestorableHealth).FullName!,
                            Data = JsonDocument.Parse("{\"current\":1,\"max\":1}").RootElement.Clone(),
                            IsTag = false,
                            Version = 999, // Far newer than the serializer's version (1).
                        }
                    ]
                }
            ],
            Singletons = []
        };

        var marker = new SnapshotMarker
        {
            FrameNumber = 0,
            ElapsedTime = TimeSpan.Zero,
            Snapshot = badSnapshot
        };

        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(2 * FrameDelta),
            FrameCount = 2,
            Frames =
            [
                new ReplayFrame { FrameNumber = 0, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.Zero, Events = [], PrecedingSnapshotIndex = 0 },
                new ReplayFrame { FrameNumber = 1, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.FromSeconds(FrameDelta), Events = [] },
            ],
            Snapshots = [marker]
        };
    }

    #endregion
}

/// <summary>
/// A minimal functional JSON serializer for the restoration tests. Unlike the stub
/// serializers used elsewhere in this test project, this one actually round-trips
/// component data so snapshots restore real state.
/// </summary>
internal sealed class RestorationTestSerializer : IComponentSerializer
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, Type> typeMap = [];
    private readonly Dictionary<Type, Func<JsonElement, object>> deserializers = [];
    private readonly Dictionary<Type, Func<object, JsonElement>> serializers = [];
    private readonly Dictionary<Type, Func<ISerializationCapability, bool, ComponentInfo>> registrars = [];

    public RestorationTestSerializer WithComponent<T>() where T : struct, IComponent
    {
        var type = typeof(T);

        foreach (var name in new[] { type.AssemblyQualifiedName, type.FullName, type.Name })
        {
            if (name is not null)
            {
                typeMap[name] = type;
            }
        }

        deserializers[type] = json => JsonSerializer.Deserialize<T>(json.GetRawText(), jsonOptions)!;
        serializers[type] = obj =>
        {
            var jsonStr = JsonSerializer.Serialize((T)obj, jsonOptions);
            using var doc = JsonDocument.Parse(jsonStr);
            return doc.RootElement.Clone();
        };
        registrars[type] = (serialization, isTag) => (ComponentInfo)serialization.Components.Register<T>(isTag);

        return this;
    }

    public bool IsSerializable(Type type) => deserializers.ContainsKey(type);

    public bool IsSerializable(string typeName) => typeMap.ContainsKey(typeName);

    public object? Deserialize(string typeName, JsonElement json)
        => typeMap.TryGetValue(typeName, out var type) && deserializers.TryGetValue(type, out var d) ? d(json) : null;

    public JsonElement? Serialize(Type type, object value)
        => serializers.TryGetValue(type, out var s) ? s(value) : null;

    public Type? GetType(string typeName)
        => typeMap.TryGetValue(typeName, out var type) ? type : null;

    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)
        => typeMap.TryGetValue(typeName, out var type) && registrars.TryGetValue(type, out var r) ? r(serialization, isTag) : null;

    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value) => false;

    public object? CreateDefault(string typeName)
        => typeMap.TryGetValue(typeName, out var type) ? Activator.CreateInstance(type) : null;

    public int GetVersion(string typeName) => 1;

    public int GetVersion(Type type) => 1;
}
