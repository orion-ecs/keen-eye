using System.Text.Json;
using KeenEyes.TestBridge.AI;
using KeenEyes.TestBridge.Animation;
using KeenEyes.TestBridge.Mutation;
using KeenEyes.TestBridge.Navigation;
using KeenEyes.TestBridge.Network;
using KeenEyes.TestBridge.Physics;
using KeenEyes.TestBridge.Profile;
using KeenEyes.TestBridge.Replay;
using KeenEyes.TestBridge.Snapshot;
using KeenEyes.TestBridge.Systems;
using KeenEyes.TestBridge.Time;
using KeenEyes.TestBridge.UI;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IWindowController for testing.
/// </summary>
internal sealed class MockWindowController : IWindowController
{
    public bool IsAvailable { get; set; } = true;
    public (int Width, int Height) Size { get; set; } = (1920, 1080);
    public string Title { get; set; } = "Test Window";

    public Task<WindowStateSnapshot> GetStateAsync() => Task.FromResult(new WindowStateSnapshot
    {
        Width = Size.Width,
        Height = Size.Height,
        Title = Title,
        IsClosing = false,
        IsFocused = true,
        AspectRatio = (float)Size.Width / Size.Height
    });

    public Task<(int Width, int Height)> GetSizeAsync() => Task.FromResult(Size);
    public Task<string> GetTitleAsync() => Task.FromResult(Title);
    public Task<bool> IsClosingAsync() => Task.FromResult(false);
    public Task<bool> IsFocusedAsync() => Task.FromResult(true);
    public Task<float> GetAspectRatioAsync() => Task.FromResult((float)Size.Width / Size.Height);
}

/// <summary>
/// Mock implementation of ITimeController for testing.
/// </summary>
internal sealed class MockTimeController : ITimeController
{
    public bool IsPaused { get; set; }
    public float TimeScale { get; set; } = 1.0f;

    public Task<TimeStateSnapshot> GetTimeStateAsync() => Task.FromResult(CreateSnapshot());
    public Task<TimeStateSnapshot> PauseAsync() { IsPaused = true; return Task.FromResult(CreateSnapshot()); }
    public Task<TimeStateSnapshot> ResumeAsync() { IsPaused = false; return Task.FromResult(CreateSnapshot()); }
    public Task<TimeStateSnapshot> SetTimeScaleAsync(float scale) { TimeScale = scale; return Task.FromResult(CreateSnapshot()); }
    public Task<TimeStateSnapshot> StepFrameAsync(int frames = 1) => Task.FromResult(CreateSnapshot());
    public Task<TimeStateSnapshot> TogglePauseAsync() { IsPaused = !IsPaused; return Task.FromResult(CreateSnapshot()); }

    private TimeStateSnapshot CreateSnapshot() => new()
    {
        IsPaused = IsPaused,
        TimeScale = TimeScale,
        TotalPausedTime = 0,
        LastModifiedFrame = 0,
        PendingStepFrames = 0
    };
}

/// <summary>
/// Mock implementation of ISystemController for testing.
/// </summary>
internal sealed class MockSystemController : ISystemController
{
    public List<SystemSnapshot> Systems { get; } = [];

    public Task<IReadOnlyList<SystemSnapshot>> GetSystemsAsync() => Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems);
    public Task<int> GetCountAsync() => Task.FromResult(Systems.Count);
    public Task<SystemSnapshot?> GetSystemAsync(string name) => Task.FromResult(Systems.FirstOrDefault(s => s.Name == name));

    public Task<SystemSnapshot> EnableSystemAsync(string name) =>
        Task.FromResult(Systems.First(s => s.Name == name));

    public Task<SystemSnapshot> DisableSystemAsync(string name) =>
        Task.FromResult(Systems.First(s => s.Name == name));

    public Task<SystemSnapshot> ToggleSystemAsync(string name) =>
        Task.FromResult(Systems.First(s => s.Name == name));

    public Task<IReadOnlyList<SystemSnapshot>> GetSystemsByPhaseAsync(string phase) =>
        Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems.Where(s => s.Phase == phase).ToList());

    public Task<IReadOnlyList<SystemSnapshot>> GetEnabledSystemsAsync() =>
        Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems.Where(s => s.Enabled).ToList());

    public Task<IReadOnlyList<SystemSnapshot>> GetDisabledSystemsAsync() =>
        Task.FromResult<IReadOnlyList<SystemSnapshot>>(Systems.Where(s => !s.Enabled).ToList());
}

