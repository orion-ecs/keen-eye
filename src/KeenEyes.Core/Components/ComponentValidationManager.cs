using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Delegate for custom component validators.
/// </summary>
/// <typeparam name="T">The component type being validated.</typeparam>
/// <param name="world">The world containing the entity.</param>
/// <param name="entity">The entity being validated.</param>
/// <param name="component">The component data being added.</param>
/// <returns><c>true</c> if validation passes; <c>false</c> otherwise.</returns>
public delegate bool ComponentValidator<T>(World world, Entity entity, T component) where T : struct, IComponent;

/// <summary>
/// Manages component validation constraints including dependencies, conflicts, and custom validators.
/// </summary>
/// <remarks>
/// <para>
/// The validation manager reads <see cref="RequiresComponentAttribute"/> and
/// <see cref="ConflictsWithAttribute"/> from component types and caches the results
/// for efficient runtime validation.
/// </para>
/// <para>
/// Validation can be controlled using <see cref="ValidationMode"/>:
/// <list type="bullet">
/// <item><description><see cref="ValidationMode.Enabled"/> - Always validate (default)</description></item>
/// <item><description><see cref="ValidationMode.Disabled"/> - Skip all validation</description></item>
/// <item><description><see cref="ValidationMode.DebugOnly"/> - Only validate when DEBUG is defined</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="world">The world this manager belongs to.</param>
internal sealed class ComponentValidationManager(World world)
{
    private readonly Dictionary<Type, ComponentValidationInfo> validationCache = [];
    private readonly Dictionary<Type, Delegate> customValidators = [];

    /// <summary>
    /// Gets or sets the validation mode for this manager.
    /// </summary>
    public ValidationMode Mode { get; set; } = ValidationMode.Enabled;

