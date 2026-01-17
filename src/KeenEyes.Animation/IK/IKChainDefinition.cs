using System.Numerics;

namespace KeenEyes.Animation.IK;

/// <summary>
/// Defines an IK chain configuration with solver settings.
/// </summary>
public sealed class IKChainDefinition
{
    /// <summary>Unique name for this chain.</summary>
    public required string Name { get; init; }

    /// <summary>Bone names from root to tip (inclusive).</summary>
    public required string[] BoneNames { get; init; }

    /// <summary>Algorithm to use for solving.</summary>
    public IKSolverType SolverType { get; set; } = IKSolverType.FABRIK;

    /// <summary>Maximum iterations for iterative solvers.</summary>
    public int MaxIterations { get; set; } = 10;

    /// <summary>Distance tolerance for convergence.</summary>
    public float Tolerance { get; set; } = 0.001f;

    /// <summary>Default pole vector direction for bend control.</summary>
    public Vector3 PoleVector { get; set; } = Vector3.UnitZ;

    /// <summary>Number of bones in the chain.</summary>
    public int BoneCount => BoneNames.Length;

    /// <summary>Creates a two-bone chain (arm/leg).</summary>
    public static IKChainDefinition TwoBone(string name, string root, string mid, string tip, Vector3 poleVector) => new()
    {
        Name = name,
        BoneNames = [root, mid, tip],
        SolverType = IKSolverType.TwoBone,
        PoleVector = poleVector
    };

    /// <summary>Creates a multi-bone FABRIK chain (spine/tail).</summary>
    public static IKChainDefinition MultiBone(string name, string[] bones, int maxIterations = 10) => new()
    {
        Name = name,
        BoneNames = bones,
        SolverType = IKSolverType.FABRIK,
        MaxIterations = maxIterations
    };
}
