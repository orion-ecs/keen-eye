using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Animation;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for animation debugging: animation players, animators, and animation clips.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose the AnimationPlugin debugging infrastructure via MCP, allowing inspection
/// and manipulation of animation components in running games.
/// </para>
/// <para>
/// Note: These tools require the AnimationPlugin to be installed in the target world.
/// Entities must have the appropriate animation component (AnimationPlayer or Animator)
/// for the operations to work.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class AnimationTools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "animation_get_statistics")]
    [Description("Get overall animation statistics including clip count, controller count, and active animation instances.")]
    public async Task<AnimationStatisticsResult> GetStatistics()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Animation.GetStatisticsAsync();
        return AnimationStatisticsResult.FromSnapshot(stats);
    }

    #endregion

    #region Animation Players

    [McpServerTool(Name = "animation_player_list")]
    [Description("List all entities that have an AnimationPlayer component.")]
    public async Task<EntityListResult> GetAnimationPlayerEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.Animation.GetAnimationPlayerEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "animation_player_get")]
    [Description("Get the current state of an entity's animation player.")]
    public async Task<AnimationPlayerResult> GetAnimationPlayerState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Animation.GetAnimationPlayerStateAsync(entityId);

        if (snapshot == null)
        {
            return new AnimationPlayerResult
            {
                Success = false,
                Error = $"No animation player found on entity {entityId}"
            };
        }

        return AnimationPlayerResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "animation_player_set_playing")]
    [Description("Set whether an animation player is playing or paused.")]
    public async Task<OperationResult> SetAnimationPlayerPlaying(
        [Description("The entity ID to modify")]
        int entityId,
        [Description("Whether the animation should be playing")]
        bool isPlaying)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Animation.SetAnimationPlayerPlayingAsync(entityId, isPlaying);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set playing state on entity {entityId}"
        };
    }

    [McpServerTool(Name = "animation_player_set_time")]
    [Description("Set the playback time of an animation player.")]
    public async Task<OperationResult> SetAnimationPlayerTime(
        [Description("The entity ID to modify")]
        int entityId,
        [Description("The playback time in seconds")]
        float time)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Animation.SetAnimationPlayerTimeAsync(entityId, time);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set time on entity {entityId}"
        };
    }

    [McpServerTool(Name = "animation_player_set_speed")]
    [Description("Set the playback speed multiplier of an animation player.")]
    public async Task<OperationResult> SetAnimationPlayerSpeed(
        [Description("The entity ID to modify")]
        int entityId,
        [Description("The playback speed multiplier (1 = normal, 2 = double speed, -1 = reverse)")]
        float speed)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Animation.SetAnimationPlayerSpeedAsync(entityId, speed);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set speed on entity {entityId}"
        };
    }

    #endregion

    #region Animators

    [McpServerTool(Name = "animation_animator_list")]
    [Description("List all entities that have an Animator component.")]
    public async Task<EntityListResult> GetAnimatorEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.Animation.GetAnimatorEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "animation_animator_get")]
    [Description("Get the current state of an entity's animator.")]
    public async Task<AnimatorResult> GetAnimatorState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Animation.GetAnimatorStateAsync(entityId);

        if (snapshot == null)
        {
            return new AnimatorResult
            {
                Success = false,
                Error = $"No animator found on entity {entityId}"
            };
        }

        return AnimatorResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "animation_animator_trigger_state")]
    [Description("Trigger a state transition in an animator by state hash.")]
    public async Task<OperationResult> TriggerAnimatorState(
        [Description("The entity ID to transition")]
        int entityId,
        [Description("The hash of the target state")]
        int stateHash)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Animation.TriggerAnimatorStateAsync(entityId, stateHash);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to trigger state transition on entity {entityId}"
        };
    }

    [McpServerTool(Name = "animation_animator_trigger_state_by_name")]
    [Description("Trigger a state transition in an animator by state name.")]
    public async Task<OperationResult> TriggerAnimatorStateByName(
        [Description("The entity ID to transition")]
        int entityId,
        [Description("The name of the target state")]
        string stateName)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Animation.TriggerAnimatorStateByNameAsync(entityId, stateName);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to trigger state '{stateName}' on entity {entityId}"
        };
    }

    #endregion

    #region Animation Clips

    [McpServerTool(Name = "animation_clip_get")]
    [Description("Get information about an animation clip by ID.")]
    public async Task<AnimationClipResult> GetClipInfo(
        [Description("The clip ID to query")]
        int clipId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Animation.GetClipInfoAsync(clipId);

        if (snapshot == null)
        {
            return new AnimationClipResult
            {
                Success = false,
                Error = $"Clip {clipId} not found"
            };
        }

        return AnimationClipResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "animation_clip_list")]
    [Description("List all registered animation clips.")]
    public async Task<AnimationClipListResult> ListClips()
    {
        var bridge = connection.GetBridge();
        var clips = await bridge.Animation.ListClipsAsync();
        return new AnimationClipListResult
        {
            Success = true,
            Clips = clips,
            Count = clips.Count
        };
    }

    #endregion
}

