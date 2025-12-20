using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="IValidationCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock tracks validation mode changes and validator registrations
/// for testing without a real World.
/// </para>
/// </remarks>
public sealed class MockValidationCapability : IValidationCapability
{
    private readonly Dictionary<Type, Delegate> validators = [];
    private readonly List<Type> registeredValidatorTypes = [];

    /// <summary>
    /// Gets the list of component types for which validators were registered.
    /// </summary>
    public IReadOnlyList<Type> RegisteredValidatorTypes => registeredValidatorTypes;

    /// <inheritdoc />
    public ValidationMode ValidationMode { get; set; } = ValidationMode.Enabled;

    /// <inheritdoc />
    public void RegisterValidator<T>(ComponentValidator<T> validator) where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(validator);
        validators[typeof(T)] = validator;
        registeredValidatorTypes.Add(typeof(T));
    }

    /// <inheritdoc />
    public bool UnregisterValidator<T>() where T : struct, IComponent
    {
        return validators.Remove(typeof(T));
    }

    /// <summary>
    /// Gets whether a validator is registered for a component type.
    /// </summary>
    public bool HasValidator<T>() where T : struct, IComponent
    {
        return validators.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Gets the registered validator for a component type.
    /// </summary>
    public ComponentValidator<T>? GetValidator<T>() where T : struct, IComponent
    {
        if (validators.TryGetValue(typeof(T), out var validator))
        {
            return (ComponentValidator<T>)validator;
        }

        return null;
    }

    /// <summary>
    /// Clears all registered validators.
    /// </summary>
    public void Clear()
    {
        validators.Clear();
        registeredValidatorTypes.Clear();
    }
}
