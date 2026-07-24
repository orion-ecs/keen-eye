namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Keeps the persistent player profile in step with play: folds each finished
/// run into the per-mode lifetime bests (score, depth, combo) and the Daily
/// Inferno medal history, then writes the profile to disk whenever it is dirty.
/// </summary>
/// <remarks>
/// <para>
/// Saving is edge-triggered and batched: systems that change the profile (this
/// one, and the menu's attempt/cosmetic writes in <see cref="GameFlowSystem"/>)
/// only set <see cref="ProfileState.Dirty"/>; the single disk write happens
/// here, at most once per frame, through <see cref="ProfilePersistence"/>.
/// </para>
/// <para>
/// With <see cref="ProfileState.SaveEnabled"/> false (headless
/// <c>--simulate</c> mode) the bests still update in memory — the simulation is
/// identical — but nothing ever touches the disk, keeping CI hermetic.
/// </para>
/// </remarks>
public sealed class ProfileSystem : SystemBase
{
    private bool runRecorded;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var profileState = ref World.GetSingleton<ProfileState>();
        if (profileState.Profile is not { } profile)
        {
            return;
        }

        var phase = World.GetSingleton<GameState>().Phase;

        if (phase == GamePhase.Dead && !runRecorded)
        {
            RecordFinishedRun(profile, ref profileState);
            runRecorded = true;
        }
        else if (phase != GamePhase.Dead)
        {
            runRecorded = false;
        }

        if (profileState.Dirty && profileState.SaveEnabled && profileState.SaveDirectory is { } directory)
        {
            ProfilePersistence.Save(profile, directory);
            profileState.Dirty = false;
        }
        else if (profileState.Dirty && !profileState.SaveEnabled)
        {
            // Headless: acknowledge the change without touching the disk.
            profileState.Dirty = false;
        }
    }

    private void RecordFinishedRun(PlayerProfile profile, ref ProfileState profileState)
    {
        var mode = World.GetSingleton<RunConfig>().Mode;
        var score = World.GetSingleton<ScoreState>().Score;
        var depth = World.GetSingleton<ScrollState>().Depth;
        var maxCombo = World.GetSingleton<ComboState>().MaxCombo;

        ref var best = ref profile.ModeBests[(int)mode];
        var improved = false;

        if (score > best.BestScore)
        {
            best.BestScore = score;
            improved = true;
        }

        if (depth > best.BestDepth)
        {
            best.BestDepth = depth;
            improved = true;
        }

        if (maxCombo > best.BestCombo)
        {
            best.BestCombo = maxCombo;
            improved = true;
        }

        if (mode == GameMode.DailyInferno)
        {
            var index = profile.DailyRecordIndexFor(profileState.TodayKey);
            var record = profile.DailyHistory[index];
            var medal = DailySchedule.MedalForDepth(depth);
            if (medal > record.Medal)
            {
                record.Medal = medal;
                profile.DailyHistory[index] = record;
                improved = true;
            }
        }

        if (improved)
        {
            profileState.Dirty = true;
        }
    }
}
