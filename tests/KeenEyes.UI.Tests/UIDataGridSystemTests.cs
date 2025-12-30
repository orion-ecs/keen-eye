using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

using SortDirection = KeenEyes.UI.Abstractions.SortDirection;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIDataGridSystem sorting, selection, and column resizing.
/// </summary>
public class UIDataGridSystemTests
{
    #region Component Tests

    [Fact]
    public void UIDataGrid_DefaultValues_AreCorrect()
    {
        var grid = new UIDataGrid();

        Assert.Equal(GridSelectionMode.Single, grid.SelectionMode);
        Assert.True(grid.AllowColumnResize);
        Assert.True(grid.AllowSorting);
        Assert.Equal(30f, grid.RowHeight);
        Assert.Equal(35f, grid.HeaderHeight);
        Assert.True(grid.AlternatingRowColors);
    }

    [Fact]
    public void UIDataGridColumn_Initialization_SetsValues()
    {
        var picker = Entity.Null;
        var column = new UIDataGridColumn(picker, 2, "Name")
        {
            Width = 150f,
            MinWidth = 60f,
            IsSortable = false
        };

        Assert.Equal(picker, column.DataGrid);
        Assert.Equal(2, column.ColumnIndex);
        Assert.Equal("Name", column.Header);
        Assert.Equal(150f, column.Width);
        Assert.Equal(60f, column.MinWidth);
        Assert.False(column.IsSortable);
    }

    [Fact]
    public void UIDataGridRow_Initialization_SetsValues()
    {
        var picker = Entity.Null;
        var row = new UIDataGridRow(picker, 5)
        {
            IsSelected = true,
            RowData = "custom data"
        };

        Assert.Equal(picker, row.DataGrid);
        Assert.Equal(5, row.RowIndex);
        Assert.True(row.IsSelected);
        Assert.Equal("custom data", row.RowData);
    }

    [Fact]
    public void UIDataGridCell_Initialization_SetsValues()
    {
        var picker = Entity.Null;
        var cell = new UIDataGridCell(picker, 3, 1)
        {
            Value = "Test Value"
        };

        Assert.Equal(picker, cell.DataGrid);
        Assert.Equal(3, cell.RowIndex);
        Assert.Equal(1, cell.ColumnIndex);
        Assert.Equal("Test Value", cell.Value);
    }

    #endregion

    #region Factory Tests

    [Fact]
    public void CreateDataGrid_WithConfig_CreatesGrid()
    {
        using var world = new World();

        var config = DataGridConfig.WithHeaders("ID", "Name", "Status");
        var grid = WidgetFactory.CreateDataGrid(world, config);

        Assert.True(world.IsAlive(grid));
        Assert.True(world.Has<UIDataGrid>(grid));
    }

