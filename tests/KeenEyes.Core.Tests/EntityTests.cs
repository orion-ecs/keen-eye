namespace KeenEyes.Tests;

public class EntityTests
{
    [Fact]
    public void Entity_Null_HasNegativeId()
    {
        var nullEntity = Entity.Null;

        Assert.Equal(-1, nullEntity.Id);
        Assert.Equal(0, nullEntity.Version);
    }

    [Fact]
    public void Entity_Null_IsValid_ReturnsFalse()
    {
        var nullEntity = Entity.Null;

        Assert.False(nullEntity.IsValid);
    }

    [Fact]
    public void Entity_WithNonNegativeId_IsValid_ReturnsTrue()
    {
        var entity = new Entity(0, 1);

        Assert.True(entity.IsValid);
    }

    [Fact]
    public void Entity_ToString_ReturnsExpectedFormat()
    {
        var entity = new Entity(42, 3);

        Assert.Equal("Entity(42v3)", entity.ToString());
    }

    [Fact]
    public void Entity_Equality_WorksCorrectly()
    {
        var entity1 = new Entity(1, 1);
        var entity2 = new Entity(1, 1);
        var entity3 = new Entity(1, 2);
        var entity4 = new Entity(2, 1);

        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, entity3);
        Assert.NotEqual(entity1, entity4);
    }
}
