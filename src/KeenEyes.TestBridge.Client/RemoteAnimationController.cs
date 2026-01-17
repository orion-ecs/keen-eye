using KeenEyes.TestBridge.Animation;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IAnimationController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteAnimationController(TestBridgeClient client) : IAnimationController
{
    #region Statistics

    /// <inheritdoc />
    public async Task<AnimationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<AnimationStatisticsSnapshot>(
            "animation.getStatistics",
            null,
            cancellationToken) ?? new AnimationStatisticsSnapshot
            {
                ClipCount = 0,
                ControllerCount = 0,
                SpriteSheetCount = 0,
                ActivePlayerCount = 0,
                ActiveAnimatorCount = 0
            };
    }

    #endregion

    #region Animation Player Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetAnimationPlayerEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "animation.getAnimationPlayerEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<AnimationPlayerSnapshot?> GetAnimationPlayerStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<AnimationPlayerSnapshot?>(
            "animation.getAnimationPlayerState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetAnimationPlayerPlayingAsync(int entityId, bool isPlaying, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "animation.setAnimationPlayerPlaying",
            new { entityId, isPlaying },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetAnimationPlayerTimeAsync(int entityId, float time, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "animation.setAnimationPlayerTime",
            new { entityId, time },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetAnimationPlayerSpeedAsync(int entityId, float speed, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "animation.setAnimationPlayerSpeed",
            new { entityId, speed },
            cancellationToken);
    }

    #endregion

    #region Animator Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetAnimatorEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "animation.getAnimatorEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<AnimatorSnapshot?> GetAnimatorStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<AnimatorSnapshot?>(
            "animation.getAnimatorState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TriggerAnimatorStateAsync(int entityId, int stateHash, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "animation.triggerAnimatorState",
            new { entityId, stateHash },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TriggerAnimatorStateByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "animation.triggerAnimatorStateByName",
            new { entityId, stateName },
            cancellationToken);
    }

    #endregion

    #region Animation Clip Operations

    /// <inheritdoc />
    public async Task<AnimationClipSnapshot?> GetClipInfoAsync(int clipId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<AnimationClipSnapshot?>(
            "animation.getClipInfo",
            new { clipId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AnimationClipSnapshot>> ListClipsAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<AnimationClipSnapshot[]>(
            "animation.listClips",
            null,
            cancellationToken);
        return result ?? [];
    }

    #endregion
}
