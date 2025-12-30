using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockTagCapabilityTests
{
    #region AddTag

    [Fact]
    public void AddTag_WithNewTag_ReturnsTrue()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        var result = capability.AddTag(entity, "Player");

        Assert.True(result);
    }

    [Fact]
    public void AddTag_WithExistingTag_ReturnsFalse()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");

        var result = capability.AddTag(entity, "Player");

        Assert.False(result);
    }

    [Fact]
    public void AddTag_WithNullTag_ThrowsArgumentNullException()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        Assert.Throws<ArgumentNullException>(() => capability.AddTag(entity, null!));
    }

    [Fact]
    public void AddTag_WithEmptyTag_ThrowsArgumentException()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        Assert.Throws<ArgumentException>(() => capability.AddTag(entity, ""));
    }

    [Fact]
    public void AddTag_WithWhitespaceTag_ThrowsArgumentException()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        Assert.Throws<ArgumentException>(() => capability.AddTag(entity, "   "));
    }

    [Fact]
    public void AddTag_LogsOperation()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        capability.AddTag(entity, "Player");

        Assert.Single(capability.OperationLog);
        Assert.Equal("AddTag", capability.OperationLog[0].Operation);
        Assert.Equal(entity, capability.OperationLog[0].Entity);
        Assert.Equal("Player", capability.OperationLog[0].Tag);
    }

    #endregion

    #region RemoveTag

    [Fact]
    public void RemoveTag_WhenTagExists_ReturnsTrue()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");

        var result = capability.RemoveTag(entity, "Player");

        Assert.True(result);
    }

    [Fact]
    public void RemoveTag_WhenTagDoesNotExist_ReturnsFalse()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        var result = capability.RemoveTag(entity, "Player");

        Assert.False(result);
    }

    [Fact]
    public void RemoveTag_WithNullTag_ThrowsArgumentNullException()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        Assert.Throws<ArgumentNullException>(() => capability.RemoveTag(entity, null!));
    }

    [Fact]
    public void RemoveTag_LogsOperation()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");

        capability.RemoveTag(entity, "Player");

        Assert.Equal(2, capability.OperationLog.Count);
        Assert.Equal("RemoveTag", capability.OperationLog[1].Operation);
    }

    #endregion

    #region HasTag

    [Fact]
    public void HasTag_WhenTagExists_ReturnsTrue()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");

        var result = capability.HasTag(entity, "Player");

        Assert.True(result);
    }

    [Fact]
    public void HasTag_WhenTagDoesNotExist_ReturnsFalse()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        var result = capability.HasTag(entity, "Player");

        Assert.False(result);
    }

    [Fact]
    public void HasTag_WhenEntityHasOtherTags_ReturnsFalse()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Enemy");

        var result = capability.HasTag(entity, "Player");

        Assert.False(result);
    }

    [Fact]
    public void HasTag_WithNullTag_ThrowsArgumentNullException()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        Assert.Throws<ArgumentNullException>(() => capability.HasTag(entity, null!));
    }

    #endregion

    #region GetTags

    [Fact]
    public void GetTags_WithTags_ReturnsAllTags()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");
        capability.AddTag(entity, "Active");

        var tags = capability.GetTags(entity);

        Assert.Equal(2, tags.Count);
        Assert.Contains("Player", tags);
        Assert.Contains("Active", tags);
    }

    [Fact]
    public void GetTags_WithNoTags_ReturnsEmpty()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        var tags = capability.GetTags(entity);

        Assert.Empty(tags);
    }

    #endregion

    #region QueryByTag

    [Fact]
    public void QueryByTag_ReturnsMatchingEntities()
    {
        var capability = new MockTagCapability();
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(2, 0);
        var entity3 = new Entity(3, 0);
        capability.AddTag(entity1, "Player");
        capability.AddTag(entity2, "Player");
        capability.AddTag(entity3, "Enemy");

        var players = capability.QueryByTag("Player").ToList();

        Assert.Equal(2, players.Count);
        Assert.Contains(entity1, players);
        Assert.Contains(entity2, players);
    }

    [Fact]
    public void QueryByTag_WithNoMatches_ReturnsEmpty()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");

        var enemies = capability.QueryByTag("Enemy");

        Assert.Empty(enemies);
    }

    [Fact]
    public void QueryByTag_WithNullTag_ThrowsArgumentNullException()
    {
        var capability = new MockTagCapability();

        Assert.Throws<ArgumentNullException>(() => capability.QueryByTag(null!).ToList());
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllTagsAndLogs()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);
        capability.AddTag(entity, "Player");
        capability.AddTag(entity, "Active");

        capability.Clear();

        Assert.Empty(capability.GetTags(entity));
        Assert.Empty(capability.OperationLog);
        Assert.Empty(capability.QueryByTag("Player"));
    }

    #endregion

    #region SetupTags

    [Fact]
    public void SetupTags_AddsTags()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        capability.SetupTags(entity, "Player", "Active", "Visible");

        var tags = capability.GetTags(entity);
        Assert.Equal(3, tags.Count);
        Assert.Contains("Player", tags);
        Assert.Contains("Active", tags);
        Assert.Contains("Visible", tags);
    }

    [Fact]
    public void SetupTags_ClearsOperationLog()
    {
        var capability = new MockTagCapability();
        var entity = new Entity(1, 0);

        capability.SetupTags(entity, "Player", "Active");

        Assert.Empty(capability.OperationLog);
    }

    #endregion
}
