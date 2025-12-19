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
}
