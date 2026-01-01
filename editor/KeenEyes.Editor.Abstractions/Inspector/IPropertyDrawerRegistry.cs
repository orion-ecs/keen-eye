namespace KeenEyes.Editor.Abstractions.Inspector;

/// <summary>
/// Registry for property drawers that handle specific field types.
/// Plugin authors can register custom drawers through this interface.
/// </summary>
public interface IPropertyDrawerRegistry
{
    /// <summary>
    /// Registers a property drawer for a specific type.
    /// </summary>
    /// <typeparam name="T">The type to register the drawer for.</typeparam>
    /// <param name="drawer">The drawer instance.</param>
    void Register<T>(PropertyDrawer drawer);

    /// <summary>
    /// Registers a property drawer for a specific type.
    /// </summary>
    /// <param name="type">The type to register the drawer for.</param>
    /// <param name="drawer">The drawer instance.</param>
    void Register(Type type, PropertyDrawer drawer);

    /// <summary>
    /// Gets the property drawer for a specific type.
    /// </summary>
    /// <param name="type">The type to get the drawer for.</param>
    /// <returns>The drawer, or the default drawer if none is registered.</returns>
    PropertyDrawer GetDrawer(Type type);

    /// <summary>
    /// Gets the property drawer for a specific type.
    /// </summary>
    /// <typeparam name="T">The type to get the drawer for.</typeparam>
    /// <returns>The drawer, or the default drawer if none is registered.</returns>
    PropertyDrawer GetDrawer<T>();

    /// <summary>
    /// Checks if a custom drawer is registered for a type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if a custom drawer exists.</returns>
    bool HasDrawer(Type type);
}
