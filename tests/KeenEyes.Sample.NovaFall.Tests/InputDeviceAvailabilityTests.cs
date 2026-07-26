using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Pins down that NOVAFALL plays on a keyboard with no controller attached, and still uses a
/// controller when one is.
/// </summary>
/// <remarks>
/// Regression coverage for #1256, where a Windows laptop with no gamepad could not run the
/// sample at all: <see cref="InputSteerSystem"/> and <see cref="GameFlowSystem"/> read
/// <see cref="IInputContext.Gamepad"/> unguarded, that getter throws when nothing is plugged
/// in, and the exception escaped frame 1 of the loop. A gamepad is optional hardware, so both
/// systems must run on keyboard alone.
/// </remarks>
public class InputDeviceAvailabilityTests
{
    private const float FixedDeltaTime = 1f / 60f;

    #region Test double contract

    [Fact]
    public void MockInputContext_WithZeroGamepads_ThrowsFromPrimaryGamepad()
    {
        // The guard below is only meaningful if the double behaves like the hardware backend:
        // a machine with no controller has no primary gamepad to hand out. If this ever stops
        // throwing, the keyboard-only tests below would pass for the wrong reason.
        using var input = new MockInputContext(gamepadCount: 0);

        Assert.Equal(0, input.ConnectedGamepadCount);
        Assert.Empty(input.Gamepads);
        Assert.Throws<InvalidOperationException>(() => input.Gamepad);
    }

    #endregion

    #region InputSteerSystem

    [Theory]
    [InlineData(Key.A, -1f)]
    [InlineData(Key.Left, -1f)]
    [InlineData(Key.D, 1f)]
    [InlineData(Key.Right, 1f)]
    public void InputSteer_WithNoGamepadConnected_SteersFromKeyboard(Key key, float expectedAxis)
    {
        using var world = CreateWorld(gamepadCount: 0, out var input);
        input.SetKeyDown(key);

        var system = new InputSteerSystem();
        system.Initialize(world);

        var exception = Record.Exception(() => system.Update(FixedDeltaTime));

        Assert.Null(exception);
        Assert.True(
            SteerAxis(world).ApproximatelyEquals(expectedAxis),
            $"Expected steer axis {expectedAxis} but got {SteerAxis(world)}.");
    }

    [Fact]
    public void InputSteer_WithNoGamepadConnectedAndNoKeysHeld_LeavesAxisAtZero()
    {
        using var world = CreateWorld(gamepadCount: 0, out _);

        var system = new InputSteerSystem();
        system.Initialize(world);

        var exception = Record.Exception(() => system.Update(FixedDeltaTime));

        Assert.Null(exception);
        Assert.True(SteerAxis(world).IsApproximatelyZero());
    }

    [Fact]
    public void InputSteer_WithGamepadConnected_SteersFromLeftStick()
    {
        using var world = CreateWorld(gamepadCount: 1, out var input);
        input.SetGamepadStick(0, isLeft: true, 1f, 0f);

        var system = new InputSteerSystem();
        system.Initialize(world);
        system.Update(FixedDeltaTime);

        Assert.True(SteerAxis(world).ApproximatelyEquals(1f));
    }

    [Fact]
    public void InputSteer_WithGamepadConnectedAndKeyHeld_PrefersTheKeyboard()
    {
        using var world = CreateWorld(gamepadCount: 1, out var input);
        input.SetGamepadStick(0, isLeft: true, 1f, 0f);
        input.SetKeyDown(Key.A);

        var system = new InputSteerSystem();
        system.Initialize(world);
        system.Update(FixedDeltaTime);

        Assert.True(SteerAxis(world).ApproximatelyEquals(-1f));
    }

    #endregion

    #region GameFlowSystem

    [Fact]
    public void GameFlow_WithNoGamepadConnected_StartsTheRunFromSpace()
    {
        using var world = CreateWorld(gamepadCount: 0, out var input);
        input.SetKeyDown(Key.Space);

        var system = new GameFlowSystem();
        system.Initialize(world);

        var exception = Record.Exception(() => system.Update(FixedDeltaTime));

        Assert.Null(exception);
        Assert.Equal(GamePhase.Playing, world.GetSingleton<GameState>().Phase);
    }

    [Fact]
    public void GameFlow_WithNoGamepadConnected_CyclesTheMenuFromTheKeyboard()
    {
        using var world = CreateWorld(gamepadCount: 0, out var input);
        var startingMode = world.GetSingleton<MenuState>().SelectedMode;

        var system = new GameFlowSystem();
        system.Initialize(world);

        // Edge-detected input: one idle frame establishes "up", then the press registers.
        system.Update(FixedDeltaTime);
        input.SetKeyDown(Key.D);
        var exception = Record.Exception(() => system.Update(FixedDeltaTime));

        Assert.Null(exception);
        Assert.NotEqual(startingMode, world.GetSingleton<MenuState>().SelectedMode);
        Assert.Equal(GamePhase.Ready, world.GetSingleton<GameState>().Phase);
    }

    [Fact]
    public void GameFlow_WithNoGamepadConnectedAndNothingPressed_StaysOnTheReadyScreen()
    {
        using var world = CreateWorld(gamepadCount: 0, out _);

        var system = new GameFlowSystem();
        system.Initialize(world);

        var exception = Record.Exception(() =>
        {
            for (var frame = 0; frame < 5; frame++)
            {
                system.Update(FixedDeltaTime);
            }
        });

        Assert.Null(exception);
        Assert.Equal(GamePhase.Ready, world.GetSingleton<GameState>().Phase);
    }

    [Fact]
    public void GameFlow_WithGamepadConnected_StartsTheRunFromTheSouthButton()
    {
        using var world = CreateWorld(gamepadCount: 1, out var input);
        input.SetGamepadButton(0, GamepadButton.South, isDown: true);

        var system = new GameFlowSystem();
        system.Initialize(world);
        system.Update(FixedDeltaTime);

        Assert.Equal(GamePhase.Playing, world.GetSingleton<GameState>().Phase);
    }

    #endregion

    #region JuiceToggleSystem

    [Fact]
    public void JuiceToggle_WithNoGamepadConnected_TogglesFromTheKeyboard()
    {
        using var world = CreateWorld(gamepadCount: 0, out var input);
        var system = new JuiceToggleSystem();
        system.Initialize(world);

        input.SetKeyDown(Key.J);
        var exception = Record.Exception(() => system.Update(FixedDeltaTime));

        Assert.Null(exception);
        Assert.False(world.GetSingleton<JuiceConfig>().Enabled);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Builds a Ready-screen world whose only input device set is a keyboard, a mouse, and
    /// <paramref name="gamepadCount"/> gamepad slots — zero models the reported machine.
    /// </summary>
    private static World CreateWorld(int gamepadCount, out MockInputContext input)
    {
        var world = new World();

        // presentation: true is the windowed path, the one that reads real input devices.
        GameSetup.InitializeSingletons(world, seed: 1319914546, pinSeed: true, presentation: true);
        GameSetup.StartRun(world, seed: 1319914546);

        input = new MockInputContext(gamepadCount);

        // owned: false - the context outlives nothing here; the test disposes the world only.
        world.SetExtension<IInputContext>(input, owned: false);

        return world;
    }

    private static float SteerAxis(World world)
    {
        foreach (var entity in world.Query<Ball, SteerInput>())
        {
            return world.Get<SteerInput>(entity).Axis;
        }

        throw new InvalidOperationException("The run has no ball to steer.");
    }

    #endregion
}
