using KeenEyes.Serialization;

namespace KeenEyes.Replay;

/// <summary>
/// Provides replay recording functionality for a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// The ReplayRecorder captures game events and periodic world snapshots during
/// gameplay, enabling later playback of recorded sessions. It integrates with
/// the world's event system and system hooks to automatically record events.
/// </para>
/// <para>
/// Recording is started with <see cref="StartRecording"/> and stopped with
/// <see cref="StopRecording"/>. Custom events can be recorded at any time
/// using <see cref="RecordEvent"/>.
/// </para>
/// <para>
/// This class is typically accessed through the world's extension API after
/// installing the <see cref="ReplayPlugin"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the plugin
/// world.InstallPlugin(new ReplayPlugin());
///
/// // Get the recorder from extensions
/// var recorder = world.GetExtension&lt;ReplayRecorder&gt;();
///
/// // Start recording
/// recorder.StartRecording("My Session");
///
/// // ... run your game loop ...
///
/// // Stop and get the recorded data
/// var replayData = recorder.StopRecording();
/// </code>
/// </example>
public sealed class ReplayRecorder
{
    private readonly IWorld _world;
    private readonly ReplayOptions _options;
    private readonly IComponentSerializer _serializer;

    private readonly List<ReplayFrame> _frames = [];
    private readonly List<SnapshotMarker> _snapshots = [];
    private readonly List<ReplayEvent> _currentFrameEvents = [];

    private bool _isRecording;
    private DateTimeOffset _recordingStarted;
    private TimeSpan _elapsedTime;
    private TimeSpan _timeSinceLastSnapshot;
    private int _frameNumber;
    private string? _recordingName;
    private IReadOnlyDictionary<string, object>? _recordingMetadata;

    // Ring buffer support
    private int _ringBufferStart;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayRecorder"/> class.
    /// </summary>
    /// <param name="world">The world to record.</param>
    /// <param name="serializer">The component serializer for snapshot creation.</param>
    /// <param name="options">The recording options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="serializer"/> is null.
    /// </exception>
    public ReplayRecorder(IWorld world, IComponentSerializer serializer, ReplayOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(serializer);

        _world = world;
        _serializer = serializer;
        _options = options ?? new ReplayOptions();
    }

    /// <summary>
    /// Gets a value indicating whether recording is currently active.
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// Gets the current frame number during recording.
    /// </summary>
    /// <remarks>
    /// Returns -1 if not currently recording.
    /// </remarks>
    public int CurrentFrameNumber => _isRecording ? _frameNumber : -1;

    /// <summary>
    /// Gets the elapsed recording time.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="TimeSpan.Zero"/> if not currently recording.
    /// </remarks>
    public TimeSpan ElapsedTime => _isRecording ? _elapsedTime : TimeSpan.Zero;

    /// <summary>
    /// Gets the number of frames recorded so far.
    /// </summary>
    /// <remarks>
    /// When using a ring buffer, this returns the current buffer size,
    /// not the total number of frames that have been recorded.
    /// </remarks>
    public int RecordedFrameCount => _frames.Count;

    /// <summary>
    /// Gets the number of snapshots captured so far.
    /// </summary>
    public int SnapshotCount => _snapshots.Count;

    /// <summary>
    /// Gets the recording options.
    /// </summary>
    public ReplayOptions Options => _options;

