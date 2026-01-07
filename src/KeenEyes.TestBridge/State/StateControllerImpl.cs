using System.Diagnostics.CodeAnalysis;
using KeenEyes.Capabilities;
using KeenEyes.TestBridge.State;

namespace KeenEyes.TestBridge.State;

/// <summary>
/// In-process implementation of <see cref="IStateController"/> using world capabilities.
/// </summary>
internal sealed class StateControllerImpl(World world) : IStateController
{
    private readonly IInspectionCapability? inspectionCapability = world as IInspectionCapability;
    private readonly IStatisticsCapability? statisticsCapability = world as IStatisticsCapability;

    // Frame timing for performance metrics
    private readonly Queue<double> frameTimes = new();
    private readonly Dictionary<string, Queue<double>> systemTimes = [];
    private long frameNumber;
    private readonly DateTime startTime = DateTime.UtcNow;

    /// <summary>
    /// Records frame time for performance metrics.
    /// </summary>
    /// <param name="frameTimeMs">The frame time in milliseconds.</param>
    internal void RecordFrameTime(double frameTimeMs)
    {
        frameNumber++;
        frameTimes.Enqueue(frameTimeMs);

        // Keep last 300 frames
        while (frameTimes.Count > 300)
        {
            frameTimes.Dequeue();
        }
    }

    /// <summary>
    /// Records system execution time.
    /// </summary>
    /// <param name="systemName">The system type name.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    internal void RecordSystemTime(string systemName, double executionTimeMs)
    {
        if (!systemTimes.TryGetValue(systemName, out var times))
        {
            times = new Queue<double>();
            systemTimes[systemName] = times;
        }

        times.Enqueue(executionTimeMs);

        // Keep last 300 samples
        while (times.Count > 300)
        {
            times.Dequeue();
        }
    }

    #region Entity Queries

    public Task<int> GetEntityCountAsync()
    {
        return Task.FromResult(world.EntityCount);
    }

    public Task<IReadOnlyList<EntitySnapshot>> QueryEntitiesAsync(EntityQuery query)
    {
        var results = new List<EntitySnapshot>();
        var count = 0;
        var skipped = 0;

        foreach (var entity in world.GetAllEntities())
        {
            if (count >= query.MaxResults)
            {
                break;
            }

            // Apply filters
            if (!MatchesQuery(entity, query))
            {
                continue;
            }

            // Skip for pagination
            if (skipped < query.Skip)
            {
                skipped++;
                continue;
            }

            results.Add(CreateEntitySnapshot(entity, includeComponentData: false));
            count++;
        }

        return Task.FromResult<IReadOnlyList<EntitySnapshot>>(results);
    }

