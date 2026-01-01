using System.Collections.Concurrent;

using KeenEyes.Editor.Abstractions.Inspector;

namespace KeenEyes.Editor.Inspector.Drawers;

/// <summary>
/// Registry for property drawers that handle specific field types.
/// </summary>
public sealed class PropertyDrawerRegistry : IPropertyDrawerRegistry
{
    private readonly ConcurrentDictionary<Type, PropertyDrawer> _drawers = new();
    private readonly DefaultPropertyDrawer _defaultDrawer = new();

    /// <summary>
    /// Gets the singleton instance of the registry.
    /// </summary>
    public static PropertyDrawerRegistry Instance { get; } = new();

    private PropertyDrawerRegistry()
    {
        // Register built-in drawers
        RegisterBuiltInDrawers();
    }

    /// <summary>
    /// Registers a property drawer for a specific type.
    /// </summary>
    /// <typeparam name="T">The type to register the drawer for.</typeparam>
    /// <param name="drawer">The drawer instance.</param>
    public void Register<T>(PropertyDrawer drawer)
    {
        Register(typeof(T), drawer);
    }

    /// <summary>
    /// Registers a property drawer for a specific type.
    /// </summary>
    /// <param name="type">The type to register the drawer for.</param>
    /// <param name="drawer">The drawer instance.</param>
    public void Register(Type type, PropertyDrawer drawer)
    {
        _drawers[type] = drawer;
    }

    /// <summary>
    /// Gets the property drawer for a specific type.
    /// </summary>
    /// <param name="type">The type to get the drawer for.</param>
    /// <returns>The drawer, or the default drawer if none is registered.</returns>
    public PropertyDrawer GetDrawer(Type type)
    {
        // Check for exact type match
        if (_drawers.TryGetValue(type, out var drawer))
        {
            return drawer;
        }

        // Check for enum type
        if (type.IsEnum)
        {
            return _drawers.TryGetValue(typeof(Enum), out var enumDrawer)
                ? enumDrawer
                : _defaultDrawer;
        }

        // Check for nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null && _drawers.TryGetValue(underlyingType, out var nullableDrawer))
        {
            return nullableDrawer;
        }

        // Return default drawer
        return _defaultDrawer;
    }

    /// <summary>
    /// Gets the property drawer for a specific type.
    /// </summary>
    /// <typeparam name="T">The type to get the drawer for.</typeparam>
    /// <returns>The drawer, or the default drawer if none is registered.</returns>
    public PropertyDrawer GetDrawer<T>()
    {
        return GetDrawer(typeof(T));
    }

    /// <summary>
    /// Checks if a custom drawer is registered for a type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if a custom drawer exists.</returns>
    public bool HasDrawer(Type type)
    {
        return _drawers.ContainsKey(type);
    }

    /// <summary>
    /// Clears all registered drawers and re-registers built-in ones.
    /// </summary>
    public void Reset()
    {
        _drawers.Clear();
        RegisterBuiltInDrawers();
    }

    private void RegisterBuiltInDrawers()
    {
        // Primitive types
        Register<int>(new IntDrawer());
        Register<float>(new FloatDrawer());
        Register<double>(new DoubleDrawer());
        Register<bool>(new BoolDrawer());
        Register<string>(new StringDrawer());

        // Vector types
        Register<System.Numerics.Vector2>(new Vector2Drawer());
        Register<System.Numerics.Vector3>(new Vector3Drawer());
        Register<System.Numerics.Vector4>(new Vector4Drawer());
        Register<System.Numerics.Quaternion>(new QuaternionDrawer());

        // Special types
        Register(typeof(Enum), new EnumDrawer());
        Register<Entity>(new EntityDrawer());
    }
}
