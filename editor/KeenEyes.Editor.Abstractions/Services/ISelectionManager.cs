// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Manages entity selection in the editor with support for multi-select.
/// </summary>
public interface ISelectionManager
{
    /// <summary>
    /// Gets the primary selected entity (the most recently selected one).
    /// </summary>
    Entity PrimarySelection { get; }

    /// <summary>
    /// Gets all selected entities.
    /// </summary>
    IReadOnlyCollection<Entity> SelectedEntities { get; }

    /// <summary>
    /// Gets whether there is any selection.
    /// </summary>
    bool HasSelection { get; }

    /// <summary>
    /// Gets whether multiple entities are selected.
    /// </summary>
    bool HasMultipleSelection { get; }

    /// <summary>
    /// Gets the number of selected entities.
    /// </summary>
    int SelectionCount { get; }

    /// <summary>
    /// Raised when the selection changes.
    /// </summary>
    event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Selects a single entity, clearing any previous selection.
    /// </summary>
    /// <param name="entity">The entity to select.</param>
    void Select(Entity entity);

    /// <summary>
    /// Adds an entity to the current selection.
    /// </summary>
    /// <param name="entity">The entity to add to selection.</param>
    void AddToSelection(Entity entity);

    /// <summary>
    /// Removes an entity from the current selection.
    /// </summary>
    /// <param name="entity">The entity to remove from selection.</param>
    void RemoveFromSelection(Entity entity);

    /// <summary>
    /// Toggles the selection state of an entity.
    /// </summary>
    /// <param name="entity">The entity to toggle.</param>
    void ToggleSelection(Entity entity);

    /// <summary>
    /// Selects multiple entities, clearing any previous selection.
    /// </summary>
    /// <param name="entities">The entities to select.</param>
    void SelectMultiple(IEnumerable<Entity> entities);

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Checks if an entity is currently selected.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is selected; otherwise, false.</returns>
    bool IsSelected(Entity entity);
}

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
