namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to change the parent of an entity.
/// </summary>
public sealed class ReparentEntityCommand : IEditorCommand
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly Entity _newParent;
    private readonly Entity _oldParent;
    private readonly string _entityName;

    /// <summary>
    /// Creates a new reparent entity command.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to reparent.</param>
    /// <param name="newParent">The new parent entity (Entity.Null to make root).</param>
    public ReparentEntityCommand(World world, Entity entity, Entity newParent)
    {
        _world = world;
        _entity = entity;
        _newParent = newParent;
        _oldParent = world.GetParent(entity);
        _entityName = world.GetName(entity) ?? $"Entity {entity.Id}";
    }

    /// <inheritdoc/>
    public string Description
    {
        get
        {
            if (!_newParent.IsValid)
            {
                return $"Move '{_entityName}' to root";
            }

            var newParentName = _world.GetName(_newParent) ?? $"Entity {_newParent.Id}";
            return $"Move '{_entityName}' under '{newParentName}'";
        }
    }

    /// <inheritdoc/>
    public void Execute()
    {
        if (_newParent.IsValid)
        {
            _world.SetParent(_entity, _newParent);
        }
        else
        {
            _world.SetParent(_entity, Entity.Null);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_oldParent.IsValid && _world.IsAlive(_oldParent))
        {
            _world.SetParent(_entity, _oldParent);
        }
        else
        {
            _world.SetParent(_entity, Entity.Null);
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
