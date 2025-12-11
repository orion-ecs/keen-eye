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
    /// This method uses the static abstract <see cref="IBundle.ComponentTypes"/> member
    /// for AOT-compatible access to bundle component types without reflection.
    /// </para>
    /// <para>
    /// For better performance, bundles are automatically pre-allocated during World initialization.
    /// Use this method when you want to pre-allocate a bundle archetype after World creation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pre-allocate for a custom bundle
    /// world.PreallocateArchetypeFor&lt;TransformBundle&gt;();
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
    public void PreallocateArchetypeFor<TBundle>(int initialCapacity = 16)
        where TBundle : struct, IBundle
    {
        // Use static abstract interface member for AOT-compatible access (no reflection)
        var componentTypes = TBundle.ComponentTypes;
        if (componentTypes.Length > 0)
        {
            archetypeManager.PreallocateArchetype(componentTypes, initialCapacity);
        }
    }
}