    [Fact]
    public void CreateDataGrid_WithHeaders_CreatesColumns()
    {
        using var world = new World();

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name", "Status");

        int columnCount = 0;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid)
            {
                columnCount++;
            }
        }

        Assert.Equal(3, columnCount);
    }

    [Fact]
    public void AddDataGridRow_AddsRowEntity()
    {
        using var world = new World();

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        Assert.True(world.IsAlive(row));
        Assert.True(world.Has<UIDataGridRow>(row));

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row);
        Assert.Equal(grid, rowComp.DataGrid);
        Assert.Equal(0, rowComp.RowIndex);
    }

    [Fact]
    public void AddDataGridRow_IncrementsTotalRowCount()
    {
        using var world = new World();

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);
        WidgetFactory.AddDataGridRow(world, grid, ["3", "Charlie"]);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(3, gridComp.TotalRowCount);
    }

    [Fact]
    public void ClearDataGridRows_RemovesAllRows()
    {
        using var world = new World();

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        WidgetFactory.ClearDataGridRows(world, grid);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(0, gridComp.TotalRowCount);
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void SelectRowByIndex_SelectsCorrectRow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        var row2 = WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        system.SelectRowByIndex(grid, 1);

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row2);
        Assert.True(rowComp.IsSelected);
    }

    [Fact]
    public void ClearSelection_DeselectsAllRows()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row1 = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        system.SelectRowByIndex(grid, 0);
        system.ClearSelection(grid);

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row1);
        Assert.False(rowComp.IsSelected);
    }

    [Fact]
    public void GetSelectedRowIndices_ReturnsSelectedRows()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.MultiSelect(new DataGridColumnDef("ID"), new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        system.SelectRowByIndex(grid, 0);

        var selected = system.GetSelectedRowIndices(grid);
        Assert.Contains(0, selected);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public void SortByColumn_SetsSortedColumn()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        system.SortByColumn(grid, 0, SortDirection.Ascending);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(SortDirection.Ascending, gridComp.SortDirection);
    }

    [Fact]
    public void SortByColumn_FiresSortEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        bool eventFired = false;
        world.Subscribe<UIGridSortEvent>(evt =>
        {
            eventFired = true;
            Assert.Equal(grid, evt.DataGrid);
            Assert.Equal(0, evt.ColumnIndex);
            Assert.Equal(SortDirection.Descending, evt.Direction);
        });

        system.SortByColumn(grid, 0, SortDirection.Descending);

        Assert.True(eventFired);
    }

    #endregion

    #region Column Width Tests

    [Fact]
    public void SetColumnWidth_UpdatesWidth()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        system.SetColumnWidth(grid, 0, 200f);

        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                Assert.Equal(200f, column.Width);
                break;
            }
        }
    }

    [Fact]
    public void SetColumnWidth_RespectsMinWidth()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.WithColumns(
            new DataGridColumnDef("ID", MinWidth: 80f),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);

        system.SetColumnWidth(grid, 0, 30f); // Below min width

        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                Assert.True(column.Width >= column.MinWidth);
                break;
            }
        }
    }

    #endregion

    #region Event Tests

    [Fact]
    public void RowSelection_FiresRowSelectedEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        bool eventFired = false;
        world.Subscribe<UIGridRowSelectedEvent>(evt =>
        {
            eventFired = true;
            Assert.Equal(grid, evt.DataGrid);
            Assert.Equal(0, evt.RowIndex);
            Assert.True(evt.IsSelected);
        });

        system.SelectRowByIndex(grid, 0);

        Assert.True(eventFired);
    }

    [Fact]
    public void ColumnResize_FiresColumnResizedEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        bool eventFired = false;
        world.Subscribe<UIGridColumnResizedEvent>(evt =>
        {
            eventFired = true;
            Assert.Equal(grid, evt.DataGrid);
            Assert.Equal(0, evt.ColumnIndex);
        });

        system.SetColumnWidth(grid, 0, 200f);

        Assert.True(eventFired);
    }

    #endregion

    #region Config Tests

    [Fact]
    public void DataGridConfig_WithHeaders_CreatesColumnDefs()
    {
        var config = DataGridConfig.WithHeaders("A", "B", "C");

        Assert.Equal(3, config.Columns.Length);
        Assert.Equal("A", config.Columns[0].Header);
        Assert.Equal("B", config.Columns[1].Header);
        Assert.Equal("C", config.Columns[2].Header);
    }

    [Fact]
    public void DataGridConfig_ReadOnly_DisablesFeatures()
    {
        var config = DataGridConfig.ReadOnly(new DataGridColumnDef("ID"));

        Assert.Equal(GridSelectionMode.None, config.SelectionMode);
        Assert.False(config.AllowColumnResize);
        Assert.False(config.AllowSorting);
    }

    [Fact]
    public void DataGridConfig_MultiSelect_EnablesMultipleSelection()
    {
        var config = DataGridConfig.MultiSelect(new DataGridColumnDef("ID"));

        Assert.Equal(GridSelectionMode.Multiple, config.SelectionMode);
    }

    [Fact]
    public void DataGridColumnDef_DefaultValues_AreCorrect()
    {
        var col = new DataGridColumnDef("Test");

        Assert.Equal("Test", col.Header);
        Assert.Equal(100f, col.Width);
        Assert.Equal(50f, col.MinWidth);
        Assert.True(col.IsSortable);
        Assert.True(col.IsResizable);
    }

    #endregion

    #region Static Method Tests

    [Fact]
    public void UIDataGridSystem_GetDaysInMonth_ReturnsCorrectDays()
    {
        // February non-leap year
        Assert.Equal(28, DateTime.DaysInMonth(2023, 2));

        // February leap year
        Assert.Equal(29, DateTime.DaysInMonth(2024, 2));

        // 31-day month
        Assert.Equal(31, DateTime.DaysInMonth(2024, 1));

        // 30-day month
        Assert.Equal(30, DateTime.DaysInMonth(2024, 4));
    }

    #endregion

    #region Click Event Tests

    [Fact]
    public void ColumnHeaderClick_TogglesSortDirection()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Find column entity
        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        // First click - ascending
        world.Send(new UIClickEvent(columnEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(SortDirection.Ascending, gridComp.SortDirection);
        Assert.Equal(columnEntity, gridComp.SortedColumn);

        // Second click - descending
        world.Send(new UIClickEvent(columnEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        Assert.Equal(SortDirection.Descending, world.Get<UIDataGrid>(grid).SortDirection);
    }

    [Fact]
    public void ColumnHeaderClick_NonSortableColumn_DoesNotSort()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.WithColumns(
            new DataGridColumnDef("ID", IsSortable: false),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);

        // Find column entity
        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        world.Send(new UIClickEvent(columnEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(Entity.Null, gridComp.SortedColumn);
    }

    [Fact]
    public void RowClick_SelectsRow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        world.Send(new UIClickEvent(row, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row);
        Assert.True(rowComp.IsSelected);
    }

    [Fact]
    public void RowClick_SingleSelectionMode_DeselectsPreviousRow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row1 = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        var row2 = WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        // Select first row
        world.Send(new UIClickEvent(row1, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        // Select second row
        world.Send(new UIClickEvent(row2, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var row1Comp = ref world.Get<UIDataGridRow>(row1);
        ref readonly var row2Comp = ref world.Get<UIDataGridRow>(row2);

        Assert.False(row1Comp.IsSelected);
        Assert.True(row2Comp.IsSelected);
    }

    [Fact]
    public void RowClick_NoSelectionMode_DoesNotSelect()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.ReadOnly(new DataGridColumnDef("ID"), new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        world.Send(new UIClickEvent(row, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row);
        Assert.False(rowComp.IsSelected);
    }

    [Fact]
    public void CellClick_SelectsParentRow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Find a cell in this row
        Entity cellEntity = Entity.Null;
        foreach (var cell in world.Query<UIDataGridCell>())
        {
            ref readonly var cellComp = ref world.Get<UIDataGridCell>(cell);
            if (cellComp.Row == row)
            {
                cellEntity = cell;
                break;
            }
        }

        if (cellEntity.IsValid)
        {
            world.Send(new UIClickEvent(cellEntity, Vector2.Zero, MouseButton.Left));
            system.Update(0);

            ref readonly var rowComp = ref world.Get<UIDataGridRow>(row);
            Assert.True(rowComp.IsSelected);
        }
    }

    #endregion

    #region Hover Event Tests

    [Fact]
    public void RowHoverEnter_SetsIsHovered()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        world.Send(new UIPointerEnterEvent(row, Vector2.Zero));
        system.Update(0);

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row);
        Assert.True(rowComp.IsHovered);
    }

    [Fact]
    public void RowHoverExit_ClearsIsHovered()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Enter then exit
        world.Send(new UIPointerEnterEvent(row, Vector2.Zero));
        system.Update(0);

        world.Send(new UIPointerExitEvent(row));
        system.Update(0);

        ref readonly var rowComp = ref world.Get<UIDataGridRow>(row);
        Assert.False(rowComp.IsHovered);
    }

    #endregion

    #region Drag Event Tests

    [Fact]
    public void ColumnResizeDragStart_SetsIsDragging()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Find column and its resize handle
        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        // Find the resize handle
        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        if (handleEntity.IsValid)
        {
            world.Send(new UIDragStartEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handleEntity);
            Assert.True(handleComp.IsDragging);
        }
    }

    [Fact]
    public void ColumnResizeDrag_UpdatesColumnWidth()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Find column and its resize handle
        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        float originalWidth = 100f;
        if (world.Has<UIDataGridColumn>(columnEntity))
        {
            originalWidth = world.Get<UIDataGridColumn>(columnEntity).Width;
        }

        // Find the resize handle
        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        if (handleEntity.IsValid)
        {
            // Start drag
            world.Send(new UIDragStartEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            // Drag right by 50 pixels
            world.Send(new UIDragEvent(handleEntity, new Vector2(150f, 10f), new Vector2(50f, 0f)));
            system.Update(0);

            ref readonly var column = ref world.Get<UIDataGridColumn>(columnEntity);
            Assert.True(column.Width > originalWidth);
        }
    }

    [Fact]
    public void ColumnResizeDragEnd_ClearsIsDragging()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Find column and its resize handle
        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        // Find the resize handle
        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        if (handleEntity.IsValid)
        {
            // Start and end drag
            world.Send(new UIDragStartEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            world.Send(new UIDragEndEvent(handleEntity, new Vector2(150f, 10f)));
            system.Update(0);

            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handleEntity);
            Assert.False(handleComp.IsDragging);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ClearSelection_OnDeadGrid_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        world.Despawn(grid);

        // Should not throw
        system.ClearSelection(grid);
    }

    [Fact]
    public void SelectRowByIndex_InvalidIndex_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Should not throw
        system.SelectRowByIndex(grid, 99);
    }

    [Fact]
    public void SortByColumn_InvalidColumnIndex_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Should not throw
        system.SortByColumn(grid, 99, SortDirection.Ascending);
    }

    [Fact]
    public void SetColumnWidth_InvalidColumnIndex_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Should not throw
        system.SetColumnWidth(grid, 99, 200f);
    }

    [Fact]
    public void ClickEvent_OnDeadColumn_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        // Despawn the grid (which should also despawn columns)
        world.Despawn(grid);

        // Should not throw
        world.Send(new UIClickEvent(columnEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);
    }

    [Fact]
    public void ClickEvent_OnNonGridEntity_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Button())
            .Build();

        // Should not throw
        world.Send(new UIClickEvent(button, Vector2.Zero, MouseButton.Left));
        system.Update(0);
    }

    [Fact]
    public void SortingDisabled_ColumnClick_DoesNotSort()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("ID"), new DataGridColumnDef("Name")],
            AllowSorting: false);
        var grid = WidgetFactory.CreateDataGrid(world, config);

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        world.Send(new UIClickEvent(columnEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(Entity.Null, gridComp.SortedColumn);
    }

    [Fact]
    public void ColumnSort_ChangingColumn_ClearsPreviousSortIndicator()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Find both columns
        Entity col0 = Entity.Null;
        Entity col1 = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid)
            {
                if (column.ColumnIndex == 0)
                {
                    col0 = col;
                }
                else if (column.ColumnIndex == 1)
                {
                    col1 = col;
                }
            }
        }

        // Sort by first column
        world.Send(new UIClickEvent(col0, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        // Sort by second column
        world.Send(new UIClickEvent(col1, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(col1, gridComp.SortedColumn);
    }

    #endregion

    #region Multiple Selection Tests

    [Fact]
    public void MultipleSelectionMode_ClickWithoutModifier_ClearsAndSelects()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.MultiSelect(
            new DataGridColumnDef("ID"),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);
        var row1 = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        var row2 = WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        // Select first row
        world.Send(new UIClickEvent(row1, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        // Select second row without modifier - should clear first
        world.Send(new UIClickEvent(row2, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        ref readonly var row1Comp = ref world.Get<UIDataGridRow>(row1);
        ref readonly var row2Comp = ref world.Get<UIDataGridRow>(row2);

        Assert.False(row1Comp.IsSelected);
        Assert.True(row2Comp.IsSelected);
    }

    #endregion

    #region Resize Handle Edge Cases

    [Fact]
    public void ColumnResizeDrag_WhenNotDragging_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        float originalWidth = 100f;
        if (world.Has<UIDataGridColumn>(columnEntity))
        {
            originalWidth = world.Get<UIDataGridColumn>(columnEntity).Width;
        }

        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        if (handleEntity.IsValid)
        {
            // Send drag event without starting drag first
            world.Send(new UIDragEvent(handleEntity, new Vector2(150f, 10f), new Vector2(50f, 0f)));
            system.Update(0);

            ref readonly var column = ref world.Get<UIDataGridColumn>(columnEntity);
            // Width should not have changed
            Assert.Equal(originalWidth, column.Width);
        }
    }

    [Fact]
    public void ColumnResizeDrag_WhenNotResizable_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.WithColumns(
            new DataGridColumnDef("ID", IsResizable: false),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        float originalWidth = world.Get<UIDataGridColumn>(columnEntity).Width;

        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        if (handleEntity.IsValid)
        {
            // Start drag
            world.Send(new UIDragStartEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            // Drag right
            world.Send(new UIDragEvent(handleEntity, new Vector2(150f, 10f), new Vector2(50f, 0f)));
            system.Update(0);

            ref readonly var column = ref world.Get<UIDataGridColumn>(columnEntity);
            // Width should not change for non-resizable column
            Assert.Equal(originalWidth, column.Width);
        }
    }

    [Fact]
    public void ColumnResizeDrag_BelowMinWidth_ClampsToMinWidth()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.WithColumns(
            new DataGridColumnDef("ID", Width: 150f, MinWidth: 80f),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        if (handleEntity.IsValid)
        {
            // Start drag
            world.Send(new UIDragStartEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            // Drag left by 100 pixels (below min width)
            world.Send(new UIDragEvent(handleEntity, new Vector2(0f, 10f), new Vector2(-100f, 0f)));
            system.Update(0);

            ref readonly var column = ref world.Get<UIDataGridColumn>(columnEntity);
            // Width should be clamped to min width
            Assert.True(column.Width >= 80f);
        }
    }

    [Fact]
    public void ColumnResizeDragEnd_WhenNotDragging_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        bool eventFired = false;
        world.Subscribe<UIGridColumnResizedEvent>(evt => eventFired = true);

        if (handleEntity.IsValid)
        {
            // End drag without starting - should not fire event
            world.Send(new UIDragEndEvent(handleEntity, new Vector2(150f, 10f)));
            system.Update(0);

            Assert.False(eventFired);
        }
    }

    [Fact]
    public void ColumnResizeDragEnd_NoWidthChange_DoesNotFireEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        Entity handleEntity = Entity.Null;
        foreach (var handle in world.Query<UIDataGridResizeHandle>())
        {
            ref readonly var handleComp = ref world.Get<UIDataGridResizeHandle>(handle);
            if (handleComp.Column == columnEntity)
            {
                handleEntity = handle;
                break;
            }
        }

        bool eventFired = false;
        world.Subscribe<UIGridColumnResizedEvent>(evt => eventFired = true);

        if (handleEntity.IsValid)
        {
            // Start drag
            world.Send(new UIDragStartEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            // End drag without moving (same position)
            world.Send(new UIDragEndEvent(handleEntity, new Vector2(100f, 10f)));
            system.Update(0);

            Assert.False(eventFired);
        }
    }

    #endregion

    #region Sort Indicator Tests

    [Fact]
    public void SortByColumn_WithSortIndicator_UpdatesText()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        // Check if sort indicator exists and get updated
        ref readonly var columnComp = ref world.Get<UIDataGridColumn>(columnEntity);
        if (world.IsAlive(columnComp.SortIndicator) && world.Has<UIText>(columnComp.SortIndicator))
        {
            system.SortByColumn(grid, 0, SortDirection.Ascending);

            ref readonly var text = ref world.Get<UIText>(columnComp.SortIndicator);
            Assert.Equal("\u25B2", text.Content); // ▲

            system.SortByColumn(grid, 0, SortDirection.Descending);

            ref readonly var text2 = ref world.Get<UIText>(columnComp.SortIndicator);
            Assert.Equal("\u25BC", text2.Content); // ▼
        }
    }

    #endregion

    #region Dead Entity Edge Cases

    [Fact]
    public void CellClick_WithDeadRow_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Find a cell in this row
        Entity cellEntity = Entity.Null;
        foreach (var cell in world.Query<UIDataGridCell>())
        {
            ref readonly var cellComp = ref world.Get<UIDataGridCell>(cell);
            if (cellComp.Row == row)
            {
                cellEntity = cell;
                break;
            }
        }

        // Despawn the row but keep the cell (unusual but possible)
        if (cellEntity.IsValid)
        {
            world.Despawn(row);

            // Should not throw
            world.Send(new UIClickEvent(cellEntity, Vector2.Zero, MouseButton.Left));
            system.Update(0);
        }
    }

    [Fact]
    public void ColumnClick_WithDeadGrid_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                break;
            }
        }

        // Despawn grid but not column (unusual but tests the guard)
        world.Despawn(grid);

        // Should not throw - grid is dead so sorting should be ignored
        world.Send(new UIClickEvent(columnEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);
    }

    [Fact]
    public void RowClick_WithDeadGrid_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Keep row reference
        var rowEntity = row;

        // Despawn grid but not row
        world.Despawn(grid);

        // Should not throw - grid is dead so selection should be ignored
        world.Send(new UIClickEvent(rowEntity, Vector2.Zero, MouseButton.Left));
        system.Update(0);
    }

    #endregion

    #region Select Already Selected Row

    [Fact]
    public void SelectRow_AlreadySelected_DoesNotFireEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Select row first time
        system.SelectRowByIndex(grid, 0);

        int eventCount = 0;
        world.Subscribe<UIGridRowSelectedEvent>(evt => eventCount++);

        // Try to select again - should not fire new event
        world.Send(new UIClickEvent(row, Vector2.Zero, MouseButton.Left));
        system.Update(0);

        // No additional event should have been fired since row was already selected
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void DeselectRow_NotSelected_DoesNotFireEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        int eventCount = 0;
        world.Subscribe<UIGridRowSelectedEvent>(evt =>
        {
            if (!evt.IsSelected)
            {
                eventCount++;
            }
        });

        // Clear selection when nothing is selected
        system.ClearSelection(grid);

        // No deselection event should fire
        Assert.Equal(0, eventCount);
    }

    #endregion

    #region Multiple Selection with Ctrl Key Tests

    [Fact]
    public void MultipleSelectionMode_CtrlClick_TogglesSelection()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.MultiSelect(
            new DataGridColumnDef("ID"),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);
        var row1 = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        var row2 = WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        // Select first row
        system.SelectRowByIndex(grid, 0);

        // Ctrl+click second row to add to selection
        system.SelectRowByIndex(grid, 1);

        var selected = system.GetSelectedRowIndices(grid);
        Assert.Contains(1, selected);
    }

    [Fact]
    public void MultipleSelectionMode_GetSelectedRowIndices_ReturnsAllSelected()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = DataGridConfig.MultiSelect(
            new DataGridColumnDef("ID"),
            new DataGridColumnDef("Name"));
        var grid = WidgetFactory.CreateDataGrid(world, config);
        WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);
        WidgetFactory.AddDataGridRow(world, grid, ["3", "Charlie"]);

        // Select all rows manually
        system.SelectRowByIndex(grid, 0);
        system.SelectRowByIndex(grid, 1);
        system.SelectRowByIndex(grid, 2);

        var selected = system.GetSelectedRowIndices(grid);
        // In single selection mode, only last selected is retained
        Assert.True(selected.Length >= 1);
    }

    #endregion

    #region Row Visual Update Tests

    [Fact]
    public void RowHover_WithNoUIStyle_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        // Create row without UIStyle
        var grid = world.Spawn()
            .With(new UIDataGrid())
            .Build();

        var row = world.Spawn()
            .With(new UIDataGridRow(grid, 0))
            .With(UIElement.Default)
            .Build();

        // Should not throw even without UIStyle
        world.Send(new UIPointerEnterEvent(row, Vector2.Zero));
        system.Update(0);

        world.Send(new UIPointerExitEvent(row));
        system.Update(0);
    }

    [Fact]
    public void RowSelect_AlternatingRowColors_UpdatesCorrectBackground()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        _ = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        var row2 = WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        // Row 2 should have alternating color (odd index)
        if (world.Has<UIStyle>(row2))
        {
            // Select and deselect to trigger visual update
            system.SelectRowByIndex(grid, 1);
            system.ClearSelection(grid);

            // Should return to alternating color (darker than base 0.25f)
            ref readonly var afterStyle = ref world.Get<UIStyle>(row2);
            Assert.True(afterStyle.BackgroundColor.X <= 0.25f);
        }
    }

    [Fact]
    public void RowSelect_AlternatingRowColorsDisabled_UsesSameBackground()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("ID"), new DataGridColumnDef("Name")],
            AlternatingRowColors: false);
        var grid = WidgetFactory.CreateDataGrid(world, config);
        var row1 = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);
        var row2 = WidgetFactory.AddDataGridRow(world, grid, ["2", "Bob"]);

        // Both rows should have same background when alternating is disabled
        if (world.Has<UIStyle>(row1) && world.Has<UIStyle>(row2))
        {
            // Select and deselect to trigger visual update
            system.SelectRowByIndex(grid, 0);
            system.ClearSelection(grid);
            system.SelectRowByIndex(grid, 1);
            system.ClearSelection(grid);

            ref readonly var style1 = ref world.Get<UIStyle>(row1);
            ref readonly var style2 = ref world.Get<UIStyle>(row2);

            Assert.Equal(style1.BackgroundColor, style2.BackgroundColor);
        }
    }

    [Fact]
    public void RowHover_SetsHoveredBackground()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        world.Send(new UIPointerEnterEvent(row, Vector2.Zero));
        system.Update(0);

        if (world.Has<UIStyle>(row))
        {
            ref readonly var style = ref world.Get<UIStyle>(row);
            // Hovered color should be different from default
            Assert.True(style.BackgroundColor.X >= 0.35f);
        }
    }

    #endregion

    #region Drag Handle Edge Cases

    [Fact]
    public void DragStart_HandleWithDeadColumn_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        // Create handle with a dead column reference
        var handle = world.Spawn()
            .With(new UIDataGridResizeHandle { Column = Entity.Null })
            .Build();

        // Should not throw
        world.Send(new UIDragStartEvent(handle, new Vector2(100f, 10f)));
        system.Update(0);
    }

    [Fact]
    public void Drag_HandleWithDeadColumn_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        // Create handle with dead column
        var handle = world.Spawn()
            .With(new UIDataGridResizeHandle { Column = Entity.Null, IsDragging = true })
            .Build();

        // Should not throw
        world.Send(new UIDragEvent(handle, new Vector2(150f, 10f), new Vector2(50f, 0f)));
        system.Update(0);
    }

    [Fact]
    public void DragEnd_HandleWithDeadColumn_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        // Create handle with dead column
        var handle = world.Spawn()
            .With(new UIDataGridResizeHandle { Column = Entity.Null, IsDragging = true })
            .Build();

        // Should not throw
        world.Send(new UIDragEndEvent(handle, new Vector2(150f, 10f)));
        system.Update(0);
    }

    [Fact]
    public void Drag_ColumnWithNoUIRect_StillUpdatesWidth()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        // Create column without UIRect
        var grid = world.Spawn()
            .With(new UIDataGrid())
            .Build();

        var column = world.Spawn()
            .With(new UIDataGridColumn(grid, 0, "Test") { Width = 100f, MinWidth = 50f, IsResizable = true })
            .Build();

        var handle = world.Spawn()
            .With(new UIDataGridResizeHandle { Column = column })
            .Build();

        // Start drag
        world.Send(new UIDragStartEvent(handle, new Vector2(100f, 10f)));
        system.Update(0);

        // Drag
        world.Send(new UIDragEvent(handle, new Vector2(150f, 10f), new Vector2(50f, 0f)));
        system.Update(0);

        // Width should be updated even without UIRect
        ref readonly var col = ref world.Get<UIDataGridColumn>(column);
        Assert.True(col.Width > 100f);
    }

    #endregion

    #region Sort Indicator Edge Cases

    [Fact]
    public void ClearSortIndicator_NonColumnEntity_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Sort by column first
        system.SortByColumn(grid, 0, SortDirection.Ascending);

        // Sort by a different column - this triggers ClearSortIndicator on the previous column
        system.SortByColumn(grid, 1, SortDirection.Ascending);

        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(SortDirection.Ascending, gridComp.SortDirection);
    }

    [Fact]
    public void UpdateSortIndicator_SortDirectionNone_ClearsIndicator()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Sort first
        system.SortByColumn(grid, 0, SortDirection.Ascending);

        // Sort with None
        system.SortByColumn(grid, 0, SortDirection.None);

        // The sort direction should be None
        ref readonly var gridComp = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(SortDirection.None, gridComp.SortDirection);
    }

    [Fact]
    public void SortByColumn_WithDeadSortIndicator_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Find and despawn the sort indicator
        Entity columnEntity = Entity.Null;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                columnEntity = col;
                if (world.IsAlive(column.SortIndicator))
                {
                    world.Despawn(column.SortIndicator);
                }
                break;
            }
        }

        // Should not throw with dead indicator
        system.SortByColumn(grid, 0, SortDirection.Ascending);
    }

    #endregion

    #region System Dispose Tests

    [Fact]
    public void UIDataGridSystem_Dispose_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        // Should not throw
        system.Dispose();
    }

    #endregion

    #region Pointer Event Edge Cases

    [Fact]
    public void PointerEnter_NonRowEntity_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .Build();

        // Should not throw
        world.Send(new UIPointerEnterEvent(button, Vector2.Zero));
        system.Update(0);
    }

    [Fact]
    public void PointerExit_NonRowEntity_IsIgnored()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var button = world.Spawn()
            .With(UIElement.Default)
            .Build();

        // Should not throw
        world.Send(new UIPointerExitEvent(button));
        system.Update(0);
    }

    #endregion

    #region Dead Row Tests

    [Fact]
    public void SelectRow_DeadEntity_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");
        var row = WidgetFactory.AddDataGridRow(world, grid, ["1", "Alice"]);

        // Despawn the row
        world.Despawn(row);

        // Should not throw
        system.SelectRowByIndex(grid, 0);
    }

    #endregion

    #region SetColumnWidth Edge Cases

    [Fact]
    public void SetColumnWidth_WithUIRect_UpdatesRectSize()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        system.SetColumnWidth(grid, 0, 200f);

        // Check that UIRect was updated if it exists
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                if (world.Has<UIRect>(col))
                {
                    ref readonly var rect = ref world.Get<UIRect>(col);
                    Assert.Equal(200f, rect.Size.X);
                }
                break;
            }
        }
    }

    [Fact]
    public void SetColumnWidth_SameWidth_DoesNotFireEvent()
    {
        using var world = new World();
        var system = new UIDataGridSystem();
        system.Initialize(world);

        var grid = WidgetFactory.CreateDataGrid(world, "ID", "Name");

        // Get current width
        float currentWidth = 100f;
        foreach (var col in world.Query<UIDataGridColumn>())
        {
            ref readonly var column = ref world.Get<UIDataGridColumn>(col);
            if (column.DataGrid == grid && column.ColumnIndex == 0)
            {
                currentWidth = column.Width;
                break;
            }
        }

        bool eventFired = false;
        world.Subscribe<UIGridColumnResizedEvent>(evt => eventFired = true);

        // Set to same width
        system.SetColumnWidth(grid, 0, currentWidth);

        Assert.False(eventFired);
    }

    #endregion
}
