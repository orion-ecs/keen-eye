using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to delete an entity from the scene.
/// </summary>
public sealed class DeleteEntityCommand : IEditorCommand
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly string _entityName;
    private readonly Entity _parent;
    private readonly List<Entity> _children;
    private EntitySnapshot? _snapshot;

    /// <summary>
    /// Creates a new delete entity command.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to delete.</param>
    public DeleteEntityCommand(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
        _entityName = world.GetName(entity) ?? $"Entity {entity.Id}";
        _parent = world.GetParent(entity);
        _children = world.GetChildren(entity).ToList();
    }

    /// <inheritdoc/>
    public string Description => $"Delete Entity '{_entityName}'";

    /// <inheritdoc/>
    public void Execute()
    {
        // Capture entity state before deletion for undo
        _snapshot = CaptureEntitySnapshot(_entity);

        // Reparent children to grandparent (or make them root)
        foreach (var child in _children)
        {
            if (_parent.IsValid)
            {
                _world.SetParent(child, _parent);
            }
            else
            {
                _world.SetParent(child, Entity.Null);
            }
        }

        _world.Despawn(_entity);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_snapshot is null)
        {
            return;
        }

        // Recreate the entity
        var builder = _world.Spawn(_entityName);

        // Restore components from snapshot
        // Note: In a full implementation, we'd use reflection or a component factory
        // to restore the exact component data. For now, we just recreate the entity.
        _ = _snapshot.Components; // Placeholder for future component restoration

        var restoredEntity = builder.Build();

        // Restore parent relationship
        if (_parent.IsValid && _world.IsAlive(_parent))
        {
            _world.SetParent(restoredEntity, _parent);
        }

        // Restore children
        foreach (var child in _children)
        {
            if (_world.IsAlive(child))
            {
                _world.SetParent(child, restoredEntity);
            }
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;

    private EntitySnapshot CaptureEntitySnapshot(Entity entity)
    {
        // In a full implementation, this would capture all component data
        // For now, we just capture the entity ID and name
        return new EntitySnapshot
        {
            EntityId = entity.Id,
            Name = _entityName,
            Components = []
        };
    }

    private sealed class EntitySnapshot
    {
        public required int EntityId { get; init; }
        public required string Name { get; init; }
        public required List<object> Components { get; init; }
    }
}
