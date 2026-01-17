namespace KeenEyes.Animation.IK;

/// <summary>
/// Specifies the algorithm used to solve an IK chain.
/// </summary>
public enum IKSolverType
{
    /// <summary>Analytical two-bone solver. O(1). Best for arms/legs.</summary>
    TwoBone,

    /// <summary>Forward And Backward Reaching IK. Iterative. Best for spines/tails.</summary>
    FABRIK,

    /// <summary>Cyclic Coordinate Descent. Iterative. Alternative to FABRIK.</summary>
    CCD,

    /// <summary>Simple aim/look-at constraint. O(1). Best for head/eye tracking.</summary>
    LookAt
}
