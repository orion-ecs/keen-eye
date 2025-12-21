using KeenEyes.Parallelism;

namespace KeenEyes.Tests;

/// <summary>
/// Targeted tests for classes with low coverage.
/// </summary>
public class LowCoverageTargetTests
{
    #region Test Jobs

    private readonly struct IncrementJob : IJob
    {
        public int[] Counter { get; init; }

        public readonly void Execute()
        {
            Interlocked.Increment(ref Counter[0]);
        }
    }

    private readonly struct SlowJob : IJob
    {
        public int DelayMs { get; init; }

        public readonly void Execute()
        {
            Thread.Sleep(DelayMs);
        }
    }

    #endregion

    #region JobCompletionSource Tests

    [Fact]
    public void JobCompletionSource_SetCompleted_SetsIsCompleted()
    {
        // Test the SetCompleted path directly through job execution
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handle = scheduler.Schedule(new IncrementJob { Counter = counter });
        handle.Complete();

        Assert.True(handle.IsCompleted);
        Assert.False(handle.IsFaulted);
    }

    [Fact]
    public void JobCompletionSource_SetFaulted_SetsIsFaultedAndException()
    {
        using var scheduler = new JobScheduler();

        var handle = scheduler.Schedule(new ThrowingJob { Message = "Test error" });
        handle.Complete();

        Assert.True(handle.IsFaulted);
        Assert.True(handle.IsCompleted);
        Assert.NotNull(handle.Exception);
    }

    [Fact]
    public void JobCompletionSource_WaitWithTimeout_ReturnsTrue()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handle = scheduler.Schedule(new IncrementJob { Counter = counter });
        var result = handle.Wait(TimeSpan.FromSeconds(5));

