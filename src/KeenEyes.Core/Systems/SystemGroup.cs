namespace KeenEyes;

/// <summary>
/// A group of systems that execute together.
/// Useful for organizing systems by phase or feature.
/// </summary>
public class SystemGroup : ISystem
{
    private readonly List<ISystem> systems = [];
    private World? world;

    /// <summary>
    /// Name of this system group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Creates a new system group.
    /// </summary>
    public SystemGroup(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Adds a system to this group.
    /// </summary>
    public SystemGroup Add<T>() where T : ISystem, new()
    {
        var system = new T();
        if (world is not null)
        {
            system.Initialize(world);
        }
        systems.Add(system);
        return this;
    }

    /// <summary>
    /// Adds a system instance to this group.
    /// </summary>
    public SystemGroup Add(ISystem system)
    {
        if (world is not null)
        {
            system.Initialize(world);
        }
        systems.Add(system);
        return this;
    }

    /// <inheritdoc />
    public void Initialize(World world)
    {
        this.world = world;
        foreach (var system in systems)
        {
            system.Initialize(world);
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        foreach (var system in systems)
        {
            system.Update(deltaTime);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var system in systems)
        {
            system.Dispose();
        }
        systems.Clear();
    }
}
