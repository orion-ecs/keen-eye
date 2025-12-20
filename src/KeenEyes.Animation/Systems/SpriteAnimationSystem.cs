using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that updates sprite animation playback for entities with SpriteAnimator components.
/// </summary>
/// <remarks>
/// <para>
/// This system advances the playback time and updates the current frame index
/// and source rectangle for sprite-based animations.
/// </para>
/// <para>
/// The output SourceRect can be used with I2DRenderer.DrawTextureRegion()
/// to render the correct sprite frame.
/// </para>
/// </remarks>
public sealed class SpriteAnimationSystem : SystemBase
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

        foreach (var entity in World.Query<SpriteAnimator>())
        {
            ref var animator = ref World.Get<SpriteAnimator>(entity);

            if (!animator.IsPlaying || animator.SheetId < 0)
            {
                continue;
            }

            if (!manager.TryGetSpriteSheet(animator.SheetId, out var sheet) || sheet == null)
            {
                continue;
            }

            // Advance time
            animator.Time += deltaTime * animator.Speed;

            // Get wrap mode
            var wrapMode = animator.WrapModeOverride ?? sheet.WrapMode;

            // Handle completion
            if (wrapMode == WrapMode.Once && animator.Time >= sheet.TotalDuration)
            {
                animator.Time = sheet.TotalDuration;
                animator.IsComplete = true;
                animator.IsPlaying = false;
            }
            else
            {
                animator.IsComplete = false;
            }

            // Get current frame
            var (frameIndex, frame) = sheet.GetFrameAtTime(animator.Time, wrapMode);
            animator.CurrentFrame = frameIndex;
            animator.SourceRect = frame.SourceRect;
        }
    }
}
