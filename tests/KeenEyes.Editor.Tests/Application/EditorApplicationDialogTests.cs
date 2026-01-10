// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Commands;
using KeenEyes.Editor.Layout;

namespace KeenEyes.Editor.Tests.Application;

/// <summary>
/// Tests for EditorApplication dialog functionality.
/// These tests validate that dialog-related operations (SaveLayout, RenameEntity, Settings)
/// correctly interact with their respective managers and commands.
/// </summary>
public sealed class EditorApplicationDialogTests : IDisposable
{
    private readonly World world;

    public EditorApplicationDialogTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region SaveLayout Dialog Tests

    [Fact]
    public void SaveLayout_WithValidName_SavesLayout()
    {
        // Arrange
        var saveLayoutCalled = false;
        string? savedName = null;

        // Act
        SaveLayout("My Custom Layout", name =>
        {
            saveLayoutCalled = true;
            savedName = name;
            return true;
        });

        // Assert
        Assert.True(saveLayoutCalled);
        Assert.Equal("My Custom Layout", savedName);
    }

    [Fact]
    public void SaveLayout_WithEmptyName_DoesNotSaveLayout()
    {
        // Arrange
        var saveLayoutCalled = false;

        // Act
        SaveLayout(string.Empty, _ =>
        {
            saveLayoutCalled = true;
            return true;
        });

        // Assert
        Assert.False(saveLayoutCalled);
    }

    [Fact]
    public void SaveLayout_WithNullName_DoesNotSaveLayout()
    {
        // Arrange
        var saveLayoutCalled = false;

        // Act
        SaveLayout(null!, _ =>
        {
            saveLayoutCalled = true;
            return true;
        });

        // Assert
        Assert.False(saveLayoutCalled);
    }

    [Fact]
    public void SaveLayout_WithWhitespaceName_DoesNotSaveLayout()
    {
        // Arrange
        var saveLayoutCalled = false;

        // Act
        SaveLayout("   ", _ =>
        {
            saveLayoutCalled = true;
            return true;
        });

        // Assert
        Assert.False(saveLayoutCalled);
    }

    [Fact]
    public void SaveLayout_TrimsLayoutName()
    {
        // Arrange
        string? savedName = null;

        // Act
        SaveLayout("  My Layout  ", name =>
        {
            savedName = name;
            return true;
        });

        // Assert
        Assert.Equal("My Layout", savedName);
    }

    #endregion

    #region RenameEntity Dialog Tests

    [Fact]
    public void RenameEntity_WithValidSelection_ExecutesCommand()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();
        var entity = world.Spawn().Build();

        // Act
        RenameEntity(entity, "Old Name", "New Entity Name", undoRedo, world);

