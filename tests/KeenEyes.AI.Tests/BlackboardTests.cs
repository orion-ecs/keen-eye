using System.Numerics;
using KeenEyes.AI;

namespace KeenEyes.AI.Tests;

/// <summary>
/// Tests for the Blackboard class.
/// </summary>
public class BlackboardTests
{
    #region Set and Get Tests

    [Fact]
    public void Set_ThenGet_ReturnsValue()
    {
        var blackboard = new Blackboard();

        blackboard.Set("key", 42);

        blackboard.Get<int>("key").ShouldBe(42);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var blackboard = new Blackboard();

        blackboard.Set("key", 42);
        blackboard.Set("key", 100);

        blackboard.Get<int>("key").ShouldBe(100);
    }

    [Fact]
    public void Get_WithMissingKey_ReturnsDefault()
    {
        var blackboard = new Blackboard();

        blackboard.Get<int>("missing").ShouldBe(0);
    }

    [Fact]
    public void Get_WithDefaultValue_ReturnsDefaultWhenMissing()
    {
        var blackboard = new Blackboard();

        blackboard.Get("missing", 42).ShouldBe(42);
    }

    [Fact]
    public void Get_WithWrongType_ReturnsDefault()
    {
        var blackboard = new Blackboard();

        blackboard.Set("key", "string value");

        blackboard.Get<int>("key").ShouldBe(0);
    }

    [Fact]
    public void Get_WithVector3_WorksCorrectly()
    {
        var blackboard = new Blackboard();
        var position = new Vector3(1, 2, 3);

        blackboard.Set(BBKeys.Destination, position);

        blackboard.Get<Vector3>(BBKeys.Destination).ShouldBe(position);
    }

    #endregion

    #region TryGet Tests

