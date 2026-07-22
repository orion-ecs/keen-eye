using System.Numerics;
using System.Text.Json;
using KeenEyes.Common;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Tests for <see cref="GhostExtractor"/> reconstructing ghost frames at delta markers
/// of delta-compressed replays, so ghost fidelity no longer depends on
/// <see cref="ReplayOptions.KeyframeInterval"/> being 1 (see issue #1035).
/// </summary>
public class GhostExtractorTests
{
    private const float FrameDelta = 0.05f;
    private const string TransformTypeName = "KeenEyes.Common.Transform3D";

    [Fact]
    public void ExtractGhost_DeltaCompressedRecording_MatchesAllKeyframeRecording()
    {
        // Identical scripted gameplay recorded twice: once with delta compression on
        // (mixed keyframes + deltas) and once with every snapshot a full keyframe.
        var serializer = new RestorationTestSerializer().WithComponent<Transform3D>();
        var deltaReplay = RecordRace(serializer, keyframeInterval: 10, snapshotFrames: 30);
        var keyframeReplay = RecordRace(serializer, keyframeInterval: 1, snapshotFrames: 30);

        // Preconditions: the delta recording actually used deltas, the keyframe one did not.
        Assert.Contains(deltaReplay.Snapshots, m => !m.IsKeyframe);
        Assert.All(keyframeReplay.Snapshots, m => Assert.True(m.IsKeyframe));

        var extractor = new GhostExtractor();
        var deltaGhost = extractor.ExtractGhost(deltaReplay, "Racer");
        var keyframeGhost = extractor.ExtractGhost(keyframeReplay, "Racer");

        Assert.NotNull(deltaGhost);
        Assert.NotNull(keyframeGhost);

        // Every marker contributes a frame in both recordings, so the counts match.
        Assert.Equal(keyframeGhost!.FrameCount, deltaGhost!.FrameCount);
        Assert.Equal(keyframeGhost.Frames.Count, deltaGhost.Frames.Count);

        // Reconstructed positions/rotations must match the all-keyframe ghost exactly
        // (within floating-point tolerance) frame for frame.
        for (int i = 0; i < keyframeGhost.Frames.Count; i++)
        {
            var expected = keyframeGhost.Frames[i];
            var actual = deltaGhost.Frames[i];

            Assert.Equal(expected.ElapsedTime, actual.ElapsedTime);
            Assert.True(expected.Position.X.ApproximatelyEquals(actual.Position.X));
            Assert.True(expected.Position.Y.ApproximatelyEquals(actual.Position.Y));
            Assert.True(expected.Position.Z.ApproximatelyEquals(actual.Position.Z));
            Assert.True(expected.Rotation.X.ApproximatelyEquals(actual.Rotation.X));
            Assert.True(expected.Rotation.Y.ApproximatelyEquals(actual.Rotation.Y));
            Assert.True(expected.Rotation.Z.ApproximatelyEquals(actual.Rotation.Z));
            Assert.True(expected.Rotation.W.ApproximatelyEquals(actual.Rotation.W));
            Assert.True(expected.Distance.ApproximatelyEquals(actual.Distance));
        }
    }

    [Fact]
    public void ExtractGhost_CorruptedDeltaMarker_SkipsMarkerAndContinues()
    {
        var serializer = new RestorationTestSerializer().WithComponent<Transform3D>();
        var replay = RecordRace(serializer, keyframeInterval: 10, snapshotFrames: 30);

        var extractor = new GhostExtractor();
        var baseline = extractor.ExtractGhost(replay, "Racer");
        Assert.NotNull(baseline);

        // Corrupt a single mid-chain delta marker's transform data. The next delta rewrites
        // the transform, so only this one marker should drop out of the ghost.
        var corruptIndex = replay.Snapshots
            .Select((marker, index) => (marker, index))
            .First(x => !x.marker.IsKeyframe && x.index < replay.Snapshots.Count - 1)
            .index;
        var corruptElapsed = replay.Snapshots[corruptIndex].ElapsedTime;
        var corrupted = CorruptTransformData(replay, corruptIndex);

        var corruptedGhost = extractor.ExtractGhost(corrupted, "Racer");

        // Extraction continues past the bad marker rather than aborting or throwing.
        Assert.NotNull(corruptedGhost);

        // Exactly the corrupt marker is skipped; every other marker still yields a frame.
        Assert.Equal(baseline!.FrameCount - 1, corruptedGhost!.FrameCount);
        Assert.DoesNotContain(corruptedGhost.Frames, f => f.ElapsedTime == corruptElapsed);
        Assert.Contains(baseline.Frames, f => f.ElapsedTime == corruptElapsed);
    }

    #region Helpers

    /// <summary>
    /// Records a scripted lap in which a single named "Racer" entity moves along a
    /// deterministic curve, capturing one snapshot per frame from frame 1 onward.
    /// </summary>
    private static ReplayData RecordRace(
        RestorationTestSerializer serializer,
        int keyframeInterval,
        int snapshotFrames)
    {
        using var world = new World();
        var racer = world.Spawn("Racer")
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        var recorder = new ReplayRecorder(
            world,
            serializer,
            new ReplayOptions
            {
                RecordChecksums = false,
                SnapshotInterval = TimeSpan.Zero, // Snapshots captured explicitly below.
                KeyframeInterval = keyframeInterval,
            });

        recorder.StartRecording("Race");

        for (int frame = 0; frame <= snapshotFrames; frame++)
        {
            recorder.BeginFrame(FrameDelta);

            ref var transform = ref world.Get<Transform3D>(racer);
            transform.Position = new Vector3(frame * 2f, MathF.Sin(frame * 0.5f), frame * 0.5f);
            transform.Rotation = Quaternion.CreateFromYawPitchRoll(frame * 0.1f, 0f, 0f);

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
    /// Returns a copy of <paramref name="replay"/> whose delta marker at
    /// <paramref name="markerIndex"/> has its Transform3D component data replaced with a
    /// non-numeric value, so parsing that reconstructed transform fails.
    /// </summary>
    private static ReplayData CorruptTransformData(ReplayData replay, int markerIndex)
    {
        var marker = replay.Snapshots[markerIndex];
        var delta = marker.Delta!;

        var badData = JsonDocument.Parse(
            "{\"position\":{\"x\":\"corrupt\",\"y\":0,\"z\":0}," +
            "\"rotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1}," +
            "\"scale\":{\"x\":1,\"y\":1,\"z\":1}}").RootElement.Clone();

        var modifiedEntities = delta.ModifiedEntities
            .Select(entityDelta => entityDelta with
            {
                ModifiedComponents = entityDelta.ModifiedComponents
                    .Select(component => component.TypeName.StartsWith(TransformTypeName, StringComparison.Ordinal)
                        ? component with { Data = badData }
                        : component)
                    .ToList(),
            })
            .ToList();

        var corruptMarker = marker with { Delta = delta with { ModifiedEntities = modifiedEntities } };

        var snapshots = replay.Snapshots.ToList();
        snapshots[markerIndex] = corruptMarker;
        return replay with { Snapshots = snapshots };
    }

    #endregion
}
