using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Compiler;

namespace KeenEyes.Graph.Kesl.Validation.Rules;

/// <summary>
/// Validates that the graph has no cycles (is a DAG).
/// </summary>
/// <remarks>
/// <para>
/// Error code KESL030: Graph contains a cycle.
/// </para>
/// <para>
/// Uses topological sort to detect cycles. If the topological sort
/// cannot process all nodes, a cycle exists.
/// </para>
/// </remarks>
public sealed class NoCyclesRule : IValidationRule
{
    /// <inheritdoc />
    public void Validate(Entity canvas, IWorld world, ValidationResult result)
    {
        // Count nodes on this canvas
        var nodeCount = 0;
        foreach (var entity in world.Query<GraphNode>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas == canvas)
            {
                nodeCount++;
            }
        }

        if (nodeCount == 0)
        {
            return;
        }

        // Try topological sort
        var sorted = GraphTraverser.TopologicalSort(canvas, world);

        // If sorted.Count != nodeCount, there's a cycle
        if (sorted.Count != nodeCount)
        {
            result.AddError("KESL030", "Graph contains a cycle which prevents compilation");

            // Try to identify nodes involved in cycles
            var sortedSet = new HashSet<Entity>(sorted);
            foreach (var entity in world.Query<GraphNode>())
            {
                var nodeData = world.Get<GraphNode>(entity);
                if (nodeData.Canvas == canvas && !sortedSet.Contains(entity))
                {
                    result.AddError("KESL030", "Node is part of a cycle", entity);
                }
            }
        }
    }

    /// <inheritdoc />
    public void ValidateNode(Entity node, Entity canvas, IWorld world, ValidationResult result)
    {
        // Cycle detection requires full graph analysis
        // Single-node validation cannot determine cycles
    }
}
