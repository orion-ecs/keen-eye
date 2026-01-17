using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.TestBridge.Animation;

namespace KeenEyes.TestBridge.AnimationImpl;

/// <summary>
/// In-process implementation of <see cref="IAnimationController"/>.
/// </summary>
internal sealed class AnimationControllerImpl(World world) : IAnimationController
{
    #region Statistics

    /// <inheritdoc />
    public Task<AnimationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AnimationManager>(out var animationManager))
        {
            return Task.FromResult(new AnimationStatisticsSnapshot
            {
                ClipCount = 0,
                ControllerCount = 0,
                SpriteSheetCount = 0,
                ActivePlayerCount = 0,
                ActiveAnimatorCount = 0
            });
        }

        // Count active players and animators
        var activePlayerCount = 0;
        foreach (var _ in world.Query<AnimationPlayer>())
        {
            activePlayerCount++;
        }

        var activeAnimatorCount = 0;
        foreach (var _ in world.Query<Animator>())
        {
            activeAnimatorCount++;
        }

        return Task.FromResult(new AnimationStatisticsSnapshot
        {
            ClipCount = animationManager.ClipCount,
            ControllerCount = animationManager.ControllerCount,
            SpriteSheetCount = animationManager.SpriteSheetCount,
            ActivePlayerCount = activePlayerCount,
            ActiveAnimatorCount = activeAnimatorCount
        });
    }

    #endregion

    #region Animation Player Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetAnimationPlayerEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<AnimationPlayer>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<AnimationPlayerSnapshot?> GetAnimationPlayerStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<AnimationPlayer>(entity))
        {
            return Task.FromResult<AnimationPlayerSnapshot?>(null);
        }

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);

        return Task.FromResult<AnimationPlayerSnapshot?>(new AnimationPlayerSnapshot
        {
            EntityId = entityId,
            ClipId = player.ClipId,
            Time = player.Time,
            Speed = player.Speed,
            IsPlaying = player.IsPlaying,
            IsComplete = player.IsComplete,
            Weight = player.Weight,
            WrapModeOverride = player.WrapModeOverride?.ToString()
        });
    }

    /// <inheritdoc />
    public Task<bool> SetAnimationPlayerPlayingAsync(int entityId, bool isPlaying, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<AnimationPlayer>(entity))
        {
            return Task.FromResult(false);
        }

        ref var player = ref world.Get<AnimationPlayer>(entity);
        player.IsPlaying = isPlaying;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SetAnimationPlayerTimeAsync(int entityId, float time, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<AnimationPlayer>(entity))
        {
            return Task.FromResult(false);
        }

        ref var player = ref world.Get<AnimationPlayer>(entity);
        player.Time = time;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> SetAnimationPlayerSpeedAsync(int entityId, float speed, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<AnimationPlayer>(entity))
        {
            return Task.FromResult(false);
        }

        ref var player = ref world.Get<AnimationPlayer>(entity);
        player.Speed = speed;
        return Task.FromResult(true);
    }

    #endregion

    #region Animator Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetAnimatorEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<Animator>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<AnimatorSnapshot?> GetAnimatorStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<Animator>(entity))
        {
            return Task.FromResult<AnimatorSnapshot?>(null);
        }

        ref readonly var animator = ref world.Get<Animator>(entity);

        // Try to get state names from controller
        string? currentStateName = null;
        string? nextStateName = null;
        if (world.TryGetExtension<AnimationManager>(out var animationManager) &&
            animationManager.TryGetController(animator.ControllerId, out var controller) &&
            controller is not null)
        {
            if (controller.TryGetState(animator.CurrentStateHash, out var currentState) && currentState is not null)
            {
                currentStateName = currentState.Name;
            }

            if (animator.NextStateHash != 0 &&
                controller.TryGetState(animator.NextStateHash, out var nextState) &&
                nextState is not null)
            {
                nextStateName = nextState.Name;
            }
        }

        return Task.FromResult<AnimatorSnapshot?>(new AnimatorSnapshot
        {
            EntityId = entityId,
            ControllerId = animator.ControllerId,
            CurrentStateHash = animator.CurrentStateHash,
            CurrentStateName = currentStateName,
            StateTime = animator.StateTime,
            IsTransitioning = animator.NextStateHash != 0,
            TransitionProgress = animator.TransitionProgress,
            Speed = animator.Speed,
            Enabled = animator.Enabled,
            NextStateHash = animator.NextStateHash,
            NextStateName = nextStateName
        });
    }

    /// <inheritdoc />
    public Task<bool> TriggerAnimatorStateAsync(int entityId, int stateHash, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<Animator>(entity))
        {
            return Task.FromResult(false);
        }

        ref var animator = ref world.Get<Animator>(entity);
        animator.TriggerStateHash = stateHash;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> TriggerAnimatorStateByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<Animator>(entity))
        {
            return Task.FromResult(false);
        }

        var stateHash = Animator.GetStateHash(stateName);
        ref var animator = ref world.Get<Animator>(entity);
        animator.TriggerStateHash = stateHash;
        return Task.FromResult(true);
    }

    #endregion

    #region Animation Clip Operations

    /// <inheritdoc />
    public Task<AnimationClipSnapshot?> GetClipInfoAsync(int clipId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AnimationManager>(out var animationManager))
        {
            return Task.FromResult<AnimationClipSnapshot?>(null);
        }

        if (!animationManager.TryGetClip(clipId, out var clip) || clip == null)
        {
            return Task.FromResult<AnimationClipSnapshot?>(null);
        }

        return Task.FromResult<AnimationClipSnapshot?>(new AnimationClipSnapshot
        {
            ClipId = clipId,
            Name = clip.Name,
            Duration = clip.Duration,
            WrapMode = clip.WrapMode.ToString(),
            BoneTrackCount = clip.BoneTracks.Count
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AnimationClipSnapshot>> ListClipsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AnimationManager>(out var animationManager))
        {
            return Task.FromResult<IReadOnlyList<AnimationClipSnapshot>>([]);
        }

        var clips = new List<AnimationClipSnapshot>();

        // AnimationManager doesn't expose registered clips directly, so we need to iterate through valid IDs
        // Since clip IDs are sequential starting from 1, we can try to get clips up to a reasonable limit
        // This is not ideal but works for debugging purposes
        for (var clipId = 1; clipId <= 10000; clipId++)
        {
            if (animationManager.TryGetClip(clipId, out var clip))
            {
                if (clip != null)
                {
                    clips.Add(new AnimationClipSnapshot
                    {
                        ClipId = clipId,
                        Name = clip.Name,
                        Duration = clip.Duration,
                        WrapMode = clip.WrapMode.ToString(),
                        BoneTrackCount = clip.BoneTracks.Count
                    });
                }
            }
            else if (clips.Count > 0 && clipId - clips.Count > 100)
            {
                // If we've tried 100 IDs beyond the last found clip, stop
                break;
            }
        }

        return Task.FromResult<IReadOnlyList<AnimationClipSnapshot>>(clips);
    }

    #endregion
}
