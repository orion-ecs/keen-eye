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
