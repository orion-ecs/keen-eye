using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

public class RenameEntityCommandTests : IDisposable
{
    private readonly World world;

    public RenameEntityCommandTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDescription()
    {
        var entity = world.Spawn("OldName").Build();
        var command = new RenameEntityCommand(world, entity, "NewName");

        Assert.Equal("Rename to 'NewName'", command.Description);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_RenamesEntity()
    {
        var entity = world.Spawn("OldName").Build();
        var command = new RenameEntityCommand(world, entity, "NewName");

        command.Execute();

        Assert.Equal("NewName", world.GetName(entity));
    }

    [Fact]
    public void Execute_CanRenameToEmptyString()
    {
        var entity = world.Spawn("OldName").Build();
        var command = new RenameEntityCommand(world, entity, "");

        command.Execute();

        Assert.Equal("", world.GetName(entity));
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_RestoresOriginalName()
    {
        var entity = world.Spawn("OldName").Build();
        var command = new RenameEntityCommand(world, entity, "NewName");
        command.Execute();

        command.Undo();

        Assert.Equal("OldName", world.GetName(entity));
    }

    [Fact]
    public void Undo_RestoresEmptyName()
    {
        var entity = world.Spawn().Build(); // No name
        var command = new RenameEntityCommand(world, entity, "NewName");
        command.Execute();

        command.Undo();

        Assert.Equal("", world.GetName(entity));
    }

    #endregion

    #region TryMerge Tests

    [Fact]
    public void TryMerge_MergesRapidRenames()
    {
        var entity = world.Spawn("Original").Build();
        var command1 = new RenameEntityCommand(world, entity, "First");
        var command2 = new RenameEntityCommand(world, entity, "Second");

        var result = command1.TryMerge(command2);

        Assert.True(result);
    }

    [Fact]
    public void TryMerge_ReturnsFalse_ForDifferentEntities()
    {
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();
        var command1 = new RenameEntityCommand(world, entity1, "NewName1");
        var command2 = new RenameEntityCommand(world, entity2, "NewName2");

        Assert.False(command1.TryMerge(command2));
    }

    [Fact]
    public void TryMerge_ReturnsFalse_ForDifferentCommandType()
    {
        var entity = world.Spawn("Test").Build();
        var renameCmd = new RenameEntityCommand(world, entity, "NewName");
        var createCmd = new CreateEntityCommand(world, "Other");

        Assert.False(renameCmd.TryMerge(createCmd));
    }

    [Fact]
    public void TryMerge_PreservesOriginalOldName_AfterMerge()
    {
        var entity = world.Spawn("Original").Build();
        var command1 = new RenameEntityCommand(world, entity, "First");
        command1.Execute();

        var command2 = new RenameEntityCommand(world, entity, "Second");
        command1.TryMerge(command2);

        // After undo, should restore to "Original", not "First"
        command1.Undo();
        Assert.Equal("Original", world.GetName(entity));
    }

    #endregion
}
