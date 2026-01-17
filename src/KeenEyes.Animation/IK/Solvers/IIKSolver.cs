using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Animation.IK.Solvers;

/// <summary>
/// Context passed to IK solvers with all data needed to solve a chain.
/// </summary>
public readonly struct IKSolverContext
{
    /// <summary>World reference for component access.</summary>
    public required IWorld World { get; init; }

    /// <summary>Chain definition with solver settings.</summary>
    public required IKChainDefinition Chain { get; init; }

    /// <summary>Bone entities from root to tip (ordered).</summary>
    public required Entity[] BoneEntities { get; init; }

    /// <summary>Target world position.</summary>
    public required Vector3 TargetPosition { get; init; }

    /// <summary>Target rotation (null if not used).</summary>
    public Quaternion? TargetRotation { get; init; }

    /// <summary>Pole vector world position (null if not used).</summary>
    public Vector3? PolePosition { get; init; }

    /// <summary>Frame delta time for smoothing.</summary>
    public float DeltaTime { get; init; }
}

/// <summary>
/// Result from an IK solver.
/// </summary>
public readonly record struct IKSolverResult
{
    /// <summary>Whether the solver converged to a solution.</summary>
    public required bool Converged { get; init; }

    /// <summary>Number of iterations used (0 for analytical solvers).</summary>
    public required int Iterations { get; init; }

    /// <summary>Final distance from end effector to target.</summary>
    public required float FinalError { get; init; }

    /// <summary>Creates a successful result.</summary>
    public static IKSolverResult Success(int iterations = 0, float error = 0f) => new()
    {
        Converged = true,
        Iterations = iterations,
        FinalError = error
    };

    /// <summary>Creates a partial/non-converged result.</summary>
    public static IKSolverResult Partial(int iterations, float error) => new()
    {
        Converged = false,
        Iterations = iterations,
        FinalError = error
    };
}

/// <summary>
/// Interface for IK chain solvers.
/// </summary>
public interface IIKSolver
{
    /// <summary>Display name for debugging.</summary>
    string Name { get; }

    /// <summary>The solver type this implementation handles.</summary>
    IKSolverType SolverType { get; }

    /// <summary>Solves the IK chain, modifying bone transforms in-place.</summary>
    IKSolverResult Solve(in IKSolverContext context);

    /// <summary>Checks if this solver can handle a chain of the given length.</summary>
    bool CanHandle(int boneCount);
}
