using KeenEyes.Graphics.Abstractions;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Commands;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.Process;
using KeenEyes.TestBridge.ProcessImpl;
using KeenEyes.TestBridge.State;
using KeenEyes.Testing.Input;

namespace KeenEyes.TestBridge;

/// <summary>
/// In-process implementation of <see cref="ITestBridge"/> for direct testing.
/// </summary>
/// <remarks>
/// <para>
/// This implementation runs in the same process as the application under test,
/// providing direct access to the world and input context without IPC overhead.
/// </para>
/// <para>
/// Use this for unit tests, integration tests, and scenarios where the test code
/// can be linked directly into the application.
/// </para>
/// </remarks>
public sealed class InProcessBridge : ITestBridge
{
    private readonly World world;
    private readonly MockInputContext inputContext;
    private readonly InputControllerImpl inputController;
    private readonly StateControllerImpl stateController;
    private readonly CaptureControllerImpl captureController;
    private readonly ProcessControllerImpl processController;
    private readonly TestBridgeOptions options;
    private bool disposed;

    /// <summary>
    /// Creates a new in-process test bridge for the specified world.
    /// </summary>
    /// <param name="world">The world to bridge.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="graphicsContext">Optional graphics context for screenshot capture.</param>
    public InProcessBridge(World world, TestBridgeOptions? options = null, IGraphicsContext? graphicsContext = null)
    {
        this.world = world;
        this.options = options ?? new TestBridgeOptions();

        // Use provided input context or create a new one
        inputContext = (this.options.CustomInputContext as MockInputContext)
            ?? new MockInputContext(this.options.GamepadCount);

        inputController = new InputControllerImpl(inputContext);
        stateController = new StateControllerImpl(world);
        captureController = new CaptureControllerImpl(graphicsContext);
        processController = new ProcessControllerImpl();
    }

    /// <inheritdoc />
    public bool IsConnected => !disposed;

    /// <inheritdoc />
    public IInputController Input => inputController;

    /// <inheritdoc />
    public ICaptureController Capture => captureController;

    /// <inheritdoc />
    public IStateController State => stateController;

    /// <inheritdoc />
    public IProcessController Process => processController;

    /// <summary>
    /// Gets the underlying mock input context for direct access.
    /// </summary>
    public MockInputContext InputContext => inputContext;

    /// <summary>
    /// Gets the underlying world for direct access.
    /// </summary>
    public World World => world;

    /// <inheritdoc />
    public Task<CommandResult> ExecuteAsync(ITestCommand command, CancellationToken cancellationToken = default)
    {
        if (disposed)
        {
            return Task.FromResult(CommandResult.Fail("Bridge is disposed"));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Route command to appropriate handler
        return command.CommandType switch
        {
            // Commands would be handled here
            // For now, return not implemented
            _ => Task.FromResult(CommandResult.Fail($"Unknown command type: {command.CommandType}"))
        };
    }

    /// <inheritdoc />
    public async Task<bool> WaitForAsync(
        Func<IStateController, Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        if (disposed)
        {
            return false;
        }

        var interval = pollInterval ?? TimeSpan.FromMilliseconds(16); // ~60fps
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await condition(stateController))
            {
                return true;
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> WaitForAsync(
        Func<IStateController, bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        return await WaitForAsync(
            state => Task.FromResult(condition(state)),
            timeout,
            pollInterval,
            cancellationToken);
    }

    /// <summary>
    /// Called by the plugin to record frame time for performance metrics.
    /// </summary>
    /// <param name="frameTimeMs">The frame time in milliseconds.</param>
    internal void RecordFrameTime(double frameTimeMs)
    {
        stateController.RecordFrameTime(frameTimeMs);
        captureController.OnFrameComplete();
    }

    /// <summary>
    /// Called by the plugin to record system execution time.
    /// </summary>
    /// <param name="systemName">The system type name.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    internal void RecordSystemTime(string systemName, double executionTimeMs)
    {
        stateController.RecordSystemTime(systemName, executionTimeMs);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Dispose managed processes
        processController.Dispose();

        // Only dispose input context if we created it
        if (options.CustomInputContext == null)
        {
            inputContext.Dispose();
        }
    }
}
