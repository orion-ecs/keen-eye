using BepuPhysics;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests for BodyLookup bidirectional mapping.
/// </summary>
public class BodyLookupTests
{
    [Fact]
    public void RegisterBody_CreatesMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(1, 0);
        var bodyHandle = new BodyHandle(42);

        lookup.RegisterBody(entity, bodyHandle);

        Assert.True(lookup.TryGetBody(entity, out var retrievedHandle));
        Assert.Equal(bodyHandle, retrievedHandle);
    }

    [Fact]
    public void RegisterBody_CreatesBidirectionalMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(1, 0);
        var bodyHandle = new BodyHandle(42);

        lookup.RegisterBody(entity, bodyHandle);

        Assert.True(lookup.TryGetEntity(bodyHandle, out var retrievedEntity));
        Assert.Equal(entity, retrievedEntity);
    }

    [Fact]
    public void RegisterStatic_CreatesMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(2, 0);
        var staticHandle = new StaticHandle(10);

        lookup.RegisterStatic(entity, staticHandle);

        Assert.True(lookup.TryGetStatic(entity, out var retrievedHandle));
        Assert.Equal(staticHandle, retrievedHandle);
    }

    [Fact]
    public void RegisterStatic_CreatesBidirectionalMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(2, 0);
        var staticHandle = new StaticHandle(10);

        lookup.RegisterStatic(entity, staticHandle);

        Assert.True(lookup.TryGetEntity(staticHandle, out var retrievedEntity));
        Assert.Equal(entity, retrievedEntity);
    }

    [Fact]
    public void Unregister_RemovesDynamicBodyMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(3, 0);
        var bodyHandle = new BodyHandle(50);

        lookup.RegisterBody(entity, bodyHandle);
        var removed = lookup.Unregister(entity);

        Assert.True(removed);
        Assert.False(lookup.TryGetBody(entity, out _));
        Assert.False(lookup.TryGetEntity(bodyHandle, out _));
    }

    [Fact]
    public void Unregister_RemovesStaticBodyMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(4, 0);
        var staticHandle = new StaticHandle(20);

        lookup.RegisterStatic(entity, staticHandle);
        var removed = lookup.Unregister(entity);

        Assert.True(removed);
        Assert.False(lookup.TryGetStatic(entity, out _));
        Assert.False(lookup.TryGetEntity(staticHandle, out _));
    }

    [Fact]
    public void Unregister_WithNonExistentEntity_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(999, 0);

        var removed = lookup.Unregister(entity);

        Assert.False(removed);
    }

    [Fact]
    public void HasBody_WithRegisteredBody_ReturnsTrue()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(5, 0);
        var bodyHandle = new BodyHandle(60);

        lookup.RegisterBody(entity, bodyHandle);

        Assert.True(lookup.HasBody(entity));
    }

    [Fact]
    public void HasBody_WithUnregisteredEntity_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(6, 0);

        Assert.False(lookup.HasBody(entity));
    }

    [Fact]
    public void HasStatic_WithRegisteredStatic_ReturnsTrue()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(7, 0);
        var staticHandle = new StaticHandle(30);

        lookup.RegisterStatic(entity, staticHandle);

        Assert.True(lookup.HasStatic(entity));
    }

    [Fact]
    public void HasStatic_WithUnregisteredEntity_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(8, 0);

        Assert.False(lookup.HasStatic(entity));
    }

    [Fact]
    public void DynamicEntities_ReturnsAllRegisteredDynamicEntities()
    {
        var lookup = new BodyLookup();
        var entity1 = new Entity(10, 0);
        var entity2 = new Entity(11, 0);
        var entity3 = new Entity(12, 0);

        lookup.RegisterBody(entity1, new BodyHandle(1));
        lookup.RegisterBody(entity2, new BodyHandle(2));
        lookup.RegisterBody(entity3, new BodyHandle(3));

        var dynamicEntities = lookup.DynamicEntities.ToList();

        Assert.Equal(3, dynamicEntities.Count);
        Assert.Contains(entity1, dynamicEntities);
        Assert.Contains(entity2, dynamicEntities);
        Assert.Contains(entity3, dynamicEntities);
    }

    [Fact]
    public void StaticEntities_ReturnsAllRegisteredStaticEntities()
    {
        var lookup = new BodyLookup();
        var entity1 = new Entity(20, 0);
        var entity2 = new Entity(21, 0);

        lookup.RegisterStatic(entity1, new StaticHandle(1));
        lookup.RegisterStatic(entity2, new StaticHandle(2));

        var staticEntities = lookup.StaticEntities.ToList();

        Assert.Equal(2, staticEntities.Count);
        Assert.Contains(entity1, staticEntities);
        Assert.Contains(entity2, staticEntities);
    }

    [Fact]
    public void Count_ReturnsCorrectTotal()
    {
        var lookup = new BodyLookup();

        lookup.RegisterBody(new Entity(1, 0), new BodyHandle(1));
        lookup.RegisterBody(new Entity(2, 0), new BodyHandle(2));
        lookup.RegisterStatic(new Entity(3, 0), new StaticHandle(1));

        Assert.Equal(3, lookup.Count);
    }

    [Fact]
    public void Clear_RemovesAllMappings()
    {
        var lookup = new BodyLookup();

        lookup.RegisterBody(new Entity(1, 0), new BodyHandle(1));
        lookup.RegisterBody(new Entity(2, 0), new BodyHandle(2));
        lookup.RegisterStatic(new Entity(3, 0), new StaticHandle(1));

        lookup.Clear();

        Assert.Equal(0, lookup.Count);
        Assert.Empty(lookup.DynamicEntities);
        Assert.Empty(lookup.StaticEntities);
    }

    [Fact]
    public void RegisterBody_OverwritesPreviousMapping()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(100, 0);
        var handle1 = new BodyHandle(10);
        var handle2 = new BodyHandle(20);

        lookup.RegisterBody(entity, handle1);
        lookup.RegisterBody(entity, handle2);

        Assert.True(lookup.TryGetBody(entity, out var retrievedHandle));
        Assert.Equal(handle2, retrievedHandle);
    }

    [Fact]
    public void TryGetBody_WithNonExistentEntity_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(999, 0);

        var found = lookup.TryGetBody(entity, out var handle);

        Assert.False(found);
        Assert.Equal(default, handle);
    }

    [Fact]
    public void TryGetStatic_WithNonExistentEntity_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(999, 0);

        var found = lookup.TryGetStatic(entity, out var handle);

        Assert.False(found);
        Assert.Equal(default, handle);
    }

    [Fact]
    public void TryGetEntity_WithNonExistentBodyHandle_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var bodyHandle = new BodyHandle(999);

        var found = lookup.TryGetEntity(bodyHandle, out var entity);

        Assert.False(found);
        Assert.Equal(default, entity);
    }

    [Fact]
    public void TryGetEntity_WithNonExistentStaticHandle_ReturnsFalse()
    {
        var lookup = new BodyLookup();
        var staticHandle = new StaticHandle(999);

        var found = lookup.TryGetEntity(staticHandle, out var entity);

        Assert.False(found);
        Assert.Equal(default, entity);
    }

    [Fact]
    public void BodyLookup_HandlesMultipleMappings()
    {
        var lookup = new BodyLookup();

        // Register 100 dynamic bodies
        for (int i = 0; i < 100; i++)
        {
            lookup.RegisterBody(new Entity(i, 0), new BodyHandle(i));
        }

        // Register 50 static bodies
        for (int i = 100; i < 150; i++)
        {
            lookup.RegisterStatic(new Entity(i, 0), new StaticHandle(i - 100));
        }

        Assert.Equal(150, lookup.Count);
        Assert.Equal(100, lookup.DynamicEntities.Count());
        Assert.Equal(50, lookup.StaticEntities.Count());
    }

    [Fact]
    public void BodyLookup_HandlesReregistration()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(1, 0);

        // Register as dynamic
        lookup.RegisterBody(entity, new BodyHandle(10));
        Assert.True(lookup.HasBody(entity));

        // Unregister
        lookup.Unregister(entity);
        Assert.False(lookup.HasBody(entity));

        // Re-register as static
        lookup.RegisterStatic(entity, new StaticHandle(5));
        Assert.True(lookup.HasStatic(entity));
    }
}
