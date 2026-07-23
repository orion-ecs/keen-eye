using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Common;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that extracts root motion from playing animations and delivers it to the
/// skeleton root entity.
/// </summary>
/// <remarks>
/// <para>
/// For each entity with <see cref="RootMotion"/>, the system samples the root bone's
/// track at the previous and current playback times and computes the frame delta
/// (position: difference of samples; rotation: current times inverse of previous).
/// When looping playback wrapped between frames, the delta is computed as the segment
/// from the previous time to the clip end plus the segment from the clip start to the
/// current time, so looping produces continuous motion with no teleport back.
/// </para>
/// <para>
/// Under an <see cref="Animator"/> crossfade, the two clips' deltas are blended with
/// the same weight (<see cref="Animator.TransitionProgress"/>) that
/// <see cref="SkeletonPoseSystem"/> uses for the pose blend, so root velocity matches
/// the blended pose and feet do not slide during transitions.
/// </para>
/// <para>
/// The root bone's animated local translation (and rotation, when
/// <see cref="RootMotion.ApplyRotation"/> is set) is suppressed after extraction; with
/// <see cref="RootMotion.PlanarOnly"/> the bone keeps its animated Y translation. The
/// system runs at order 56, directly after <see cref="SkeletonPoseSystem"/> (order 55)
/// wrote the frame's pose and before IK (order 57) and skinning consume bone transforms.
/// </para>
/// <para>
/// Wrap compensation applies to forward <see cref="WrapMode.Loop"/> playback. Reverse
/// playback and <see cref="WrapMode.PingPong"/> use the direct sample difference, and
/// <see cref="Animator"/> state times are unwrapped so they never require compensation.
/// </para>
/// </remarks>
public sealed class RootMotionSystem : SystemBase
{
    private AnimationManager? manager;

    // Per-frame cache of skeleton roots whose root bone must be suppressed:
    // skeleton root entity id -> suppression settings.
    private readonly Dictionary<int, (string RootBoneName, bool SuppressPosition, bool KeepY, bool SuppressRotation)> suppressionCache = [];

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        World.TryGetExtension<AnimationManager>(out manager);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        manager ??= World.TryGetExtension<AnimationManager>(out var m) ? m : null;
        if (manager == null)
        {
            return;
        }

        suppressionCache.Clear();

        foreach (var entity in World.Query<RootMotion, Transform3D>())
        {
            ref var rootMotion = ref World.Get<RootMotion>(entity);

            if (!rootMotion.Enabled || string.IsNullOrEmpty(rootMotion.RootBoneName))
            {
                continue;
            }

            if (!TryExtractDelta(entity, in rootMotion, out var deltaPosition, out var deltaRotation))
            {
                continue;
            }

            ref var transform = ref World.Get<Transform3D>(entity);

            // Transform the local-space position delta into the entity's parent space
            // using the entity's current orientation, then scale and filter it.
            var entityDelta = Vector3.Zero;
            if (rootMotion.ApplyPosition)
            {
                entityDelta = Vector3.Transform(deltaPosition * rootMotion.PositionScale, transform.Rotation);
                if (rootMotion.PlanarOnly)
                {
                    entityDelta.Y = 0f;
                }
            }

            var entityDeltaRotation = Quaternion.Identity;
            if (rootMotion.ApplyRotation)
            {
                entityDeltaRotation = rootMotion.RotationScale.ApproximatelyEquals(1f)
                    ? deltaRotation
                    : Quaternion.Slerp(Quaternion.Identity, deltaRotation, rootMotion.RotationScale);
            }

            // Expose the extracted delta in both modes.
            rootMotion.DeltaPosition = entityDelta;
            rootMotion.DeltaRotation = entityDeltaRotation;

            if (rootMotion.Mode == RootMotionMode.ApplyToEntity)
            {
                transform.Position += entityDelta;
                if (rootMotion.ApplyRotation)
                {
                    transform.Rotation = Quaternion.Normalize(entityDeltaRotation * transform.Rotation);
                }
            }

            // The motion has been transferred to the entity (or exposed for a
            // controller), so the root bone's animated transform must be suppressed
            // to avoid applying it twice.
            suppressionCache[entity.Id] = (
                rootMotion.RootBoneName,
                rootMotion.ApplyPosition,
                rootMotion.PlanarOnly,
                rootMotion.ApplyRotation);
        }

        if (suppressionCache.Count == 0)
        {
            return;
        }

