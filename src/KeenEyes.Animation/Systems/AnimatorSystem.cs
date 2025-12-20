using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that manages animator state machines, handling state transitions and crossfading.
/// </summary>
/// <remarks>
/// <para>
/// This system processes Animator components and manages their state machine logic:
/// - Advances state time
/// - Processes triggered state transitions
/// - Handles crossfade blending between states
/// </para>
/// <para>
/// Animation sampling and bone updates are handled by separate systems.
/// </para>
/// </remarks>
public sealed class AnimatorSystem : SystemBase
{
    private AnimationManager? manager;

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

        foreach (var entity in World.Query<Animator>())
        {
            ref var animator = ref World.Get<Animator>(entity);

            if (!animator.Enabled || animator.ControllerId < 0)
            {
                continue;
            }

            if (!manager.TryGetController(animator.ControllerId, out var controller) || controller == null)
            {
                continue;
            }

            // Initialize to default state if not set
            if (animator.CurrentStateHash == 0)
            {
                animator.CurrentStateHash = controller.DefaultStateHash;
            }

            // Get current state
            if (!controller.TryGetState(animator.CurrentStateHash, out var currentState) || currentState == null)
            {
                continue;
            }

            // Process triggered state change
            if (animator.TriggerStateHash != 0)
            {
                if (controller.TryGetState(animator.TriggerStateHash, out var targetState) && targetState != null)
                {
                    // Find transition to target state
                    AnimatorTransition? transition = null;
                    foreach (var t in currentState.Transitions)
                    {
                        if (t.TargetStateHash == animator.TriggerStateHash)
                        {
                            transition = t;
                            break;
                        }
                    }

                    if (transition.HasValue)
                    {
                        // Start transition with crossfade
                        animator.NextStateHash = animator.TriggerStateHash;
                        animator.TransitionDuration = transition.Value.Duration;
                        animator.TransitionProgress = 0f;
                        animator.NextStateTime = 0f;
                    }
                    else
                    {
                        // Immediate transition (no defined crossfade)
                        animator.CurrentStateHash = animator.TriggerStateHash;
                        animator.StateTime = 0f;
                        animator.NextStateHash = 0;
                    }
                }

                animator.TriggerStateHash = 0;
            }

            // Update state time
            var speed = animator.Speed * currentState.Speed;
            animator.StateTime += deltaTime * speed;

            // Update transition
            if (animator.NextStateHash != 0)
            {
                animator.TransitionProgress += deltaTime / animator.TransitionDuration;
                animator.NextStateTime += deltaTime * speed;

                if (animator.TransitionProgress >= 1f)
                {
                    // Complete transition
                    animator.CurrentStateHash = animator.NextStateHash;
                    animator.StateTime = animator.NextStateTime;
                    animator.NextStateHash = 0;
                    animator.TransitionProgress = 0f;
                    animator.NextStateTime = 0f;
                }
            }

            // Check for automatic transitions based on exit time
            if (animator.NextStateHash == 0)
            {
                CheckAutoTransitions(ref animator, currentState, controller);
            }
        }
    }

    private void CheckAutoTransitions(ref Animator animator, AnimatorState currentState, AnimatorController controller)
    {
        // Get clip duration for normalized time calculation
        AnimationClip? clip = null;
        if (currentState.ClipId >= 0)
        {
            manager?.TryGetClip(currentState.ClipId, out clip);
        }

        var duration = clip?.Duration ?? 1f;
        var normalizedTime = duration > 0f ? animator.StateTime / duration : 0f;

        foreach (var transition in currentState.Transitions)
        {
            if (!transition.ExitTime.HasValue)
            {
                continue;
            }

            // Check if we've passed the exit time
            if (normalizedTime >= transition.ExitTime.Value)
            {
                if (controller.TryGetState(transition.TargetStateHash, out var targetState) && targetState != null)
                {
                    animator.NextStateHash = transition.TargetStateHash;
                    animator.TransitionDuration = transition.Duration;
                    animator.TransitionProgress = 0f;
                    animator.NextStateTime = 0f;
                    break;
                }
            }
        }
    }
}
