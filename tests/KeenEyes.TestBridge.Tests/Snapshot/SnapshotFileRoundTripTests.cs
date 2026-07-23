using System.Linq;

namespace KeenEyes.TestBridge.Tests.Snapshot;

/// <summary>
/// Regression tests for snapshot persistence to and from disk (issue #1176).
/// </summary>
public class SnapshotFileRoundTripTests
{
    [Fact]
    public async Task RestoreAsync_AfterFileRoundTrip_PreservesNestedStructAndArrayFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"keeneyes-snapshot-{Guid.NewGuid():N}.json");

        try
        {
            using var world = new World();
            world.Spawn()
                .With(new TestComplexComponent
                {
                    Flat = 7,
                    Nested = new TestNested { A = 3, B = 4 },
                    Values = [1, 2, 3]
                })
                .Build();

            using var bridge = new InProcessBridge(world);

            var create = await bridge.Snapshot.CreateAsync("complex");
            create.Success.ShouldBeTrue();

            // Persist to disk and reload: this forces every component value
            // through JsonSerializer, turning nested structs and arrays into
            // JsonElement.Object / JsonElement.Array on the way back in.
            var save = await bridge.Snapshot.SaveToFileAsync("complex", path);
            save.Success.ShouldBeTrue();

            var load = await bridge.Snapshot.LoadFromFileAsync(path, "loaded");
            load.Success.ShouldBeTrue();

            var restore = await bridge.Snapshot.RestoreAsync("loaded");
            restore.Success.ShouldBeTrue();

            var entity = world.Query<TestComplexComponent>().Single();
            ref readonly var restored = ref world.Get<TestComplexComponent>(entity);

            restored.Flat.ShouldBe(7);
            restored.Nested.A.ShouldBe(3);
            restored.Nested.B.ShouldBe(4);
            restored.Values.ShouldBe([1, 2, 3]);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
