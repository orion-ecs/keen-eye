using System.Reflection;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Editor.Abstractions.Inspector;

/// <summary>
/// Base class for property drawers that provide custom UI for editing specific field types.
/// </summary>
public abstract class PropertyDrawer
{
    /// <summary>
    /// Gets the type that this drawer handles.
    /// </summary>
    public abstract Type TargetType { get; }

    /// <summary>
    /// Gets the height required to draw this field.
    /// </summary>
    /// <param name="field">The field being drawn.</param>
    /// <param name="value">The current field value.</param>
    /// <returns>The height in pixels.</returns>
    public virtual float GetHeight(FieldInfo field, object? value) => 20f;

    /// <summary>
    /// Creates the UI widgets for editing this field.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="field">The field being drawn.</param>
    /// <param name="value">The current field value.</param>
    /// <returns>The created UI entity.</returns>
    public abstract Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value);

    /// <summary>
    /// Updates the UI with a new value.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    /// <param name="uiEntity">The UI entity created by CreateUI.</param>
    /// <param name="value">The new value to display.</param>
    public virtual void UpdateUI(PropertyDrawerContext context, Entity uiEntity, object? value)
    {
        // Default implementation does nothing - subclasses override for reactive updates
    }
}

/// <summary>
/// Context provided to property drawers for creating UI.
/// </summary>
public sealed class PropertyDrawerContext
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
    /// Gets the font to use for text.
    /// </summary>
    public required FontHandle Font { get; init; }

    /// <summary>
    /// Gets the metadata for the field being drawn.
    /// </summary>
    public required FieldMetadata Metadata { get; init; }

    /// <summary>
    /// Called when the field value is changed by the user.
    /// </summary>
    public Action<object?>? OnValueChanged { get; init; }
}
