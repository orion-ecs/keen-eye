namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the sort direction for a column.
/// </summary>
public enum SortDirection
{
    /// <summary>No sorting applied.</summary>
    None,

    /// <summary>Sort in ascending order (A-Z, 0-9).</summary>
    Ascending,

    /// <summary>Sort in descending order (Z-A, 9-0).</summary>
    Descending
}

/// <summary>
/// Specifies the selection mode for a data grid.
/// </summary>
public enum GridSelectionMode
{
    /// <summary>No selection allowed.</summary>
    None,

    /// <summary>Single row selection only.</summary>
    Single,

    /// <summary>Multiple row selection allowed.</summary>
    Multiple
}

/// <summary>
/// Component for a data grid that displays tabular data with sorting and selection.
/// </summary>
/// <remarks>
/// <para>
/// Data grids display tabular data in rows and columns. They support:
/// <list type="bullet">
/// <item>Column header sorting (click to sort)</item>
/// <item>Row selection (single or multiple)</item>
/// <item>Resizable columns</item>
/// <item>Virtual scrolling for large datasets</item>
/// </list>
/// </para>
/// </remarks>
public struct UIDataGrid() : IComponent
{
    /// <summary>
    /// The selection mode for the grid.
    /// </summary>
    public GridSelectionMode SelectionMode = GridSelectionMode.Single;

    /// <summary>
    /// Whether columns can be resized by dragging.
    /// </summary>
    public bool AllowColumnResize = true;

    /// <summary>
    /// Whether clicking column headers sorts the data.
    /// </summary>
    public bool AllowSorting = true;

    /// <summary>
    /// The row height in pixels.
    /// </summary>
    public float RowHeight = 30f;

    /// <summary>
    /// The header row height in pixels.
    /// </summary>
    public float HeaderHeight = 35f;

    /// <summary>
    /// The currently sorted column entity, or Entity.Null if no sorting.
    /// </summary>
    public Entity SortedColumn = Entity.Null;

    /// <summary>
    /// The current sort direction.
    /// </summary>
    public SortDirection SortDirection = SortDirection.None;

    /// <summary>
    /// The header row entity containing column headers.
    /// </summary>
    public Entity HeaderRow = Entity.Null;

    /// <summary>
    /// The body container entity containing data rows.
    /// </summary>
    public Entity BodyContainer = Entity.Null;

    /// <summary>
    /// Total number of rows in the data source.
    /// </summary>
    public int TotalRowCount = 0;

    /// <summary>
    /// Index of the first visible row (for virtual scrolling).
    /// </summary>
    public int FirstVisibleRow = 0;

    /// <summary>
    /// Number of visible rows in the viewport.
    /// </summary>
    public int VisibleRowCount = 0;

    /// <summary>
    /// The currently selected row entity (for single selection mode).
    /// </summary>
    public Entity SelectedRow = Entity.Null;

    /// <summary>
    /// Whether the grid shows alternating row colors.
    /// </summary>
    public bool AlternatingRowColors = true;
}

/// <summary>
/// Component for a column header in a data grid.
/// </summary>
/// <param name="dataGrid">The owning data grid.</param>
/// <param name="columnIndex">The index of this column.</param>
/// <param name="header">The header text.</param>
public struct UIDataGridColumn(Entity dataGrid, int columnIndex, string header) : IComponent
{
    /// <summary>
    /// The data grid this column belongs to.
    /// </summary>
    public Entity DataGrid = dataGrid;

    /// <summary>
    /// The index of this column (0-based).
    /// </summary>
    public int ColumnIndex = columnIndex;

    /// <summary>
    /// The header text displayed in this column.
    /// </summary>
    public string Header = header;

    /// <summary>
    /// The width of this column in pixels.
    /// </summary>
    public float Width = 100f;

    /// <summary>
    /// The minimum width when resizing.
    /// </summary>
    public float MinWidth = 50f;

    /// <summary>
    /// Whether this column is sortable.
    /// </summary>
    public bool IsSortable = true;

    /// <summary>
    /// Whether this column is resizable.
    /// </summary>
    public bool IsResizable = true;

    /// <summary>
    /// The sort indicator entity (arrow icon).
    /// </summary>
    public Entity SortIndicator = Entity.Null;

    /// <summary>
    /// The resize handle entity at the right edge.
    /// </summary>
    public Entity ResizeHandle = Entity.Null;
}

