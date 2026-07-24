using System.Globalization;
using KeenEyes.Input.Abstractions;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Drives the Ready → Playing → Dead → Playing loop: starts the run on any steer
/// key, announces the final score on death, and restarts with a fresh seed
/// (unless <see cref="RunConfig.PinSeed"/> pins one).
/// </summary>
/// <remarks>
/// Without an input context (headless <c>--simulate</c> mode) this system takes no
/// actions; the harness drives <see cref="GameState"/> directly.
/// </remarks>
public sealed class GameFlowSystem : SystemBase
{
    private bool deathAnnounced;
    private float restartCooldown;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var state = ref World.GetSingleton<GameState>();

        switch (state.Phase)
        {
            case GamePhase.Ready:
                if (IsStartPressed())
                {
                    state.Phase = GamePhase.Playing;
                }

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
                if (restartCooldown <= 0f && IsStartPressed())
                {
                    var runConfig = World.GetSingleton<RunConfig>();
                    var seed = runConfig.PinSeed
                        ? runConfig.Seed
                        : SeededGenerator.NextSeed(runConfig.Seed);

                    GameSetup.StartRun(World, seed);
                    state.Phase = GamePhase.Playing;
                    deathAnnounced = false;
                }

                break;

            case GamePhase.Playing:
            default:
                break;
        }
    }

    private bool IsStartPressed()
    {
        if (!World.TryGetExtension<IInputContext>(out var input))
        {
            return false;
        }

        var keyboard = input.Keyboard;
        if (keyboard.IsKeyDown(Key.Space) || keyboard.IsKeyDown(Key.Enter)
            || keyboard.IsKeyDown(Key.A) || keyboard.IsKeyDown(Key.D)
            || keyboard.IsKeyDown(Key.Left) || keyboard.IsKeyDown(Key.Right))
        {
            return true;
        }

        var gamepad = input.Gamepad;
        return gamepad.IsConnected && gamepad.IsButtonDown(GamepadButton.South);
    }

    private void AnnounceDeath()
    {
        var score = World.GetSingleton<ScoreState>();
        var depth = World.GetSingleton<ScrollState>().Depth;

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"The Furnace claims you. Score {score.Score:F0} at {depth:F0}m (best {score.Best}). Press A/D to dive again.");
        Console.WriteLine(summary);

        if (World.TryGetExtension<ISilkWindowProvider>(out var windowProvider))
        {
            windowProvider.Window.Title = $"NOVAFALL — {summary}";
        }
    }
}
