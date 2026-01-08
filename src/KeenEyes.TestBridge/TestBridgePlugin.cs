using System.Diagnostics;
using KeenEyes.Capabilities;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

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
    private IInputContext? originalInputContext;

    /// <inheritdoc />
    public string Name => "TestBridge";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        var world = context.World as World
            ?? throw new InvalidOperationException("TestBridgePlugin requires a concrete World instance.");

        // Try to get graphics context (optional - may not exist in headless mode)
        context.TryGetExtension<IGraphicsContext>(out var graphicsContext);

        // Try to get loop provider (optional - needed for render thread marshalling)
        context.TryGetExtension<ILoopProvider>(out var loopProvider);

        // Get existing input context for hybrid mode (may be null if no input plugin installed)
        context.TryGetExtension<IInputContext>(out originalInputContext);

        // Create options with real input context for hybrid mode
        var bridgeOptions = options with { RealInputContext = originalInputContext };

        // Create the in-process bridge
        bridge = new InProcessBridge(world, bridgeOptions, graphicsContext, loopProvider);

        // Expose the bridge as an extension
        context.SetExtension<ITestBridge>(bridge);

        // Also expose the concrete type for direct access
        context.SetExtension(bridge);

        // Replace the World's IInputContext with the composite (enables hybrid input)
        context.SetExtension<IInputContext>(bridge.InputContext);

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

        // Restore original input context if it existed, otherwise remove it
        if (originalInputContext != null)
        {
            context.SetExtension<IInputContext>(originalInputContext);
        }
        else
        {
            context.RemoveExtension<IInputContext>();
        }

        originalInputContext = null;

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
