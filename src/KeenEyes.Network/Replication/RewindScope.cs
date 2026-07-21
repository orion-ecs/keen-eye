namespace KeenEyes.Network.Replication;

/// <summary>
/// A disposable scope that temporarily swaps live entity components to their historical
/// values for lag-compensated hit testing, restoring the originals on dispose.
/// </summary>
/// <remarks>
/// <para>
/// Obtain a scope from <see cref="LagCompensation.Rewind"/>. Inside the scope the listed
/// entities read as they did at the rewound tick, so hit detection against the live world
/// resolves against the state the acting client actually saw. Disposing the scope restores
/// every swapped component to the exact boxed value it held before the swap.
/// </para>
/// <para>
/// This is a <see langword="ref"/> struct: it lives on the stack and must be consumed with a
/// <c>using</c> statement in the same method. Because <c>using</c> compiles to a
/// <c>try</c>/<c>finally</c>, restoration runs even when an exception is thrown inside the
/// scope. Restoration is also self-guarded: if applying the historical values fails partway,
/// the constructor rolls back what it already changed before rethrowing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var tick = lagCompensation.EstimateClientPerceivedTick(attackerClientId);
/// using (lagCompensation.Rewind(targets, tick))
/// {
///     // Positions now read as the attacker saw them; resolve the shot here.
///     var hit = RaycastAgainstLiveWorld(world, ray);
/// }
/// // Live positions are restored exactly here.
/// </code>
/// </example>
public ref struct RewindScope
{
    private readonly IWorld world;
    private readonly List<(Entity Entity, Type Type, object Original)> originals;
    private bool disposed;

    /// <summary>
    /// Initializes a new <see cref="RewindScope"/>, capturing the live values it is about to
    /// overwrite and then applying the historical values.
    /// </summary>
    /// <param name="world">The live world whose components are swapped.</param>
    /// <param name="swaps">
    /// The per-component swaps to perform: the entity, the component type, its current live
    /// (boxed) value, and the historical (boxed) value to apply. Only components the entity
    /// currently has are included, so every applied value has a captured original.
    /// </param>
    internal RewindScope(IWorld world, List<(Entity Entity, Type Type, object Live, object Historical)> swaps)
    {
        this.world = world;
        originals = new List<(Entity, Type, object)>(swaps.Count);

        // Phase 1: record the exact live boxed values first, so restore is byte-for-byte.
        foreach (var (entity, type, live, _) in swaps)
        {
            originals.Add((entity, type, live));
        }

        // Phase 2: apply the historical values. If any application throws, undo the ones
        // already applied (newest first) before propagating, so the world is never left
        // in a partially rewound state.
        var applied = 0;
        try
        {
            foreach (var (entity, type, _, historical) in swaps)
            {
                world.SetComponent(entity, type, historical);
                applied++;
            }
        }
        catch
        {
            for (var i = applied - 1; i >= 0; i--)
            {
                var (entity, type, original) = originals[i];
                if (world.IsAlive(entity))
                {
                    world.SetComponent(entity, type, original);
                }
            }

            disposed = true;
            throw;
        }
    }

    /// <summary>
    /// Restores every swapped component to its original live value.
    /// </summary>
    /// <remarks>
    /// Idempotent: calling more than once (or after a failed construction rollback) does
    /// nothing further. Components on entities that were despawned inside the scope are
    /// skipped rather than resurrected.
    /// </remarks>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Restore in reverse application order to mirror the swap sequence exactly.
        for (var i = originals.Count - 1; i >= 0; i--)
        {
            var (entity, type, original) = originals[i];
            if (world.IsAlive(entity))
            {
                world.SetComponent(entity, type, original);
            }
        }

        originals.Clear();
    }
}
