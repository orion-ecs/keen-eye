namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to create a new entity in the scene.
/// </summary>
public sealed class CreateEntityCommand : IEditorCommand
{
    private readonly World _world;
    private readonly string _name;
    private readonly Entity _parent;
    private Entity _createdEntity;

    /// <summary>
    /// Creates a new create entity command.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="name">The name for the new entity.</param>
    /// <param name="parent">Optional parent entity.</param>
    public CreateEntityCommand(World world, string name, Entity parent = default)
    {
        _world = world;
        _name = name;
        _parent = parent;
    }

    /// <inheritdoc/>
    public string Description => $"Create Entity '{_name}'";

    /// <summary>
    /// Gets the entity that was created by this command.
    /// </summary>
    public Entity CreatedEntity => _createdEntity;

    /// <inheritdoc/>
    public void Execute()
    {
        _createdEntity = _world.Spawn(_name).Build();

        if (_parent.IsValid && _world.IsAlive(_parent))
        {
            _world.SetParent(_createdEntity, _parent);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_createdEntity.IsValid)
        {
            _world.Despawn(_createdEntity);
            _createdEntity = Entity.Null;
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
