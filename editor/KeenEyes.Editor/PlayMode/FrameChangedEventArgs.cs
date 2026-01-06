namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Event arguments for frame change events during replay playback.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised whenever the current playback frame changes,
/// whether through normal playback, stepping, or seeking.
/// </para>
/// </remarks>
public sealed class FrameChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous frame number, or -1 if this is the first frame.
    /// </summary>
    public int PreviousFrame { get; }

    /// <summary>
    /// Gets the new current frame number.
    /// </summary>
    public int CurrentFrame { get; }

    /// <summary>
    /// Gets the reason for the frame change.
    /// </summary>
    public FrameChangeReason Reason { get; }

    /// <summary>
    /// Creates a new instance of <see cref="FrameChangedEventArgs"/>.
    /// </summary>
    /// <param name="previousFrame">The previous frame number.</param>
    /// <param name="currentFrame">The new current frame number.</param>
    /// <param name="reason">The reason for the frame change.</param>
    public FrameChangedEventArgs(int previousFrame, int currentFrame, FrameChangeReason reason)
    {
        PreviousFrame = previousFrame;
        CurrentFrame = currentFrame;
        Reason = reason;
    }
}

/// <summary>
/// The reason for a frame change during replay playback.
/// </summary>
public enum FrameChangeReason
{
    /// <summary>
    /// The frame changed due to normal playback progression.
    /// </summary>
    Playback,

    /// <summary>
    /// The frame changed due to stepping forward.
    /// </summary>
    StepForward,

    /// <summary>
    /// The frame changed due to stepping backward.
    /// </summary>
    StepBackward,

    /// <summary>
    /// The frame changed due to seeking to a specific frame.
    /// </summary>
    Seek,

    /// <summary>
    /// The frame changed due to stopping and resetting to the beginning.
    /// </summary>
    Stop,

    /// <summary>
    /// The frame changed due to loading a new replay.
    /// </summary>
    Load
}
