using KeenEyes.Replay;
using KeenEyes.Serialization;

namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Provides replay playback functionality for debugging and bug analysis in the editor.
/// </summary>
/// <remarks>
/// <para>
/// ReplayPlaybackMode enables loading and playing back recorded replay files,
/// with frame-by-frame stepping, seeking, and inspection capabilities.
/// It creates a separate playback world to preserve the editing world state.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Load replay files from disk</description></item>
/// <item><description>Play/pause/stop controls</description></item>
/// <item><description>Frame-by-frame stepping forward and backward</description></item>
/// <item><description>Seek to specific frames</description></item>
/// <item><description>Variable playback speed</description></item>
/// <item><description>Frame inspection for debugging</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var playbackMode = new ReplayPlaybackMode(serializer);
///
/// // Load a replay file
/// playbackMode.LoadReplay("crash_replay.kreplay");
///
/// // Play at half speed
/// playbackMode.PlaybackSpeed = 0.5f;
/// playbackMode.Play();
///
/// // Or step through frame by frame
/// playbackMode.StepFrame();
/// var frameData = playbackMode.GetCurrentFrameData();
/// Console.WriteLine($"Frame {frameData.FrameNumber}: {frameData.Events.Count} events");
/// </code>
/// </example>
public sealed class ReplayPlaybackMode : IDisposable
{
    private readonly IComponentSerializer serializer;
    private readonly ReplayPlayer player;
    private World? playbackWorld;
    private float playbackSpeed = 1.0f;
    private bool disposed;

    /// <summary>
    /// Raised when the current playback frame changes.
    /// </summary>
    public event EventHandler<FrameChangedEventArgs>? FrameChanged;

    /// <summary>
    /// Raised when the playback state changes.
    /// </summary>
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Creates a new instance of <see cref="ReplayPlaybackMode"/>.
    /// </summary>
    /// <param name="serializer">The component serializer for world snapshot operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> is null.</exception>
    public ReplayPlaybackMode(IComponentSerializer serializer)
    {
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        player = new ReplayPlayer();
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public PlaybackState State => player.State;

    /// <summary>
    /// Gets the current frame number (0-based).
    /// </summary>
    /// <remarks>
    /// Returns -1 if no replay is loaded.
    /// </remarks>
    public int CurrentFrame => player.CurrentFrame;

    /// <summary>
    /// Gets the total number of frames in the loaded replay.
    /// </summary>
    public int TotalFrames => player.TotalFrames;

    /// <summary>
    /// Gets the current playback time.
    /// </summary>
    public TimeSpan CurrentTime => player.CurrentTime;

    /// <summary>
    /// Gets the total duration of the loaded replay.
    /// </summary>
    public TimeSpan TotalDuration => player.TotalDuration;

    /// <summary>
    /// Gets a value indicating whether a replay is currently loaded.
    /// </summary>
    public bool IsLoaded => player.IsLoaded;

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value of 1.0 plays at normal speed, 0.5 at half speed, 2.0 at double speed, etc.
    /// </para>
    /// <para>
    /// Valid range is 0.1 to 10.0. Values outside this range are clamped.
    /// </para>
    /// </remarks>
    public float PlaybackSpeed
    {
        get => playbackSpeed;
        set => playbackSpeed = Math.Clamp(value, 0.1f, 10.0f);
    }

    /// <summary>
    /// Gets the playback world created for replay playback.
    /// </summary>
    /// <remarks>
    /// Returns null if no replay is loaded. The playback world is a separate
    /// instance from the editing world to preserve editor state during playback.
    /// </remarks>
    public World? PlaybackWorld => playbackWorld;

    /// <summary>
    /// Gets the loaded replay data.
    /// </summary>
    public ReplayData? LoadedReplay => player.LoadedReplay;

    /// <summary>
    /// Gets the file info for the loaded replay, if loaded from a file.
    /// </summary>
    public ReplayFileInfo? FileInfo => player.FileInfo;

    /// <summary>
    /// Loads a replay from a file path.
    /// </summary>
    /// <param name="path">The path to the .kreplay file.</param>
    /// <param name="validateChecksum">Whether to validate the file checksum.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ReplayFormatException">Thrown when the file format is invalid.</exception>
    /// <exception cref="ReplayVersionException">Thrown when the replay version is not supported.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void LoadReplay(string path, bool validateChecksum = true)
    {
        ThrowIfDisposed();

        var prevFrame = player.CurrentFrame;

        // Dispose existing playback world
        DisposePlaybackWorld();

        // Load the replay
        player.LoadReplay(path, validateChecksum);

        // Create a new playback world for this replay
        CreatePlaybackWorld();

        OnFrameChanged(prevFrame, 0, FrameChangeReason.Load);
    }

    /// <summary>
    /// Loads a replay from a stream.
    /// </summary>
    /// <param name="stream">The stream containing replay data.</param>
    /// <param name="validateChecksum">Whether to validate the file checksum.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="ReplayFormatException">Thrown when the data format is invalid.</exception>
    /// <exception cref="ReplayVersionException">Thrown when the replay version is not supported.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void LoadReplay(Stream stream, bool validateChecksum = true)
    {
        ThrowIfDisposed();

        var prevFrame = player.CurrentFrame;

        DisposePlaybackWorld();
        player.LoadReplay(stream, validateChecksum);
        CreatePlaybackWorld();

        OnFrameChanged(prevFrame, 0, FrameChangeReason.Load);
    }

    /// <summary>
    /// Loads replay data directly.
    /// </summary>
    /// <param name="replayData">The replay data to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="replayData"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void LoadReplay(ReplayData replayData)
    {
        ThrowIfDisposed();

        var prevFrame = player.CurrentFrame;

        DisposePlaybackWorld();
        player.LoadReplay(replayData);
        CreatePlaybackWorld();

        OnFrameChanged(prevFrame, 0, FrameChangeReason.Load);
    }

    /// <summary>
    /// Unloads the currently loaded replay and disposes the playback world.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Unload()
    {
        ThrowIfDisposed();

        player.UnloadReplay();
        DisposePlaybackWorld();
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Play()
    {
        ThrowIfDisposed();

        var prevState = player.State;
        player.Play();

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }
    }

    /// <summary>
    /// Pauses playback at the current position.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Pause()
    {
        ThrowIfDisposed();

        var prevState = player.State;
        player.Pause();

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }
    }

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Stop()
    {
        ThrowIfDisposed();

        var prevState = player.State;
        var prevFrame = player.CurrentFrame;
        player.Stop();

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }

