using System.Collections.Concurrent;

namespace KeenEyes;

/// <summary>
/// Provides per-system isolated command buffers for parallel system execution.
/// </summary>
/// <remarks>
/// <para>
/// In parallel system execution, each system needs its own <see cref="CommandBuffer"/>
/// to avoid contention. The <see cref="CommandBufferPool"/> manages buffer allocation
/// and provides deterministic merging across multiple buffers.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> This class is thread-safe. Multiple threads can
/// safely rent and return buffers concurrently.
/// </para>
/// <para>
/// <strong>Placeholder Resolution:</strong> When merging buffers, placeholder IDs
/// are remapped to avoid conflicts between buffers. Cross-buffer entity references
/// are resolved during the merge operation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pool = new CommandBufferPool();
///
/// // In parallel systems
/// var buffer1 = pool.Rent(systemId: 0);
/// var buffer2 = pool.Rent(systemId: 1);
///
/// // Systems queue commands to their respective buffers
/// buffer1.Spawn().With(new Position { X = 0, Y = 0 });
/// buffer2.Spawn().With(new Position { X = 10, Y = 10 });
///
/// // After parallel execution, merge and flush all buffers
/// var entityMap = pool.FlushAll(world);
/// </code>
/// </example>
public sealed class CommandBufferPool
{
    private readonly ConcurrentDictionary<int, TrackedCommandBuffer> activeBuffers = [];
    private readonly ConcurrentBag<CommandBuffer> pooledBuffers = [];
    private int nextBufferId;

    /// <summary>
    /// Gets the number of command buffers currently rented.
    /// </summary>
    public int ActiveBufferCount => activeBuffers.Count;

    /// <summary>
    /// Gets the total number of commands across all active buffers.
    /// </summary>
    public int TotalCommandCount
    {
        get
        {
            var count = 0;
            foreach (var (_, tracked) in activeBuffers)
            {
                count += tracked.Buffer.Count;
            }

            return count;
        }
    }

    /// <summary>
    /// Rents a command buffer for a specific system.
    /// </summary>
    /// <param name="systemId">A unique identifier for the system (used for deterministic ordering).</param>
    /// <returns>A command buffer for the system to use.</returns>
    /// <remarks>
    /// The systemId is used to ensure deterministic merge ordering. Systems with
    /// lower IDs have their commands executed first.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a buffer is already rented for the specified system ID.
    /// </exception>
    public CommandBuffer Rent(int systemId)
    {
        if (!pooledBuffers.TryTake(out var buffer))
        {
            buffer = new CommandBuffer();
        }

        var bufferId = Interlocked.Increment(ref nextBufferId);
        var tracked = new TrackedCommandBuffer(buffer, bufferId, systemId);

        if (!activeBuffers.TryAdd(systemId, tracked))
        {
            // Return buffer to pool and throw
            pooledBuffers.Add(buffer);
            throw new InvalidOperationException(
                $"A command buffer is already rented for system ID {systemId}.");
        }

        return buffer;
    }

    /// <summary>
    /// Returns a rented command buffer to the pool.
    /// </summary>
    /// <param name="systemId">The system ID used when renting the buffer.</param>
    /// <remarks>
    /// The buffer is cleared and returned to the pool for reuse.
    /// Call this after flushing the buffer or if the buffer is no longer needed.
    /// </remarks>
    public void Return(int systemId)
    {
        if (activeBuffers.TryRemove(systemId, out var tracked))
        {
            tracked.Buffer.Clear();
            pooledBuffers.Add(tracked.Buffer);
        }
    }

    /// <summary>
    /// Flushes all active command buffers to the world in a deterministic order.
    /// </summary>
    /// <param name="world">The world to execute commands on.</param>
    /// <returns>
    /// A dictionary mapping placeholder IDs (prefixed with system ID) to created entities.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Buffers are flushed in system ID order to ensure deterministic execution.
    /// Placeholder IDs are prefixed with the buffer ID to ensure uniqueness across buffers.
    /// </para>
    /// <para>
    /// After flushing, all buffers are cleared and returned to the pool.
    /// </para>
    /// </remarks>
    public Dictionary<long, Entity> FlushAll(IWorld world)
    {
        // Get all buffers sorted by system ID for deterministic ordering
        var sortedBuffers = activeBuffers
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToList();

        var globalEntityMap = new Dictionary<long, Entity>();

        foreach (var tracked in sortedBuffers)
        {
            // Flush buffer and get local entity map
            var localEntityMap = tracked.Buffer.Flush(world);

            // Remap placeholder IDs to global scope using buffer ID as prefix
            foreach (var (placeholderId, entity) in localEntityMap)
            {
                var globalId = GetGlobalPlaceholderId(tracked.BufferId, placeholderId);
                globalEntityMap[globalId] = entity;
            }
        }

        // Return all buffers to pool
        foreach (var (systemId, _) in activeBuffers)
        {
            Return(systemId);
        }

        return globalEntityMap;
    }

