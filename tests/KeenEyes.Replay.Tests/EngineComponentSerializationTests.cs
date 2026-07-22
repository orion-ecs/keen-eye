using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Generated;
using KeenEyes.Replay.Ghost;
using KeenEyes.Serialization;

// Engine components are opted into this test assembly's generated ComponentSerializer
// exactly the way a game does it - this is the mechanism under test.
[assembly: SerializeEngineComponents(typeof(Transform3D))]

namespace KeenEyes.Replay.Tests;

/// <summary>
/// End-to-end tests proving that an engine component (<see cref="Transform3D"/> from
/// KeenEyes.Common) opted in via <see cref="SerializeEngineComponentsAttribute"/> flows
/// through the source-generated <see cref="ComponentSerializer"/> and into the replay
/// ghost pipeline without any hand-written serializer wrapper.
/// </summary>
public class EngineComponentSerializationTests
{
    #region Generated Serializer Coverage Tests

    [Fact]
    public void IsSerializable_WithOptedInEngineComponent_ReturnsTrue()
    {
        var serializer = ComponentSerializer.Instance;

        Assert.True(serializer.IsSerializable(typeof(Transform3D)));
        Assert.True(serializer.IsSerializable("KeenEyes.Common.Transform3D"));
    }

    [Fact]
    public void Serialize_WithTransform3D_RoundTripsAllFields()
    {
        var serializer = ComponentSerializer.Instance;
        var original = new Transform3D(
            new Vector3(12.5f, -3.75f, 42f),
            Quaternion.CreateFromYawPitchRoll(1.2f, -0.4f, 0.9f),
            new Vector3(1f, 2f, 0.5f));

        var json = serializer.Serialize(typeof(Transform3D), original);
        Assert.NotNull(json);

        var restored = serializer.Deserialize("KeenEyes.Common.Transform3D", json.Value);
        var transform = Assert.IsType<Transform3D>(restored);

        Assert.Equal(original.Position.X, transform.Position.X, 5);
        Assert.Equal(original.Position.Y, transform.Position.Y, 5);
        Assert.Equal(original.Position.Z, transform.Position.Z, 5);
        Assert.Equal(original.Rotation.X, transform.Rotation.X, 5);
        Assert.Equal(original.Rotation.Y, transform.Rotation.Y, 5);
        Assert.Equal(original.Rotation.Z, transform.Rotation.Z, 5);
        Assert.Equal(original.Rotation.W, transform.Rotation.W, 5);
        Assert.Equal(original.Scale.X, transform.Scale.X, 5);
        Assert.Equal(original.Scale.Y, transform.Scale.Y, 5);
        Assert.Equal(original.Scale.Z, transform.Scale.Z, 5);
    }

    [Fact]
    public void WriteTo_WithTransform3D_RoundTripsThroughBinaryFormat()
    {
        IBinaryComponentSerializer serializer = ComponentSerializer.Instance;
        var original = new Transform3D(
            new Vector3(1f, 2f, 3f),
            new Quaternion(0.1f, 0.2f, 0.3f, 0.9f),
            new Vector3(4f, 5f, 6f));

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            Assert.True(serializer.WriteTo(typeof(Transform3D), original, writer));
        }

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var restored = serializer.ReadFrom("KeenEyes.Common.Transform3D", reader);
        var transform = Assert.IsType<Transform3D>(restored);

        Assert.Equal(original.Position.X, transform.Position.X, 5);
        Assert.Equal(original.Position.Y, transform.Position.Y, 5);
        Assert.Equal(original.Position.Z, transform.Position.Z, 5);
        Assert.Equal(original.Rotation.W, transform.Rotation.W, 5);
        Assert.Equal(original.Scale.Z, transform.Scale.Z, 5);
    }

    #endregion

    #region Ghost Pipeline Integration Tests

    [Fact]
    public void ExtractGhost_FromRecordingWithGeneratedSerializer_ProducesTransformFrames()
    {
        var options = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromMilliseconds(10),
            RecordSystemEvents = false,
            RecordComponentEvents = false,
            RecordEntityEvents = false,
        };

        using var world = new World();
        world.InstallPlugin(new ReplayPlugin(ComponentSerializer.Instance, options));

        var rotation = Quaternion.CreateFromYawPitchRoll(0.5f, 0f, 0f);
        var car = world.Spawn("Car")
            .With(new Transform3D(Vector3.Zero, rotation, Vector3.One))
            .Build();

        var recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording("engine-component-test");

        // Drive the car along +X; every update crosses the snapshot interval.
        for (var frame = 1; frame <= 20; frame++)
        {
            world.Get<Transform3D>(car).Position = new Vector3(frame * 0.5f, 0f, 0f);
            world.Update(0.02f);
        }

        var replay = recorder.StopRecording();
        Assert.NotNull(replay);

        var extractor = new GhostExtractor();
        var ghost = extractor.ExtractGhost(replay, "Car");

        // Without engine component serialization the extractor finds no Transform3D
        // data and returns null - this is the regression the opt-in attribute fixes.
        Assert.NotNull(ghost);
        Assert.True(ghost.FrameCount >= 2);

        // Positions advance along +X exactly as driven.
        var first = ghost.Frames[0];
        var last = ghost.Frames[^1];
        Assert.True(last.Position.X > first.Position.X);
        Assert.Equal(0f, last.Position.Y, 3);
        Assert.Equal(0f, last.Position.Z, 3);

        // Rotation and scale survive the JSON round-trip on every frame.
        foreach (var frame in ghost.Frames)
        {
            Assert.Equal(rotation.W, frame.Rotation.W, 3);
            Assert.Equal(rotation.Y, frame.Rotation.Y, 3);
            Assert.Equal(1f, frame.Scale.X, 3);
        }
    }

    #endregion
}
