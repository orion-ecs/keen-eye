using KeenEyes.Graph;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Validation.Rules;

/// <summary>
/// Validates that connection source and target port types are compatible.
/// </summary>
/// <remarks>
/// <para>
/// Error code KESL020: Incompatible port types in connection.
/// </para>
/// <para>
/// Type compatibility rules:
/// <list type="bullet">
/// <item>Any type can connect to Any</item>
/// <item>Float can connect to Float, Float2, Float3, Float4 (promotion)</item>
/// <item>Int can connect to Int, Int2, Int3, Int4 (promotion)</item>
/// <item>Same types always compatible</item>
/// </list>
/// </para>
/// </remarks>
public sealed class TypeCompatibilityRule : IValidationRule
{
    private readonly NodeTypeRegistry nodeTypeRegistry;

    /// <summary>
    /// Initializes a new TypeCompatibilityRule.
    /// </summary>
    /// <param name="nodeTypeRegistry">The node type registry for port definitions.</param>
    public TypeCompatibilityRule(NodeTypeRegistry nodeTypeRegistry)
    {
        this.nodeTypeRegistry = nodeTypeRegistry;
    }

    /// <inheritdoc />
    public void Validate(Entity canvas, IWorld world, ValidationResult result)
    {
        foreach (var entity in world.Query<GraphConnection>())
        {
            var connection = world.Get<GraphConnection>(entity);
            if (connection.Canvas != canvas)
            {
                continue;
            }

            ValidateConnection(connection, world, result);
        }
    }

    /// <inheritdoc />
    public void ValidateNode(Entity node, Entity canvas, IWorld world, ValidationResult result)
    {
        // Check all connections to/from this node
        foreach (var entity in world.Query<GraphConnection>())
        {
            var connection = world.Get<GraphConnection>(entity);
            if (connection.Canvas != canvas)
            {
                continue;
            }

            if (connection.SourceNode == node || connection.TargetNode == node)
            {
                ValidateConnection(connection, world, result);
            }
        }
    }

    private void ValidateConnection(GraphConnection connection, IWorld world, ValidationResult result)
    {
        // Get source port type
        var sourceNodeData = world.Get<GraphNode>(connection.SourceNode);
        var sourceNodeType = nodeTypeRegistry.GetDefinition(sourceNodeData.NodeTypeId);
        if (sourceNodeType is null)
        {
            return;
        }

        var sourceOutputs = sourceNodeType.OutputPorts;
        if (connection.SourcePortIndex >= sourceOutputs.Count)
        {
            return;
        }
        var sourceType = sourceOutputs[connection.SourcePortIndex].TypeId;

        // Get target port type
        var targetNodeData = world.Get<GraphNode>(connection.TargetNode);
        var targetNodeType = nodeTypeRegistry.GetDefinition(targetNodeData.NodeTypeId);
        if (targetNodeType is null)
        {
            return;
        }

        var targetInputs = targetNodeType.InputPorts;
        if (connection.TargetPortIndex >= targetInputs.Count)
        {
            return;
        }
        var targetType = targetInputs[connection.TargetPortIndex].TypeId;

        // Check compatibility
        if (!AreTypesCompatible(sourceType, targetType))
        {
            result.AddError("KESL020",
                $"Type mismatch: cannot connect {sourceType} to {targetType}",
                connection.TargetNode,
                connection.TargetPortIndex);
        }
    }

    private static bool AreTypesCompatible(PortTypeId source, PortTypeId target)
    {
        // Same type is always compatible
        if (source == target)
        {
            return true;
        }

        // Any type accepts anything
        if (target == PortTypeId.Any || source == PortTypeId.Any)
        {
            return true;
        }

        // Float can be promoted to vector types
        if (source == PortTypeId.Float)
        {
            return target is PortTypeId.Float2 or PortTypeId.Float3 or PortTypeId.Float4;
        }

        // Int can be promoted to int vector types
        if (source == PortTypeId.Int)
        {
            return target is PortTypeId.Int2 or PortTypeId.Int3 or PortTypeId.Int4;
        }

        // Float and Int are interchangeable in shader contexts
        if ((source == PortTypeId.Float && target == PortTypeId.Int) ||
            (source == PortTypeId.Int && target == PortTypeId.Float))
        {
            return true;
        }

        return false;
    }
}