    [Fact]
    public void TryGet_WithExistingKey_ReturnsTrue()
    {
        var blackboard = new Blackboard();
        blackboard.Set("key", 42);

        var result = blackboard.TryGet<int>("key", out var value);

        result.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void TryGet_WithMissingKey_ReturnsFalse()
    {
        var blackboard = new Blackboard();

        var result = blackboard.TryGet<int>("missing", out var value);

        result.ShouldBeFalse();
        value.ShouldBe(0);
    }

    [Fact]
    public void TryGet_WithWrongType_ReturnsFalse()
    {
        var blackboard = new Blackboard();
        blackboard.Set("key", "string value");

        var result = blackboard.TryGet<int>("key", out var value);

        result.ShouldBeFalse();
        value.ShouldBe(0);
    }

    #endregion

    #region Has Tests

    [Fact]
    public void Has_WithExistingKey_ReturnsTrue()
    {
        var blackboard = new Blackboard();
        blackboard.Set("key", 42);

        blackboard.Has("key").ShouldBeTrue();
    }

    [Fact]
    public void Has_WithMissingKey_ReturnsFalse()
    {
        var blackboard = new Blackboard();

        blackboard.Has("missing").ShouldBeFalse();
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_WithExistingKey_RemovesValue()
    {
        var blackboard = new Blackboard();
        blackboard.Set("key", 42);

        var removed = blackboard.Remove("key");

        removed.ShouldBeTrue();
        blackboard.Has("key").ShouldBeFalse();
    }

    [Fact]
    public void Remove_WithMissingKey_ReturnsFalse()
    {
        var blackboard = new Blackboard();

        blackboard.Remove("missing").ShouldBeFalse();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllValues()
    {
        var blackboard = new Blackboard();
        blackboard.Set("key1", 1);
        blackboard.Set("key2", 2);
        blackboard.Set("key3", 3);

        blackboard.Clear();

        blackboard.Count.ShouldBe(0);
        blackboard.Has("key1").ShouldBeFalse();
        blackboard.Has("key2").ShouldBeFalse();
        blackboard.Has("key3").ShouldBeFalse();
    }

    #endregion

    #region Count Tests

    [Fact]
    public void Count_ReturnsNumberOfEntries()
    {
        var blackboard = new Blackboard();

        blackboard.Count.ShouldBe(0);

        blackboard.Set("key1", 1);
        blackboard.Count.ShouldBe(1);

        blackboard.Set("key2", 2);
        blackboard.Count.ShouldBe(2);
    }

    #endregion

    #region Entries Tests

    [Fact]
    public void Entries_WithEmptyBlackboard_YieldsNothing()
    {
        var blackboard = new Blackboard();

        blackboard.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void Entries_WithValueTypeEntry_YieldsBoxedValueWithRealRuntimeType()
    {
        var blackboard = new Blackboard();
        blackboard.Set("speed", 1.5f);

        var entry = blackboard.Entries.ShouldHaveSingleItem();

        entry.Key.ShouldBe("speed");
        entry.Value.GetType().ShouldBe(typeof(float));
        entry.Value.ShouldBe(1.5f);
    }

    [Fact]
    public void Entries_WithStructEntry_YieldsBoxedStructNotStorageWrapper()
    {
        var blackboard = new Blackboard();
        blackboard.Set("target", new Vector3(1, 2, 3));

        var entry = blackboard.Entries.ShouldHaveSingleItem();

        entry.Value.GetType().ShouldBe(typeof(Vector3));
        entry.Value.ShouldBe(new Vector3(1, 2, 3));
    }

    [Fact]
    public void Entries_WithReferenceTypeEntry_YieldsSameInstance()
    {
        var blackboard = new Blackboard();
        var waypoints = new List<Vector3> { new(1, 0, 0) };
        blackboard.Set("waypoints", waypoints);

        var entry = blackboard.Entries.ShouldHaveSingleItem();

        entry.Value.ShouldBeSameAs(waypoints);
    }

    [Fact]
    public void Entries_AfterRepeatedSetOfSameKeyAndType_YieldsLatestValueOnce()
    {
        var blackboard = new Blackboard();

        // The same-type write path mutates the existing cell in place rather than replacing it.
        blackboard.Set("speed", 1.5f);
        blackboard.Set("speed", 9.25f);
        blackboard.Set("speed", 4.5f);

        var entry = blackboard.Entries.ShouldHaveSingleItem();

        entry.Value.GetType().ShouldBe(typeof(float));
        entry.Value.ShouldBe(4.5f);
    }

    [Fact]
    public void Entries_WithMixedEntries_YieldsEveryKeyWithItsRealType()
    {
        var blackboard = new Blackboard();
        blackboard.Set("speed", 1.5f);
        blackboard.Set("ticks", 7);
        blackboard.Set("name", "goblin");

        var entries = blackboard.Entries.ToDictionary(e => e.Key, e => e.Value);

        entries.Count.ShouldBe(3);
        entries["speed"].ShouldBe(1.5f);
        entries["ticks"].ShouldBe(7);
        entries["name"].ShouldBe("goblin");
    }

    [Fact]
    public void Entries_AfterRemove_DoesNotYieldRemovedKey()
    {
        var blackboard = new Blackboard();
        blackboard.Set("speed", 1.5f);
        blackboard.Set("ticks", 7);

        blackboard.Remove("speed");

        var entry = blackboard.Entries.ShouldHaveSingleItem();
        entry.Key.ShouldBe("ticks");
    }

    [Fact]
    public void Entries_AfterOverwritingValueTypeWithReferenceType_YieldsReferenceValue()
    {
        var blackboard = new Blackboard();
        blackboard.Set("k", 7);
        blackboard.Set("k", "hello");

        var entry = blackboard.Entries.ShouldHaveSingleItem();

        entry.Value.ShouldBe("hello");
    }

    #endregion

    #region BBKeys Tests

    [Fact]
    public void BBKeys_HasExpectedNavigationKeys()
    {
        BBKeys.Destination.ShouldBe("Destination");
        BBKeys.CurrentPath.ShouldBe("CurrentPath");
        BBKeys.PatrolIndex.ShouldBe("PatrolIndex");
        BBKeys.PatrolWaypoints.ShouldBe("PatrolWaypoints");
    }

    [Fact]
    public void BBKeys_HasExpectedTargetKeys()
    {
        BBKeys.Target.ShouldBe("Target");
        BBKeys.TargetPosition.ShouldBe("TargetPosition");
        BBKeys.TargetLastSeen.ShouldBe("TargetLastSeen");
    }

    [Fact]
    public void BBKeys_HasExpectedTimeKeys()
    {
        BBKeys.Time.ShouldBe("Time");
        BBKeys.DeltaTime.ShouldBe("DeltaTime");
    }

    #endregion
}
