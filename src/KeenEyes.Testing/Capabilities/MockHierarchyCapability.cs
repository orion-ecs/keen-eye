using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="IHierarchyCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock tracks hierarchy operations and allows configuring custom parent-child
/// relationships for testing without a real World.
/// </para>
/// </remarks>
public sealed class MockHierarchyCapability : IHierarchyCapability
{
    private readonly Dictionary<Entity, Entity> parents = [];
    private readonly Dictionary<Entity, HashSet<Entity>> children = [];
    private readonly List<(string Operation, Entity Entity, Entity? Related)> operationLog = [];

    /// <summary>
    /// Gets the log of all operations performed.
    /// </summary>
    public IReadOnlyList<(string Operation, Entity Entity, Entity? Related)> OperationLog => operationLog;

    /// <summary>
    /// Gets or sets whether to throw on invalid operations (like setting circular parents).
    /// Default is false, which silently ignores invalid operations.
    /// </summary>
    public bool ThrowOnInvalidOperation { get; set; }

    /// <inheritdoc />
    public void SetParent(Entity child, Entity parent)
    {
        operationLog.Add(("SetParent", child, parent));

        if (!parent.IsValid)
        {
            parents.Remove(child);
            return;
        }

        // Check for circular reference
        if (ThrowOnInvalidOperation && WouldCreateCycle(child, parent))
        {
            throw new InvalidOperationException(
                $"Setting {parent} as parent of {child} would create a circular reference.");
        }

        parents[child] = parent;

        if (!children.TryGetValue(parent, out var childSet))
        {
            childSet = [];
            children[parent] = childSet;
        }

        childSet.Add(child);
    }

    /// <inheritdoc />
    public Entity GetParent(Entity entity)
    {
        return parents.TryGetValue(entity, out var parent) ? parent : Entity.Null;
    }

    /// <inheritdoc />
    public IEnumerable<Entity> GetChildren(Entity entity)
    {
        return children.TryGetValue(entity, out var childSet) ? childSet : [];
    }

    /// <inheritdoc />
    public void AddChild(Entity parent, Entity child)
    {
        operationLog.Add(("AddChild", parent, child));
        SetParent(child, parent);
    }

    /// <inheritdoc />
    public bool RemoveChild(Entity parent, Entity child)
    {
        operationLog.Add(("RemoveChild", parent, child));

        if (!parents.TryGetValue(child, out var currentParent) || currentParent != parent)
        {
            return false;
        }

        parents.Remove(child);

        if (children.TryGetValue(parent, out var childSet))
        {
            childSet.Remove(child);
        }

        return true;
    }

    /// <inheritdoc />
    public IEnumerable<Entity> GetDescendants(Entity entity)
    {
        var queue = new Queue<Entity>();
        foreach (var child in GetChildren(entity))
        {
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var child in GetChildren(current))
            {
                queue.Enqueue(child);
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<Entity> GetAncestors(Entity entity)
    {
        var current = GetParent(entity);
        while (current.IsValid)
        {
            yield return current;
            current = GetParent(current);
        }
    }

    /// <inheritdoc />
    public Entity GetRoot(Entity entity)
    {
        var current = entity;
        var parent = GetParent(current);

        while (parent.IsValid)
        {
            current = parent;
            parent = GetParent(current);
        }

        return current;
    }

    /// <inheritdoc />
    public int DespawnRecursive(Entity entity)
    {
        operationLog.Add(("DespawnRecursive", entity, null));

        var descendants = GetDescendants(entity).ToList();
        var count = descendants.Count + 1;

        // Remove all descendants
        foreach (var descendant in descendants)
        {
            parents.Remove(descendant);
            children.Remove(descendant);
        }

        // Remove the entity itself
        parents.Remove(entity);
        children.Remove(entity);

        return count;
    }

    /// <summary>
    /// Clears all hierarchy data and operation log.
    /// </summary>
    public void Clear()
    {
        parents.Clear();
        children.Clear();
        operationLog.Clear();
    }

    /// <summary>
    /// Sets up a parent-child relationship directly for testing.
    /// </summary>
    public void SetupHierarchy(Entity parent, params Entity[] childEntities)
    {
        foreach (var child in childEntities)
        {
            parents[child] = parent;

            if (!children.TryGetValue(parent, out var childSet))
            {
                childSet = [];
                children[parent] = childSet;
            }

            childSet.Add(child);
        }
    }

    private bool WouldCreateCycle(Entity child, Entity potentialParent)
    {
        var current = potentialParent;
        while (current.IsValid)
        {
            if (current == child)
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }
}
