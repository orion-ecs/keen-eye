using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Tests for delta-compressed snapshot markers: recording as keyframe/delta chains,
/// reconstructing state by replaying deltas during navigation, file-format versioning,
/// and the resulting size reduction (see issue #531).
/// </summary>
public class DeltaCompressionTests
{
    private const float FrameDelta = 0.1f;

    #region Recording / Keyframe Interval Tests

    [Fact]
    public void CaptureSnapshot_FirstMarker_IsAlwaysKeyframe()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 3, snapshotFrames: 6);

        Assert.NotEmpty(replay.Snapshots);
        Assert.True(replay.Snapshots[0].IsKeyframe);
        Assert.NotNull(replay.Snapshots[0].Snapshot);
        Assert.Null(replay.Snapshots[0].Delta);
    }

    [Fact]
    public void CaptureSnapshot_WithKeyframeInterval_HonorsIntervalForKeyframesAndDeltas()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 3, snapshotFrames: 6);

        // Markers: idx 0,3,6 are keyframes; 1,2,4,5 are deltas.
        for (int i = 0; i < replay.Snapshots.Count; i++)
        {
            var marker = replay.Snapshots[i];
            if (i % 3 == 0)
            {
                Assert.True(marker.IsKeyframe, $"Marker {i} should be a keyframe.");
                Assert.NotNull(marker.Snapshot);
                Assert.Null(marker.Delta);
                Assert.Null(marker.BaselineFrameNumber);
            }
            else
            {
                Assert.False(marker.IsKeyframe, $"Marker {i} should be a delta.");
                Assert.Null(marker.Snapshot);
                Assert.NotNull(marker.Delta);
                Assert.Equal(replay.Snapshots[i - 1].FrameNumber, marker.BaselineFrameNumber);
            }
        }
    }

    [Fact]
    public void CaptureSnapshot_WithKeyframeIntervalOne_CapturesAllKeyframes()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 1, snapshotFrames: 6);

        Assert.All(replay.Snapshots, m => Assert.True(m.IsKeyframe));
        Assert.All(replay.Snapshots, m => Assert.Null(m.Delta));
    }

    [Fact]
    public void ReplayData_CurrentVersion_IsTwo()
    {
        Assert.Equal(2, ReplayData.CurrentVersion);
    }

    #endregion

    #region Delta Chain Reconstruction Tests

    [Fact]
    public void SeekToFrame_DeltaMarker_ReconstructsStateFromKeyframePlusDeltaChain()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 3, snapshotFrames: 9);

        // Frame 8's marker is a delta whose chain roots at the keyframe on frame 6.
        var marker8 = replay.Snapshots.Single(s => s.FrameNumber == 8);
        Assert.False(marker8.IsKeyframe);

        using var playbackWorld = new World();
        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        player.SeekToFrame(8);

        // Timeline advances to the exact target frame.
        Assert.Equal(8, player.CurrentFrame);

        // The recorded value at frame f is 100 + f, so the delta chain must reconstruct 108.
        Assert.Equal(3, playbackWorld.Query<RestorableHealth>().Count());
        Assert.All(
            playbackWorld.Query<RestorableHealth>(),
            e => Assert.Equal(108, playbackWorld.Get<RestorableHealth>(e).Current));

        // Checksum-verified: reconstructed state matches the marker's recorded checksum.
        Assert.Equal(marker8.Checksum, WorldChecksum.Calculate(playbackWorld, serializer));
    }

    [Fact]
    public void SeekToFrame_DeltaMarkerImmediatelyAfterKeyframe_ReconstructsState()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 3, snapshotFrames: 9);

        // Frame 4's marker is the first delta after the keyframe on frame 3.
        var marker4 = replay.Snapshots.Single(s => s.FrameNumber == 4);
        Assert.False(marker4.IsKeyframe);

        using var playbackWorld = new World();
        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        player.SeekToFrame(4);

        Assert.All(
            playbackWorld.Query<RestorableHealth>(),
            e => Assert.Equal(104, playbackWorld.Get<RestorableHealth>(e).Current));
        Assert.Equal(marker4.Checksum, WorldChecksum.Calculate(playbackWorld, serializer));
    }

    [Fact]
    public void SeekToFrame_KeyframeMarker_RestoresDirectlyWithoutDeltas()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 3, snapshotFrames: 9);

        var marker6 = replay.Snapshots.Single(s => s.FrameNumber == 6);
        Assert.True(marker6.IsKeyframe);

        using var playbackWorld = new World();
        using var player = new ReplayPlayer();
        player.LoadReplay(replay);
        player.SetValidationContext(playbackWorld, serializer);
        player.EnableStateRestoration = true;

        player.SeekToFrame(6);

        Assert.All(
            playbackWorld.Query<RestorableHealth>(),
            e => Assert.Equal(106, playbackWorld.Get<RestorableHealth>(e).Current));
        Assert.Equal(marker6.Checksum, WorldChecksum.Calculate(playbackWorld, serializer));
    }

    #endregion

    #region File Format Versioning Tests

    [Fact]
    public void ReplayFileFormat_CurrentVersion_IsTwo()
    {
        Assert.Equal((ushort)2, ReplayFileFormat.CurrentVersion);
    }

    [Fact]
    public void LoadReplay_Version1File_LoadsAsAllKeyframeMarkers()
    {
        // Build a version-1 style recording: all markers are full keyframes and the
        // payload carries no delta fields, exactly as a pre-delta writer would produce.
        var v1Bytes = CreateVersion1FileBytes();

        using var player = new ReplayPlayer();
        player.LoadReplay(v1Bytes, validateChecksum: true);

        Assert.NotNull(player.LoadedReplay);
        Assert.Equal(1, player.LoadedReplay!.Version);
        Assert.Equal(2, player.LoadedReplay.Snapshots.Count);

        // Absent delta fields deserialize as keyframes.
        Assert.All(player.LoadedReplay.Snapshots, m => Assert.True(m.IsKeyframe));
        Assert.All(player.LoadedReplay.Snapshots, m => Assert.Null(m.Delta));
    }

    [Fact]
    public void WriteThenRead_DeltaCompressedReplay_RoundTripsMarkerKinds()
    {
        var serializer = CreateSerializer();
        var replay = RecordSession(serializer, keyframeInterval: 3, snapshotFrames: 6);

        var bytes = ReplayFileFormat.Write(replay);
        var (_, restored) = ReplayFileFormat.Read(bytes);

        Assert.Equal(2, restored.Version);
        Assert.Equal(replay.Snapshots.Count, restored.Snapshots.Count);

        for (int i = 0; i < restored.Snapshots.Count; i++)
        {
            Assert.Equal(replay.Snapshots[i].IsKeyframe, restored.Snapshots[i].IsKeyframe);
            Assert.Equal(replay.Snapshots[i].BaselineFrameNumber, restored.Snapshots[i].BaselineFrameNumber);
        }
    }

    #endregion

    #region Corrupt Delta Safety Tests

    [Fact]
    public void SeekToFrame_CorruptDeltaInChain_RollsBackWorldAndThrows()
    {
        var serializer = CreateSerializer();
        var replay = CreateReplayWithCorruptDelta(serializer);

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

        // Frame 1's marker is a delta with malformed component data; its chain root is the
        // keyframe at frame 0. The keyframe restores fine, then the delta apply fails.
        var ex = Assert.Throws<ReplayStateRestorationException>(() => player.SeekToFrame(1));

        // The failure references the delta marker (frame 1), not the keyframe.
        Assert.Equal(1, ex.FrameNumber);
        Assert.Equal(1, ex.SnapshotFrameNumber);

        // World is rolled back to its pre-seek state, not left half-mutated by the
        // successful keyframe restore that preceded the failed delta.
        Assert.Equal(countBefore, playbackWorld.GetAllEntities().Count());
        Assert.Equal(checksumBefore, WorldChecksum.Calculate(playbackWorld, serializer));
        Assert.All(
            playbackWorld.Query<RestorableHealth>(),
            e => Assert.Equal(42, playbackWorld.Get<RestorableHealth>(e).Current));

        // Timeline position is unchanged because the frame index only advances after a
        // successful restore.
        Assert.Equal(frameBefore, player.CurrentFrame);
    }

    #endregion

    #region Size Comparison Tests

    [Fact]
    public void DeltaCompression_ManyEntitiesFewChanges_ProducesMateriallySmallerRecording()
    {
        var serializer = CreateSerializer();

        // Scale entity count high so the delta's fixed structural overhead cannot approach
        // the full-snapshot size (see the DeltaSave lesson in AutoSaveSystemTests).
        const int entityCount = 500;
        const int snapshotFrames = 10;

        var deltaReplay = RecordLargeSession(serializer, entityCount, snapshotFrames, keyframeInterval: 100);
        var fullReplay = RecordLargeSession(serializer, entityCount, snapshotFrames, keyframeInterval: 1);

        // Compare uncompressed serialized size so the structural payload is measured
        // directly rather than the stream compressor's ability to dedupe repetition.
        var options = new ReplayFileOptions { Compression = CompressionMode.None, IncludeChecksum = false };
        var deltaSize = ReplayFileFormat.Write(deltaReplay, options).Length;
        var fullSize = ReplayFileFormat.Write(fullReplay, options).Length;

        // Sanity: both recordings captured the same number of markers.
        Assert.Equal(fullReplay.Snapshots.Count, deltaReplay.Snapshots.Count);

        Assert.True(
            deltaSize < fullSize,
            $"Delta recording ({deltaSize} bytes) should be smaller than all-full ({fullSize} bytes).");

        // With 1 of 500 entities changing per snapshot, deltas should be far under half
        // the all-full size. Threshold is deliberately loose to survive serializer drift.
        Assert.True(
            deltaSize < fullSize * 0.5,
            $"Delta recording ({deltaSize} bytes) should be under 50% of all-full ({fullSize} bytes); " +
            $"reduction was {(1.0 - (double)deltaSize / fullSize) * 100:F1}%.");
    }

    #endregion

    #region Helpers

    private static RestorationTestSerializer CreateSerializer()
        => new RestorationTestSerializer().WithComponent<RestorableHealth>();

    /// <summary>
    /// Records a session with three entities whose health is set to <c>100 + frame</c>
    /// each frame, capturing one snapshot per frame from frame 1 onward (frame 0's marker
    /// is the recorder's automatic initial snapshot).
    /// </summary>
    private static ReplayData RecordSession(
        RestorationTestSerializer serializer,
        int keyframeInterval,
        int snapshotFrames)
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
                SnapshotInterval = TimeSpan.Zero, // Snapshots captured explicitly below.
                KeyframeInterval = keyframeInterval,
            });

        recorder.StartRecording("Delta Session");

        for (int frame = 0; frame <= snapshotFrames; frame++)
        {
            recorder.BeginFrame(FrameDelta);

            foreach (var entity in entities)
            {
                ref var health = ref recordingWorld.Get<RestorableHealth>(entity);
                health.Current = 100 + frame;
            }

            // Frame 0 already has the automatic initial snapshot; capture the rest.
            if (frame > 0)
            {
                recorder.CaptureSnapshot();
            }

            recorder.EndFrame(FrameDelta);
        }

        var data = recorder.StopRecording();
        Assert.NotNull(data);
        return data!;
    }

    /// <summary>
    /// Records a session with many entities where exactly one entity changes per snapshot,
    /// capturing one snapshot per frame.
    /// </summary>
    private static ReplayData RecordLargeSession(
        RestorationTestSerializer serializer,
        int entityCount,
        int snapshotFrames,
        int keyframeInterval)
    {
        using var recordingWorld = new World();
        var entities = new Entity[entityCount];
        for (int i = 0; i < entityCount; i++)
        {
            entities[i] = recordingWorld.Spawn($"Entity{i}")
                .With(new RestorableHealth { Current = i, Max = 100 })
                .Build();
        }

        var recorder = new ReplayRecorder(
            recordingWorld,
            serializer,
            new ReplayOptions
            {
                RecordChecksums = false,
                SnapshotInterval = TimeSpan.Zero,
                KeyframeInterval = keyframeInterval,
            });

        recorder.StartRecording("Large Session");

        for (int frame = 0; frame < snapshotFrames; frame++)
        {
            recorder.BeginFrame(FrameDelta);

            // Change exactly one entity per frame.
            ref var health = ref recordingWorld.Get<RestorableHealth>(entities[frame % entityCount]);
            health.Current += 1000;

            if (frame > 0)
            {
                recorder.CaptureSnapshot();
            }

            recorder.EndFrame(FrameDelta);
        }

        var data = recorder.StopRecording();
        Assert.NotNull(data);
        return data!;
    }

    /// <summary>
    /// Builds the raw bytes of a version-1 .kreplay file: a full-keyframe recording whose
    /// container header advertises version 1 and whose markers carry no delta fields.
    /// </summary>
    private static byte[] CreateVersion1FileBytes()
    {
        var serializer = CreateSerializer();

        using var world = new World();
        world.Spawn("Hero").With(new RestorableHealth { Current = 100, Max = 100 }).Build();
        var snapshotA = SnapshotManager.CreateSnapshot(world, serializer);

        world.Spawn("Villain").With(new RestorableHealth { Current = 50, Max = 50 }).Build();
        var snapshotB = SnapshotManager.CreateSnapshot(world, serializer);

        var replay = new ReplayData
        {
            Version = 1,
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(2 * FrameDelta),
            FrameCount = 2,
            Frames =
            [
                new ReplayFrame { FrameNumber = 0, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.Zero, Events = [], PrecedingSnapshotIndex = 0 },
                new ReplayFrame { FrameNumber = 1, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.FromSeconds(FrameDelta), Events = [], PrecedingSnapshotIndex = 1 },
            ],
            Snapshots =
            [
                new SnapshotMarker { FrameNumber = 0, ElapsedTime = TimeSpan.Zero, Snapshot = snapshotA },
                new SnapshotMarker { FrameNumber = 1, ElapsedTime = TimeSpan.FromSeconds(FrameDelta), Snapshot = snapshotB },
            ],
        };

        var bytes = ReplayFileFormat.Write(replay);

        // Patch the container header version (ushort at offset 4) from the current version
        // down to 1 to produce a genuine version-1 file. The stored CRC32 covers only the
        // compressed payload, so the header edit does not invalidate it.
        bytes[4] = 1;
        bytes[5] = 0;
        return bytes;
    }

    /// <summary>
    /// Builds a replay with a valid keyframe at frame 0 followed by a delta marker at
    /// frame 1 whose modified-component data is malformed, forcing the delta apply to
    /// throw during reconstruction.
    /// </summary>
    private static ReplayData CreateReplayWithCorruptDelta(RestorationTestSerializer serializer)
    {
        using var world = new World();
        world.Spawn("Recorded").With(new RestorableHealth { Current = 1, Max = 1 }).Build();
        var keyframe = SnapshotManager.CreateSnapshot(world, serializer);

        // Delta references entity id 0 (the sole entity in the keyframe) but supplies a
        // string where the "current" int field is expected, which throws on deserialize.
        var badDelta = new DeltaSnapshot
        {
            BaselineSlotName = "0",
            ModifiedEntities =
            [
                new EntityDelta
                {
                    EntityId = 0,
                    ModifiedComponents =
                    [
                        new SerializedComponent
                        {
                            TypeName = typeof(RestorableHealth).FullName!,
                            Data = JsonDocument.Parse("{\"current\":\"not-an-int\",\"max\":1}").RootElement.Clone(),
                            IsTag = false,
                            Version = 1,
                        }
                    ]
                }
            ]
        };

        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(2 * FrameDelta),
            FrameCount = 2,
            Frames =
            [
                new ReplayFrame { FrameNumber = 0, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.Zero, Events = [], PrecedingSnapshotIndex = 0 },
                new ReplayFrame { FrameNumber = 1, DeltaTime = TimeSpan.FromSeconds(FrameDelta), ElapsedTime = TimeSpan.FromSeconds(FrameDelta), Events = [], PrecedingSnapshotIndex = 1 },
            ],
            Snapshots =
            [
                new SnapshotMarker { FrameNumber = 0, ElapsedTime = TimeSpan.Zero, Snapshot = keyframe },
                new SnapshotMarker { FrameNumber = 1, ElapsedTime = TimeSpan.FromSeconds(FrameDelta), Delta = badDelta, BaselineFrameNumber = 0 },
            ],
        };
    }

    #endregion
}
