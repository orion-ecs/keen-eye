namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Represents a single input source that can trigger an <see cref="InputAction"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="InputBinding"/> maps a physical input (key, button, axis) to a logical action.
/// Multiple bindings can be assigned to a single action to support different input devices.
/// </para>
/// <para>
/// Use the factory methods to create bindings:
/// <list type="bullet">
/// <item><see cref="FromKey"/> for keyboard keys</item>
/// <item><see cref="FromMouseButton"/> for mouse buttons</item>
/// <item><see cref="FromGamepadButton"/> for gamepad buttons</item>
/// <item><see cref="FromGamepadAxis"/> for analog sticks and triggers</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create bindings for a "Jump" action
/// var spaceBinding = InputBinding.FromKey(Key.Space);
/// var buttonBinding = InputBinding.FromGamepadButton(GamepadButton.South);
///
/// // Create axis binding with threshold (axis-as-button)
/// var triggerBinding = InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger, 0.5f);
/// </code>
/// </example>
public readonly record struct InputBinding
{
    /// <summary>
    /// Gets the type of input source this binding represents.
    /// </summary>
    public InputBindingType Type { get; init; }

    /// <summary>
    /// Gets the keyboard key for this binding.
    /// </summary>
    /// <remarks>Only valid when <see cref="Type"/> is <see cref="InputBindingType.Key"/>.</remarks>
    public Key Key { get; init; }

    /// <summary>
    /// Gets the mouse button for this binding.
    /// </summary>
    /// <remarks>Only valid when <see cref="Type"/> is <see cref="InputBindingType.MouseButton"/>.</remarks>
    public MouseButton MouseButton { get; init; }

    /// <summary>
    /// Gets the gamepad button for this binding.
    /// </summary>
    /// <remarks>Only valid when <see cref="Type"/> is <see cref="InputBindingType.GamepadButton"/>.</remarks>
    public GamepadButton GamepadButton { get; init; }

    /// <summary>
    /// Gets the gamepad axis for this binding.
    /// </summary>
    /// <remarks>Only valid when <see cref="Type"/> is <see cref="InputBindingType.GamepadAxis"/>.</remarks>
    public GamepadAxis GamepadAxis { get; init; }

    /// <summary>
    /// Gets the threshold value for axis-as-button conversion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When checking if an axis binding is "pressed", the axis value must exceed
    /// this threshold. For example, a threshold of 0.5 means the trigger must be
    /// at least 50% pressed to register as active.
    /// </para>
    /// <para>Only used when <see cref="Type"/> is <see cref="InputBindingType.GamepadAxis"/>.</para>
    /// </remarks>
    public float AxisThreshold { get; init; }

    /// <summary>
    /// Gets whether this binding represents a positive axis direction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For 2D axes (sticks), this determines whether the positive or negative
    /// direction triggers the action. For triggers (0-1 range), this is typically true.
    /// </para>
    /// <para>Only used when <see cref="Type"/> is <see cref="InputBindingType.GamepadAxis"/>.</para>
    /// </remarks>
    public bool IsPositiveAxis { get; init; }

    /// <summary>
    /// Creates a binding for a keyboard key.
    /// </summary>
    /// <param name="key">The keyboard key to bind.</param>
    /// <returns>A new <see cref="InputBinding"/> for the specified key.</returns>
    public static InputBinding FromKey(Key key) => new()
    {
        Type = InputBindingType.Key,
        Key = key
    };

    /// <summary>
    /// Creates a binding for a mouse button.
    /// </summary>
    /// <param name="button">The mouse button to bind.</param>
    /// <returns>A new <see cref="InputBinding"/> for the specified button.</returns>
    public static InputBinding FromMouseButton(MouseButton button) => new()
    {
        Type = InputBindingType.MouseButton,
        MouseButton = button
    };

    /// <summary>
    /// Creates a binding for a gamepad button.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <returns>A new <see cref="InputBinding"/> for the specified button.</returns>
    public static InputBinding FromGamepadButton(GamepadButton button) => new()
    {
        Type = InputBindingType.GamepadButton,
        GamepadButton = button
    };

    /// <summary>
    /// Creates a binding for a gamepad axis.
    /// </summary>
    /// <param name="axis">The gamepad axis to bind.</param>
    /// <param name="threshold">
    /// The threshold value (0.0 to 1.0) for axis-as-button conversion.
    /// The axis value must exceed this threshold to be considered "pressed".
    /// </param>
    /// <param name="isPositive">
    /// Whether to detect positive axis values. Set to false to detect negative values
    /// (e.g., for left stick "left" direction).
    /// </param>
    /// <returns>A new <see cref="InputBinding"/> for the specified axis.</returns>
    public static InputBinding FromGamepadAxis(GamepadAxis axis, float threshold = 0.5f, bool isPositive = true) => new()
    {
        Type = InputBindingType.GamepadAxis,
        GamepadAxis = axis,
        AxisThreshold = threshold,
        IsPositiveAxis = isPositive
    };

    /// <summary>
    /// Checks if this binding is currently active (pressed/triggered).
    /// </summary>
    /// <param name="input">The input context to check against.</param>
    /// <param name="gamepadIndex">
    /// The gamepad index to check for gamepad bindings.
    /// Pass -1 to check all connected gamepads.
    /// </param>
    /// <returns><c>true</c> if the bound input is active; otherwise, <c>false</c>.</returns>
    public bool IsActive(IInputContext input, int gamepadIndex = -1)
    {
        return Type switch
        {
            InputBindingType.Key => input.Keyboard.IsKeyDown(Key),
            InputBindingType.MouseButton => input.Mouse.IsButtonDown(MouseButton),
            InputBindingType.GamepadButton => CheckGamepadButton(input, gamepadIndex),
            InputBindingType.GamepadAxis => CheckGamepadAxis(input, gamepadIndex),
            _ => false
        };
    }

    /// <summary>
    /// Gets the analog value for this binding.
    /// </summary>
    /// <param name="input">The input context to check against.</param>
    /// <param name="gamepadIndex">
    /// The gamepad index to check for gamepad bindings.
    /// Pass -1 to check all connected gamepads and return the largest absolute value.
    /// </param>
    /// <returns>
    /// For digital inputs (keys, buttons), returns 1.0 if pressed, 0.0 if not.
    /// For analog inputs (axes), returns the raw axis value.
    /// </returns>
    public float GetValue(IInputContext input, int gamepadIndex = -1)
    {
        return Type switch
        {
            InputBindingType.Key => input.Keyboard.IsKeyDown(Key) ? 1f : 0f,
            InputBindingType.MouseButton => input.Mouse.IsButtonDown(MouseButton) ? 1f : 0f,
            InputBindingType.GamepadButton => CheckGamepadButton(input, gamepadIndex) ? 1f : 0f,
            InputBindingType.GamepadAxis => GetGamepadAxisValue(input, gamepadIndex),
            _ => 0f
        };
    }

    private bool CheckGamepadButton(IInputContext input, int gamepadIndex)
    {
        if (gamepadIndex >= 0)
        {
            var gamepads = input.Gamepads;
            if (gamepadIndex < gamepads.Length && gamepads[gamepadIndex].IsConnected)
            {
                return gamepads[gamepadIndex].IsButtonDown(GamepadButton);
            }

            return false;
        }

        // Check all connected gamepads
        var allGamepads = input.Gamepads;
        for (int i = 0; i < allGamepads.Length; i++)
        {
            if (allGamepads[i].IsConnected && allGamepads[i].IsButtonDown(GamepadButton))
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckGamepadAxis(IInputContext input, int gamepadIndex)
    {
        float value = GetGamepadAxisValue(input, gamepadIndex);

        if (IsPositiveAxis)
        {
            return value >= AxisThreshold;
        }
        else
        {
            return value <= -AxisThreshold;
        }
    }

    private float GetGamepadAxisValue(IInputContext input, int gamepadIndex)
    {
        if (gamepadIndex >= 0)
        {
            var gamepads = input.Gamepads;
            if (gamepadIndex < gamepads.Length && gamepads[gamepadIndex].IsConnected)
            {
                return gamepads[gamepadIndex].GetAxis(GamepadAxis);
            }

            return 0f;
        }

        // Find the gamepad with the largest absolute axis value
        float maxValue = 0f;
        var allGamepads = input.Gamepads;
        for (int i = 0; i < allGamepads.Length; i++)
        {
            if (allGamepads[i].IsConnected)
            {
                float value = allGamepads[i].GetAxis(GamepadAxis);
                if (MathF.Abs(value) > MathF.Abs(maxValue))
                {
                    maxValue = value;
                }
            }
        }

        return maxValue;
    }
}
