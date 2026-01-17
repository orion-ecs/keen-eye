using System.Text.Json;
using KeenEyes.TestBridge.State;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IStateController for testing.
/// </summary>
internal sealed class MockStateController : IStateController
{
    public Dictionary<int, EntitySnapshot> Entities { get; } = [];
    public Dictionary<string, object?> Extensions { get; } = [];
    public List<SystemInfo> Systems { get; } = [];

    // Store component data separately for GetComponentAsync
    private readonly Dictionary<(int EntityId, string ComponentType), JsonElement> componentData = [];

    public WorldStats Stats { get; set; } = new()
    {
        EntityCount = 0,
        ArchetypeCount = 0,
        SystemCount = 0,
        MemoryBytes = 0,
        ComponentTypeCount = 0,
        PluginCount = 0,
        FrameNumber = 0,
        ElapsedTime = TimeSpan.Zero
    };

    public PerformanceMetrics Metrics { get; set; } = new()
    {
        AverageFrameTimeMs = 16.67,
        MinFrameTimeMs = 14.0,
        MaxFrameTimeMs = 20.0,
        AverageFps = 60.0,
        P99FrameTimeMs = 18.0,
        SampleCount = 60,
        SystemAverages = new Dictionary<string, double>()
    };

    public Task<int> GetEntityCountAsync() => Task.FromResult(Entities.Count);

    public Task<IReadOnlyList<EntitySnapshot>> QueryEntitiesAsync(EntityQuery query)
    {
        var results = Entities.Values.AsEnumerable();

        if (query.WithComponents?.Length > 0)
        {
            results = results.Where(e => query.WithComponents.All(c =>
                e.ComponentTypes.Contains(c, StringComparer.OrdinalIgnoreCase)));
        }

        if (query.WithoutComponents?.Length > 0)
        {
            results = results.Where(e => !query.WithoutComponents.Any(c =>
                e.ComponentTypes.Contains(c, StringComparer.OrdinalIgnoreCase)));
        }

        if (query.WithTags?.Length > 0)
        {
            results = results.Where(e => query.WithTags.All(t =>
                e.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrEmpty(query.NamePattern))
        {
            var pattern = query.NamePattern.Replace("*", ".*").Replace("?", ".");
            results = results.Where(e => e.Name != null &&
                System.Text.RegularExpressions.Regex.IsMatch(e.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }

        if (query.ParentId.HasValue)
        {
            results = results.Where(e => e.ParentId == query.ParentId);
        }

        return Task.FromResult<IReadOnlyList<EntitySnapshot>>(
            results.Skip(query.Skip).Take(query.MaxResults).ToList());
    }

    public Task<EntitySnapshot?> GetEntityAsync(int entityId)
    {
        return Task.FromResult(Entities.GetValueOrDefault(entityId));
    }

    public Task<EntitySnapshot?> GetEntityByNameAsync(string name)
    {
        var entity = Entities.Values.FirstOrDefault(e =>
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task<JsonElement?> GetComponentAsync(int entityId, string componentTypeName)
    {
        if (!Entities.TryGetValue(entityId, out var entity))
        {
            return Task.FromResult<JsonElement?>(null);
        }

        if (!entity.Components.TryGetValue(componentTypeName, out var component))
        {
            // Check the separate component data storage
            if (componentData.TryGetValue((entityId, componentTypeName), out var storedComponent))
            {
                return Task.FromResult<JsonElement?>(storedComponent);
            }

            return Task.FromResult<JsonElement?>(null);
        }

        return Task.FromResult<JsonElement?>(component);
    }

    public Task<WorldStats> GetWorldStatsAsync() => Task.FromResult(Stats);

    public Task<IReadOnlyList<SystemInfo>> GetSystemsAsync() =>
        Task.FromResult<IReadOnlyList<SystemInfo>>(Systems);

    public Task<object?> GetExtensionAsync(string typeName) =>
        Task.FromResult(Extensions.GetValueOrDefault(typeName));

    public Task<bool> HasExtensionAsync(string typeName) =>
        Task.FromResult(Extensions.ContainsKey(typeName));

    public Task<PerformanceMetrics> GetPerformanceMetricsAsync(int frameCount = 60) =>
        Task.FromResult(Metrics);

    public Task<IReadOnlyList<int>> GetEntitiesWithTagAsync(string tag)
    {
        var entityIds = Entities.Values
            .Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .Select(e => e.Id)
            .ToList();
        return Task.FromResult<IReadOnlyList<int>>(entityIds);
    }

    public Task<IReadOnlyList<int>> GetChildrenAsync(int parentId)
    {
        var children = Entities.Values
            .Where(e => e.ParentId == parentId)
            .Select(e => e.Id)
            .ToList();
        return Task.FromResult<IReadOnlyList<int>>(children);
    }

    public Task<int?> GetParentAsync(int entityId)
    {
        if (!Entities.TryGetValue(entityId, out var entity))
        {
            return Task.FromResult<int?>(null);
        }

        return Task.FromResult(entity.ParentId);
    }

    /// <summary>
    /// Helper to add a simple entity to the mock.
    /// </summary>
    public void AddEntity(int id, string? name = null, string[]? componentTypes = null, string[]? tags = null, int? parentId = null)
    {
        Entities[id] = new EntitySnapshot
        {
            Id = id,
            Version = 1,
            Name = name,
            Components = new Dictionary<string, JsonElement>(),
            ComponentTypes = componentTypes ?? [],
            ParentId = parentId,
            ChildIds = [],
            Tags = tags ?? []
        };
    }

    /// <summary>
    /// Helper to add an entity with component data.
    /// </summary>
    public void AddEntityWithComponents(int id, string? name, Dictionary<string, JsonElement> components)
    {
        Entities[id] = new EntitySnapshot
        {
            Id = id,
            Version = 1,
            Name = name,
            Components = components,
            ComponentTypes = [.. components.Keys],
            ParentId = null,
            ChildIds = [],
            Tags = []
        };

        // Also store in component data for GetComponentAsync
        foreach (var (componentType, data) in components)
        {
            componentData[(id, componentType)] = data;
        }
    }

    /// <summary>
    /// Helper to set component data for an entity.
    /// </summary>
    public void SetComponentData(int entityId, string componentType, JsonElement data)
    {
        componentData[(entityId, componentType)] = data;
    }
}
