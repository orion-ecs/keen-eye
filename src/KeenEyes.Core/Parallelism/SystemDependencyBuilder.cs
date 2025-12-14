namespace KeenEyes;

/// <summary>
/// Builder for constructing component dependencies from system declarations.
/// </summary>
internal sealed class SystemDependencyBuilder : ISystemDependencyBuilder
{
    private readonly HashSet<Type> reads = [];
    private readonly HashSet<Type> writes = [];

    /// <inheritdoc />
    public ISystemDependencyBuilder Reads<T>() where T : struct, IComponent
    {
        reads.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public ISystemDependencyBuilder Writes<T>() where T : struct, IComponent
    {
        writes.Add(typeof(T));
        return this;
    }

    /// <inheritdoc />
    public ISystemDependencyBuilder ReadWrites<T>() where T : struct, IComponent
    {
        var type = typeof(T);
        reads.Add(type);
        writes.Add(type);
        return this;
    }

    /// <summary>
    /// Registers a query that this system uses, extracting dependencies from it.
    /// </summary>
    /// <param name="description">The query description.</param>
    /// <returns>This builder for chaining.</returns>
    public SystemDependencyBuilder UsesQuery(QueryDescription description)
    {
        foreach (var type in description.Read)
        {
            reads.Add(type);
        }

        foreach (var type in description.Write)
        {
            writes.Add(type);
        }

        return this;
    }

    /// <summary>
    /// Builds the component dependencies from the accumulated declarations.
    /// </summary>
    /// <returns>The built dependencies.</returns>
    public ComponentDependencies Build()
    {
        return new ComponentDependencies(reads, writes);
    }

    /// <summary>
    /// Resets the builder for reuse.
    /// </summary>
    public void Reset()
    {
        reads.Clear();
        writes.Clear();
    }
}
