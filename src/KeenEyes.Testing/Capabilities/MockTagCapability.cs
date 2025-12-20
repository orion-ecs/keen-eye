using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="ITagCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock tracks string tag operations without requiring a real World.
/// </para>
/// </remarks>
public sealed class MockTagCapability : ITagCapability
{
    private readonly Dictionary<Entity, HashSet<string>> entityTags = [];
    private readonly Dictionary<string, HashSet<Entity>> tagEntities = [];
    private readonly List<(string Operation, Entity Entity, string Tag)> operationLog = [];

    /// <summary>
    /// Gets the log of all operations performed.
    /// </summary>
    public IReadOnlyList<(string Operation, Entity Entity, string Tag)> OperationLog => operationLog;

    /// <inheritdoc />
    public bool AddTag(Entity entity, string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        operationLog.Add(("AddTag", entity, tag));

        if (!entityTags.TryGetValue(entity, out var tags))
        {
            tags = [];
            entityTags[entity] = tags;
        }

        if (!tags.Add(tag))
        {
            return false;
        }

        if (!tagEntities.TryGetValue(tag, out var entities))
        {
            entities = [];
            tagEntities[tag] = entities;
        }

        entities.Add(entity);
        return true;
    }

    /// <inheritdoc />
    public bool RemoveTag(Entity entity, string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        operationLog.Add(("RemoveTag", entity, tag));

        if (!entityTags.TryGetValue(entity, out var tags) || !tags.Remove(tag))
        {
            return false;
        }

        if (tagEntities.TryGetValue(tag, out var entities))
        {
            entities.Remove(entity);
        }

        return true;
    }

    /// <inheritdoc />
    public bool HasTag(Entity entity, string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return entityTags.TryGetValue(entity, out var tags) && tags.Contains(tag);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetTags(Entity entity)
    {
        return entityTags.TryGetValue(entity, out var tags) ? tags : [];
    }

    /// <inheritdoc />
    public IEnumerable<Entity> QueryByTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return tagEntities.TryGetValue(tag, out var entities) ? entities : [];
    }

    /// <summary>
    /// Clears all tags and operation log.
    /// </summary>
    public void Clear()
    {
        entityTags.Clear();
        tagEntities.Clear();
        operationLog.Clear();
    }

    /// <summary>
    /// Sets up tags for an entity directly for testing.
    /// </summary>
    public void SetupTags(Entity entity, params string[] tags)
    {
        foreach (var tag in tags)
        {
            AddTag(entity, tag);
        }

        // Clear the operation log so setup doesn't appear in it
        operationLog.Clear();
    }
}
