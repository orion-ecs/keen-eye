using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to remove a component from an entity.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
public sealed class RemoveComponentCommand<T> : IEditorCommand where T : struct, IComponent
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly T _previousValue;
    private readonly bool _hadComponent;
    private readonly string _componentName;
    private readonly string _entityName;

    /// <summary>
    /// Creates a new remove component command.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to remove the component from.</param>
    public RemoveComponentCommand(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
        _hadComponent = world.Has<T>(entity);
        _previousValue = _hadComponent ? world.Get<T>(entity) : default;
        _componentName = typeof(T).Name;
        _entityName = world.GetName(entity) ?? $"Entity {entity.Id}";
    }

    /// <inheritdoc/>
    public string Description => $"Remove {_componentName} from '{_entityName}'";

    /// <inheritdoc/>
    public void Execute()
    {
        if (_world.Has<T>(_entity))
        {
            _world.Remove<T>(_entity);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_hadComponent && !_world.Has<T>(_entity))
        {
            _world.Add(_entity, _previousValue);
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
