namespace KeenEyes;

/// <summary>
/// Command that sets (replaces) a component value on an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class SetComponentCommand : ICommand
{
    private readonly Entity targetEntity;
    private readonly int? placeholderId;
    private readonly Action<IWorld, Entity> setAction;

    /// <summary>
    /// Creates a set component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="setAction">Delegate that sets the component on the world.</param>
    public SetComponentCommand(Entity entity, Action<IWorld, Entity> setAction)
    {
        targetEntity = entity;
        placeholderId = null;
        this.setAction = setAction;
    }

    /// <summary>
    /// Creates a set component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="setAction">Delegate that sets the component on the world.</param>
    public SetComponentCommand(int placeholderId, Action<IWorld, Entity> setAction)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
        this.setAction = setAction;
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
        setAction(world, entity);
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
