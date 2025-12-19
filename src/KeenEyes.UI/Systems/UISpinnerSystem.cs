using System.Numerics;

using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles spinner animation and progress bar updates.
/// </summary>
/// <remarks>
/// <para>
/// This system manages:
/// <list type="bullet">
/// <item>Spinner rotation animation based on speed</item>
/// <item>Progress bar value interpolation</item>
/// </list>
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase to update
/// animations before layout and rendering.
/// </para>
/// </remarks>
public sealed class UISpinnerSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        UpdateSpinners(deltaTime);
        UpdateProgressBars(deltaTime);
    }

    private void UpdateSpinners(float deltaTime)
    {
        foreach (var entity in World.Query<UISpinner>())
        {
            // Skip hidden elements
            if (World.Has<UIHiddenTag>(entity))
            {
                continue;
            }

            ref var spinner = ref World.Get<UISpinner>(entity);

            // Update rotation angle
            spinner.CurrentAngle += spinner.Speed * deltaTime;

            // Keep angle in [0, 2*PI] range to avoid floating point overflow
            const float TwoPi = MathF.PI * 2;
            if (spinner.CurrentAngle > TwoPi)
            {
                spinner.CurrentAngle -= TwoPi;
            }
            else if (spinner.CurrentAngle < 0)
            {
                spinner.CurrentAngle += TwoPi;
            }
        }
    }

    private void UpdateProgressBars(float deltaTime)
    {
        foreach (var entity in World.Query<UIProgressBar>())
        {
            // Skip hidden elements
            if (World.Has<UIHiddenTag>(entity))
            {
                continue;
            }

            ref var progressBar = ref World.Get<UIProgressBar>(entity);

            // Skip if already at target value
            if (Math.Abs(progressBar.AnimatedValue - progressBar.Value) < 0.001f)
            {
                progressBar.AnimatedValue = progressBar.Value;
                continue;
            }

            // Interpolate toward target value
            float delta = progressBar.Value - progressBar.AnimatedValue;
            float step = progressBar.AnimationSpeed * deltaTime;

            if (Math.Abs(delta) <= step)
            {
                progressBar.AnimatedValue = progressBar.Value;
            }
            else
            {
                progressBar.AnimatedValue += Math.Sign(delta) * step;
            }
        }
    }

    /// <summary>
    /// Sets the progress value of a progress bar.
    /// </summary>
    /// <param name="entity">The progress bar entity.</param>
    /// <param name="value">The new progress value (0 to 1).</param>
    public void SetProgress(Entity entity, float value)
    {
        if (!World.IsAlive(entity) || !World.Has<UIProgressBar>(entity))
        {
            return;
        }

        ref var progressBar = ref World.Get<UIProgressBar>(entity);
        progressBar.Value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Sets the progress value immediately without animation.
    /// </summary>
    /// <param name="entity">The progress bar entity.</param>
    /// <param name="value">The new progress value (0 to 1).</param>
    public void SetProgressImmediate(Entity entity, float value)
    {
        if (!World.IsAlive(entity) || !World.Has<UIProgressBar>(entity))
        {
            return;
        }

        ref var progressBar = ref World.Get<UIProgressBar>(entity);
        progressBar.Value = Math.Clamp(value, 0f, 1f);
        progressBar.AnimatedValue = progressBar.Value;
    }

    /// <summary>
    /// Resets a spinner's rotation to zero.
    /// </summary>
    /// <param name="entity">The spinner entity.</param>
    public void ResetSpinner(Entity entity)
    {
        if (!World.IsAlive(entity) || !World.Has<UISpinner>(entity))
        {
            return;
        }

        ref var spinner = ref World.Get<UISpinner>(entity);
        spinner.CurrentAngle = 0;
    }

    /// <summary>
    /// Sets the speed of a spinner.
    /// </summary>
    /// <param name="entity">The spinner entity.</param>
    /// <param name="speed">The new rotation speed in radians per second.</param>
    public void SetSpinnerSpeed(Entity entity, float speed)
    {
        if (!World.IsAlive(entity) || !World.Has<UISpinner>(entity))
        {
            return;
        }

        ref var spinner = ref World.Get<UISpinner>(entity);
        spinner.Speed = speed;
    }
}
