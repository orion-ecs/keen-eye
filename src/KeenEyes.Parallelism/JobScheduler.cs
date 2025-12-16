using System.Collections.Concurrent;

namespace KeenEyes.Parallelism;

/// <summary>
/// Schedules and executes jobs across worker threads with support for dependencies.
/// </summary>
/// <remarks>
/// <para>
/// The job scheduler provides fine-grained control over parallel work distribution.
/// It manages a queue of pending jobs and dispatches them to worker threads while
/// respecting dependency chains.
/// </para>
/// <para>
/// Jobs can be scheduled with dependencies on other jobs, creating execution chains.
/// A job will not begin execution until all its dependencies have completed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var scheduler = new JobScheduler();
///
/// // Schedule a simple job
/// var handle1 = scheduler.Schedule(new MyJob { Data = data1 });
///
/// // Schedule a job that depends on the first
/// var handle2 = scheduler.Schedule(new ProcessResultsJob(), handle1);
///
/// // Wait for all work to complete
/// handle2.Complete();
/// </code>
/// </example>
public sealed class JobScheduler : IDisposable
{
    private readonly ConcurrentQueue<ScheduledJob> jobQueue = new();
    private readonly ConcurrentDictionary<int, JobCompletionSource> activeJobs = new();
    private readonly ParallelOptions parallelOptions;
    private int nextJobId;
    private volatile bool isDisposed;

    /// <summary>
    /// Gets the number of jobs currently queued for execution.
    /// </summary>
    public int PendingJobCount => jobQueue.Count;

    /// <summary>
    /// Gets the number of jobs currently being executed.
    /// </summary>
    public int ActiveJobCount => activeJobs.Count;

    /// <summary>
    /// Creates a new job scheduler with default options.
    /// </summary>
    public JobScheduler() : this(null)
    {
    }

