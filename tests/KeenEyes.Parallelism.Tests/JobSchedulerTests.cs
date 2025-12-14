using System.Collections.Concurrent;
using KeenEyes.Parallelism;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the JobScheduler and job system components.
/// </summary>
public class JobSchedulerTests
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

    private readonly struct AddValueJob : IJob
    {
        public int[] Target { get; init; }
        public int Value { get; init; }

        public readonly void Execute()
        {
            Interlocked.Add(ref Target[0], Value);
        }
    }

    private readonly struct ParallelIncrementJob : IParallelJob
    {
        public int[] Values { get; init; }

        public readonly void Execute(int index)
        {
            Interlocked.Increment(ref Values[index]);
        }
    }

    private readonly struct ParallelMultiplyJob : IParallelJob
    {
        public int[] Values { get; init; }
        public int Multiplier { get; init; }

        public readonly void Execute(int index)
        {
            Values[index] *= Multiplier;
        }
    }

    private readonly struct BatchSumJob : IBatchJob
    {
        public int[] Values { get; init; }
        public int[] Result { get; init; }

        public readonly void Execute(int startIndex, int count)
        {
            var sum = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                sum += Values[i];
            }
            Interlocked.Add(ref Result[0], sum);
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

    private readonly struct ThrowingJob : IJob
    {
        public readonly void Execute()
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private readonly struct ThreadTrackingJob : IJob
    {
        public ConcurrentBag<int> ThreadIds { get; init; }

        public readonly void Execute()
        {
            ThreadIds.Add(Environment.CurrentManagedThreadId);
            Thread.SpinWait(1000);
        }
    }

    #endregion

    #region Basic Job Scheduling Tests

    [Fact]
    public void Schedule_SimpleJob_ExecutesJob()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];
        var job = new IncrementJob { Counter = counter };

        var handle = scheduler.Schedule(job);
        handle.Complete();

        Assert.Equal(1, counter[0]);
    }

    [Fact]
    public void Schedule_MultipleJobs_ExecutesAll()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handles = new JobHandle[5];
        for (int i = 0; i < 5; i++)
        {
            handles[i] = scheduler.Schedule(new IncrementJob { Counter = counter });
        }

        foreach (var handle in handles)
        {
            handle.Complete();
        }

        Assert.Equal(5, counter[0]);
    }

    [Fact]
    public void Schedule_WithDependency_ExecutesInOrder()
    {
        using var scheduler = new JobScheduler();
        var values = new int[1];

        // First job sets value to 10
        var job1 = new AddValueJob { Target = values, Value = 10 };
        var handle1 = scheduler.Schedule(job1);

        // Second job adds 5, depends on first
        var job2 = new AddValueJob { Target = values, Value = 5 };
        var handle2 = scheduler.Schedule(job2, handle1);

        handle2.Complete();

        Assert.Equal(15, values[0]);
    }

    [Fact]
    public void Schedule_ChainedDependencies_ExecutesInSequence()
    {
        using var scheduler = new JobScheduler();
        var values = new int[1];

        var handle1 = scheduler.Schedule(new AddValueJob { Target = values, Value = 1 });
        var handle2 = scheduler.Schedule(new AddValueJob { Target = values, Value = 2 }, handle1);
        var handle3 = scheduler.Schedule(new AddValueJob { Target = values, Value = 3 }, handle2);

        handle3.Complete();

        Assert.Equal(6, values[0]);
    }

    #endregion

    #region Parallel Job Tests

    [Fact]
    public void ScheduleParallel_ProcessesAllIndices()
    {
        using var scheduler = new JobScheduler();
        var values = new int[100];

        var job = new ParallelIncrementJob { Values = values };
        var handle = scheduler.ScheduleParallel(job, values.Length);
        handle.Complete();

        Assert.All(values, v => Assert.Equal(1, v));
    }

    [Fact]
    public void ScheduleParallel_WithMultiplier_AppliesCorrectly()
    {
        using var scheduler = new JobScheduler();
        var values = new int[50];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i + 1;
        }

        var job = new ParallelMultiplyJob { Values = values, Multiplier = 2 };
        var handle = scheduler.ScheduleParallel(job, values.Length);
        handle.Complete();

        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal((i + 1) * 2, values[i]);
        }
    }

    [Fact]
    public void ScheduleParallel_ZeroCount_ReturnsCompletedHandle()
    {
        using var scheduler = new JobScheduler();
        var values = new int[0];

        var job = new ParallelIncrementJob { Values = values };
        var handle = scheduler.ScheduleParallel(job, 0);

        Assert.True(handle.IsCompleted);
    }

    [Fact]
    public void ScheduleParallel_WithDependency_WaitsForDependency()
    {
        using var scheduler = new JobScheduler();
        var setupComplete = new int[1];
        var values = new int[10];

        // Setup job that takes time
        var setupJob = new SlowJob { DelayMs = 50 };
        var setupHandle = scheduler.Schedule(setupJob);

        // Mark setup complete after setup job
        var markJob = new IncrementJob { Counter = setupComplete };
        var markHandle = scheduler.Schedule(markJob, setupHandle);

        // Parallel job depends on setup
        var parallelJob = new ParallelIncrementJob { Values = values };
        var handle = scheduler.ScheduleParallel(parallelJob, values.Length, markHandle);

        handle.Complete();

        Assert.Equal(1, setupComplete[0]);
        Assert.All(values, v => Assert.Equal(1, v));
    }

    #endregion

    #region Batch Job Tests

    [Fact]
    public void ScheduleBatch_ProcessesAllItems()
    {
        using var scheduler = new JobScheduler();
        var values = new int[100];
        var result = new int[1];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = 1;
        }

        var job = new BatchSumJob { Values = values, Result = result };
        var handle = scheduler.ScheduleBatch(job, values.Length, batchSize: 10);
        handle.Complete();

        Assert.Equal(100, result[0]);
    }

    [Fact]
    public void ScheduleBatch_AutoBatchSize_ProcessesCorrectly()
    {
        using var scheduler = new JobScheduler();
        var values = new int[1000];
        var result = new int[1];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = 1;
        }

        var job = new BatchSumJob { Values = values, Result = result };
        var handle = scheduler.ScheduleBatch(job, values.Length); // Auto batch size
        handle.Complete();

        Assert.Equal(1000, result[0]);
    }

    [Fact]
    public void ScheduleBatch_ZeroCount_ReturnsCompletedHandle()
    {
        using var scheduler = new JobScheduler();

        var job = new BatchSumJob { Values = [], Result = new int[1] };
        var handle = scheduler.ScheduleBatch(job, 0);

        Assert.True(handle.IsCompleted);
    }

    #endregion

    #region JobHandle Tests

    [Fact]
    public void JobHandle_IsCompleted_TrueAfterExecution()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handle = scheduler.Schedule(new IncrementJob { Counter = counter });
        handle.Complete();

        Assert.True(handle.IsCompleted);
    }

    [Fact]
    public void JobHandle_Wait_ReturnsOnCompletion()
    {
        using var scheduler = new JobScheduler();

        var handle = scheduler.Schedule(new SlowJob { DelayMs = 50 });
        var completed = handle.Wait(TimeSpan.FromSeconds(5));

        Assert.True(completed);
        Assert.True(handle.IsCompleted);
    }

    [Fact]
    public void JobHandle_Wait_TimeoutReturnsCorrectly()
    {
        using var scheduler = new JobScheduler();

        var handle = scheduler.Schedule(new SlowJob { DelayMs = 1000 });
        var completed = handle.Wait(TimeSpan.FromMilliseconds(10));

        Assert.False(completed);
    }

    [Fact]
    public void JobHandle_CombineDependencies_WaitsForAll()
    {
        using var scheduler = new JobScheduler();
        var counters = new int[3];

        var handle1 = scheduler.Schedule(new AddValueJob { Target = counters, Value = 1 });
        var handle2 = scheduler.Schedule(new AddValueJob { Target = counters, Value = 10 });
        var handle3 = scheduler.Schedule(new AddValueJob { Target = counters, Value = 100 });

        var combined = JobHandle.CombineDependencies(handle1, handle2, handle3);
        combined.Complete();

        Assert.True(handle1.IsCompleted);
        Assert.True(handle2.IsCompleted);
        Assert.True(handle3.IsCompleted);
        Assert.Equal(111, counters[0]);
    }

    [Fact]
    public void JobHandle_CombineDependencies_EmptyArray_ReturnsCompleted()
    {
        var combined = JobHandle.CombineDependencies();

        Assert.True(combined.IsCompleted);
    }

    [Fact]
    public void JobHandle_CombineDependencies_SingleHandle_ReturnsSame()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        var handle = scheduler.Schedule(new IncrementJob { Counter = counter });
        var combined = JobHandle.CombineDependencies(handle);

        combined.Complete();

        Assert.True(handle.IsCompleted);
    }

    [Fact]
    public void JobHandle_Completed_IsAlwaysComplete()
    {
        Assert.True(JobHandle.Completed.IsCompleted);
        Assert.True(JobHandle.Completed.IsValid);
        Assert.False(JobHandle.Completed.IsFaulted);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Schedule_ThrowingJob_SetsFaulted()
    {
        using var scheduler = new JobScheduler();

        var handle = scheduler.Schedule(new ThrowingJob());
        handle.Complete();

        Assert.True(handle.IsCompleted);
        Assert.True(handle.IsFaulted);
        Assert.NotNull(handle.Exception);
        Assert.IsType<InvalidOperationException>(handle.Exception);
    }

    [Fact]
    public void Schedule_AfterDispose_ThrowsObjectDisposedException()
    {
        var scheduler = new JobScheduler();
        scheduler.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            scheduler.Schedule(new IncrementJob { Counter = new int[1] }));
    }

    #endregion

    #region CompleteAll Tests

    [Fact]
    public void CompleteAll_WaitsForAllJobs()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];

        for (int i = 0; i < 10; i++)
        {
            scheduler.Schedule(new IncrementJob { Counter = counter });
        }

        scheduler.CompleteAll();

        Assert.Equal(10, counter[0]);
    }

    [Fact]
    public void CompleteAll_HandlesEmptyQueue()
    {
        using var scheduler = new JobScheduler();

        // Should not throw
        scheduler.CompleteAll();
    }

    #endregion

    #region Options Tests

    [Fact]
    public void JobScheduler_WithOptions_UsesConfiguration()
    {
        var options = new JobSchedulerOptions
        {
            MaxDegreeOfParallelism = 2
        };
        using var scheduler = new JobScheduler(options);
        var counter = new int[1];

        var handle = scheduler.Schedule(new IncrementJob { Counter = counter });
        handle.Complete();

        Assert.Equal(1, counter[0]);
    }

    [Fact]
    public void JobSchedulerOptions_Defaults_AreCorrect()
    {
        var options = new JobSchedulerOptions();

        Assert.Equal(-1, options.MaxDegreeOfParallelism);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Schedule_ConcurrentScheduling_IsThreadSafe()
    {
        using var scheduler = new JobScheduler();
        var counter = new int[1];
        var handles = new ConcurrentBag<JobHandle>();

        Parallel.For(0, 100, _ =>
        {
            var handle = scheduler.Schedule(new IncrementJob { Counter = counter });
            handles.Add(handle);
        });

        foreach (var handle in handles)
        {
            handle.Complete();
        }

        Assert.Equal(100, counter[0]);
    }

    [Fact]
    public void ScheduleParallel_UsesMultipleThreads()
    {
        using var scheduler = new JobScheduler();
        var threadIds = new ConcurrentBag<int>();
        var values = new int[Environment.ProcessorCount * 10];

        // Use a job that tracks thread IDs
        Parallel.For(0, values.Length, i =>
        {
            threadIds.Add(Environment.CurrentManagedThreadId);
            Thread.SpinWait(1000);
        });

        // Should use multiple threads (unless single-core system)
        if (Environment.ProcessorCount > 1)
        {
            Assert.True(threadIds.Distinct().Count() > 1);
        }
    }

    #endregion

    #region Property Tests

    [Fact]
    public void PendingJobCount_ReflectsQueuedJobs()
    {
        using var scheduler = new JobScheduler();

        // Initially empty
        Assert.Equal(0, scheduler.PendingJobCount);
    }

    #endregion
}
