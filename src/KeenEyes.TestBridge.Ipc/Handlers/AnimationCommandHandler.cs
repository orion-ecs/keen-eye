using System.Text.Json;
using KeenEyes.TestBridge.Animation;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles animation debugging commands for animation players, animators, and clips.
/// </summary>
internal sealed class AnimationCommandHandler(IAnimationController animationController) : ICommandHandler
{
    public string Prefix => "animation";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Statistics
            "getStatistics" => await animationController.GetStatisticsAsync(cancellationToken),

            // Animation Player
            "getAnimationPlayerEntities" => await animationController.GetAnimationPlayerEntitiesAsync(cancellationToken),
            "getAnimationPlayerState" => await HandleGetAnimationPlayerStateAsync(args, cancellationToken),
            "setAnimationPlayerPlaying" => await HandleSetAnimationPlayerPlayingAsync(args, cancellationToken),
            "setAnimationPlayerTime" => await HandleSetAnimationPlayerTimeAsync(args, cancellationToken),
            "setAnimationPlayerSpeed" => await HandleSetAnimationPlayerSpeedAsync(args, cancellationToken),

            // Animator
            "getAnimatorEntities" => await animationController.GetAnimatorEntitiesAsync(cancellationToken),
            "getAnimatorState" => await HandleGetAnimatorStateAsync(args, cancellationToken),
            "triggerAnimatorState" => await HandleTriggerAnimatorStateAsync(args, cancellationToken),
            "triggerAnimatorStateByName" => await HandleTriggerAnimatorStateByNameAsync(args, cancellationToken),

            // Animation Clips
            "getClipInfo" => await HandleGetClipInfoAsync(args, cancellationToken),
            "listClips" => await animationController.ListClipsAsync(cancellationToken),

            _ => throw new InvalidOperationException($"Unknown animation command: {command}")
        };
    }

    #region Animation Player Handlers

    private async Task<AnimationPlayerSnapshot?> HandleGetAnimationPlayerStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await animationController.GetAnimationPlayerStateAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleSetAnimationPlayerPlayingAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var isPlaying = GetRequiredBool(args, "isPlaying");
        return await animationController.SetAnimationPlayerPlayingAsync(entityId, isPlaying, cancellationToken);
    }

    private async Task<bool> HandleSetAnimationPlayerTimeAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var time = GetRequiredFloat(args, "time");
        return await animationController.SetAnimationPlayerTimeAsync(entityId, time, cancellationToken);
    }

    private async Task<bool> HandleSetAnimationPlayerSpeedAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var speed = GetRequiredFloat(args, "speed");
        return await animationController.SetAnimationPlayerSpeedAsync(entityId, speed, cancellationToken);
    }

    #endregion

    #region Animator Handlers

    private async Task<AnimatorSnapshot?> HandleGetAnimatorStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await animationController.GetAnimatorStateAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleTriggerAnimatorStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var stateHash = GetRequiredInt(args, "stateHash");
        return await animationController.TriggerAnimatorStateAsync(entityId, stateHash, cancellationToken);
    }

    private async Task<bool> HandleTriggerAnimatorStateByNameAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var stateName = GetRequiredString(args, "stateName");
        return await animationController.TriggerAnimatorStateByNameAsync(entityId, stateName, cancellationToken);
    }

    #endregion

    #region Animation Clip Handlers

    private async Task<AnimationClipSnapshot?> HandleGetClipInfoAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var clipId = GetRequiredInt(args, "clipId");
        return await animationController.GetClipInfoAsync(clipId, cancellationToken);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetSingle();
    }

    private static bool GetRequiredBool(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetBoolean();
    }

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    #endregion
}