    /// <summary>
    /// Creates a new job scheduler with the specified options.
    /// </summary>
    /// <param name="options">Options controlling parallel execution.</param>
    public JobScheduler(JobSchedulerOptions? options)
    {
        var opts = options ?? new JobSchedulerOptions();
        parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = opts.MaxDegreeOfParallelism
        };
    }

    /// <summary>
    /// Schedules a job for execution.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <returns>A handle for tracking job completion.</returns>
    /// <example>
    /// <code>
    /// var job = new MyProcessingJob { Items = items };
    /// var handle = scheduler.Schedule(job);
    /// handle.Complete(); // Wait for job to finish
    /// </code>
    /// </example>
    public JobHandle Schedule<T>(T job) where T : IJob
    {
        return Schedule(job, JobHandle.Completed);
    }

    /// <summary>
    /// Schedules a job for execution after a dependency completes.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="dependency">The job that must complete before this job can run.</param>
    /// <returns>A handle for tracking job completion.</returns>
    /// <example>
    /// <code>
    /// var handle1 = scheduler.Schedule(new FirstJob());
    /// var handle2 = scheduler.Schedule(new SecondJob(), handle1); // Runs after FirstJob
    /// handle2.Complete();
    /// </code>
    /// </example>
    public JobHandle Schedule<T>(T job, JobHandle dependency) where T : IJob
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        var jobId = Interlocked.Increment(ref nextJobId);
        var source = new JobCompletionSource();
        activeJobs[jobId] = source;

        var scheduled = new ScheduledJob(
            jobId,
            source,
            () => job.Execute(),
            dependency);

        jobQueue.Enqueue(scheduled);

        // Always queue processing - don't block ThreadPool threads waiting for dependencies
        ThreadPool.QueueUserWorkItem(_ => ProcessQueue());

        return new JobHandle(jobId, source);
    }

    /// <summary>
    /// Schedules a parallel job to execute across multiple indices.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="count">The number of iterations to execute.</param>
    /// <returns>A handle for tracking job completion.</returns>
    /// <example>
    /// <code>
    /// var job = new UpdatePositionsJob { Positions = positions };
    /// var handle = scheduler.ScheduleParallel(job, positions.Length);
    /// handle.Complete();
    /// </code>
    /// </example>
    public JobHandle ScheduleParallel<T>(T job, int count) where T : IParallelJob
    {
        return ScheduleParallel(job, count, JobHandle.Completed);
    }

    /// <summary>
    /// Schedules a parallel job to execute across multiple indices after a dependency completes.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="count">The number of iterations to execute.</param>
    /// <param name="dependency">The job that must complete before this job can run.</param>
    /// <returns>A handle for tracking job completion.</returns>
    public JobHandle ScheduleParallel<T>(T job, int count, JobHandle dependency) where T : IParallelJob
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        if (count <= 0)
        {
            return JobHandle.Completed;
        }

        var jobId = Interlocked.Increment(ref nextJobId);
        var source = new JobCompletionSource();
        activeJobs[jobId] = source;

        var scheduled = new ScheduledJob(
            jobId,
            source,
            () => ExecuteParallelJob(job, count),
            dependency);

        jobQueue.Enqueue(scheduled);

        // Always queue processing - don't block ThreadPool threads waiting for dependencies
        ThreadPool.QueueUserWorkItem(_ => ProcessQueue());

        return new JobHandle(jobId, source);
    }

    /// <summary>
    /// Schedules a batch job to execute across ranges of indices.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="count">The total number of items to process.</param>
    /// <param name="batchSize">The size of each batch. If -1, batch size is auto-determined.</param>
    /// <returns>A handle for tracking job completion.</returns>
    /// <example>
    /// <code>
    /// var job = new ProcessChunkJob { Data = data };
    /// var handle = scheduler.ScheduleBatch(job, data.Length, batchSize: 64);
    /// handle.Complete();
    /// </code>
    /// </example>
    public JobHandle ScheduleBatch<T>(T job, int count, int batchSize = -1) where T : IBatchJob
    {
        return ScheduleBatch(job, count, batchSize, JobHandle.Completed);
    }

    /// <summary>
    /// Schedules a batch job to execute across ranges of indices after a dependency completes.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="count">The total number of items to process.</param>
    /// <param name="batchSize">The size of each batch. If -1, batch size is auto-determined.</param>
    /// <param name="dependency">The job that must complete before this job can run.</param>
    /// <returns>A handle for tracking job completion.</returns>
    public JobHandle ScheduleBatch<T>(T job, int count, int batchSize, JobHandle dependency) where T : IBatchJob
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);

        if (count <= 0)
        {
            return JobHandle.Completed;
        }

        // Auto-determine batch size based on processor count
        if (batchSize <= 0)
        {
            batchSize = Math.Max(1, count / (Environment.ProcessorCount * 4));
        }

        var jobId = Interlocked.Increment(ref nextJobId);
        var source = new JobCompletionSource();
        activeJobs[jobId] = source;

        var scheduled = new ScheduledJob(
            jobId,
            source,
            () => ExecuteBatchJob(job, count, batchSize),
            dependency);

        jobQueue.Enqueue(scheduled);

        // Always queue processing - don't block ThreadPool threads waiting for dependencies
        ThreadPool.QueueUserWorkItem(_ => ProcessQueue());

        return new JobHandle(jobId, source);
    }

    /// <summary>
    /// Completes all pending and active jobs, blocking until finished.
    /// </summary>
    /// <remarks>
    /// This method processes all queued jobs and waits for completion.
    /// Useful for ensuring all work is done before continuing.
    /// </remarks>
    public void CompleteAll()
    {
        CompleteAll(Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Completes all pending and active jobs, blocking until finished or timeout expires.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for jobs to complete.</param>
    /// <returns>True if all jobs completed within the timeout; false otherwise.</returns>
    /// <remarks>
    /// This method processes all queued jobs and waits for completion.
    /// Useful for ensuring all work is done before continuing with a bounded wait time.
    /// </remarks>
    public bool CompleteAll(TimeSpan timeout)
    {
        var deadline = timeout == Timeout.InfiniteTimeSpan
            ? DateTime.MaxValue
            : DateTime.UtcNow + timeout;

        // Process remaining queue items, handling dependencies
        var pendingWithDeps = new List<ScheduledJob>();

        while (true)
        {
            // Check timeout
            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            // Drain current queue
            while (jobQueue.TryDequeue(out var scheduled))
            {
                if (scheduled.Dependency.IsCompleted)
                {
                    ExecuteScheduledJob(scheduled);
                }
                else
                {
                    pendingWithDeps.Add(scheduled);
                }
            }

            // If nothing pending, we're done with queue processing
            if (pendingWithDeps.Count == 0)
            {
                break;
            }

            // Re-queue items with pending dependencies and retry
            foreach (var job in pendingWithDeps)
            {
                jobQueue.Enqueue(job);
            }

            pendingWithDeps.Clear();

            // Use Sleep(1) instead of Yield() - on Windows, Sleep(1) actually
            // releases the time slice and allows other threads to make progress
            Thread.Sleep(1);
        }

        // Wait for any active jobs with timeout
        foreach (var kvp in activeJobs)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            if (!kvp.Value.Wait(remaining))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Releases resources used by the scheduler.
    /// </summary>
    /// <remarks>
    /// Waits up to 5 seconds for all pending jobs to complete before disposing.
    /// Jobs that do not complete within this timeout will be abandoned.
    /// </remarks>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        // Use a reasonable timeout to prevent test hangs when ThreadPool is contended.
        // In production, CompleteAll() should be called explicitly before Dispose if
        // all jobs must complete.
        CompleteAll(TimeSpan.FromSeconds(5));
        activeJobs.Clear();
    }

    private void ProcessQueue()
    {
        var retryCount = 0;
        const int maxRetries = 100; // Limit retries to prevent infinite loops

        while (jobQueue.TryDequeue(out var scheduled))
        {
            // If dependency isn't ready, re-queue and continue
            if (!scheduled.Dependency.IsCompleted)
            {
                jobQueue.Enqueue(scheduled);
                retryCount++;

                // If we've retried too many times, exit and let other work items continue
                if (retryCount > maxRetries)
                {
                    return;
                }

                // Use Sleep(1) instead of Yield() - on Windows, Sleep(1) actually
                // releases the time slice and allows other threads to make progress
                Thread.Sleep(1);
                continue;
            }

            retryCount = 0; // Reset on successful execution
            ExecuteScheduledJob(scheduled);
        }
    }

    private void ExecuteScheduledJob(ScheduledJob scheduled)
    {
        try
        {
            scheduled.Action();
            scheduled.CompletionSource.SetCompleted();
        }
        catch (Exception ex)
        {
            scheduled.CompletionSource.SetFaulted(ex);
        }
        finally
        {
            activeJobs.TryRemove(scheduled.Id, out _);
        }
    }

    private void ExecuteParallelJob<T>(T job, int count) where T : IParallelJob
    {
        Parallel.For(0, count, parallelOptions, i => job.Execute(i));
    }

    private void ExecuteBatchJob<T>(T job, int count, int batchSize) where T : IBatchJob
    {
        var batchCount = (count + batchSize - 1) / batchSize;

        Parallel.For(0, batchCount, parallelOptions, batchIndex =>
        {
            var startIndex = batchIndex * batchSize;
            var actualBatchSize = Math.Min(batchSize, count - startIndex);
            job.Execute(startIndex, actualBatchSize);
        });
    }

    private readonly record struct ScheduledJob(
        int Id,
        JobCompletionSource CompletionSource,
        Action Action,
        JobHandle Dependency);
}

/// <summary>
/// Configuration options for the job scheduler.
/// </summary>
public sealed record JobSchedulerOptions
{
    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// </summary>
    /// <remarks>
    /// Use -1 (default) to allow the scheduler to use all available processors.
    /// Values greater than 0 limit the number of concurrent operations.
    /// </remarks>
    public int MaxDegreeOfParallelism { get; init; } = -1;
}
