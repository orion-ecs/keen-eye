namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Ends the run when the ball touches the Furnace ceiling.
/// </summary>
public sealed class CrushSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var state = ref World.GetSingleton<GameState>();
        if (state.Phase != GamePhase.Playing)
        {
            return;
        }

        foreach (var entity in World.Query<Ball, Position2D>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);

            if (position.Y - ball.Radius <= Tuning.CeilingY)
            {
                state.Phase = GamePhase.Dead;

                ref var score = ref World.GetSingleton<ScoreState>();
                score.Best = Math.Max(score.Best, (int)score.Score);

                // Only death resets heat.
                ResetHeat();
                break;
            }
        }
    }

    private void ResetHeat()
    {
        ref var heat = ref World.GetSingleton<HeatState>();
        heat.Heat = 0f;
        heat.Tier = 0;
    }
}
