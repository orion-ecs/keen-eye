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
/// Frame boundaries are detected automatically using an internal system that
/// runs at the end of the Update phase. No manual intervention is required.
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
/// // Run your game loop - frames are recorded automatically
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
    private readonly IComponentSerializer componentSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ReplayOptions replayOptions = options ?? new ReplayOptions();

    private ReplayRecorder? recorder;
    private ReplayFrameEndSystem? frameEndSystem;
    private EventSubscription? frameHook;
    private EventSubscription? systemHook;
    private EventSubscription? entityCreatedSub;
    private EventSubscription? entityDestroyedSub;
    private readonly List<EventSubscription> componentSubs = [];

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => "Replay";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Create the recorder
        recorder = new ReplayRecorder(context.World, componentSerializer, replayOptions);
        context.SetExtension(recorder);

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
        frameHook = hookCapability.AddSystemHook(
            beforeHook: OnBeforeAnySystem,
            afterHook: OnAfterAnySystem,
            phase: null // All phases
        );

        // Set up system event recording if enabled
        if (replayOptions.RecordSystemEvents)
        {
            systemHook = hookCapability.AddSystemHook(
                beforeHook: (system, dt) => recorder.RecordSystemStart(system.GetType().Name),
                afterHook: (system, dt) => recorder.RecordSystemEnd(system.GetType().Name),
                phase: replayOptions.SystemEventPhase
            );
        }

        // Set up entity event subscriptions if enabled
        if (replayOptions.RecordEntityEvents)
        {
            entityCreatedSub = world.OnEntityCreated((entity, name) =>
            {
                recorder.RecordEntityCreated(entity.Id, name);
            });

            entityDestroyedSub = world.OnEntityDestroyed(entity =>
            {
                recorder.RecordEntityDestroyed(entity.Id);
            });
        }

        // Note: Component events would require generic subscriptions for each component type
        // which would need to be done differently. For Phase 1, we rely on snapshots for
        // component state. Full component event tracking can be added in a future phase.

        // Register frame end system to automatically finalize frames
        // Runs at int.MaxValue order to ensure it executes after all other systems
        frameEndSystem = new ReplayFrameEndSystem(recorder, this);
        context.AddSystem(frameEndSystem, SystemPhase.Update, int.MaxValue);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Stop any active recording
        recorder?.CancelRecording();

        // Dispose all subscriptions
        frameHook?.Dispose();
        systemHook?.Dispose();
        entityCreatedSub?.Dispose();
        entityDestroyedSub?.Dispose();

        foreach (var sub in componentSubs)
        {
            sub.Dispose();
        }
        componentSubs.Clear();

        // Remove extension
        context.RemoveExtension<ReplayRecorder>();

        recorder = null;
    }

    // Track whether we've seen the first system this frame
    private bool firstSystemThisFrame = true;

    private void OnBeforeAnySystem(ISystem system, float deltaTime)
    {
        if (recorder is null || !recorder.IsRecording)
        {
            return;
        }

        if (firstSystemThisFrame)
        {
            firstSystemThisFrame = false;
            recorder.BeginFrame(deltaTime);
        }
    }

    private void OnAfterAnySystem(ISystem system, float deltaTime)
    {
        // Frame end is now handled by ReplayFrameEndSystem
        // This hook remains for potential future use (e.g., tracking last system per phase)
    }

    /// <summary>
    /// Manually triggers frame end processing.
    /// </summary>
    /// <param name="deltaTime">The delta time for the frame.</param>
    /// <remarks>
    /// <para>
    /// This method is provided for advanced scenarios where manual control over
    /// frame boundaries is needed. In most cases, you don't need to call this
    /// method as frame boundaries are detected automatically.
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
        if (recorder is null || !recorder.IsRecording)
        {
            return null;
        }

        // Note: EndFrame is already called by ReplayFrameEndSystem
        // This method is here for manual control if needed
        firstSystemThisFrame = true; // Reset for next frame

        // Check if we should auto-stop
        if (recorder.ShouldStopRecording())
        {
            return recorder.StopRecording();
        }

        return null;
    }

    /// <summary>
    /// Called internally by ReplayFrameEndSystem after each frame.
    /// </summary>
    internal void ResetFrameTracking()
    {
        firstSystemThisFrame = true;
    }
}
