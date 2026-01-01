namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Represents the current state of play mode in the editor.
/// </summary>
public enum PlayModeState
{
    /// <summary>
    /// The editor is in editing mode. Systems are not running.
    /// </summary>
    Editing,

    /// <summary>
    /// The editor is playing. Systems execute each frame.
    /// </summary>
    Playing,

    /// <summary>
    /// The editor is paused. Systems are suspended but can be inspected.
    /// </summary>
    Paused
}
