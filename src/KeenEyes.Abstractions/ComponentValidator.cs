namespace KeenEyes;

/// <summary>
/// Delegate for custom component validation.
/// </summary>
/// <typeparam name="T">The component type to validate.</typeparam>
/// <param name="world">The world the entity belongs to.</param>
/// <param name="entity">The entity receiving the component.</param>
/// <param name="component">The component data being added.</param>
/// <returns><c>true</c> if validation passes; <c>false</c> otherwise.</returns>
/// <remarks>
/// <para>
/// Custom validators are called when components are added to entities, after
/// any attribute-based validation (like <see cref="RequiresComponentAttribute"/>).
/// </para>
/// <para>
/// If the validator returns <c>false</c>, a <c>ComponentValidationException</c>
/// will be thrown, preventing the component from being added.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate that Health.Current never exceeds Health.Max
/// world.RegisterValidator&lt;Health&gt;((world, entity, health) =>
///     health.Current >= 0 &amp;&amp; health.Current &lt;= health.Max &amp;&amp; health.Max > 0);
/// </code>
/// </example>
public delegate bool ComponentValidator<T>(IWorld world, Entity entity, T component)
    where T : struct, IComponent;
