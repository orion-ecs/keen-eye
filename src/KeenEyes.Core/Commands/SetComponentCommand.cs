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
    private readonly Type componentType;
    private readonly object componentValue;

    /// <summary>
    /// Creates a set component command for an existing entity.
    /// </summary>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="value">The new component value.</param>
    public SetComponentCommand(Entity entity, Type componentType, object value)
    {
        targetEntity = entity;
        placeholderId = null;
        this.componentType = componentType;
        componentValue = value;
    }

    /// <summary>
    /// Creates a set component command for a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="value">The new component value.</param>
    public SetComponentCommand(int placeholderId, Type componentType, object value)
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

        // Use reflection to call the generic Set<T> method
        var setMethod = typeof(World).GetMethod(nameof(World.Set))!
            .MakeGenericMethod(componentType);
        setMethod.Invoke(world, [entity, componentValue]);
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
