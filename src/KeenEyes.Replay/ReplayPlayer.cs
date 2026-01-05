namespace KeenEyes.Replay;

/// <summary>
/// Provides replay playback functionality for recorded game sessions.
/// </summary>
/// <remarks>
/// <para>
/// The ReplayPlayer loads recorded replay data and provides controls for playback,
/// including play, pause, stop, and frame-by-frame advancement. It supports loading
/// replays from files, streams, or byte arrays.
/// </para>
/// <para>
/// Basic usage:
/// <list type="number">
/// <item><description>Create a ReplayPlayer instance.</description></item>
/// <item><description>Load a replay using <see cref="LoadReplay(string, bool)"/> or an overload.</description></item>
/// <item><description>Call <see cref="Play"/> to start playback.</description></item>
/// <item><description>Call <see cref="Update"/> each frame with the delta time.</description></item>
/// </list>
/// </para>
/// <para>
/// The player maintains playback state and position, which can be queried through
/// <see cref="State"/>, <see cref="CurrentFrame"/>, and <see cref="CurrentTime"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var player = new ReplayPlayer();
///
/// // Load a replay file
/// player.LoadReplay("recording.kreplay");
///
/// // Start playback
/// player.Play();
///
/// // In your game loop
/// while (player.State == PlaybackState.Playing)
/// {
///     player.Update(deltaTime);
///
///     // Process the current frame
///     var frame = player.GetCurrentFrame();
///     // ... handle frame events ...
/// }
/// </code>
/// </example>
public sealed class ReplayPlayer : IDisposable
{
    private readonly Lock syncRoot = new();

