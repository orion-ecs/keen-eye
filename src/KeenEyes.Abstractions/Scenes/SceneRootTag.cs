namespace KeenEyes.Scenes;

/// <summary>
/// Marks an entity as the root of a spawned scene.
/// </summary>
/// <remarks>
/// Scene roots are created when a scene is spawned via <c>world.Scenes.Spawn()</c>.
/// Each spawned scene has exactly one root entity with this tag.
/// </remarks>
[TagComponent]
public partial struct SceneRootTag : ITagComponent;
