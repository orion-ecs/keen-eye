using KeenEyes.Graph;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Validation.Rules;

namespace KeenEyes.Graph.Kesl.Validation;

/// <summary>
/// Validates KESL shader graphs for correctness.
/// </summary>
/// <remarks>
/// <para>
/// The graph validator runs a set of validation rules against a shader graph
/// and reports errors and warnings. It supports both full graph validation
/// and incremental single-node validation for real-time feedback.
/// </para>
/// <para>
/// Built-in validation rules:
/// <list type="bullet">
/// <item><see cref="SingleRootRule"/> - KESL001/002: Exactly one ComputeShader node</item>
/// <item><see cref="RequiredInputsRule"/> - KESL010: All required ports connected</item>
/// <item><see cref="TypeCompatibilityRule"/> - KESL020: Port type matching</item>
/// <item><see cref="NoCyclesRule"/> - KESL030: No cycles in the graph</item>
/// </list>
/// </para>
/// </remarks>
public sealed class KeslGraphValidator
{
    private readonly List<IValidationRule> rules = [];

    /// <summary>
    /// Initializes a new validator with the standard rule set.
    /// </summary>
    /// <param name="nodeTypeRegistry">The node type registry for port lookups.</param>
    public KeslGraphValidator(NodeTypeRegistry nodeTypeRegistry)
    {
        // Add standard rules
        rules.Add(new SingleRootRule());
        rules.Add(new RequiredInputsRule(nodeTypeRegistry));
        rules.Add(new TypeCompatibilityRule(nodeTypeRegistry));
        rules.Add(new NoCyclesRule());
    }

    /// <summary>
    /// Adds a custom validation rule.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    public void AddRule(IValidationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        rules.Add(rule);
    }

    /// <summary>
    /// Validates an entire shader graph.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The validation result with all errors and warnings.</returns>
    public ValidationResult Validate(Entity canvas, IWorld world)
    {
        var result = new ValidationResult();

        foreach (var rule in rules)
        {
            rule.Validate(canvas, world, result);
        }

        return result;
    }

    /// <summary>
    /// Validates a single node for incremental validation.
    /// </summary>
    /// <param name="node">The node to validate.</param>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The validation result for this node.</returns>
    /// <remarks>
    /// <para>
    /// Incremental validation is useful for real-time feedback as the user
    /// edits the graph. Not all rules support incremental validation; graph-level
    /// rules like cycle detection may be skipped or return incomplete results.
    /// </para>
    /// </remarks>
    public ValidationResult ValidateNode(Entity node, Entity canvas, IWorld world)
    {
        var result = new ValidationResult();

        foreach (var rule in rules)
        {
            rule.ValidateNode(node, canvas, world, result);
        }

        return result;
    }

    /// <summary>
    /// Validates the graph and returns whether it's ready for compilation.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>True if the graph has no errors and can be compiled.</returns>
    public bool CanCompile(Entity canvas, IWorld world)
    {
        return Validate(canvas, world).IsValid;
    }
}
