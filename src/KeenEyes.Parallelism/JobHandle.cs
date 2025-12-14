using System.Collections.Concurrent;

namespace KeenEyes.Parallelism;

/// <summary>
/// Represents a handle to a scheduled job, enabling completion tracking and dependency chains.
/// </summary>
/// <remarks>
/// <para>
/// Job handles are returned when jobs are scheduled and provide a way to:
/// - Check if a job has completed
/// - Wait for job completion (blocking)
/// - Chain jobs together using dependencies
/// </para>
/// <para>
/// Job handles are lightweight value types that reference internal completion state.
/// Multiple handles can reference the same job.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Schedule a job and get a handle
/// var handle = scheduler.Schedule(myJob);
///
/// // Do other work...
///
/// // Wait for completion
/// handle.Complete();
///
/// // Or check without blocking
/// if (handle.IsCompleted)
/// {
///     // Job finished
/// }
/// </code>
/// </example>
public readonly struct JobHandle : IEquatable<JobHandle>
{
    private readonly int id;
    private readonly JobCompletionSource? source;

    /// <summary>
    /// Gets a completed job handle that can be used as a no-op dependency.
    /// </summary>
    public static JobHandle Completed { get; } = new(0, JobCompletionSource.CompletedSource);

    /// <summary>
    /// Gets whether this handle represents a valid scheduled job.
    /// </summary>
    public bool IsValid => source != null;

    /// <summary>
    /// Gets whether the job has completed execution.
    /// </summary>
    public bool IsCompleted => source?.IsCompleted ?? true;

    /// <summary>
    /// Gets whether the job encountered an error during execution.
    /// </summary>
    public bool IsFaulted => source?.IsFaulted ?? false;

    /// <summary>
    /// Gets the exception that caused the job to fail, or null if no error occurred.
    /// </summary>
    public Exception? Exception => source?.Exception;

    internal JobHandle(int id, JobCompletionSource source)
    {
        this.id = id;
        this.source = source;
    }

    /// <summary>
    /// Blocks the calling thread until the job completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will block indefinitely until the job finishes execution.
    /// For timeout-based waiting, use <see cref="Wait(TimeSpan)"/>.
    /// </para>
    /// <para>
    /// If the job faulted, this method will not throw. Check <see cref="IsFaulted"/>
    /// and <see cref="Exception"/> to handle errors.
    /// </para>
    /// </remarks>
    public void Complete()
    {
        source?.Wait();
    }

    /// <summary>
    /// Blocks the calling thread until the job completes or the timeout expires.
    /// </summary>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>True if the job completed within the timeout; false if the timeout expired.</returns>
    public bool Wait(TimeSpan timeout)
    {
        return source?.Wait(timeout) ?? true;
    }

    /// <summary>
    /// Combines multiple job handles into a single handle that completes when all jobs complete.
    /// </summary>
    /// <param name="handles">The job handles to combine.</param>
    /// <returns>A handle that completes when all input jobs have completed.</returns>
    /// <example>
    /// <code>
    /// var handle1 = scheduler.Schedule(job1);
    /// var handle2 = scheduler.Schedule(job2);
    /// var handle3 = scheduler.Schedule(job3);
    ///
    /// var combined = JobHandle.CombineDependencies(handle1, handle2, handle3);
    /// combined.Complete(); // Waits for all three jobs
    /// </code>
    /// </example>
    public static JobHandle CombineDependencies(params JobHandle[] handles)
    {
        if (handles.Length == 0)
        {
            return Completed;
        }

        if (handles.Length == 1)
        {
            return handles[0];
        }

        return new JobHandle(0, new CombinedJobCompletionSource(handles));
    }

    /// <summary>
    /// Combines multiple job handles into a single handle that completes when all jobs complete.
    /// </summary>
    /// <param name="handles">The job handles to combine.</param>
    /// <returns>A handle that completes when all input jobs have completed.</returns>
    public static JobHandle CombineDependencies(ReadOnlySpan<JobHandle> handles)
    {
        if (handles.Length == 0)
        {
            return Completed;
        }

        if (handles.Length == 1)
        {
            return handles[0];
        }

        return new JobHandle(0, new CombinedJobCompletionSource(handles.ToArray()));
    }

    /// <inheritdoc />
    public bool Equals(JobHandle other) => id == other.id && ReferenceEquals(source, other.source);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is JobHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(id, source);

    /// <summary>
    /// Determines whether two job handles are equal.
    /// </summary>
    public static bool operator ==(JobHandle left, JobHandle right) => left.Equals(right);

    /// <summary>
    /// Determines whether two job handles are not equal.
    /// </summary>
    public static bool operator !=(JobHandle left, JobHandle right) => !left.Equals(right);
}

/// <summary>
/// Internal completion source for tracking job state.
/// </summary>
internal class JobCompletionSource
{
    private readonly ManualResetEventSlim completionEvent = new(false);
    private volatile bool isCompleted;
    private volatile bool isFaulted;
    private Exception? exception;

    public static JobCompletionSource CompletedSource { get; } = CreateCompleted();

    public bool IsCompleted => isCompleted;
    public bool IsFaulted => isFaulted;
    public Exception? Exception => exception;

    private static JobCompletionSource CreateCompleted()
    {
        var source = new JobCompletionSource();
        source.SetCompleted();
        return source;
    }

    public void SetCompleted()
    {
        isCompleted = true;
        completionEvent.Set();
    }

    public void SetFaulted(Exception ex)
    {
        exception = ex;
        isFaulted = true;
        isCompleted = true;
        completionEvent.Set();
    }

    public void Wait()
    {
        completionEvent.Wait();
    }

    public bool Wait(TimeSpan timeout)
    {
        return completionEvent.Wait(timeout);
    }
}

/// <summary>
/// Completion source that tracks multiple job handles.
/// </summary>
internal sealed class CombinedJobCompletionSource : JobCompletionSource
{
    private readonly JobHandle[] handles;

    public CombinedJobCompletionSource(JobHandle[] handles)
    {
        this.handles = handles;

        // Check if all are already completed
        if (handles.All(h => h.IsCompleted))
        {
            SetCompleted();
        }
    }

    public new void Wait()
    {
        foreach (var handle in handles)
        {
            handle.Complete();
        }
        SetCompleted();
    }

    public new bool Wait(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        foreach (var handle in handles)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            if (!handle.Wait(remaining))
            {
                return false;
            }
        }

        SetCompleted();
        return true;
    }
}
