namespace KeenEyes;

/// <summary>
/// Interface for fluent query building.
/// </summary>
public interface IQueryBuilder : IEnumerable<Entity>
{
    /// <summary>
    /// Requires the entity to have this component (filter only, not accessed).
    /// </summary>
    /// <typeparam name="TWith">The component type that must be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder With<TWith>() where TWith : struct, IComponent;

    /// <summary>
    /// Excludes entities that have this component.
    /// </summary>
    /// <typeparam name="TWithout">The component type that must NOT be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder Without<TWithout>() where TWithout : struct, IComponent;

    /// <summary>
    /// Requires the entity to have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder WithTag(string tag);

    /// <summary>
    /// Excludes entities that have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must NOT be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder WithoutTag(string tag);

    /// <summary>
    /// Counts the number of entities matching this query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is optimized to avoid LINQ overhead and is safe to use in hot paths.
    /// When no string tag filters are applied, it sums archetype counts directly (O(archetypes)).
    /// When string tag filters are present, it must iterate entities (O(entities)).
    /// </para>
    /// </remarks>
    /// <returns>The number of entities matching the query.</returns>
    int Count();
}
