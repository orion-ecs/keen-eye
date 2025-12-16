namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Represents a keyboard input device.
/// </summary>
/// <remarks>
/// <para>
/// Provides both polling-based state queries and event-based input notification.
/// Polling is ideal for continuous input (movement), while events are better
/// for discrete actions (menu selection, text input).
/// </para>
/// <para>
/// Implementations should capture keyboard state each frame and raise events
/// for key state changes and character input.
/// </para>
/// </remarks>
public interface IKeyboard
{
    /// <summary>
    /// Gets the current keyboard state snapshot.
    /// </summary>
    /// <returns>A snapshot of the current keyboard state.</returns>
    KeyboardState GetState();

    /// <summary>
    /// Gets whether the specified key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><c>true</c> if the key is pressed; otherwise, <c>false</c>.</returns>
    bool IsKeyDown(Key key);

    /// <summary>
    /// Gets whether the specified key is currently released.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns><c>true</c> if the key is released; otherwise, <c>false</c>.</returns>
    bool IsKeyUp(Key key);

    /// <summary>
    /// Gets the current modifier key state.
    /// </summary>
    KeyModifiers Modifiers { get; }

    #region Events

    /// <summary>
    /// Raised when a key is pressed.
    /// </summary>
    /// <remarks>
    /// The event provides the key that was pressed and the current modifier state.
    /// For key repeat, this event fires repeatedly while the key is held.
    /// </remarks>
    event Action<KeyEventArgs>? OnKeyDown;

    /// <summary>
    /// Raised when a key is released.
    /// </summary>
    /// <remarks>
    /// The event provides the key that was released and the current modifier state.
    /// </remarks>
    event Action<KeyEventArgs>? OnKeyUp;

    /// <summary>
    /// Raised when a character is typed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event provides Unicode characters suitable for text input.
    /// It handles keyboard layout translation and modifier combinations
    /// (e.g., Shift+A produces 'A', not 'a').
    /// </para>
    /// <para>
    /// Use this event for text input fields rather than OnKeyDown,
    /// as it properly handles character composition and international keyboards.
    /// </para>
    /// </remarks>
    event Action<char>? OnTextInput;

    #endregion
}
