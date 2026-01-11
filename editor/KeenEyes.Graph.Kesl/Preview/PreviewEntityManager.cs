using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Preview;

/// <summary>
/// Manages preview entities for shader execution visualization.
/// </summary>
/// <remarks>
/// <para>
/// The PreviewEntityManager creates and manages a collection of preview entities
/// based on the query bindings extracted from a compiled shader graph. Each entity
/// contains components matching the shader's query requirements, with varied initial
/// values to demonstrate shader behavior.
/// </para>
/// </remarks>
public sealed class PreviewEntityManager
{
    private readonly List<PreviewEntity> entities = [];
    private readonly List<QueryBinding> currentBindings = [];

    /// <summary>
    /// Gets the current preview entities.
    /// </summary>
    public IReadOnlyList<PreviewEntity> Entities => entities;

    /// <summary>
    /// Gets or sets the number of preview entities to create.
    /// </summary>
    /// <remarks>
    /// Changing this value requires calling <see cref="RebuildFromBindings"/> to update entities.
    /// </remarks>
    public int EntityCount { get; set; } = 3;

    /// <summary>
    /// Rebuilds preview entities based on new query bindings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called when the graph changes and query bindings are updated.
    /// Creates <see cref="EntityCount"/> entities, each with components
    /// matching the provided bindings.
    /// </para>
    /// <para>
    /// Components are initialized with varied values per entity:
    /// - Entity 0: X=0, Y=0, Z=0
    /// - Entity 1: X=1, Y=0.5, Z=0
    /// - Entity 2: X=2, Y=1, Z=0
    /// </para>
    /// </remarks>
    /// <param name="bindings">The query bindings from the compiled shader.</param>
    public void RebuildFromBindings(IReadOnlyList<QueryBinding> bindings)
    {
        currentBindings.Clear();
        currentBindings.AddRange(bindings);

        entities.Clear();
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = new PreviewEntity(i);
            foreach (var binding in bindings)
            {
                // Skip 'without' bindings - they exclude entities
                if (binding.AccessMode == AccessMode.Without)
                {
                    continue;
                }

                var component = CreateComponentForBinding(binding, i);
                entity.Components[binding.ComponentName] = component;
            }
            entities.Add(entity);
        }
    }

    /// <summary>
    /// Creates a deep copy of current entity state (for before/after comparison).
    /// </summary>
    /// <returns>A list of cloned preview entities.</returns>
    public List<PreviewEntity> CloneCurrentState()
    {
        return entities.Select(e => e.Clone()).ToList();
    }

    /// <summary>
    /// Resets all entity component values to their initial state.
    /// </summary>
    public void Reset()
    {
        if (currentBindings.Count > 0)
        {
            // Create a copy to avoid self-modification during RebuildFromBindings
            var bindingsCopy = currentBindings.ToList();
            RebuildFromBindings(bindingsCopy);
        }
    }

    private static PreviewComponent CreateComponentForBinding(QueryBinding binding, int entityIndex)
    {
        var component = new PreviewComponent(binding.ComponentName);

        // Create default float3 pattern - common for Position, Velocity, etc.
        // Values vary per entity to demonstrate shader behavior
        component.Fields["X"] = entityIndex * 1.0f;
        component.Fields["Y"] = entityIndex * 0.5f;
        component.Fields["Z"] = 0.0f;

        return component;
    }
}
