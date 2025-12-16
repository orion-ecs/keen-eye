namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Represents a named input action with one or more input bindings.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="InputAction"/> provides an abstraction layer between game logic and
/// physical input devices. Instead of checking specific keys or buttons, game code
/// can check if an action (like "Jump" or "Fire") is active.
/// </para>
/// <para>
/// Actions support multiple bindings, allowing the same action to be triggered by
/// different input sources (e.g., Space key and gamepad A button for "Jump").
/// </para>
/// <para>
/// Bindings can be modified at runtime to support player key rebinding.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create an action with multiple bindings
/// var jump = new InputAction("Jump",
///     InputBinding.FromKey(Key.Space),
///     InputBinding.FromGamepadButton(GamepadButton.South));
///
/// // Check if action is pressed
/// if (jump.IsPressed(input))
///     player.Jump();
///
/// // Rebind at runtime
/// jump.ClearBindings();
/// jump.AddBinding(InputBinding.FromKey(Key.W));
/// </code>
/// </example>
public sealed class InputAction
{
    private readonly List<InputBinding> bindings;

    /// <summary>
    /// Gets the name of this action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the bindings associated with this action.
    /// </summary>
    public IReadOnlyList<InputBinding> Bindings => bindings;

    /// <summary>
    /// Gets or sets whether this action is enabled.
    /// </summary>
    /// <remarks>
    /// Disabled actions always return false for <see cref="IsPressed"/> and 0 for <see cref="GetValue"/>.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the gamepad index to check for this action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to -1 (default), the action checks all connected gamepads.
    /// Set to a specific index (0-3) to limit gamepad input to a single controller.
    /// </para>
    /// <para>
    /// This is useful for local multiplayer where each player has their own action set
    /// bound to a specific controller.
    /// </para>
    /// </remarks>
    public int GamepadIndex { get; set; } = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputAction"/> class.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <param name="bindings">The initial bindings for this action.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public InputAction(string name, params InputBinding[] bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        this.bindings = [.. bindings];
    }

    /// <summary>
    /// Adds a binding to this action.
    /// </summary>
    /// <param name="binding">The binding to add.</param>
    public void AddBinding(InputBinding binding)
    {
        bindings.Add(binding);
    }

    /// <summary>
    /// Removes a binding from this action.
    /// </summary>
    /// <param name="binding">The binding to remove.</param>
    /// <returns><c>true</c> if the binding was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveBinding(InputBinding binding)
    {
        return bindings.Remove(binding);
    }

    /// <summary>
    /// Removes all bindings from this action.
    /// </summary>
    public void ClearBindings()
    {
        bindings.Clear();
    }

    /// <summary>
    /// Replaces all bindings with the specified bindings.
    /// </summary>
    /// <param name="newBindings">The new bindings to set.</param>
    public void SetBindings(params InputBinding[] newBindings)
    {
        bindings.Clear();
        bindings.AddRange(newBindings);
    }

    /// <summary>
    /// Checks if any of this action's bindings are currently active (pressed).
    /// </summary>
    /// <param name="input">The input context to check against.</param>
    /// <returns>
    /// <c>true</c> if any binding is active and the action is enabled; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPressed(IInputContext input)
    {
        if (!Enabled)
        {
            return false;
        }

        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].IsActive(input, GamepadIndex))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if all of this action's bindings are currently inactive (released).
    /// </summary>
    /// <param name="input">The input context to check against.</param>
    /// <returns>
    /// <c>true</c> if no binding is active or the action is disabled; otherwise, <c>false</c>.
    /// </returns>
    public bool IsReleased(IInputContext input)
    {
        return !IsPressed(input);
    }

    /// <summary>
    /// Gets the analog value for this action.
    /// </summary>
    /// <param name="input">The input context to check against.</param>
    /// <returns>
    /// <para>Returns 0 if the action is disabled.</para>
    /// <para>
    /// For digital bindings, returns 1.0 if pressed, 0.0 if not.
    /// For analog bindings (axes), returns the raw axis value.
    /// </para>
    /// <para>
    /// When multiple bindings are active, returns the value with the largest absolute magnitude.
    /// </para>
    /// </returns>
    public float GetValue(IInputContext input)
    {
        if (!Enabled)
        {
            return 0f;
        }

        float maxValue = 0f;

        for (int i = 0; i < bindings.Count; i++)
        {
            float value = bindings[i].GetValue(input, GamepadIndex);
            if (MathF.Abs(value) > MathF.Abs(maxValue))
            {
                maxValue = value;
            }
        }

        return maxValue;
    }
}
