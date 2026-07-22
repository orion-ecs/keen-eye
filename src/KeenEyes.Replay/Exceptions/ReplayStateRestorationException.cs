namespace KeenEyes.Replay;

/// <summary>
/// Thrown when restoring world state from a replay snapshot fails.
/// </summary>
/// <remarks>
/// <para>
/// This exception is raised by <see cref="ReplayPlayer"/> navigation methods
/// (<see cref="ReplayPlayer.SeekToFrame"/>, <see cref="ReplayPlayer.SeekToTime"/>,
/// and <see cref="ReplayPlayer.Step"/>) when
/// <see cref="ReplayPlayer.EnableStateRestoration"/> is enabled and a snapshot
/// cannot be applied to the world (for example, an unregistered component type
/// or a schema version mismatch).
/// </para>
/// <para>
/// Restoration is atomic: when this exception is thrown the target world is left
/// in its pre-navigation state. The player's timeline position is also unchanged,
/// because the frame index only advances after a successful restore.
/// </para>
/// </remarks>
/// <seealso cref="ReplayPlayer.EnableStateRestoration"/>
public sealed class ReplayStateRestorationException : ReplayException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStateRestorationException"/> class.
    /// </summary>
    public ReplayStateRestorationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStateRestorationException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ReplayStateRestorationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStateRestorationException"/> class
    /// with a specified error message and a reference to the inner exception that is the
    /// cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception.
    /// </param>
    public ReplayStateRestorationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStateRestorationException"/> class
    /// for a failure restoring the snapshot preceding the specified frame.
    /// </summary>
    /// <param name="frameNumber">The target frame whose preceding snapshot failed to restore.</param>
    /// <param name="snapshotFrameNumber">The frame number of the snapshot that failed to restore.</param>
    /// <param name="innerException">The underlying exception that caused the failure.</param>
    public ReplayStateRestorationException(int frameNumber, int snapshotFrameNumber, Exception innerException)
        : base(
            $"Failed to restore world state from the snapshot at frame {snapshotFrameNumber} " +
            $"while seeking to frame {frameNumber}. The world has been left in its pre-seek state.",
            innerException)
    {
        FrameNumber = frameNumber;
        SnapshotFrameNumber = snapshotFrameNumber;
    }

    /// <summary>
    /// Gets the target frame that the navigation was seeking to, if known.
    /// </summary>
    public int? FrameNumber { get; }

    /// <summary>
    /// Gets the frame number of the snapshot that failed to restore, if known.
    /// </summary>
    public int? SnapshotFrameNumber { get; }
}
