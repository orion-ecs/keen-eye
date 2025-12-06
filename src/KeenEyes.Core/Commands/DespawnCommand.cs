namespace KeenEyes;

/// <summary>
/// Command that destroys an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class DespawnCommand : ICommand
{
    private readonly Entity targetEntity;
    private readonly int? placeholderId;

    /// <summary>
    /// Creates a despawn command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to despawn.</param>
    public DespawnCommand(Entity entity)
    {
        targetEntity = entity;
        placeholderId = null;
    }

    /// <summary>
    /// Creates a despawn command for a placeholder entity (spawned in the same buffer).
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity to despawn.</param>
    public DespawnCommand(int placeholderId)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
    }

    /// <inheritdoc />
    public void Execute(World world, Dictionary<int, Entity> entityMap)
    {
        var entity = ResolveEntity(entityMap);
        if (entity.IsValid)
        {
            world.Despawn(entity);
        }
    }

    private Entity ResolveEntity(Dictionary<int, Entity> entityMap)
    {
        if (placeholderId.HasValue)
        {
            return entityMap.TryGetValue(placeholderId.Value, out var resolved)
                ? resolved
                : Entity.Null;
        }
        return targetEntity;
    }
}