#region Result Types

/// <summary>
/// Result for animation statistics query.
/// </summary>
public sealed class AnimationStatisticsResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public int ClipCount { get; init; }
    public int ControllerCount { get; init; }
    public int SpriteSheetCount { get; init; }
    public int ActivePlayerCount { get; init; }
    public int ActiveAnimatorCount { get; init; }

    public static AnimationStatisticsResult FromSnapshot(AnimationStatisticsSnapshot snapshot)
    {
        return new AnimationStatisticsResult
        {
            Success = true,
            ClipCount = snapshot.ClipCount,
            ControllerCount = snapshot.ControllerCount,
            SpriteSheetCount = snapshot.SpriteSheetCount,
            ActivePlayerCount = snapshot.ActivePlayerCount,
            ActiveAnimatorCount = snapshot.ActiveAnimatorCount
        };
    }
}

/// <summary>
/// Result for animation player state query.
/// </summary>
public sealed class AnimationPlayerResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public int EntityId { get; init; }
    public int ClipId { get; init; }
    public float Time { get; init; }
    public float Speed { get; init; }
    public bool IsPlaying { get; init; }
    public bool IsComplete { get; init; }
    public float Weight { get; init; }
    public string? WrapModeOverride { get; init; }

    public static AnimationPlayerResult FromSnapshot(AnimationPlayerSnapshot snapshot)
    {
        return new AnimationPlayerResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            ClipId = snapshot.ClipId,
            Time = snapshot.Time,
            Speed = snapshot.Speed,
            IsPlaying = snapshot.IsPlaying,
            IsComplete = snapshot.IsComplete,
            Weight = snapshot.Weight,
            WrapModeOverride = snapshot.WrapModeOverride
        };
    }
}

/// <summary>
/// Result for animator state query.
/// </summary>
public sealed class AnimatorResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public int EntityId { get; init; }
    public int ControllerId { get; init; }
    public int CurrentStateHash { get; init; }
    public string? CurrentStateName { get; init; }
    public float StateTime { get; init; }
    public bool IsTransitioning { get; init; }
    public float TransitionProgress { get; init; }
    public float Speed { get; init; }
    public bool Enabled { get; init; }
    public int NextStateHash { get; init; }
    public string? NextStateName { get; init; }

    public static AnimatorResult FromSnapshot(AnimatorSnapshot snapshot)
    {
        return new AnimatorResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            ControllerId = snapshot.ControllerId,
            CurrentStateHash = snapshot.CurrentStateHash,
            CurrentStateName = snapshot.CurrentStateName,
            StateTime = snapshot.StateTime,
            IsTransitioning = snapshot.IsTransitioning,
            TransitionProgress = snapshot.TransitionProgress,
            Speed = snapshot.Speed,
            Enabled = snapshot.Enabled,
            NextStateHash = snapshot.NextStateHash,
            NextStateName = snapshot.NextStateName
        };
    }
}

/// <summary>
/// Result for animation clip query.
/// </summary>
public sealed class AnimationClipResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public int ClipId { get; init; }
    public string Name { get; init; } = string.Empty;
    public float Duration { get; init; }
    public string WrapMode { get; init; } = string.Empty;
    public int BoneTrackCount { get; init; }

    public static AnimationClipResult FromSnapshot(AnimationClipSnapshot snapshot)
    {
        return new AnimationClipResult
        {
            Success = true,
            ClipId = snapshot.ClipId,
            Name = snapshot.Name,
            Duration = snapshot.Duration,
            WrapMode = snapshot.WrapMode,
            BoneTrackCount = snapshot.BoneTrackCount
        };
    }
}

/// <summary>
/// Result for animation clip list query.
/// </summary>
public sealed class AnimationClipListResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<AnimationClipSnapshot> Clips { get; init; } = [];
    public int Count { get; init; }
}

#endregion