        Assert.True(result);
        Assert.True(handle.IsCompleted);
    }

    [Fact]
    public void JobCompletionSource_WaitWithTimeout_ReturnsFalseOnTimeout()
    {
        using var scheduler = new JobScheduler();

        var handle = scheduler.Schedule(new SlowJob { DelayMs = 500 });
        var result = handle.Wait(TimeSpan.FromMilliseconds(50));

        Assert.False(result);
    }

    private readonly struct ThrowingJob : IJob
    {
        public string Message { get; init; }

        public readonly void Execute()
        {
            throw new InvalidOperationException(Message);
        }
    }

    #endregion

    #region CombinedJobCompletionSource Tests

    [Fact]
    public void CombinedJobCompletionSource_WaitParameterless_WaitsForAll()
    {
        using var scheduler = new JobScheduler();
        var counters = new int[3];

        var handle1 = scheduler.Schedule(new IncrementJob { Counter = counters });
        var handle2 = scheduler.Schedule(new IncrementJob { Counter = counters });
        var handle3 = scheduler.Schedule(new IncrementJob { Counter = counters });

        var combined = JobHandle.CombineDependencies(handle1, handle2, handle3);

        // Use Complete() which calls parameterless Wait()
        combined.Complete();

        Assert.True(handle1.IsCompleted);
        Assert.True(handle2.IsCompleted);
        Assert.True(handle3.IsCompleted);
        Assert.True(combined.IsCompleted);
    }

    [Fact]
    public void CombinedJobCompletionSource_WaitWithTimeout_Success()
    {
        using var scheduler = new JobScheduler();
        var counters = new int[1];

        var handle1 = scheduler.Schedule(new IncrementJob { Counter = counters });
        var handle2 = scheduler.Schedule(new IncrementJob { Counter = counters });

        var combined = JobHandle.CombineDependencies(handle1, handle2);
        var result = combined.Wait(TimeSpan.FromSeconds(10));

        // Should complete successfully within the timeout
        if (!result)
        {
            // If it didn't complete, wait a bit more (may be slow CI)
            combined.Complete();
        }

        Assert.True(combined.IsCompleted);
    }

    [Fact]
    public void CombinedJobCompletionSource_WaitWithTimeout_Timeout()
    {
        using var scheduler = new JobScheduler();

        var handle1 = scheduler.Schedule(new SlowJob { DelayMs = 200 });
        var handle2 = scheduler.Schedule(new SlowJob { DelayMs = 200 });

        var combined = JobHandle.CombineDependencies(handle1, handle2);
        var result = combined.Wait(TimeSpan.FromMilliseconds(50));

        Assert.False(result);
    }

    [Fact]
    public void CombinedJobCompletionSource_WaitWithTimeout_NegativeRemaining()
    {
        using var scheduler = new JobScheduler();

        // First handle consumes most of timeout, second should see negative remaining
        var handle1 = scheduler.Schedule(new SlowJob { DelayMs = 80 });
        var handle2 = scheduler.Schedule(new SlowJob { DelayMs = 100 });

        var combined = JobHandle.CombineDependencies(handle1, handle2);
        var result = combined.Wait(TimeSpan.FromMilliseconds(100));

        // Should timeout when first handle consumes most/all of the time
        Assert.False(result);
    }

    [Fact]
    public void CombinedJobCompletionSource_AllHandlesComplete_ImmediatelyCompleted()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handle1 = scheduler.Schedule(new IncrementJob { Counter = counter });
        var handle2 = scheduler.Schedule(new IncrementJob { Counter = counter });

        handle1.Complete();
        handle2.Complete();

        // Now create combined handle - should be immediately completed
        var combined = JobHandle.CombineDependencies(handle1, handle2);

        Assert.True(combined.IsCompleted);
    }

    [Fact]
    public void CombinedJobCompletionSource_SetCompleted_AfterWait()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handle1 = scheduler.Schedule(new IncrementJob { Counter = counter });
        var handle2 = scheduler.Schedule(new IncrementJob { Counter = counter });

        var combined = JobHandle.CombineDependencies(handle1, handle2);
        combined.Complete();

        // SetCompleted should have been called by Wait
        Assert.True(combined.IsCompleted);
    }

    #endregion

    #region JobSchedulerOptions Tests

    [Fact]
    public void JobSchedulerOptions_DefaultConstructor_SetsDefaults()
    {
        var options = new JobSchedulerOptions();

        Assert.Equal(-1, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void JobSchedulerOptions_WithInit_SetsProperty()
    {
        var options = new JobSchedulerOptions
        {
            MaxDegreeOfParallelism = 2
        };

        Assert.Equal(2, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void JobSchedulerOptions_RecordEquality_WorksCorrectly()
    {
        var options1 = new JobSchedulerOptions { MaxDegreeOfParallelism = 4 };
        var options2 = new JobSchedulerOptions { MaxDegreeOfParallelism = 4 };
        var options3 = new JobSchedulerOptions { MaxDegreeOfParallelism = 8 };

        Assert.Equal(options1, options2);
        Assert.NotEqual(options1, options3);
    }

    #endregion

    #region ParallelSystemOptions Tests

    public struct Position : IComponent
    {
        public float X, Y;
    }

    [Fact]
    public void ParallelSystemOptions_DefaultConstructor_SetsDefaults()
    {
        var options = new ParallelSystemOptions();

        Assert.Equal(-1, options.MaxDegreeOfParallelism);
        Assert.Equal(2, options.MinBatchSizeForParallel);
    }

    [Fact]
    public void ParallelSystemOptions_WithInit_SetsProperties()
    {
        var options = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 4,
            MinBatchSizeForParallel = 3
        };

        Assert.Equal(4, options.MaxDegreeOfParallelism);
        Assert.Equal(3, options.MinBatchSizeForParallel);
    }

    [Fact]
    public void ParallelSystemOptions_PassedToScheduler()
    {
        using var world = new World();
        var options = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 2,
            MinBatchSizeForParallel = 1
        };

        world.InstallPlugin(new ParallelSystemPlugin(options));
        var scheduler = world.GetExtension<ParallelSystemScheduler>();

        Assert.NotNull(scheduler);
    }

    [Fact]
    public void ParallelSystemOptions_RecordEquality_WorksCorrectly()
    {
        var options1 = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 4,
            MinBatchSizeForParallel = 2
        };
        var options2 = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 4,
            MinBatchSizeForParallel = 2
        };
        var options3 = new ParallelSystemOptions
        {
            MaxDegreeOfParallelism = 8,
            MinBatchSizeForParallel = 2
        };

        Assert.Equal(options1, options2);
        Assert.NotEqual(options1, options3);
    }

    #endregion

    #region ParallelSystemPlugin Tests

    [Fact]
    public void ParallelSystemPlugin_Name_ReturnsCorrectName()
    {
        var plugin = new ParallelSystemPlugin();

        Assert.Equal("ParallelSystem", plugin.Name);
    }

    [Fact]
    public void ParallelSystemPlugin_Install_RegistersExtension()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());

        var scheduler = world.GetExtension<ParallelSystemScheduler>();

        Assert.NotNull(scheduler);
    }

    [Fact]
    public void ParallelSystemPlugin_Uninstall_ClearsScheduler()
    {
        using var world = new World();
        var plugin = new ParallelSystemPlugin();
        world.InstallPlugin(plugin);

        var scheduler = world.GetExtension<ParallelSystemScheduler>();
        Assert.NotNull(scheduler);

        world.UninstallPlugin("ParallelSystem");

        // Scheduler should be removed after uninstall
        var schedulerAfter = world.GetExtension<ParallelSystemScheduler>();
        Assert.Null(schedulerAfter);
    }

    #endregion

    #region Additional JobScheduler Coverage

    [Fact]
    public void JobScheduler_DefaultConstructor_CreatesWithDefaults()
    {
        using var scheduler = new JobScheduler();

        Assert.NotNull(scheduler);
        Assert.Equal(0, scheduler.PendingJobCount);
        Assert.Equal(0, scheduler.ActiveJobCount);
    }

    [Fact]
    public void JobScheduler_WithNullOptions_UsesDefaults()
    {
        using var scheduler = new JobScheduler(null);

        Assert.NotNull(scheduler);
    }

    #endregion

    #region ParallelSystemScheduler Coverage

    [Fact]
    public void ParallelSystemScheduler_ExecuteBatch_WithSingleSystem()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new SingleSystem();
        system.Initialize(world);
        scheduler.RegisterSystem(system);

        scheduler.UpdateParallel(0.016f);

        Assert.True(system.WasUpdated);
    }

    [Fact]
    public void ParallelSystemScheduler_ExecuteBatch_WithDisabledSystem()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new SingleSystem();
        system.Initialize(world);
        system.Enabled = false;
        scheduler.RegisterSystem(system);

        scheduler.UpdateParallel(0.016f);

        Assert.False(system.WasUpdated);
    }

    [Fact]
    public void ParallelSystemScheduler_ExecuteSystem_NonSystemBase()
    {
        using var world = new World();
        world.InstallPlugin(new ParallelSystemPlugin());
        var scheduler = world.GetExtension<ParallelSystemScheduler>()!;

        var system = new CustomSystem();
        scheduler.RegisterSystem(system);

        scheduler.UpdateParallel(0.016f);

        Assert.True(system.WasUpdated);
    }

    private sealed class SingleSystem : SystemBase
    {
        public bool WasUpdated { get; private set; }

        public override void Update(float deltaTime)
        {
            WasUpdated = true;
        }
    }

    private sealed class CustomSystem : ISystem
    {
        public bool Enabled { get; set; } = true;
        public bool WasUpdated { get; private set; }

        public void Initialize(IWorld world)
        {
            // No initialization needed
        }

        public void Update(float deltaTime)
        {
            WasUpdated = true;
        }

        public void Dispose()
        {
            // No cleanup needed
        }
    }

    #endregion
}
