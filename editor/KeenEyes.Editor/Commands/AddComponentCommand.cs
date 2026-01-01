using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to add a component to an entity.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
public sealed class AddComponentCommand<T> : IEditorCommand where T : struct, IComponent
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly T _value;
    private readonly string _componentName;
    private readonly string _entityName;

    /// <summary>
    /// Creates a new add component command.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="value">The component value to add.</param>
    public AddComponentCommand(World world, Entity entity, T value = default)
    {
        _world = world;
        _entity = entity;
        _value = value;
        _componentName = typeof(T).Name;
        _entityName = world.GetName(entity) ?? $"Entity {entity.Id}";
    }

    /// <inheritdoc/>
    public string Description => $"Add {_componentName} to '{_entityName}'";

    /// <inheritdoc/>
    public void Execute()
    {
        if (!_world.Has<T>(_entity))
        {
            _world.Add(_entity, _value);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_world.Has<T>(_entity))
        {
            _world.Remove<T>(_entity);
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
