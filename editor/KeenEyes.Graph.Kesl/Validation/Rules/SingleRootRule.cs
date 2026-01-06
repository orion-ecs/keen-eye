using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Validation.Rules;

/// <summary>
/// Validates that a graph has exactly one ComputeShader root node.
/// </summary>
/// <remarks>
/// <para>
/// Error codes:
/// <list type="bullet">
/// <item>KESL001: No ComputeShader node found</item>
/// <item>KESL002: Multiple ComputeShader nodes found</item>
/// </list>
/// </para>
/// </remarks>
public sealed class SingleRootRule : IValidationRule
{
    /// <inheritdoc />
    public void Validate(Entity canvas, IWorld world, ValidationResult result)
    {
        var rootNodes = new List<Entity>();

        foreach (var entity in world.Query<GraphNode>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            if (nodeData.NodeTypeId == KeslNodeIds.ComputeShader)
            {
                rootNodes.Add(entity);
            }
        }

        if (rootNodes.Count == 0)
        {
            result.AddError("KESL001", "Graph must have a ComputeShader node as the root");
        }
        else if (rootNodes.Count > 1)
        {
            result.AddError("KESL002", $"Graph has {rootNodes.Count} ComputeShader nodes but should have exactly one");
            foreach (var node in rootNodes)
            {
                result.AddError("KESL002", "Duplicate ComputeShader node", node);
            }
        }
    }

    /// <inheritdoc />
    public void ValidateNode(Entity node, Entity canvas, IWorld world, ValidationResult result)
    {
        // Single node validation can't check for multiple roots
        // This is only meaningful as a full graph validation
    }
}
