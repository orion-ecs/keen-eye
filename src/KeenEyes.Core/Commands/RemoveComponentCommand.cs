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
    private readonly Type componentType;

    /// <summary>
    /// Creates a remove component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="componentType">The type of component to remove.</param>
    public RemoveComponentCommand(Entity entity, Type componentType)
    {
        targetEntity = entity;
        placeholderId = null;
        this.componentType = componentType;
    }

    /// <summary>
    /// Creates a remove component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="componentType">The type of component to remove.</param>
    public RemoveComponentCommand(int placeholderId, Type componentType)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
        this.componentType = componentType;
    }

    /// <inheritdoc />
    public void Execute(World world, Dictionary<int, Entity> entityMap)
    {
        var entity = ResolveEntity(entityMap);
        if (!entity.IsValid || !world.IsAlive(entity))
        {
            return;
        }

        // Use reflection to call the generic Remove<T> method
        var removeMethod = typeof(World).GetMethod(nameof(World.Remove))!
            .MakeGenericMethod(componentType);
        removeMethod.Invoke(world, [entity]);
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
