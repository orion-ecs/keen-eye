namespace KeenEyes.Scenes;

/// <summary>
/// Metadata for a scene root entity.
/// </summary>
/// <remarks>
/// This component is automatically added to scene root entities when spawned.
/// It provides identification and state tracking for the scene.
/// </remarks>
[Component(Serializable = true)]
public partial struct SceneMetadata : IComponent
{
    /// <summary>
    /// The name of the scene (matches the .kescene file name).
    /// </summary>
    public required string Name;

    /// <summary>
    /// Unique identifier for this scene instance.
    /// </summary>
    public Guid SceneId;

    /// <summary>
    /// Current state of the scene.
    /// </summary>
    public SceneState State;
}
