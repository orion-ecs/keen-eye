using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Validation.Rules;

/// <summary>
/// Interface for graph validation rules.
/// </summary>
/// <remarks>
/// <para>
/// Validation rules check specific aspects of a shader graph and report
/// errors or warnings to the validation result. Rules can validate the
/// entire graph or individual nodes.
/// </para>
/// </remarks>
public interface IValidationRule
{
    /// <summary>
    /// Validates the entire graph.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <param name="result">The validation result to add messages to.</param>
    void Validate(Entity canvas, IWorld world, ValidationResult result);

    /// <summary>
    /// Validates a single node for incremental validation.
    /// </summary>
    /// <param name="node">The node to validate.</param>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <param name="result">The validation result to add messages to.</param>
    void ValidateNode(Entity node, Entity canvas, IWorld world, ValidationResult result);
}
