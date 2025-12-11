namespace KeenEyes;

public sealed partial class World
{
    /// <summary>
    /// Pre-allocates an archetype for the specified component types.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <param name="initialCapacity">Initial capacity for entity storage. Defaults to 16.</param>
    /// <remarks>
    /// <para>
    /// Pre-allocating archetypes can reduce the number of archetype transitions when spawning
    /// many entities with the same component set. This is particularly useful when used with bundles.
    /// </para>
    /// <para>
    /// If an archetype with these component types already exists, this method returns immediately
    /// without creating a new archetype.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.PreallocateArchetype&lt;Position&gt;();
    ///
    /// // Now spawning entities with Position is optimized
    /// for (int i = 0; i &lt; 1000; i++)
    /// {
    ///     var entity = world.Spawn().With(new Position { X = i, Y = i }).Build();
    /// }
    /// </code>
    /// </example>
    public void PreallocateArchetype<T1>(int initialCapacity = 16)
        where T1 : struct, IComponent
    {
        archetypeManager.PreallocateArchetype([typeof(T1)], initialCapacity);
    }

    /// <summary>
    /// Pre-allocates an archetype for the specified component types.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <param name="initialCapacity">Initial capacity for entity storage. Defaults to 16.</param>
    /// <remarks>
    /// <para>
    /// Pre-allocating archetypes can reduce the number of archetype transitions when spawning
    /// many entities with the same component set. This is particularly useful when used with bundles.
    /// </para>
    /// <para>
    /// If an archetype with these component types already exists, this method returns immediately
    /// without creating a new archetype.
    /// </para>
    /// </remarks>
    public void PreallocateArchetype<T1, T2>(int initialCapacity = 16)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        archetypeManager.PreallocateArchetype([typeof(T1), typeof(T2)], initialCapacity);
    }

    /// <summary>
    /// Pre-allocates an archetype for the specified component types.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <typeparam name="T3">The third component type.</typeparam>
    /// <param name="initialCapacity">Initial capacity for entity storage. Defaults to 16.</param>
    /// <remarks>
    /// <para>
    /// Pre-allocating archetypes can reduce the number of archetype transitions when spawning
    /// many entities with the same component set. This is particularly useful when used with bundles.
    /// </para>
    /// <para>
    /// If an archetype with these component types already exists, this method returns immediately
    /// without creating a new archetype.
    /// </para>
    /// </remarks>
    public void PreallocateArchetype<T1, T2, T3>(int initialCapacity = 16)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        archetypeManager.PreallocateArchetype([typeof(T1), typeof(T2), typeof(T3)], initialCapacity);
    }

    /// <summary>
    /// Pre-allocates an archetype for the specified component types.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <typeparam name="T3">The third component type.</typeparam>
    /// <typeparam name="T4">The fourth component type.</typeparam>
    /// <param name="initialCapacity">Initial capacity for entity storage. Defaults to 16.</param>
    /// <remarks>
    /// <para>
    /// Pre-allocating archetypes can reduce the number of archetype transitions when spawning
    /// many entities with the same component set. This is particularly useful when used with bundles.
    /// </para>
    /// <para>
    /// If an archetype with these component types already exists, this method returns immediately
    /// without creating a new archetype.
    /// </para>
    /// </remarks>
    public void PreallocateArchetype<T1, T2, T3, T4>(int initialCapacity = 16)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        archetypeManager.PreallocateArchetype([typeof(T1), typeof(T2), typeof(T3), typeof(T4)], initialCapacity);
    }

    /// <summary>
    /// Pre-allocates an archetype for all components in the specified bundle type.
    /// </summary>
    /// <typeparam name="TBundle">The bundle type.</typeparam>
    /// <param name="initialCapacity">Initial capacity for entity storage. Defaults to 16.</param>
    /// <remarks>
    /// <para>
    /// This method uses reflection to extract component types from the bundle.
    /// For better performance, bundles are automatically pre-allocated during World initialization.
    /// </para>
    /// <para>
    /// Use this method when you want to pre-allocate a bundle archetype after World creation,
    /// or when working with bundles from dynamically loaded assemblies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pre-allocate for a custom bundle
    /// world.PreallocateArchetype&lt;TransformBundle&gt;();
    ///
    /// // Now spawning entities with this bundle is optimized
    /// for (int i = 0; i &lt; 1000; i++)
    /// {
    ///     var entity = world.Spawn()
    ///         .With(new TransformBundle(...))
    ///         .Build();
    /// }
    /// </code>
    /// </example>
    public void PreallocateArchetype<TBundle>(int initialCapacity = 16)
        where TBundle : struct, IBundle
    {
        // Use reflection to get component types from the bundle
        var bundleType = typeof(TBundle);
        var componentTypesField = bundleType.GetField("ComponentTypes",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        if (componentTypesField is not null && componentTypesField.GetValue(null) is Type[] componentTypes)
        {
            archetypeManager.PreallocateArchetype(componentTypes, initialCapacity);
        }
        else
        {
            // Fallback: extract component types from fields
            var fields = bundleType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var types = fields.Where(f => typeof(IComponent).IsAssignableFrom(f.FieldType))
                              .Select(f => f.FieldType)
                              .ToArray();

            if (types.Length > 0)
            {
                archetypeManager.PreallocateArchetype(types, initialCapacity);
            }
        }
    }
}
