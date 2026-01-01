using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Commands;

/// <summary>
/// Command to rename an entity.
/// </summary>
public sealed class RenameEntityCommand : IEditorCommand
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly string _newName;
    private readonly string _oldName;
    private DateTime _timestamp;

    /// <summary>
    /// Creates a new rename entity command.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to rename.</param>
    /// <param name="newName">The new name for the entity.</param>
    public RenameEntityCommand(World world, Entity entity, string newName)
    {
        _world = world;
        _entity = entity;
        _newName = newName;
        _oldName = world.GetName(entity) ?? "";
        _timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string Description => $"Rename to '{_newName}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _world.SetName(_entity, _newName);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        _world.SetName(_entity, _oldName);
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other)
    {
        // Merge rapid rename commands on the same entity (e.g., while typing)
        if (other is not RenameEntityCommand renameCmd)
        {
            return false;
        }

        if (renameCmd._entity != _entity)
        {
            return false;
        }

        // Only merge if within 500ms
        var timeDelta = renameCmd._timestamp - _timestamp;
        if (timeDelta.TotalMilliseconds > 500)
        {
            return false;
        }

        // Update to the newest name but keep the original old name
        _timestamp = renameCmd._timestamp;
        // The _newName stays as the current name (from this command)
        // We don't need to update anything since we already applied the name

        return true;
    }
}