    /// <summary>
    /// Starts a new recording session.
    /// </summary>
    /// <param name="name">Optional name for the recording.</param>
    /// <param name="metadata">Optional metadata to include in the recording.</param>
    /// <exception cref="InvalidOperationException">Thrown when recording is already in progress.</exception>
    /// <remarks>
    /// <para>
    /// This clears any previously recorded data and begins a new recording.
    /// An initial snapshot is captured immediately to provide a restoration
    /// point for the beginning of the recording.
    /// </para>
    /// <para>
    /// If <paramref name="name"/> is null, <see cref="ReplayOptions.DefaultRecordingName"/>
    /// is used if set.
    /// </para>
    /// </remarks>
    public void StartRecording(string? name = null, IReadOnlyDictionary<string, object>? metadata = null)
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Recording is already in progress. Call StopRecording first.");
        }

        // Clear previous recording data
        _frames.Clear();
        _snapshots.Clear();
        _currentFrameEvents.Clear();
        _ringBufferStart = 0;

        // Initialize recording state
        _isRecording = true;
        _recordingStarted = DateTimeOffset.UtcNow;
        _elapsedTime = TimeSpan.Zero;
        _timeSinceLastSnapshot = TimeSpan.Zero;
        _frameNumber = 0;
        _recordingName = name ?? _options.DefaultRecordingName;
        _recordingMetadata = metadata;

        // Capture initial snapshot
        CaptureSnapshot();
    }

    /// <summary>
    /// Stops the current recording and returns the recorded data.
    /// </summary>
    /// <returns>The complete replay data, or null if no recording was in progress.</returns>
    /// <remarks>
    /// <para>
    /// This finalizes the recording by creating a <see cref="ReplayData"/> instance
    /// containing all recorded frames and snapshots. The recorder is then ready
    /// to start a new recording.
    /// </para>
    /// <para>
    /// If using a ring buffer, only the frames within the buffer window are included.
    /// </para>
    /// </remarks>
    public ReplayData? StopRecording()
    {
        if (!_isRecording)
        {
            return null;
        }

        _isRecording = false;

        // Build the final replay data
        IReadOnlyList<ReplayFrame> frames;
        IReadOnlyList<SnapshotMarker> snapshots;

        if (_options.UseRingBuffer && _ringBufferStart > 0)
        {
            // Extract frames from ring buffer in correct order
            var actualFrames = new List<ReplayFrame>(_frames.Count);
            for (var i = 0; i < _frames.Count; i++)
            {
                var index = (_ringBufferStart + i) % _frames.Count;
                actualFrames.Add(_frames[index]);
            }
            frames = actualFrames;

            // Filter snapshots to only include those within the ring buffer window
            var minFrameNumber = actualFrames.Count > 0 ? actualFrames[0].FrameNumber : 0;
            snapshots = _snapshots.Where(s => s.FrameNumber >= minFrameNumber).ToList();
        }
        else
        {
            frames = _frames.ToList();
            snapshots = _snapshots.ToList();
        }

        return new ReplayData
        {
            Name = _recordingName,
            RecordingStarted = _recordingStarted,
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = _elapsedTime,
            FrameCount = frames.Count,
            Frames = frames,
            Snapshots = snapshots,
            Metadata = _recordingMetadata
        };
    }

    /// <summary>
    /// Cancels the current recording without returning data.
    /// </summary>
    /// <remarks>
    /// This stops recording and clears all recorded data. Use this when you
    /// want to abort a recording without saving it.
    /// </remarks>
    public void CancelRecording()
    {
        _isRecording = false;
        _frames.Clear();
        _snapshots.Clear();
        _currentFrameEvents.Clear();
    }

    /// <summary>
    /// Records a custom event.
    /// </summary>
    /// <param name="replayEvent">The event to record.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="replayEvent"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method adds the event to the current frame's event list.
    /// The event's timestamp should be relative to the frame start;
    /// if not set, it defaults to the current elapsed time within the frame.
    /// </para>
    /// <para>
    /// This method does nothing if recording is not in progress.
    /// </para>
    /// </remarks>
    public void RecordEvent(ReplayEvent replayEvent)
    {
        ArgumentNullException.ThrowIfNull(replayEvent);

        if (!_isRecording)
        {
            return;
        }

        _currentFrameEvents.Add(replayEvent);
    }

    /// <summary>
    /// Records a custom event with the specified type and optional data.
    /// </summary>
    /// <param name="customType">The custom event type name.</param>
    /// <param name="data">Optional event data.</param>
    /// <remarks>
    /// This is a convenience method for recording custom events without
    /// constructing a full <see cref="ReplayEvent"/> instance.
    /// </remarks>
    public void RecordCustomEvent(string customType, IReadOnlyDictionary<string, object>? data = null)
    {
        RecordEvent(new ReplayEvent
        {
            Type = ReplayEventType.Custom,
            CustomType = customType,
            Timestamp = TimeSpan.Zero, // Will be set relative to frame
            Data = data
        });
    }

    /// <summary>
    /// Called at the start of each frame to begin recording frame events.
    /// </summary>
    /// <param name="deltaTime">The delta time for this frame.</param>
    /// <remarks>
    /// This method is called automatically by the <see cref="ReplayPlugin"/>
    /// through system hooks. It should not be called directly.
    /// </remarks>
    internal void BeginFrame(float deltaTime)
    {
        if (!_isRecording)
        {
            return;
        }

        // Check if we've exceeded the maximum duration or frame count
        if (_options.MaxDuration.HasValue && _elapsedTime >= _options.MaxDuration.Value)
        {
            return; // Recording will be stopped by plugin
        }

        if (_options.MaxFrames.HasValue && _frameNumber >= _options.MaxFrames.Value)
        {
            return; // Recording will be stopped by plugin
        }

        _currentFrameEvents.Clear();

        // Record frame start event
        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.FrameStart,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Called at the end of each frame to finalize frame recording.
    /// </summary>
    /// <param name="deltaTime">The delta time for this frame.</param>
    /// <remarks>
    /// This method is called automatically by the <see cref="ReplayPlugin"/>
    /// through system hooks. It should not be called directly.
    /// </remarks>
    internal void EndFrame(float deltaTime)
    {
        if (!_isRecording)
        {
            return;
        }

        var dt = TimeSpan.FromSeconds(deltaTime);

        // Record frame end event
        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.FrameEnd,
            Timestamp = dt
        });

        // Determine if this frame follows a snapshot
        int? precedingSnapshotIndex = null;
        if (_snapshots.Count > 0 && _snapshots[^1].FrameNumber == _frameNumber)
        {
            precedingSnapshotIndex = _snapshots.Count - 1;
        }

        // Create the frame record
        var frame = new ReplayFrame
        {
            FrameNumber = _frameNumber,
            DeltaTime = dt,
            ElapsedTime = _elapsedTime,
            Events = _currentFrameEvents.ToList(),
            PrecedingSnapshotIndex = precedingSnapshotIndex
        };

        // Add to frames (with ring buffer support)
        if (_options.UseRingBuffer && _options.MaxFrames.HasValue)
        {
            if (_frames.Count < _options.MaxFrames.Value)
            {
                _frames.Add(frame);
            }
            else
            {
                // Overwrite oldest frame
                var index = _ringBufferStart % _options.MaxFrames.Value;
                _frames[index] = frame;
                _ringBufferStart = (index + 1) % _options.MaxFrames.Value;
            }
        }
        else
        {
            _frames.Add(frame);
        }

        // Update timing
        _elapsedTime += dt;
        _timeSinceLastSnapshot += dt;
        _frameNumber++;

        // Check if we need to capture a snapshot
        if (_options.SnapshotInterval > TimeSpan.Zero && _timeSinceLastSnapshot >= _options.SnapshotInterval)
        {
            CaptureSnapshot();
            _timeSinceLastSnapshot = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Records a system start event.
    /// </summary>
    /// <param name="systemTypeName">The type name of the system.</param>
    internal void RecordSystemStart(string systemTypeName)
    {
        if (!_isRecording || !_options.RecordSystemEvents)
        {
            return;
        }

        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.SystemStart,
            SystemTypeName = systemTypeName,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Records a system end event.
    /// </summary>
    /// <param name="systemTypeName">The type name of the system.</param>
    internal void RecordSystemEnd(string systemTypeName)
    {
        if (!_isRecording || !_options.RecordSystemEvents)
        {
            return;
        }

        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.SystemEnd,
            SystemTypeName = systemTypeName,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Records an entity creation event.
    /// </summary>
    /// <param name="entityId">The ID of the created entity.</param>
    /// <param name="name">The optional name of the entity.</param>
    internal void RecordEntityCreated(int entityId, string? name)
    {
        if (!_isRecording || !_options.RecordEntityEvents)
        {
            return;
        }

        var data = name is not null
            ? new Dictionary<string, object> { ["name"] = name }
            : null;

        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.EntityCreated,
            EntityId = entityId,
            Timestamp = TimeSpan.Zero,
            Data = data
        });
    }

    /// <summary>
    /// Records an entity destruction event.
    /// </summary>
    /// <param name="entityId">The ID of the destroyed entity.</param>
    internal void RecordEntityDestroyed(int entityId)
    {
        if (!_isRecording || !_options.RecordEntityEvents)
        {
            return;
        }

        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.EntityDestroyed,
            EntityId = entityId,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Records a component added event.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="componentTypeName">The type name of the added component.</param>
    internal void RecordComponentAdded(int entityId, string componentTypeName)
    {
        if (!_isRecording || !_options.RecordComponentEvents)
        {
            return;
        }

        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.ComponentAdded,
            EntityId = entityId,
            ComponentTypeName = componentTypeName,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Records a component removed event.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="componentTypeName">The type name of the removed component.</param>
    internal void RecordComponentRemoved(int entityId, string componentTypeName)
    {
        if (!_isRecording || !_options.RecordComponentEvents)
        {
            return;
        }

        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.ComponentRemoved,
            EntityId = entityId,
            ComponentTypeName = componentTypeName,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Forces capture of a snapshot at the current frame.
    /// </summary>
    /// <remarks>
    /// This can be called to capture a snapshot outside of the automatic
    /// snapshot interval, for example at significant game events.
    /// </remarks>
    public void CaptureSnapshot()
    {
        if (!_isRecording)
        {
            return;
        }

        // Get the concrete World for snapshot creation
        if (_world is not World concreteWorld)
        {
            return; // Can't create snapshot without concrete World
        }

        var snapshot = SnapshotManager.CreateSnapshot(concreteWorld, _serializer);
        var marker = new SnapshotMarker
        {
            FrameNumber = _frameNumber,
            ElapsedTime = _elapsedTime,
            Snapshot = snapshot
        };

        _snapshots.Add(marker);

        // Record snapshot event
        _currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.Snapshot,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Checks if recording should automatically stop based on configured limits.
    /// </summary>
    /// <returns>True if recording should stop; otherwise, false.</returns>
    internal bool ShouldStopRecording()
    {
        if (!_isRecording)
        {
            return false;
        }

        // Don't auto-stop if using ring buffer (it just overwrites)
        if (_options.UseRingBuffer)
        {
            return false;
        }

        if (_options.MaxDuration.HasValue && _elapsedTime >= _options.MaxDuration.Value)
        {
            return true;
        }

        if (_options.MaxFrames.HasValue && _frameNumber >= _options.MaxFrames.Value)
        {
            return true;
        }

        return false;
    }
}
