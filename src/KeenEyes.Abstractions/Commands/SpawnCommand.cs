namespace KeenEyes;

/// <summary>
/// Command that spawns a new entity with the specified components.
/// </summary>
/// <remarks>
/// The placeholder ID is a negative value used to track entities before they are created.
/// After execution, the mapping from placeholder to real entity is stored in the entity map,
/// allowing subsequent commands to reference the newly created entity.
/// </remarks>
/// <param name="entityCommands">The entity commands builder containing component configuration.</param>
internal sealed class SpawnCommand(EntityCommands entityCommands) : ICommand
{
    private readonly EntityCommands entityCommands = entityCommands;

    /// <inheritdoc />
    public void Execute(IWorld world, Dictionary<int, Entity> entityMap)
    {
        // Build entity using IWorld.Spawn() API
        var builder = world.Spawn(entityCommands.Name);

        // Apply all component additions via stored delegates (no reflection)
        foreach (var adder in entityCommands.ComponentAdders)
        {
            builder = adder(builder);
        }

        var entity = builder.Build();
        entityMap[entityCommands.PlaceholderId] = entity;
    }
}
