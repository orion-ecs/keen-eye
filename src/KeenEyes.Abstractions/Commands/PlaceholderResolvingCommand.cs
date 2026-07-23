namespace KeenEyes;

/// <summary>
/// Base class for commands that target an entity which may be a placeholder
/// created earlier in the same command buffer.
/// </summary>
/// <remarks>
/// Stores either a real entity handle or a placeholder ID (negative value) and
/// resolves the actual target through the entity map at execution time.
/// </remarks>
internal abstract class PlaceholderResolvingCommand : ICommand
{
    private readonly Entity targetEntity;
    private readonly int? placeholderId;

    /// <summary>
    /// Initializes the command to target an existing entity.
    /// </summary>
    /// <param name="entity">The entity the command operates on.</param>
    protected PlaceholderResolvingCommand(Entity entity)
    {
        targetEntity = entity;
        placeholderId = null;
    }

    /// <summary>
    /// Initializes the command to target a placeholder entity.
    /// </summary>
    /// <param name="placeholderId">The placeholder ID of the entity the command operates on.</param>
    protected PlaceholderResolvingCommand(int placeholderId)
    {
        targetEntity = Entity.Null;
        this.placeholderId = placeholderId;
    }

    /// <inheritdoc />
    public abstract void Execute(IWorld world, Dictionary<int, Entity> entityMap);

    /// <summary>
    /// Resolves the target entity, mapping a placeholder ID to the real entity if needed.
    /// </summary>
    /// <param name="entityMap">The placeholder-to-real entity map.</param>
    /// <returns>
    /// The resolved entity, or <see cref="Entity.Null"/> if the placeholder has no mapping.
    /// </returns>
    protected Entity ResolveEntity(Dictionary<int, Entity> entityMap)
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
