namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Provides replay recording and playback control for debugging sessions.
/// </summary>
/// <remarks>
/// <para>
/// The replay controller enables recording gameplay sessions and playing them back
/// for debugging, bug reproduction, and determinism validation.
/// </para>
/// <para>
/// Recording captures:
/// <list type="bullet">
/// <item><description>Input events (keyboard, mouse, gamepad)</description></item>
/// <item><description>World state snapshots at intervals</description></item>
/// <item><description>System and entity events</description></item>
/// </list>
/// </para>
/// <para>
/// Playback supports:
/// <list type="bullet">
/// <item><description>Variable speed playback (0.25x to 4x)</description></item>
/// <item><description>Frame-by-frame stepping</description></item>
/// <item><description>Seeking to specific frames or times</description></item>
/// <item><description>Determinism validation via checksums</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IReplayController
{
    #region Recording Control

    /// <summary>
    /// Starts a new recording session.
    /// </summary>
    /// <param name="name">Optional name for the recording.</param>
    /// <param name="maxFrames">Maximum frames to record (default: 36000 = 10 minutes at 60fps).</param>
    /// <param name="snapshotIntervalMs">Interval between snapshots in milliseconds (default: 5000).</param>
    /// <returns>The result of the operation with recording info.</returns>
    Task<ReplayOperationResult> StartRecordingAsync(
        string? name = null,
        int maxFrames = 36000,
        int snapshotIntervalMs = 5000);

    /// <summary>
    /// Stops the current recording and returns the recorded data.
    /// </summary>
    /// <returns>The result with recording info, or failure if not recording.</returns>
    Task<ReplayOperationResult> StopRecordingAsync();

    /// <summary>
    /// Cancels the current recording without saving data.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    Task<ReplayOperationResult> CancelRecordingAsync();

    /// <summary>
    /// Gets whether recording is currently active.
    /// </summary>
    /// <returns>True if recording; otherwise, false.</returns>
    Task<bool> IsRecordingAsync();

    /// <summary>
    /// Gets information about the current recording session.
    /// </summary>
    /// <returns>Recording info, or null if not recording.</returns>
    Task<RecordingInfoSnapshot?> GetRecordingInfoAsync();

    /// <summary>
    /// Forces a snapshot capture at the current frame.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    Task<ReplayOperationResult> ForceSnapshotAsync();

    #endregion

    #region Recording Management

    /// <summary>
    /// Saves the current recording to a file.
    /// </summary>
    /// <param name="path">The file path to save to.</param>
    /// <returns>The result of the operation.</returns>
    Task<ReplayOperationResult> SaveAsync(string path);

    /// <summary>
    /// Loads a recording from a file for playback.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <returns>The result with replay metadata.</returns>
    Task<ReplayOperationResult> LoadAsync(string path);

    /// <summary>
    /// Lists available recording files in a directory.
    /// </summary>
    /// <param name="directory">The directory to search (default: current directory).</param>
    /// <returns>List of replay file information.</returns>
    Task<IReadOnlyList<ReplayFileSnapshot>> ListAsync(string? directory = null);

    /// <summary>
    /// Deletes a recording file.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    /// <returns>True if deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(string path);

    /// <summary>
    /// Gets metadata from a recording file without loading it.
    /// </summary>
    /// <param name="path">The file path to read.</param>
    /// <returns>The replay metadata, or null if invalid.</returns>
    Task<ReplayMetadataSnapshot?> GetMetadataAsync(string path);

    #endregion

    #region Playback Control

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <returns>The result with current playback state.</returns>
    Task<ReplayOperationResult> PlayAsync();

    /// <summary>
    /// Pauses playback at the current position.
    /// </summary>
    /// <returns>The result with current playback state.</returns>
    Task<ReplayOperationResult> PauseAsync();

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    /// <returns>The result with current playback state.</returns>
    Task<ReplayOperationResult> StopPlaybackAsync();

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    /// <returns>The current playback state.</returns>
    Task<PlaybackStateSnapshot> GetPlaybackStateAsync();

    /// <summary>
    /// Sets the playback speed multiplier.
    /// </summary>
    /// <param name="speed">Speed multiplier (0.25 to 4.0).</param>
    /// <returns>The result with updated playback state.</returns>
    Task<ReplayOperationResult> SetSpeedAsync(float speed);

    #endregion

    #region Playback Navigation

    /// <summary>
    /// Seeks to a specific frame number.
    /// </summary>
    /// <param name="frame">The 0-based frame number.</param>
    /// <returns>The result with updated playback state.</returns>
    Task<ReplayOperationResult> SeekFrameAsync(int frame);

    /// <summary>
    /// Seeks to a specific time in seconds.
    /// </summary>
    /// <param name="seconds">The time in seconds from the start.</param>
    /// <returns>The result with updated playback state.</returns>
    Task<ReplayOperationResult> SeekTimeAsync(float seconds);

    /// <summary>
    /// Steps forward by a number of frames.
    /// </summary>
    /// <param name="frames">Number of frames to step (default: 1).</param>
    /// <returns>The result with updated playback state.</returns>
    Task<ReplayOperationResult> StepForwardAsync(int frames = 1);

    /// <summary>
    /// Steps backward by a number of frames.
    /// </summary>
    /// <param name="frames">Number of frames to step back (default: 1).</param>
    /// <returns>The result with updated playback state.</returns>
    Task<ReplayOperationResult> StepBackwardAsync(int frames = 1);

    #endregion

    #region Frame Inspection

    /// <summary>
    /// Gets data for a specific frame.
    /// </summary>
    /// <param name="frame">The 0-based frame number.</param>
    /// <returns>The frame data, or null if not found.</returns>
    Task<ReplayFrameSnapshot?> GetFrameAsync(int frame);

    /// <summary>
    /// Gets a range of frames.
    /// </summary>
    /// <param name="startFrame">The starting frame number.</param>
    /// <param name="count">Number of frames to retrieve.</param>
    /// <returns>List of frame data.</returns>
    Task<IReadOnlyList<ReplayFrameSnapshot>> GetFrameRangeAsync(int startFrame, int count);

    /// <summary>
    /// Gets input events in a frame range.
    /// </summary>
    /// <param name="startFrame">The starting frame number.</param>
    /// <param name="endFrame">The ending frame number (inclusive).</param>
    /// <returns>List of input events.</returns>
    Task<IReadOnlyList<InputEventSnapshot>> GetInputsAsync(int startFrame, int endFrame);

    /// <summary>
    /// Gets replay events in a frame range.
    /// </summary>
    /// <param name="startFrame">The starting frame number.</param>
    /// <param name="endFrame">The ending frame number (inclusive).</param>
    /// <returns>List of replay events.</returns>
    Task<IReadOnlyList<ReplayEventSnapshot>> GetEventsAsync(int startFrame, int endFrame);

    /// <summary>
    /// Gets all snapshot markers in the loaded replay.
    /// </summary>
    /// <returns>List of snapshot marker info.</returns>
    Task<IReadOnlyList<SnapshotMarkerSnapshot>> GetSnapshotsAsync();

    #endregion

    #region Validation

    /// <summary>
    /// Validates a replay file for integrity.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResultSnapshot> ValidateAsync(string path);

    /// <summary>
    /// Checks if the loaded replay is deterministic by comparing checksums.
    /// </summary>
    /// <returns>The determinism check result.</returns>
    Task<DeterminismResultSnapshot> CheckDeterminismAsync();

    #endregion
}
