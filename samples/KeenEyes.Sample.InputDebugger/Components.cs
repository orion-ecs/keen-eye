using System.Numerics;

namespace KeenEyes.Sample.InputDebugger;

/// <summary>
/// Tag for the mouse position marker.
/// </summary>
[TagComponent]
public partial struct MouseMarkerTag;

/// <summary>
/// Tag for click markers that spawn on mouse clicks.
/// </summary>
[TagComponent]
public partial struct ClickMarkerTag;

/// <summary>
/// Component for markers that fade out over time.
/// </summary>
[Component]
public partial struct FadeOut
{
    /// <summary>
    /// Time remaining before marker is removed.
    /// </summary>
    public float TimeRemaining;

    /// <summary>
    /// Total fade duration for alpha calculation.
    /// </summary>
    public float TotalDuration;
}

/// <summary>
/// Tag for keyboard key visualization cubes.
/// </summary>
[TagComponent]
public partial struct KeyVisualizerTag;

/// <summary>
/// Component associating a visual element with a keyboard key.
/// </summary>
[Component]
public partial struct KeyBinding
{
    /// <summary>
    /// The keyboard key this element represents.
    /// </summary>
    public KeenEyes.Input.Abstractions.Key Key;
}

/// <summary>
/// Tag for the left stick marker.
/// </summary>
[TagComponent]
public partial struct LeftStickMarkerTag;

/// <summary>
/// Tag for the right stick marker.
/// </summary>
[TagComponent]
public partial struct RightStickMarkerTag;

/// <summary>
/// Tag for gamepad button indicators.
/// </summary>
[TagComponent]
public partial struct GamepadButtonTag;

/// <summary>
/// Component associating a visual with a gamepad button.
/// </summary>
[Component]
public partial struct GamepadButtonBinding
{
    /// <summary>
    /// The gamepad button this element represents.
    /// </summary>
    public KeenEyes.Input.Abstractions.GamepadButton Button;
}

