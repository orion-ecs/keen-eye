namespace KeenEyes;

public sealed partial class World
{
    #region Queries

    /// <summary>
    /// Creates a query for entities with the specified component.
    /// </summary>
    /// <typeparam name="T1">The required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    public QueryBuilder Query<T1>()
        where T1 : struct, IComponent
    {
        return new QueryBuilder(this).WithWrite<T1>();
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    public QueryBuilder Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        return new QueryBuilder(this).WithWrite<T1>().WithWrite<T2>();
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <typeparam name="T3">The third required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    public QueryBuilder Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        return new QueryBuilder(this).WithWrite<T1>().WithWrite<T2>().WithWrite<T3>();
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <typeparam name="T3">The third required component type.</typeparam>
    /// <typeparam name="T4">The fourth required component type.</typeparam>
    /// <returns>A query builder for filtering and enumerating entities.</returns>
    public QueryBuilder Query<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        return new QueryBuilder(this).WithWrite<T1>().WithWrite<T2>().WithWrite<T3>().WithWrite<T4>();
    }

    #endregion

    #region IWorld Query Implementations

    /// <inheritdoc />
    IQueryBuilder IWorld.Query<T1>() => Query<T1>();

    /// <inheritdoc />
    IQueryBuilder IWorld.Query<T1, T2>() => Query<T1, T2>();

    /// <inheritdoc />
    IQueryBuilder IWorld.Query<T1, T2, T3>() => Query<T1, T2, T3>();

    /// <inheritdoc />
    IQueryBuilder IWorld.Query<T1, T2, T3, T4>() => Query<T1, T2, T3, T4>();

    #endregion
}
