namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Resolves the ball touching the Furnace ceiling: death — unless the run's
/// one Adrenaline Save is still unspent, in which case the kill converts into
/// a 20%-speed last chance (see <see cref="AdrenalineSystem"/>). In Ember
/// Garden the crusher is configured off and the ceiling merely clamps.
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

        var settings = World.GetSingleton<RunConfig>().Settings;

        foreach (var entity in World.Query<Ball, Position2D>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref var position = ref World.Get<Position2D>(entity);

            if (position.Y - ball.Radius > Tuning.CeilingY)
            {
                break;
            }

            // Ember Garden: no crusher, no death — the ceiling is just a wall.
            if (!settings.CrusherEnabled)
            {
                position.Y = Tuning.CeilingY + ball.Radius;
                break;
            }

            ref var adrenaline = ref World.GetSingleton<AdrenalineState>();

            // The frame this would kill, the unspent Adrenaline Save fires
            // instead: time snaps to 20% for 1.5 real seconds — one last steer.
            if (settings.AdrenalineEnabled && adrenaline.Available)
            {
                adrenaline.Available = false;
                adrenaline.Active = true;
                adrenaline.RealSecondsRemaining = Tuning.AdrenalineRealSeconds;
                World.GetSingleton<TimeScale>().Value = Tuning.AdrenalineTimeScale;
                World.GetSingleton<FrameEvents>().AdrenalineTriggered = true;
                World.GetSingleton<RunEventCounters>().AdrenalineSavesUsed++;
                break;
            }

            // While the save window is open the Furnace holds its breath;
            // when it expires with the ball still here, this branch stops
            // being taken and the next line runs.
            if (adrenaline.Active)
            {
                break;
            }

            state.Phase = GamePhase.Dead;

            ref var score = ref World.GetSingleton<ScoreState>();
            score.Best = Math.Max(score.Best, (int)score.Score);

            // Only death resets heat.
            ResetHeat();
            break;
        }
    }

    private void ResetHeat()
    {
        ref var heat = ref World.GetSingleton<HeatState>();
        heat.Heat = 0f;
        heat.Tier = 0;
    }
}
