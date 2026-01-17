using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;

namespace KeenEyes.Animation;

/// <summary>
/// Manages IK rig definitions, chain configurations, and solver instances.
/// </summary>
public sealed class IKManager : IDisposable
{
    private readonly Dictionary<int, IKRigDefinition> rigs = [];
    private readonly Dictionary<int, IKChainDefinition> chains = [];
    private readonly Dictionary<IKSolverType, IIKSolver> solvers = [];

    private int nextRigId = 1;
    private int nextChainId = 1;
    private bool isDisposed;

    /// <summary>Number of registered rigs.</summary>
    public int RigCount => rigs.Count;

    /// <summary>Number of registered chains.</summary>
    public int ChainCount => chains.Count;

    /// <summary>Number of registered solvers.</summary>
    public int SolverCount => solvers.Count;

    #region Rig Registration

    /// <summary>Registers an IK rig definition and returns its ID.</summary>
    /// <param name="rig">The rig definition to register.</param>
    /// <returns>The assigned rig ID.</returns>
    public int RegisterRig(IKRigDefinition rig)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(rig);

        var id = nextRigId++;
        rigs[id] = rig;

        // Also register all chains from the rig
        foreach (var chain in rig.Chains)
        {
            RegisterChain(chain);
        }

        return id;
    }

    /// <summary>Gets a rig by ID.</summary>
    /// <param name="rigId">The rig ID.</param>
    /// <returns>The rig definition.</returns>
    public IKRigDefinition GetRig(int rigId) => rigs[rigId];

    /// <summary>Tries to get a rig by ID.</summary>
    /// <param name="rigId">The rig ID.</param>
    /// <param name="rig">The rig definition if found.</param>
    /// <returns>True if the rig was found.</returns>
    public bool TryGetRig(int rigId, out IKRigDefinition? rig)
        => rigs.TryGetValue(rigId, out rig);

    /// <summary>Unregisters a rig by ID.</summary>
    /// <param name="rigId">The rig ID to unregister.</param>
    /// <returns>True if the rig was removed.</returns>
    public bool UnregisterRig(int rigId) => rigs.Remove(rigId);

    #endregion

    #region Chain Registration

    /// <summary>Registers an IK chain definition and returns its ID.</summary>
    /// <param name="chain">The chain definition to register.</param>
    /// <returns>The assigned chain ID.</returns>
    public int RegisterChain(IKChainDefinition chain)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(chain);

        var id = nextChainId++;
        chains[id] = chain;
        return id;
    }

    /// <summary>Gets a chain by ID.</summary>
    /// <param name="chainId">The chain ID.</param>
    /// <returns>The chain definition.</returns>
    public IKChainDefinition GetChain(int chainId) => chains[chainId];

    /// <summary>Tries to get a chain by ID.</summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="chain">The chain definition if found.</param>
    /// <returns>True if the chain was found.</returns>
    public bool TryGetChain(int chainId, out IKChainDefinition? chain)
        => chains.TryGetValue(chainId, out chain);

    /// <summary>Unregisters a chain by ID.</summary>
    /// <param name="chainId">The chain ID to unregister.</param>
    /// <returns>True if the chain was removed.</returns>
    public bool UnregisterChain(int chainId) => chains.Remove(chainId);

    #endregion

    #region Solver Management

    /// <summary>Registers a solver instance for its solver type.</summary>
    /// <param name="solver">The solver instance to register.</param>
    public void RegisterSolver(IIKSolver solver)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(solver);

        solvers[solver.SolverType] = solver;
    }

    /// <summary>Gets the solver for a chain by chain ID.</summary>
    /// <param name="chainId">The chain ID.</param>
    /// <returns>The solver for the chain's solver type.</returns>
    /// <exception cref="KeyNotFoundException">If the chain ID is not found.</exception>
    /// <exception cref="InvalidOperationException">If no solver is registered for the chain's type.</exception>
    public IIKSolver GetSolver(int chainId)
    {
        if (!TryGetChain(chainId, out var chain) || chain is null)
        {
            throw new KeyNotFoundException($"Chain with ID {chainId} not found.");
        }

        return GetSolverForType(chain.SolverType);
    }

    /// <summary>Gets a solver by type.</summary>
    /// <param name="solverType">The solver type.</param>
    /// <returns>The registered solver.</returns>
    /// <exception cref="InvalidOperationException">If no solver is registered for the type.</exception>
    public IIKSolver GetSolverForType(IKSolverType solverType)
    {
        if (solvers.TryGetValue(solverType, out var solver))
        {
            return solver;
        }

        throw new InvalidOperationException(
            $"No solver registered for type {solverType}. " +
            "Register solvers using RegisterSolver().");
    }

    /// <summary>Tries to get a solver by type.</summary>
    /// <param name="solverType">The solver type.</param>
    /// <param name="solver">The solver if found.</param>
    /// <returns>True if the solver was found.</returns>
    public bool TryGetSolver(IKSolverType solverType, out IIKSolver? solver)
        => solvers.TryGetValue(solverType, out solver);

    /// <summary>Checks if a solver is registered for the given type.</summary>
    /// <param name="solverType">The solver type to check.</param>
    /// <returns>True if a solver is registered.</returns>
    public bool HasSolver(IKSolverType solverType) => solvers.ContainsKey(solverType);

    #endregion

    #region Cleanup

    /// <summary>Clears all registered rigs, chains, and solvers.</summary>
    public void Clear()
    {
        rigs.Clear();
        chains.Clear();
        solvers.Clear();
        nextRigId = 1;
        nextChainId = 1;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        Clear();
        isDisposed = true;
    }

    #endregion
}
