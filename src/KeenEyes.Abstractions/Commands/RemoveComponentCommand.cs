namespace KeenEyes;

/// <summary>
/// Command that removes a component from an existing entity.
/// </summary>
/// <remarks>
/// If the target is a placeholder entity (negative ID), it will be resolved
/// to the real entity through the entity map during execution.
/// </remarks>
internal sealed class RemoveComponentCommand : PlaceholderResolvingCommand
{
    private readonly Action<IWorld, Entity> removeAction;

    /// <summary>
    /// Creates a remove component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="removeAction">Delegate that removes the component from the world.</param>
    public RemoveComponentCommand(Entity entity, Action<IWorld, Entity> removeAction)
        : base(entity)
    {
        this.removeAction = removeAction;
    }

    /// <summary>
    /// Creates a remove component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="removeAction">Delegate that removes the component from the world.</param>
    public RemoveComponentCommand(int placeholderId, Action<IWorld, Entity> removeAction)
        : base(placeholderId)
    {
        this.removeAction = removeAction;
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
        removeAction(world, entity);
    }
}
