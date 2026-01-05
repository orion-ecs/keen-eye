using System.Collections.Concurrent;
using DotRecast.Detour;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Thread-safe pool of navigation mesh queries.
/// </summary>
/// <remarks>
/// <para>
/// DtNavMeshQuery is not thread-safe, so this pool provides a way to safely
/// use queries from multiple threads. Each thread borrows a query from the pool,
/// uses it, and then returns it.
/// </para>
/// <para>
/// The pool automatically grows as needed but maintains a minimum number of
/// queries for efficiency.
/// </para>
/// </remarks>
internal sealed class NavMeshQueryPool : IDisposable
{
    private readonly DtNavMesh navMesh;
    private readonly ConcurrentBag<DtNavMeshQuery> pool;
    private bool disposed;

    /// <summary>
    /// Creates a new query pool for the specified navigation mesh.
    /// </summary>
    /// <param name="navMesh">The navigation mesh to query.</param>
    /// <param name="initialSize">Initial number of queries to create.</param>
    public NavMeshQueryPool(DtNavMesh navMesh, int initialSize = 4)
    {
        ArgumentNullException.ThrowIfNull(navMesh);

        this.navMesh = navMesh;
        pool = [];

        // Pre-populate the pool
        for (int i = 0; i < initialSize; i++)
        {
            pool.Add(new DtNavMeshQuery(navMesh));
        }
    }

    /// <summary>
    /// Borrows a query from the pool.
    /// </summary>
    /// <returns>A pooled query wrapper that returns the query when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the pool is disposed.</exception>
    public PooledQuery Borrow()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!pool.TryTake(out var query))
        {
            // Pool is empty, create a new query
            query = new DtNavMeshQuery(navMesh);
        }

        return new PooledQuery(this, query);
    }

    private void Return(DtNavMeshQuery query)
    {
        if (!disposed)
        {
            pool.Add(query);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Clear the pool (DtNavMeshQuery is not IDisposable in DotRecast 2024.4.1)
        while (pool.TryTake(out _))
        {
            // Just drain the pool
        }
    }

    /// <summary>
    /// A borrowed query that returns itself to the pool when disposed.
    /// </summary>
    public readonly struct PooledQuery : IDisposable
    {
        private readonly NavMeshQueryPool pool;

        /// <summary>
        /// Gets the borrowed query.
        /// </summary>
        public DtNavMeshQuery Query { get; }

        internal PooledQuery(NavMeshQueryPool pool, DtNavMeshQuery query)
        {
            this.pool = pool;
            Query = query;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            pool.Return(Query);
        }
    }
}
