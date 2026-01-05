using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Represents an asynchronous path computation request for grid navigation.
/// </summary>
/// <param name="start">The starting position.</param>
/// <param name="end">The destination position.</param>
/// <param name="agent">The agent settings.</param>
/// <param name="areaMask">The area mask for filtering.</param>
internal sealed class GridPathRequest(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask) : IPathRequest
{
    private static int nextId;

    private readonly TaskCompletionSource<NavPath> taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private PathRequestStatus status = PathRequestStatus.Pending;
    private NavPath result = NavPath.Empty;

    /// <inheritdoc/>
    public int Id { get; } = Interlocked.Increment(ref nextId);

    /// <inheritdoc/>
    public PathRequestStatus Status => status;

    /// <inheritdoc/>
    public Vector3 Start { get; } = start;

    /// <inheritdoc/>
    public Vector3 End { get; } = end;

    /// <inheritdoc/>
    public AgentSettings Agent { get; } = agent;

    /// <summary>
    /// Gets the area mask for this request.
    /// </summary>
    public NavAreaMask AreaMask { get; } = areaMask;

    /// <inheritdoc/>
    public NavPath Result => result;

    /// <inheritdoc/>
    public void Cancel()
    {
        if (status is PathRequestStatus.Pending or PathRequestStatus.Computing)
        {
            status = PathRequestStatus.Cancelled;
            result = NavPath.Empty;
            taskSource.TrySetResult(NavPath.Empty);
        }
    }

    /// <inheritdoc/>
    public bool Wait(TimeSpan timeout)
    {
        return taskSource.Task.Wait(timeout);
    }

    /// <inheritdoc/>
    public Task<NavPath> AsTask() => taskSource.Task;

    /// <inheritdoc/>
    public void Dispose()
    {
        Cancel();
    }

    /// <summary>
    /// Marks the request as computing.
    /// </summary>
    internal void MarkComputing()
    {
        if (status == PathRequestStatus.Pending)
        {
            status = PathRequestStatus.Computing;
        }
    }

    /// <summary>
    /// Completes the request with a path result.
    /// </summary>
    internal void Complete(NavPath path)
    {
        if (status is PathRequestStatus.Pending or PathRequestStatus.Computing)
        {
            result = path;
            status = path.IsValid ? PathRequestStatus.Completed : PathRequestStatus.Failed;
            taskSource.TrySetResult(path);
        }
    }

    /// <summary>
    /// Marks the request as failed.
    /// </summary>
    internal void Fail()
    {
        if (status is PathRequestStatus.Pending or PathRequestStatus.Computing)
        {
            status = PathRequestStatus.Failed;
            result = NavPath.Empty;
            taskSource.TrySetResult(NavPath.Empty);
        }
    }
}