        // Suppress the root bones' animated local transforms written by SkeletonPoseSystem.
        foreach (var entity in World.Query<BoneReference, Transform3D>())
        {
            ref readonly var boneRef = ref World.Get<BoneReference>(entity);

            if (!suppressionCache.TryGetValue(boneRef.SkeletonRootId, out var info) ||
                !string.Equals(boneRef.BoneName, info.RootBoneName, StringComparison.Ordinal))
            {
                continue;
            }

            ref var transform = ref World.Get<Transform3D>(entity);

            if (info.SuppressPosition)
            {
                transform.Position = info.KeepY
                    ? new Vector3(0f, transform.Position.Y, 0f)
                    : Vector3.Zero;
            }

            if (info.SuppressRotation)
            {
                transform.Rotation = Quaternion.Identity;
            }
        }
    }

    private bool TryExtractDelta(Entity entity, in RootMotion rootMotion, out Vector3 deltaPosition, out Quaternion deltaRotation)
    {
        deltaPosition = Vector3.Zero;
        deltaRotation = Quaternion.Identity;

        // Single-clip playback path.
        if (World.Has<AnimationPlayer>(entity))
        {
            ref readonly var player = ref World.Get<AnimationPlayer>(entity);

            if (player.ClipId < 0 ||
                !manager!.TryGetClip(player.ClipId, out var clip) || clip == null ||
                !clip.TryGetBoneTrack(rootMotion.RootBoneName, out var track) || track == null)
            {
                return false;
            }

            var wrapMode = player.WrapModeOverride ?? clip.WrapMode;
            var playingForward = player.Speed * clip.Speed >= 0f;
            var wrapped = wrapMode == WrapMode.Loop && playingForward && player.Time < player.PreviousTime;

            ComputeTrackDelta(track, clip.Duration, player.PreviousTime, player.Time, wrapped,
                out deltaPosition, out deltaRotation);
            return true;
        }

        // Animator (state machine) path with crossfade blending.
        if (World.Has<Animator>(entity))
        {
            ref readonly var animator = ref World.Get<Animator>(entity);

            if (!animator.Enabled || animator.ControllerId < 0 ||
                !manager!.TryGetController(animator.ControllerId, out var controller) || controller == null)
            {
                return false;
            }

            AnimationClip? currentClip = null;
            if (controller.TryGetState(animator.CurrentStateHash, out var currentState) && currentState != null)
            {
                manager.TryGetClip(currentState.ClipId, out currentClip);
            }

            AnimationClip? nextClip = null;
            if (animator.NextStateHash != 0 &&
                controller.TryGetState(animator.NextStateHash, out var nextState) && nextState != null)
            {
                manager.TryGetClip(nextState.ClipId, out nextClip);
            }

            var hasCurrentDelta = false;
            var currentDelta = Vector3.Zero;
            var currentDeltaRotation = Quaternion.Identity;

            if (currentClip != null &&
                currentClip.TryGetBoneTrack(rootMotion.RootBoneName, out var currentTrack) && currentTrack != null)
            {
                // Animator state times are unwrapped, so no loop compensation is needed.
                ComputeTrackDelta(currentTrack, currentClip.Duration,
                    animator.PreviousStateTime, animator.StateTime, wrapped: false,
                    out currentDelta, out currentDeltaRotation);
                hasCurrentDelta = true;
            }

            // Mirror the SkeletonPoseSystem crossfade: blend the two clips' deltas
            // with the same weight the pose blend uses (TransitionProgress).
            if (nextClip != null && animator.TransitionProgress > 0f &&
                nextClip.TryGetBoneTrack(rootMotion.RootBoneName, out var nextTrack) && nextTrack != null)
            {
                ComputeTrackDelta(nextTrack, nextClip.Duration,
                    animator.PreviousNextStateTime, animator.NextStateTime, wrapped: false,
                    out var nextDelta, out var nextDeltaRotation);

                if (hasCurrentDelta)
                {
                    var t = animator.TransitionProgress;
                    deltaPosition = Vector3.Lerp(currentDelta, nextDelta, t);
                    deltaRotation = Quaternion.Slerp(currentDeltaRotation, nextDeltaRotation, t);
                }
                else
                {
                    deltaPosition = nextDelta;
                    deltaRotation = nextDeltaRotation;
                }

                return true;
            }

            if (hasCurrentDelta)
            {
                deltaPosition = currentDelta;
                deltaRotation = currentDeltaRotation;
                return true;
            }
        }

        return false;
    }

    private static void ComputeTrackDelta(
        BoneTrack track,
        float duration,
        float previousTime,
        float currentTime,
        bool wrapped,
        out Vector3 deltaPosition,
        out Quaternion deltaRotation)
    {
        track.Sample(previousTime, out var previousPosition, out var previousRotation, out _);
        track.Sample(currentTime, out var currentPosition, out var currentRotation, out _);

        if (wrapped)
        {
            // Playback wrapped between frames: accumulate [previousTime -> end]
            // plus [start -> currentTime] so the loop seam produces no teleport.
            track.Sample(duration, out var endPosition, out var endRotation, out _);
            track.Sample(0f, out var startPosition, out var startRotation, out _);

            deltaPosition = (endPosition - previousPosition) + (currentPosition - startPosition);
            deltaRotation = Quaternion.Normalize(
                (currentRotation * Quaternion.Inverse(startRotation)) *
                (endRotation * Quaternion.Inverse(previousRotation)));
        }
        else
        {
            deltaPosition = currentPosition - previousPosition;
            deltaRotation = Quaternion.Normalize(currentRotation * Quaternion.Inverse(previousRotation));
        }
    }
}
