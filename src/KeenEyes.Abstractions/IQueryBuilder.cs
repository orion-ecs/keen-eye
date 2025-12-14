namespace KeenEyes;

/// <summary>
/// Interface for fluent query building with a single component type.
/// </summary>
/// <typeparam name="T1">The first component type.</typeparam>
public interface IQueryBuilder<T1> : IEnumerable<Entity>
    where T1 : struct, IComponent
{
    /// <summary>
    /// Requires the entity to have this component (filter only, not accessed).
    /// </summary>
    /// <typeparam name="TWith">The component type that must be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1> With<TWith>() where TWith : struct, IComponent;

    /// <summary>
    /// Excludes entities that have this component.
    /// </summary>
    /// <typeparam name="TWithout">The component type that must NOT be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1> Without<TWithout>() where TWithout : struct, IComponent;

    /// <summary>
    /// Requires the entity to have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1> WithTag(string tag);

    /// <summary>
    /// Excludes entities that have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must NOT be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1> WithoutTag(string tag);

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

/// <summary>
/// Interface for fluent query building with two component types.
/// </summary>
/// <typeparam name="T1">The first component type.</typeparam>
/// <typeparam name="T2">The second component type.</typeparam>
public interface IQueryBuilder<T1, T2> : IEnumerable<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    /// <summary>
    /// Requires the entity to have this component (filter only, not accessed).
    /// </summary>
    /// <typeparam name="TWith">The component type that must be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1, T2> With<TWith>() where TWith : struct, IComponent;

    /// <summary>
    /// Excludes entities that have this component.
    /// </summary>
    /// <typeparam name="TWithout">The component type that must NOT be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1, T2> Without<TWithout>() where TWithout : struct, IComponent;

    /// <summary>
    /// Requires the entity to have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1, T2> WithTag(string tag);

    /// <summary>
    /// Excludes entities that have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must NOT be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1, T2> WithoutTag(string tag);

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

/// <summary>
/// Interface for fluent query building with three component types.
/// </summary>
/// <typeparam name="T1">The first component type.</typeparam>
/// <typeparam name="T2">The second component type.</typeparam>
/// <typeparam name="T3">The third component type.</typeparam>
public interface IQueryBuilder<T1, T2, T3> : IEnumerable<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    /// <summary>
    /// Requires the entity to have this component (filter only, not accessed).
    /// </summary>
    /// <typeparam name="TWith">The component type that must be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1, T2, T3> With<TWith>() where TWith : struct, IComponent;

    /// <summary>
    /// Excludes entities that have this component.
    /// </summary>
    /// <typeparam name="TWithout">The component type that must NOT be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1, T2, T3> Without<TWithout>() where TWithout : struct, IComponent;

    /// <summary>
    /// Requires the entity to have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1, T2, T3> WithTag(string tag);

    /// <summary>
    /// Excludes entities that have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must NOT be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1, T2, T3> WithoutTag(string tag);

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

/// <summary>
/// Interface for fluent query building with four component types.
/// </summary>
/// <typeparam name="T1">The first component type.</typeparam>
/// <typeparam name="T2">The second component type.</typeparam>
/// <typeparam name="T3">The third component type.</typeparam>
/// <typeparam name="T4">The fourth component type.</typeparam>
public interface IQueryBuilder<T1, T2, T3, T4> : IEnumerable<Entity>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    /// <summary>
    /// Requires the entity to have this component (filter only, not accessed).
    /// </summary>
    /// <typeparam name="TWith">The component type that must be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1, T2, T3, T4> With<TWith>() where TWith : struct, IComponent;

    /// <summary>
    /// Excludes entities that have this component.
    /// </summary>
    /// <typeparam name="TWithout">The component type that must NOT be present.</typeparam>
    /// <returns>A query builder with the additional filter.</returns>
    IQueryBuilder<T1, T2, T3, T4> Without<TWithout>() where TWithout : struct, IComponent;

    /// <summary>
    /// Requires the entity to have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1, T2, T3, T4> WithTag(string tag);

    /// <summary>
    /// Excludes entities that have this string tag.
    /// </summary>
    /// <param name="tag">The string tag that must NOT be present.</param>
    /// <returns>A query builder with the additional filter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    IQueryBuilder<T1, T2, T3, T4> WithoutTag(string tag);

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
