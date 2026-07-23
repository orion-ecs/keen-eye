namespace KeenEyes;

/// <summary>
/// Command that sets (replaces) a component value on an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class SetComponentCommand : PlaceholderResolvingCommand
{
    private readonly Action<IWorld, Entity> setAction;

    /// <summary>
    /// Creates a set component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="setAction">Delegate that sets the component on the world.</param>
    public SetComponentCommand(Entity entity, Action<IWorld, Entity> setAction)
        : base(entity)
    {
        this.setAction = setAction;
    }

    /// <summary>
    /// Creates a set component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="setAction">Delegate that sets the component on the world.</param>
    public SetComponentCommand(int placeholderId, Action<IWorld, Entity> setAction)
        : base(placeholderId)
    {
        this.setAction = setAction;
    }

    /// <inheritdoc />
    public override void Execute(IWorld world, Dictionary<int, Entity> entityMap)
    {
        var entity = ResolveEntity(entityMap);
        if (!entity.IsValid || !world.IsAlive(entity))
        {
            return;
        }

        // Invoke the stored delegate (no reflection)
        setAction(world, entity);
    }
}
