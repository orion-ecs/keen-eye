using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Commands;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.State;

namespace KeenEyes.TestBridge;

/// <summary>
/// Main interface for the debugging bridge, providing command execution
/// for automated testing of KeenEyes applications.
/// </summary>
/// <remarks>
/// <para>
/// The test bridge provides a unified API for controlling and inspecting
/// running KeenEyes applications. It supports input injection, screenshot
/// capture, and state queries.
/// </para>
/// <para>
/// The bridge can operate in two modes:
/// </para>
/// <list type="bullet">
/// <item><description>In-process: Test code runs alongside the application with direct API access</description></item>
/// <item><description>IPC: External test process connects via named pipes or TCP</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // In-process usage with TestWorld
/// using var testWorld = new TestWorldBuilder()
///     .WithTestBridge()
///     .Build();
///
/// await testWorld.TestBridge!.Input.KeyPressAsync(Key.Space);
/// testWorld.Step();
///
/// var entity = await testWorld.TestBridge.State.GetEntityByNameAsync("Player");
/// </code>
/// </example>
public interface ITestBridge : IDisposable
{
    /// <summary>
    /// Gets whether the bridge is currently connected and operational.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the input controller for injecting input events.
    /// </summary>
    IInputController Input { get; }

    /// <summary>
    /// Gets the capture controller for screenshots and frame capture.
    /// </summary>
    ICaptureController Capture { get; }

    /// <summary>
    /// Gets the state query controller for inspecting world state.
    /// </summary>
    IStateController State { get; }

    /// <summary>
    /// Executes a command and returns the result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command result.</returns>
    /// <remarks>
    /// Commands provide a low-level API for executing arbitrary operations.
    /// For most use cases, prefer the typed methods on <see cref="Input"/>,
    /// <see cref="Capture"/>, and <see cref="State"/>.
    /// </remarks>
    Task<CommandResult> ExecuteAsync(ITestCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for a condition to become true.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">How often to check the condition. Defaults to 16ms (~60fps).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the condition was met; false if timeout.</returns>
    /// <remarks>
    /// This is useful for waiting for game state to change after injecting input.
    /// The condition is evaluated synchronously on each poll.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Wait for player to spawn
    /// var spawned = await bridge.WaitForAsync(
    ///     async state => await state.GetEntityByNameAsync("Player") != null,
    ///     TimeSpan.FromSeconds(5));
    /// </code>
    /// </example>
    Task<bool> WaitForAsync(
        Func<IStateController, Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for a condition to become true using synchronous evaluation.
    /// </summary>
    /// <param name="condition">The condition to check synchronously.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">How often to check the condition. Defaults to 16ms (~60fps).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the condition was met; false if timeout.</returns>
    Task<bool> WaitForAsync(
        Func<IStateController, bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default);
}
