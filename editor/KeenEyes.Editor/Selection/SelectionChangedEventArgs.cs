namespace KeenEyes.Editor.Selection;

/// <summary>
/// Event arguments for selection changes.
/// </summary>
public sealed class SelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new selection changed event args.
    /// </summary>
    /// <param name="added">Entities added to selection.</param>
    /// <param name="removed">Entities removed from selection.</param>
    /// <param name="primarySelection">The new primary selection.</param>
    public SelectionChangedEventArgs(
        IReadOnlyList<Entity> added,
        IReadOnlyList<Entity> removed,
        Entity primarySelection)
    {
        Added = added;
        Removed = removed;
        PrimarySelection = primarySelection;
    }

    /// <summary>
    /// Gets the entities that were added to the selection.
    /// </summary>
    public IReadOnlyList<Entity> Added { get; }

    /// <summary>
    /// Gets the entities that were removed from the selection.
    /// </summary>
    public IReadOnlyList<Entity> Removed { get; }

    /// <summary>
    /// Gets the new primary (most recently selected) entity.
    /// </summary>
    public Entity PrimarySelection { get; }
}
