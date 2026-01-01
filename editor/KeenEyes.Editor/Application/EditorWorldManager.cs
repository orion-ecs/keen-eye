namespace KeenEyes.Editor.Application;

/// <summary>
/// Manages the separation between the editor UI world and scene editing worlds.
/// The editor runs in its own ECS world, while scenes being edited exist in separate worlds.
/// </summary>
public sealed class EditorWorldManager : IDisposable
{
    private World? _currentSceneWorld;
    private string? _currentScenePath;
    private bool _isDisposed;
    private int _sceneModificationCount;

    /// <summary>
    /// Gets the current scene world being edited, or null if no scene is open.
    /// </summary>
    public World? CurrentSceneWorld => _currentSceneWorld;

    /// <summary>
    /// Gets the file path of the currently open scene, or null if unsaved/no scene.
    /// </summary>
    public string? CurrentScenePath => _currentScenePath;

    /// <summary>
    /// Gets whether the current scene has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => _sceneModificationCount > 0;

    /// <summary>
    /// Occurs when a new scene is created or loaded.
    /// </summary>
    public event Action<World>? SceneOpened;

    /// <summary>
    /// Occurs when the current scene is closed.
    /// </summary>
    public event Action? SceneClosed;

    /// <summary>
    /// Occurs when the scene is modified.
    /// </summary>
    public event Action? SceneModified;

    /// <summary>
    /// Occurs when an entity is selected in the scene.
    /// </summary>
    public event Action<Entity>? EntitySelected;

    /// <summary>
    /// Occurs when the selection is cleared.
    /// </summary>
    public event Action? SelectionCleared;

    /// <summary>
    /// Gets the currently selected entity, or Entity.Null if nothing is selected.
    /// </summary>
    public Entity SelectedEntity { get; private set; }

    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    public void NewScene()
    {
        CloseScene();

        _currentSceneWorld = new World();
        _currentScenePath = null;
        _sceneModificationCount = 0;

        // Optionally add default entities here (e.g., a root entity)

        SceneOpened?.Invoke(_currentSceneWorld);
    }

    /// <summary>
    /// Loads a scene from a file path.
    /// </summary>
    /// <param name="path">The path to the .kescene file.</param>
    public void LoadScene(string path)
    {
        CloseScene();

        _currentSceneWorld = new World();
        _currentScenePath = path;
        _sceneModificationCount = 0;

        // TODO: Load scene data from file using SceneSerializer
        // For now, just create an empty world
        Console.WriteLine($"Loading scene: {path}");

        SceneOpened?.Invoke(_currentSceneWorld);
    }

    /// <summary>
    /// Saves the current scene to its file path.
    /// </summary>
    /// <returns>True if saved successfully, false if no scene or no path.</returns>
    public bool SaveScene()
    {
        if (_currentSceneWorld is null || _currentScenePath is null)
        {
            return false;
        }

        return SaveSceneAs(_currentScenePath);
    }

    /// <summary>
    /// Saves the current scene to a new file path.
    /// </summary>
    /// <param name="path">The path to save to.</param>
    /// <returns>True if saved successfully.</returns>
    public bool SaveSceneAs(string path)
    {
        if (_currentSceneWorld is null)
        {
            return false;
        }

        // TODO: Serialize scene to file using SceneSerializer
        Console.WriteLine($"Saving scene to: {path}");

        _currentScenePath = path;
        _sceneModificationCount = 0;

        return true;
    }

    /// <summary>
    /// Closes the current scene.
    /// </summary>
    public void CloseScene()
    {
        if (_currentSceneWorld is not null)
        {
            ClearSelection();
            _currentSceneWorld.Dispose();
            _currentSceneWorld = null;
            _currentScenePath = null;
            _sceneModificationCount = 0;

            SceneClosed?.Invoke();
        }
    }

    /// <summary>
    /// Marks the scene as modified.
    /// </summary>
    public void MarkModified()
    {
        _sceneModificationCount++;
        SceneModified?.Invoke();
    }

    /// <summary>
    /// Selects an entity in the scene.
    /// </summary>
    /// <param name="entity">The entity to select.</param>
    public void Select(Entity entity)
    {
        if (_currentSceneWorld is null)
        {
            return;
        }

        SelectedEntity = entity;
        EntitySelected?.Invoke(entity);
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectedEntity = Entity.Null;
        SelectionCleared?.Invoke();
    }

    /// <summary>
    /// Gets all root entities in the current scene (entities without parents).
    /// </summary>
    /// <returns>Enumerable of root entities, or empty if no scene is open.</returns>
    public IEnumerable<Entity> GetRootEntities()
    {
        if (_currentSceneWorld is null)
        {
            yield break;
        }

        foreach (var entity in _currentSceneWorld.GetAllEntities())
        {
            var parent = _currentSceneWorld.GetParent(entity);
            if (!parent.IsValid)
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets the children of an entity in the current scene.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>Enumerable of child entities.</returns>
    public IEnumerable<Entity> GetChildren(Entity parent)
    {
        if (_currentSceneWorld is null)
        {
            return [];
        }

        return _currentSceneWorld.GetChildren(parent);
    }

    /// <summary>
    /// Gets the name of an entity in the current scene.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity name, or a default name if unnamed.</returns>
    public string GetEntityName(Entity entity)
    {
        if (_currentSceneWorld is null)
        {
            return "Entity";
        }

        var name = _currentSceneWorld.GetName(entity);
        return string.IsNullOrEmpty(name) ? $"Entity {entity.Id}" : name;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CloseScene();
    }
}
