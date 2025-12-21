using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Common;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that samples animation clips and applies poses to skeleton bone entities.
/// </summary>
/// <remarks>
/// <para>
/// This system queries entities with BoneReference and Transform3D components,
/// finds their skeleton root's AnimationPlayer or Animator, samples the current
/// animation pose, and writes the result to the bone's Transform3D.
/// </para>
/// <para>
/// For optimal performance, the system caches lookups per skeleton root within
/// a single frame.
/// </para>
/// </remarks>
public sealed class SkeletonPoseSystem : SystemBase
{
    private AnimationManager? manager;

    // Per-frame cache of skeleton root animations
    private readonly Dictionary<int, (AnimationClip? Clip, float Time, float Weight)> skeletonCache = [];
    private readonly Dictionary<int, (AnimationClip? Clip, float Time, AnimationClip? NextClip, float NextTime, float BlendWeight)> animatorCache = [];

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        World.TryGetExtension(out manager);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        manager ??= World.TryGetExtension<AnimationManager>(out var m) ? m : null;
        if (manager == null)
        {
            return;
        }

        // Clear per-frame caches
        skeletonCache.Clear();
        animatorCache.Clear();

        // Cache all skeleton root animation states
        CacheAnimationPlayers();
        CacheAnimators();

        // Apply poses to all bones
        foreach (var entity in World.Query<BoneReference, Transform3D>())
        {
            ref readonly var boneRef = ref World.Get<BoneReference>(entity);
            ref var transform = ref World.Get<Transform3D>(entity);

            ApplyPoseToBone(in boneRef, ref transform);
        }
    }

    private void CacheAnimationPlayers()
    {
        foreach (var entity in World.Query<AnimationPlayer>())
        {
            ref readonly var player = ref World.Get<AnimationPlayer>(entity);

            if (player.ClipId < 0)
            {
                continue;
            }

            if (!manager!.TryGetClip(player.ClipId, out var clip))
            {
                continue;
            }

            skeletonCache[entity.Id] = (clip, player.Time, player.Weight);
        }
    }

    private void CacheAnimators()
    {
        foreach (var entity in World.Query<Animator>())
        {
            ref readonly var animator = ref World.Get<Animator>(entity);

            if (!animator.Enabled || animator.ControllerId < 0)
            {
                continue;
            }

            if (!manager!.TryGetController(animator.ControllerId, out var controller) || controller == null)
            {
                continue;
            }

            // Get current state clip
            AnimationClip? currentClip = null;
            if (controller.TryGetState(animator.CurrentStateHash, out var currentState) && currentState != null)
            {
                manager.TryGetClip(currentState.ClipId, out currentClip);
            }

            // Get next state clip if transitioning
            AnimationClip? nextClip = null;
            if (animator.NextStateHash != 0)
            {
                if (controller.TryGetState(animator.NextStateHash, out var nextState) && nextState != null)
                {
                    manager.TryGetClip(nextState.ClipId, out nextClip);
                }
            }

            animatorCache[entity.Id] = (currentClip, animator.StateTime, nextClip, animator.NextStateTime, animator.TransitionProgress);
        }
    }

    private void ApplyPoseToBone(in BoneReference boneRef, ref Transform3D transform)
    {
        // Try AnimationPlayer first
        if (skeletonCache.TryGetValue(boneRef.SkeletonRootId, out var playerData))
        {
            if (playerData.Clip != null && playerData.Clip.TryGetBoneTrack(boneRef.BoneName, out var track) && track != null)
            {
                track.Sample(playerData.Time, out var position, out var rotation, out var scale);
                transform.Position = position;
                transform.Rotation = rotation;
                transform.Scale = scale;
            }
            return;
        }

        // Try Animator with blending
        if (animatorCache.TryGetValue(boneRef.SkeletonRootId, out var animatorData))
        {
            var hasCurrentPose = false;
            Vector3 currentPos = Vector3.Zero, currentScale = Vector3.One;
            Quaternion currentRot = Quaternion.Identity;

            if (animatorData.Clip != null && animatorData.Clip.TryGetBoneTrack(boneRef.BoneName, out var currentTrack) && currentTrack != null)
            {
                currentTrack.Sample(animatorData.Time, out currentPos, out currentRot, out currentScale);
                hasCurrentPose = true;
            }

            // If transitioning, blend with next pose
            if (animatorData.NextClip != null && animatorData.BlendWeight > 0f)
            {
                if (animatorData.NextClip.TryGetBoneTrack(boneRef.BoneName, out var nextTrack) && nextTrack != null)
                {
                    nextTrack.Sample(animatorData.NextTime, out var nextPos, out var nextRot, out var nextScale);

                    if (hasCurrentPose)
                    {
                        // Crossfade blend
                        var t = animatorData.BlendWeight;
                        transform.Position = Vector3.Lerp(currentPos, nextPos, t);
                        transform.Rotation = Quaternion.Slerp(currentRot, nextRot, t);
                        transform.Scale = Vector3.Lerp(currentScale, nextScale, t);
                    }
                    else
                    {
                        // Only next pose available
                        transform.Position = nextPos;
                        transform.Rotation = nextRot;
                        transform.Scale = nextScale;
                    }
                    return;
                }
            }

            // No transition, just use current pose
            if (hasCurrentPose)
            {
                transform.Position = currentPos;
                transform.Rotation = currentRot;
                transform.Scale = currentScale;
            }
        }
    }
}
