namespace KeenEyes;

/// <summary>
/// Defines a reusable entity template (prefab) that can be instantiated multiple times.
/// </summary>
/// <remarks>
/// <para>
/// Prefabs are templates for creating entities with predefined components. They support
/// inheritance through the <see cref="BasePrefab"/> property, allowing derived prefabs
/// to extend or override components from a base prefab.
/// </para>
/// <para>
/// Use the fluent builder pattern to define prefab components:
/// </para>
/// <example>
/// <code>
/// var enemyPrefab = new EntityPrefab()
///     .With(new Position { X = 0, Y = 0 })
///     .With(new Health { Current = 100, Max = 100 })
///     .WithTag&lt;EnemyTag&gt;();
///
/// world.RegisterPrefab("Enemy", enemyPrefab);
/// var enemy = world.SpawnFromPrefab("Enemy").Build();
/// </code>
/// </example>
/// </remarks>
public sealed class EntityPrefab
{
    private readonly List<ComponentDefinition> components = [];
    private readonly HashSet<Type> tagTypes = [];

    /// <summary>
    /// Gets the base prefab name for inheritance. When set, this prefab will inherit
    /// all components and tags from the base prefab, which can be overridden.
    /// </summary>
    /// <value>
    /// The name of the base prefab, or <c>null</c> if this prefab has no base.
    /// </value>
    public string? BasePrefab { get; private set; }

    /// <summary>
    /// Gets the component definitions in this prefab.
    /// </summary>
    /// <remarks>
    /// Components are stored in the order they were added. When inheriting from a base prefab,
    /// components with the same type will override the base prefab's component values.
    /// </remarks>
    internal IReadOnlyList<ComponentDefinition> Components => components;

    /// <summary>
    /// Gets the tag component types in this prefab.
    /// </summary>
    internal IReadOnlyCollection<Type> TagTypes => tagTypes;

    /// <summary>
    /// Creates a new empty entity prefab.
    /// </summary>
    public EntityPrefab()
    {
    }

    /// <summary>
    /// Adds a component to this prefab definition.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="component">The component value.</param>
    /// <returns>This prefab for fluent chaining.</returns>
    /// <remarks>
    /// If a component of the same type has already been added, it will be replaced.
    /// This allows derived prefabs to override base prefab components.
    /// </remarks>
    public EntityPrefab With<T>(T component) where T : struct, IComponent
    {
        var type = typeof(T);

        // Remove existing component of same type (allows overriding)
        components.RemoveAll(c => c.Type == type);
        components.Add(new ComponentDefinition(type, component, false));

        return this;
    }

    /// <summary>
    /// Adds a tag component to this prefab definition.
    /// </summary>
    /// <typeparam name="T">The tag component type to add.</typeparam>
    /// <returns>This prefab for fluent chaining.</returns>
    /// <remarks>
    /// Tag components are zero-size markers used for filtering queries.
    /// They carry no data, only presence.
    /// </remarks>
    public EntityPrefab WithTag<T>() where T : struct, ITagComponent
    {
        var type = typeof(T);
        tagTypes.Add(type);

        // Also add to components list with default value
        if (!components.Exists(c => c.Type == type))
        {
            components.Add(new ComponentDefinition(type, default(T)!, true));
        }

        return this;
    }

    /// <summary>
    /// Sets the base prefab for inheritance.
    /// </summary>
    /// <param name="basePrefabName">The name of the base prefab to inherit from.</param>
    /// <returns>This prefab for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// When a prefab inherits from a base prefab, it receives all components and tags
    /// from the base. Components added to this prefab will override matching components
    /// from the base prefab (matched by type).
    /// </para>
    /// <para>
    /// Inheritance is resolved at spawn time, so the base prefab must be registered
    /// before spawning from this prefab.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="basePrefabName"/> is null.</exception>
    public EntityPrefab Extends(string basePrefabName)
    {
        ArgumentNullException.ThrowIfNull(basePrefabName);
        BasePrefab = basePrefabName;
        return this;
    }
}

/// <summary>
/// Represents a component definition within a prefab.
/// </summary>
/// <param name="Type">The CLR type of the component.</param>
/// <param name="Data">The component data (boxed struct).</param>
/// <param name="IsTag">Whether this is a tag component.</param>
internal readonly record struct ComponentDefinition(Type Type, object Data, bool IsTag);
