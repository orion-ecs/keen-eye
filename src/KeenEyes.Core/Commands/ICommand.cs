namespace KeenEyes;

/// <summary>
/// Represents a deferred operation to be executed on a world.
/// Commands are queued in a <see cref="CommandBuffer"/> and executed atomically via <see cref="CommandBuffer.Flush"/>.
/// </summary>
/// <remarks>
/// <para>
/// The command pattern enables safe modification of entities during system iteration.
/// Instead of modifying the world directly (which can cause iterator invalidation),
/// commands are queued and executed after iteration completes.
/// </para>
/// <para>
/// Commands are executed in the order they were queued, with spawn commands
/// creating placeholder-to-real entity mappings before subsequent commands.
/// </para>
/// </remarks>
internal interface ICommand
{
    /// <summary>
    /// Executes this command on the specified world.
    /// </summary>
    /// <param name="world">The world to execute the command on.</param>
    /// <param name="entityMap">
    /// A mapping from placeholder entity IDs (negative values) to real entities.
    /// Commands that create entities should add their mapping here.
    /// Commands that reference entities should resolve placeholders through this map.
    /// </param>
    void Execute(World world, Dictionary<int, Entity> entityMap);
}
