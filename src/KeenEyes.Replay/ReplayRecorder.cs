using System.Numerics;
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
public sealed class ReplayRecorder : IInputRecorder
{
    private readonly IWorld world;
    private readonly ReplayOptions options;
    private readonly IComponentSerializer serializer;

    private readonly List<ReplayFrame> frames = [];
    private readonly List<SnapshotMarker> snapshots = [];
    private readonly List<ReplayEvent> currentFrameEvents = [];
    private readonly List<InputEvent> currentFrameInputs = [];

    private bool isRecording;
    private DateTimeOffset recordingStarted;
    private TimeSpan elapsedTime;
    private TimeSpan timeSinceLastSnapshot;
    private int frameNumber;
    private string? recordingName;
    private IReadOnlyDictionary<string, object>? recordingMetadata;

    // Ring buffer support
    private int ringBufferStart;

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

        this.world = world;
        this.serializer = serializer;
        this.options = options ?? new ReplayOptions();
    }

    /// <summary>
    /// Gets a value indicating whether recording is currently active.
    /// </summary>
    public bool IsRecording => isRecording;

    /// <summary>
    /// Gets the current frame number during recording.
    /// </summary>
    /// <remarks>
    /// Returns -1 if not currently recording.
    /// </remarks>
    public int CurrentFrameNumber => isRecording ? frameNumber : -1;

    /// <summary>
    /// Gets the elapsed recording time.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="TimeSpan.Zero"/> if not currently recording.
    /// </remarks>
    public TimeSpan ElapsedTime => isRecording ? elapsedTime : TimeSpan.Zero;

    /// <summary>
    /// Gets the number of frames recorded so far.
    /// </summary>
    /// <remarks>
    /// When using a ring buffer, this returns the current buffer size,
    /// not the total number of frames that have been recorded.
    /// </remarks>
    public int RecordedFrameCount => frames.Count;

    /// <summary>
    /// Gets the number of snapshots captured so far.
    /// </summary>
    public int SnapshotCount => snapshots.Count;

    /// <summary>
    /// Gets the recording options.
    /// </summary>
    public ReplayOptions Options => options;

    /// <inheritdoc/>
    public bool IsRecordingInputs => isRecording;

    /// <inheritdoc/>
    public int CurrentInputFrame => isRecording ? frameNumber : -1;

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
        if (isRecording)
        {
            throw new InvalidOperationException("Recording is already in progress. Call StopRecording first.");
        }

        // Clear previous recording data
        frames.Clear();
        snapshots.Clear();
        currentFrameEvents.Clear();
        currentFrameInputs.Clear();
        ringBufferStart = 0;

        // Initialize recording state
        isRecording = true;
        recordingStarted = DateTimeOffset.UtcNow;
        elapsedTime = TimeSpan.Zero;
        timeSinceLastSnapshot = TimeSpan.Zero;
        frameNumber = 0;
        recordingName = name ?? options.DefaultRecordingName;
        recordingMetadata = metadata;

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
        if (!isRecording)
        {
            return null;
        }

        isRecording = false;

        // Build the final replay data
        IReadOnlyList<ReplayFrame> finalFrames;
        IReadOnlyList<SnapshotMarker> finalSnapshots;

        if (options.UseRingBuffer && ringBufferStart > 0)
        {
            // Extract frames from ring buffer in correct order
            var actualFrames = new List<ReplayFrame>(frames.Count);
            for (var i = 0; i < frames.Count; i++)
            {
                var index = (ringBufferStart + i) % frames.Count;
                actualFrames.Add(frames[index]);
            }
            finalFrames = actualFrames;

            // Filter snapshots to only include those within the ring buffer window
            var minFrameNumber = actualFrames.Count > 0 ? actualFrames[0].FrameNumber : 0;
            finalSnapshots = snapshots.Where(s => s.FrameNumber >= minFrameNumber).ToList();
        }
        else
        {
            finalFrames = frames.ToList();
            finalSnapshots = snapshots.ToList();
        }

        return new ReplayData
        {
            Name = recordingName,
            RecordingStarted = recordingStarted,
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = elapsedTime,
            FrameCount = finalFrames.Count,
            Frames = finalFrames,
            Snapshots = finalSnapshots,
            Metadata = recordingMetadata
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
        isRecording = false;
        frames.Clear();
        snapshots.Clear();
        currentFrameEvents.Clear();
        currentFrameInputs.Clear();
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

        if (!isRecording)
        {
            return;
        }

        currentFrameEvents.Add(replayEvent);
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
        if (!isRecording)
        {
            return;
        }

        // Check if we've exceeded the maximum duration or frame count
        if (options.MaxDuration.HasValue && elapsedTime >= options.MaxDuration.Value)
        {
            return; // Recording will be stopped by plugin
        }

        if (options.MaxFrames.HasValue && frameNumber >= options.MaxFrames.Value)
        {
            return; // Recording will be stopped by plugin
        }

        currentFrameEvents.Clear();
        currentFrameInputs.Clear();

        // Record frame start event
        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording)
        {
            return;
        }

        var dt = TimeSpan.FromSeconds(deltaTime);

        // Record frame end event
        currentFrameEvents.Add(new ReplayEvent
        {
            Type = ReplayEventType.FrameEnd,
            Timestamp = dt
        });

        // Determine if this frame follows a snapshot
        int? precedingSnapshotIndex = null;
        if (snapshots.Count > 0 && snapshots[^1].FrameNumber == frameNumber)
        {
            precedingSnapshotIndex = snapshots.Count - 1;
        }

        // Calculate checksum if enabled
        uint? checksum = null;
        if (options.RecordChecksums && world is World concreteWorld)
        {
            checksum = WorldChecksum.Calculate(concreteWorld, serializer);
        }

        // Create the frame record
        var frame = new ReplayFrame
        {
            FrameNumber = frameNumber,
            DeltaTime = dt,
            ElapsedTime = elapsedTime,
            Events = currentFrameEvents.ToList(),
            InputEvents = currentFrameInputs.ToList(),
            PrecedingSnapshotIndex = precedingSnapshotIndex,
            Checksum = checksum
        };

        // Add to frames (with ring buffer support)
        if (options.UseRingBuffer && options.MaxFrames.HasValue)
        {
            if (frames.Count < options.MaxFrames.Value)
            {
                frames.Add(frame);
            }
            else
            {
                // Overwrite oldest frame
                var index = ringBufferStart % options.MaxFrames.Value;
                frames[index] = frame;
                ringBufferStart = (index + 1) % options.MaxFrames.Value;
            }
        }
        else
        {
            frames.Add(frame);
        }

        // Update timing
        elapsedTime += dt;
        timeSinceLastSnapshot += dt;
        frameNumber++;

        // Check if we need to capture a snapshot
        if (options.SnapshotInterval > TimeSpan.Zero && timeSinceLastSnapshot >= options.SnapshotInterval)
        {
            CaptureSnapshot();
            timeSinceLastSnapshot = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Records a system start event.
    /// </summary>
    /// <param name="systemTypeName">The type name of the system.</param>
    internal void RecordSystemStart(string systemTypeName)
    {
        if (!isRecording || !options.RecordSystemEvents)
        {
            return;
        }

        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording || !options.RecordSystemEvents)
        {
            return;
        }

        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording || !options.RecordEntityEvents)
        {
            return;
        }

        var data = name is not null
            ? new Dictionary<string, object> { ["name"] = name }
            : null;

        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording || !options.RecordEntityEvents)
        {
            return;
        }

        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording || !options.RecordComponentEvents)
        {
            return;
        }

        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording || !options.RecordComponentEvents)
        {
            return;
        }

        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording)
        {
            return;
        }

        // Get the concrete World for snapshot creation
        if (world is not World concreteWorld)
        {
            return; // Can't create snapshot without concrete World
        }

        var snapshot = SnapshotManager.CreateSnapshot(concreteWorld, serializer);

        // Calculate checksum if enabled
        uint? checksum = null;
        if (options.RecordChecksums)
        {
            checksum = WorldChecksum.Calculate(concreteWorld, serializer);
        }

        var marker = new SnapshotMarker
        {
            FrameNumber = frameNumber,
            ElapsedTime = elapsedTime,
            Snapshot = snapshot,
            Checksum = checksum
        };

        snapshots.Add(marker);

        // Record snapshot event
        currentFrameEvents.Add(new ReplayEvent
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
        if (!isRecording)
        {
            return false;
        }

        // Don't auto-stop if using ring buffer (it just overwrites)
        if (options.UseRingBuffer)
        {
            return false;
        }

        if (options.MaxDuration.HasValue && elapsedTime >= options.MaxDuration.Value)
        {
            return true;
        }

        if (options.MaxFrames.HasValue && frameNumber >= options.MaxFrames.Value)
        {
            return true;
        }

        return false;
    }

    #region IInputRecorder Implementation

    /// <inheritdoc/>
    public void RecordInput(InputEvent inputEvent)
    {
        if (!isRecording)
        {
            return;
        }

        // Ensure the frame number is set correctly
        var eventWithFrame = inputEvent.Frame == frameNumber
            ? inputEvent
            : inputEvent with { Frame = frameNumber };

        currentFrameInputs.Add(eventWithFrame);
    }

    /// <inheritdoc/>
    public void RecordKeyDown(string key)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.KeyDown,
            Frame = frameNumber,
            Key = key,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordKeyUp(string key)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.KeyUp,
            Frame = frameNumber,
            Key = key,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordMouseMove(Vector2 position)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.MouseMove,
            Frame = frameNumber,
            Position = position,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordMouseButtonDown(string button, Vector2 position)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.MouseButtonDown,
            Frame = frameNumber,
            Key = button,
            Position = position,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordMouseButtonUp(string button, Vector2 position)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.MouseButtonUp,
            Frame = frameNumber,
            Key = button,
            Position = position,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordMouseWheel(float delta, Vector2 position)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.MouseWheel,
            Frame = frameNumber,
            Value = delta,
            Position = position,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordGamepadButton(string button, bool pressed)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.GamepadButton,
            Frame = frameNumber,
            Key = button,
            Value = pressed ? 1.0f : 0.0f,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordGamepadAxis(string axis, float value)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.GamepadAxis,
            Frame = frameNumber,
            Key = axis,
            Value = value,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordCustomInput(string customType, object? customData = null)
    {
        RecordInput(new InputEvent
        {
            Type = InputEventType.Custom,
            Frame = frameNumber,
            CustomType = customType,
            CustomData = customData,
            Timestamp = TimeSpan.Zero
        });
    }

    /// <inheritdoc/>
    public void RecordCustomInput<T>(string customType, T customData)
    {
        RecordCustomInput(customType, (object?)customData);
    }

    #endregion
}
