namespace KeenEyes.Editor.Selection;

/// <summary>
/// Manages entity selection in the editor with support for multi-select.
/// </summary>
public sealed class SelectionManager
{
    private readonly HashSet<Entity> _selectedEntities = [];
    private Entity _primarySelection = Entity.Null;

    /// <summary>
    /// Raised when the selection changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Gets the primary selected entity (the most recently selected one).
    /// </summary>
    public Entity PrimarySelection => _primarySelection;

    /// <summary>
    /// Gets all selected entities.
    /// </summary>
    public IReadOnlyCollection<Entity> SelectedEntities => _selectedEntities;

    /// <summary>
    /// Gets whether there is any selection.
    /// </summary>
    public bool HasSelection => _selectedEntities.Count > 0;

    /// <summary>
    /// Gets whether multiple entities are selected.
    /// </summary>
    public bool HasMultipleSelection => _selectedEntities.Count > 1;

    /// <summary>
    /// Gets the number of selected entities.
    /// </summary>
    public int SelectionCount => _selectedEntities.Count;

    /// <summary>
    /// Selects a single entity, clearing any previous selection.
    /// </summary>
    /// <param name="entity">The entity to select.</param>
    public void Select(Entity entity)
    {
        var previousSelection = _selectedEntities.ToList();

        _selectedEntities.Clear();

        if (entity.IsValid)
        {
            _selectedEntities.Add(entity);
            _primarySelection = entity;
        }
        else
        {
            _primarySelection = Entity.Null;
        }

        RaiseSelectionChanged(previousSelection);
    }

    /// <summary>
    /// Adds an entity to the current selection.
    /// </summary>
    /// <param name="entity">The entity to add to selection.</param>
    public void AddToSelection(Entity entity)
    {
        if (!entity.IsValid || _selectedEntities.Contains(entity))
        {
            return;
        }

        var previousSelection = _selectedEntities.ToList();

        _selectedEntities.Add(entity);
        _primarySelection = entity;

        RaiseSelectionChanged(previousSelection);
    }

    /// <summary>
    /// Removes an entity from the current selection.
    /// </summary>
    /// <param name="entity">The entity to remove from selection.</param>
    public void RemoveFromSelection(Entity entity)
    {
        if (!_selectedEntities.Contains(entity))
        {
            return;
        }

        var previousSelection = _selectedEntities.ToList();

        _selectedEntities.Remove(entity);

        // Update primary selection if we removed it
        if (_primarySelection == entity)
        {
            _primarySelection = _selectedEntities.Count > 0
                ? _selectedEntities.First()
                : Entity.Null;
        }

        RaiseSelectionChanged(previousSelection);
    }

    /// <summary>
    /// Toggles the selection state of an entity.
    /// </summary>
    /// <param name="entity">The entity to toggle.</param>
    public void ToggleSelection(Entity entity)
    {
        if (_selectedEntities.Contains(entity))
        {
            RemoveFromSelection(entity);
        }
        else
        {
            AddToSelection(entity);
        }
    }

    /// <summary>
    /// Selects multiple entities, clearing any previous selection.
    /// </summary>
    /// <param name="entities">The entities to select.</param>
    public void SelectMultiple(IEnumerable<Entity> entities)
    {
        var previousSelection = _selectedEntities.ToList();

        _selectedEntities.Clear();

        foreach (var entity in entities)
        {
            if (entity.IsValid)
            {
                _selectedEntities.Add(entity);
                _primarySelection = entity; // Last one becomes primary
            }
        }

        if (_selectedEntities.Count == 0)
        {
            _primarySelection = Entity.Null;
        }

        RaiseSelectionChanged(previousSelection);
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedEntities.Count == 0)
        {
            return;
        }

        var previousSelection = _selectedEntities.ToList();

        _selectedEntities.Clear();
        _primarySelection = Entity.Null;

        RaiseSelectionChanged(previousSelection);
    }

    /// <summary>
    /// Checks if an entity is currently selected.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is selected; otherwise, false.</returns>
    public bool IsSelected(Entity entity)
    {
        return _selectedEntities.Contains(entity);
    }

    /// <summary>
    /// Handles entities being despawned from the world.
    /// Removes any despawned entities from the selection.
    /// </summary>
    /// <param name="entity">The despawned entity.</param>
    public void OnEntityDespawned(Entity entity)
    {
        if (_selectedEntities.Contains(entity))
        {
            RemoveFromSelection(entity);
        }
    }

    private void RaiseSelectionChanged(List<Entity> previousSelection)
    {
        var added = _selectedEntities.Except(previousSelection).ToList();
        var removed = previousSelection.Except(_selectedEntities).ToList();

        if (added.Count > 0 || removed.Count > 0)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(
                added.AsReadOnly(),
                removed.AsReadOnly(),
                _primarySelection
            ));
        }
    }
}
