namespace KeenEyes;

/// <summary>
/// Tracks component dependencies for all registered systems.
/// </summary>
/// <remarks>
/// <para>
/// The SystemDependencyTracker collects and manages component read/write
/// dependencies for systems, enabling the parallel scheduler to determine
/// which systems can execute concurrently.
/// </para>
/// <para>
/// Dependencies can come from three sources:
/// <list type="number">
///   <item>Explicit declaration via <see cref="ISystemDependencyProvider"/></item>
///   <item>Manual registration via <see cref="RegisterDependencies"/></item>
///   <item>Assumed full access if not declared (conservative)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class SystemDependencyTracker
{
    private readonly Dictionary<Type, ComponentDependencies> systemDependencies = [];
    private readonly SystemDependencyBuilder builder = new();

    /// <summary>
    /// Gets the number of systems with registered dependencies.
    /// </summary>
    public int Count => systemDependencies.Count;

    /// <summary>
    /// Registers a system and extracts its dependencies.
    /// </summary>
    /// <param name="system">The system to register.</param>
    /// <remarks>
    /// If the system implements <see cref="ISystemDependencyProvider"/>,
    /// dependencies are extracted via <see cref="ISystemDependencyProvider.GetDependencies"/>.
    /// Otherwise, empty dependencies are assumed (system is assumed to have no conflicts).
    /// </remarks>
    public void RegisterSystem(ISystem system)
    {
        var systemType = system.GetType();

        if (systemDependencies.ContainsKey(systemType))
        {
            return; // Already registered
        }

        if (system is ISystemDependencyProvider provider)
        {
            builder.Reset();
            provider.GetDependencies(builder);
            systemDependencies[systemType] = builder.Build();
        }
        else
        {
            // Systems without explicit dependencies are assumed to have none
            // This is optimistic - they can run in parallel with anything
            systemDependencies[systemType] = ComponentDependencies.Empty;
        }
    }

    /// <summary>
    /// Registers a system with explicit dependencies.
    /// </summary>
    /// <param name="systemType">The system type.</param>
    /// <param name="dependencies">The component dependencies.</param>
    public void RegisterDependencies(Type systemType, ComponentDependencies dependencies)
    {
        systemDependencies[systemType] = dependencies;
    }

    /// <summary>
    /// Registers a system with dependencies derived from query descriptions.
    /// </summary>
    /// <typeparam name="TSystem">The system type.</typeparam>
    /// <param name="queries">The queries used by the system.</param>
    public void RegisterDependencies<TSystem>(params QueryDescription[] queries)
        where TSystem : ISystem
    {
        var dependencies = ComponentDependencies.FromQueries(queries);
        systemDependencies[typeof(TSystem)] = dependencies;
    }

    /// <summary>
    /// Gets the dependencies for a system type.
    /// </summary>
    /// <param name="systemType">The system type.</param>
    /// <returns>The system's dependencies, or empty if not registered.</returns>
    public ComponentDependencies GetDependencies(Type systemType)
    {
        return systemDependencies.TryGetValue(systemType, out var deps)
            ? deps
            : ComponentDependencies.Empty;
    }

    /// <summary>
    /// Tries to get the dependencies for a system type.
    /// </summary>
    /// <param name="systemType">The system type.</param>
    /// <param name="dependencies">The dependencies if found.</param>
    /// <returns>True if the system was registered and has dependencies.</returns>
    public bool TryGetDependencies(Type systemType, out ComponentDependencies dependencies)
    {
        return systemDependencies.TryGetValue(systemType, out dependencies!);
    }

    /// <summary>
    /// Gets the dependencies for a system type.
    /// </summary>
    /// <typeparam name="TSystem">The system type.</typeparam>
    /// <returns>The system's dependencies, or empty if not registered.</returns>
    public ComponentDependencies GetDependencies<TSystem>() where TSystem : ISystem
    {
        return GetDependencies(typeof(TSystem));
    }

    /// <summary>
    /// Checks if two system types have conflicting dependencies.
    /// </summary>
    /// <param name="systemType1">First system type.</param>
    /// <param name="systemType2">Second system type.</param>
    /// <returns>True if the systems have conflicting dependencies.</returns>
    public bool HasConflict(Type systemType1, Type systemType2)
    {
        var deps1 = GetDependencies(systemType1);
        var deps2 = GetDependencies(systemType2);
        return deps1.ConflictsWith(deps2);
    }

    /// <summary>
    /// Gets the component types that cause conflicts between two systems.
    /// </summary>
    /// <param name="systemType1">First system type.</param>
    /// <param name="systemType2">Second system type.</param>
    /// <returns>The conflicting component types.</returns>
    public IReadOnlyCollection<Type> GetConflicts(Type systemType1, Type systemType2)
    {
        var deps1 = GetDependencies(systemType1);
        var deps2 = GetDependencies(systemType2);
        return deps1.GetConflictingComponents(deps2);
    }

    /// <summary>
    /// Checks if a system can run in parallel with all specified systems.
    /// </summary>
    /// <param name="systemType">The system to check.</param>
    /// <param name="otherSystems">The other systems that would run concurrently.</param>
    /// <returns>True if the system can run in parallel with all others.</returns>
    public bool CanRunInParallelWith(Type systemType, IEnumerable<Type> otherSystems)
    {
        var deps = GetDependencies(systemType);

        foreach (var otherType in otherSystems)
        {
            var otherDeps = GetDependencies(otherType);
            if (deps.ConflictsWith(otherDeps))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets all registered system types.
    /// </summary>
    /// <returns>An enumerable of all registered system types.</returns>
    public IEnumerable<Type> GetRegisteredSystems()
    {
        return systemDependencies.Keys;
    }

    /// <summary>
    /// Removes a system's dependency registration.
    /// </summary>
    /// <param name="systemType">The system type to unregister.</param>
    /// <returns>True if the system was unregistered.</returns>
    public bool Unregister(Type systemType)
    {
        return systemDependencies.Remove(systemType);
    }

    /// <summary>
    /// Clears all registered dependencies.
    /// </summary>
    public void Clear()
    {
        systemDependencies.Clear();
    }
}
