using System.Numerics;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Provides playback functionality for a single ghost recording.
/// </summary>
/// <remarks>
/// <para>
/// The GhostPlayer loads ghost data and provides controls for playback,
/// including play, pause, stop, and frame interpolation. It supports
/// multiple synchronization modes for different gameplay scenarios.
/// </para>
/// <para>
/// Basic usage:
/// <list type="number">
/// <item><description>Create a GhostPlayer instance.</description></item>
/// <item><description>Load ghost data using <see cref="Load(GhostData)"/>.</description></item>
/// <item><description>Call <see cref="Play"/> to start playback.</description></item>
/// <item><description>Call <see cref="Update"/> each frame to advance playback.</description></item>
/// <item><description>Read <see cref="Position"/> and <see cref="Rotation"/> to render the ghost.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var player = new GhostPlayer();
///
/// // Load a ghost file
/// var (_, ghostData) = GhostFileFormat.ReadFromFile("ghost.keghost");
/// player.Load(ghostData);
///
/// // Configure sync mode
/// player.SyncMode = GhostSyncMode.TimeSynced;
///
/// // Start playback
/// player.Play();
///
/// // In your game loop
/// while (player.State == GhostPlaybackState.Playing)
/// {
///     player.Update(deltaTime);
///
///     // Render ghost at current position
///     RenderGhost(player.Position, player.Rotation, player.Scale);
/// }
/// </code>
/// </example>
public sealed class GhostPlayer : IDisposable
{
    private readonly Lock syncRoot = new();

    private GhostData? ghostData;
    private GhostPlaybackState state;
    private int currentFrameIndex;
    private TimeSpan currentTime;
    private float playbackSpeed = 1.0f;
    private GhostSyncMode syncMode = GhostSyncMode.TimeSynced;
    private bool disposed;

    // Cached interpolated values
    private Vector3 position = Vector3.Zero;
    private Quaternion rotation = Quaternion.Identity;
    private Vector3 scale = Vector3.One;
    private float currentDistance;

    /// <summary>
    /// Raised when playback starts or resumes.
    /// </summary>
    public event Action? PlaybackStarted;

    /// <summary>
    /// Raised when playback is paused.
    /// </summary>
    public event Action? PlaybackPaused;

    /// <summary>
    /// Raised when playback is stopped.
    /// </summary>
    public event Action? PlaybackStopped;

    /// <summary>
    /// Raised when playback reaches the end naturally.
    /// </summary>
    public event Action? PlaybackEnded;

    /// <summary>
    /// Raised when the current frame changes during playback.
    /// </summary>
    public event Action<int>? FrameChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="GhostPlayer"/> class.
    /// </summary>
    public GhostPlayer()
    {
    }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    public GhostPlaybackState State
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
    /// Gets a value indicating whether ghost data is loaded.
    /// </summary>
    public bool IsLoaded
    {
        get
        {
            lock (syncRoot)
            {
                return ghostData is not null;
            }
        }
    }

    /// <summary>
    /// Gets the loaded ghost data.
    /// </summary>
    public GhostData? LoadedGhost
    {
        get
        {
            lock (syncRoot)
            {
                return ghostData;
            }
        }
    }

    /// <summary>
    /// Gets the current frame index.
    /// </summary>
    public int CurrentFrame
    {
        get
        {
            lock (syncRoot)
            {
                return ghostData is not null ? currentFrameIndex : -1;
            }
        }
    }

    /// <summary>
    /// Gets the total number of frames in the loaded ghost.
    /// </summary>
    public int TotalFrames
    {
        get
        {
            lock (syncRoot)
            {
                return ghostData?.FrameCount ?? 0;
            }
        }
    }

    /// <summary>
    /// Gets the current playback time.
    /// </summary>
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
    /// Gets the total duration of the loaded ghost.
    /// </summary>
    public TimeSpan TotalDuration
    {
        get
        {
            lock (syncRoot)
            {
                return ghostData?.Duration ?? TimeSpan.Zero;
            }
        }
    }

    /// <summary>
    /// Gets the current interpolated position of the ghost.
    /// </summary>
    public Vector3 Position
    {
        get
        {
            lock (syncRoot)
            {
                return position;
            }
        }
    }

