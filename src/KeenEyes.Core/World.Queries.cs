namespace KeenEyes;

public sealed partial class World
{
    #region Queries

    /// <summary>
    /// Creates a query for entities with the specified component.
    /// </summary>
    public QueryBuilder Query<T1>()
        where T1 : struct, IComponent
    {
        return new QueryBuilder(this).WithWrite<T1>();
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
    public QueryBuilder Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        return new QueryBuilder(this).WithWrite<T1>().WithWrite<T2>();
    }

    /// <summary>
    /// Creates a query for entities with the specified components.
    /// </summary>
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
