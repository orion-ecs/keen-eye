using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that updates tween interpolation for all tween components.
/// </summary>
/// <remarks>
/// <para>
/// This system updates TweenFloat, TweenVector2, TweenVector3, and TweenVector4
/// components, advancing their elapsed time and computing the current interpolated value.
/// </para>
/// <para>
/// The interpolated values can be read from CurrentValue and applied to other
/// components by user systems.
/// </para>
/// </remarks>
public sealed class TweenSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        UpdateFloatTweens(deltaTime);
        UpdateVector2Tweens(deltaTime);
        UpdateVector3Tweens(deltaTime);
        UpdateVector4Tweens(deltaTime);
    }

    private void UpdateFloatTweens(float deltaTime)
    {
        foreach (var entity in World.Query<TweenFloat>())
        {
            ref var tween = ref World.Get<TweenFloat>(entity);

            if (!tween.IsPlaying || tween.IsComplete)
            {
                continue;
            }

            tween.ElapsedTime += deltaTime;

            var t = ComputeProgress(ref tween.ElapsedTime, tween.Duration, tween.Loop, tween.PingPong, out var complete);
            tween.IsComplete = complete;

            if (complete && !tween.Loop)
            {
                tween.IsPlaying = false;
            }

            var easedT = Easing.Evaluate(tween.EaseType, t);
            tween.CurrentValue = tween.StartValue + (tween.EndValue - tween.StartValue) * easedT;
        }
    }

    private void UpdateVector2Tweens(float deltaTime)
    {
        foreach (var entity in World.Query<TweenVector2>())
        {
            ref var tween = ref World.Get<TweenVector2>(entity);

            if (!tween.IsPlaying || tween.IsComplete)
            {
                continue;
            }

            tween.ElapsedTime += deltaTime;

            var t = ComputeProgress(ref tween.ElapsedTime, tween.Duration, tween.Loop, tween.PingPong, out var complete);
            tween.IsComplete = complete;

            if (complete && !tween.Loop)
            {
                tween.IsPlaying = false;
            }

            var easedT = Easing.Evaluate(tween.EaseType, t);
            tween.CurrentValue = Vector2.Lerp(tween.StartValue, tween.EndValue, easedT);
        }
    }

    private void UpdateVector3Tweens(float deltaTime)
    {
        foreach (var entity in World.Query<TweenVector3>())
        {
            ref var tween = ref World.Get<TweenVector3>(entity);

            if (!tween.IsPlaying || tween.IsComplete)
            {
                continue;
            }

            tween.ElapsedTime += deltaTime;

            var t = ComputeProgress(ref tween.ElapsedTime, tween.Duration, tween.Loop, tween.PingPong, out var complete);
            tween.IsComplete = complete;

            if (complete && !tween.Loop)
            {
                tween.IsPlaying = false;
            }

            var easedT = Easing.Evaluate(tween.EaseType, t);
            tween.CurrentValue = Vector3.Lerp(tween.StartValue, tween.EndValue, easedT);
        }
    }

    private void UpdateVector4Tweens(float deltaTime)
    {
        foreach (var entity in World.Query<TweenVector4>())
        {
            ref var tween = ref World.Get<TweenVector4>(entity);

            if (!tween.IsPlaying || tween.IsComplete)
            {
                continue;
            }

            tween.ElapsedTime += deltaTime;

            var t = ComputeProgress(ref tween.ElapsedTime, tween.Duration, tween.Loop, tween.PingPong, out var complete);
            tween.IsComplete = complete;

            if (complete && !tween.Loop)
            {
                tween.IsPlaying = false;
            }

            var easedT = Easing.Evaluate(tween.EaseType, t);
            tween.CurrentValue = Vector4.Lerp(tween.StartValue, tween.EndValue, easedT);
        }
    }

    private static float ComputeProgress(ref float elapsed, float duration, bool loop, bool pingPong, out bool complete)
    {
        complete = false;

        if (duration <= 0f)
        {
            complete = true;
            return 1f;
        }

        if (elapsed >= duration)
        {
            if (loop)
            {
                if (pingPong)
                {
                    // Ping-pong: calculate which cycle we're in and direction
                    var cycles = (int)(elapsed / duration);
                    elapsed %= duration;
                    return (cycles % 2 == 0) ? elapsed / duration : 1f - elapsed / duration;
                }
                else
                {
                    elapsed %= duration;
                    return elapsed / duration;
                }
            }
            else
            {
                complete = true;
                elapsed = duration;
                return 1f;
            }
        }

        return elapsed / duration;
    }
}
