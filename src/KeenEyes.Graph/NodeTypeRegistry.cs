using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Nodes;

namespace KeenEyes.Graph;

/// <summary>
/// Registry for node type definitions.
/// </summary>
/// <remarks>
/// <para>
/// The node type registry stores <see cref="INodeTypeDefinition"/> instances and provides
/// factory methods for node creation. It wraps the legacy <see cref="PortRegistry"/> for
/// backward compatibility while adding support for custom node rendering and initialization.
/// </para>
/// <para>
/// Register custom node types during plugin installation:
/// </para>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     var registry = context.World.GetExtension&lt;NodeTypeRegistry&gt;();
///     registry.Register&lt;AddNode&gt;();
///     registry.Register&lt;MultiplyNode&gt;();
/// }
/// </code>
/// </remarks>
/// <param name="portRegistry">The underlying port registry for backward compatibility.</param>
public sealed class NodeTypeRegistry(PortRegistry portRegistry)
{
    private readonly Dictionary<int, INodeTypeDefinition> definitions = [];

    /// <summary>
    /// Gets the underlying port registry.
    /// </summary>
    /// <remarks>
    /// For backward compatibility with code that accesses port definitions directly.
    /// New code should use <see cref="GetDefinition"/> instead.
    /// </remarks>
    public PortRegistry PortRegistry => portRegistry;

    /// <summary>
    /// Registers a node type definition.
    /// </summary>
    /// <param name="definition">The node type definition to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if definition is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the type ID is already registered.</exception>
    public void Register(INodeTypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definitions.ContainsKey(definition.TypeId))
        {
            throw new ArgumentException(
                $"Node type ID {definition.TypeId} is already registered.",
                nameof(definition));
        }

        definitions[definition.TypeId] = definition;

        // Also register in PortRegistry for backward compatibility
        portRegistry.RegisterNodeType(
            definition.TypeId,
            definition.Name,
            definition.Category,
            [.. definition.InputPorts],
            [.. definition.OutputPorts]);
    }

    /// <summary>
    /// Registers a node type definition by type.
    /// </summary>
    /// <typeparam name="TDefinition">The definition type with a parameterless constructor.</typeparam>
    /// <exception cref="ArgumentException">Thrown if the type ID is already registered.</exception>
    public void Register<TDefinition>() where TDefinition : INodeTypeDefinition, new()
    {
        Register(new TDefinition());
    }

    /// <summary>
    /// Gets the node type definition for the specified type ID.
    /// </summary>
    /// <param name="typeId">The type ID to look up.</param>
    /// <returns>The definition if found; otherwise, null.</returns>
    public INodeTypeDefinition? GetDefinition(int typeId)
    {
        return definitions.TryGetValue(typeId, out var definition) ? definition : null;
    }

    /// <summary>
    /// Gets all node type definitions in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>An enumerable of definitions in the specified category.</returns>
    public IEnumerable<INodeTypeDefinition> GetByCategory(string category)
    {
        return definitions.Values.Where(d => d.Category == category);
    }

    /// <summary>
    /// Gets all unique categories from registered definitions.
    /// </summary>
    /// <returns>An enumerable of category names, sorted alphabetically.</returns>
    public IEnumerable<string> GetCategories()
    {
        return definitions.Values
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c);
    }

    /// <summary>
    /// Gets all registered node type definitions.
    /// </summary>
    /// <returns>An enumerable of all definitions.</returns>
    public IEnumerable<INodeTypeDefinition> GetAll()
    {
        return definitions.Values;
    }

    /// <summary>
    /// Checks if a node type definition is registered.
    /// </summary>
    /// <param name="typeId">The type ID to check.</param>
    /// <returns>True if a definition with the specified ID is registered.</returns>
    public bool IsRegistered(int typeId)
    {
        return definitions.ContainsKey(typeId);
    }

    /// <summary>
    /// Gets the number of registered node type definitions.
    /// </summary>
    public int Count => definitions.Count;

    /// <summary>
    /// Creates a node of the specified type.
    /// </summary>
    /// <param name="canvas">The parent canvas entity.</param>
    /// <param name="typeId">The node type ID.</param>
    /// <param name="position">The initial position in canvas coordinates.</param>
    /// <param name="world">The world to create the node in.</param>
    /// <returns>The created node entity.</returns>
    /// <exception cref="ArgumentException">Thrown if canvas is invalid or typeId is not registered.</exception>
    /// <remarks>
    /// <para>
    /// This factory method creates a node entity with the standard <see cref="GraphNode"/> component
    /// and then calls <see cref="INodeTypeDefinition.Initialize"/> to allow the definition to add
    /// node-specific components.
    /// </para>
    /// <para>
    /// For undoable node creation, use <c>GraphContext.CreateNodeUndoable</c> instead.
    /// </para>
    /// </remarks>
    public Entity CreateNode(Entity canvas, int typeId, Vector2 position, IWorld world)
    {
        if (!world.IsAlive(canvas))
        {
            throw new ArgumentException("Canvas entity is not valid.", nameof(canvas));
        }

        if (!world.Has<GraphCanvas>(canvas))
        {
            throw new ArgumentException("Entity is not a graph canvas.", nameof(canvas));
        }

        if (!definitions.TryGetValue(typeId, out var definition))
        {
            throw new ArgumentException($"Node type {typeId} is not registered.", nameof(typeId));
        }

        var node = world.Spawn()
            .With(new GraphNode
            {
                Position = position,
                Width = 200f,
                Height = 0f, // Calculated by layout
                NodeTypeId = typeId,
                Canvas = canvas,
                DisplayName = null
            })
            .Build();

        world.SetParent(node, canvas);

        // Call the definition's Initialize to add node-specific components
        definition.Initialize(node, world);

        return node;
    }

    /// <summary>
    /// Validates that a type ID is in the user-defined range.
    /// </summary>
    /// <param name="typeId">The type ID to validate.</param>
    /// <returns>True if the type ID is valid for user-defined types (>= 101).</returns>
    public static bool IsUserDefinedTypeId(int typeId)
    {
        return typeId >= BuiltInNodeIds.UserDefinedStart;
    }

    /// <summary>
    /// Validates that a type ID is in the built-in range.
    /// </summary>
    /// <param name="typeId">The type ID to validate.</param>
    /// <returns>True if the type ID is reserved for built-in types (1-100).</returns>
    public static bool IsBuiltInTypeId(int typeId)
    {
        return typeId >= 1 && typeId < BuiltInNodeIds.UserDefinedStart;
    }
}