/// <summary>
/// Component for a row in a data grid.
/// </summary>
/// <param name="dataGrid">The owning data grid.</param>
/// <param name="rowIndex">The index of this row.</param>
public struct UIDataGridRow(Entity dataGrid, int rowIndex) : IComponent
{
    /// <summary>
    /// The data grid this row belongs to.
    /// </summary>
    public Entity DataGrid = dataGrid;

    /// <summary>
    /// The index of this row in the data source (0-based).
    /// </summary>
    public int RowIndex = rowIndex;

    /// <summary>
    /// Whether this row is currently selected.
    /// </summary>
    public bool IsSelected = false;

    /// <summary>
    /// Whether this row is currently hovered.
    /// </summary>
    public bool IsHovered = false;

    /// <summary>
    /// User-defined data associated with this row.
    /// </summary>
    public object? RowData = null;
}

/// <summary>
/// Component for a cell in a data grid.
/// </summary>
/// <param name="dataGrid">The owning data grid.</param>
/// <param name="rowIndex">The row index.</param>
/// <param name="columnIndex">The column index.</param>
public struct UIDataGridCell(Entity dataGrid, int rowIndex, int columnIndex) : IComponent
{
    /// <summary>
    /// The data grid this cell belongs to.
    /// </summary>
    public Entity DataGrid = dataGrid;

    /// <summary>
    /// The row index of this cell.
    /// </summary>
    public int RowIndex = rowIndex;

    /// <summary>
    /// The column index of this cell.
    /// </summary>
    public int ColumnIndex = columnIndex;

    /// <summary>
    /// The cell value as a string.
    /// </summary>
    public string Value = string.Empty;

    /// <summary>
    /// The row entity this cell belongs to.
    /// </summary>
    public Entity Row = Entity.Null;

    /// <summary>
    /// The column entity this cell belongs to.
    /// </summary>
    public Entity Column = Entity.Null;
}

/// <summary>
/// Component for a column resize handle.
/// </summary>
/// <param name="column">The column this handle resizes.</param>
public struct UIDataGridResizeHandle(Entity column) : IComponent
{
    /// <summary>
    /// The column entity this handle resizes.
    /// </summary>
    public Entity Column = column;

    /// <summary>
    /// Whether the handle is currently being dragged.
    /// </summary>
    public bool IsDragging = false;

    /// <summary>
    /// The X position when drag started.
    /// </summary>
    public float DragStartX = 0f;

    /// <summary>
    /// The column width when drag started.
    /// </summary>
    public float StartWidth = 0f;
}

/// <summary>
/// Tag component for the currently selected row(s) in a data grid.
/// </summary>
public struct UIDataGridRowSelectedTag : ITagComponent;

/// <summary>
/// Event raised when the grid sort changes.
/// </summary>
/// <param name="DataGrid">The data grid entity.</param>
/// <param name="Column">The sorted column entity.</param>
/// <param name="ColumnIndex">The column index.</param>
/// <param name="Direction">The sort direction.</param>
public readonly record struct UIGridSortEvent(
    Entity DataGrid,
    Entity Column,
    int ColumnIndex,
    SortDirection Direction);

/// <summary>
/// Event raised when a row is selected or deselected.
/// </summary>
/// <param name="DataGrid">The data grid entity.</param>
/// <param name="Row">The row entity.</param>
/// <param name="RowIndex">The row index.</param>
/// <param name="IsSelected">Whether the row is now selected.</param>
public readonly record struct UIGridRowSelectedEvent(
    Entity DataGrid,
    Entity Row,
    int RowIndex,
    bool IsSelected);

/// <summary>
/// Event raised when a row is double-clicked.
/// </summary>
/// <param name="DataGrid">The data grid entity.</param>
/// <param name="Row">The row entity.</param>
/// <param name="RowIndex">The row index.</param>
public readonly record struct UIGridRowDoubleClickEvent(
    Entity DataGrid,
    Entity Row,
    int RowIndex);

/// <summary>
/// Event raised when a column is resized.
/// </summary>
/// <param name="DataGrid">The data grid entity.</param>
/// <param name="Column">The column entity.</param>
/// <param name="ColumnIndex">The column index.</param>
/// <param name="OldWidth">The previous width.</param>
/// <param name="NewWidth">The new width.</param>
public readonly record struct UIGridColumnResizedEvent(
    Entity DataGrid,
    Entity Column,
    int ColumnIndex,
    float OldWidth,
    float NewWidth);
