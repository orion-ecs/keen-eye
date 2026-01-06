namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Comparison operators for the Compare node.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>Equal to (==).</summary>
    Equal,

    /// <summary>Not equal to (!=).</summary>
    NotEqual,

    /// <summary>Less than (&lt;).</summary>
    LessThan,

    /// <summary>Less than or equal (&lt;=).</summary>
    LessThanOrEqual,

    /// <summary>Greater than (&gt;).</summary>
    GreaterThan,

    /// <summary>Greater than or equal (&gt;=).</summary>
    GreaterThanOrEqual
}

/// <summary>
/// Component data for a Compare node.
/// </summary>
/// <remarks>
/// <para>
/// The Compare node compares two values using a configurable operator and
/// outputs a boolean result.
/// </para>
/// </remarks>
public struct CompareNodeData : IComponent
{
    /// <summary>
    /// The comparison operator to use.
    /// </summary>
    public ComparisonOperator Operator;

    /// <summary>
    /// Creates default Compare node data.
    /// </summary>
    public static CompareNodeData Default => new()
    {
        Operator = ComparisonOperator.Equal
    };
}
