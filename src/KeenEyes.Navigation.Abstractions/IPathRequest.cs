using System.Numerics;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Represents an asynchronous path computation request.
/// </summary>
/// <remarks>
/// <para>
/// Path requests allow non-blocking path computation. Use
/// <see cref="INavigationProvider.RequestPath"/> to create a request,
/// then poll <see cref="Status"/> or await completion.
/// </para>
/// <para>
/// Always dispose of path requests when no longer needed to allow
/// the navigation system to recycle resources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var request = navigationProvider.RequestPath(start, end, agentSettings);
///
/// // Poll for completion
/// while (request.Status == PathRequestStatus.Pending ||
///        request.Status == PathRequestStatus.Computing)
/// {
///     await Task.Delay(10);
/// }
///
/// if (request.Status == PathRequestStatus.Completed)
/// {
///     var path = request.Result;
///     // Use the path
/// }
///
/// request.Dispose();
/// </code>
/// </example>
public interface IPathRequest : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this path request.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the current status of the path computation.
    /// </summary>
    PathRequestStatus Status { get; }

    /// <summary>
    /// Gets the starting position of the path request.
    /// </summary>
    Vector3 Start { get; }

    /// <summary>
    /// Gets the destination position of the path request.
    /// </summary>
    Vector3 End { get; }

    /// <summary>
    /// Gets the agent settings used for this path request.
    /// </summary>
    AgentSettings Agent { get; }

    /// <summary>
    /// Gets the computed path result.
    /// </summary>
    /// <remarks>
    /// Only valid when <see cref="Status"/> is <see cref="PathRequestStatus.Completed"/>.
    /// Returns <see cref="NavPath.Empty"/> if the path computation failed or is incomplete.
    /// </remarks>
    NavPath Result { get; }

    /// <summary>
    /// Cancels the path request if it is still pending or computing.
    /// </summary>
    /// <remarks>
    /// After cancellation, <see cref="Status"/> will be <see cref="PathRequestStatus.Cancelled"/>
    /// and <see cref="Result"/> will be <see cref="NavPath.Empty"/>.
    /// </remarks>
    void Cancel();

    /// <summary>
    /// Blocks until the path computation completes or times out.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for completion.</param>
    /// <returns>True if the path completed within the timeout, false otherwise.</returns>
    bool Wait(TimeSpan timeout);

    /// <summary>
    /// Gets a task that completes when the path computation finishes.
    /// </summary>
    /// <returns>A task that completes with the computed path.</returns>
    Task<NavPath> AsTask();
}
