namespace KeenEyes.Animation.Components;

/// <summary>
/// Component that identifies an entity as a bone in a skeleton hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// Entities with BoneReference are part of a skeleton and can be animated
/// by AnimationPlayer or Animator components on their root entity.
/// </para>
/// <para>
/// The bone entity should also have a Transform3D component for the animation
/// system to write pose data to.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a skeleton hierarchy
/// var hip = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new BoneReference { BoneName = "Hip", SkeletonRoot = characterEntity })
///     .Build();
///
/// var spine = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new BoneReference { BoneName = "Spine", SkeletonRoot = characterEntity })
///     .Build();
///
/// world.SetParent(spine, hip);
/// </code>
/// </example>
[Component]
public partial struct BoneReference
{
    /// <summary>
    /// The name of this bone, matching the bone names in animation clips.
    /// </summary>
    public string BoneName;

    /// <summary>
    /// The entity ID of the skeleton root (the entity with AnimationPlayer or Animator).
    /// </summary>
    public int SkeletonRootId;

    /// <summary>
    /// Creates a bone reference with the specified name and root.
    /// </summary>
    /// <param name="boneName">The bone name.</param>
    /// <param name="skeletonRootId">The skeleton root entity ID.</param>
    /// <returns>A configured bone reference.</returns>
    public static BoneReference Create(string boneName, int skeletonRootId) => new()
    {
        BoneName = boneName,
        SkeletonRootId = skeletonRootId
    };
}