    /// <summary>
    /// Flushes command buffers in batches for staged parallel execution.
    /// </summary>
    /// <param name="world">The world to execute commands on.</param>
    /// <param name="systemIdBatches">
    /// Batches of system IDs. Each batch is flushed in order, enabling
    /// cross-batch placeholder resolution.
    /// </param>
    /// <returns>
    /// A dictionary mapping global placeholder IDs to created entities.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method when parallel systems are executed in batches (sequential batches
    /// of parallel systems). Systems in later batches can reference entities created
    /// in earlier batches.
    /// </para>
    /// <para>
    /// Within each batch, systems are flushed in system ID order for determinism.
    /// </para>
    /// </remarks>
    public Dictionary<long, Entity> FlushBatches(IWorld world, IEnumerable<IEnumerable<int>> systemIdBatches)
    {
        var globalEntityMap = new Dictionary<long, Entity>();

        foreach (var batch in systemIdBatches)
        {
            // Sort systems within batch for deterministic ordering
            var sortedSystemIds = batch.OrderBy(id => id).ToList();

            foreach (var systemId in sortedSystemIds)
            {
                if (activeBuffers.TryGetValue(systemId, out var tracked))
                {
                    var localEntityMap = tracked.Buffer.Flush(world);

                    foreach (var (placeholderId, entity) in localEntityMap)
                    {
                        var globalId = GetGlobalPlaceholderId(tracked.BufferId, placeholderId);
                        globalEntityMap[globalId] = entity;
                    }

                    Return(systemId);
                }
            }
        }

        return globalEntityMap;
    }

    /// <summary>
    /// Clears all active buffers without executing commands.
    /// </summary>
    /// <remarks>
    /// All buffers are cleared and returned to the pool.
    /// </remarks>
    public void Clear()
    {
        foreach (var (_, tracked) in activeBuffers)
        {
            tracked.Buffer.Clear();
            pooledBuffers.Add(tracked.Buffer);
        }

        activeBuffers.Clear();
    }

    /// <summary>
    /// Gets the global placeholder ID for cross-buffer entity reference.
    /// </summary>
    /// <param name="bufferId">The buffer ID that created the placeholder.</param>
    /// <param name="localPlaceholderId">The local placeholder ID within that buffer.</param>
    /// <returns>A globally unique placeholder ID.</returns>
    /// <remarks>
    /// Use this method to create entity references that can be resolved after
    /// FlushAll or FlushBatches is called.
    /// </remarks>
    public static long GetGlobalPlaceholderId(int bufferId, int localPlaceholderId)
    {
        // Combine buffer ID and local placeholder ID into a unique long
        // Local placeholder IDs are negative, so we negate them for the lower bits
        return ((long)bufferId << 32) | (uint)(-localPlaceholderId);
    }

    /// <summary>
    /// Parses a global placeholder ID back to buffer ID and local placeholder ID.
    /// </summary>
    /// <param name="globalId">The global placeholder ID.</param>
    /// <returns>A tuple of (bufferId, localPlaceholderId).</returns>
    public static (int BufferId, int LocalPlaceholderId) ParseGlobalPlaceholderId(long globalId)
    {
        var bufferId = (int)(globalId >> 32);
        var localPlaceholderId = -(int)(globalId & 0xFFFFFFFF);
        return (bufferId, localPlaceholderId);
    }

    /// <summary>
    /// Gets the buffer ID for a rented buffer.
    /// </summary>
    /// <param name="systemId">The system ID used when renting.</param>
    /// <returns>The buffer ID, or -1 if no buffer is rented for this system.</returns>
    public int GetBufferId(int systemId)
    {
        return activeBuffers.TryGetValue(systemId, out var tracked)
            ? tracked.BufferId
            : -1;
    }

    private sealed record TrackedCommandBuffer(CommandBuffer Buffer, int BufferId, int SystemId);
}
