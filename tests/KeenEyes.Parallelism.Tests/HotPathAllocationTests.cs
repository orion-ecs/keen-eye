using KeenEyes.Parallelism;

namespace KeenEyes.Tests;

/// <summary>
/// Behavior-preservation tests for the parallel hot-path allocation reductions (issue #1238):
/// the pre-sized chunk buffer in ParallelQueryExtensions.CollectChunks and the reused
/// per-batch scratch buffers in ParallelSystemScheduler.ExecuteBatch. These assert that
/// reusing/pre-sizing buffers does not change query coverage or command-buffer flushing.
/// </summary>
[Collection("ParallelismTests")]
public class HotPathAllocationTests
{
    #region Test Components

    private struct HotPosition : IComponent
    {
        public float X;
        public float Y;
    }

    private struct HotSpawned : IComponent
    {
        public int Value;
    }

    #endregion

    #region CollectChunks (pre-sized buffer)

    [Fact]
    public void ForEachParallel_AcrossManyChunks_ProcessesEveryEntityExactlyOnce()
    {
        using var world = new World();

        // Spawn enough entities to span many chunks so CollectChunks collects a large buffer.
        const int entityCount = 5000;
        for (int i = 0; i < entityCount; i++)
        {
            world.Spawn().With(new HotPosition { X = i, Y = 0 }).Build();
        }

        var processed = new int[entityCount];

        world.Query<HotPosition>().ForEachParallel<HotPosition>(
            (Entity entity, ref HotPosition pos) =>
            {
                Interlocked.Increment(ref processed[(int)pos.X]);
            },
            minEntityCount: 1); // Force the parallel path.

        // Every entity processed exactly once - no chunk dropped or double-counted.
        Assert.All(processed, count => Assert.Equal(1, count));
    }

    [Fact]
    public void ForEachParallel_EmptyResult_DoesNotThrow()
    {
        using var world = new World();

        var invoked = 0;
        world.Query<HotPosition>().ForEachParallel<HotPosition>(
            (Entity entity, ref HotPosition pos) => Interlocked.Increment(ref invoked),
            minEntityCount: 1);

        Assert.Equal(0, invoked);
    }

    #endregion

    #region ExecuteBatch (reused scratch buffers)

    private sealed class SpawningSystem : SystemBase, ICommandBufferConsumer, ISystemDependencyProvider
    {
        private ICommandBuffer? commandBuffer;

        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            builder.Writes<HotSpawned>();
        }

        public void SetCommandBuffer(ICommandBuffer buffer) => commandBuffer = buffer;

        public override void Update(float deltaTime)
        {
            commandBuffer?.Spawn().With(new HotSpawned { Value = 1 });
        }
    }

    private sealed class SecondSpawningSystem : SystemBase, ICommandBufferConsumer, ISystemDependencyProvider
    {
        private ICommandBuffer? commandBuffer;

        public void GetDependencies(ISystemDependencyBuilder builder)
        {
            // No declared conflict with SpawningSystem, so the two may share or split batches;
            // either way both command buffers must flush each frame.
            builder.Writes<HotPosition>();
        }

        public void SetCommandBuffer(ICommandBuffer buffer) => commandBuffer = buffer;

        public override void Update(float deltaTime)
        {
            commandBuffer?.Spawn().With(new HotSpawned { Value = 2 });
        }
    }

    [Fact]
    public void UpdateParallel_MultipleFrames_FlushesCommandBuffersEachFrame()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new SpawningSystem();
        system.Initialize(world);
        scheduler.RegisterSystem(system);

        const int frames = 5;
        for (int i = 0; i < frames; i++)
        {
            scheduler.UpdateParallel(0.016f);
        }

        // One entity spawned and flushed per frame; reusing the scratch bufferIds across frames
        // must not drop or duplicate flushes.
        var count = 0;
        foreach (var _ in world.Query<HotSpawned>())
        {
            count++;
        }

        Assert.Equal(frames, count);
    }

    [Fact]
    public void UpdateParallel_MultipleSystemsMultipleFrames_FlushesAllBuffers()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var first = new SpawningSystem();
        var second = new SecondSpawningSystem();
        first.Initialize(world);
        second.Initialize(world);
        scheduler.RegisterSystem(first);
        scheduler.RegisterSystem(second);

        const int frames = 4;
        for (int i = 0; i < frames; i++)
        {
            scheduler.UpdateParallel(0.016f);
        }

        // Two systems each spawn one entity per frame; the reused batch wrapper must flush every
        // system's buffer across every batch and frame.
        var count = 0;
        foreach (var _ in world.Query<HotSpawned>())
        {
            count++;
        }

        Assert.Equal(frames * 2, count);
    }

    #endregion
}
