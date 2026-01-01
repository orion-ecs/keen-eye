using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

public class UndoRedoManagerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesEmptyManager()
    {
        var manager = new UndoRedoManager();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
        Assert.Null(manager.NextUndoDescription);
        Assert.Null(manager.NextRedoDescription);
    }

    [Fact]
    public void Constructor_AcceptsMaxHistorySize()
    {
        var manager = new UndoRedoManager(maxHistorySize: 5);

        // Execute more commands than max
        for (int i = 0; i < 10; i++)
        {
            manager.Execute(new TestCommand($"Command {i}"));
        }

        // Should only be able to undo up to max
        int undoCount = 0;
        while (manager.CanUndo)
        {
            manager.Undo();
            undoCount++;
        }

        Assert.Equal(5, undoCount);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_AddsToUndoStack()
    {
        var manager = new UndoRedoManager();
        var command = new TestCommand("Test");

        manager.Execute(command);

        Assert.True(manager.CanUndo);
        Assert.Equal("Test", manager.NextUndoDescription);
    }

    [Fact]
    public void Execute_CallsCommandExecute()
    {
        var manager = new UndoRedoManager();
        var command = new TestCommand("Test");

        manager.Execute(command);

        Assert.True(command.WasExecuted);
    }

    [Fact]
    public void Execute_ClearsRedoStack()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("First"));
        manager.Undo();

        Assert.True(manager.CanRedo);

        manager.Execute(new TestCommand("Second"));

        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Execute_RaisesStateChangedEvent()
    {
        var manager = new UndoRedoManager();
        var eventRaised = false;
        manager.StateChanged += () => eventRaised = true;

        manager.Execute(new TestCommand("Test"));

        Assert.True(eventRaised);
    }

    [Fact]
    public void Execute_MergesWithPreviousCommand_WhenMergeable()
    {
        var manager = new UndoRedoManager();
        var command1 = new MergeableCommand("Key", 1);
        var command2 = new MergeableCommand("Key", 2);

        manager.Execute(command1);
        manager.Execute(command2);

        // Should only need one undo (commands were merged)
        manager.Undo();
        Assert.False(manager.CanUndo);
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_CallsCommandUndo()
    {
        var manager = new UndoRedoManager();
        var command = new TestCommand("Test");
        manager.Execute(command);

        manager.Undo();

        Assert.True(command.WasUndone);
    }

    [Fact]
    public void Undo_MovesToRedoStack()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));

        manager.Undo();

        Assert.False(manager.CanUndo);
        Assert.True(manager.CanRedo);
        Assert.Equal("Test", manager.NextRedoDescription);
    }

    [Fact]
    public void Undo_ReturnsFalse_WhenStackEmpty()
    {
        var manager = new UndoRedoManager();

        var result = manager.Undo();

        Assert.False(result);
    }

    [Fact]
    public void Undo_ReturnsTrue_WhenSuccessful()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));

        var result = manager.Undo();

        Assert.True(result);
    }

    [Fact]
    public void Undo_RaisesStateChangedEvent()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));

        var eventRaised = false;
        manager.StateChanged += () => eventRaised = true;

        manager.Undo();

        Assert.True(eventRaised);
    }

    #endregion

    #region Redo Tests

    [Fact]
    public void Redo_CallsCommandExecute()
    {
        var manager = new UndoRedoManager();
        var command = new TestCommand("Test");
        manager.Execute(command);
        manager.Undo();
        command.WasExecuted = false; // Reset flag

        manager.Redo();

        Assert.True(command.WasExecuted);
    }

    [Fact]
    public void Redo_MovesToUndoStack()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));
        manager.Undo();

        manager.Redo();

        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Redo_ReturnsFalse_WhenStackEmpty()
    {
        var manager = new UndoRedoManager();

        var result = manager.Redo();

        Assert.False(result);
    }

    [Fact]
    public void Redo_ReturnsTrue_WhenSuccessful()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));
        manager.Undo();

        var result = manager.Redo();

        Assert.True(result);
    }

    [Fact]
    public void Redo_RaisesStateChangedEvent()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));
        manager.Undo();

        var eventRaised = false;
        manager.StateChanged += () => eventRaised = true;

        manager.Redo();

        Assert.True(eventRaised);
    }

    #endregion

    #region Batch Tests

    [Fact]
    public void BeginBatch_StartsBatchMode()
    {
        var manager = new UndoRedoManager();

        manager.BeginBatch("Batch Operation");
        manager.Execute(new TestCommand("One"));
        manager.Execute(new TestCommand("Two"));
        manager.EndBatch();

        // Both commands in one undo
        manager.Undo();
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void BeginBatch_ThrowsIfAlreadyActive()
    {
        var manager = new UndoRedoManager();
        manager.BeginBatch("First");

        Assert.Throws<InvalidOperationException>(() => manager.BeginBatch("Second"));
    }

    [Fact]
    public void EndBatch_ThrowsIfNotActive()
    {
        var manager = new UndoRedoManager();

        Assert.Throws<InvalidOperationException>(() => manager.EndBatch());
    }

    [Fact]
    public void EndBatch_UsesBatchDescription()
    {
        var manager = new UndoRedoManager();

        manager.BeginBatch("Batch Operation");
        manager.Execute(new TestCommand("One"));
        manager.EndBatch();

        Assert.Equal("Batch Operation", manager.NextUndoDescription);
    }

    [Fact]
    public void EndBatch_DoesNotAddEmptyBatch()
    {
        var manager = new UndoRedoManager();

        manager.BeginBatch("Empty Batch");
        manager.EndBatch();

        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void CancelBatch_DiscardsCurrentBatch()
    {
        var manager = new UndoRedoManager();

        manager.BeginBatch("Cancelled");
        manager.Execute(new TestCommand("One"));
        manager.CancelBatch();

        // Commands were executed but not added to undo stack
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void Batch_UndosInReverseOrder()
    {
        var manager = new UndoRedoManager();
        var order = new List<string>();

        var cmd1 = new TestCommand("One", () => order.Add("Undo One"));
        var cmd2 = new TestCommand("Two", () => order.Add("Undo Two"));

        manager.BeginBatch("Batch");
        manager.Execute(cmd1);
        manager.Execute(cmd2);
        manager.EndBatch();

        manager.Undo();

        Assert.Equal(["Undo Two", "Undo One"], order);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllCommands()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("One"));
        manager.Execute(new TestCommand("Two"));
        manager.Undo();

        manager.Clear();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Clear_CancelsBatch()
    {
        var manager = new UndoRedoManager();
        manager.BeginBatch("Batch");
        manager.Execute(new TestCommand("Test"));

        manager.Clear();

        // Should not throw when not in batch mode
        manager.BeginBatch("New Batch");
        manager.EndBatch();
    }

    [Fact]
    public void Clear_RaisesStateChangedEvent()
    {
        var manager = new UndoRedoManager();
        manager.Execute(new TestCommand("Test"));

        var eventRaised = false;
        manager.StateChanged += () => eventRaised = true;

        manager.Clear();

        Assert.True(eventRaised);
    }

    #endregion

    #region Test Helpers

    private sealed class TestCommand(string description, Action? onUndo = null) : IEditorCommand
    {
        public string Description { get; } = description;
        public bool WasExecuted { get; set; }
        public bool WasUndone { get; private set; }

        public void Execute() => WasExecuted = true;

        public void Undo()
        {
            WasUndone = true;
            onUndo?.Invoke();
        }

        public bool TryMerge(IEditorCommand other) => false;
    }

    private sealed class MergeableCommand(string key, int value) : IEditorCommand
    {
        public string Key { get; } = key;
        public int Value { get; private set; } = value;
        public string Description => $"Set {Key} to {Value}";

        public void Execute() { }
        public void Undo() { }

        public bool TryMerge(IEditorCommand other)
        {
            if (other is MergeableCommand mergeable && mergeable.Key == Key)
            {
                Value = mergeable.Value;
                return true;
            }
            return false;
        }
    }

    #endregion
}
