using System.Globalization;
using KeenEyes.Input.Abstractions;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Drives the Ready → Playing → Dead → Ready loop and the Ready-screen menu:
/// Left/Right (the one sacred axis) cycles the active row — game mode, or
/// cosmetic style when Tab has switched rows — and Space/Enter dives. Also
/// enforces Daily Inferno's attempt budget and its three-minute time limit.
/// </summary>
/// <remarks>
/// <para>
/// Mode changes take effect immediately: the shaft is rebuilt with the new
/// mode's settings and seed while still on the Ready screen, so the menu is a
/// live preview of the run you are about to take.
/// </para>
/// <para>
/// Without an input context (headless <c>--simulate</c> mode) the menu takes no
/// actions; the harness drives <see cref="GameState"/> directly. The time limit,
/// being a pure music-clock comparison, applies identically in both. Devices are
/// resolved through <see cref="InputDevices"/>, so the loop runs on keyboard alone
/// when no controller is attached.
/// </para>
/// </remarks>
public sealed class GameFlowSystem : SystemBase
{
    private bool deathAnnounced;
    private float restartCooldown;
    private bool leftWasDown;
    private bool rightWasDown;
    private bool tabWasDown;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var state = ref World.GetSingleton<GameState>();

        switch (state.Phase)
        {
            case GamePhase.Ready:
                UpdateMenu();

                if (IsStartPressed() && TryConsumeAttempt())
                {
                    state.Phase = GamePhase.Playing;
                }

                break;

            case GamePhase.Playing:
                UpdateTimeLimit(ref state);
                break;

            case GamePhase.Dead:
                if (!deathAnnounced)
                {
                    AnnounceDeath();
                    deathAnnounced = true;
                    restartCooldown = Tuning.RestartCooldown;
                }

                // Brief cooldown so the key that killed you cannot instantly
                // skip the death screen.
                restartCooldown -= deltaTime;
                if (restartCooldown <= 0f && (IsStartPressed() || IsSteerPressed()))
                {
                    // Back to the Ready menu with the next run already staged,
                    // so mode and style can be changed between dives.
                    GameSetup.StartRun(World, NextSeed());
                    state.Phase = GamePhase.Ready;
                    deathAnnounced = false;
                }

                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Ends a timed run (Daily Inferno) when the music clock passes the mode's
    /// duration limit — the same death path as the crusher, minus the crushing.
    /// </summary>
    private void UpdateTimeLimit(ref GameState state)
    {
        var settings = World.GetSingleton<RunConfig>().Settings;
        if (settings.DurationLimitSeconds <= 0f
            || World.GetSingleton<MusicClock>().Seconds < settings.DurationLimitSeconds)
        {
            return;
        }

        state.Phase = GamePhase.Dead;
        World.GetSingleton<MenuState>().LastRunTimedOut = true;

        ref var score = ref World.GetSingleton<ScoreState>();
        score.Best = Math.Max(score.Best, (int)score.Score);

        ref var heat = ref World.GetSingleton<HeatState>();
        heat.Heat = 0f;
        heat.Tier = 0;
    }

    /// <summary>
    /// Handles the Ready-screen menu input: Left/Right cycles the active row,
    /// Tab switches between the mode row and the cosmetics row.
    /// </summary>
    private void UpdateMenu()
    {
        if (!World.TryGetExtension<IInputContext>(out var input))
        {
            return;
        }

        var keyboard = InputDevices.FirstKeyboard(input);
        if (keyboard is null)
        {
            return;
        }

        var leftDown = keyboard.IsKeyDown(Key.A) || keyboard.IsKeyDown(Key.Left);
        var rightDown = keyboard.IsKeyDown(Key.D) || keyboard.IsKeyDown(Key.Right);
        var tabDown = keyboard.IsKeyDown(Key.Tab);

        var step = 0;
        if (leftDown && !leftWasDown)
        {
            step = -1;
        }
        else if (rightDown && !rightWasDown)
        {
            step = 1;
        }

        ref var menu = ref World.GetSingleton<MenuState>();

        if (tabDown && !tabWasDown)
        {
            menu.Row = menu.Row == MenuRow.Mode ? MenuRow.Cosmetics : MenuRow.Mode;
        }

        if (step != 0)
        {
            if (menu.Row == MenuRow.Mode)
            {
                CycleMode(ref menu, step);
            }
            else
            {
                CycleCosmetic(step);
            }
        }

        leftWasDown = leftDown;
        rightWasDown = rightDown;
        tabWasDown = tabDown;
    }

    /// <summary>
    /// Selects the previous/next mode and immediately restages the run with the
    /// new mode's settings and seed.
    /// </summary>
    private void CycleMode(ref MenuState menu, int step)
    {
        var index = Array.IndexOf(ModeCatalog.All, menu.SelectedMode);
        index = (index + step + ModeCatalog.All.Length) % ModeCatalog.All.Length;
        menu.SelectedMode = ModeCatalog.All[index];

        ref var runConfig = ref World.GetSingleton<RunConfig>();
        runConfig.Mode = menu.SelectedMode;
        runConfig.Settings = ModeSettings.For(menu.SelectedMode);

        // Daily Inferno always runs today's shared seed; the other modes keep
        // whatever seed the session was on (pinned or rolling).
        var seed = runConfig.Seed;
        if (menu.SelectedMode == GameMode.DailyInferno)
        {
            seed = SeededGenerator.NextSeed((ulong)World.GetSingleton<ProfileState>().TodayKey);
        }

        GameSetup.StartRun(World, seed);
    }

    /// <summary>
    /// Selects the previous/next UNLOCKED cosmetic style and marks the profile
    /// dirty so the choice persists.
    /// </summary>
    private void CycleCosmetic(int step)
    {
        ref var profileState = ref World.GetSingleton<ProfileState>();
        if (profileState.Profile is not { } profile)
        {
            return;
        }

        var count = CosmeticStyles.All.Length;
        var index = profile.SelectedStyle;
        for (var i = 0; i < count; i++)
        {
            index = (index + step + count) % count;
            if (CosmeticStyles.IsUnlocked(index, profile))
            {
                break;
            }
        }

        if (index != profile.SelectedStyle)
        {
            profile.SelectedStyle = index;
            profileState.Dirty = true;
        }
    }

    /// <summary>
    /// Consumes a Daily Inferno attempt, or refuses the start when today's
    /// budget is spent. Non-daily modes always start.
    /// </summary>
    private bool TryConsumeAttempt()
    {
        if (World.GetSingleton<RunConfig>().Mode != GameMode.DailyInferno)
        {
            return true;
        }

        ref var profileState = ref World.GetSingleton<ProfileState>();
        if (profileState.Profile is not { } profile)
        {
            return true;
        }

        var index = profile.DailyRecordIndexFor(profileState.TodayKey);
        var record = profile.DailyHistory[index];
        if (record.AttemptsUsed >= Tuning.DailyAttemptsPerDay)
        {
            return false;
        }

        record.AttemptsUsed++;
        profile.DailyHistory[index] = record;
        profileState.Dirty = true;
        return true;
    }

    private bool IsStartPressed()
    {
        if (!World.TryGetExtension<IInputContext>(out var input))
        {
            return false;
        }

        var keyboard = InputDevices.FirstKeyboard(input);
        if (keyboard is not null && (keyboard.IsKeyDown(Key.Space) || keyboard.IsKeyDown(Key.Enter)))
        {
            return true;
        }

        // Space/Enter is the whole control scheme; the South button is a bonus for
        // whoever brought a controller, so its absence must not block the dive.
        var gamepad = InputDevices.FirstConnectedGamepad(input);
        return gamepad is not null && gamepad.IsButtonDown(GamepadButton.South);
    }

    private bool IsSteerPressed()
    {
        if (!World.TryGetExtension<IInputContext>(out var input))
        {
            return false;
        }

        var keyboard = InputDevices.FirstKeyboard(input);
        return keyboard is not null
            && (keyboard.IsKeyDown(Key.A) || keyboard.IsKeyDown(Key.D)
                || keyboard.IsKeyDown(Key.Left) || keyboard.IsKeyDown(Key.Right));
    }

    /// <summary>
    /// Picks the seed for the run after a death, per the mode's rules.
    /// </summary>
    private ulong NextSeed()
    {
        var runConfig = World.GetSingleton<RunConfig>();
        if (runConfig.Mode == GameMode.DailyInferno)
        {
            // Everyone plays the same shaft all day.
            return SeededGenerator.NextSeed((ulong)World.GetSingleton<ProfileState>().TodayKey);
        }

        return runConfig.PinSeed ? runConfig.Seed : SeededGenerator.NextSeed(runConfig.Seed);
    }

    private void AnnounceDeath()
    {
        var score = World.GetSingleton<ScoreState>();
        var depth = World.GetSingleton<ScrollState>().Depth;
        var timedOut = World.GetSingleton<MenuState>().LastRunTimedOut;

        var cause = timedOut ? "Time." : "The Furnace claims you.";
        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{cause} Score {score.Score:F0} at {depth:F0}m (best {score.Best}). Press A/D for the menu.");
        Console.WriteLine(summary);

        if (World.TryGetExtension<ISilkWindowProvider>(out var windowProvider))
        {
            windowProvider.Window.Title = $"NOVAFALL — {summary}";
        }
    }
}
