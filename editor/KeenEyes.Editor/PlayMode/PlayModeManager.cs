using KeenEyes;
using KeenEyes.Serialization;

namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Manages the play mode state machine for the editor.
/// </summary>
/// <remarks>
/// <para>
/// The play mode manager handles transitions between Editing, Playing, and Paused states.
/// When entering play mode, it captures a snapshot of the world state. When stopping,
/// it restores the snapshot to revert any changes made during play.
/// </para>
/// <para>
/// This enables a Unity/Godot-like workflow where changes made during play mode are
/// discarded when stopping.
/// </para>
/// </remarks>
public sealed class PlayModeManager
{
    private readonly World world;
    private readonly IComponentSerializer serializer;
    private WorldSnapshot? editingSnapshot;

    /// <summary>
    /// Raised when the play mode state changes.
    /// </summary>
    public event EventHandler<PlayModeStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Creates a new play mode manager.
    /// </summary>
    /// <param name="world">The world to manage play mode for.</param>
    /// <param name="serializer">The component serializer for snapshot operations.</param>
    public PlayModeManager(World world, IComponentSerializer serializer)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// Gets the current play mode state.
    /// </summary>
    public PlayModeState CurrentState { get; private set; } = PlayModeState.Editing;

    /// <summary>
    /// Gets whether the editor is currently in play mode (Playing or Paused).
    /// </summary>
    public bool IsInPlayMode => CurrentState is PlayModeState.Playing or PlayModeState.Paused;

    /// <summary>
    /// Gets whether the editor is currently playing (not editing and not paused).
    /// </summary>
    public bool IsPlaying => CurrentState == PlayModeState.Playing;

    /// <summary>
    /// Gets whether the editor is currently paused.
    /// </summary>
    public bool IsPaused => CurrentState == PlayModeState.Paused;

    /// <summary>
    /// Gets whether the editor is in editing mode.
    /// </summary>
    public bool IsEditing => CurrentState == PlayModeState.Editing;

    /// <summary>
    /// Enters play mode from editing mode.
    /// </summary>
    /// <returns>True if the transition was successful; false if already in play mode.</returns>
    /// <remarks>
    /// <para>
    /// When entering play mode, a snapshot of the current world state is captured.
    /// This snapshot will be restored when <see cref="Stop"/> is called.
    /// </para>
    /// </remarks>
    public bool Play()
    {
        if (CurrentState != PlayModeState.Editing)
        {
            return false;
        }

        // Capture the current world state before playing
        editingSnapshot = SnapshotManager.CreateSnapshot(world, serializer);

        TransitionTo(PlayModeState.Playing);
        return true;
    }

    /// <summary>
    /// Pauses the current play session.
    /// </summary>
    /// <returns>True if the transition was successful; false if not currently playing.</returns>
    public bool Pause()
    {
        if (CurrentState != PlayModeState.Playing)
        {
            return false;
        }

        TransitionTo(PlayModeState.Paused);
        return true;
    }

    /// <summary>
    /// Resumes a paused play session.
    /// </summary>
    /// <returns>True if the transition was successful; false if not currently paused.</returns>
    public bool Resume()
    {
        if (CurrentState != PlayModeState.Paused)
        {
            return false;
        }

        TransitionTo(PlayModeState.Playing);
        return true;
    }

    /// <summary>
    /// Stops play mode and restores the world to its pre-play state.
    /// </summary>
    /// <returns>True if the transition was successful; false if not in play mode.</returns>
    /// <remarks>
    /// <para>
    /// When stopping play mode, the world state is restored from the snapshot
    /// captured when <see cref="Play"/> was called. All changes made during
    /// play mode are discarded.
    /// </para>
    /// </remarks>
    public bool Stop()
    {
        if (CurrentState == PlayModeState.Editing)
        {
            return false;
        }

        // Restore the world state from before play mode
        if (editingSnapshot != null)
        {
            SnapshotManager.RestoreSnapshot(world, editingSnapshot, serializer);
            editingSnapshot = null;
        }

        TransitionTo(PlayModeState.Editing);
        return true;
    }

    /// <summary>
    /// Toggles between playing and paused states when in play mode,
    /// or enters play mode from editing mode.
    /// </summary>
    public void TogglePlayPause()
    {
        switch (CurrentState)
        {
            case PlayModeState.Editing:
                Play();
                break;
            case PlayModeState.Playing:
                Pause();
                break;
            case PlayModeState.Paused:
                Resume();
                break;
        }
    }

    private void TransitionTo(PlayModeState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        var previousState = CurrentState;
        CurrentState = newState;

        StateChanged?.Invoke(this, new PlayModeStateChangedEventArgs(previousState, newState));
    }
}
