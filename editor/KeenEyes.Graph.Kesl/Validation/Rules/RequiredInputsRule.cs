using KeenEyes.Graph;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Validation.Rules;

/// <summary>
/// Validates that all required input ports have connections.
/// </summary>
/// <remarks>
/// <para>
/// Error code KESL010: Required input port is not connected.
/// </para>
/// </remarks>
public sealed class RequiredInputsRule : IValidationRule
{
    private readonly NodeTypeRegistry nodeTypeRegistry;

    /// <summary>
    /// Initializes a new RequiredInputsRule.
    /// </summary>
    /// <param name="nodeTypeRegistry">The node type registry for port definitions.</param>
    public RequiredInputsRule(NodeTypeRegistry nodeTypeRegistry)
    {
        this.nodeTypeRegistry = nodeTypeRegistry;
    }

    /// <inheritdoc />
    public void Validate(Entity canvas, IWorld world, ValidationResult result)
    {
        foreach (var entity in world.Query<GraphNode>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            ValidateNode(entity, canvas, world, result);
        }
    }

    /// <inheritdoc />
    public void ValidateNode(Entity node, Entity canvas, IWorld world, ValidationResult result)
    {
        var nodeData = world.Get<GraphNode>(node);
        var nodeType = nodeTypeRegistry.GetDefinition(nodeData.NodeTypeId);
        if (nodeType is null)
        {
            return;
        }

        var inputPorts = nodeType.InputPorts;
        for (int i = 0; i < inputPorts.Count; i++)
        {
            var port = inputPorts[i];

            // In KESL, all ports are considered required unless they allow multiple
            // (which implies they can have zero or more connections)
            if (port.AllowMultiple)
            {
                continue;
            }

            // Check if this port has a connection
            var hasConnection = false;
            foreach (var entity in world.Query<GraphConnection>())
            {
                var connection = world.Get<GraphConnection>(entity);
                if (connection.Canvas == canvas &&
                    connection.TargetNode == node &&
                    connection.TargetPortIndex == i)
                {
                    hasConnection = true;
                    break;
                }
            }

            if (!hasConnection)
            {
                result.AddError("KESL010",
                    $"Required input '{port.Name}' on node is not connected",
                    node,
                    i);
            }
        }
    }
}
