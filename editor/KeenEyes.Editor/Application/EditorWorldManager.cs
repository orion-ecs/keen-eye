using KeenEyes.Editor.Assets;
using KeenEyes.Scenes;

namespace KeenEyes.Editor.Application;

/// <summary>
/// Manages the separation between the editor UI world and scene editing worlds.
/// Uses SceneManager API for scene lifecycle management with a single persistent World.
/// </summary>
public sealed class EditorWorldManager : IDisposable
{
    private readonly World _world;
    private readonly SceneSerializer _sceneSerializer = new();
    private Entity _currentSceneRoot;
    private string? _currentScenePath;
    private bool _isDisposed;
    private int _sceneModificationCount;

    /// <summary>
    /// Creates a new EditorWorldManager with an empty scene.
    /// </summary>
    public EditorWorldManager()
    {
        _world = new World();
        // Create initial untitled scene
        _currentSceneRoot = _world.Scenes.Spawn("Untitled");
    }

    /// <summary>
    /// Gets the world containing all scenes being edited.
    /// </summary>
    public World World => _world;

    /// <summary>
    /// Gets the current scene world being edited, or null if no scene is open.
    /// </summary>
    /// <remarks>
    /// For backward compatibility. Returns the shared world if a scene is open.
    /// </remarks>
    public World? CurrentSceneWorld => _currentSceneRoot.IsValid ? _world : null;

    /// <summary>
    /// Gets the root entity of the currently open scene.
    /// </summary>
    public Entity CurrentSceneRoot => _currentSceneRoot;

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
        // Unload current scene if any
        if (_currentSceneRoot.IsValid && _world.IsAlive(_currentSceneRoot))
        {
            _world.Scenes.Unload(_currentSceneRoot);
        }

        ClearSelection();

        // Create a new scene
        _currentSceneRoot = _world.Scenes.Spawn("Untitled");
        _currentScenePath = null;
        _sceneModificationCount = 0;

        SceneOpened?.Invoke(_world);
    }

    /// <summary>
    /// Opens a scene from a file path.
    /// </summary>
    /// <param name="path">The path to the .kescene file.</param>
    /// <remarks>
    /// This is an alias for <see cref="LoadScene"/> for API consistency.
    /// Does not check for unsaved changes - caller should handle that.
    /// </remarks>
    public void OpenScene(string path) => LoadScene(path);

    /// <summary>
    /// Loads a scene from a file path.
    /// </summary>
    /// <param name="path">The path to the .kescene file.</param>
    public void LoadScene(string path)
    {
        // Unload current scene if any
        if (_currentSceneRoot.IsValid && _world.IsAlive(_currentSceneRoot))
        {
            _world.Scenes.Unload(_currentSceneRoot);
        }

        ClearSelection();

        // Create a new scene root
        var sceneName = Path.GetFileNameWithoutExtension(path);
        _currentSceneRoot = _world.Scenes.Spawn(sceneName);
        _currentScenePath = path;
        _sceneModificationCount = 0;

        // Load scene data and restore entities, associating with the scene root
        _ = _sceneSerializer.Load(_world, path, _currentSceneRoot);

        SceneOpened?.Invoke(_world);
    }

    /// <summary>
    /// Saves the current scene to its file path.
    /// </summary>
    /// <returns>True if saved successfully, false if no scene or no path.</returns>
    public bool SaveScene()
    {
        if (!_currentSceneRoot.IsValid || _currentScenePath is null)
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
        if (!_currentSceneRoot.IsValid || !_world.IsAlive(_currentSceneRoot))
        {
            return false;
        }

        // Capture only entities belonging to the current scene
        var sceneData = _sceneSerializer.CaptureScene(_world, _currentSceneRoot);

        // Override the name with the filename
        sceneData.Name = Path.GetFileNameWithoutExtension(path);

        // Write to file
        var json = System.Text.Json.JsonSerializer.Serialize(sceneData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        File.WriteAllText(path, json);

        _currentScenePath = path;
        _sceneModificationCount = 0;

        return true;
    }

    /// <summary>
    /// Closes the current scene.
    /// </summary>
    public void CloseScene()
    {
        if (_currentSceneRoot.IsValid && _world.IsAlive(_currentSceneRoot))
        {
            ClearSelection();
            _world.Scenes.Unload(_currentSceneRoot);
            _currentSceneRoot = Entity.Null;
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
        if (!_currentSceneRoot.IsValid)
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
    /// Gets all root entities in the current scene (entities without parents, excluding scene root).
    /// </summary>
    /// <returns>Enumerable of root entities, or empty if no scene is open.</returns>
    public IEnumerable<Entity> GetRootEntities()
    {
        if (!_currentSceneRoot.IsValid)
        {
            yield break;
        }

        // Return entities that have no parent and are not the scene root itself
        foreach (var entity in _world.GetAllEntities())
        {
            // Skip the scene root
            if (entity.Id == _currentSceneRoot.Id)
            {
                continue;
            }

            // Skip scene-related entities (with SceneRootTag)
            if (_world.Has<SceneRootTag>(entity))
            {
                continue;
            }

            var parent = _world.GetParent(entity);
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
        if (!_currentSceneRoot.IsValid)
        {
            return [];
        }

        return _world.GetChildren(parent);
    }

    /// <summary>
    /// Gets the name of an entity in the current scene.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity name, or a default name if unnamed.</returns>
    public string GetEntityName(Entity entity)
    {
        if (!_currentSceneRoot.IsValid)
        {
            return "Entity";
        }

        var name = _world.GetName(entity);
        return string.IsNullOrEmpty(name) ? $"Entity {entity.Id}" : name;
    }

    /// <summary>
    /// Creates a new entity in the current scene.
    /// </summary>
    /// <param name="name">Optional name for the entity.</param>
    /// <returns>The created entity, or Entity.Null if no scene is open.</returns>
    public Entity CreateEntity(string? name = null)
    {
        if (!_currentSceneRoot.IsValid)
        {
            return Entity.Null;
        }

        var entity = _world.Spawn(name).Build();

        // Add the entity to the current scene
        _world.Scenes.AddToScene(entity, _currentSceneRoot);

        MarkModified();
        return entity;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _world.Dispose();
    }
}