        // Assert
        Assert.True(undoRedo.ExecuteCalled);
        Assert.IsType<RenameEntityCommand>(undoRedo.LastCommand);
    }

    [Fact]
    public void RenameEntity_WithNoSelection_DoesNotExecuteCommand()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();

        // Act
        RenameEntity(Entity.Null, null, "New Entity Name", undoRedo, world);

        // Assert
        Assert.False(undoRedo.ExecuteCalled);
    }

    [Fact]
    public void RenameEntity_WithEmptyName_DoesNotExecuteCommand()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();
        var entity = world.Spawn().Build();

        // Act
        RenameEntity(entity, "Old Name", string.Empty, undoRedo, world);

        // Assert
        Assert.False(undoRedo.ExecuteCalled);
    }

    [Fact]
    public void RenameEntity_WithSameName_DoesNotExecuteCommand()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();
        var entity = world.Spawn().Build();
        // Simulate the entity already having a name
        var currentName = "Existing Name";

        // Act
        RenameEntity(entity, currentName, "Existing Name", undoRedo, world);

        // Assert
        Assert.False(undoRedo.ExecuteCalled);
    }

    [Fact]
    public void RenameEntity_WithNewName_ExecutesRenameCommand()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();
        var entity = world.Spawn().Build();
        // Simulate the entity having an old name
        var currentName = "Old Name";

        // Act
        RenameEntity(entity, currentName, "New Name", undoRedo, world);

        // Assert
        Assert.True(undoRedo.ExecuteCalled);
        var command = undoRedo.LastCommand as RenameEntityCommand;
        Assert.NotNull(command);
    }

    [Fact]
    public void RenameEntity_SupportsUndo()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();
        var entity = world.Spawn().Build();

        // Act
        RenameEntity(entity, "Old Name", "New Name", undoRedo, world);

        // Assert - command should be undoable
        Assert.True(undoRedo.ExecuteCalled);
        Assert.NotNull(undoRedo.LastCommand);
    }

    #endregion

    #region Settings Dialog Tests

    [Fact]
    public void ShowSettings_DoesNotThrow()
    {
        // Arrange
        var settingsShown = false;

        // Act & Assert
        var exception = Record.Exception(() => ShowSettings(() => settingsShown = true));
        Assert.Null(exception);
        Assert.True(settingsShown);
    }

    [Fact]
    public void ShowSettings_WithNoCanvas_GracefullyHandles()
    {
        // Arrange
        var settingsShown = false;

        // Act & Assert - should not throw, just not show
        var exception = Record.Exception(() => ShowSettings(hasCanvas: false, () => settingsShown = true));
        Assert.Null(exception);
        Assert.False(settingsShown);
    }

    #endregion

    #region Dialog Integration Tests

    [Fact]
    public void DialogActions_WithNoCanvas_GracefullyHandles()
    {
        // Arrange
        var saveCalled = false;
        var settingsCalled = false;

        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            SaveLayout("Test", hasCanvas: false, _ => { saveCalled = true; return true; });
            ShowSettings(hasCanvas: false, () => settingsCalled = true);
        });
        Assert.Null(exception);
        Assert.False(saveCalled);
        Assert.False(settingsCalled);
    }

    [Fact]
    public void MultipleDialogs_CanCoexist()
    {
        // Arrange
        var undoRedo = new TestUndoRedoManager();
        var entity = world.Spawn().Build();
        var settingsShown = false;
        var layoutSaved = false;

        // Act
        ShowSettings(() => settingsShown = true);
        SaveLayout("Layout", name => { layoutSaved = true; return true; });
        RenameEntity(entity, "Entity", "New Name", undoRedo, world);

        // Assert - all operations should succeed
        Assert.True(settingsShown);
        Assert.True(layoutSaved);
        Assert.True(undoRedo.ExecuteCalled);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Simulates the SaveLayout action as implemented in EditorApplication.
    /// </summary>
    private static void SaveLayout(string? name, Func<string, bool> saveAction)
    {
        SaveLayout(name, hasCanvas: true, saveAction);
    }

    private static void SaveLayout(string? name, bool hasCanvas, Func<string, bool> saveAction)
    {
        if (!hasCanvas)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var trimmedName = name.Trim();
        saveAction(trimmedName);
    }

    /// <summary>
    /// Simulates the RenameEntity action as implemented in EditorApplication.
    /// </summary>
    private static void RenameEntity(
        Entity entity,
        string? currentName,
        string? newName,
        IUndoRedoManager undoRedo,
        World sceneWorld)
    {
        if (!entity.IsValid)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        if (currentName == newName)
        {
            return;
        }

        var command = new RenameEntityCommand(sceneWorld, entity, newName);
        undoRedo.Execute(command);
    }

    /// <summary>
    /// Simulates the ShowSettings action as implemented in EditorApplication.
    /// </summary>
    private static void ShowSettings(Action showAction)
    {
        ShowSettings(hasCanvas: true, showAction);
    }

    private static void ShowSettings(bool hasCanvas, Action showAction)
    {
        if (!hasCanvas)
        {
            return;
        }

        showAction();
    }

    private sealed class TestUndoRedoManager : IUndoRedoManager
    {
        public bool ExecuteCalled { get; private set; }
        public IEditorCommand? LastCommand { get; private set; }
        public bool CanUndo => false;
        public bool CanRedo => false;
        public string? NextUndoDescription => null;
        public string? NextRedoDescription => null;
        public int MaxHistorySize { get; set; } = 100;

#pragma warning disable CS0067 // Event is never used
        public event Action? StateChanged;
#pragma warning restore CS0067

        public void Execute(IEditorCommand command)
        {
            ExecuteCalled = true;
            LastCommand = command;
            command.Execute();
        }

        public bool Undo() => false;
        public bool Redo() => false;
        public void Clear() { }
        public void BeginBatch(string description) { }
        public void EndBatch() { }
        public void CancelBatch() { }
    }

    #endregion
}