/// <summary>
/// Mock implementation of IMutationController for testing.
/// </summary>
internal sealed class MockMutationController : IMutationController
{
    public Task<EntityResult> SpawnAsync(string? name = null, IReadOnlyList<ComponentData>? components = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityResult { Success = true, EntityId = 1, EntityVersion = 1 });

    public Task<bool> DespawnAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<EntityResult> CloneAsync(int entityId, string? name = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new EntityResult { Success = true, EntityId = 2, EntityVersion = 1 });

    public Task<bool> SetNameAsync(int entityId, string name, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> ClearNameAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> SetParentAsync(int entityId, int? parentId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IReadOnlyList<int>> GetRootEntitiesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<int>>([]);
    public Task<bool> AddComponentAsync(int entityId, string componentType, JsonElement? data = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RemoveComponentAsync(int entityId, string componentType, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> SetComponentAsync(int entityId, string componentType, JsonElement data, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> SetFieldAsync(int entityId, string componentType, string fieldName, JsonElement value, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> AddTagAsync(int entityId, string tag, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RemoveTagAsync(int entityId, string tag, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
}

/// <summary>
/// Mock implementation of IProfileController for testing.
/// </summary>
internal sealed class MockProfileController : IProfileController
{
    public Task<bool> IsDebugModeEnabledAsync() => Task.FromResult(false);
    public Task EnableDebugModeAsync() => Task.CompletedTask;
    public Task DisableDebugModeAsync() => Task.CompletedTask;
    public Task<bool> IsProfilingAvailableAsync() => Task.FromResult(false);
    public Task<SystemProfileSnapshot?> GetSystemProfileAsync(string systemName) => Task.FromResult<SystemProfileSnapshot?>(null);
    public Task<IReadOnlyList<SystemProfileSnapshot>> GetAllSystemProfilesAsync() => Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>([]);
    public Task<IReadOnlyList<SystemProfileSnapshot>> GetSlowestSystemsAsync(int count = 10) => Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>([]);
    public Task ResetSystemProfilesAsync() => Task.CompletedTask;
    public Task<bool> IsQueryProfilingAvailableAsync() => Task.FromResult(false);
    public Task<QueryProfileSnapshot?> GetQueryProfileAsync(string queryName) => Task.FromResult<QueryProfileSnapshot?>(null);
    public Task<IReadOnlyList<QueryProfileSnapshot>> GetAllQueryProfilesAsync() => Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>([]);
    public Task<IReadOnlyList<QueryProfileSnapshot>> GetSlowestQueriesAsync(int count = 10) => Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>([]);

    public Task<QueryCacheStatsSnapshot> GetQueryCacheStatsAsync() => Task.FromResult(new QueryCacheStatsSnapshot
    {
        CacheHits = 0,
        CacheMisses = 0,
        CachedQueryCount = 0,
        HitRate = 0
    });

    public Task ResetQueryProfilesAsync() => Task.CompletedTask;
    public Task<bool> IsGCTrackingAvailableAsync() => Task.FromResult(false);
    public Task<AllocationProfileSnapshot?> GetAllocationProfileAsync(string systemName) => Task.FromResult<AllocationProfileSnapshot?>(null);
    public Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllAllocationProfilesAsync() => Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>([]);
    public Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllocationHotspotsAsync(int count = 10) => Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>([]);
    public Task ResetAllocationProfilesAsync() => Task.CompletedTask;
    public Task<bool> IsMemoryTrackingAvailableAsync() => Task.FromResult(false);

    public Task<MemoryStatsSnapshot> GetMemoryStatsAsync() => Task.FromResult(new MemoryStatsSnapshot
    {
        EntitiesAllocated = 0,
        EntitiesActive = 0,
        EntitiesRecycled = 0,
        EntityRecycleCount = 0,
        ArchetypeCount = 0,
        ComponentTypeCount = 0,
        SystemCount = 0,
        CachedQueryCount = 0,
        QueryCacheHits = 0,
        QueryCacheMisses = 0,
        EstimatedComponentBytes = 0,
        RecycleEfficiency = 0,
        QueryCacheHitRate = 0
    });

    public Task<IReadOnlyList<ArchetypeStatsSnapshot>> GetArchetypeStatsAsync() => Task.FromResult<IReadOnlyList<ArchetypeStatsSnapshot>>([]);
    public Task<bool> IsTimelineAvailableAsync() => Task.FromResult(false);

    public Task<TimelineStatsSnapshot> GetTimelineStatsAsync() => Task.FromResult(new TimelineStatsSnapshot
    {
        IsRecording = false,
        CurrentFrame = 0,
        EntryCount = 0
    });

    public Task EnableTimelineRecordingAsync() => Task.CompletedTask;
    public Task DisableTimelineRecordingAsync() => Task.CompletedTask;
    public Task<IReadOnlyList<TimelineEntrySnapshot>> GetTimelineEntriesForFrameAsync(long frameNumber) => Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>([]);
    public Task<IReadOnlyList<TimelineEntrySnapshot>> GetRecentTimelineEntriesAsync(int count = 100) => Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>([]);
    public Task<IReadOnlyList<TimelineSystemStatsSnapshot>> GetTimelineSystemStatsAsync() => Task.FromResult<IReadOnlyList<TimelineSystemStatsSnapshot>>([]);
    public Task ResetTimelineAsync() => Task.CompletedTask;
}

/// <summary>
/// Mock implementation of ISnapshotController for testing.
/// </summary>
internal sealed class MockSnapshotController : ISnapshotController
{
    public Task<SnapshotResult> CreateAsync(string name) => Task.FromResult(new SnapshotResult { Success = true });
    public Task<SnapshotResult> RestoreAsync(string name) => Task.FromResult(new SnapshotResult { Success = true });
    public Task<bool> DeleteAsync(string name) => Task.FromResult(true);
    public Task<IReadOnlyList<SnapshotInfo>> ListAsync() => Task.FromResult<IReadOnlyList<SnapshotInfo>>([]);
    public Task<SnapshotInfo?> GetInfoAsync(string name) => Task.FromResult<SnapshotInfo?>(null);

    public Task<SnapshotDiff> DiffAsync(string name1, string name2) => Task.FromResult(new SnapshotDiff
    {
        Snapshot1 = name1,
        Snapshot2 = name2,
        AddedEntities = [],
        RemovedEntities = [],
        ModifiedEntities = [],
        TotalChanges = 0
    });

    public Task<SnapshotDiff> DiffCurrentAsync(string name) => Task.FromResult(new SnapshotDiff
    {
        Snapshot1 = name,
        Snapshot2 = "current",
        AddedEntities = [],
        RemovedEntities = [],
        ModifiedEntities = [],
        TotalChanges = 0
    });

    public Task<SnapshotResult> SaveToFileAsync(string name, string path) => Task.FromResult(new SnapshotResult { Success = true });
    public Task<SnapshotResult> LoadFromFileAsync(string path, string? name = null) => Task.FromResult(new SnapshotResult { Success = true });
    public Task<string> ExportJsonAsync(string name) => Task.FromResult("{}");
    public Task<SnapshotResult> ImportJsonAsync(string json, string name) => Task.FromResult(new SnapshotResult { Success = true });
    public Task<SnapshotResult> QuickSaveAsync() => Task.FromResult(new SnapshotResult { Success = true });
    public Task<SnapshotResult> QuickLoadAsync() => Task.FromResult(new SnapshotResult { Success = true });
}

/// <summary>
/// Mock implementation of IAIController for testing.
/// </summary>
internal sealed class MockAIController : IAIController
{
    public Task<AIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AIStatisticsSnapshot
        {
            BehaviorTreeCount = 0,
            StateMachineCount = 0,
            UtilityAICount = 0,
            ActiveBehaviorTreeCount = 0,
            ActiveStateMachineCount = 0,
            ActiveUtilityAICount = 0
        });

    public Task<IReadOnlyList<int>> GetBehaviorTreeEntitiesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<int>>([]);
    public Task<BehaviorTreeSnapshot?> GetBehaviorTreeStateAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult<BehaviorTreeSnapshot?>(null);
    public Task<bool> ResetBehaviorTreeAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IReadOnlyList<int>> GetStateMachineEntitiesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<int>>([]);
    public Task<StateMachineSnapshot?> GetStateMachineStateAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult<StateMachineSnapshot?>(null);
    public Task<bool> ForceStateTransitionAsync(int entityId, int stateIndex, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> ForceStateTransitionByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IReadOnlyList<int>> GetUtilityAIEntitiesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<int>>([]);
    public Task<UtilityAISnapshot?> GetUtilityAIStateAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult<UtilityAISnapshot?>(null);
    public Task<IReadOnlyList<UtilityScoreSnapshot>> ScoreAllActionsAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UtilityScoreSnapshot>>([]);
    public Task<bool> ForceUtilityEvaluationAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IReadOnlyList<BlackboardEntrySnapshot>> GetBlackboardAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BlackboardEntrySnapshot>>([]);
    public Task<BlackboardEntrySnapshot?> GetBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default) => Task.FromResult<BlackboardEntrySnapshot?>(null);
    public Task<bool> SetBlackboardValueAsync(int entityId, string key, JsonElement value, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> RemoveBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> ClearBlackboardAsync(int entityId, CancellationToken cancellationToken = default) => Task.FromResult(true);
}

/// <summary>
/// Mock implementation of IReplayController for testing.
/// </summary>
internal sealed class MockReplayController : IReplayController
{
    public Task<ReplayOperationResult> StartRecordingAsync(string? name = null, int maxFrames = 36000, int snapshotIntervalMs = 5000)
        => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> StopRecordingAsync() => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> CancelRecordingAsync() => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<bool> IsRecordingAsync() => Task.FromResult(false);
    public Task<RecordingInfoSnapshot?> GetRecordingInfoAsync() => Task.FromResult<RecordingInfoSnapshot?>(null);
    public Task<ReplayOperationResult> ForceSnapshotAsync() => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> SaveAsync(string path) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> LoadAsync(string path) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<IReadOnlyList<ReplayFileSnapshot>> ListAsync(string? directory = null) => Task.FromResult<IReadOnlyList<ReplayFileSnapshot>>([]);
    public Task<bool> DeleteAsync(string path) => Task.FromResult(true);
    public Task<ReplayMetadataSnapshot?> GetMetadataAsync(string path) => Task.FromResult<ReplayMetadataSnapshot?>(null);
    public Task<ReplayOperationResult> PlayAsync() => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> PauseAsync() => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> StopPlaybackAsync() => Task.FromResult(new ReplayOperationResult { Success = true });

    public Task<PlaybackStateSnapshot> GetPlaybackStateAsync() => Task.FromResult(new PlaybackStateSnapshot
    {
        IsLoaded = false,
        IsPlaying = false,
        IsPaused = false,
        IsStopped = true,
        CurrentFrame = 0,
        TotalFrames = 0,
        CurrentTimeSeconds = 0,
        TotalTimeSeconds = 0,
        PlaybackSpeed = 1.0f,
        ReplayName = null
    });

    public Task<ReplayOperationResult> SetSpeedAsync(float speed) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> SeekFrameAsync(int frame) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> SeekTimeAsync(float seconds) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> StepForwardAsync(int frames = 1) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayOperationResult> StepBackwardAsync(int frames = 1) => Task.FromResult(new ReplayOperationResult { Success = true });
    public Task<ReplayFrameSnapshot?> GetFrameAsync(int frame) => Task.FromResult<ReplayFrameSnapshot?>(null);
    public Task<IReadOnlyList<ReplayFrameSnapshot>> GetFrameRangeAsync(int startFrame, int count) => Task.FromResult<IReadOnlyList<ReplayFrameSnapshot>>([]);
    public Task<IReadOnlyList<InputEventSnapshot>> GetInputsAsync(int startFrame, int endFrame) => Task.FromResult<IReadOnlyList<InputEventSnapshot>>([]);
    public Task<IReadOnlyList<ReplayEventSnapshot>> GetEventsAsync(int startFrame, int endFrame) => Task.FromResult<IReadOnlyList<ReplayEventSnapshot>>([]);
    public Task<IReadOnlyList<SnapshotMarkerSnapshot>> GetSnapshotsAsync() => Task.FromResult<IReadOnlyList<SnapshotMarkerSnapshot>>([]);

    public Task<ValidationResultSnapshot> ValidateAsync(string path) => Task.FromResult(new ValidationResultSnapshot
    {
        IsValid = true,
        Path = path,
        Errors = null
    });

    public Task<DeterminismResultSnapshot> CheckDeterminismAsync() => Task.FromResult(new DeterminismResultSnapshot
    {
        IsDeterministic = true,
        TotalFramesChecked = 0,
        FramesWithChecksums = 0
    });
}

/// <summary>
/// Mock implementation of IPhysicsController for testing.
/// </summary>
internal sealed class MockPhysicsController : IPhysicsController
{
    public Task<PhysicsStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new PhysicsStatisticsSnapshot
        {
            BodyCount = 0,
            StaticCount = 0,
            InterpolationAlpha = 0
        });

    public Task<RayHitSnapshot?> RaycastAsync(Vector3Snapshot origin, Vector3Snapshot direction, float maxDistance, CancellationToken cancellationToken = default)
        => Task.FromResult<RayHitSnapshot?>(null);

    public Task<IReadOnlyList<int>> OverlapSphereAsync(Vector3Snapshot center, float radius, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<IReadOnlyList<int>> OverlapBoxAsync(Vector3Snapshot center, Vector3Snapshot halfExtents, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<PhysicsBodySnapshot?> GetBodyStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<PhysicsBodySnapshot?>(null);

    public Task<Vector3Snapshot?> GetVelocityAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<Vector3Snapshot?>(null);

    public Task<bool> SetVelocityAsync(int entityId, Vector3Snapshot velocity, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool?> IsAwakeAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<bool?>(true);

    public Task<bool> WakeUpAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> ApplyForceAsync(int entityId, Vector3Snapshot force, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> ApplyImpulseAsync(int entityId, Vector3Snapshot impulse, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<Vector3Snapshot> GetGravityAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new Vector3Snapshot { X = 0, Y = -9.81f, Z = 0 });

    public Task<bool> SetGravityAsync(Vector3Snapshot gravity, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

/// <summary>
/// Mock implementation of IAnimationController for testing.
/// </summary>
internal sealed class MockAnimationController : IAnimationController
{
    public Task<AnimationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AnimationStatisticsSnapshot
        {
            ClipCount = 0,
            ControllerCount = 0,
            SpriteSheetCount = 0,
            ActivePlayerCount = 0,
            ActiveAnimatorCount = 0
        });

    public Task<IReadOnlyList<int>> GetAnimationPlayerEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<AnimationPlayerSnapshot?> GetAnimationPlayerStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<AnimationPlayerSnapshot?>(null);

    public Task<bool> SetAnimationPlayerPlayingAsync(int entityId, bool isPlaying, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SetAnimationPlayerTimeAsync(int entityId, float time, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SetAnimationPlayerSpeedAsync(int entityId, float speed, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<int>> GetAnimatorEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<AnimatorSnapshot?> GetAnimatorStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<AnimatorSnapshot?>(null);

    public Task<bool> TriggerAnimatorStateAsync(int entityId, int stateHash, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> TriggerAnimatorStateByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<AnimationClipSnapshot?> GetClipInfoAsync(int clipId, CancellationToken cancellationToken = default)
        => Task.FromResult<AnimationClipSnapshot?>(null);

    public Task<IReadOnlyList<AnimationClipSnapshot>> ListClipsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AnimationClipSnapshot>>([]);
}

/// <summary>
/// Mock implementation of INavigationController for testing.
/// </summary>
internal sealed class MockNavigationController : INavigationController
{
    public Task<NavigationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new NavigationStatisticsSnapshot
        {
            IsReady = true,
            Strategy = "Mock",
            ActiveAgentCount = 0,
            PendingRequestCount = 0
        });

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<int>> GetNavigationEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<NavAgentSnapshot?> GetAgentStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<NavAgentSnapshot?>(null);

    public Task<NavPathSnapshot?> GetPathAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<NavPathSnapshot?>(null);

    public Task<bool> SetDestinationAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> StopAgentAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> ResumeAgentAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> WarpAgentAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<NavPathSnapshot?> FindPathAsync(float startX, float startY, float startZ, float endX, float endY, float endZ, CancellationToken cancellationToken = default)
        => Task.FromResult<NavPathSnapshot?>(null);

    public Task<bool> IsNavigableAsync(float x, float y, float z, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<NavPointSnapshot?> FindNearestPointAsync(float x, float y, float z, float searchRadius, CancellationToken cancellationToken = default)
        => Task.FromResult<NavPointSnapshot?>(null);
}

/// <summary>
/// Mock implementation of INetworkController for testing.
/// </summary>
internal sealed class MockNetworkController : INetworkController
{
    public Task<NetworkStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new NetworkStatisticsSnapshot
        {
            IsConnected = false,
            IsServer = false,
            IsClient = false,
            CurrentTick = 0,
            LocalClientId = 0,
            ClientCount = 0,
            NetworkedEntityCount = 0
        });

    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> IsServerAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<uint> GetCurrentTickAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0u);

    public Task<float> GetLatencyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0f);

    public Task<ConnectionStatsSnapshot?> GetConnectionStatsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<ConnectionStatsSnapshot?>(null);

    public Task<IReadOnlyList<ClientSnapshot>> GetConnectedClientsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ClientSnapshot>>([]);

    public Task<IReadOnlyList<int>> GetNetworkedEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<uint?> GetNetworkIdAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<uint?>(null);

    public Task<int?> GetOwnershipAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<int?>(null);

    public Task<ReplicationStateSnapshot?> GetReplicationStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<ReplicationStateSnapshot?>(null);
}

/// <summary>
/// Mock implementation of IUIController for testing.
/// </summary>
internal sealed class MockUIController : IUIController
{
    public Task<UIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new UIStatisticsSnapshot
        {
            TotalElementCount = 0,
            VisibleElementCount = 0,
            InteractableCount = 0,
            FocusedElementId = null
        });

    public Task<int?> GetFocusedElementAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<int?>(null);

    public Task<bool> SetFocusAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> ClearFocusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<UIElementSnapshot?> GetElementAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<UIElementSnapshot?>(null);

    public Task<IReadOnlyList<UIElementSnapshot>> GetElementTreeAsync(int? rootEntityId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UIElementSnapshot>>([]);

    public Task<IReadOnlyList<int>> GetRootElementsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<UIBoundsSnapshot?> GetElementBoundsAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<UIBoundsSnapshot?>(null);

    public Task<UIStyleSnapshot?> GetElementStyleAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<UIStyleSnapshot?>(null);

    public Task<UIInteractionSnapshot?> GetInteractionStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<UIInteractionSnapshot?>(null);

    public Task<int?> HitTestAsync(float x, float y, CancellationToken cancellationToken = default)
        => Task.FromResult<int?>(null);

    public Task<IReadOnlyList<int>> HitTestAllAsync(float x, float y, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<int?> FindElementByNameAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<int?>(null);

    public Task<bool> SimulateClickAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