    /// <summary>
    /// Registers a custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type to validate.</typeparam>
    /// <param name="validator">The validation delegate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validator"/> is null.</exception>
    public void RegisterValidator<T>(ComponentValidator<T> validator) where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(validator);
        customValidators[typeof(T)] = validator;
    }

    /// <summary>
    /// Removes the custom validator for a component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns><c>true</c> if a validator was removed; <c>false</c> if no validator was registered.</returns>
    public bool UnregisterValidator<T>() where T : struct, IComponent
    {
        return customValidators.Remove(typeof(T));
    }

    /// <summary>
    /// Validates a component being added to an existing entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity receiving the component.</param>
    /// <param name="component">The component data.</param>
    /// <exception cref="ComponentValidationException">Thrown when validation fails.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ValidateAdd<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (!ShouldValidate())
        {
            return;
        }

        var type = typeof(T);
        var info = GetOrCreateValidationInfo(type);

        // Check required components
        ValidateRequirements(entity, type, info);

        // Check conflicting components
        ValidateConflicts(entity, type, info);

        // Run custom validator if registered
        ValidateCustom(entity, in component);
    }

    /// <summary>
    /// Validates a set of components being added during entity creation.
    /// </summary>
    /// <param name="components">The components being added.</param>
    /// <exception cref="ComponentValidationException">Thrown when validation fails.</exception>
    public void ValidateBuild(IReadOnlyList<(ComponentInfo Info, object Data)> components)
    {
        if (!ShouldValidate())
        {
            return;
        }

        // Build a set of component types being added for cross-validation
        var componentTypes = new HashSet<Type>();
        foreach (var (info, _) in components)
        {
            componentTypes.Add(info.Type);
        }

        // Validate each component
        foreach (var (info, _) in components)
        {
            var validationInfo = GetOrCreateValidationInfo(info.Type);

            // Check requirements against the set of components being added
            ValidateRequirementsBuild(info.Type, validationInfo, componentTypes);

            // Check conflicts against the set of components being added
            ValidateConflictsBuild(info.Type, validationInfo, componentTypes);
        }
    }

    /// <summary>
    /// Validates a set of components being added during entity creation, including custom validators.
    /// </summary>
    /// <param name="entity">The newly created entity.</param>
    /// <param name="components">The components that were added.</param>
    /// <exception cref="ComponentValidationException">Thrown when custom validation fails.</exception>
    /// <remarks>
    /// Uses <see cref="ComponentInfo.InvokeValidator"/> delegate for AOT-compatible validation
    /// without reflection.
    /// </remarks>
    public void ValidateBuildCustom(Entity entity, IReadOnlyList<(ComponentInfo Info, object Data)> components)
    {
        if (!ShouldValidate())
        {
            return;
        }

        // Run custom validators for each component
        foreach (var (info, data) in components)
        {
            if (customValidators.TryGetValue(info.Type, out var validator))
            {
                // Use the pre-stored invoker delegate (AOT-compatible)
                if (info.InvokeValidator is null)
                {
                    throw new InvalidOperationException(
                        $"Component type '{info.Type.Name}' does not have a validator invoker delegate.");
                }

                var result = info.InvokeValidator(world, entity, data, validator);
                if (!result)
                {
                    throw new ComponentValidationException(
                        $"Custom validation failed for component '{info.Type.Name}' on entity {entity}.",
                        info.Type,
                        entity);
                }
            }
        }
    }

    /// <summary>
    /// Gets the cached validation info for a component type, or creates and caches it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ComponentValidationInfo GetOrCreateValidationInfo(Type componentType)
    {
        if (validationCache.TryGetValue(componentType, out var info))
        {
            return info;
        }

        info = CreateValidationInfo(componentType);
        validationCache[componentType] = info;
        return info;
    }

    /// <summary>
    /// Creates validation info from the registered constraint provider.
    /// </summary>
    /// <remarks>
    /// Returns empty constraints if no constraint provider has been registered.
    /// To enable validation, call <see cref="RegisterConstraintProvider"/> early
    /// in application startup with the generated <c>ComponentValidationMetadata.TryGetConstraints</c>.
    /// </remarks>
    private static ComponentValidationInfo CreateValidationInfo(Type componentType)
    {
        if (TryGetGeneratedConstraints(componentType, out var required, out var conflicts))
        {
            return new ComponentValidationInfo(required, conflicts);
        }

        // No constraints registered for this component type
        return new ComponentValidationInfo([], []);
    }

    /// <summary>
    /// Validates required components are present on an existing entity.
    /// </summary>
    private void ValidateRequirements(Entity entity, Type componentType, ComponentValidationInfo info)
    {
        var missingRequired = info.RequiredComponents
            .Where(requiredType => !world.HasComponent(entity, requiredType))
            .FirstOrDefault();

        if (missingRequired != null)
        {
            throw new ComponentValidationException(
                $"Component '{componentType.Name}' requires '{missingRequired.Name}' to be present on entity {entity}.",
                componentType,
                entity);
        }
    }

    /// <summary>
    /// Validates no conflicting components are present on an existing entity.
    /// </summary>
    private void ValidateConflicts(Entity entity, Type componentType, ComponentValidationInfo info)
    {
        var presentConflict = info.ConflictingComponents
            .Where(conflictType => world.HasComponent(entity, conflictType))
            .FirstOrDefault();

        if (presentConflict != null)
        {
            throw new ComponentValidationException(
                $"Component '{componentType.Name}' conflicts with '{presentConflict.Name}' which is present on entity {entity}.",
                componentType,
                entity);
        }
    }

    /// <summary>
    /// Validates required components are present in the build set.
    /// </summary>
    private static void ValidateRequirementsBuild(Type componentType, ComponentValidationInfo info, HashSet<Type> componentTypes)
    {
        var missingRequired = info.RequiredComponents
            .Where(requiredType => !componentTypes.Contains(requiredType))
            .FirstOrDefault();

        if (missingRequired != null)
        {
            throw new ComponentValidationException(
                $"Component '{componentType.Name}' requires '{missingRequired.Name}' to be present. " +
                $"Add '{missingRequired.Name}' to the entity builder before '{componentType.Name}'.",
                componentType);
        }
    }

    /// <summary>
    /// Validates no conflicting components are present in the build set.
    /// </summary>
    private static void ValidateConflictsBuild(Type componentType, ComponentValidationInfo info, HashSet<Type> componentTypes)
    {
        var presentConflict = info.ConflictingComponents
            .Where(conflictType => componentTypes.Contains(conflictType) && conflictType != componentType)
            .FirstOrDefault();

        if (presentConflict != null)
        {
            throw new ComponentValidationException(
                $"Component '{componentType.Name}' conflicts with '{presentConflict.Name}'. " +
                $"These components cannot be added to the same entity.",
                componentType);
        }
    }

    /// <summary>
    /// Runs the custom validator for a component.
    /// </summary>
    private void ValidateCustom<T>(Entity entity, in T component) where T : struct, IComponent
    {
        if (customValidators.TryGetValue(typeof(T), out var validator))
        {
            var typedValidator = (ComponentValidator<T>)validator;
            if (!typedValidator(world, entity, component))
            {
                throw new ComponentValidationException(
                    $"Custom validation failed for component '{typeof(T).Name}' on entity {entity}.",
                    typeof(T),
                    entity);
            }
        }
    }

    /// <summary>
    /// Determines if validation should be performed based on the current mode.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldValidate()
    {
        return Mode switch
        {
            ValidationMode.Enabled => true,
            ValidationMode.Disabled => false,
            ValidationMode.DebugOnly => IsDebugBuild(),
            _ => true
        };
    }

    /// <summary>
    /// Checks if this is a debug build.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDebugBuild()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Delegate type for the generated TryGetConstraints method.
    /// </summary>
    /// <param name="componentType">The component type to look up.</param>
    /// <param name="required">Output array of required component types.</param>
    /// <param name="conflicts">Output array of conflicting component types.</param>
    /// <returns><c>true</c> if constraints were found; <c>false</c> otherwise.</returns>
    public delegate bool TryGetConstraintsDelegate(Type componentType, out Type[] required, out Type[] conflicts);

    /// <summary>
    /// Registered constraint provider delegate (AOT-compatible).
    /// </summary>
    private static TryGetConstraintsDelegate? registeredConstraintProvider;

    /// <summary>
    /// Registers a constraint provider for AOT-compatible validation metadata lookup.
    /// </summary>
    /// <param name="provider">The delegate that provides validation constraints for component types.</param>
    /// <remarks>
    /// <para>
    /// This method enables AOT-compatible constraint lookup without assembly scanning or reflection.
    /// The source generator creates a <c>ComponentValidationMetadata</c> class with a static
    /// <c>TryGetConstraints</c> method that should be registered here.
    /// </para>
    /// <para>
    /// Call this method early in application startup (e.g., in <c>Main</c> or module initializer):
    /// </para>
    /// <code>
    /// ComponentValidationManager.RegisterConstraintProvider(ComponentValidationMetadata.TryGetConstraints);
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
    public static void RegisterConstraintProvider(TryGetConstraintsDelegate provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        registeredConstraintProvider = provider;
    }

    /// <summary>
    /// Attempts to get validation constraints from the registered constraint provider.
    /// </summary>
    /// <param name="componentType">The component type to look up.</param>
    /// <param name="required">Output array of required component types.</param>
    /// <param name="conflicts">Output array of conflicting component types.</param>
    /// <returns><c>true</c> if constraints were found; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Returns <c>false</c> if no constraint provider has been registered via
    /// <see cref="RegisterConstraintProvider"/>. Call that method early in application
    /// startup to enable validation constraint lookup.
    /// </remarks>
    private static bool TryGetGeneratedConstraints(Type componentType, out Type[] required, out Type[] conflicts)
    {
        required = [];
        conflicts = [];

        if (registeredConstraintProvider is not null)
        {
            return registeredConstraintProvider(componentType, out required, out conflicts);
        }

        return false;
    }
}

/// <summary>
/// Cached validation information for a component type.
/// </summary>
/// <param name="requiredComponents">Types that must be present on the entity when this component is added.</param>
/// <param name="conflictingComponents">Types that cannot coexist with this component on the same entity.</param>
internal readonly struct ComponentValidationInfo(Type[] requiredComponents, Type[] conflictingComponents)
{
    /// <summary>
    /// Types that must be present on the entity when this component is added.
    /// </summary>
    public Type[] RequiredComponents { get; } = requiredComponents;

    /// <summary>
    /// Types that cannot coexist with this component on the same entity.
    /// </summary>
    public Type[] ConflictingComponents { get; } = conflictingComponents;

    /// <summary>
    /// Whether this component has any validation constraints.
    /// </summary>
    public bool HasConstraints => RequiredComponents.Length > 0 || ConflictingComponents.Length > 0;
}
