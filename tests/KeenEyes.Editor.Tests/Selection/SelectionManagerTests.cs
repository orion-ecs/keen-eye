using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Selection;

namespace KeenEyes.Editor.Tests.Selection;

public class SelectionManagerTests : IDisposable
{
    private readonly World world;

    public SelectionManagerTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_HasNoSelection()
    {
        var manager = new SelectionManager();

        Assert.False(manager.HasSelection);
        Assert.False(manager.HasMultipleSelection);
        Assert.Equal(0, manager.SelectionCount);
        Assert.Equal(Entity.Null, manager.PrimarySelection);
        Assert.Empty(manager.SelectedEntities);
    }

    #endregion

    #region Select Tests

    [Fact]
    public void Select_SingleEntity_SetsSelection()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();

        manager.Select(entity);

        Assert.True(manager.HasSelection);
        Assert.False(manager.HasMultipleSelection);
        Assert.Equal(1, manager.SelectionCount);
        Assert.Equal(entity, manager.PrimarySelection);
        Assert.Contains(entity, manager.SelectedEntities);
    }

    [Fact]
    public void Select_ClearsPreviousSelection()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();

        manager.Select(entity1);
        manager.Select(entity2);

        Assert.Equal(1, manager.SelectionCount);
        Assert.Equal(entity2, manager.PrimarySelection);
        Assert.DoesNotContain(entity1, manager.SelectedEntities);
    }

    [Fact]
    public void Select_NullEntity_ClearsSelection()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        manager.Select(Entity.Null);

        Assert.False(manager.HasSelection);
        Assert.Equal(Entity.Null, manager.PrimarySelection);
    }

    [Fact]
    public void Select_RaisesSelectionChangedEvent()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        SelectionChangedEventArgs? eventArgs = null;
        manager.SelectionChanged += (_, e) => eventArgs = e;

        manager.Select(entity);

        Assert.NotNull(eventArgs);
        Assert.Contains(entity, eventArgs.Added);
        Assert.Empty(eventArgs.Removed);
        Assert.Equal(entity, eventArgs.PrimarySelection);
    }

    #endregion

    #region AddToSelection Tests

    [Fact]
    public void AddToSelection_AddsToExistingSelection()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();

        manager.Select(entity1);
        manager.AddToSelection(entity2);

        Assert.True(manager.HasMultipleSelection);
        Assert.Equal(2, manager.SelectionCount);
        Assert.Contains(entity1, manager.SelectedEntities);
        Assert.Contains(entity2, manager.SelectedEntities);
    }

    [Fact]
    public void AddToSelection_SetsAsPrimary()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();

        manager.Select(entity1);
        manager.AddToSelection(entity2);

        Assert.Equal(entity2, manager.PrimarySelection);
    }

    [Fact]
    public void AddToSelection_IgnoresDuplicate()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();

        manager.Select(entity);
        manager.AddToSelection(entity);

        Assert.Equal(1, manager.SelectionCount);
    }

    [Fact]
    public void AddToSelection_IgnoresNullEntity()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        manager.AddToSelection(Entity.Null);

        Assert.Equal(1, manager.SelectionCount);
    }

    [Fact]
    public void AddToSelection_RaisesEventOnlyWhenChanged()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        var eventCount = 0;
        manager.SelectionChanged += (_, _) => eventCount++;

        manager.AddToSelection(entity); // Duplicate, no change
        manager.AddToSelection(Entity.Null); // Invalid, no change

        Assert.Equal(0, eventCount);
    }

    #endregion

    #region RemoveFromSelection Tests

    [Fact]
    public void RemoveFromSelection_RemovesEntity()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();
        manager.Select(entity1);
        manager.AddToSelection(entity2);

        manager.RemoveFromSelection(entity2);

        Assert.Equal(1, manager.SelectionCount);
        Assert.DoesNotContain(entity2, manager.SelectedEntities);
    }

    [Fact]
    public void RemoveFromSelection_UpdatesPrimary_WhenRemovingPrimary()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();
        manager.Select(entity1);
        manager.AddToSelection(entity2);

        manager.RemoveFromSelection(entity2); // entity2 is primary

        Assert.Equal(entity1, manager.PrimarySelection);
    }

    [Fact]
    public void RemoveFromSelection_SetsPrimaryToNull_WhenLastRemoved()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        manager.RemoveFromSelection(entity);

        Assert.Equal(Entity.Null, manager.PrimarySelection);
        Assert.False(manager.HasSelection);
    }

    [Fact]
    public void RemoveFromSelection_IgnoresUnselectedEntity()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();
        manager.Select(entity1);

        var eventRaised = false;
        manager.SelectionChanged += (_, _) => eventRaised = true;

        manager.RemoveFromSelection(entity2);

        Assert.False(eventRaised);
    }

    #endregion

    #region ToggleSelection Tests

    [Fact]
    public void ToggleSelection_AddsIfNotSelected()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();

        manager.ToggleSelection(entity);

        Assert.True(manager.IsSelected(entity));
    }

    [Fact]
    public void ToggleSelection_RemovesIfSelected()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        manager.ToggleSelection(entity);

        Assert.False(manager.IsSelected(entity));
    }

    #endregion

    #region SelectMultiple Tests

    [Fact]
    public void SelectMultiple_SelectsAllEntities()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();
        var entity3 = world.Spawn("Three").Build();

        manager.SelectMultiple([entity1, entity2, entity3]);

        Assert.Equal(3, manager.SelectionCount);
        Assert.Contains(entity1, manager.SelectedEntities);
        Assert.Contains(entity2, manager.SelectedEntities);
        Assert.Contains(entity3, manager.SelectedEntities);
    }

    [Fact]
    public void SelectMultiple_ClearsPreviousSelection()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();
        manager.Select(entity1);

        manager.SelectMultiple([entity2]);

        Assert.Equal(1, manager.SelectionCount);
        Assert.DoesNotContain(entity1, manager.SelectedEntities);
    }

    [Fact]
    public void SelectMultiple_LastEntityIsPrimary()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();

        manager.SelectMultiple([entity1, entity2]);

        Assert.Equal(entity2, manager.PrimarySelection);
    }

    [Fact]
    public void SelectMultiple_IgnoresNullEntities()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();

        manager.SelectMultiple([Entity.Null, entity, Entity.Null]);

        Assert.Equal(1, manager.SelectionCount);
    }

    [Fact]
    public void SelectMultiple_EmptyCollection_ClearsSelection()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        manager.SelectMultiple([]);

        Assert.False(manager.HasSelection);
    }

    #endregion

    #region ClearSelection Tests

    [Fact]
    public void ClearSelection_RemovesAllEntities()
    {
        var manager = new SelectionManager();
        var entity1 = world.Spawn("One").Build();
        var entity2 = world.Spawn("Two").Build();
        manager.Select(entity1);
        manager.AddToSelection(entity2);

        manager.ClearSelection();

        Assert.False(manager.HasSelection);
        Assert.Equal(0, manager.SelectionCount);
        Assert.Equal(Entity.Null, manager.PrimarySelection);
    }

    [Fact]
    public void ClearSelection_DoesNotRaiseEvent_WhenAlreadyEmpty()
    {
        var manager = new SelectionManager();
        var eventRaised = false;
        manager.SelectionChanged += (_, _) => eventRaised = true;

        manager.ClearSelection();

        Assert.False(eventRaised);
    }

    [Fact]
    public void ClearSelection_RaisesEvent_WithRemovedEntities()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);
        SelectionChangedEventArgs? eventArgs = null;
        manager.SelectionChanged += (_, e) => eventArgs = e;

        manager.ClearSelection();

        Assert.NotNull(eventArgs);
        Assert.Contains(entity, eventArgs.Removed);
        Assert.Empty(eventArgs.Added);
    }

    #endregion

    #region IsSelected Tests

    [Fact]
    public void IsSelected_ReturnsTrue_ForSelectedEntity()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        Assert.True(manager.IsSelected(entity));
    }

    [Fact]
    public void IsSelected_ReturnsFalse_ForUnselectedEntity()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();

        Assert.False(manager.IsSelected(entity));
    }

    #endregion

    #region OnEntityDespawned Tests

    [Fact]
    public void OnEntityDespawned_RemovesFromSelection()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        manager.Select(entity);

        manager.OnEntityDespawned(entity);

        Assert.False(manager.IsSelected(entity));
        Assert.False(manager.HasSelection);
    }

    [Fact]
    public void OnEntityDespawned_IgnoresUnselectedEntity()
    {
        var manager = new SelectionManager();
        var entity = world.Spawn("Test").Build();
        var eventRaised = false;
        manager.SelectionChanged += (_, _) => eventRaised = true;

        manager.OnEntityDespawned(entity);

        Assert.False(eventRaised);
    }

    #endregion
}
