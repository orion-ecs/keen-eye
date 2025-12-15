namespace KeenEyes;

/// <summary>
/// Provides parallel iteration extensions for query builders.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods enable parallel processing of entities matching a query.
/// Parallelization occurs at the archetype chunk level for optimal cache locality -
/// each chunk is processed by a single thread, preserving cache-friendly access patterns.
/// </para>
/// <para>
/// For small entity counts, the overhead of parallelization may outweigh benefits.
/// Use the minEntityCount parameter to control the threshold.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Process entities with Position and Velocity in parallel
/// world.Query&lt;Position, Velocity&gt;()
///     .ForEachParallel&lt;Position, Velocity&gt;((Entity e, ref Position pos, ref Velocity vel) =>
///     {
///         pos.X += vel.X * deltaTime;
///         pos.Y += vel.Y * deltaTime;
///     });
/// </code>
/// </example>
public static class ParallelQueryExtensions
{
    /// <summary>
    /// Default minimum entity count before enabling parallel processing.
    /// </summary>
    public const int DefaultMinEntityCount = 1000;

    #region Single Component

    /// <summary>
    /// Processes all matching entities in parallel.
    /// </summary>
    /// <typeparam name="T1">The component type.</typeparam>
    /// <param name="query">The query builder.</param>
    /// <param name="action">The action to execute for each entity.</param>
    /// <param name="minEntityCount">Minimum entity count to enable parallelization.</param>
    public static void ForEachParallel<T1>(
        this QueryBuilder query,
        EntityAction<T1> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            // Fall back to sequential processing
            foreach (var entity in query)
            {
                ref var c1 = ref query.World.Get<T1>(entity);
                action(entity, ref c1);
            }
            return;
        }

