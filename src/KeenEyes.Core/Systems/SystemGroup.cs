namespace KeenEyes;

/// <summary>
/// A group of systems that execute together.
/// Useful for organizing systems by phase or feature.
/// </summary>
public class SystemGroup : ISystem
{
    private readonly List<SystemEntry> systems = [];
    private World? world;
    private bool enabled = true;
    private bool systemsSorted = true;

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
    /// Adds a system to this group with specified execution order.
    /// </summary>
    /// <typeparam name="T">The system type to add.</typeparam>
    /// <param name="order">The execution order within the group. Lower values execute first. Defaults to 0.</param>
    /// <returns>This group for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Systems within a group are sorted by order value. Systems with lower order values execute first.
    /// Systems with the same order maintain stable relative ordering.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var updateGroup = new SystemGroup("Update")
    ///     .Add&lt;MovementSystem&gt;(order: 0)
    ///     .Add&lt;CollisionSystem&gt;(order: 10)
    ///     .Add&lt;DamageSystem&gt;(order: 20);
    /// </code>
    /// </example>
    public SystemGroup Add<T>(int order = 0) where T : ISystem, new()
    {
        var system = new T();
        if (world is not null)
        {
            system.Initialize(world);
        }
        systems.Add(new SystemEntry(system, order));
        systemsSorted = false;
        return this;
    }

    /// <summary>
    /// Adds a system instance to this group with specified execution order.
    /// </summary>
    /// <param name="system">The system instance to add.</param>
    /// <param name="order">The execution order within the group. Lower values execute first. Defaults to 0.</param>
    /// <returns>This group for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured system instance or a system
    /// that requires constructor parameters.
    /// </para>
    /// </remarks>
    public SystemGroup Add(ISystem system, int order = 0)
    {
        if (world is not null)
        {
            system.Initialize(world);
        }
        systems.Add(new SystemEntry(system, order));
        systemsSorted = false;
        return this;
    }

    /// <summary>
    /// Gets a system of the specified type from this group.
    /// </summary>
    /// <typeparam name="T">The type of system to retrieve.</typeparam>
    /// <returns>The system instance, or null if not found.</returns>
    public T? GetSystem<T>() where T : class, ISystem
    {
        foreach (var entry in systems)
        {
            if (entry.System is T typedSystem)
            {
                return typedSystem;
            }
            if (entry.System is SystemGroup group)
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
        foreach (var entry in systems)
        {
            entry.System.Initialize(world);
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        EnsureSystemsSorted();

        foreach (var entry in systems)
        {
            var system = entry.System;

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
        foreach (var entry in systems)
        {
            entry.System.Dispose();
        }
        systems.Clear();
    }

    /// <summary>
    /// Ensures systems are sorted by order before iteration.
    /// </summary>
    private void EnsureSystemsSorted()
    {
        if (systemsSorted)
        {
            return;
        }

        systems.Sort((a, b) => a.Order.CompareTo(b.Order));
        systemsSorted = true;
    }

    /// <summary>
    /// Internal record for storing system with its execution order.
    /// </summary>
    private sealed record SystemEntry(ISystem System, int Order);
}
