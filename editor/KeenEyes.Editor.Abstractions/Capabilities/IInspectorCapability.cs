using KeenEyes.Editor.Abstractions.Inspector;

namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for customizing the inspector panel.
/// Allows plugins to register custom property drawers, component inspectors, and actions.
/// </summary>
public interface IInspectorCapability : IEditorCapability
{
    /// <summary>
    /// Registers a property drawer for a specific field type.
    /// </summary>
    /// <param name="fieldType">The field type to handle.</param>
    /// <param name="drawer">The drawer instance.</param>
    void RegisterPropertyDrawer(Type fieldType, PropertyDrawer drawer);

    /// <summary>
    /// Registers a property drawer for a specific field type.
    /// </summary>
    /// <typeparam name="T">The field type to handle.</typeparam>
    /// <param name="drawer">The drawer instance.</param>
    void RegisterPropertyDrawer<T>(PropertyDrawer drawer);

    /// <summary>
    /// Registers a property drawer for fields decorated with a specific attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to match.</typeparam>
    /// <param name="drawer">The drawer instance.</param>
    void RegisterDrawerForAttribute<TAttribute>(PropertyDrawer drawer) where TAttribute : Attribute;

    /// <summary>
    /// Registers a custom inspector for a specific component type.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="inspector">The custom inspector.</param>
    void RegisterComponentInspector<TComponent>(IComponentInspector inspector) where TComponent : struct, IComponent;

    /// <summary>
    /// Registers context menu actions for a specific component type.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="provider">The action provider.</param>
    void RegisterComponentActions<TComponent>(IComponentActionProvider provider) where TComponent : struct, IComponent;

    /// <summary>
    /// Gets the property drawer registry for direct access.
    /// </summary>
    IPropertyDrawerRegistry PropertyDrawers { get; }
}

/// <summary>
/// Interface for custom component inspectors that replace the default component display.
/// </summary>
public interface IComponentInspector
{
    /// <summary>
    /// Creates the UI for inspecting a component instance.
    /// </summary>
    /// <param name="context">The inspector context.</param>
    /// <param name="componentValue">The current component value.</param>
    /// <returns>The root entity of the inspector UI.</returns>
    Entity CreateUI(ComponentInspectorContext context, object componentValue);

    /// <summary>
    /// Updates the inspector UI with new component values.
    /// </summary>
    /// <param name="context">The inspector context.</param>
    /// <param name="rootEntity">The root entity returned by CreateUI.</param>
    /// <param name="componentValue">The new component value.</param>
    void UpdateUI(ComponentInspectorContext context, Entity rootEntity, object componentValue);
}

/// <summary>
/// Context provided to component inspectors for creating UI.
/// </summary>
public sealed class ComponentInspectorContext
{
    /// <summary>
    /// Gets the editor world for creating UI entities.
    /// </summary>
    public required IWorld EditorWorld { get; init; }

    /// <summary>
    /// Gets the parent entity to add UI widgets to.
    /// </summary>
    public required Entity Parent { get; init; }

    /// <summary>
    /// Gets the entity being inspected.
    /// </summary>
    public required Entity InspectedEntity { get; init; }

    /// <summary>
    /// Gets the component type being inspected.
    /// </summary>
    public required Type ComponentType { get; init; }

    /// <summary>
    /// Called when the component value changes.
    /// </summary>
    public Action<object>? OnValueChanged { get; init; }
}

/// <summary>
/// Interface for providing context menu actions for components in the inspector.
/// </summary>
public interface IComponentActionProvider
{
    /// <summary>
    /// Gets the context menu actions for a component.
    /// </summary>
    /// <param name="context">The action context.</param>
    /// <returns>The available actions.</returns>
    IEnumerable<ComponentAction> GetActions(ComponentActionContext context);
}

/// <summary>
/// Context for component actions.
/// </summary>
public sealed class ComponentActionContext
{
    /// <summary>
    /// Gets the entity that has the component.
    /// </summary>
    public required Entity Entity { get; init; }

    /// <summary>
    /// Gets the current component value.
    /// </summary>
    public required object ComponentValue { get; init; }

    /// <summary>
    /// Gets the editor context for accessing editor services.
    /// </summary>
    public required IEditorContext EditorContext { get; init; }
}

/// <summary>
/// Represents an action that can be performed on a component.
/// </summary>
public sealed class ComponentAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the action to execute.
    /// </summary>
    public required Action Execute { get; init; }

    /// <summary>
    /// Gets whether the action is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the optional icon for the action.
    /// </summary>
    public string? Icon { get; init; }
}
