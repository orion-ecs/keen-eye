namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that identifies a checkbox widget and tracks its state.
/// </summary>
/// <param name="isChecked">Initial checked state.</param>
public struct UICheckbox(bool isChecked = false) : IComponent
{
    /// <summary>
    /// Gets or sets whether the checkbox is checked.
    /// </summary>
    public bool IsChecked = isChecked;

    /// <summary>
    /// Entity reference to the visual box element.
    /// </summary>
    public Entity BoxEntity = Entity.Null;
}

/// <summary>
/// Component that identifies a toggle/switch widget and tracks its state.
/// </summary>
/// <param name="isOn">Initial on state.</param>
public struct UIToggle(bool isOn = false) : IComponent
{
    /// <summary>
    /// Gets or sets whether the toggle is on.
    /// </summary>
    public bool IsOn = isOn;

    /// <summary>
    /// Entity reference to the track element.
    /// </summary>
    public Entity TrackEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the thumb element.
    /// </summary>
    public Entity ThumbEntity = Entity.Null;
}

/// <summary>
/// Component that identifies a slider widget and tracks its value.
/// </summary>
/// <param name="minValue">Minimum slider value.</param>
/// <param name="maxValue">Maximum slider value.</param>
/// <param name="value">Initial value.</param>
public struct UISlider(float minValue = 0f, float maxValue = 1f, float value = 0f) : IComponent
{
    /// <summary>
    /// Minimum slider value.
    /// </summary>
    public float MinValue = minValue;

    /// <summary>
    /// Maximum slider value.
    /// </summary>
    public float MaxValue = maxValue;

    /// <summary>
    /// Current slider value.
    /// </summary>
    public float Value = value;

    /// <summary>
    /// Entity reference to the fill element.
    /// </summary>
    public Entity FillEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the thumb element.
    /// </summary>
    public Entity ThumbEntity = Entity.Null;
}
