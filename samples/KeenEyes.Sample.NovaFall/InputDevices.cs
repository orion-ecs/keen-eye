using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Device lookups that survive absent hardware, so NOVAFALL is fully playable on a
/// keyboard with no controller plugged in.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IInputContext.Gamepad"/> and <see cref="IInputContext.Keyboard"/> return the
/// primary device and throw when there is none. A gamepad is optional hardware that most
/// machines do not have, so reading <c>input.Gamepad</c> in a per-frame system is a crash
/// waiting for its first player without a controller — and checking
/// <c>input.Gamepad.IsConnected</c> does not help, because the getter has already thrown.
/// </para>
/// <para>
/// The plural collections (<see cref="IInputContext.Gamepads"/>,
/// <see cref="IInputContext.Keyboards"/>) are always safe to enumerate, so "first connected
/// device, or none" is the pattern every game should use for optional input. Both helpers
/// return <see langword="null"/> instead of throwing, which lets each caller decide what
/// missing hardware means for it.
/// </para>
/// </remarks>
public static class InputDevices
{
    /// <summary>
    /// Gets the first connected gamepad, or <see langword="null"/> when no controller is attached.
    /// </summary>
    /// <param name="input">The input context to search.</param>
    /// <returns>The first connected gamepad, or <see langword="null"/> if there is none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static IGamepad? FirstConnectedGamepad(IInputContext input)
    {
        ArgumentNullException.ThrowIfNull(input);

        foreach (var gamepad in input.Gamepads)
        {
            if (gamepad.IsConnected)
            {
                return gamepad;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the first keyboard, or <see langword="null"/> when the input backend reports none.
    /// </summary>
    /// <param name="input">The input context to search.</param>
    /// <returns>The first keyboard, or <see langword="null"/> if there is none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    /// <remarks>
    /// A keyboard is nearly always present, but it does not exist before the window has
    /// loaded. Going through the collection keeps the systems below free of ordering
    /// assumptions about when they first run.
    /// </remarks>
    public static IKeyboard? FirstKeyboard(IInputContext input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.Keyboards.Length > 0 ? input.Keyboards[0] : null;
    }
}
