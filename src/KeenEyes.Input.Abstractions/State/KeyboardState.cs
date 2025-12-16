using System.Collections.Immutable;

namespace KeenEyes.Input.Abstractions;

/// <summary>
/// A snapshot of keyboard state at a specific point in time.
/// </summary>
/// <remarks>
/// <para>
/// This readonly struct captures the complete keyboard state for polling-based input.
/// It is designed for zero-allocation state queries in hot paths.
/// </para>
/// <para>
/// Use <see cref="IsKeyDown"/> and <see cref="IsKeyUp"/> for efficient state queries,
/// or <see cref="PressedKeys"/> for iterating over all currently pressed keys.
/// </para>
/// </remarks>
/// <param name="PressedKeys">The set of keys currently pressed.</param>
/// <param name="Modifiers">The current modifier key state.</param>
public readonly record struct KeyboardState(
    ImmutableHashSet<Key> PressedKeys,
    KeyModifiers Modifiers)
{
    /// <summary>
    /// An empty keyboard state with no keys pressed.
    /// </summary>
    public static readonly KeyboardState Empty = new([], KeyModifiers.None);

    /// <summary>
    /// Gets whether the specified key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><c>true</c> if the key is pressed; otherwise, <c>false</c>.</returns>
    public bool IsKeyDown(Key key) => PressedKeys.Contains(key);

    /// <summary>
    /// Gets whether the specified key is currently released.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><c>true</c> if the key is released; otherwise, <c>false</c>.</returns>
    public bool IsKeyUp(Key key) => !PressedKeys.Contains(key);

    /// <summary>
    /// Gets whether the Shift modifier is active.
    /// </summary>
    public bool IsShiftDown => (Modifiers & KeyModifiers.Shift) != 0;

    /// <summary>
    /// Gets whether the Control modifier is active.
    /// </summary>
    public bool IsControlDown => (Modifiers & KeyModifiers.Control) != 0;

    /// <summary>
    /// Gets whether the Alt modifier is active.
    /// </summary>
    public bool IsAltDown => (Modifiers & KeyModifiers.Alt) != 0;

    /// <summary>
    /// Gets whether the Super/Windows/Command modifier is active.
    /// </summary>
    public bool IsSuperDown => (Modifiers & KeyModifiers.Super) != 0;

    /// <summary>
    /// Gets whether Caps Lock is active.
    /// </summary>
    public bool IsCapsLockOn => (Modifiers & KeyModifiers.CapsLock) != 0;

    /// <summary>
    /// Gets whether Num Lock is active.
    /// </summary>
    public bool IsNumLockOn => (Modifiers & KeyModifiers.NumLock) != 0;

    /// <summary>
    /// Gets the number of keys currently pressed.
    /// </summary>
    public int PressedKeyCount => PressedKeys.Count;

    /// <inheritdoc />
    public override string ToString()
        => PressedKeys.Count == 0
            ? "KeyboardState(Empty)"
            : $"KeyboardState({PressedKeys.Count} keys, {Modifiers})";
}