    /// <summary>
    /// Gets the current interpolated rotation of the ghost.
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            lock (syncRoot)
            {
                return rotation;
            }
        }
    }

    /// <summary>
    /// Gets the current interpolated scale of the ghost.
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            lock (syncRoot)
            {
                return scale;
            }
        }
    }

    /// <summary>
    /// Gets the current distance traveled by the ghost.
    /// </summary>
    public float Distance
    {
        get
        {
            lock (syncRoot)
            {
                return currentDistance;
            }
        }
    }

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    /// <value>
    /// A value between 0.25 and 4.0. Default is 1.0 (normal speed).
    /// </value>
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
            if (value < 0.25f || value > 4.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Playback speed must be between 0.25 and 4.0.");
            }

            lock (syncRoot)
            {
                playbackSpeed = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the synchronization mode for playback.
    /// </summary>
    public GhostSyncMode SyncMode
    {
        get
        {
            lock (syncRoot)
            {
                return syncMode;
            }
        }
        set
        {
            lock (syncRoot)
            {
                syncMode = value;
            }
        }
    }

    /// <summary>
    /// Loads ghost data for playback.
    /// </summary>
    /// <param name="ghost">The ghost data to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ghost"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Load(GhostData ghost)
    {
        ArgumentNullException.ThrowIfNull(ghost);
        ThrowIfDisposed();

        lock (syncRoot)
        {
            Unload();
            ghostData = ghost;
            ResetPlaybackPosition();

            // Initialize position from first frame
            if (ghost.Frames.Count > 0)
            {
                var firstFrame = ghost.Frames[0];
                position = firstFrame.Position;
                rotation = firstFrame.Rotation;
                scale = firstFrame.Scale;
                currentDistance = firstFrame.Distance;
            }
        }
    }

    /// <summary>
    /// Loads ghost data from a file.
    /// </summary>
    /// <param name="path">The path to the .keghost file.</param>
    /// <param name="validateChecksum">Whether to validate the file checksum.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="GhostFormatException">Thrown when the file format is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void LoadFromFile(string path, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(path);
        ThrowIfDisposed();

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Ghost file not found: {path}", path);
        }

        try
        {
            var (_, ghost) = GhostFileFormat.ReadFromFile(path, validateChecksum);
            Load(ghost);
        }
        catch (InvalidDataException ex)
        {
            throw GhostFormatException.Corrupted(path, ex.Message);
        }
    }

    /// <summary>
    /// Unloads the currently loaded ghost.
    /// </summary>
    public void Unload()
    {
        lock (syncRoot)
        {
            ghostData = null;
            ResetPlaybackPosition();
            position = Vector3.Zero;
            rotation = Quaternion.Identity;
            scale = Vector3.One;
            currentDistance = 0f;
        }
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no ghost is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Play()
    {
        ThrowIfDisposed();

        bool shouldFireEvent = false;

        lock (syncRoot)
        {
            ThrowIfNoGhostLoaded();

            if (state != GhostPlaybackState.Playing)
            {
                state = GhostPlaybackState.Playing;
                shouldFireEvent = true;
            }
        }

        if (shouldFireEvent)
        {
            PlaybackStarted?.Invoke();
        }
    }

    /// <summary>
    /// Pauses playback at the current position.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no ghost is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Pause()
    {
        ThrowIfDisposed();

        bool shouldFireEvent = false;

        lock (syncRoot)
        {
            ThrowIfNoGhostLoaded();

            if (state == GhostPlaybackState.Playing)
            {
                state = GhostPlaybackState.Paused;
                shouldFireEvent = true;
            }
        }

        if (shouldFireEvent)
        {
            PlaybackPaused?.Invoke();
        }
    }

    /// <summary>
    /// Stops playback and resets to the beginning.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no ghost is loaded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Stop()
    {
        ThrowIfDisposed();

        bool shouldFireEvent = false;

        lock (syncRoot)
        {
            ThrowIfNoGhostLoaded();

            if (state != GhostPlaybackState.Stopped)
            {
                shouldFireEvent = true;
            }

            ResetPlaybackPosition();

            // Reset to first frame position
            if (ghostData!.Frames.Count > 0)
            {
                var firstFrame = ghostData.Frames[0];
                position = firstFrame.Position;
                rotation = firstFrame.Rotation;
                scale = firstFrame.Scale;
                currentDistance = firstFrame.Distance;
            }
        }

        if (shouldFireEvent)
        {
            PlaybackStopped?.Invoke();
        }
    }

    /// <summary>
    /// Advances playback by the specified time delta.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <returns>True if the position changed; false otherwise.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// For <see cref="GhostSyncMode.TimeSynced"/>, the ghost position is interpolated
    /// based on accumulated time.
    /// </para>
    /// <para>
    /// For <see cref="GhostSyncMode.FrameSynced"/>, exactly one frame is advanced
    /// per call, regardless of delta time.
    /// </para>
    /// <para>
    /// For <see cref="GhostSyncMode.Independent"/>, the ghost plays at its own pace
    /// scaled by <see cref="PlaybackSpeed"/>.
    /// </para>
    /// </remarks>
    public bool Update(float deltaTime)
    {
        ThrowIfDisposed();

        bool playbackEnded = false;
        int previousFrame;
        int newFrame;

        lock (syncRoot)
        {
            if (state != GhostPlaybackState.Playing || ghostData is null)
            {
                return false;
            }

            var frames = ghostData.Frames;
            if (frames.Count == 0)
            {
                state = GhostPlaybackState.Stopped;
                return false;
            }

            previousFrame = currentFrameIndex;

            switch (syncMode)
            {
                case GhostSyncMode.TimeSynced:
                case GhostSyncMode.Independent:
                    playbackEnded = UpdateTimeSynced(deltaTime);
                    break;

                case GhostSyncMode.FrameSynced:
                    playbackEnded = UpdateFrameSynced();
                    break;

                case GhostSyncMode.DistanceSynced:
                    // Distance sync requires UpdateByDistance to be called
                    break;
            }

            newFrame = currentFrameIndex;
        }

        // Fire events outside lock
        if (newFrame != previousFrame)
        {
            FrameChanged?.Invoke(newFrame);
        }

        if (playbackEnded)
        {
            PlaybackEnded?.Invoke();
            return true;
        }

        return newFrame != previousFrame;
    }

    /// <summary>
    /// Updates the ghost position based on traveled distance.
    /// </summary>
    /// <param name="distance">The current distance traveled by the player.</param>
    /// <returns>True if the position changed; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method is used with <see cref="GhostSyncMode.DistanceSynced"/> to
    /// synchronize the ghost position with the player's progress along a track.
    /// </para>
    /// </remarks>
    public bool UpdateByDistance(float distance)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            if (state != GhostPlaybackState.Playing || ghostData is null)
            {
                return false;
            }

            var frames = ghostData.Frames;
            if (frames.Count < 2)
            {
                return false;
            }

            // Find frames surrounding the target distance
            int frameIndex = 0;
            for (int i = 0; i < frames.Count - 1; i++)
            {
                if (frames[i + 1].Distance > distance)
                {
                    frameIndex = i;
                    break;
                }
                frameIndex = i;
            }

            // Interpolate between frames
            var frameA = frames[frameIndex];
            var frameB = frames[Math.Min(frameIndex + 1, frames.Count - 1)];

            float t = 0f;
            float distanceDelta = frameB.Distance - frameA.Distance;
            if (distanceDelta > 0f)
            {
                t = (distance - frameA.Distance) / distanceDelta;
                t = Math.Clamp(t, 0f, 1f);
            }

            var interpolated = GhostFrame.Lerp(frameA, frameB, t);
            position = interpolated.Position;
            rotation = interpolated.Rotation;
            scale = interpolated.Scale;
            currentDistance = interpolated.Distance;

            bool frameChanged = currentFrameIndex != frameIndex;
            currentFrameIndex = frameIndex;
            currentTime = interpolated.ElapsedTime;

            return frameChanged;
        }
    }

    /// <summary>
    /// Seeks to the specified time in the ghost playback.
    /// </summary>
    /// <param name="time">The time to seek to.</param>
    /// <exception cref="InvalidOperationException">Thrown when no ghost is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when time is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void SeekToTime(TimeSpan time)
    {
        ThrowIfDisposed();

        bool shouldFireFrameChanged;
        int newFrame;

        lock (syncRoot)
        {
            ThrowIfNoGhostLoaded();

            if (time < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time cannot be negative.");
            }

            var frames = ghostData!.Frames;
            if (frames.Count == 0)
            {
                return;
            }

            var duration = ghostData.Duration;
            if (time > duration)
            {
                throw new ArgumentOutOfRangeException(nameof(time), time,
                    $"Time cannot exceed ghost duration of {duration}.");
            }

            // Pause if playing
            if (state == GhostPlaybackState.Playing)
            {
                state = GhostPlaybackState.Paused;
            }

            // Find frame at time
            int frameIndex = FindFrameAtTime(frames, time);
            shouldFireFrameChanged = currentFrameIndex != frameIndex;
            currentFrameIndex = frameIndex;
            newFrame = frameIndex;
            currentTime = time;

            // Interpolate position at exact time
            InterpolateAtTime(frames, time);
        }

        if (shouldFireFrameChanged)
        {
            FrameChanged?.Invoke(newFrame);
        }
    }

    /// <summary>
    /// Seeks to the specified frame.
    /// </summary>
    /// <param name="frameNumber">The frame number to seek to.</param>
    /// <exception cref="InvalidOperationException">Thrown when no ghost is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when frame is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void SeekToFrame(int frameNumber)
    {
        ThrowIfDisposed();

        bool shouldFireFrameChanged;
        int newFrame;

        lock (syncRoot)
        {
            ThrowIfNoGhostLoaded();

            var frames = ghostData!.Frames;
            if (frameNumber < 0 || frameNumber >= frames.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(frameNumber), frameNumber,
                    $"Frame must be between 0 and {frames.Count - 1}.");
            }

            // Pause if playing
            if (state == GhostPlaybackState.Playing)
            {
                state = GhostPlaybackState.Paused;
            }

            shouldFireFrameChanged = currentFrameIndex != frameNumber;
            currentFrameIndex = frameNumber;
            newFrame = frameNumber;

            var frame = frames[frameNumber];
            currentTime = frame.ElapsedTime;

            position = frame.Position;
            rotation = frame.Rotation;
            scale = frame.Scale;
            currentDistance = frame.Distance;
        }

        if (shouldFireFrameChanged)
        {
            FrameChanged?.Invoke(newFrame);
        }
    }

    /// <summary>
    /// Gets the frame data at the specified index.
    /// </summary>
    /// <param name="frameIndex">The frame index.</param>
    /// <returns>The ghost frame at the specified index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no ghost is loaded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public GhostFrame GetFrame(int frameIndex)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            ThrowIfNoGhostLoaded();

            var frames = ghostData!.Frames;
            if (frameIndex < 0 || frameIndex >= frames.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex,
                    $"Frame index must be between 0 and {frames.Count - 1}.");
            }

            return frames[frameIndex];
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="GhostPlayer"/>.
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

            Unload();
            disposed = true;
        }
    }

    private bool UpdateTimeSynced(float deltaTime)
    {
        var frames = ghostData!.Frames;

        // Accumulate time scaled by playback speed
        currentTime += TimeSpan.FromSeconds(deltaTime * playbackSpeed);

        // Check for end of playback
        var lastFrame = frames[^1];
        if (currentTime >= lastFrame.ElapsedTime)
        {
            currentFrameIndex = frames.Count - 1;
            position = lastFrame.Position;
            rotation = lastFrame.Rotation;
            scale = lastFrame.Scale;
            currentDistance = lastFrame.Distance;
            state = GhostPlaybackState.Stopped;
            return true;
        }

        // Find and interpolate between frames
        InterpolateAtTime(frames, currentTime);
        return false;
    }

    private bool UpdateFrameSynced()
    {
        var frames = ghostData!.Frames;

        currentFrameIndex++;

        if (currentFrameIndex >= frames.Count)
        {
            currentFrameIndex = frames.Count - 1;
            var lastFrame = frames[^1];
            position = lastFrame.Position;
            rotation = lastFrame.Rotation;
            scale = lastFrame.Scale;
            currentDistance = lastFrame.Distance;
            currentTime = lastFrame.ElapsedTime;
            state = GhostPlaybackState.Stopped;
            return true;
        }

        var frame = frames[currentFrameIndex];
        position = frame.Position;
        rotation = frame.Rotation;
        scale = frame.Scale;
        currentDistance = frame.Distance;
        currentTime = frame.ElapsedTime;
        return false;
    }

    private void InterpolateAtTime(IReadOnlyList<GhostFrame> frames, TimeSpan time)
    {
        // Find frames surrounding the target time
        int frameIndex = FindFrameAtTime(frames, time);
        currentFrameIndex = frameIndex;

        var frameA = frames[frameIndex];
        var frameB = frames[Math.Min(frameIndex + 1, frames.Count - 1)];

        // Calculate interpolation factor
        float t = 0f;
        var timeDelta = (frameB.ElapsedTime - frameA.ElapsedTime).TotalSeconds;
        if (timeDelta > 0)
        {
            t = (float)((time - frameA.ElapsedTime).TotalSeconds / timeDelta);
            t = Math.Clamp(t, 0f, 1f);
        }

        // Interpolate
        var interpolated = GhostFrame.Lerp(frameA, frameB, t);
        position = interpolated.Position;
        rotation = interpolated.Rotation;
        scale = interpolated.Scale;
        currentDistance = interpolated.Distance;
    }

    private static int FindFrameAtTime(IReadOnlyList<GhostFrame> frames, TimeSpan time)
    {
        // Binary search for frame at or before time
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

    private void ResetPlaybackPosition()
    {
        state = GhostPlaybackState.Stopped;
        currentFrameIndex = 0;
        currentTime = TimeSpan.Zero;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private void ThrowIfNoGhostLoaded()
    {
        if (ghostData is null)
        {
            throw new InvalidOperationException("No ghost is loaded. Call Load first.");
        }
    }
}
