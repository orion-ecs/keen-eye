namespace KeenEyes;

/// <summary>
/// A group of systems that execute together.
/// Useful for organizing systems by phase or feature.
/// </summary>
public class SystemGroup : ISystem
{
    private readonly List<ISystem> systems = [];
    private World? world;
    private bool enabled = true;

    /// <summary>
    /// Name of this system group.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

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

    /// <summary>
    /// Gets a system of the specified type from this group.
    /// </summary>
    /// <typeparam name="T">The type of system to retrieve.</typeparam>
    /// <returns>The system instance, or null if not found.</returns>
    public T? GetSystem<T>() where T : class, ISystem
    {
        foreach (var system in systems)
        {
            if (system is T typedSystem)
            {
                return typedSystem;
            }
            if (system is SystemGroup group)
            {
                var found = group.GetSystem<T>();
                if (found is not null)
                {
                    return found;
                }
            }
        }
        return null;
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
            if (!system.Enabled)
            {
                continue;
            }

            if (system is SystemBase systemBase)
            {
                systemBase.InvokeBeforeUpdate(deltaTime);
                systemBase.Update(deltaTime);
                systemBase.InvokeAfterUpdate(deltaTime);
            }
            else
            {
                system.Update(deltaTime);
            }
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
