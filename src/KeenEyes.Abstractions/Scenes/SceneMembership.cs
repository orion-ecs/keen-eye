namespace KeenEyes.Scenes;

/// <summary>
/// Tracks which scene an entity belongs to and its reference count.
/// </summary>
/// <remarks>
/// <para>
/// Entities can belong to multiple scenes through reference counting.
/// When an entity transitions between scenes, its reference count is adjusted.
/// </para>
/// <para>
/// When a scene unloads, entities with <see cref="ReferenceCount"/> reaching zero
/// are despawned (unless they have <see cref="PersistentTag"/>).
/// </para>
/// </remarks>
[Component(Serializable = true)]
public partial struct SceneMembership : IComponent
{
    /// <summary>
    /// The scene root entity this entity originated from.
    /// </summary>
    public Entity OriginScene;

    /// <summary>
    /// Number of scenes currently referencing this entity.
    /// </summary>
    public int ReferenceCount;
}