    private ReplayData? replayData;
    private ReplayFileInfo? fileInfo;
    private PlaybackState state;
    private int currentFrameIndex;
    private TimeSpan currentTime;
    private TimeSpan accumulatedTime;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayPlayer"/> class.
    /// </summary>
    public ReplayPlayer()
    {
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    /// <remarks>
    /// The state indicates whether playback is stopped, playing, or paused.
    /// </remarks>
    public PlaybackState State
    {
        get
        {
            lock (syncRoot)
            {
                return state;
            }
        }
    }

    /// <summary>
    /// Gets the current frame index (0-based).
    /// </summary>
    /// <remarks>
    /// Returns -1 if no replay is loaded.
    /// </remarks>
    public int CurrentFrame
    {
        get
        {
            lock (syncRoot)
            {
                return replayData is not null ? currentFrameIndex : -1;
            }
        }
    }

    /// <summary>
    /// Gets the total number of frames in the loaded replay.
    /// </summary>
    /// <remarks>
    /// Returns 0 if no replay is loaded.
    /// </remarks>
    public int TotalFrames
    {
        get
        {
            lock (syncRoot)
            {
                return replayData?.FrameCount ?? 0;
            }
        }
    }

    /// <summary>
    /// Gets the current playback time.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="TimeSpan.Zero"/> if no replay is loaded.
    /// </remarks>
    public TimeSpan CurrentTime
    {
        get
        {
            lock (syncRoot)
            {
                return currentTime;
            }
        }
    }

    /// <summary>
    /// Gets the total duration of the loaded replay.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="TimeSpan.Zero"/> if no replay is loaded.
    /// </remarks>
    public TimeSpan TotalDuration
    {
        get
        {
            lock (syncRoot)
            {
                return replayData?.Duration ?? TimeSpan.Zero;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a replay is currently loaded.
    /// </summary>
    public bool IsLoaded
    {
        get
        {
            lock (syncRoot)
            {
                return replayData is not null;
            }
        }
    }

    /// <summary>
    /// Gets the loaded replay data, if any.
    /// </summary>
    /// <remarks>
    /// Returns null if no replay is loaded.
    /// </remarks>
    public ReplayData? LoadedReplay
    {
        get
        {
            lock (syncRoot)
            {
                return replayData;
            }
        }
    }

    /// <summary>
    /// Gets the file info for the loaded replay, if loaded from a file.
    /// </summary>
    /// <remarks>
    /// Returns null if no replay is loaded or if the replay was loaded from raw data.
    /// </remarks>
    public ReplayFileInfo? FileInfo
    {
        get
        {
            lock (syncRoot)
            {
                return fileInfo;
            }
        }
    }

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
    /// <remarks>
    /// <para>
    /// This method loads the replay file and prepares it for playback.
    /// Any previously loaded replay is automatically unloaded.
    /// </para>
    /// <para>
    /// After loading, the player is in <see cref="PlaybackState.Stopped"/> state
    /// at frame 0. Call <see cref="Play"/> to begin playback.
    /// </para>
    /// </remarks>
    public void LoadReplay(string path, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ThrowIfDisposed();

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Replay file not found: {path}", path);
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            LoadReplayCore(stream, validateChecksum, path);
        }
        catch (InvalidDataException ex)
        {
            throw ConvertToReplayException(ex, path);
        }
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
    /// <remarks>
    /// <para>
    /// The stream is read but not disposed. The caller retains ownership of the stream.
    /// </para>
    /// <para>
    /// Any previously loaded replay is automatically unloaded.
    /// </para>
    /// </remarks>
    public void LoadReplay(Stream stream, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        ThrowIfDisposed();

        try
        {
            LoadReplayCore(stream, validateChecksum, filePath: null);
        }
        catch (InvalidDataException ex)
        {
            throw ConvertToReplayException(ex, filePath: null);
        }
    }

    /// <summary>
    /// Loads a replay from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing replay data.</param>
    /// <param name="validateChecksum">Whether to validate the file checksum.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    /// <exception cref="ReplayFormatException">Thrown when the data format is invalid.</exception>
    /// <exception cref="ReplayVersionException">Thrown when the replay version is not supported.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Any previously loaded replay is automatically unloaded.
    /// </para>
    /// </remarks>
    public void LoadReplay(byte[] data, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(data);

        ThrowIfDisposed();

        try
        {
            using var stream = new MemoryStream(data);
            LoadReplayCore(stream, validateChecksum, filePath: null);
        }
        catch (InvalidDataException ex)
        {
            throw ConvertToReplayException(ex, filePath: null);
        }
    }

    /// <summary>
    /// Loads replay data directly without file parsing.
    /// </summary>
    /// <param name="replay">The replay data to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="replay"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method allows loading replay data that was obtained programmatically,
    /// such as directly from a <see cref="ReplayRecorder"/> or network transfer.
    /// </para>
    /// <para>
    /// Any previously loaded replay is automatically unloaded.
    /// </para>
    /// </remarks>
    public void LoadReplay(ReplayData replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        ThrowIfDisposed();

        lock (syncRoot)
        {
            UnloadReplayCore();
            replayData = replay;
            fileInfo = null;
            ResetPlaybackPosition();
        }
    }

    /// <summary>
    /// Unloads the currently loaded replay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After unloading, all playback properties return their default values
    /// (e.g., <see cref="TotalFrames"/> returns 0, <see cref="CurrentFrame"/> returns -1).
    /// </para>
    /// <para>
    /// This method does nothing if no replay is loaded.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void UnloadReplay()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            UnloadReplayCore();
        }
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// If playback was stopped, this starts from the beginning.
    /// If playback was paused, this resumes from the current position.
    /// If already playing, this method has no effect.
    /// </para>
    /// </remarks>
    public void Play()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            state = PlaybackState.Playing;
        }
    }

    /// <summary>
    /// Pauses playback at the current position.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Pausing preserves the current playback position. Call <see cref="Play"/>
    /// to resume from the paused position.
    /// </para>
    /// <para>
    /// If already paused or stopped, this method has no effect.
    /// </para>
    /// </remarks>
    public void Pause()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (state == PlaybackState.Playing)
            {
                state = PlaybackState.Paused;
            }
        }
    }

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This resets <see cref="CurrentFrame"/> to 0 and <see cref="CurrentTime"/>
    /// to <see cref="TimeSpan.Zero"/>.
    /// </para>
    /// <para>
    /// If already stopped, this method has no effect.
    /// </para>
    /// </remarks>
    public void Stop()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            ResetPlaybackPosition();
        }
    }

    /// <summary>
    /// Advances playback by the specified time delta.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <returns>
    /// True if the playback position changed (frame advanced or playback completed);
    /// false if playback is not active or no change occurred.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method should be called each frame in your game loop while playback is active.
    /// It accumulates time and advances frames based on the original recording timing.
    /// </para>
    /// <para>
    /// When playback reaches the end of the replay, the state automatically
    /// transitions to <see cref="PlaybackState.Stopped"/>.
    /// </para>
    /// <para>
    /// If the state is not <see cref="PlaybackState.Playing"/>, this method
    /// returns false without making any changes.
    /// </para>
    /// </remarks>
    public bool Update(float deltaTime)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            if (state != PlaybackState.Playing || replayData is null)
            {
                return false;
            }

            var frames = replayData.Frames;
            if (frames.Count == 0 || currentFrameIndex >= frames.Count)
            {
                state = PlaybackState.Stopped;
                return true;
            }

            // Accumulate time
            accumulatedTime += TimeSpan.FromSeconds(deltaTime);
            var changed = false;

            // Advance frames based on accumulated time
            while (currentFrameIndex < frames.Count)
            {
                var currentFrame = frames[currentFrameIndex];
                var frameEndTime = currentFrame.ElapsedTime + currentFrame.DeltaTime;

                if (accumulatedTime < currentFrame.DeltaTime)
                {
                    // Not enough time has passed to complete this frame
                    break;
                }

                // Advance to the next frame
                accumulatedTime -= currentFrame.DeltaTime;
                currentTime = frameEndTime;
                currentFrameIndex++;
                changed = true;

                // Check if we've reached the end
                if (currentFrameIndex >= frames.Count)
                {
                    state = PlaybackState.Stopped;
                    break;
                }
            }

            return changed;
        }
    }

    /// <summary>
    /// Gets the current frame data.
    /// </summary>
    /// <returns>The current frame, or null if no replay is loaded or playback is at the end.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public ReplayFrame? GetCurrentFrame()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            if (replayData is null || currentFrameIndex < 0 || currentFrameIndex >= replayData.Frames.Count)
            {
                return null;
            }

            return replayData.Frames[currentFrameIndex];
        }
    }

    /// <summary>
    /// Gets a frame at the specified index.
    /// </summary>
    /// <param name="frameIndex">The 0-based frame index.</param>
    /// <returns>The frame at the specified index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public ReplayFrame GetFrame(int frameIndex)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (frameIndex < 0 || frameIndex >= replayData!.Frames.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameIndex),
                    frameIndex,
                    $"Frame index must be between 0 and {replayData!.Frames.Count - 1}.");
            }

            return replayData!.Frames[frameIndex];
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ReplayPlayer"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            UnloadReplayCore();
            disposed = true;
        }
    }

    private void LoadReplayCore(Stream stream, bool validateChecksum, string? filePath)
    {
        // Validate format first
        if (!ReplayFileFormat.IsValidFormat(stream))
        {
            throw ReplayFormatException.InvalidMagicBytes(filePath);
        }

        // Reset stream position after format check
        stream.Position = 0;

        // Read the replay file
        var (info, data) = ReplayFileFormat.Read(stream, validateChecksum);

        // Check version compatibility
        if (data.Version > ReplayData.CurrentVersion)
        {
            throw new ReplayVersionException(data.Version, ReplayData.CurrentVersion, filePath);
        }

        lock (syncRoot)
        {
            UnloadReplayCore();
            replayData = data;
            fileInfo = info;
            ResetPlaybackPosition();
        }
    }

    private void UnloadReplayCore()
    {
        replayData = null;
        fileInfo = null;
        ResetPlaybackPosition();
    }

    private void ResetPlaybackPosition()
    {
        state = PlaybackState.Stopped;
        currentFrameIndex = 0;
        currentTime = TimeSpan.Zero;
        accumulatedTime = TimeSpan.Zero;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private void ThrowIfNoReplayLoaded()
    {
        if (replayData is null)
        {
            throw new InvalidOperationException("No replay is loaded. Call LoadReplay first.");
        }
    }

    private static ReplayException ConvertToReplayException(InvalidDataException ex, string? filePath)
    {
        var message = ex.Message;

        // Check for known error patterns
        if (message.Contains("magic bytes", StringComparison.OrdinalIgnoreCase))
        {
            return ReplayFormatException.InvalidMagicBytes(filePath);
        }

        if (message.Contains("checksum", StringComparison.OrdinalIgnoreCase))
        {
            // Extract checksum values if present in the message
            return new ReplayFormatException(message, filePath, "ChecksumMismatch");
        }

        if (message.Contains("version", StringComparison.OrdinalIgnoreCase))
        {
            // Try to extract version info - default to unknown versions
            return ReplayVersionException.UnknownVersion(message, filePath);
        }

        // Generic format error
        return ReplayFormatException.Corrupted(filePath, message);
    }
}