        if (prevFrame != player.CurrentFrame)
        {
            OnFrameChanged(prevFrame, player.CurrentFrame, FrameChangeReason.Stop);
        }
    }

    /// <summary>
    /// Steps forward by one frame.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Stepping pauses playback if it was playing. This is intended for
    /// frame-by-frame debugging.
    /// </para>
    /// </remarks>
    public void StepFrame()
    {
        ThrowIfDisposed();

        var prevState = player.State;
        var prevFrame = player.CurrentFrame;
        player.Step(1);

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }

        if (prevFrame != player.CurrentFrame)
        {
            OnFrameChanged(prevFrame, player.CurrentFrame, FrameChangeReason.StepForward);
        }
    }

    /// <summary>
    /// Steps backward by one frame.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Stepping pauses playback if it was playing. This is intended for
    /// frame-by-frame debugging.
    /// </para>
    /// <para>
    /// Backward stepping uses the nearest snapshot to restore world state
    /// and then replays events up to the target frame.
    /// </para>
    /// </remarks>
    public void StepFrameBack()
    {
        ThrowIfDisposed();

        var prevState = player.State;
        var prevFrame = player.CurrentFrame;
        player.Step(-1);

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }

        if (prevFrame != player.CurrentFrame)
        {
            OnFrameChanged(prevFrame, player.CurrentFrame, FrameChangeReason.StepBackward);
        }
    }

    /// <summary>
    /// Seeks to a specific frame number.
    /// </summary>
    /// <param name="frame">The 0-based frame number to seek to.</param>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="frame"/> is negative or greater than or equal to <see cref="TotalFrames"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Seeking pauses playback if it was playing. This is intended for
    /// timeline scrubbing in the editor UI.
    /// </para>
    /// </remarks>
    public void SeekToFrame(int frame)
    {
        ThrowIfDisposed();

        var prevState = player.State;
        var prevFrame = player.CurrentFrame;
        player.SeekToFrame(frame);

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }

        if (prevFrame != player.CurrentFrame)
        {
            OnFrameChanged(prevFrame, player.CurrentFrame, FrameChangeReason.Seek);
        }
    }

    /// <summary>
    /// Seeks to the frame at or before the specified time.
    /// </summary>
    /// <param name="time">The time to seek to.</param>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="time"/> is negative or greater than <see cref="TotalDuration"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void SeekToTime(TimeSpan time)
    {
        ThrowIfDisposed();

        var prevState = player.State;
        var prevFrame = player.CurrentFrame;
        player.SeekToTime(time);

        if (prevState != player.State)
        {
            OnStateChanged(prevState, player.State);
        }

        if (prevFrame != player.CurrentFrame)
        {
            OnFrameChanged(prevFrame, player.CurrentFrame, FrameChangeReason.Seek);
        }
    }

    /// <summary>
    /// Updates playback based on elapsed time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <returns>True if the playback position changed; false otherwise.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method should be called each frame when playback is active.
    /// It advances the playback position based on the provided delta time
    /// and the current <see cref="PlaybackSpeed"/> setting.
    /// </para>
    /// </remarks>
    public bool Update(float deltaTime)
    {
        ThrowIfDisposed();

        if (player.State != PlaybackState.Playing)
        {
            return false;
        }

        var prevFrame = player.CurrentFrame;
        var adjustedDelta = deltaTime * playbackSpeed;
        var changed = player.Update(adjustedDelta);

        if (changed && prevFrame != player.CurrentFrame)
        {
            OnFrameChanged(prevFrame, player.CurrentFrame, FrameChangeReason.Playback);
        }

        // Check if playback completed (reached end)
        if (player.State == PlaybackState.Stopped && changed)
        {
            OnStateChanged(PlaybackState.Playing, PlaybackState.Stopped);
        }

        return changed;
    }

    /// <summary>
    /// Gets inspection data for the current frame.
    /// </summary>
    /// <returns>
    /// The frame inspection data, or null if no replay is loaded or the current frame is invalid.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public FrameInspectionData? GetCurrentFrameData()
    {
        ThrowIfDisposed();

        var frame = player.GetCurrentFrame();
        return frame is not null ? new FrameInspectionData(frame) : null;
    }

    /// <summary>
    /// Gets inspection data for a specific frame.
    /// </summary>
    /// <param name="frameNumber">The 0-based frame number.</param>
    /// <returns>The frame inspection data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the frame number is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public FrameInspectionData GetFrameData(int frameNumber)
    {
        ThrowIfDisposed();

        var frame = player.GetFrame(frameNumber);
        return new FrameInspectionData(frame);
    }

    /// <summary>
    /// Gets the nearest snapshot at or before the specified frame.
    /// </summary>
    /// <param name="targetFrame">The target frame number.</param>
    /// <returns>The snapshot marker, or null if no snapshot exists before the target.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public SnapshotMarker? GetNearestSnapshot(int targetFrame)
    {
        ThrowIfDisposed();
        return player.GetNearestSnapshot(targetFrame);
    }

    /// <summary>
    /// Toggles between playing and paused states.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void TogglePlayPause()
    {
        ThrowIfDisposed();

        if (player.State == PlaybackState.Playing)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ReplayPlaybackMode"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        DisposePlaybackWorld();
        player.Dispose();
    }

    private void CreatePlaybackWorld()
    {
        // Create a fresh world for playback
        playbackWorld = new World();

        // If the replay has an initial snapshot, restore it to the playback world
        var replay = player.LoadedReplay;
        if (replay is not null && replay.Snapshots.Count > 0)
        {
            var firstSnapshot = replay.Snapshots[0];
            if (firstSnapshot.FrameNumber == 0 && firstSnapshot.Snapshot is not null)
            {
                SnapshotManager.RestoreSnapshot(playbackWorld, firstSnapshot.Snapshot, serializer);
            }
        }
    }

    private void DisposePlaybackWorld()
    {
        if (playbackWorld is not null)
        {
            playbackWorld.Dispose();
            playbackWorld = null;
        }
    }

    private void OnFrameChanged(int previousFrame, int currentFrame, FrameChangeReason reason)
    {
        FrameChanged?.Invoke(this, new FrameChangedEventArgs(previousFrame, currentFrame, reason));
    }

    private void OnStateChanged(PlaybackState previousState, PlaybackState newState)
    {
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(previousState, newState));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}

/// <summary>
/// Event arguments for playback state changes.
/// </summary>
public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous playback state.
    /// </summary>
    public PlaybackState PreviousState { get; }

    /// <summary>
    /// Gets the new playback state.
    /// </summary>
    public PlaybackState NewState { get; }

    /// <summary>
    /// Creates a new instance of <see cref="PlaybackStateChangedEventArgs"/>.
    /// </summary>
    /// <param name="previousState">The previous playback state.</param>
    /// <param name="newState">The new playback state.</param>
    public PlaybackStateChangedEventArgs(PlaybackState previousState, PlaybackState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}
