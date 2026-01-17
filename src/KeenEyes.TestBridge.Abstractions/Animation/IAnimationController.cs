namespace KeenEyes.TestBridge.Animation;

/// <summary>
/// Controller interface for animation debugging and inspection operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides access to animation state including animation players,
/// animators, animation clips, and sprite sheets. It enables inspection and
/// manipulation of animation components for debugging and testing.
/// </para>
/// <para>
/// <strong>Note:</strong> Requires the AnimationPlugin to be installed on the world
/// for full functionality.
/// </para>
/// </remarks>
public interface IAnimationController
{
    #region Statistics

    /// <summary>
    /// Gets statistics about animation component usage in the world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics about animation components and assets.</returns>
    Task<AnimationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Animation Player Operations

    /// <summary>
    /// Gets all entities with animation player components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have animation player components.</returns>
    Task<IReadOnlyList<int>> GetAnimationPlayerEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of an animation player for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The animation player state, or null if the entity has no animation player.</returns>
    Task<AnimationPlayerSnapshot?> GetAnimationPlayerStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the playing state of an animation player.
    /// </summary>
    /// <param name="entityId">The entity ID to modify.</param>
    /// <param name="isPlaying">Whether the animation should be playing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the state was set successfully.</returns>
    Task<bool> SetAnimationPlayerPlayingAsync(int entityId, bool isPlaying, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the playback time of an animation player.
    /// </summary>
    /// <param name="entityId">The entity ID to modify.</param>
    /// <param name="time">The playback time in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the time was set successfully.</returns>
    Task<bool> SetAnimationPlayerTimeAsync(int entityId, float time, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the playback speed of an animation player.
    /// </summary>
    /// <param name="entityId">The entity ID to modify.</param>
    /// <param name="speed">The playback speed multiplier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the speed was set successfully.</returns>
    Task<bool> SetAnimationPlayerSpeedAsync(int entityId, float speed, CancellationToken cancellationToken = default);

    #endregion

    #region Animator Operations

    /// <summary>
    /// Gets all entities with animator components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have animator components.</returns>
    Task<IReadOnlyList<int>> GetAnimatorEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of an animator for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The animator state, or null if the entity has no animator.</returns>
    Task<AnimatorSnapshot?> GetAnimatorStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a state transition in an animator by state hash.
    /// </summary>
    /// <param name="entityId">The entity ID to transition.</param>
    /// <param name="stateHash">The hash of the target state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the trigger was set successfully.</returns>
    Task<bool> TriggerAnimatorStateAsync(int entityId, int stateHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a state transition in an animator by state name.
    /// </summary>
    /// <param name="entityId">The entity ID to transition.</param>
    /// <param name="stateName">The name of the target state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the trigger was set successfully.</returns>
    Task<bool> TriggerAnimatorStateByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default);

    #endregion

    #region Animation Clip Operations

    /// <summary>
    /// Gets information about an animation clip by ID.
    /// </summary>
    /// <param name="clipId">The clip ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The clip information, or null if not found.</returns>
    Task<AnimationClipSnapshot?> GetClipInfoAsync(int clipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all registered animation clips.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered animation clips.</returns>
    Task<IReadOnlyList<AnimationClipSnapshot>> ListClipsAsync(CancellationToken cancellationToken = default);

    #endregion
}
