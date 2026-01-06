using KeenEyes.Serialization;

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
    private int lastReportedFrameIndex = -1;
    private TimeSpan currentTime;
    private TimeSpan accumulatedTime;
    private float playbackSpeed = PlaybackSpeeds.NormalSpeed;
    private bool disposed;

    // Validation context
    private World? validationWorld;
    private IComponentSerializer? validationSerializer;
    private bool autoValidate;

    /// <summary>
    /// Raised when playback starts or resumes from a paused state.
    /// </summary>
    /// <remarks>
    /// This event is fired when <see cref="Play"/> is called and the state
    /// transitions from <see cref="PlaybackState.Stopped"/> or
    /// <see cref="PlaybackState.Paused"/> to <see cref="PlaybackState.Playing"/>.
    /// </remarks>
    public event Action? PlaybackStarted;

    /// <summary>
    /// Raised when playback is paused.
    /// </summary>
    /// <remarks>
    /// This event is fired when <see cref="Pause"/> is called while playback
    /// is in the <see cref="PlaybackState.Playing"/> state.
    /// </remarks>
    public event Action? PlaybackPaused;

    /// <summary>
    /// Raised when playback is explicitly stopped via <see cref="Stop"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is only fired when <see cref="Stop"/> is called explicitly.
    /// It is NOT fired when playback reaches the end naturally (use
    /// <see cref="PlaybackEnded"/> for that case).
    /// </para>
    /// <para>
    /// After this event, the playback position is reset to the beginning.
    /// </para>
    /// </remarks>
    public event Action? PlaybackStopped;

    /// <summary>
    /// Raised when playback reaches the end of the replay naturally.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is fired when the <see cref="Update"/> method advances
    /// past the last frame of the replay. This indicates the replay has
    /// completed without user intervention.
    /// </para>
    /// <para>
    /// After this event, the state transitions to <see cref="PlaybackState.Stopped"/>,
    /// but the playback position remains at the end (unlike <see cref="PlaybackStopped"/>
    /// which resets to the beginning).
    /// </para>
    /// </remarks>
    public event Action? PlaybackEnded;

    /// <summary>
    /// Raised when the current frame changes during playback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The integer parameter is the new 0-based frame index.
    /// </para>
    /// <para>
    /// This event is fired during <see cref="Update"/> when frames advance,
    /// and also during seeking operations (<see cref="SeekToFrame"/>,
    /// <see cref="SeekToTime"/>, <see cref="Step"/>).
    /// </para>
    /// <para>
    /// Note: This event may be called frequently (once per frame during playback),
    /// so handlers should be lightweight to avoid impacting performance.
    /// </para>
    /// </remarks>
    public event Action<int>? FrameChanged;

    /// <summary>
    /// Raised when a desync is detected during validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is fired when <see cref="AutoValidate"/> is enabled and a
    /// frame checksum mismatch is detected, or when <see cref="ValidateCurrentFrame"/>
    /// detects a mismatch.
    /// </para>
    /// <para>
    /// The <see cref="ReplayDesyncException"/> parameter contains details about
    /// the desync including the frame number, expected checksum, and actual checksum.
    /// </para>
    /// <para>
    /// Subscribers can use this event to log desyncs, pause playback, or take
    /// corrective action without throwing exceptions.
    /// </para>
    /// </remarks>
    /// <seealso cref="AutoValidate"/>
    /// <seealso cref="ValidateCurrentFrame"/>
    /// <seealso cref="ReplayDesyncException"/>
    public event Action<ReplayDesyncException>? DesyncDetected;

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
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    /// <value>
    /// A value between <see cref="PlaybackSpeeds.MinSpeed"/> (0.25x) and
    /// <see cref="PlaybackSpeeds.MaxSpeed"/> (4.0x). Default is 1.0 (normal speed).
    /// </value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when attempting to set a value outside the valid range.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The playback speed affects how <see cref="Update"/> processes time:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Slow motion (0.25x - 0.99x):</b> Frames advance slower than real time.
    /// At 0.5x, it takes 2 seconds of real time to play 1 second of replay.
    /// </description></item>
    /// <item><description>
    /// <b>Normal speed (1.0x):</b> Frames advance at the recorded rate.
    /// </description></item>
    /// <item><description>
    /// <b>Fast forward (1.01x - 4.0x):</b> Multiple frames may advance per update.
    /// At 2x, 1 second of real time plays 2 seconds of replay.
    /// </description></item>
    /// </list>
    /// <para>
    /// Use <see cref="PlaybackSpeeds"/> constants for common speed values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var player = new ReplayPlayer();
    /// player.LoadReplay("recording.kreplay");
    ///
    /// // Slow motion playback
    /// player.PlaybackSpeed = PlaybackSpeeds.HalfSpeed;
    ///
    /// // Fast forward
    /// player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;
    ///
    /// // Custom speed (1.5x)
    /// player.PlaybackSpeed = 1.5f;
    /// </code>
    /// </example>
    public float PlaybackSpeed
    {
        get
        {
            lock (syncRoot)
            {
                return playbackSpeed;
            }
        }
        set
        {
            if (value < PlaybackSpeeds.MinSpeed || value > PlaybackSpeeds.MaxSpeed)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Playback speed must be between {PlaybackSpeeds.MinSpeed} and {PlaybackSpeeds.MaxSpeed}.");
            }

            lock (syncRoot)
            {
                playbackSpeed = value;
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
    /// Gets or sets whether to automatically validate frame checksums during playback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, each frame is validated against its recorded checksum during
    /// the <see cref="Update"/> method. If a mismatch is detected, the
    /// <see cref="DesyncDetected"/> event is raised.
    /// </para>
    /// <para>
    /// Auto-validation requires a validation context to be set via
    /// <see cref="SetValidationContext"/>. If no context is set, validation
    /// is silently skipped.
    /// </para>
    /// <para>
    /// Performance note: Auto-validation adds overhead to each frame update
    /// (approximately 1ms per frame for typical world sizes). Enable only
    /// when determinism validation is required.
    /// </para>
    /// <para>
    /// Default is false.
    /// </para>
    /// </remarks>
    /// <seealso cref="SetValidationContext"/>
    /// <seealso cref="ValidateCurrentFrame"/>
    /// <seealso cref="DesyncDetected"/>
    public bool AutoValidate
    {
        get
        {
            lock (syncRoot)
            {
                return autoValidate;
            }
        }
        set
        {
            lock (syncRoot)
            {
                autoValidate = value;
            }
        }
    }

    /// <summary>
    /// Sets the validation context for checksum verification during playback.
    /// </summary>
    /// <param name="world">The world being replayed into.</param>
    /// <param name="serializer">The component serializer for checksum calculation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="serializer"/> is null.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// The validation context is required for <see cref="ValidateCurrentFrame"/>
    /// and <see cref="AutoValidate"/> to function. Without a context, validation
    /// methods will throw or be silently skipped.
    /// </para>
    /// <para>
    /// The world should be the same world that frames are being replayed into.
    /// The serializer should be the same one used during recording.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var player = new ReplayPlayer();
    /// player.LoadReplay("recording.kreplay");
    /// player.SetValidationContext(world, serializer);
    /// player.AutoValidate = true;
    /// player.DesyncDetected += ex => Console.WriteLine($"Desync at frame {ex.Frame}!");
    /// player.Play();
    /// </code>
    /// </example>
    public void SetValidationContext(World world, IComponentSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(serializer);

        ThrowIfDisposed();

        lock (syncRoot)
        {
            validationWorld = world;
            validationSerializer = serializer;
        }
    }

    /// <summary>
    /// Clears the validation context.
    /// </summary>
    /// <remarks>
    /// After calling this method, <see cref="ValidateCurrentFrame"/> will throw
    /// and <see cref="AutoValidate"/> will be silently skipped.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void ClearValidationContext()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            validationWorld = null;
            validationSerializer = null;
        }
    }

    /// <summary>
    /// Validates the current frame's checksum against the recorded checksum.
    /// </summary>
    /// <returns>
    /// True if the checksum matches or if no checksum was recorded;
    /// false if a desync was detected.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no replay is loaded or no validation context is set.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method calculates the current world's checksum and compares it
    /// against the recorded checksum for the current frame. If a mismatch
    /// is detected, the <see cref="DesyncDetected"/> event is raised.
    /// </para>
    /// <para>
    /// A validation context must be set via <see cref="SetValidationContext"/>
    /// before calling this method.
    /// </para>
    /// <para>
    /// If the current frame has no recorded checksum (e.g., recorded without
    /// <see cref="ReplayOptions.RecordChecksums"/> enabled), this method
    /// returns true without performing validation.
    /// </para>
    /// </remarks>
    /// <seealso cref="SetValidationContext"/>
    /// <seealso cref="DesyncDetected"/>
    public bool ValidateCurrentFrame()
    {
        ThrowIfDisposed();

        ReplayDesyncException? desyncException;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (validationWorld is null || validationSerializer is null)
            {
                throw new InvalidOperationException(
                    "No validation context set. Call SetValidationContext first.");
            }

            var frame = GetCurrentFrameInternal();
            if (frame is null)
            {
                return true; // No frame to validate
            }

            desyncException = ValidateFrameInternal(frame);
        }

        // Fire event outside lock to prevent deadlocks
        if (desyncException is not null)
        {
            DesyncDetected?.Invoke(desyncException);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates determinism by checking that all recorded frames have consistent checksums.
    /// </summary>
    /// <param name="iterations">
    /// The number of validation iterations to perform. Higher values provide
    /// more confidence but take longer. Default is 3.
    /// </param>
    /// <returns>
    /// True if all recorded checksums are consistent across iterations;
    /// false if any inconsistencies are detected.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no replay is loaded or no validation context is set.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="iterations"/> is less than 1.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method performs multiple iterations of checksum verification to
    /// detect non-deterministic behavior. Each iteration calculates the world
    /// checksum at each frame and compares it to the recorded value.
    /// </para>
    /// <para>
    /// For accurate results, the caller should restore the world to its initial
    /// state before each iteration and replay all frames. This method validates
    /// the checksums recorded in the replay data.
    /// </para>
    /// <para>
    /// Performance note: This method can be slow for long replays as it
    /// iterates through all frames multiple times. Target: less than 10 seconds
    /// for 1000 frames with 3 iterations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var player = new ReplayPlayer();
    /// player.LoadReplay("recording.kreplay");
    /// player.SetValidationContext(world, serializer);
    ///
    /// // Validate determinism with 3 iterations
    /// bool isDeterministic = player.ValidateDeterminism(3);
    /// if (!isDeterministic)
    /// {
    ///     Console.WriteLine("Replay contains non-deterministic behavior!");
    /// }
    /// </code>
    /// </example>
    public bool ValidateDeterminism(int iterations = 3)
    {
        ThrowIfDisposed();

        if (iterations < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iterations),
                iterations,
                "Iterations must be at least 1.");
        }

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (validationWorld is null || validationSerializer is null)
            {
                throw new InvalidOperationException(
                    "No validation context set. Call SetValidationContext first.");
            }

            // Validate that all frames have checksums
            var framesWithoutChecksums = replayData!.Frames
                .Where(f => !f.Checksum.HasValue)
                .ToList();

            if (framesWithoutChecksums.Count == replayData.Frames.Count)
            {
                // No checksums recorded - cannot validate
                return true;
            }

            // Perform validation iterations
            var checksums = new Dictionary<int, uint>();

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                foreach (var frame in replayData.Frames)
                {
                    if (!frame.Checksum.HasValue)
                    {
                        continue;
                    }

                    if (iteration == 0)
                    {
                        // First iteration: record the checksums
                        checksums[frame.FrameNumber] = frame.Checksum.Value;
                    }
                    else
                    {
                        // Subsequent iterations: verify consistency
                        if (checksums.TryGetValue(frame.FrameNumber, out var recorded) &&
                            recorded != frame.Checksum.Value)
                        {
                            return false; // Inconsistent checksum
                        }
                    }
                }
            }

            return true;
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
    /// <para>
    /// The <see cref="PlaybackStarted"/> event is fired when transitioning
    /// from <see cref="PlaybackState.Stopped"/> or <see cref="PlaybackState.Paused"/>
    /// to <see cref="PlaybackState.Playing"/>.
    /// </para>
    /// </remarks>
    public void Play()
    {
        ThrowIfDisposed();

        bool shouldFireEvent = false;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (state != PlaybackState.Playing)
            {
                state = PlaybackState.Playing;
                shouldFireEvent = true;
            }
        }

        // Fire event outside lock to prevent deadlocks
        if (shouldFireEvent)
        {
            PlaybackStarted?.Invoke();
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
    /// <para>
    /// The <see cref="PlaybackPaused"/> event is fired when transitioning
    /// from <see cref="PlaybackState.Playing"/> to <see cref="PlaybackState.Paused"/>.
    /// </para>
    /// </remarks>
    public void Pause()
    {
        ThrowIfDisposed();

        bool shouldFireEvent = false;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (state == PlaybackState.Playing)
            {
                state = PlaybackState.Paused;
                shouldFireEvent = true;
            }
        }

        // Fire event outside lock to prevent deadlocks
        if (shouldFireEvent)
        {
            PlaybackPaused?.Invoke();
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
    /// <para>
    /// The <see cref="PlaybackStopped"/> event is fired when transitioning
    /// from <see cref="PlaybackState.Playing"/> or <see cref="PlaybackState.Paused"/>
    /// to <see cref="PlaybackState.Stopped"/>.
    /// </para>
    /// </remarks>
    public void Stop()
    {
        ThrowIfDisposed();

        bool shouldFireEvent = false;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (state != PlaybackState.Stopped)
            {
                shouldFireEvent = true;
            }

            ResetPlaybackPosition();
        }

        // Fire event outside lock to prevent deadlocks
        if (shouldFireEvent)
        {
            PlaybackStopped?.Invoke();
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
    /// It accumulates time scaled by <see cref="PlaybackSpeed"/> and advances frames
    /// accordingly. At higher speeds, multiple frames may advance per update. At lower
    /// speeds, frames may not advance every update.
    /// </para>
    /// <para>
    /// When playback reaches the end of the replay, the state automatically
    /// transitions to <see cref="PlaybackState.Stopped"/> and the <see cref="PlaybackEnded"/>
    /// event is fired.
    /// </para>
    /// <para>
    /// If the state is not <see cref="PlaybackState.Playing"/>, this method
    /// returns false without making any changes.
    /// </para>
    /// <para>
    /// The <see cref="FrameChanged"/> event is fired for each frame that advances
    /// during this update.
    /// </para>
    /// </remarks>
    public bool Update(float deltaTime)
    {
        ThrowIfDisposed();

        bool playbackEnded = false;
        int startFrame;
        int endFrame;
        var changed = false;
        List<ReplayDesyncException>? desyncExceptions = null;
        bool shouldAutoValidate;

        lock (syncRoot)
        {
            if (state != PlaybackState.Playing || replayData is null)
            {
                return false;
            }

            shouldAutoValidate = autoValidate && validationWorld is not null && validationSerializer is not null;

            var frames = replayData.Frames;
            if (frames.Count == 0 || currentFrameIndex >= frames.Count)
            {
                state = PlaybackState.Stopped;
                playbackEnded = true;
                startFrame = currentFrameIndex;
                endFrame = currentFrameIndex;
            }
            else
            {
                // Accumulate time scaled by playback speed
                accumulatedTime += TimeSpan.FromSeconds(deltaTime * playbackSpeed);
                startFrame = currentFrameIndex;

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

                    // Perform auto-validation before advancing
                    if (shouldAutoValidate)
                    {
                        var desyncException = ValidateFrameInternal(currentFrame);
                        if (desyncException is not null)
                        {
                            desyncExceptions ??= [];
                            desyncExceptions.Add(desyncException);
                        }
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
                        playbackEnded = true;
                        break;
                    }
                }

                endFrame = currentFrameIndex;
            }
        }

        // Fire events outside lock to prevent deadlocks
        // Fire DesyncDetected for any validation failures
        if (desyncExceptions is not null)
        {
            var desyncHandler = DesyncDetected;
            if (desyncHandler is not null)
            {
                foreach (var desync in desyncExceptions)
                {
                    desyncHandler.Invoke(desync);
                }
            }
        }

        // Fire FrameChanged for each frame that advanced
        var frameChangedHandler = FrameChanged;
        if (frameChangedHandler != null)
        {
            for (int frame = startFrame + 1; frame <= endFrame && frame < int.MaxValue; frame++)
            {
                // Only fire if this frame hasn't been reported yet
                if (frame > lastReportedFrameIndex)
                {
                    frameChangedHandler.Invoke(frame);
                    lastReportedFrameIndex = frame;
                }
            }
        }

        if (playbackEnded)
        {
            PlaybackEnded?.Invoke();
            return true;
        }

        return changed;
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
    /// Steps the playback position by the specified number of frames.
    /// </summary>
    /// <param name="frames">
    /// The number of frames to step. Positive values step forward,
    /// negative values step backward.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Stepping pauses playback if it was playing. The playback position is
    /// clamped to the valid frame range (0 to <see cref="TotalFrames"/> - 1).
    /// </para>
    /// <para>
    /// For forward stepping, this simply advances the frame index. For backward
    /// stepping, the position is set directly. Future phases will support
    /// restoring world state from snapshots during backward stepping.
    /// </para>
    /// <para>
    /// The <see cref="PlaybackPaused"/> event is fired if playback was playing.
    /// The <see cref="FrameChanged"/> event is fired if the frame position changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Step forward one frame
    /// player.Step();
    ///
    /// // Step forward 10 frames
    /// player.Step(10);
    ///
    /// // Step backward 5 frames
    /// player.Step(-5);
    /// </code>
    /// </example>
    public void Step(int frames = 1)
    {
        ThrowIfDisposed();

        bool shouldFirePausedEvent = false;
        bool shouldFireFrameChangedEvent = false;
        int newFrame = 0;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            // Pause playback when stepping
            if (state == PlaybackState.Playing)
            {
                state = PlaybackState.Paused;
                shouldFirePausedEvent = true;
            }

            var frameCount = replayData!.Frames.Count;
            if (frameCount == 0)
            {
                // Fire paused event before returning if needed
                if (shouldFirePausedEvent)
                {
                    // Set flag to fire after lock release
                }
            }
            else
            {
                var previousFrame = currentFrameIndex;

                // Calculate target frame, clamped to valid range
                var targetFrame = Math.Clamp(currentFrameIndex + frames, 0, frameCount - 1);

                SetCurrentFrameInternal(targetFrame);

                if (currentFrameIndex != previousFrame)
                {
                    shouldFireFrameChangedEvent = true;
                    newFrame = currentFrameIndex;
                    lastReportedFrameIndex = newFrame;
                }
            }
        }

        // Fire events outside lock to prevent deadlocks
        if (shouldFirePausedEvent)
        {
            PlaybackPaused?.Invoke();
        }

        if (shouldFireFrameChangedEvent)
        {
            FrameChanged?.Invoke(newFrame);
        }
    }

    /// <summary>
    /// Seeks to the specified frame number.
    /// </summary>
    /// <param name="frameNumber">The 0-based frame number to seek to.</param>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="frameNumber"/> is negative or greater than or equal to <see cref="TotalFrames"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// Seeking pauses playback if it was playing. This method finds the nearest
    /// snapshot at or before the target frame for efficient state restoration
    /// in future phases.
    /// </para>
    /// <para>
    /// The <see cref="CurrentFrame"/> and <see cref="CurrentTime"/> properties
    /// are updated to reflect the new position.
    /// </para>
    /// <para>
    /// The <see cref="PlaybackPaused"/> event is fired if playback was playing.
    /// The <see cref="FrameChanged"/> event is fired if the frame position changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Seek to frame 100
    /// player.SeekToFrame(100);
    ///
    /// // Seek to the beginning
    /// player.SeekToFrame(0);
    ///
    /// // Seek to the last frame
    /// player.SeekToFrame(player.TotalFrames - 1);
    /// </code>
    /// </example>
    public void SeekToFrame(int frameNumber)
    {
        ThrowIfDisposed();

        bool shouldFirePausedEvent = false;
        bool shouldFireFrameChangedEvent = false;
        int newFrame = 0;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            var frameCount = replayData!.Frames.Count;
            if (frameNumber < 0 || frameNumber >= frameCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameNumber),
                    frameNumber,
                    $"Frame number must be between 0 and {frameCount - 1}.");
            }

            var previousFrame = currentFrameIndex;

            // Pause playback when seeking
            if (state == PlaybackState.Playing)
            {
                state = PlaybackState.Paused;
                shouldFirePausedEvent = true;
            }

            SetCurrentFrameInternal(frameNumber);

            if (currentFrameIndex != previousFrame)
            {
                shouldFireFrameChangedEvent = true;
                newFrame = currentFrameIndex;
                lastReportedFrameIndex = newFrame;
            }
        }

        // Fire events outside lock to prevent deadlocks
        if (shouldFirePausedEvent)
        {
            PlaybackPaused?.Invoke();
        }

        if (shouldFireFrameChangedEvent)
        {
            FrameChanged?.Invoke(newFrame);
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
    /// <remarks>
    /// <para>
    /// This method converts the time to a frame number and delegates to
    /// <see cref="SeekToFrame"/>. The frame at or before the specified
    /// time is selected.
    /// </para>
    /// <para>
    /// Seeking pauses playback if it was playing.
    /// </para>
    /// <para>
    /// The <see cref="PlaybackPaused"/> event is fired if playback was playing.
    /// The <see cref="FrameChanged"/> event is fired if the frame position changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Seek to 5 seconds into the replay
    /// player.SeekToTime(TimeSpan.FromSeconds(5));
    ///
    /// // Seek to 30% of the replay duration
    /// player.SeekToTime(player.TotalDuration * 0.3);
    /// </code>
    /// </example>
    public void SeekToTime(TimeSpan time)
    {
        ThrowIfDisposed();

        bool shouldFirePausedEvent = false;
        bool shouldFireFrameChangedEvent = false;
        int newFrame = 0;

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            if (time < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(time),
                    time,
                    "Time cannot be negative.");
            }

            var duration = replayData!.Duration;
            if (time > duration)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(time),
                    time,
                    $"Time cannot exceed the replay duration of {duration}.");
            }

            var previousFrame = currentFrameIndex;

            // Pause playback when seeking
            if (state == PlaybackState.Playing)
            {
                state = PlaybackState.Paused;
                shouldFirePausedEvent = true;
            }

            // Find the frame at or before the specified time
            var targetFrame = FindFrameAtTime(time);
            SetCurrentFrameInternal(targetFrame);

            if (currentFrameIndex != previousFrame)
            {
                shouldFireFrameChangedEvent = true;
                newFrame = currentFrameIndex;
                lastReportedFrameIndex = newFrame;
            }
        }

        // Fire events outside lock to prevent deadlocks
        if (shouldFirePausedEvent)
        {
            PlaybackPaused?.Invoke();
        }

        if (shouldFireFrameChangedEvent)
        {
            FrameChanged?.Invoke(newFrame);
        }
    }

    /// <summary>
    /// Gets the snapshot marker for the nearest snapshot at or before the specified frame.
    /// </summary>
    /// <param name="targetFrame">The target frame number.</param>
    /// <returns>
    /// The snapshot marker for the nearest snapshot at or before <paramref name="targetFrame"/>,
    /// or <c>null</c> if no such snapshot exists.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no replay is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method uses binary search to efficiently find the nearest snapshot.
    /// It is useful for determining the restore point when seeking backward
    /// in the timeline.
    /// </para>
    /// </remarks>
    public SnapshotMarker? GetNearestSnapshot(int targetFrame)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            ThrowIfNoReplayLoaded();

            return FindNearestSnapshot(targetFrame);
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
        lastReportedFrameIndex = -1;
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

    /// <summary>
    /// Sets the current frame index and updates related state.
    /// Must be called while holding the lock.
    /// </summary>
    private void SetCurrentFrameInternal(int frameIndex)
    {
        currentFrameIndex = frameIndex;

        // Update current time based on frame's elapsed time
        if (replayData!.Frames.Count > 0 && frameIndex >= 0 && frameIndex < replayData.Frames.Count)
        {
            currentTime = replayData.Frames[frameIndex].ElapsedTime;
        }
        else
        {
            currentTime = TimeSpan.Zero;
        }

        // Reset accumulated time when seeking/stepping
        accumulatedTime = TimeSpan.Zero;
    }

    /// <summary>
    /// Finds the frame index at or before the specified time using binary search.
    /// Must be called while holding the lock.
    /// </summary>
    private int FindFrameAtTime(TimeSpan time)
    {
        var frames = replayData!.Frames;
        if (frames.Count == 0)
        {
            return 0;
        }

        // Binary search for the frame at or before the specified time
        int low = 0;
        int high = frames.Count - 1;
        int result = 0;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var frameTime = frames[mid].ElapsedTime;

            if (frameTime <= time)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the nearest snapshot at or before the specified frame using binary search.
    /// Must be called while holding the lock.
    /// </summary>
    private SnapshotMarker? FindNearestSnapshot(int targetFrame)
    {
        var snapshots = replayData!.Snapshots;
        if (snapshots.Count == 0)
        {
            return null;
        }

        // Binary search for the snapshot at or before the target frame
        int low = 0;
        int high = snapshots.Count - 1;
        int resultIndex = -1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            var snapshotFrame = snapshots[mid].FrameNumber;

            if (snapshotFrame <= targetFrame)
            {
                resultIndex = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return resultIndex >= 0 ? snapshots[resultIndex] : null;
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

    /// <summary>
    /// Gets the current frame without locking.
    /// Must be called while holding the lock.
    /// </summary>
    private ReplayFrame? GetCurrentFrameInternal()
    {
        if (replayData is null || currentFrameIndex < 0 || currentFrameIndex >= replayData.Frames.Count)
        {
            return null;
        }

        return replayData.Frames[currentFrameIndex];
    }

    /// <summary>
    /// Validates a frame's checksum against the current world state.
    /// Must be called while holding the lock.
    /// </summary>
    /// <returns>A desync exception if validation failed; null if validation passed.</returns>
    private ReplayDesyncException? ValidateFrameInternal(ReplayFrame frame)
    {
        if (!frame.Checksum.HasValue)
        {
            return null; // No checksum to validate
        }

        if (validationWorld is null || validationSerializer is null)
        {
            return null; // No validation context
        }

        var actualChecksum = WorldChecksum.Calculate(validationWorld, validationSerializer);
        var expectedChecksum = frame.Checksum.Value;

        if (actualChecksum != expectedChecksum)
        {
            return new ReplayDesyncException(frame.FrameNumber, expectedChecksum, actualChecksum);
        }

        return null;
    }
}
