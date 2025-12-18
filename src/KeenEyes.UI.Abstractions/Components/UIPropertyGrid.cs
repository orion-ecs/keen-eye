namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a property grid that displays name-value pairs for editing.
/// </summary>
/// <remarks>
/// <para>
/// Property grids are commonly used in editors and inspectors to display
/// and edit object properties. They show a two-column layout with property
/// names on the left and editable values on the right.
/// </para>
/// <para>
/// Properties can be grouped into collapsible categories. Different property
/// types (string, number, boolean, color, etc.) use appropriate editor controls.
/// </para>
/// </remarks>
/// <param name="labelWidthRatio">Ratio of width for property labels (0.0-1.0).</param>
public struct UIPropertyGrid(float labelWidthRatio = 0.4f) : IComponent
{
    /// <summary>
    /// Ratio of the grid width allocated to property labels.
    /// The remaining width is for value editors.
    /// </summary>
    public float LabelWidthRatio = labelWidthRatio;

    /// <summary>
    /// Container entity for property categories and rows.
    /// </summary>
    public Entity ContentContainer = Entity.Null;

    /// <summary>
    /// Number of property categories in this grid.
    /// </summary>
    public int CategoryCount = 0;

    /// <summary>
    /// Number of property rows in this grid.
    /// </summary>
    public int RowCount = 0;
}

/// <summary>
/// Component for a collapsible category within a property grid.
/// </summary>
/// <remarks>
/// Categories group related properties together and can be expanded or collapsed.
/// </remarks>
/// <param name="propertyGrid">The owning property grid entity.</param>
/// <param name="name">The category display name.</param>
public struct UIPropertyCategory(Entity propertyGrid, string name) : IComponent
{
    /// <summary>
    /// The property grid this category belongs to.
    /// </summary>
    public Entity PropertyGrid = propertyGrid;

    /// <summary>
    /// The category display name.
    /// </summary>
    public string Name = name;

    /// <summary>
    /// Whether this category is expanded (showing its properties).
    /// </summary>
    public bool IsExpanded = true;

    /// <summary>
    /// Container entity for this category's property rows.
    /// </summary>
    public Entity ContentContainer = Entity.Null;

    /// <summary>
    /// Index of this category among siblings.
    /// </summary>
    public int Index = 0;
}

/// <summary>
/// Component for a property row within a property grid.
/// </summary>
/// <remarks>
/// Each row represents a single property with a label and an editor control.
/// </remarks>
/// <param name="propertyGrid">The owning property grid entity.</param>
/// <param name="propertyName">The unique property identifier.</param>
public struct UIPropertyRow(Entity propertyGrid, string propertyName) : IComponent
{
    /// <summary>
    /// The property grid this row belongs to.
    /// </summary>
    public Entity PropertyGrid = propertyGrid;

    /// <summary>
    /// The unique property identifier/name.
    /// </summary>
    public string PropertyName = propertyName;

    /// <summary>
    /// The display label for this property.
    /// </summary>
    public string Label = propertyName;

    /// <summary>
    /// The type of property editor to use.
    /// </summary>
    public PropertyType Type = PropertyType.String;

    /// <summary>
    /// The category this property belongs to (or Entity.Null for uncategorized).
    /// </summary>
    public Entity Category = Entity.Null;

    /// <summary>
    /// The entity containing the value editor control.
    /// </summary>
    public Entity EditorEntity = Entity.Null;

    /// <summary>
    /// Index of this row among siblings.
    /// </summary>
    public int Index = 0;

    /// <summary>
    /// Whether this property is read-only.
    /// </summary>
    public bool IsReadOnly = false;
}

/// <summary>
/// Types of property editors available in property grids.
/// </summary>
public enum PropertyType : byte
{
    /// <summary>
    /// Text string editor (single-line text field).
    /// </summary>
    String = 0,

    /// <summary>
    /// Integer number editor.
    /// </summary>
    Int = 1,

    /// <summary>
    /// Floating-point number editor.
    /// </summary>
    Float = 2,

    /// <summary>
    /// Boolean checkbox editor.
    /// </summary>
    Bool = 3,

    /// <summary>
    /// Color picker editor.
    /// </summary>
    Color = 4,

    /// <summary>
    /// Two-component vector editor.
    /// </summary>
    Vector2 = 5,

    /// <summary>
    /// Three-component vector editor.
    /// </summary>
    Vector3 = 6,

    /// <summary>
    /// Four-component vector editor.
    /// </summary>
    Vector4 = 7,

    /// <summary>
    /// Enumeration dropdown editor.
    /// </summary>
    Enum = 8,

    /// <summary>
    /// Multi-line text editor.
    /// </summary>
    MultiLineString = 9,

    /// <summary>
    /// Custom editor (user-provided).
    /// </summary>
    Custom = 10
}

/// <summary>
/// Tag for the header row of a property category.
/// </summary>
public struct UIPropertyCategoryHeaderTag : ITagComponent;

/// <summary>
/// Tag for the expand/collapse arrow in a property category.
/// </summary>
public struct UIPropertyCategoryArrowTag : ITagComponent;
