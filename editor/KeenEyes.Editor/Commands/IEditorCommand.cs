namespace KeenEyes.Editor.Commands;

/// <summary>
/// Represents an undoable editor command.
/// </summary>
public interface IEditorCommand
{
    /// <summary>
    /// Gets a human-readable description of the command for display in the Edit menu.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    void Execute();

    /// <summary>
    /// Undoes the command, restoring the previous state.
    /// </summary>
    void Undo();

    /// <summary>
    /// Attempts to merge this command with another command of the same type.
    /// Used to combine rapid successive edits (e.g., typing, dragging).
    /// </summary>
    /// <param name="other">The other command to merge with.</param>
    /// <returns>True if the commands were merged; false otherwise.</returns>
    bool TryMerge(IEditorCommand other);
}
