// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Manages the current scene world in the editor.
/// </summary>
public interface IEditorWorldManager
{
    /// <summary>
    /// Gets the current scene world, or null if no scene is loaded.
    /// </summary>
    IWorld? CurrentSceneWorld { get; }

    /// <summary>
    /// Gets the path to the current scene file, or null if the scene hasn't been saved.
    /// </summary>
    string? CurrentScenePath { get; }

    /// <summary>
    /// Gets whether the current scene has unsaved changes.
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Raised when a scene is opened.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneOpened;

    /// <summary>
    /// Raised when a scene is closed.
    /// </summary>
    event EventHandler? SceneClosed;

    /// <summary>
    /// Raised when the scene is modified.
    /// </summary>
    event EventHandler? SceneModified;

    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    void NewScene();

    /// <summary>
    /// Loads a scene from the specified path.
    /// </summary>
    /// <param name="path">The path to the scene file.</param>
    /// <returns>True if the scene was loaded successfully; otherwise, false.</returns>
    bool LoadScene(string path);

    /// <summary>
    /// Saves the current scene.
    /// </summary>
    /// <returns>True if the scene was saved successfully; otherwise, false.</returns>
    bool SaveScene();

    /// <summary>
    /// Saves the current scene to a new path.
    /// </summary>
    /// <param name="path">The path to save to.</param>
    /// <returns>True if the scene was saved successfully; otherwise, false.</returns>
    bool SaveSceneAs(string path);

    /// <summary>
    /// Closes the current scene.
    /// </summary>
    void CloseScene();

    /// <summary>
    /// Marks the scene as having unsaved changes.
    /// </summary>
    void MarkModified();

    /// <summary>
    /// Gets the root entities in the scene.
    /// </summary>
    /// <returns>An enumerable of root entities.</returns>
    IEnumerable<Entity> GetRootEntities();

    /// <summary>
    /// Gets the children of an entity.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>An enumerable of child entities.</returns>
    IEnumerable<Entity> GetChildren(Entity parent);

    /// <summary>
    /// Gets the name of an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity name, or a default name if not named.</returns>
    string GetEntityName(Entity entity);
}

/// <summary>
/// Event arguments for scene events.
/// </summary>
/// <param name="World">The scene world.</param>
/// <param name="Path">The path to the scene file, if any.</param>
public sealed class SceneEventArgs(IWorld World, string? Path) : EventArgs
{
    /// <summary>
    /// Gets the scene world.
    /// </summary>
    public IWorld World { get; } = World;

    /// <summary>
    /// Gets the path to the scene file, if any.
    /// </summary>
    public string? Path { get; } = Path;
}
