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
    private readonly Type componentType;
    private readonly object componentValue;

    /// <summary>
    /// Creates an add component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="value">The component value.</param>
    public AddComponentCommand(Entity entity, Type componentType, object value)
    {
        targetEntity = entity;
        placeholderId = null;
        this.componentType = componentType;
        componentValue = value;
    }

    /// <summary>
    /// Creates an add component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="value">The component value.</param>
    public AddComponentCommand(int placeholderId, Type componentType, object value)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
        this.componentType = componentType;
        componentValue = value;
    }

    /// <inheritdoc />
    public void Execute(World world, Dictionary<int, Entity> entityMap)
    {
        var entity = ResolveEntity(entityMap);
        if (!entity.IsValid || !world.IsAlive(entity))
        {
            return;
        }

        // Use reflection to call the generic Add<T> method
        // This maintains type safety while allowing deferred execution
        var addMethod = typeof(World).GetMethod(nameof(World.Add))!
            .MakeGenericMethod(componentType);
        addMethod.Invoke(world, [entity, componentValue]);
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
