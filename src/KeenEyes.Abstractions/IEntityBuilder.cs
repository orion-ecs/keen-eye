namespace KeenEyes;

/// <summary>
/// Interface for fluent entity builders.
/// Implemented by EntityBuilder, PrefabBuilder, etc.
/// </summary>
public interface IEntityBuilder
{
    /// <summary>
    /// Adds a component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>This builder for chaining.</returns>
    IEntityBuilder With<T>(T component) where T : struct, IComponent;

    /// <summary>
    /// Adds a tag component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The tag component type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    IEntityBuilder WithTag<T>() where T : struct, ITagComponent;

    /// <summary>
    /// Builds the entity and adds it to the world.
    /// </summary>
    /// <returns>The created entity.</returns>
    Entity Build();
}

/// <summary>
/// Strongly-typed entity builder interface for better fluent API support.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type.</typeparam>
public interface IEntityBuilder<TSelf> : IEntityBuilder where TSelf : IEntityBuilder<TSelf>
{
    /// <summary>
    /// Adds a component to the entity being built.
    /// </summary>
    new TSelf With<T>(T component) where T : struct, IComponent;

    /// <summary>
    /// Adds a tag component to the entity being built.
    /// </summary>
    new TSelf WithTag<T>() where T : struct, ITagComponent;
}
