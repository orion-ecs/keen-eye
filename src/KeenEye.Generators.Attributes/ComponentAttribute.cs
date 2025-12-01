using System;

namespace KeenEye;

/// <summary>
/// Marks a struct as an ECS component. Source generators will produce:
/// - Component ID and metadata
/// - Fluent builder methods (WithComponentName)
/// - Serialization code (if Serializable = true)
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ComponentAttribute : Attribute
{
    /// <summary>
    /// If true, generates binary serialization methods for this component.
    /// </summary>
    public bool Serializable { get; set; }
}

/// <summary>
/// Marks a struct as a tag component (zero-size marker).
/// Tag components have no data and are used purely for filtering queries.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TagComponentAttribute : Attribute;

/// <summary>
/// Specifies a default value for a component field in generated builder methods.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class DefaultValueAttribute : Attribute
{
    public object? Value { get; }

    public DefaultValueAttribute(object? value)
    {
        Value = value;
    }
}

/// <summary>
/// Excludes a field from the generated fluent builder method parameters.
/// The field will use its default value.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BuilderIgnoreAttribute : Attribute;
