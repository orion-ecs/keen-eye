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
}
