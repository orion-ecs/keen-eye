namespace KeenEyes.Shaders;

/// <summary>
/// Specifies how a component is accessed by a compute shader.
/// </summary>
public enum ComponentAccess
{
    /// <summary>
    /// Component is read but not modified.
    /// </summary>
    Read,

    /// <summary>
    /// Component is written (and possibly read).
    /// </summary>
    Write,

    /// <summary>
    /// Component is optional and may not be present.
    /// </summary>
    Optional
}

/// <summary>
/// Describes a component binding in a shader query.
/// </summary>
/// <param name="ComponentType">The type name of the component.</param>
/// <param name="Access">How the component is accessed.</param>
/// <param name="BindingIndex">The GPU buffer binding index.</param>
public readonly record struct ComponentBinding(
    string ComponentType,
    ComponentAccess Access,
    int BindingIndex);

/// <summary>
/// Describes the ECS query requirements for a compute shader.
/// </summary>
/// <remarks>
/// <para>
/// A QueryDescriptor defines which components a compute shader needs and how it accesses them.
/// This enables the runtime to set up the correct data bindings and validate that all
/// required components are available.
/// </para>
/// <para>
/// The descriptor is typically generated at compile time from KESL shader declarations.
/// </para>
/// </remarks>
public sealed class QueryDescriptor
{
    /// <summary>
    /// Gets the name of this query (typically the compute shader name).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the components that must be read by this query.
    /// </summary>
    public IReadOnlyList<ComponentBinding> ReadComponents { get; }

    /// <summary>
    /// Gets the components that will be written by this query.
    /// </summary>
    public IReadOnlyList<ComponentBinding> WriteComponents { get; }

    /// <summary>
    /// Gets the components that are optionally accessed by this query.
    /// </summary>
    public IReadOnlyList<ComponentBinding> OptionalComponents { get; }

    /// <summary>
    /// Gets the component types that must NOT be present (exclusion filter).
    /// </summary>
    public IReadOnlyList<string> WithoutComponents { get; }

    /// <summary>
    /// Gets all component bindings in binding index order.
    /// </summary>
    public IReadOnlyList<ComponentBinding> AllBindings { get; }

    /// <summary>
    /// Creates a new query descriptor.
    /// </summary>
    /// <param name="name">The query name.</param>
    /// <param name="readComponents">Components to read.</param>
    /// <param name="writeComponents">Components to write.</param>
    /// <param name="optionalComponents">Optional components.</param>
    /// <param name="withoutComponents">Exclusion filter components.</param>
    public QueryDescriptor(
        string name,
        IEnumerable<ComponentBinding>? readComponents = null,
        IEnumerable<ComponentBinding>? writeComponents = null,
        IEnumerable<ComponentBinding>? optionalComponents = null,
        IEnumerable<string>? withoutComponents = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ReadComponents = (readComponents?.ToList() ?? []).AsReadOnly();
        WriteComponents = (writeComponents?.ToList() ?? []).AsReadOnly();
        OptionalComponents = (optionalComponents?.ToList() ?? []).AsReadOnly();
        WithoutComponents = (withoutComponents?.ToList() ?? []).AsReadOnly();

        // Build all bindings sorted by binding index
        AllBindings = ReadComponents
            .Concat(WriteComponents)
            .Concat(OptionalComponents)
            .OrderBy(b => b.BindingIndex)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the binding for a specific component type.
    /// </summary>
    /// <param name="componentType">The component type name to look up.</param>
    /// <returns>The binding, or null if not found.</returns>
    public ComponentBinding? GetBinding(string componentType)
    {
        foreach (var binding in AllBindings)
        {
            if (binding.ComponentType == componentType)
            {
                return binding;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets whether a component type is required (read or write, not optional).
    /// </summary>
    /// <param name="componentType">The component type name.</param>
    /// <returns>True if the component is required.</returns>
    public bool IsRequired(string componentType)
    {
        return ReadComponents.Any(b => b.ComponentType == componentType) ||
               WriteComponents.Any(b => b.ComponentType == componentType);
    }

    /// <summary>
    /// Gets whether a component type is excluded.
    /// </summary>
    /// <param name="componentType">The component type name.</param>
    /// <returns>True if the component is excluded.</returns>
    public bool IsExcluded(string componentType)
    {
        return WithoutComponents.Contains(componentType);
    }

    /// <summary>
    /// Creates a query descriptor builder.
    /// </summary>
    /// <param name="name">The query name.</param>
    /// <returns>A new builder instance.</returns>
    public static QueryDescriptorBuilder Builder(string name) => new(name);
}

/// <summary>
/// Fluent builder for creating <see cref="QueryDescriptor"/> instances.
/// </summary>
/// <param name="name">The query name.</param>
public sealed class QueryDescriptorBuilder(string name)
{
    private readonly string name = name ?? throw new ArgumentNullException(nameof(name));
    private readonly List<ComponentBinding> readComponents = [];
    private readonly List<ComponentBinding> writeComponents = [];
    private readonly List<ComponentBinding> optionalComponents = [];
    private readonly List<string> withoutComponents = [];
    private int nextBinding;

    /// <summary>
    /// Adds a read-only component binding.
    /// </summary>
    /// <param name="componentType">The component type name.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryDescriptorBuilder Read(string componentType)
    {
        readComponents.Add(new ComponentBinding(componentType, ComponentAccess.Read, nextBinding++));
        return this;
    }

    /// <summary>
    /// Adds a writable component binding.
    /// </summary>
    /// <param name="componentType">The component type name.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryDescriptorBuilder Write(string componentType)
    {
        writeComponents.Add(new ComponentBinding(componentType, ComponentAccess.Write, nextBinding++));
        return this;
    }

    /// <summary>
    /// Adds an optional component binding.
    /// </summary>
    /// <param name="componentType">The component type name.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryDescriptorBuilder Optional(string componentType)
    {
        optionalComponents.Add(new ComponentBinding(componentType, ComponentAccess.Optional, nextBinding++));
        return this;
    }

    /// <summary>
    /// Adds an exclusion filter.
    /// </summary>
    /// <param name="componentType">The component type name to exclude.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryDescriptorBuilder Without(string componentType)
    {
        withoutComponents.Add(componentType);
        return this;
    }

    /// <summary>
    /// Builds the query descriptor.
    /// </summary>
    /// <returns>A new QueryDescriptor instance.</returns>
    public QueryDescriptor Build()
    {
        return new QueryDescriptor(
            name,
            readComponents,
            writeComponents,
            optionalComponents,
            withoutComponents);
    }
}