        // Collect all chunks across archetypes for parallel processing
        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetSpan<T1>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, ref span1[i]);
            }
        });
    }

    /// <summary>
    /// Processes all matching entities in parallel with readonly component access.
    /// </summary>
    /// <typeparam name="T1">The component type.</typeparam>
    /// <param name="query">The query builder.</param>
    /// <param name="action">The action to execute for each entity.</param>
    /// <param name="minEntityCount">Minimum entity count to enable parallelization.</param>
    public static void ForEachParallelReadOnly<T1>(
        this QueryBuilder query,
        EntityActionReadOnly<T1> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref readonly var c1 = ref query.World.Get<T1>(entity);
                action(entity, in c1);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetReadOnlySpan<T1>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, in span1[i]);
            }
        });
    }

    #endregion

    #region Two Components

    /// <summary>
    /// Processes all matching entities in parallel.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <param name="query">The query builder.</param>
    /// <param name="action">The action to execute for each entity.</param>
    /// <param name="minEntityCount">Minimum entity count to enable parallelization.</param>
    public static void ForEachParallel<T1, T2>(
        this QueryBuilder query,
        EntityAction<T1, T2> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref var c1 = ref query.World.Get<T1>(entity);
                ref var c2 = ref query.World.Get<T2>(entity);
                action(entity, ref c1, ref c2);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetSpan<T1>();
            var span2 = chunk.GetSpan<T2>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, ref span1[i], ref span2[i]);
            }
        });
    }

    /// <summary>
    /// Processes all matching entities in parallel with readonly component access.
    /// </summary>
    public static void ForEachParallelReadOnly<T1, T2>(
        this QueryBuilder query,
        EntityActionReadOnly<T1, T2> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref readonly var c1 = ref query.World.Get<T1>(entity);
                ref readonly var c2 = ref query.World.Get<T2>(entity);
                action(entity, in c1, in c2);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetReadOnlySpan<T1>();
            var span2 = chunk.GetReadOnlySpan<T2>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, in span1[i], in span2[i]);
            }
        });
    }

    #endregion

    #region Three Components

    /// <summary>
    /// Processes all matching entities in parallel.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <param name="query">The query builder.</param>
    /// <param name="action">The action to execute for each entity.</param>
    /// <param name="minEntityCount">Minimum entity count to enable parallelization.</param>
    public static void ForEachParallel<T1, T2, T3>(
        this QueryBuilder query,
        EntityAction<T1, T2, T3> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref var c1 = ref query.World.Get<T1>(entity);
                ref var c2 = ref query.World.Get<T2>(entity);
                ref var c3 = ref query.World.Get<T3>(entity);
                action(entity, ref c1, ref c2, ref c3);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetSpan<T1>();
            var span2 = chunk.GetSpan<T2>();
            var span3 = chunk.GetSpan<T3>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, ref span1[i], ref span2[i], ref span3[i]);
            }
        });
    }

    /// <summary>
    /// Processes all matching entities in parallel with readonly component access.
    /// </summary>
    public static void ForEachParallelReadOnly<T1, T2, T3>(
        this QueryBuilder query,
        EntityActionReadOnly<T1, T2, T3> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref readonly var c1 = ref query.World.Get<T1>(entity);
                ref readonly var c2 = ref query.World.Get<T2>(entity);
                ref readonly var c3 = ref query.World.Get<T3>(entity);
                action(entity, in c1, in c2, in c3);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetReadOnlySpan<T1>();
            var span2 = chunk.GetReadOnlySpan<T2>();
            var span3 = chunk.GetReadOnlySpan<T3>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, in span1[i], in span2[i], in span3[i]);
            }
        });
    }

    #endregion

    #region Four Components

    /// <summary>
    /// Processes all matching entities in parallel.
    /// </summary>
    /// <typeparam name="T1">First component type.</typeparam>
    /// <typeparam name="T2">Second component type.</typeparam>
    /// <typeparam name="T3">Third component type.</typeparam>
    /// <typeparam name="T4">Fourth component type.</typeparam>
    /// <param name="query">The query builder.</param>
    /// <param name="action">The action to execute for each entity.</param>
    /// <param name="minEntityCount">Minimum entity count to enable parallelization.</param>
    public static void ForEachParallel<T1, T2, T3, T4>(
        this QueryBuilder query,
        EntityAction<T1, T2, T3, T4> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref var c1 = ref query.World.Get<T1>(entity);
                ref var c2 = ref query.World.Get<T2>(entity);
                ref var c3 = ref query.World.Get<T3>(entity);
                ref var c4 = ref query.World.Get<T4>(entity);
                action(entity, ref c1, ref c2, ref c3, ref c4);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetSpan<T1>();
            var span2 = chunk.GetSpan<T2>();
            var span3 = chunk.GetSpan<T3>();
            var span4 = chunk.GetSpan<T4>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, ref span1[i], ref span2[i], ref span3[i], ref span4[i]);
            }
        });
    }

    /// <summary>
    /// Processes all matching entities in parallel with readonly component access.
    /// </summary>
    public static void ForEachParallelReadOnly<T1, T2, T3, T4>(
        this QueryBuilder query,
        EntityActionReadOnly<T1, T2, T3, T4> action,
        int minEntityCount = DefaultMinEntityCount)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        var archetypes = query.World.GetMatchingArchetypes(query.Description);
        var totalCount = CountEntities(archetypes);

        if (totalCount < minEntityCount)
        {
            foreach (var entity in query)
            {
                ref var c1 = ref query.World.Get<T1>(entity);
                ref var c2 = ref query.World.Get<T2>(entity);
                ref var c3 = ref query.World.Get<T3>(entity);
                ref var c4 = ref query.World.Get<T4>(entity);
                action(entity, in c1, in c2, in c3, in c4);
            }
            return;
        }

        var chunks = CollectChunks(archetypes);

        Parallel.ForEach(chunks, chunk =>
        {
            var span1 = chunk.GetReadOnlySpan<T1>();
            var span2 = chunk.GetReadOnlySpan<T2>();
            var span3 = chunk.GetReadOnlySpan<T3>();
            var span4 = chunk.GetReadOnlySpan<T4>();
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = chunk.GetEntity(i);
                action(entity, in span1[i], in span2[i], in span3[i], in span4[i]);
            }
        });
    }

    #endregion

    #region Helper Methods

    private static int CountEntities(IReadOnlyList<Archetype> archetypes)
    {
        var count = 0;
        foreach (var archetype in archetypes)
        {
            count += archetype.Count;
        }
        return count;
    }

    private static List<ArchetypeChunk> CollectChunks(IReadOnlyList<Archetype> archetypes)
    {
        var chunks = new List<ArchetypeChunk>();
        foreach (var archetype in archetypes)
        {
            foreach (var chunk in archetype.Chunks)
            {
                if (chunk.Count > 0)
                {
                    chunks.Add(chunk);
                }
            }
        }
        return chunks;
    }

    #endregion
}

#region Delegate Types

/// <summary>
/// Delegate for processing an entity with one component.
/// </summary>
public delegate void EntityAction<T1>(Entity entity, ref T1 c1)
    where T1 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with one readonly component.
/// </summary>
public delegate void EntityActionReadOnly<T1>(Entity entity, in T1 c1)
    where T1 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with two components.
/// </summary>
public delegate void EntityAction<T1, T2>(Entity entity, ref T1 c1, ref T2 c2)
    where T1 : struct, IComponent
    where T2 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with two readonly components.
/// </summary>
public delegate void EntityActionReadOnly<T1, T2>(Entity entity, in T1 c1, in T2 c2)
    where T1 : struct, IComponent
    where T2 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with three components.
/// </summary>
public delegate void EntityAction<T1, T2, T3>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with three readonly components.
/// </summary>
public delegate void EntityActionReadOnly<T1, T2, T3>(Entity entity, in T1 c1, in T2 c2, in T3 c3)
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with four components.
/// </summary>
public delegate void EntityAction<T1, T2, T3, T4>(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent;

/// <summary>
/// Delegate for processing an entity with four readonly components.
/// </summary>
public delegate void EntityActionReadOnly<T1, T2, T3, T4>(Entity entity, in T1 c1, in T2 c2, in T3 c3, in T4 c4)
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent;

#endregion
