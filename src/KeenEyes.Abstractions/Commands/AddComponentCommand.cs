namespace KeenEyes;

/// <summary>
/// Command that adds a component to an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class AddComponentCommand : ICommand
{
    private readonly Entity targetEntity;
    private readonly int? placeholderId;
    private readonly Action<IWorld, Entity> addAction;

    /// <summary>
    /// Creates an add component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="addAction">Delegate that adds the component to the world.</param>
    public AddComponentCommand(Entity entity, Action<IWorld, Entity> addAction)
    {
        targetEntity = entity;
        placeholderId = null;
        this.addAction = addAction;
    }

    /// <summary>
    /// Creates an add component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="addAction">Delegate that adds the component to the world.</param>
    public AddComponentCommand(int placeholderId, Action<IWorld, Entity> addAction)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
        this.addAction = addAction;
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
        addAction(world, entity);
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
