using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

public static partial class WidgetFactory
{
    #region DataGrid

    /// <summary>
    /// Creates a data grid with the specified configuration.
    /// </summary>
    /// <param name="world">The world to create the data grid in.</param>
    /// <param name="config">The data grid configuration.</param>
    /// <returns>The data grid entity.</returns>
    public static Entity CreateDataGrid(IWorld world, DataGridConfig config)
    {
        var size = config.Size ?? new Vector2(500f, 300f);

        // Calculate total width from columns
        float totalWidth = 0f;
        foreach (var col in config.Columns)
        {
            totalWidth += col.Width;
        }

        if (totalWidth < size.X)
        {
            totalWidth = size.X;
        }

        // Create main container
        var grid = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0f, 1f),
                AnchorMax = new Vector2(0f, 1f),
                Pivot = new Vector2(0f, 1f),
                Size = size
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                BorderWidth = 1f
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start
            })
            .With(new UIDataGrid
            {
                SelectionMode = config.SelectionMode,
                AllowColumnResize = config.AllowColumnResize,
                AllowSorting = config.AllowSorting,
                RowHeight = config.RowHeight,
                HeaderHeight = config.HeaderHeight,
                AlternatingRowColors = config.AlternatingRowColors
            })
            .WithTag<UIClipChildrenTag>()
            .Build();

        // Create header row
        var headerRow = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0f, 1f),
                AnchorMax = new Vector2(1f, 1f),
                Pivot = new Vector2(0f, 1f),
                Size = new Vector2(totalWidth, config.HeaderHeight)
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                BorderWidth = 1f
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start
            })
            .Build();

        world.SetParent(headerRow, grid);

        // Create column headers
        float xOffset = 0f;
        for (int i = 0; i < config.Columns.Length; i++)
        {
            var colDef = config.Columns[i];
            CreateColumnHeader(world, grid, headerRow, i, colDef, xOffset, config.HeaderHeight);
            xOffset += colDef.Width;
        }

        // Create body container (scrollable area for rows)
        var bodyContainer = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 1f),
                Offset = new UIEdges(0, config.HeaderHeight, 0, 0),
                Size = new Vector2(totalWidth, size.Y - config.HeaderHeight)
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.25f, 0.25f, 0.25f, 1f)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start
            })
            .With(new UIScrollable { VerticalScroll = true, HorizontalScroll = false })
            .WithTag<UIClipChildrenTag>()
            .Build();

        world.SetParent(bodyContainer, grid);

        // Update grid references
        ref var gridComp = ref world.Get<UIDataGrid>(grid);
        gridComp.HeaderRow = headerRow;
        gridComp.BodyContainer = bodyContainer;

        return grid;
    }

    /// <summary>
    /// Creates a data grid with simple column headers.
    /// </summary>
    /// <param name="world">The world to create the data grid in.</param>
    /// <param name="headers">The column header texts.</param>
    /// <returns>The data grid entity.</returns>
    public static Entity CreateDataGrid(IWorld world, params string[] headers)
    {
        return CreateDataGrid(world, DataGridConfig.WithHeaders(headers));
    }

    private static Entity CreateColumnHeader(
        IWorld world,
        Entity grid,
        Entity headerRow,
        int columnIndex,
        DataGridColumnDef colDef,
        float xOffset,
        float height)
    {
        // Column header container
        var column = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0f, 0f),
                AnchorMax = new Vector2(0f, 1f),
                Pivot = new Vector2(0f, 0.5f),
                Offset = new UIEdges(xOffset, 0, 0, 0),
                Size = new Vector2(colDef.Width, height)
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.18f, 0.18f, 0.18f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                BorderWidth = 1f
            })
            .With(new UIInteractable { CanClick = true, CanFocus = false })
            .With(new UIDataGridColumn(grid, columnIndex, colDef.Header)
            {
                Width = colDef.Width,
                MinWidth = colDef.MinWidth,
                IsSortable = colDef.IsSortable,
                IsResizable = colDef.IsResizable
            })
            .Build();

        world.SetParent(column, headerRow);

        // Header text
        var headerText = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Offset = new UIEdges(8f, 0, 30f, 0)
            })
            .With(new UIText
            {
                Content = colDef.Header,
                FontSize = 14f,
                Color = Vector4.One,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(headerText, column);

        // Sort indicator
        var sortIndicator = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(1f, 0f),
                AnchorMax = new Vector2(1f, 1f),
                Pivot = new Vector2(1f, 0.5f),
                Offset = new UIEdges(0, 0, 20f, 0),
                Size = new Vector2(16f, height)
            })
            .With(new UIText
            {
                Content = string.Empty,
                FontSize = 10f,
                Color = new Vector4(0.7f, 0.7f, 0.7f, 1f),
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(sortIndicator, column);

        // Update column reference
        ref var colComp = ref world.Get<UIDataGridColumn>(column);
        colComp.SortIndicator = sortIndicator;

        // Resize handle (if resizable)
        if (colDef.IsResizable)
        {
            var resizeHandle = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = new Vector2(1f, 0f),
                    AnchorMax = new Vector2(1f, 1f),
                    Pivot = new Vector2(1f, 0.5f),
                    Size = new Vector2(6f, height)
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.3f, 0.3f, 0.3f, 0f)
                })
                .With(new UIInteractable { CanClick = false, CanDrag = true })
                .With(new UIDataGridResizeHandle(column))
                .Build();

            world.SetParent(resizeHandle, column);
            colComp.ResizeHandle = resizeHandle;
        }

        return column;
    }

    /// <summary>
    /// Adds a row to a data grid.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="gridEntity">The data grid entity.</param>
    /// <param name="cellValues">The cell values for each column.</param>
    /// <param name="rowData">Optional user data to associate with the row.</param>
    /// <returns>The row entity.</returns>
    public static Entity AddDataGridRow(IWorld world, Entity gridEntity, string[] cellValues, object? rowData = null)
    {
        if (!world.Has<UIDataGrid>(gridEntity))
        {
            return Entity.Null;
        }

        ref var grid = ref world.Get<UIDataGrid>(gridEntity);
        int rowIndex = grid.TotalRowCount;
        grid.TotalRowCount++;

        // Get column widths
        var columnWidths = new List<float>();
        foreach (var colEntity in world.Query<UIDataGridColumn>())
        {
            ref readonly var col = ref world.Get<UIDataGridColumn>(colEntity);
            if (col.DataGrid == gridEntity)
            {
                while (columnWidths.Count <= col.ColumnIndex)
                {
                    columnWidths.Add(100f);
                }

                columnWidths[col.ColumnIndex] = col.Width;
            }
        }

        // Calculate total width
        float totalWidth = 0f;
        foreach (var w in columnWidths)
        {
            totalWidth += w;
        }

        // Determine row background color (alternating)
        var bgColor = grid.AlternatingRowColors && rowIndex % 2 == 1
            ? new Vector4(0.22f, 0.22f, 0.22f, 1f)
            : new Vector4(0.25f, 0.25f, 0.25f, 1f);

        // Create row
        var row = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0f, 1f),
                AnchorMax = new Vector2(1f, 1f),
                Pivot = new Vector2(0f, 1f),
                Offset = new UIEdges(0, rowIndex * grid.RowHeight, 0, 0),
                Size = new Vector2(totalWidth, grid.RowHeight)
            })
            .With(new UIStyle
            {
                BackgroundColor = bgColor,
                BorderColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                BorderWidth = 1f
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start
            })
            .With(new UIInteractable { CanClick = true, CanFocus = true })
            .With(new UIDataGridRow(gridEntity, rowIndex) { RowData = rowData })
            .Build();

        world.SetParent(row, grid.BodyContainer);

        // Create cells
        float xOffset = 0f;
        for (int i = 0; i < cellValues.Length && i < columnWidths.Count; i++)
        {
            CreateDataGridCell(world, gridEntity, row, rowIndex, i, cellValues[i], columnWidths[i], xOffset, grid.RowHeight);
            xOffset += columnWidths[i];
        }

        return row;
    }

    private static Entity CreateDataGridCell(
        IWorld world,
        Entity gridEntity,
        Entity row,
        int rowIndex,
        int columnIndex,
        string value,
        float width,
        float xOffset,
        float height)
    {
        // Find column entity
        Entity columnEntity = Entity.Null;
        foreach (var colEntity in world.Query<UIDataGridColumn>())
        {
            ref readonly var col = ref world.Get<UIDataGridColumn>(colEntity);
            if (col.DataGrid == gridEntity && col.ColumnIndex == columnIndex)
            {
                columnEntity = colEntity;
                break;
            }
        }

        var cell = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0f, 0f),
                AnchorMax = new Vector2(0f, 1f),
                Pivot = new Vector2(0f, 0.5f),
                Offset = new UIEdges(xOffset, 0, 0, 0),
                Size = new Vector2(width, height)
            })
            .With(new UIStyle
            {
                BackgroundColor = Vector4.Zero,
                BorderColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                BorderWidth = 1f
            })
            .With(new UIInteractable { CanClick = true, CanFocus = false })
            .With(new UIDataGridCell(gridEntity, rowIndex, columnIndex)
            {
                Value = value,
                Row = row,
                Column = columnEntity
            })
            .Build();

        world.SetParent(cell, row);

        // Cell text
        var cellText = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Offset = new UIEdges(8f, 0, 8f, 0)
            })
            .With(new UIText
            {
                Content = value,
                FontSize = 13f,
                Color = new Vector4(0.9f, 0.9f, 0.9f, 1f),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(cellText, cell);

        return cell;
    }

    /// <summary>
    /// Clears all rows from a data grid.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="gridEntity">The data grid entity.</param>
    public static void ClearDataGridRows(IWorld world, Entity gridEntity)
    {
        if (!world.Has<UIDataGrid>(gridEntity))
        {
            return;
        }

        ref var grid = ref world.Get<UIDataGrid>(gridEntity);

        // Find and destroy all rows
        var rowsToDestroy = new List<Entity>();
        foreach (var rowEntity in world.Query<UIDataGridRow>())
        {
            ref readonly var row = ref world.Get<UIDataGridRow>(rowEntity);
            if (row.DataGrid == gridEntity)
            {
                rowsToDestroy.Add(rowEntity);
            }
        }

        foreach (var rowEntity in rowsToDestroy)
        {
            world.Despawn(rowEntity);
        }

        grid.TotalRowCount = 0;
        grid.FirstVisibleRow = 0;
        grid.SelectedRow = Entity.Null;
    }

    /// <summary>
    /// Sets the cell value in a data grid.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="gridEntity">The data grid entity.</param>
    /// <param name="rowIndex">The row index.</param>
    /// <param name="columnIndex">The column index.</param>
    /// <param name="value">The new value.</param>
    public static void SetDataGridCellValue(IWorld world, Entity gridEntity, int rowIndex, int columnIndex, string value)
    {
        foreach (var cellEntity in world.Query<UIDataGridCell>())
        {
            ref var cell = ref world.Get<UIDataGridCell>(cellEntity);
            if (cell.DataGrid == gridEntity && cell.RowIndex == rowIndex && cell.ColumnIndex == columnIndex)
            {
                cell.Value = value;

                // Update text in first child (the text entity)
                foreach (var child in world.GetChildren(cellEntity))
                {
                    if (world.Has<UIText>(child))
                    {
                        ref var text = ref world.Get<UIText>(child);
                        text.Content = value;
                        break;
                    }
                }
                return;
            }
        }
    }

    #endregion
}
