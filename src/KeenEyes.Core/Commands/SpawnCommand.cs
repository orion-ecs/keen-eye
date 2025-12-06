namespace KeenEyes;

/// <summary>
/// Command that spawns a new entity with the specified components.
/// </summary>
/// <remarks>
/// The placeholder ID is a negative value used to track entities before they are created.
/// After execution, the mapping from placeholder to real entity is stored in the entity map,
/// allowing subsequent commands to reference the newly created entity.
/// </remarks>
internal sealed class SpawnCommand : ICommand
{
    /// <summary>
    /// The placeholder entity ID (negative value) used to reference this entity before creation.
    /// </summary>
    public int PlaceholderId { get; }

    /// <summary>
    /// The optional name for the spawned entity.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The components to add to the spawned entity (type, value, isTag).
    /// </summary>
    public List<(Type Type, object Data, bool IsTag)> Components { get; }

    /// <summary>
    /// Creates a new spawn command.
    /// </summary>
    /// <param name="placeholderId">A negative placeholder ID for this entity.</param>
    /// <param name="name">The optional name for the entity.</param>
    public SpawnCommand(int placeholderId, string? name = null)
    {
        PlaceholderId = placeholderId;
        Name = name;
        Components = [];
    }

    /// <inheritdoc />
    public void Execute(World world, Dictionary<int, Entity> entityMap)
    {
        // Register components with the world's registry and build the component list
        var worldComponents = new List<(ComponentInfo Info, object Data)>();
        foreach (var (type, data, isTag) in Components)
        {
            // Use reflection to call the generic GetOrRegister<T> method
            var method = typeof(ComponentRegistry)
                .GetMethods()
                .First(m => m.Name == nameof(ComponentRegistry.GetOrRegister) && m.IsGenericMethod)
                .MakeGenericMethod(type);
            var info = (ComponentInfo)method.Invoke(world.Components, [isTag])!;
            worldComponents.Add((info, data));
        }

        var entity = world.CreateEntity(worldComponents, Name);
        entityMap[PlaceholderId] = entity;
    }
}
