using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="InstanceBatchingSystem"/> GPU buffer capacity bookkeeping.
/// </summary>
public class InstanceBatchingSystemTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    private void SpawnInstances(int meshId, int batchId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            world!.Spawn()
                .With(new Transform3D(new Vector3(i, 0, 0), Quaternion.Identity, Vector3.One))
                .With(new Renderable(meshId, 0))
                .With(new InstanceBatch(batchId))
                .Build();
        }
    }

    /// <summary>
    /// Regression test for #1184: a batch that grows after a different batch has already
    /// enlarged the shared CPU staging array must still resize its own GPU buffer.
    /// </summary>
    /// <remarks>
    /// With the pre-fix code, capacity was gated on the shared, only-growing
    /// <c>uploadBuffer.Length</c>. Once a large batch inflated that array, a smaller batch
    /// that later grew past its own GPU capacity (but stayed under the staging length) was
    /// never resized, leaving its GPU buffer too small for the uploaded data.
    /// </remarks>
    [Fact]
    public void Update_SecondBatchGrowsAfterAnotherEnlargedStaging_ResizesOwnGpuBuffer()
    {
        world = new World();
        var context = new MockGraphicsContext();
        world.SetExtension<IGraphicsContext>(context);

        var system = new InstanceBatchingSystem();
        world.AddSystem(system);

        // Frame 1: a large batch (A) inflates the shared staging buffer well past the
        // initial capacity, while a small batch (B) gets a small GPU buffer.
        const int largeCount = 600;
        const int smallCount = 130;
        SpawnInstances(meshId: 1, batchId: 1, count: largeCount);
        SpawnInstances(meshId: 1, batchId: 2, count: smallCount);
        system.Update(1f / 60f);

        // Frame 2: batch B grows past its GPU capacity but stays below the staging length
        // that batch A already grew to.
        const int grownCount = 500;
        SpawnInstances(meshId: 1, batchId: 2, count: grownCount - smallCount);
        system.Update(1f / 60f);

        // Every batch's GPU buffer must be able to hold the instances it reports uploading.
        Assert.NotEmpty(system.Batches);
        foreach (var batch in system.Batches)
        {
            var bufferInfo = context.InstanceBuffers[batch.InstanceBuffer];
            Assert.True(
                bufferInfo.MaxInstances >= batch.InstanceCount,
                $"GPU buffer capacity ({bufferInfo.MaxInstances}) is smaller than the batch " +
                $"instance count ({batch.InstanceCount}).");
        }

        // And specifically, the grown batch must have kept up.
        var grownBatch = system.Batches.Single(b => b.InstanceCount == grownCount);
        Assert.True(context.InstanceBuffers[grownBatch.InstanceBuffer].MaxInstances >= grownCount);
    }

    /// <summary>
    /// A single batch that grows across frames must resize its GPU buffer to fit.
    /// </summary>
    [Fact]
    public void Update_SingleBatchGrows_ResizesGpuBuffer()
    {
        world = new World();
        var context = new MockGraphicsContext();
        world.SetExtension<IGraphicsContext>(context);

        var system = new InstanceBatchingSystem();
        world.AddSystem(system);

        SpawnInstances(meshId: 1, batchId: 1, count: 100);
        system.Update(1f / 60f);

        SpawnInstances(meshId: 1, batchId: 1, count: 900);
        system.Update(1f / 60f);

        var batch = system.Batches.Single();
        Assert.Equal(1000, batch.InstanceCount);
        Assert.True(context.InstanceBuffers[batch.InstanceBuffer].MaxInstances >= 1000);
    }
}