    public Task<EntitySnapshot?> GetEntityAsync(int entityId)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.Id == entityId)
            {
                return Task.FromResult<EntitySnapshot?>(CreateEntitySnapshot(entity, includeComponentData: true));
            }
        }

        return Task.FromResult<EntitySnapshot?>(null);
    }

    public Task<EntitySnapshot?> GetEntityByNameAsync(string name)
    {
        var entity = world.GetEntityByName(name);
        if (!entity.IsValid || !world.IsAlive(entity))
        {
            return Task.FromResult<EntitySnapshot?>(null);
        }

        return Task.FromResult<EntitySnapshot?>(CreateEntitySnapshot(entity, includeComponentData: true));
    }

    public Task<IReadOnlyDictionary<string, object?>?> GetComponentAsync(int entityId, string componentTypeName)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.Id == entityId)
            {
                foreach (var (type, value) in world.GetComponents(entity))
                {
                    if (type.Name == componentTypeName || type.FullName == componentTypeName)
                    {
                        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(
                            SerializeComponent(type, value));
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
    }

    #endregion

    #region World Statistics

    public Task<WorldStats> GetWorldStatsAsync()
    {
        long memoryBytes = 0;
        int archetypeCount = 0;
        int componentTypeCount = 0;
        int pluginCount = 0;

        if (statisticsCapability != null)
        {
            var memStats = statisticsCapability.GetMemoryStats();
            memoryBytes = memStats.EstimatedComponentBytes;
            archetypeCount = memStats.ArchetypeCount;
        }

        if (inspectionCapability != null)
        {
            componentTypeCount = inspectionCapability.GetRegisteredComponents().Count();
        }

        // Count systems (we'll need to estimate this)
        var systemCount = systemTimes.Count;

        return Task.FromResult(new WorldStats
        {
            EntityCount = world.EntityCount,
            ArchetypeCount = archetypeCount,
            SystemCount = systemCount,
            MemoryBytes = memoryBytes,
            ComponentTypeCount = componentTypeCount,
            PluginCount = pluginCount,
            FrameNumber = frameNumber,
            ElapsedTime = DateTime.UtcNow - startTime
        });
    }

    public Task<IReadOnlyList<SystemInfo>> GetSystemsAsync()
    {
        // Return info for systems we've tracked
        var results = new List<SystemInfo>();

        foreach (var (systemName, times) in systemTimes)
        {
            var avgMs = times.Count > 0 ? times.Average() : 0;

            results.Add(new SystemInfo
            {
                TypeName = systemName,
                Phase = "Unknown", // Would need system registration info
                Order = 0,
                Enabled = true,
                AverageExecutionMs = avgMs,
                GroupName = null
            });
        }

        return Task.FromResult<IReadOnlyList<SystemInfo>>(results);
    }

    public Task<PerformanceMetrics> GetPerformanceMetricsAsync(int frameCount = 60)
    {
        var samples = frameTimes.TakeLast(frameCount).ToList();

        double avgFrameTime = 0;
        double minFrameTime = 0;
        double maxFrameTime = 0;
        double avgFps = 0;
        double p99FrameTime = 0;

        if (samples.Count > 0)
        {
            avgFrameTime = samples.Average();
            minFrameTime = samples.Min();
            maxFrameTime = samples.Max();
            avgFps = avgFrameTime > 0 ? 1000.0 / avgFrameTime : 0;

            // Calculate P99
            var sorted = samples.OrderBy(x => x).ToList();
            var p99Index = (int)(sorted.Count * 0.99);
            p99FrameTime = sorted[Math.Min(p99Index, sorted.Count - 1)];
        }

        var systemAverages = new Dictionary<string, double>();
        foreach (var (systemName, times) in systemTimes)
        {
            var recentTimes = times.TakeLast(frameCount).ToList();
            if (recentTimes.Count > 0)
            {
                systemAverages[systemName] = recentTimes.Average();
            }
        }

        return Task.FromResult(new PerformanceMetrics
        {
            AverageFrameTimeMs = avgFrameTime,
            MinFrameTimeMs = minFrameTime,
            MaxFrameTimeMs = maxFrameTime,
            AverageFps = avgFps,
            P99FrameTimeMs = p99FrameTime,
            SampleCount = samples.Count,
            SystemAverages = systemAverages
        });
    }

    #endregion

    #region Extensions

    public Task<object?> GetExtensionAsync(string typeName)
    {
        // We can't easily get extensions by type name without reflection
        // This would need to be implemented with a registry pattern
        return Task.FromResult<object?>(null);
    }

    public Task<bool> HasExtensionAsync(string typeName)
    {
        // Same limitation as GetExtensionAsync
        return Task.FromResult(false);
    }

    #endregion

    #region Tags

    public Task<IReadOnlyList<int>> GetEntitiesWithTagAsync(string tag)
    {
        var results = new List<int>();

        // Tags are typically implemented as tag components
        // For now, return empty list - would need tag manager access
        return Task.FromResult<IReadOnlyList<int>>(results);
    }

    #endregion

    #region Hierarchy

    public Task<IReadOnlyList<int>> GetChildrenAsync(int parentId)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.Id == parentId)
            {
                var children = world.GetChildren(entity).Select(e => e.Id).ToList();
                return Task.FromResult<IReadOnlyList<int>>(children);
            }
        }

        return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    public Task<int?> GetParentAsync(int entityId)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.Id == entityId)
            {
                var parent = world.GetParent(entity);
                if (parent.IsValid)
                {
                    return Task.FromResult<int?>(parent.Id);
                }
                break;
            }
        }

        return Task.FromResult<int?>(null);
    }

    #endregion

    #region Private Helpers

    private bool MatchesQuery(Entity entity, EntityQuery query)
    {
        // Check name pattern
        if (!string.IsNullOrEmpty(query.NamePattern))
        {
            var name = world.GetName(entity);
            if (name == null || !MatchesWildcard(name, query.NamePattern))
            {
                return false;
            }
        }

        // Check parent
        if (query.ParentId.HasValue)
        {
            var parent = world.GetParent(entity);
            if (!parent.IsValid || parent.Id != query.ParentId.Value)
            {
                return false;
            }
        }

        // Check WithComponents
        if (query.WithComponents != null && query.WithComponents.Length > 0)
        {
            var entityComponents = world.GetComponents(entity)
                .Select(c => c.Type.Name)
                .ToHashSet();

            foreach (var requiredComponent in query.WithComponents)
            {
                if (!entityComponents.Contains(requiredComponent))
                {
                    return false;
                }
            }
        }

        // Check WithoutComponents
        if (query.WithoutComponents != null && query.WithoutComponents.Length > 0)
        {
            var entityComponents = world.GetComponents(entity)
                .Select(c => c.Type.Name)
                .ToHashSet();

            foreach (var excludedComponent in query.WithoutComponents)
            {
                if (entityComponents.Contains(excludedComponent))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool MatchesWildcard(string text, string pattern)
    {
        // Simple wildcard matching: * = any chars, ? = single char
        var regexPattern = "^" +
            System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") +
            "$";

        return System.Text.RegularExpressions.Regex.IsMatch(
            text,
            regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private EntitySnapshot CreateEntitySnapshot(Entity entity, bool includeComponentData)
    {
        var name = world.GetName(entity);
        var parent = world.GetParent(entity);
        var children = world.GetChildren(entity).Select(e => e.Id).ToArray();

        var components = world.GetComponents(entity).ToList();
        var componentTypes = components.Select(c => c.Type.Name).ToArray();

        var componentData = new Dictionary<string, IReadOnlyDictionary<string, object?>>();
        if (includeComponentData)
        {
            foreach (var (type, value) in components)
            {
                componentData[type.Name] = SerializeComponent(type, value);
            }
        }

        return new EntitySnapshot
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = name,
            Components = componentData,
            ComponentTypes = componentTypes,
            ParentId = parent.IsValid ? parent.Id : null,
            ChildIds = children,
            Tags = [] // Would need tag manager access
        };
    }

    private static IReadOnlyDictionary<string, object?> SerializeComponent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        object value)
    {
        var result = new Dictionary<string, object?>();

        // Get public fields and properties
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            result[field.Name] = field.GetValue(value);
        }

        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (prop.CanRead)
            {
                try
                {
                    result[prop.Name] = prop.GetValue(value);
                }
                catch
                {
                    // Skip properties that throw
                }
            }
        }

        return result;
    }

    #endregion
}
