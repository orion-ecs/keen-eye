using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to set a component value on an entity.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
public sealed class SetComponentCommand<T> : IEditorCommand where T : struct, IComponent
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly T _newValue;
    private readonly T _oldValue;
    private readonly string _componentName;
    private DateTime _timestamp;

    /// <summary>
    /// Creates a new set component command.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="newValue">The new component value.</param>
    public SetComponentCommand(World world, Entity entity, T newValue)
    {
        _world = world;
        _entity = entity;
        _newValue = newValue;
        _oldValue = world.Has<T>(entity) ? world.Get<T>(entity) : default;
        _componentName = typeof(T).Name;
        _timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string Description => $"Modify {_componentName}";

    /// <inheritdoc/>
    public void Execute()
    {
        if (_world.Has<T>(_entity))
        {
            _world.Get<T>(_entity) = _newValue;
        }
        else
        {
            _world.Add(_entity, _newValue);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_world.Has<T>(_entity))
        {
            _world.Get<T>(_entity) = _oldValue;
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other)
    {
        // Merge rapid changes to the same component on the same entity
        if (other is not SetComponentCommand<T> setCmd)
        {
            return false;
        }

        if (setCmd._entity != _entity)
        {
            return false;
        }

        // Only merge if within 300ms (for dragging/sliding values)
        var timeDelta = setCmd._timestamp - _timestamp;
        if (timeDelta.TotalMilliseconds > 300)
        {
            return false;
        }

        _timestamp = setCmd._timestamp;
        // Keep original _oldValue, update to newest value is already applied

        return true;
    }
}
