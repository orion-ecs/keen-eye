namespace KeenEyes;

/// <summary>
/// Command that destroys an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class DespawnCommand : PlaceholderResolvingCommand
{
    /// <summary>
    /// Creates a despawn command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to despawn.</param>
    public DespawnCommand(Entity entity)
        : base(entity)
    {
    }

    /// <summary>
    /// Creates a despawn command for a placeholder entity (spawned in the same buffer).
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity to despawn.</param>
    public DespawnCommand(int placeholderId)
        : base(placeholderId)
    {
    }

    /// <inheritdoc />
    public override void Execute(IWorld world, Dictionary<int, Entity> entityMap)
    {
        var entity = ResolveEntity(entityMap);
        if (entity.IsValid)
        {
            world.Despawn(entity);
        }
    }
}
