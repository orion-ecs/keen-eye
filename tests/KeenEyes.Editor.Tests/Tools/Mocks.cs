// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Selection;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Tests.Tools;

/// <summary>
/// Mock implementation of IEditorContext for testing tools.
/// </summary>
internal sealed class MockEditorContext : IEditorContext
{
    private readonly Dictionary<Type, object> extensions = [];
    private readonly Dictionary<Type, IEditorCapability> capabilities = [];
    private readonly SelectionManager selectionManager = new();
    private readonly MockUndoRedoManager undoRedoManager = new();

    public IEditorWorldManager Worlds => throw new NotImplementedException();

    public ISelectionManager Selection => selectionManager;

    public IUndoRedoManager UndoRedo => undoRedoManager;

    public IAssetDatabase Assets => throw new NotImplementedException();

    public IWorld EditorWorld => throw new NotImplementedException();

    public ILogQueryable? Log => null;

    public void SetExtension<T>(T extension) where T : class
    {
        extensions[typeof(T)] = extension;
    }

    public T GetExtension<T>() where T : class
    {
        if (extensions.TryGetValue(typeof(T), out var ext))
        {
            return (T)ext;
        }

        throw new InvalidOperationException($"Extension {typeof(T).Name} not registered");
    }

    public bool TryGetExtension<T>(out T? extension) where T : class
    {
        if (extensions.TryGetValue(typeof(T), out var ext))
        {
            extension = (T)ext;
            return true;
        }

        extension = null;
        return false;
    }

    public bool RemoveExtension<T>() where T : class
    {
        return extensions.Remove(typeof(T));
    }

    public T GetCapability<T>() where T : class, IEditorCapability
    {
        if (capabilities.TryGetValue(typeof(T), out var cap))
        {
            return (T)cap;
        }

        throw new InvalidOperationException($"Capability {typeof(T).Name} not available");
    }

    public bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability
    {
        if (capabilities.TryGetValue(typeof(T), out var cap))
        {
            capability = (T)cap;
            return true;
        }

        capability = null;
        return false;
    }

    public bool HasCapability<T>() where T : class, IEditorCapability
    {
        return capabilities.ContainsKey(typeof(T));
    }

    public void RegisterCapability<T>(T capability) where T : class, IEditorCapability
    {
        capabilities[typeof(T)] = capability;
    }

    public EventSubscription OnSceneOpened(Action<IWorld> handler) => new(() => { });

    public EventSubscription OnSceneClosed(Action handler) => new(() => { });

    public EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler) => new(() => { });

    public EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler) => new(() => { });
}

/// <summary>
/// Mock implementation of IUndoRedoManager for testing tools.
/// </summary>
internal sealed class MockUndoRedoManager : IUndoRedoManager
{
    private readonly List<IEditorCommand> executedCommands = [];
    private readonly List<IEditorCommand> redoStack = [];

    public bool CanUndo => executedCommands.Count > 0;

    public bool CanRedo => redoStack.Count > 0;

    public string? NextUndoDescription => executedCommands.Count > 0 ? executedCommands[^1].Description : null;

    public string? NextRedoDescription => redoStack.Count > 0 ? redoStack[^1].Description : null;

    public int MaxHistorySize { get; set; } = 100;

    public IReadOnlyList<IEditorCommand> ExecutedCommands => executedCommands;

    public event Action? StateChanged;

    public void Execute(IEditorCommand command)
    {
        command.Execute();
        executedCommands.Add(command);
        redoStack.Clear();
        StateChanged?.Invoke();
    }

    public bool Undo()
    {
        if (executedCommands.Count == 0)
        {
            return false;
        }

        var command = executedCommands[^1];
        command.Undo();
        executedCommands.RemoveAt(executedCommands.Count - 1);
        redoStack.Add(command);
        StateChanged?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (redoStack.Count == 0)
        {
            return false;
        }

        var command = redoStack[^1];
        command.Execute();
        redoStack.RemoveAt(redoStack.Count - 1);
        executedCommands.Add(command);
        StateChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        executedCommands.Clear();
        redoStack.Clear();
        StateChanged?.Invoke();
    }

    public void BeginBatch(string description)
    {
        // Not implemented for test mocks
    }

    public void EndBatch()
    {
        // Not implemented for test mocks
    }

    public void CancelBatch()
    {
        // Not implemented for test mocks
    }
}
