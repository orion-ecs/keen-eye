namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Status of an asynchronous path computation request.
/// </summary>
public enum PathRequestStatus
{
    /// <summary>
    /// The request is queued but computation has not started.
    /// </summary>
    Pending,

    /// <summary>
    /// Path computation is currently in progress.
    /// </summary>
    Computing,

    /// <summary>
    /// Path computation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Path computation failed (no valid path found).
    /// </summary>
    Failed,

    /// <summary>
    /// The request was cancelled before completion.
    /// </summary>
    Cancelled
}
