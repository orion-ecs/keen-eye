namespace KeenEyes;

public sealed partial class World
{
    #region Validation

    /// <summary>
    /// Gets or sets the validation mode for component constraints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Validation checks <see cref="RequiresComponentAttribute"/> and
    /// <see cref="ConflictsWithAttribute"/> constraints when components are added.
    /// </para>
    /// <para>
    /// Available modes:
    /// <list type="bullet">
    /// <item><description><see cref="KeenEyes.ValidationMode.Enabled"/> - Always validate (default)</description></item>
    /// <item><description><see cref="KeenEyes.ValidationMode.Disabled"/> - Skip all validation</description></item>
    /// <item><description><see cref="KeenEyes.ValidationMode.DebugOnly"/> - Only validate in DEBUG builds</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Disable validation for maximum performance in production
    /// world.ValidationMode = ValidationMode.Disabled;
    ///
    /// // Enable validation only in debug builds
    /// world.ValidationMode = ValidationMode.DebugOnly;
    /// </code>
    /// </example>
    public ValidationMode ValidationMode
    {
        get => validationManager.Mode;
        set => validationManager.Mode = value;
    }

    /// <summary>
    /// Registers a custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to validate.</typeparam>
    /// <param name="validator">
    /// A delegate that receives the world, entity, and component data,
    /// and returns <c>true</c> if validation passes.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Custom validators run in addition to attribute-based validation
    /// (<see cref="RequiresComponentAttribute"/> and <see cref="ConflictsWithAttribute"/>).
    /// </para>
    /// <para>
    /// The validator is called when the component is added via <see cref="Add{T}"/>
    /// or during entity creation via <see cref="EntityBuilder.Build"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Validate that Health.Current never exceeds Health.Max
    /// world.RegisterValidator&lt;Health&gt;((world, entity, health) =>
    ///     health.Current &gt;= 0 &amp;&amp; health.Current &lt;= health.Max &amp;&amp; health.Max &gt; 0);
    ///
    /// // This will throw ComponentValidationException:
    /// world.Add(entity, new Health { Current = 150, Max = 100 });
    /// </code>
    /// </example>
    public void RegisterValidator<T>(ComponentValidator<T> validator) where T : struct, IComponent
        => validationManager.RegisterValidator(validator);

    /// <summary>
    /// Removes a previously registered custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns><c>true</c> if a validator was removed; <c>false</c> if no validator was registered.</returns>
    public bool UnregisterValidator<T>() where T : struct, IComponent
        => validationManager.UnregisterValidator<T>();

    #endregion
}
