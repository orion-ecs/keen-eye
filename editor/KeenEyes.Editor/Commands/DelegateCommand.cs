// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Commands;

/// <summary>
/// A command that executes delegate actions for do and undo operations.
/// </summary>
/// <remarks>
/// <para>
/// This command is useful for one-off operations where creating a dedicated
/// command class would be excessive. For frequently-used operations, consider
/// creating a specific command class instead.
/// </para>
/// </remarks>
internal sealed class DelegateCommand : IEditorCommand
{
    private readonly Action executeAction;
    private readonly Action undoAction;

    /// <summary>
    /// Creates a new delegate command.
    /// </summary>
    /// <param name="description">A description of the command for the undo menu.</param>
    /// <param name="executeAction">The action to execute when doing the command.</param>
    /// <param name="undoAction">The action to execute when undoing the command.</param>
    public DelegateCommand(string description, Action executeAction, Action undoAction)
    {
        Description = description;
        this.executeAction = executeAction;
        this.undoAction = undoAction;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Execute() => executeAction();

    /// <inheritdoc />
    public void Undo() => undoAction();

    /// <inheritdoc />
    public bool TryMerge(IEditorCommand other) => false;
}
