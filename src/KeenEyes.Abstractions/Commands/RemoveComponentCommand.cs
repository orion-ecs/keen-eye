namespace KeenEyes;

/// <summary>
/// Command that removes a component from an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class RemoveComponentCommand : ICommand
{
    private readonly Entity targetEntity;
    private readonly int? placeholderId;
    private readonly Action<IWorld, Entity> removeAction;

    /// <summary>
    /// Creates a remove component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="removeAction">Delegate that removes the component from the world.</param>
    public RemoveComponentCommand(Entity entity, Action<IWorld, Entity> removeAction)
    {
        targetEntity = entity;
        placeholderId = null;
        this.removeAction = removeAction;
    }

    /// <summary>
    /// Creates a remove component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="removeAction">Delegate that removes the component from the world.</param>
    public RemoveComponentCommand(int placeholderId, Action<IWorld, Entity> removeAction)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
        this.removeAction = removeAction;
    }

    /// <inheritdoc />
    public void Execute(IWorld world, Dictionary<int, Entity> entityMap)
    {
        var entity = ResolveEntity(entityMap);
        if (!entity.IsValid || !world.IsAlive(entity))
        {
            return;
        }

        // Invoke the stored delegate (no reflection)
        removeAction(world, entity);
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
