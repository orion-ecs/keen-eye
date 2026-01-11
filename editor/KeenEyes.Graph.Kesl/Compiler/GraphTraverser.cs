using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Compiler;

/// <summary>
/// Traverses a graph in topological order for compilation.
/// </summary>
/// <remarks>
/// <para>
/// Uses Kahn's algorithm to sort nodes by dependency order. Nodes with no
/// input dependencies are processed first, followed by nodes whose dependencies
/// have all been processed.
/// </para>
/// </remarks>
public static class GraphTraverser
{
    /// <summary>
    /// Returns graph nodes in topological (dependency) order.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>Nodes sorted by dependency order, or empty if cycle detected.</returns>
    public static IReadOnlyList<Entity> TopologicalSort(Entity canvas, IWorld world)
    {
        // Collect all nodes on this canvas
        var nodes = new List<Entity>();
        var inDegree = new Dictionary<Entity, int>();
        var adjacency = new Dictionary<Entity, List<Entity>>();

        // First pass: collect nodes and initialize in-degree
        foreach (var entity in world.Query<GraphNode>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            nodes.Add(entity);
            inDegree[entity] = 0;
            adjacency[entity] = [];
        }

        // Second pass: build adjacency from connections and count in-degrees
        foreach (var entity in world.Query<GraphConnection>())
        {
            var connection = world.Get<GraphConnection>(entity);
            if (connection.Canvas != canvas)
            {
                continue;
            }

            // Source -> Target means Target depends on Source
            if (adjacency.TryGetValue(connection.SourceNode, out var neighbors))
            {
                neighbors.Add(connection.TargetNode);
            }

            if (inDegree.ContainsKey(connection.TargetNode))
            {
                inDegree[connection.TargetNode]++;
            }
        }

        // Kahn's algorithm
        var queue = new Queue<Entity>();
        var result = new List<Entity>();

        // Start with nodes that have no dependencies
        foreach (var (node, degree) in inDegree)
        {
            if (degree == 0)
            {
                queue.Enqueue(node);
            }
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            result.Add(node);

            foreach (var neighbor in adjacency[node])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        // If not all nodes were processed, there's a cycle
        if (result.Count != nodes.Count)
        {
            return [];
        }

        return result;
    }

    /// <summary>
    /// Finds the root shader node in a graph (ComputeShader, VertexShader, or FragmentShader).
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The root node, or null if not found.</returns>
    public static Entity? FindRootNode(Entity canvas, IWorld world)
    {
        foreach (var entity in world.Query<GraphNode>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            if (IsRootShaderNode(nodeData.NodeTypeId))
            {
                return entity;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the root shader node and returns its type.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The root node and its type, or null if not found.</returns>
    public static (Entity Node, int TypeId)? FindRootNodeWithType(Entity canvas, IWorld world)
    {
        foreach (var entity in world.Query<GraphNode>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            if (IsRootShaderNode(nodeData.NodeTypeId))
            {
                return (entity, nodeData.NodeTypeId);
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if a node type ID represents a root shader node.
    /// </summary>
    private static bool IsRootShaderNode(int nodeTypeId)
    {
        return nodeTypeId switch
        {
            KeslNodeIds.ComputeShader => true,
            KeslNodeIds.VertexShader => true,
            KeslNodeIds.FragmentShader => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets input connections for a node.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <param name="portIndex">The input port index.</param>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The source nodes connected to this input.</returns>
    public static IEnumerable<(Entity SourceNode, int SourcePort)> GetInputConnections(
        Entity node,
        int portIndex,
        Entity canvas,
        IWorld world)
    {
        foreach (var entity in world.Query<GraphConnection>())
        {
            var connection = world.Get<GraphConnection>(entity);
            if (connection.Canvas != canvas)
            {
                continue;
            }

            if (connection.TargetNode == node && connection.TargetPortIndex == portIndex)
            {
                yield return (connection.SourceNode, connection.SourcePortIndex);
            }
        }
    }
}
