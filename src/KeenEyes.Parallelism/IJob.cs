namespace KeenEyes.Parallelism;

/// <summary>
/// Represents a unit of work that can be scheduled for parallel execution.
/// </summary>
/// <remarks>
/// <para>
/// Jobs are lightweight work units that execute on worker threads. Unlike systems,
/// jobs are stateless and designed for one-time execution of specific tasks like
/// bulk component processing, physics calculations, or pathfinding.
/// </para>
/// <para>
/// Implement this interface to define custom parallel work. The <see cref="Execute"/>
/// method will be called once per job when scheduled via <see cref="JobScheduler"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public struct ProcessPositionsJob : IJob
/// {
///     public Span&lt;Position&gt; Positions { get; init; }
///     public float DeltaTime { get; init; }
///
///     public void Execute()
///     {
///         foreach (ref var pos in Positions)
///         {
///             pos.X += DeltaTime;
///             pos.Y += DeltaTime;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IJob
{
    /// <summary>
    /// Executes the job's work.
    /// </summary>
    /// <remarks>
    /// This method is called on a worker thread. Implementations must be thread-safe
    /// and should avoid accessing shared mutable state without proper synchronization.
    /// </remarks>
    void Execute();
}

/// <summary>
/// Represents a job that can be executed in parallel across multiple iterations.
/// </summary>
/// <remarks>
/// <para>
/// Parallel jobs are similar to <see cref="IJob"/> but execute multiple times with
/// different indices. This is ideal for processing arrays or collections in parallel
/// where each element can be processed independently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public struct UpdateVelocitiesJob : IParallelJob
/// {
///     public Span&lt;Velocity&gt; Velocities { get; init; }
///     public float Gravity { get; init; }
///
///     public void Execute(int index)
///     {
///         Velocities[index].Y -= Gravity;
///     }
/// }
///
/// // Schedule to process all velocities in parallel
/// var job = new UpdateVelocitiesJob { Velocities = velocities, Gravity = 9.8f };
/// scheduler.ScheduleParallel(job, velocities.Length);
/// </code>
/// </example>
public interface IParallelJob
{
    /// <summary>
    /// Executes the job's work for the specified index.
    /// </summary>
    /// <param name="index">The iteration index to process.</param>
    /// <remarks>
    /// This method may be called concurrently from multiple threads with different indices.
    /// Implementations must ensure thread-safe access to any shared data.
    /// </remarks>
    void Execute(int index);
}

/// <summary>
/// Represents a job that can process a range of items in batches.
/// </summary>
/// <remarks>
/// <para>
/// Batch jobs execute on ranges of indices, providing better cache locality than
/// per-element parallel jobs. The scheduler divides the total range into batches
/// and calls <see cref="Execute"/> once per batch.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public struct ProcessChunkJob : IBatchJob
/// {
///     public Memory&lt;Position&gt; Positions { get; init; }
///     public float DeltaTime { get; init; }
///
///     public void Execute(int startIndex, int count)
///     {
///         var span = Positions.Span.Slice(startIndex, count);
///         foreach (ref var pos in span)
///         {
///             pos.X += DeltaTime;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IBatchJob
{
    /// <summary>
    /// Executes the job's work for the specified range.
    /// </summary>
    /// <param name="startIndex">The starting index of the batch.</param>
    /// <param name="count">The number of items to process in this batch.</param>
    void Execute(int startIndex, int count);
}
