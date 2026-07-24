using System.Numerics;
using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Tween-driven squash and stretch: landing slams the ball into a pancake that
/// elastic-eases back to round; a Floor Smash gives a shorter, snappier squash.
/// The render system multiplies this recovery scale with the continuous
/// velocity-driven teardrop stretch.
/// </summary>
/// <remarks>
/// Phase A faked squash purely from instantaneous velocity, which meant the ball
/// snapped back to round the moment it stopped. The tween upgrade gives impacts
/// a MEMORY: the pancake overshoots and wobbles back over ~half a second via
/// <c>EaseType.ElasticOut</c>, which is what sells weight. The tween lives in a
/// <c>TweenVector2</c> (X = width scale, Y = height scale) on the ball entity;
/// <c>KeenEyes.Animation.TweenSystem</c> advances it, this system only retargets
/// it on impact events.
/// </remarks>
public sealed class SquashStretchSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        if (!juice.PresentationAvailable || !juice.Enabled
            || !World.TryGetExtension<AnimationManager>(out _))
        {
            return;
        }

        ref readonly var events = ref World.GetSingleton<FrameEvents>();
        if (!events.Landed && !events.Smashed)
        {
            return;
        }

        // Exactly one ball; resolve it before any structural change.
        var ballEntity = default(Entity);
        var ballFound = false;
        foreach (var entity in World.Query<Ball, Position2D>())
        {
            ballEntity = entity;
            ballFound = true;
            break;
        }

        if (!ballFound)
        {
            return;
        }

        TweenVector2 tween;
        if (events.Landed)
        {
            // Pancake amount scales with how hard the landing was.
            var impact = Math.Clamp(events.LandingSpeed / Tuning.MaxFallSpeed, 0f, 1f);
            var pancake = new Vector2(1f + 0.55f * impact, 1f - 0.42f * impact);
            tween = TweenVector2.Create(pancake, Vector2.One, duration: 0.55f, EaseType.ElasticOut);
        }
        else
        {
            // Smash: a quick punch-through squash that recovers with a snap.
            tween = TweenVector2.Create(new Vector2(1.35f, 0.70f), Vector2.One, duration: 0.35f, EaseType.BackOut);
        }

        if (World.Has<TweenVector2>(ballEntity))
        {
            World.Get<TweenVector2>(ballEntity) = tween;
        }
        else
        {
            World.Add(ballEntity, tween);
        }
    }
}
