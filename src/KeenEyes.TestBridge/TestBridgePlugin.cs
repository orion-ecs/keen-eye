using System.Diagnostics;
using KeenEyes.Capabilities;

namespace KeenEyes.TestBridge;

/// <summary>
/// Plugin that installs the test bridge into a world for automated testing.
/// </summary>
/// <remarks>
/// <para>
/// The test bridge plugin provides input injection, state queries, and screenshot
/// capture capabilities for automated testing of KeenEyes applications.
/// </para>
/// <para>
/// Install this plugin during test setup:
/// </para>
/// <code>
/// world.InstallPlugin(new TestBridgePlugin());
/// var bridge = world.GetExtension&lt;ITestBridge&gt;();
/// </code>
/// </remarks>
/// <param name="options">Configuration options. If null, default options are used.</param>
public sealed class TestBridgePlugin(TestBridgeOptions? options = null) : IWorldPlugin
{
    private readonly TestBridgeOptions options = options ?? new TestBridgeOptions();
    private InProcessBridge? bridge;
    private EventSubscription? systemHookSubscription;
    private readonly Stopwatch frameStopwatch = new();

    /// <inheritdoc />
    public string Name => "TestBridge";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        var world = context.World as World
            ?? throw new InvalidOperationException("TestBridgePlugin requires a concrete World instance.");

        // Create the in-process bridge
        bridge = new InProcessBridge(world, options);

        // Expose the bridge as an extension
        context.SetExtension<ITestBridge>(bridge);

        // Also expose the concrete type for direct access
        context.SetExtension(bridge);

        // Hook into system execution for profiling
        if (context.TryGetCapability<ISystemHookCapability>(out var hookCapability) && hookCapability is not null)
        {
            var systemStopwatch = new Stopwatch();

            systemHookSubscription = hookCapability.AddSystemHook(
                beforeHook: (system, dt) =>
                {
                    systemStopwatch.Restart();
                },
                afterHook: (system, dt) =>
                {
                    systemStopwatch.Stop();
                    bridge.RecordSystemTime(
                        system.GetType().Name,
                        systemStopwatch.Elapsed.TotalMilliseconds);
                });
        }

        // Start frame timing
        frameStopwatch.Start();
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Stop frame timing
        frameStopwatch.Stop();

        // Unsubscribe from system hooks
        systemHookSubscription?.Dispose();
        systemHookSubscription = null;

        // Remove extensions
        context.RemoveExtension<ITestBridge>();
        context.RemoveExtension<InProcessBridge>();

        // Dispose the bridge
        bridge?.Dispose();
        bridge = null;
    }

    /// <summary>
    /// Call this at the end of each frame to record frame timing.
    /// </summary>
    /// <remarks>
    /// This is typically called by the game loop or a PostRender system.
    /// </remarks>
    public void OnFrameComplete()
    {
        if (bridge != null)
        {
            bridge.RecordFrameTime(frameStopwatch.Elapsed.TotalMilliseconds);
            frameStopwatch.Restart();
        }
    }
}
