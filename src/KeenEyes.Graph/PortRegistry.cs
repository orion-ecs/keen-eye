using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// Registry for node type port definitions.
/// </summary>
/// <remarks>
/// <para>
/// The port registry stores port definitions for each node type. Node types are
/// identified by integer IDs. Each node type has a name and arrays of input/output
/// port definitions.
/// </para>
/// <para>
/// In Phase 1, node types are registered manually. In Phase 4, the
/// <c>INodeTypeDefinition</c> interface will provide this metadata.
/// </para>
/// </remarks>
public sealed class PortRegistry
{
    private readonly Dictionary<int, NodeTypeInfo> nodeTypes = [];
    private int nextTypeId = 1;

    /// <summary>
    /// Information about a registered node type.
    /// </summary>
    /// <param name="TypeId">The unique type identifier.</param>
    /// <param name="Name">The display name for this node type.</param>
    /// <param name="Category">The category for node palette organization.</param>
    /// <param name="InputPorts">The input port definitions.</param>
    /// <param name="OutputPorts">The output port definitions.</param>
    public readonly record struct NodeTypeInfo(
        int TypeId,
        string Name,
        string Category,
        PortDefinition[] InputPorts,
        PortDefinition[] OutputPorts
    );

    /// <summary>
    /// Registers a new node type with the specified ports.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <param name="category">The category for organization.</param>
    /// <param name="inputPorts">The input port definitions.</param>
    /// <param name="outputPorts">The output port definitions.</param>
    /// <returns>The assigned type ID.</returns>
    public int RegisterNodeType(
        string name,
        string category,
        PortDefinition[] inputPorts,
        PortDefinition[] outputPorts)
    {
        int typeId = nextTypeId++;
        nodeTypes[typeId] = new NodeTypeInfo(typeId, name, category, inputPorts, outputPorts);
        return typeId;
    }

    /// <summary>
    /// Registers a new node type with a specific ID.
    /// </summary>
    /// <param name="typeId">The type ID to use.</param>
    /// <param name="name">The display name.</param>
    /// <param name="category">The category for organization.</param>
    /// <param name="inputPorts">The input port definitions.</param>
    /// <param name="outputPorts">The output port definitions.</param>
    /// <exception cref="ArgumentException">Thrown if typeId is already registered.</exception>
    public void RegisterNodeType(
        int typeId,
        string name,
        string category,
        PortDefinition[] inputPorts,
        PortDefinition[] outputPorts)
    {
        if (nodeTypes.ContainsKey(typeId))
        {
            throw new ArgumentException($"Node type ID {typeId} is already registered.", nameof(typeId));
        }

        nodeTypes[typeId] = new NodeTypeInfo(typeId, name, category, inputPorts, outputPorts);

        if (typeId >= nextTypeId)
        {
            nextTypeId = typeId + 1;
        }
    }

    /// <summary>
    /// Gets the node type information for the specified type ID.
    /// </summary>
    /// <param name="typeId">The type ID to look up.</param>
    /// <returns>The node type information.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the type ID is not registered.</exception>
    public NodeTypeInfo GetNodeType(int typeId)
    {
        if (!nodeTypes.TryGetValue(typeId, out var info))
        {
            throw new KeyNotFoundException($"Node type ID {typeId} is not registered.");
        }

        return info;
    }

    /// <summary>
    /// Tries to get the node type information for the specified type ID.
    /// </summary>
    /// <param name="typeId">The type ID to look up.</param>
    /// <param name="info">The node type information if found.</param>
    /// <returns>True if the type was found.</returns>
    public bool TryGetNodeType(int typeId, out NodeTypeInfo info)
    {
        return nodeTypes.TryGetValue(typeId, out info);
    }

    /// <summary>
    /// Gets all registered node types.
    /// </summary>
    /// <returns>An enumerable of all node type information.</returns>
    public IEnumerable<NodeTypeInfo> GetAllNodeTypes() => nodeTypes.Values;

    /// <summary>
    /// Gets all node types in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>An enumerable of matching node type information.</returns>
    public IEnumerable<NodeTypeInfo> GetNodeTypesByCategory(string category)
        => nodeTypes.Values.Where(t => t.Category == category);

    /// <summary>
    /// Gets all unique categories.
    /// </summary>
    /// <returns>An enumerable of category names.</returns>
    public IEnumerable<string> GetCategories()
        => nodeTypes.Values.Select(t => t.Category).Distinct().OrderBy(c => c);

    /// <summary>
    /// Checks if a node type is registered.
    /// </summary>
    /// <param name="typeId">The type ID to check.</param>
    /// <returns>True if the type is registered.</returns>
    public bool IsRegistered(int typeId) => nodeTypes.ContainsKey(typeId);

    /// <summary>
    /// Gets the total number of registered node types.
    /// </summary>
    public int Count => nodeTypes.Count;

    /// <summary>
    /// Gets the input port definitions for a node type.
    /// </summary>
    /// <param name="typeId">The type ID.</param>
    /// <returns>The input port definitions.</returns>
    public PortDefinition[] GetInputPorts(int typeId) => GetNodeType(typeId).InputPorts;

    /// <summary>
    /// Gets the output port definitions for a node type.
    /// </summary>
    /// <param name="typeId">The type ID.</param>
    /// <returns>The output port definitions.</returns>
    public PortDefinition[] GetOutputPorts(int typeId) => GetNodeType(typeId).OutputPorts;

    /// <summary>
    /// Registers a default "Generic" node type with configurable ports.
    /// </summary>
    /// <remarks>
    /// This is a placeholder node type for Phase 1 testing before
    /// <c>INodeTypeDefinition</c> is implemented.
    /// </remarks>
    /// <returns>The type ID for the generic node.</returns>
    public int RegisterGenericNode()
    {
        return RegisterNodeType(
            "Node",
            "General",
            [
                PortDefinition.Input("Input 1", PortTypeId.Any, 60f),
                PortDefinition.Input("Input 2", PortTypeId.Any, 90f)
            ],
            [
                PortDefinition.Output("Output", PortTypeId.Any, 60f)
            ]);
    }
}
