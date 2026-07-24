using System.Numerics;
using KeenEyes.AI;
using Shouldly;

namespace KeenEyes.AI.Tests;

/// <summary>
/// Behavior-preservation tests for the Blackboard value-type fast path (issue #1238).
/// The typed cell that avoids boxing on value-type writes must not change any observable
/// semantics of the string-keyed get/set API, including cross-type overwrites, boxed/object
/// lookups, and nullable-value-type handling.
/// </summary>
public class BlackboardHotPathTests
{
    #region Value-type round trips

    [Fact]
    public void Set_Vector3_ThenGet_ReturnsSameValue()
    {
        var blackboard = new Blackboard();
        var value = new Vector3(1, 2, 3);

        blackboard.Set("target", value);

        blackboard.Get<Vector3>("target").ShouldBe(value);
    }

    [Fact]
    public void Set_SameKeySameType_OverwritesValue()
    {
        var blackboard = new Blackboard();

        blackboard.Set("pos", new Vector3(1, 1, 1));
        blackboard.Set("pos", new Vector3(9, 9, 9));

        blackboard.Get<Vector3>("pos").ShouldBe(new Vector3(9, 9, 9));
        blackboard.Count.ShouldBe(1);
    }

    #endregion

    #region Type-mismatch semantics preserved

    [Fact]
    public void Get_WithWrongValueType_ReturnsDefault()
    {
        var blackboard = new Blackboard();
        blackboard.Set("x", 5);

        // A stored int must not satisfy a Get<long>; matches the original `is T` semantics.
        blackboard.Get<long>("x").ShouldBe(0L);
    }

    [Fact]
    public void Get_AsObject_AfterValueTypeSet_ReturnsBoxedValue()
    {
        var blackboard = new Blackboard();
        blackboard.Set("x", 42);

        // Get<object> must recover the boxed value, exactly as a Dictionary<string, object>
        // store would, even though the value lives in a typed cell.
        var boxed = blackboard.Get<object>("x");

        boxed.ShouldBe(42);
    }

    [Fact]
    public void Set_OverwriteValueTypeWithReferenceType_SwitchesStorage()
    {
        var blackboard = new Blackboard();

        blackboard.Set("k", 7);
        blackboard.Set("k", "hello");

        blackboard.Get<string>("k").ShouldBe("hello");
        blackboard.Get<int>("k").ShouldBe(0);
        blackboard.Count.ShouldBe(1);
    }

    [Fact]
    public void Set_OverwriteReferenceTypeWithValueType_SwitchesStorage()
    {
        var blackboard = new Blackboard();

        blackboard.Set("k", "hello");
        blackboard.Set("k", 7);

        blackboard.Get<int>("k").ShouldBe(7);
        blackboard.Get<string>("k").ShouldBeNull();
    }

    [Fact]
    public void Set_OverwriteValueTypeWithDifferentValueType_SwitchesStorage()
    {
        var blackboard = new Blackboard();

        blackboard.Set("k", 7);
        blackboard.Set("k", 1.5f);

        blackboard.Get<float>("k").ShouldBe(1.5f);
        blackboard.Get<int>("k").ShouldBe(0);
    }

    #endregion

    #region Nullable, Has, Remove, TryGet

    [Fact]
    public void Get_NullableIntType_MatchesUnderlyingBoxedInt()
    {
        var blackboard = new Blackboard();
        blackboard.Set("x", 5);

        // A boxed int satisfies both int and int? checks (Nullable boxing rules), preserved
        // by the boxed-value fallback path.
        blackboard.Get<int?>("x").ShouldBe(5);
    }

    [Fact]
    public void TryGet_ValueType_ReturnsTrueAndValue()
    {
        var blackboard = new Blackboard();
        blackboard.Set("x", new Vector3(4, 5, 6));

        blackboard.TryGet<Vector3>("x", out var value).ShouldBeTrue();
        value.ShouldBe(new Vector3(4, 5, 6));
    }

    [Fact]
    public void TryGet_WrongType_ReturnsFalse()
    {
        var blackboard = new Blackboard();
        blackboard.Set("x", 5);

        blackboard.TryGet<string>("x", out var value).ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public void HasAndRemove_WorkForValueTypeCells()
    {
        var blackboard = new Blackboard();
        blackboard.Set("x", new Vector3(1, 2, 3));

        blackboard.Has("x").ShouldBeTrue();
        blackboard.Remove("x").ShouldBeTrue();
        blackboard.Has("x").ShouldBeFalse();
        blackboard.Count.ShouldBe(0);
    }

    #endregion

    #region Allocation regression guard

    [Fact]
    public void Set_RepeatedSameKeyValueType_DoesNotAllocateAfterFirstWrite()
    {
        // The old Dictionary<string, object> store boxed the Vector3 on every Set. The typed
        // cell is allocated once and mutated in place, so a steady-state write loop allocates
        // nothing.
        var blackboard = new Blackboard();
        blackboard.Set("pos", Vector3.Zero);

        // Warm up JIT/tiering and the initial cell allocation.
        for (int i = 0; i < 512; i++)
        {
            blackboard.Set("pos", new Vector3(i, i, i));
        }

        const int iterations = 4096;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            blackboard.Set("pos", new Vector3(i, i, i));
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.ShouldBeLessThan(1024, $"Expected ~0 steady-state allocation, saw {allocated} bytes.");
        blackboard.Get<Vector3>("pos").ShouldBe(new Vector3(iterations - 1, iterations - 1, iterations - 1));
    }

    #endregion
}
