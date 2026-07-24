using BepuPhysics;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Behavior-preservation tests for the BodyLookup key-enumeration change (issue #1238).
/// DynamicEntities/StaticEntities now expose the concrete Dictionary KeyCollection so the
/// per-fixed-step foreach uses the value-type enumerator instead of boxing an IEnumerator.
/// These assert the concrete type is returned and that enumeration yields the correct keys.
/// </summary>
public class BodyLookupHotPathTests
{
    [Fact]
    public void DynamicEntities_ReturnsConcreteKeyCollection()
    {
        var lookup = new BodyLookup();
        lookup.RegisterBody(new Entity(1, 0), new BodyHandle(10));

        // Assigning to the concrete KeyCollection type compiles only if the property returns
        // it; foreach over this type binds to the struct enumerator (no boxing).
        Dictionary<Entity, BodyHandle>.KeyCollection keys = lookup.DynamicEntities;

        Assert.Single(keys);
    }

    [Fact]
    public void StaticEntities_ReturnsConcreteKeyCollection()
    {
        var lookup = new BodyLookup();
        lookup.RegisterStatic(new Entity(2, 0), new StaticHandle(20));

        Dictionary<Entity, StaticHandle>.KeyCollection keys = lookup.StaticEntities;

        Assert.Single(keys);
    }

    [Fact]
    public void DynamicEntities_EnumeratesAllRegisteredEntities()
    {
        var lookup = new BodyLookup();
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);
        var e3 = new Entity(3, 0);
        lookup.RegisterBody(e1, new BodyHandle(1));
        lookup.RegisterBody(e2, new BodyHandle(2));
        lookup.RegisterBody(e3, new BodyHandle(3));

        var seen = new HashSet<Entity>();
        foreach (var entity in lookup.DynamicEntities)
        {
            seen.Add(entity);
        }

        HashSet<Entity> expected = [e1, e2, e3];
        Assert.Equal(expected, seen);
    }

    [Fact]
    public void StaticEntities_EnumeratesAllRegisteredEntities()
    {
        var lookup = new BodyLookup();
        var e1 = new Entity(10, 0);
        var e2 = new Entity(11, 0);
        lookup.RegisterStatic(e1, new StaticHandle(1));
        lookup.RegisterStatic(e2, new StaticHandle(2));

        var seen = new HashSet<Entity>();
        foreach (var entity in lookup.StaticEntities)
        {
            seen.Add(entity);
        }

        HashSet<Entity> expected = [e1, e2];
        Assert.Equal(expected, seen);
    }

    [Fact]
    public void DynamicEntities_ReflectsRegisterAndUnregister()
    {
        var lookup = new BodyLookup();
        var entity = new Entity(5, 0);
        lookup.RegisterBody(entity, new BodyHandle(99));

        Assert.Single(lookup.DynamicEntities);

        lookup.Unregister(entity);

        Assert.Empty(lookup.DynamicEntities);
    }

    [Fact]
    public void DynamicAndStaticEntities_AreDisjoint()
    {
        var lookup = new BodyLookup();
        var dynamicEntity = new Entity(1, 0);
        var staticEntity = new Entity(2, 0);
        lookup.RegisterBody(dynamicEntity, new BodyHandle(1));
        lookup.RegisterStatic(staticEntity, new StaticHandle(1));

        Assert.Contains(dynamicEntity, lookup.DynamicEntities);
        Assert.DoesNotContain(staticEntity, lookup.DynamicEntities);
        Assert.Contains(staticEntity, lookup.StaticEntities);
        Assert.DoesNotContain(dynamicEntity, lookup.StaticEntities);
    }
}
