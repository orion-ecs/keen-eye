namespace KeenEyes.Editor.Commands;

/// <summary>
/// Manages the undo/redo stack for editor commands.
/// </summary>
public sealed class UndoRedoManager
{
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();
    private readonly int _maxHistorySize;
    private bool _isBatchActive;
    private CommandBatch? _currentBatch;

    /// <summary>
    /// Creates a new undo/redo manager.
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of commands to keep in history.</param>
    public UndoRedoManager(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Gets whether there are commands that can be undone.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether there are commands that can be redone.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the description of the next command to undo, or null if none.
    /// </summary>
    public string? NextUndoDescription => _undoStack.TryPeek(out var cmd) ? cmd.Description : null;

    /// <summary>
    /// Gets the description of the next command to redo, or null if none.
    /// </summary>
    public string? NextRedoDescription => _redoStack.TryPeek(out var cmd) ? cmd.Description : null;

    /// <summary>
    /// Occurs when the undo/redo state changes.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    public void Execute(IEditorCommand command)
    {
        command.Execute();

        if (_isBatchActive && _currentBatch is not null)
        {
            _currentBatch.Commands.Add(command);
        }
        else
        {
            AddToUndoStack(command);
        }

        // Clear redo stack when new command is executed
        _redoStack.Clear();

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    /// <returns>True if a command was undone; false if the undo stack was empty.</returns>
    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);

        StateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    /// <returns>True if a command was redone; false if the redo stack was empty.</returns>
    public bool Redo()
    {
        if (!CanRedo)
        {
            return false;
        }

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);

        StateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Begins a batch of commands that will be treated as a single undo operation.
    /// </summary>
    /// <param name="description">Description for the batch.</param>
    public void BeginBatch(string description)
    {
        if (_isBatchActive)
        {
            throw new InvalidOperationException("A batch is already active. Call EndBatch() first.");
        }

        _isBatchActive = true;
        _currentBatch = new CommandBatch(description);
    }

    /// <summary>
    /// Ends the current batch and adds it to the undo stack.
    /// </summary>
    public void EndBatch()
    {
        if (!_isBatchActive || _currentBatch is null)
        {
            throw new InvalidOperationException("No batch is active. Call BeginBatch() first.");
        }

        _isBatchActive = false;

        if (_currentBatch.Commands.Count > 0)
        {
            AddToUndoStack(_currentBatch);
            _redoStack.Clear();
            StateChanged?.Invoke();
        }

        _currentBatch = null;
    }

    /// <summary>
    /// Cancels the current batch without adding it to the undo stack.
    /// Any commands executed during the batch are NOT undone.
    /// </summary>
    public void CancelBatch()
    {
        if (!_isBatchActive)
        {
            return;
        }

        _isBatchActive = false;
        _currentBatch = null;
    }

    /// <summary>
    /// Clears both undo and redo stacks.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _isBatchActive = false;
        _currentBatch = null;

        StateChanged?.Invoke();
    }

    private void AddToUndoStack(IEditorCommand command)
    {
        // Try to merge with the previous command
        if (_undoStack.TryPeek(out var previous) && previous.TryMerge(command))
        {
            // Command was merged, nothing more to do
            return;
        }

        _undoStack.Push(command);

        // Trim history if it exceeds max size
        if (_undoStack.Count > _maxHistorySize)
        {
            var temp = new Stack<IEditorCommand>();
            for (int i = 0; i < _maxHistorySize; i++)
            {
                temp.Push(_undoStack.Pop());
            }
            _undoStack.Clear();
            while (temp.Count > 0)
            {
                _undoStack.Push(temp.Pop());
            }
        }
    }

    /// <summary>
    /// A batch of commands treated as a single undo operation.
    /// </summary>
    private sealed class CommandBatch : IEditorCommand
    {
        public List<IEditorCommand> Commands { get; } = [];

        public string Description { get; }

        public CommandBatch(string description)
        {
            Description = description;
        }

        public void Execute()
        {
            foreach (var command in Commands)
            {
                command.Execute();
            }
        }

        public void Undo()
        {
            // Undo in reverse order
            for (int i = Commands.Count - 1; i >= 0; i--)
            {
                Commands[i].Undo();
            }
        }

        public bool TryMerge(IEditorCommand other)
        {
            // Batches don't merge
            return false;
        }
    }
}
