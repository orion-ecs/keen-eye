using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Replay;

/// <summary>
/// Plugin that adds replay recording capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// The ReplayPlugin provides replay recording functionality for debugging,
/// crash reproduction, and gameplay analysis. It captures world events and
/// periodic snapshots that can be saved and played back later.
/// </para>
/// <para>
/// Recording has zero overhead when not actively recording. The plugin uses
/// system hooks for frame boundary tracking and the world's event system
/// to capture entity and component changes.
/// </para>
/// <para>
/// After installation, access the <see cref="ReplayRecorder"/> through the
/// world's extension API to control recording.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install with default options
/// using var world = new World();
/// var serializer = new ComponentSerializationRegistry(); // Generated
/// world.InstallPlugin(new ReplayPlugin(serializer));
///
/// // Get the recorder and start recording
/// var recorder = world.GetExtension&lt;ReplayRecorder&gt;();
/// recorder.StartRecording("Debug Session");
///
/// // Run your game loop
/// for (int i = 0; i &lt; 1000; i++)
/// {
///     world.Update(0.016f);
/// }
///
/// // Stop and get the replay data
/// var replayData = recorder.StopRecording();
/// Console.WriteLine($"Recorded {replayData.FrameCount} frames");
/// </code>
/// </example>
/// <param name="serializer">
/// The component serializer for snapshot creation. Pass an instance of the
/// generated <c>ComponentSerializationRegistry</c>.
/// </param>
/// <param name="options">Optional configuration for the replay recording.</param>
public sealed class ReplayPlugin(IComponentSerializer serializer, ReplayOptions? options = null) : IWorldPlugin
{
    private readonly IComponentSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ReplayOptions _options = options ?? new ReplayOptions();

    private ReplayRecorder? _recorder;
    private EventSubscription? _frameHook;
    private EventSubscription? _systemHook;
    private EventSubscription? _entityCreatedSub;
    private EventSubscription? _entityDestroyedSub;
    private readonly List<EventSubscription> _componentSubs = [];

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => "Replay";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Create the recorder
        _recorder = new ReplayRecorder(context.World, _serializer, _options);
        context.SetExtension(_recorder);

        // Get the concrete world for event subscriptions
        if (context.World is not World world)
        {
            throw new InvalidOperationException(
                "ReplayPlugin requires a concrete World instance. " +
                "Mock worlds are not supported for replay recording.");
        }

        // Get system hook capability for frame tracking
        if (!context.TryGetCapability<ISystemHookCapability>(out var hookCapability) || hookCapability is null)
        {
            throw new InvalidOperationException(
                "ReplayPlugin requires ISystemHookCapability for frame tracking. " +
                "Ensure the world supports system hooks.");
        }

        // Set up frame boundary tracking
        // We use a hook that runs on ALL phases to catch the first and last system executions
        _frameHook = hookCapability.AddSystemHook(
            beforeHook: OnBeforeAnySystem,
            afterHook: OnAfterAnySystem,
            phase: null // All phases
        );

        // Set up system event recording if enabled
        if (_options.RecordSystemEvents)
        {
            _systemHook = hookCapability.AddSystemHook(
                beforeHook: (system, dt) => _recorder.RecordSystemStart(system.GetType().Name),
                afterHook: (system, dt) => _recorder.RecordSystemEnd(system.GetType().Name),
                phase: _options.SystemEventPhase
            );
        }

        // Set up entity event subscriptions if enabled
        if (_options.RecordEntityEvents)
        {
            _entityCreatedSub = world.OnEntityCreated((entity, name) =>
            {
                _recorder.RecordEntityCreated(entity.Id, name);
            });

            _entityDestroyedSub = world.OnEntityDestroyed(entity =>
            {
                _recorder.RecordEntityDestroyed(entity.Id);
            });
        }

        // Note: Component events would require generic subscriptions for each component type
        // which would need to be done differently. For Phase 1, we rely on snapshots for
        // component state. Full component event tracking can be added in a future phase.
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Stop any active recording
        _recorder?.CancelRecording();

        // Dispose all subscriptions
        _frameHook?.Dispose();
        _systemHook?.Dispose();
        _entityCreatedSub?.Dispose();
        _entityDestroyedSub?.Dispose();

        foreach (var sub in _componentSubs)
        {
            sub.Dispose();
        }
        _componentSubs.Clear();

        // Remove extension
        context.RemoveExtension<ReplayRecorder>();

        _recorder = null;
    }

    // Track whether we've seen the first system this frame
    private bool _firstSystemThisFrame = true;

    private void OnBeforeAnySystem(ISystem system, float deltaTime)
    {
        if (_recorder is null || !_recorder.IsRecording)
        {
            return;
        }

        if (_firstSystemThisFrame)
        {
            _firstSystemThisFrame = false;
            _recorder.BeginFrame(deltaTime);
        }
    }

    private void OnAfterAnySystem(ISystem system, float deltaTime)
    {
        if (_recorder is null || !_recorder.IsRecording)
        {
            return;
        }

        // We need a different approach - track when the frame actually ends
        // For now, we'll rely on the Update method being called externally
        // and use EndFrame tracking differently
    }

    /// <summary>
    /// Call this method after each world.Update() to finalize frame recording.
    /// </summary>
    /// <param name="deltaTime">The delta time passed to Update.</param>
    /// <remarks>
    /// <para>
    /// This method should be called after each world.Update() call to properly
    /// record frame boundaries. The plugin cannot automatically detect frame
    /// end without this explicit call.
    /// </para>
    /// <para>
    /// If recording limits have been exceeded, this method returns the completed
    /// replay data and stops recording.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The completed replay data if recording stopped due to limits; otherwise, null.
    /// </returns>
    public ReplayData? OnFrameEnd(float deltaTime)
    {
        if (_recorder is null || !_recorder.IsRecording)
        {
            return null;
        }

        _recorder.EndFrame(deltaTime);
        _firstSystemThisFrame = true; // Reset for next frame

        // Check if we should auto-stop
        if (_recorder.ShouldStopRecording())
        {
            return _recorder.StopRecording();
        }

        return null;
    }
}
