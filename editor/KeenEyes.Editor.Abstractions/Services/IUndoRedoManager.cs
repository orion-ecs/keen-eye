// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Manages the undo/redo stack for editor commands.
/// </summary>
public interface IUndoRedoManager
{
    /// <summary>
    /// Gets whether there are commands that can be undone.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether there are commands that can be redone.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Gets the description of the next command to undo, or null if none.
    /// </summary>
    string? NextUndoDescription { get; }

    /// <summary>
    /// Gets the description of the next command to redo, or null if none.
    /// </summary>
    string? NextRedoDescription { get; }

    /// <summary>
    /// Gets or sets the maximum history size.
    /// </summary>
    int MaxHistorySize { get; set; }

    /// <summary>
    /// Occurs when the undo/redo state changes.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    void Execute(IEditorCommand command);

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    /// <returns>True if a command was undone; false if the undo stack was empty.</returns>
    bool Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    /// <returns>True if a command was redone; false if the redo stack was empty.</returns>
    bool Redo();

    /// <summary>
    /// Begins a batch of commands that will be treated as a single undo operation.
    /// </summary>
    /// <param name="description">Description for the batch.</param>
    void BeginBatch(string description);

    /// <summary>
    /// Ends the current batch and adds it to the undo stack.
    /// </summary>
    void EndBatch();

    /// <summary>
    /// Cancels the current batch without adding it to the undo stack.
    /// </summary>
    void CancelBatch();

    /// <summary>
    /// Clears both undo and redo stacks.
    /// </summary>
    void Clear();
}

/// <summary>
/// Interface for commands that can be executed and undone.
/// </summary>
public interface IEditorCommand
{
    /// <summary>
    /// Gets a description of this command for display in UI.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    void Execute();

    /// <summary>
    /// Undoes the command.
    /// </summary>
    void Undo();

    /// <summary>
    /// Tries to merge this command with a subsequent command.
    /// </summary>
    /// <param name="other">The subsequent command.</param>
    /// <returns>True if the commands were merged; false otherwise.</returns>
    bool TryMerge(IEditorCommand other);
}
