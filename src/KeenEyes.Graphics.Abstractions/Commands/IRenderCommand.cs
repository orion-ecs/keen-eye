namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Marker interface for render commands.
/// </summary>
/// <remarks>
/// <para>
/// Render commands represent discrete rendering operations that can be queued,
/// sorted, batched, and executed by a render backend. This command pattern enables:
/// <list type="bullet">
///   <item><description>Deferred rendering - commands queued now, executed later</description></item>
///   <item><description>Command batching - similar commands grouped for efficiency</description></item>
///   <item><description>Render sorting - commands reordered by state or depth</description></item>
///   <item><description>Multi-threaded recording - commands recorded on multiple threads</description></item>
/// </list>
/// </para>
/// <para>
/// Commands are value types (readonly record struct) for cache efficiency and
/// to avoid heap allocations in hot rendering paths.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Queue render commands
/// commandBuffer.Add(new ClearCommand(ClearMask.ColorBuffer | ClearMask.DepthBuffer));
/// commandBuffer.Add(new SetViewportCommand(0, 0, 1920, 1080));
/// commandBuffer.Add(new DrawMeshCommand(meshHandle, transform, material));
///
/// // Execute all commands
/// renderer.Execute(commandBuffer);
/// </code>
/// </example>
public interface IRenderCommand
{
    /// <summary>
    /// Gets the sort key for this command, used for batching and ordering.
    /// </summary>
    /// <remarks>
    /// Commands with the same sort key can be batched together.
    /// Lower values are executed first (front-to-back for opaque, back-to-front for transparent).
    /// </remarks>
    ulong SortKey { get; }
}

/// <summary>
/// Interface for a command buffer that queues render commands for deferred execution.
/// </summary>
public interface IRenderCommandBuffer : IDisposable
{
    /// <summary>
    /// Gets the number of commands in the buffer.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Adds a command to the buffer.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command to add.</param>
    void Add<T>(in T command) where T : struct, IRenderCommand;

    /// <summary>
    /// Clears all commands from the buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Sorts commands by their sort keys for optimal execution order.
    /// </summary>
    void Sort();
}
