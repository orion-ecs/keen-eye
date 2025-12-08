namespace KeenEyes;

/// <summary>
/// Exception thrown when component validation fails during entity creation or modification.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when:
/// <list type="bullet">
/// <item><description>
/// A component marked with <see cref="RequiresComponentAttribute"/> is added to an entity
/// that is missing the required component.
/// </description></item>
/// <item><description>
/// A component marked with <see cref="ConflictsWithAttribute"/> is added to an entity
/// that already has the conflicting component.
/// </description></item>
/// <item><description>
/// A custom validator registered via <see cref="ComponentValidationManager.RegisterValidator{T}"/>
/// returns <c>false</c>.
/// </description></item>
/// </list>
/// </para>
/// </remarks>
public class ComponentValidationException : InvalidOperationException
{
    /// <summary>
    /// Gets the type of component that failed validation.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Gets the entity on which validation failed, if applicable.
    /// </summary>
    public Entity? Entity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="componentType">The type of component that failed validation.</param>
    public ComponentValidationException(string message, Type componentType)
        : base(message)
    {
        ComponentType = componentType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="componentType">The type of component that failed validation.</param>
    /// <param name="entity">The entity on which validation failed.</param>
    public ComponentValidationException(string message, Type componentType, Entity entity)
        : base(message)
    {
        ComponentType = componentType;
        Entity = entity;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="componentType">The type of component that failed validation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ComponentValidationException(string message, Type componentType, Exception innerException)
        : base(message, innerException)
    {
        ComponentType = componentType;
    }
}
