namespace KeenEyes.Scenes;

/// <summary>
/// Marks an entity as persistent across scene unloads.
/// </summary>
/// <remarks>
/// Entities with this tag are never despawned by scene unload operations,
/// regardless of their <see cref="SceneMembership"/>. Use for player entities,
/// managers, or other objects that should survive scene transitions.
/// </remarks>
[TagComponent]
public partial struct PersistentTag : ITagComponent;
