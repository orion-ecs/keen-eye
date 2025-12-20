namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for component validation configuration.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to component validation settings and
/// custom validator registration. Plugins that need to configure validation
/// behavior or register custom validators should request this capability via
/// <see cref="IPluginContext.GetCapability{T}"/>.
/// </para>
/// <para>
/// Validation checks component constraints when components are added to entities,
/// helping catch invalid component combinations at runtime.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;IValidationCapability&gt;(out var validation))
///     {
///         // Disable validation for performance in production
///         validation.ValidationMode = ValidationMode.DebugOnly;
///
///         // Register a custom validator
///         validation.RegisterValidator&lt;Health&gt;((world, entity, health) =>
///             health.Current >= 0 &amp;&amp; health.Current &lt;= health.Max);
///     }
/// }
/// </code>
/// </example>
public interface IValidationCapability
{
    /// <summary>
    /// Gets or sets the validation mode for component constraints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Available modes:
    /// <list type="bullet">
    /// <item><description><see cref="KeenEyes.ValidationMode.Enabled"/> - Always validate (default)</description></item>
    /// <item><description><see cref="KeenEyes.ValidationMode.Disabled"/> - Skip all validation</description></item>
    /// <item><description><see cref="KeenEyes.ValidationMode.DebugOnly"/> - Only validate in DEBUG builds</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    ValidationMode ValidationMode { get; set; }

    /// <summary>
    /// Registers a custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to validate.</typeparam>
    /// <param name="validator">
    /// A delegate that receives the world, entity, and component data,
    /// and returns true if validation passes.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when validator is null.</exception>
    void RegisterValidator<T>(ComponentValidator<T> validator) where T : struct, IComponent;

    /// <summary>
    /// Removes a previously registered custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>True if a validator was removed; false if no validator was registered.</returns>
    bool UnregisterValidator<T>() where T : struct, IComponent;
}
