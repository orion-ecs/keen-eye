using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

using KeenEyes.Editor.Abstractions.Inspector;

namespace KeenEyes.Editor.Common.Inspector;

/// <summary>
/// Provides reflection-based inspection of component types for the editor.
/// This class is editor-only and uses reflection to inspect components dynamically.
/// </summary>
public static partial class ComponentIntrospector
{
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<MemberInfo, FieldMetadata> MetadataCache = new();

    /// <summary>
    /// Gets all editable public instance fields of a component type.
    /// Fields marked with <see cref="HideInInspectorAttribute"/> are excluded.
    /// </summary>
    /// <param name="componentType">The component type to inspect.</param>
    /// <returns>The editable fields.</returns>
    public static IEnumerable<FieldInfo> GetEditableFields(Type componentType)
    {
        return FieldCache.GetOrAdd(componentType, static type =>
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsDefined(typeof(HideInInspectorAttribute), false))
                .ToArray();
        });
    }

    /// <summary>
    /// Gets all editable public instance properties of a component type.
    /// Properties must have a getter and setter, and not be marked with <see cref="HideInInspectorAttribute"/>.
    /// </summary>
    /// <param name="componentType">The component type to inspect.</param>
    /// <returns>The editable properties.</returns>
    public static IEnumerable<PropertyInfo> GetEditableProperties(Type componentType)
    {
        return PropertyCache.GetOrAdd(componentType, static type =>
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !p.IsDefined(typeof(HideInInspectorAttribute), false))
                .ToArray();
        });
    }

    /// <summary>
    /// Gets metadata for a field, including display name, tooltip, range, etc.
    /// </summary>
    /// <param name="field">The field to get metadata for.</param>
    /// <returns>The field metadata.</returns>
    public static FieldMetadata GetFieldMetadata(FieldInfo field)
    {
        return GetMemberMetadata(field);
    }

    /// <summary>
    /// Gets metadata for a property, including display name, tooltip, range, etc.
    /// </summary>
    /// <param name="property">The property to get metadata for.</param>
    /// <returns>The field metadata.</returns>
    public static FieldMetadata GetPropertyMetadata(PropertyInfo property)
    {
        return GetMemberMetadata(property);
    }

    private static FieldMetadata GetMemberMetadata(MemberInfo member)
    {
        return MetadataCache.GetOrAdd(member, static m =>
        {
            var displayName = m.GetCustomAttribute<DisplayNameAttribute>()?.Name
                ?? FormatFieldName(m.Name);

            var tooltip = m.GetCustomAttribute<TooltipAttribute>()?.Text;
            var header = m.GetCustomAttribute<HeaderAttribute>()?.Text;
            var space = m.GetCustomAttribute<SpaceAttribute>();
            var range = m.GetCustomAttribute<RangeAttribute>();
            var isReadOnly = m.IsDefined(typeof(ReadOnlyInInspectorAttribute), false);
            var foldoutGroup = m.GetCustomAttribute<FoldoutGroupAttribute>()?.GroupName;
            var textArea = m.GetCustomAttribute<TextAreaAttribute>();

            return new FieldMetadata
            {
                DisplayName = displayName,
                Tooltip = tooltip,
                Header = header,
                SpaceHeight = space?.Height,
                Range = range is not null ? (range.Min, range.Max) : null,
                IsReadOnly = isReadOnly,
                FoldoutGroup = foldoutGroup,
                TextArea = textArea is not null ? (textArea.MinLines, textArea.MaxLines) : null
            };
        });
    }

    /// <summary>
    /// Gets the value of a field from a component instance.
    /// </summary>
    /// <param name="component">The component instance (boxed if struct).</param>
    /// <param name="field">The field to get.</param>
    /// <returns>The field value.</returns>
    public static object? GetFieldValue(object component, FieldInfo field)
    {
        return field.GetValue(component);
    }

    /// <summary>
    /// Gets the value of a property from a component instance.
    /// </summary>
    /// <param name="component">The component instance (boxed if struct).</param>
    /// <param name="property">The property to get.</param>
    /// <returns>The property value.</returns>
    public static object? GetPropertyValue(object component, PropertyInfo property)
    {
        return property.GetValue(component);
    }

    /// <summary>
    /// Sets the value of a field on a component instance.
    /// For struct components, the modified instance must be written back to the entity.
    /// </summary>
    /// <param name="component">The component instance (boxed if struct).</param>
    /// <param name="field">The field to set.</param>
    /// <param name="value">The value to set.</param>
    public static void SetFieldValue(ref object component, FieldInfo field, object? value)
    {
        field.SetValue(component, value);
    }

    /// <summary>
    /// Sets the value of a property on a component instance.
    /// For struct components, the modified instance must be written back to the entity.
    /// </summary>
    /// <param name="component">The component instance (boxed if struct).</param>
    /// <param name="property">The property to set.</param>
    /// <param name="value">The value to set.</param>
    public static void SetPropertyValue(ref object component, PropertyInfo property, object? value)
    {
        property.SetValue(component, value);
    }

    /// <summary>
    /// Determines if a type is a primitive or simple type that can be directly edited.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type can be directly edited.</returns>
    public static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type.IsEnum
            || IsVectorType(type);
    }

    /// <summary>
    /// Determines if a type is a vector type (Vector2, Vector3, Vector4, Quaternion).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a vector type.</returns>
    public static bool IsVectorType(Type type)
    {
        return type == typeof(System.Numerics.Vector2)
            || type == typeof(System.Numerics.Vector3)
            || type == typeof(System.Numerics.Vector4)
            || type == typeof(System.Numerics.Quaternion);
    }

    /// <summary>
    /// Determines if a type is a color type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a color type.</returns>
    public static bool IsColorType(Type type)
    {
        // Check for Vector4 (common color representation) or any type named "Color"
        return type == typeof(System.Numerics.Vector4)
            || type.Name.Contains("Color", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a type is a collection type (array or generic collection).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection.</returns>
    public static bool IsCollectionType(Type type)
    {
        return type.IsArray
            || (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type));
    }

    /// <summary>
    /// Gets the element type of a collection.
    /// </summary>
    /// <param name="type">The collection type.</param>
    /// <returns>The element type, or null if not a collection.</returns>
    public static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if a type is an Entity reference.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is an Entity.</returns>
    public static bool IsEntityType(Type type)
    {
        return type == typeof(Entity);
    }

    /// <summary>
    /// Formats a field name for display (e.g., "maxHealth" -> "Max Health").
    /// </summary>
    /// <param name="fieldName">The field name to format.</param>
    /// <returns>The formatted display name.</returns>
    public static string FormatFieldName(string fieldName)
    {
        // Remove leading underscore if present
        if (fieldName.StartsWith('_'))
        {
            fieldName = fieldName[1..];
        }

        // Insert spaces before capital letters (but not at the start)
        var result = InsertSpacesBeforeCapitals().Replace(fieldName, " $1").TrimStart();

        // Capitalize first letter
        if (result.Length == 0)
        {
            return result;
        }

        return char.ToUpper(result[0]) + result[1..];
    }

    /// <summary>
    /// Clears all cached reflection data.
    /// </summary>
    public static void ClearCache()
    {
        FieldCache.Clear();
        PropertyCache.Clear();
        MetadataCache.Clear();
    }

    [GeneratedRegex(@"(\p{Lu})")]
    private static partial Regex InsertSpacesBeforeCapitals();
}
