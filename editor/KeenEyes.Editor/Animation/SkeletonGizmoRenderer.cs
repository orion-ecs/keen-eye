using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Animation;

/// <summary>
/// Gizmo renderer that visualizes skeleton bone hierarchies in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// This renderer draws bones as lines connecting parent-child joints, with spheres
/// at each bone position. Selected bones are highlighted in a different color.
/// </para>
/// <para>
/// The renderer activates for entities that have either:
/// <list type="bullet">
///   <item><description>SkinnedMesh component (shows the mesh's skeleton)</description></item>
///   <item><description>BoneReference component (individual bone visualization)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SkeletonGizmoRenderer : IGizmoRenderer
{
    private static readonly Vector4 BoneColor = new(0.8f, 0.8f, 0.2f, 1.0f); // Yellow
    private static readonly Vector4 SelectedBoneColor = new(1.0f, 0.5f, 0.0f, 1.0f); // Orange
    private static readonly Vector4 RootBoneColor = new(0.2f, 0.8f, 0.2f, 1.0f); // Green
    private static readonly Vector4 BoneLineColor = new(0.6f, 0.6f, 0.6f, 0.8f); // Gray

    private const float BonePointSize = 6.0f;
    private const float BoneLineWidth = 2.0f;
    private const float BoneSphereRadius = 0.02f;

    /// <inheritdoc />
    public string Id => "skeleton-gizmo";

    /// <inheritdoc />
    public string DisplayName => "Skeleton";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public int Order => 100; // Render after most other gizmos

    /// <inheritdoc />
    public void Render(GizmoRenderContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        var selectedIds = new HashSet<int>();
        foreach (var entity in context.SelectedEntities)
        {
            selectedIds.Add(entity.Id);
        }

        // Find all bones in the scene and draw their connections
        var drawnBones = new HashSet<int>();

        foreach (var entity in context.SceneWorld.Query<BoneReference, Transform3D>())
        {
            if (drawnBones.Contains(entity.Id))
            {
                continue;
            }

            ref readonly var boneRef = ref context.SceneWorld.Get<BoneReference>(entity);

            // Get world position of this bone
            var bonePosition = GetWorldPosition(context.SceneWorld, entity);

            // Determine if this bone or its skeleton root is selected
            var isSelected = selectedIds.Contains(entity.Id) ||
                            selectedIds.Contains(boneRef.SkeletonRootId);

            // Check if this is a root bone (no parent bone)
            var isRootBone = !HasParentBone(context.SceneWorld, entity);

            // Select appropriate color
            var color = isSelected ? SelectedBoneColor :
                       isRootBone ? RootBoneColor : BoneColor;

            // Draw bone position
            context.DrawPoint(bonePosition, color, BonePointSize);
            context.Drawer.DrawWireSphere(bonePosition, BoneSphereRadius, color);

            // Draw line to parent if exists and parent is also a bone
            var parentEntity = context.SceneWorld.GetParent(entity);
            if (parentEntity.IsValid && context.SceneWorld.Has<BoneReference>(parentEntity))
            {
                var parentPosition = GetWorldPosition(context.SceneWorld, parentEntity);
                context.DrawLine(parentPosition, bonePosition, BoneLineColor, BoneLineWidth);
            }

            drawnBones.Add(entity.Id);
        }
    }

    /// <inheritdoc />
    public bool ShouldRender(Entity entity, IWorld sceneWorld)
    {
        // Render for entities with SkinnedMesh or BoneReference
        return sceneWorld.Has<SkinnedMesh>(entity) ||
               sceneWorld.Has<BoneReference>(entity);
    }

    /// <summary>
    /// Gets the world position of a bone entity.
    /// </summary>
    private static Vector3 GetWorldPosition(IWorld world, Entity entity)
    {
        if (!world.Has<Transform3D>(entity))
        {
            return Vector3.Zero;
        }

        ref readonly var transform = ref world.Get<Transform3D>(entity);
        var localPosition = transform.Position;

        // Traverse up the hierarchy to get world position
        var parentEntity = world.GetParent(entity);
        if (parentEntity.IsValid)
        {
            var parentWorld = GetWorldMatrix(world, parentEntity);
            return Vector3.Transform(localPosition, parentWorld);
        }

        return localPosition;
    }

    /// <summary>
    /// Gets the world transformation matrix of an entity.
    /// </summary>
    private static Matrix4x4 GetWorldMatrix(IWorld world, Entity entity)
    {
        if (!world.Has<Transform3D>(entity))
        {
            return Matrix4x4.Identity;
        }

        ref readonly var transform = ref world.Get<Transform3D>(entity);

        var localMatrix = Matrix4x4.CreateScale(transform.Scale) *
                         Matrix4x4.CreateFromQuaternion(transform.Rotation) *
                         Matrix4x4.CreateTranslation(transform.Position);

        var parentEntity = world.GetParent(entity);
        if (parentEntity.IsValid)
        {
            var parentWorld = GetWorldMatrix(world, parentEntity);
            return localMatrix * parentWorld;
        }

        return localMatrix;
    }

    /// <summary>
    /// Checks if a bone entity has a parent that is also a bone.
    /// </summary>
    private static bool HasParentBone(IWorld world, Entity entity)
    {
        var parentEntity = world.GetParent(entity);
        if (!parentEntity.IsValid)
        {
            return false;
        }

        return world.Has<BoneReference>(parentEntity);
    }
}
