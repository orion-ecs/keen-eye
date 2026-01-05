namespace KeenEyes.Scenes;

/// <summary>
/// Represents the current state of a loaded scene.
/// </summary>
public enum SceneState
{
    /// <summary>
    /// The scene is fully loaded and active.
    /// </summary>
    Loaded,

    /// <summary>
    /// The scene is in the process of being unloaded.
    /// </summary>
    Unloading
}
