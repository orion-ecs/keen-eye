namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Event arguments for play mode state changes.
/// </summary>
public sealed class PlayModeStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Creates new play mode state changed event args.
    /// </summary>
    /// <param name="previousState">The state before the change.</param>
    /// <param name="currentState">The state after the change.</param>
    public PlayModeStateChangedEventArgs(PlayModeState previousState, PlayModeState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    /// <summary>
    /// Gets the state before the change.
    /// </summary>
    public PlayModeState PreviousState { get; }

    /// <summary>
    /// Gets the state after the change.
    /// </summary>
    public PlayModeState CurrentState { get; }
}
