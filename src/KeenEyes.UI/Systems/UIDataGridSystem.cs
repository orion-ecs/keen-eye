using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles data grid interaction including sorting, selection, and column resizing.
/// </summary>
/// <remarks>
/// <para>
/// This system manages:
/// <list type="bullet">
/// <item>Column header click for sorting</item>
/// <item>Row click for selection</item>
/// <item>Column resize handle dragging</item>
/// <item>Row hover states</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIDataGridSystem : SystemBase
{
    private EventSubscription? clickSubscription;
    private EventSubscription? dragStartSubscription;
    private EventSubscription? dragSubscription;
    private EventSubscription? dragEndSubscription;
    private EventSubscription? pointerEnterSubscription;
    private EventSubscription? pointerExitSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to click events for header and row clicks
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);

        // Subscribe to drag events for column resizing
        dragStartSubscription = World.Subscribe<UIDragStartEvent>(OnDragStart);
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);
        dragEndSubscription = World.Subscribe<UIDragEndEvent>(OnDragEnd);

        // Subscribe to hover events for row highlighting
        pointerEnterSubscription = World.Subscribe<UIPointerEnterEvent>(OnPointerEnter);
        pointerExitSubscription = World.Subscribe<UIPointerExitEvent>(OnPointerExit);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            clickSubscription?.Dispose();
            dragStartSubscription?.Dispose();
            dragSubscription?.Dispose();
            dragEndSubscription?.Dispose();
            pointerEnterSubscription?.Dispose();
            pointerExitSubscription?.Dispose();
            clickSubscription = null;
            dragStartSubscription = null;
            dragSubscription = null;
            dragEndSubscription = null;
            pointerEnterSubscription = null;
            pointerExitSubscription = null;
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Most work is event-driven
    }

    private void OnClick(UIClickEvent evt)
    {
        // Handle column header click for sorting
        if (World.Has<UIDataGridColumn>(evt.Element))
        {
            HandleColumnClick(evt.Element);
            return;
        }

        // Handle row click for selection
        if (World.Has<UIDataGridRow>(evt.Element))
        {
            HandleRowClick(evt.Element, false, false);
            return;
        }

        // Handle cell click (select row)
        if (World.Has<UIDataGridCell>(evt.Element))
        {
            ref readonly var cell = ref World.Get<UIDataGridCell>(evt.Element);
            if (World.IsAlive(cell.Row))
            {
                HandleRowClick(cell.Row, false, false);
            }
        }
    }

    private void HandleColumnClick(Entity columnEntity)
    {
        ref readonly var column = ref World.Get<UIDataGridColumn>(columnEntity);
        var gridEntity = column.DataGrid;

        if (!World.IsAlive(gridEntity) || !World.Has<UIDataGrid>(gridEntity))
        {
            return;
        }

        if (!column.IsSortable)
        {
            return;
        }

        ref var grid = ref World.Get<UIDataGrid>(gridEntity);

        if (!grid.AllowSorting)
        {
            return;
        }

        // Determine new sort direction
        SortDirection newDirection;
        if (grid.SortedColumn == columnEntity)
        {
            // Toggle between ascending and descending
            newDirection = grid.SortDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }
        else
        {
            // New column, start with ascending
            newDirection = SortDirection.Ascending;
        }

        // Update previous sorted column indicator
        if (World.IsAlive(grid.SortedColumn) && grid.SortedColumn != columnEntity)
        {
            ClearSortIndicator(grid.SortedColumn);
        }

        // Update grid state
        grid.SortedColumn = columnEntity;
        grid.SortDirection = newDirection;

        // Update sort indicator visual
        UpdateSortIndicator(columnEntity, newDirection);

        // Fire sort event
        World.Send(new UIGridSortEvent(gridEntity, columnEntity, column.ColumnIndex, newDirection));
    }

    private void ClearSortIndicator(Entity columnEntity)
    {
        if (!World.Has<UIDataGridColumn>(columnEntity))
        {
            return;
        }

        ref readonly var column = ref World.Get<UIDataGridColumn>(columnEntity);
        if (World.IsAlive(column.SortIndicator) && World.Has<UIText>(column.SortIndicator))
        {
            ref var text = ref World.Get<UIText>(column.SortIndicator);
            text.Content = string.Empty;
        }
    }

    private void UpdateSortIndicator(Entity columnEntity, SortDirection direction)
    {
        if (!World.Has<UIDataGridColumn>(columnEntity))
        {
            return;
        }

        ref readonly var column = ref World.Get<UIDataGridColumn>(columnEntity);
        if (World.IsAlive(column.SortIndicator) && World.Has<UIText>(column.SortIndicator))
        {
            ref var text = ref World.Get<UIText>(column.SortIndicator);
            text.Content = direction switch
            {
                SortDirection.Ascending => "\u25B2", // ▲
                SortDirection.Descending => "\u25BC", // ▼
                _ => string.Empty
            };
        }
    }

    private void HandleRowClick(Entity rowEntity, bool isShiftHeld, bool isCtrlHeld)
    {
        ref var row = ref World.Get<UIDataGridRow>(rowEntity);
        var gridEntity = row.DataGrid;

        if (!World.IsAlive(gridEntity) || !World.Has<UIDataGrid>(gridEntity))
        {
            return;
        }

        ref var grid = ref World.Get<UIDataGrid>(gridEntity);

        if (grid.SelectionMode == GridSelectionMode.None)
        {
            return;
        }

        if (grid.SelectionMode == GridSelectionMode.Single)
        {
            // Deselect previous row
            if (World.IsAlive(grid.SelectedRow) && grid.SelectedRow != rowEntity)
            {
                DeselectRow(grid.SelectedRow, gridEntity);
            }

            // Select new row
            SelectRow(rowEntity, gridEntity);
            grid.SelectedRow = rowEntity;
        }
        else if (grid.SelectionMode == GridSelectionMode.Multiple)
        {
            if (isCtrlHeld)
            {
                // Toggle selection
                if (row.IsSelected)
                {
                    DeselectRow(rowEntity, gridEntity);
                }
                else
                {
                    SelectRow(rowEntity, gridEntity);
                }
            }
            else
            {
                // Clear all and select this row
                ClearAllRowSelections(gridEntity);
                SelectRow(rowEntity, gridEntity);
                grid.SelectedRow = rowEntity;
            }
        }
    }

    private void SelectRow(Entity rowEntity, Entity gridEntity)
    {
        if (!World.Has<UIDataGridRow>(rowEntity))
        {
            return;
        }

        ref var row = ref World.Get<UIDataGridRow>(rowEntity);

        if (row.IsSelected)
        {
            return;
        }

        row.IsSelected = true;

        // Add tag
        if (!World.Has<UIDataGridRowSelectedTag>(rowEntity))
        {
            World.Add(rowEntity, new UIDataGridRowSelectedTag());
        }

        // Update visual
        UpdateRowVisual(rowEntity, ref row);

        // Fire event
        World.Send(new UIGridRowSelectedEvent(gridEntity, rowEntity, row.RowIndex, true));
    }

    private void DeselectRow(Entity rowEntity, Entity gridEntity)
    {
        if (!World.Has<UIDataGridRow>(rowEntity))
        {
            return;
        }

        ref var row = ref World.Get<UIDataGridRow>(rowEntity);

        if (!row.IsSelected)
        {
            return;
        }

        row.IsSelected = false;

        // Remove tag
        if (World.Has<UIDataGridRowSelectedTag>(rowEntity))
        {
            World.Remove<UIDataGridRowSelectedTag>(rowEntity);
        }

        // Update visual
        UpdateRowVisual(rowEntity, ref row);

        // Fire event
        World.Send(new UIGridRowSelectedEvent(gridEntity, rowEntity, row.RowIndex, false));
    }

    private void ClearAllRowSelections(Entity gridEntity)
    {
        foreach (var rowEntity in World.Query<UIDataGridRow>())
        {
            ref readonly var row = ref World.Get<UIDataGridRow>(rowEntity);
            if (row.DataGrid == gridEntity && row.IsSelected)
            {
                DeselectRow(rowEntity, gridEntity);
            }
        }
    }

    private void UpdateRowVisual(Entity rowEntity, ref UIDataGridRow row)
    {
        if (!World.Has<UIStyle>(rowEntity))
        {
            return;
        }

        ref var style = ref World.Get<UIStyle>(rowEntity);

        if (row.IsSelected)
        {
            style.BackgroundColor = new System.Numerics.Vector4(0.3f, 0.5f, 0.9f, 1f);
        }
        else if (row.IsHovered)
        {
            style.BackgroundColor = new System.Numerics.Vector4(0.35f, 0.35f, 0.35f, 1f);
        }
        else
        {
            // Alternating row colors
            if (World.Has<UIDataGrid>(row.DataGrid))
            {
                ref readonly var grid = ref World.Get<UIDataGrid>(row.DataGrid);
                if (grid.AlternatingRowColors && row.RowIndex % 2 == 1)
                {
                    style.BackgroundColor = new System.Numerics.Vector4(0.22f, 0.22f, 0.22f, 1f);
                }
                else
                {
                    style.BackgroundColor = new System.Numerics.Vector4(0.25f, 0.25f, 0.25f, 1f);
                }
            }
        }
    }

    private void OnPointerEnter(UIPointerEnterEvent evt)
    {
        // Handle row hover enter
        if (World.Has<UIDataGridRow>(evt.Element))
        {
            ref var row = ref World.Get<UIDataGridRow>(evt.Element);
            row.IsHovered = true;
            UpdateRowVisual(evt.Element, ref row);
        }
    }

    private void OnPointerExit(UIPointerExitEvent evt)
    {
        // Handle row hover exit
        if (World.Has<UIDataGridRow>(evt.Element))
        {
            ref var row = ref World.Get<UIDataGridRow>(evt.Element);
            row.IsHovered = false;
            UpdateRowVisual(evt.Element, ref row);
        }
    }

    private void OnDragStart(UIDragStartEvent evt)
    {
        // Handle column resize handle drag start
        if (World.Has<UIDataGridResizeHandle>(evt.Element))
        {
            ref var handle = ref World.Get<UIDataGridResizeHandle>(evt.Element);
            handle.IsDragging = true;
            handle.DragStartX = evt.StartPosition.X;

            if (World.Has<UIDataGridColumn>(handle.Column))
            {
                ref readonly var column = ref World.Get<UIDataGridColumn>(handle.Column);
                handle.StartWidth = column.Width;
            }
        }
    }

    private void OnDrag(UIDragEvent evt)
    {
        // Handle column resize handle drag
        if (World.Has<UIDataGridResizeHandle>(evt.Element))
        {
            ref var handle = ref World.Get<UIDataGridResizeHandle>(evt.Element);

            if (!handle.IsDragging)
            {
                return;
            }

            if (!World.Has<UIDataGridColumn>(handle.Column))
            {
                return;
            }

            ref var column = ref World.Get<UIDataGridColumn>(handle.Column);

            if (!column.IsResizable)
            {
                return;
            }

            float deltaX = evt.Position.X - handle.DragStartX;
            float newWidth = handle.StartWidth + deltaX;

            // Clamp to minimum width
            if (newWidth < column.MinWidth)
            {
                newWidth = column.MinWidth;
            }

            column.Width = newWidth;

            // Update column visual width
            if (World.Has<UIRect>(handle.Column))
            {
                ref var rect = ref World.Get<UIRect>(handle.Column);
                rect.Size = new System.Numerics.Vector2(newWidth, rect.Size.Y);
            }
        }
    }

    private void OnDragEnd(UIDragEndEvent evt)
    {
        // Handle column resize handle drag end
        if (World.Has<UIDataGridResizeHandle>(evt.Element))
        {
            ref var handle = ref World.Get<UIDataGridResizeHandle>(evt.Element);

            if (!handle.IsDragging)
            {
                return;
            }

            handle.IsDragging = false;

            if (World.Has<UIDataGridColumn>(handle.Column))
            {
                ref readonly var column = ref World.Get<UIDataGridColumn>(handle.Column);
                float oldWidth = handle.StartWidth;
                float newWidth = column.Width;

                if (System.Math.Abs(oldWidth - newWidth) > 0.1f)
                {
                    World.Send(new UIGridColumnResizedEvent(
                        column.DataGrid,
                        handle.Column,
                        column.ColumnIndex,
                        oldWidth,
                        newWidth));
                }
            }
        }
    }

    /// <summary>
    /// Selects a row by index.
    /// </summary>
    /// <param name="gridEntity">The data grid entity.</param>
    /// <param name="rowIndex">The row index to select.</param>
    public void SelectRowByIndex(Entity gridEntity, int rowIndex)
    {
        foreach (var rowEntity in World.Query<UIDataGridRow>())
        {
            ref readonly var row = ref World.Get<UIDataGridRow>(rowEntity);
            if (row.DataGrid == gridEntity && row.RowIndex == rowIndex)
            {
                HandleRowClick(rowEntity, false, false);
                return;
            }
        }
    }

    /// <summary>
    /// Clears all row selections in a data grid.
    /// </summary>
    /// <param name="gridEntity">The data grid entity.</param>
    public void ClearSelection(Entity gridEntity)
    {
        if (!World.IsAlive(gridEntity) || !World.Has<UIDataGrid>(gridEntity))
        {
            return;
        }

        ClearAllRowSelections(gridEntity);

        ref var grid = ref World.Get<UIDataGrid>(gridEntity);
        grid.SelectedRow = Entity.Null;
    }

    /// <summary>
    /// Sorts a data grid by column index.
    /// </summary>
    /// <param name="gridEntity">The data grid entity.</param>
    /// <param name="columnIndex">The column index to sort by.</param>
    /// <param name="direction">The sort direction.</param>
    public void SortByColumn(Entity gridEntity, int columnIndex, SortDirection direction)
    {
        foreach (var columnEntity in World.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref World.Get<UIDataGridColumn>(columnEntity);
            if (column.DataGrid == gridEntity && column.ColumnIndex == columnIndex)
            {
                if (!World.Has<UIDataGrid>(gridEntity))
                {
                    return;
                }

                ref var grid = ref World.Get<UIDataGrid>(gridEntity);

                // Clear previous sort indicator
                if (World.IsAlive(grid.SortedColumn) && grid.SortedColumn != columnEntity)
                {
                    ClearSortIndicator(grid.SortedColumn);
                }

                grid.SortedColumn = columnEntity;
                grid.SortDirection = direction;

                UpdateSortIndicator(columnEntity, direction);
                World.Send(new UIGridSortEvent(gridEntity, columnEntity, columnIndex, direction));
                return;
            }
        }
    }

    /// <summary>
    /// Gets the currently selected row indices.
    /// </summary>
    /// <param name="gridEntity">The data grid entity.</param>
    /// <returns>Array of selected row indices.</returns>
    public int[] GetSelectedRowIndices(Entity gridEntity)
    {
        var indices = new List<int>();

        foreach (var rowEntity in World.Query<UIDataGridRow>())
        {
            ref readonly var row = ref World.Get<UIDataGridRow>(rowEntity);
            if (row.DataGrid == gridEntity && row.IsSelected)
            {
                indices.Add(row.RowIndex);
            }
        }

        return [.. indices];
    }

    /// <summary>
    /// Sets the column width.
    /// </summary>
    /// <param name="gridEntity">The data grid entity.</param>
    /// <param name="columnIndex">The column index.</param>
    /// <param name="width">The new width.</param>
    public void SetColumnWidth(Entity gridEntity, int columnIndex, float width)
    {
        foreach (var columnEntity in World.Query<UIDataGridColumn>())
        {
            ref var column = ref World.Get<UIDataGridColumn>(columnEntity);
            if (column.DataGrid == gridEntity && column.ColumnIndex == columnIndex)
            {
                float oldWidth = column.Width;
                column.Width = Math.Max(width, column.MinWidth);

                if (World.Has<UIRect>(columnEntity))
                {
                    ref var rect = ref World.Get<UIRect>(columnEntity);
                    rect.Size = new System.Numerics.Vector2(column.Width, rect.Size.Y);
                }

                if (Math.Abs(oldWidth - column.Width) > 0.1f)
                {
                    World.Send(new UIGridColumnResizedEvent(
                        gridEntity,
                        columnEntity,
                        columnIndex,
                        oldWidth,
                        column.Width));
                }
                return;
            }
        }
    }
}
