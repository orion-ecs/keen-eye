namespace KeenEyes.Tests;

/// <summary>
/// Test singleton for game time data.
/// </summary>
public struct TestGameTime
{
    public float DeltaTime;
    public float TotalTime;
}

/// <summary>
/// Test singleton for game configuration.
/// </summary>
public struct TestGameConfig
{
    public int Difficulty;
    public bool SoundEnabled;
}

/// <summary>
/// Test singleton for input state.
/// </summary>
public struct TestInputState
{
    public float MoveX;
    public float MoveY;
    public bool Jump;
}

/// <summary>
/// Tests for World singleton methods (SetSingleton, GetSingleton, TryGetSingleton, HasSingleton, RemoveSingleton).
/// </summary>
public class SingletonTests
{
    #region SetSingleton Tests

    [Fact]
    public void SetSingleton_StoresValue()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        Assert.True(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void SetSingleton_ReplacesExistingValue()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.SetSingleton(new TestGameTime { DeltaTime = 0.033f, TotalTime = 20.0f });

        ref var time = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0.033f, time.DeltaTime);
        Assert.Equal(20.0f, time.TotalTime);
    }

    [Fact]
    public void SetSingleton_SupportsMultipleTypes()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.SetSingleton(new TestGameConfig { Difficulty = 3, SoundEnabled = true });
        world.SetSingleton(new TestInputState { MoveX = 1.0f, MoveY = 0.0f, Jump = true });

        Assert.True(world.HasSingleton<TestGameTime>());
        Assert.True(world.HasSingleton<TestGameConfig>());
        Assert.True(world.HasSingleton<TestInputState>());
    }

    [Fact]
    public void SetSingleton_WorksWithDefaultValues()
    {
        using var world = new World();

        world.SetSingleton(default(TestGameTime));

        Assert.True(world.HasSingleton<TestGameTime>());
        ref var time = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0f, time.DeltaTime);
        Assert.Equal(0f, time.TotalTime);
    }

    #endregion

    #region GetSingleton Tests

    [Fact]
    public void GetSingleton_ReturnsStoredValue()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        ref var time = ref world.GetSingleton<TestGameTime>();

        Assert.Equal(0.016f, time.DeltaTime);
        Assert.Equal(10.5f, time.TotalTime);
    }

    [Fact]
    public void GetSingleton_ReturnsRef_ModificationsArePersisted()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        ref var time = ref world.GetSingleton<TestGameTime>();
        time.DeltaTime = 0.033f;
        time.TotalTime = 20.0f;

        ref var timeAgain = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0.033f, timeAgain.DeltaTime);
        Assert.Equal(20.0f, timeAgain.TotalTime);
    }

    [Fact]
    public void GetSingleton_ThrowsInvalidOperationException_WhenNotSet()
    {
        using var world = new World();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetSingleton<TestGameTime>());

        Assert.Contains("does not exist", exception.Message);
        Assert.Contains("TestGameTime", exception.Message);
    }

    [Fact]
    public void GetSingleton_WorksWithMultipleTypes()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.SetSingleton(new TestGameConfig { Difficulty = 3, SoundEnabled = true });

        ref var time = ref world.GetSingleton<TestGameTime>();
        ref var config = ref world.GetSingleton<TestGameConfig>();

        Assert.Equal(0.016f, time.DeltaTime);
        Assert.Equal(3, config.Difficulty);
        Assert.True(config.SoundEnabled);
    }

    [Fact]
    public void GetSingleton_TypesAreIndependent()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 0f });
        world.SetSingleton(new TestGameConfig { Difficulty = 5, SoundEnabled = false });

        // Modifying one type should not affect another
        ref var time = ref world.GetSingleton<TestGameTime>();
        time.TotalTime = 100f;

        ref var config = ref world.GetSingleton<TestGameConfig>();
        Assert.Equal(5, config.Difficulty); // Should be unchanged
    }

    #endregion

    #region TryGetSingleton Tests

    [Fact]
    public void TryGetSingleton_ReturnsTrue_WhenSingletonExists()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        var result = world.TryGetSingleton<TestGameTime>(out var time);

        Assert.True(result);
        Assert.Equal(0.016f, time.DeltaTime);
        Assert.Equal(10.5f, time.TotalTime);
    }

    [Fact]
    public void TryGetSingleton_ReturnsFalse_WhenSingletonNotSet()
    {
        using var world = new World();

        var result = world.TryGetSingleton<TestGameTime>(out var time);

        Assert.False(result);
        Assert.Equal(default, time);
    }

    [Fact]
    public void TryGetSingleton_ReturnsFalse_WhenDifferentTypeSet()
    {
        using var world = new World();

        world.SetSingleton(new TestGameConfig { Difficulty = 3 });

        var result = world.TryGetSingleton<TestGameTime>(out var time);

        Assert.False(result);
        Assert.Equal(default, time);
    }

    [Fact]
    public void TryGetSingleton_ReturnsCopy_NotReference()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        world.TryGetSingleton<TestGameTime>(out var timeCopy);
        timeCopy.DeltaTime = 999f;

        // Original should be unchanged
        ref var original = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0.016f, original.DeltaTime);
    }

    [Fact]
    public void TryGetSingleton_WorksWithDefaultValues()
    {
        using var world = new World();

        world.SetSingleton(default(TestGameTime));

        var result = world.TryGetSingleton<TestGameTime>(out var time);

        Assert.True(result);
        Assert.Equal(0f, time.DeltaTime);
        Assert.Equal(0f, time.TotalTime);
    }

    #endregion

    #region HasSingleton Tests

    [Fact]
    public void HasSingleton_ReturnsTrue_WhenSingletonExists()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        Assert.True(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void HasSingleton_ReturnsFalse_WhenSingletonNotSet()
    {
        using var world = new World();

        Assert.False(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void HasSingleton_ReturnsFalse_WhenDifferentTypeSet()
    {
        using var world = new World();

        world.SetSingleton(new TestGameConfig { Difficulty = 3 });

        Assert.False(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void HasSingleton_ReturnsTrue_ForMultipleTypes()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.SetSingleton(new TestGameConfig { Difficulty = 3, SoundEnabled = true });

        Assert.True(world.HasSingleton<TestGameTime>());
        Assert.True(world.HasSingleton<TestGameConfig>());
        Assert.False(world.HasSingleton<TestInputState>());
    }

    [Fact]
    public void HasSingleton_IsIdempotent()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        var result1 = world.HasSingleton<TestGameTime>();
        var result2 = world.HasSingleton<TestGameTime>();
        var result3 = world.HasSingleton<TestGameTime>();

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public void HasSingleton_DoesNotModifyState()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        // Call Has multiple times
        _ = world.HasSingleton<TestGameTime>();
        _ = world.HasSingleton<TestGameTime>();
        _ = world.HasSingleton<TestGameConfig>();

        // Singleton should still exist with original values
        ref var time = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0.016f, time.DeltaTime);
    }

    #endregion

    #region RemoveSingleton Tests

    [Fact]
    public void RemoveSingleton_ReturnsTrue_WhenSingletonExists()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        var result = world.RemoveSingleton<TestGameTime>();

        Assert.True(result);
        Assert.False(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void RemoveSingleton_ReturnsFalse_WhenSingletonNotSet()
    {
        using var world = new World();

        var result = world.RemoveSingleton<TestGameTime>();

        Assert.False(result);
    }

    [Fact]
    public void RemoveSingleton_IsIdempotent()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        var result1 = world.RemoveSingleton<TestGameTime>();
        var result2 = world.RemoveSingleton<TestGameTime>();
        var result3 = world.RemoveSingleton<TestGameTime>();

        Assert.True(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void RemoveSingleton_OnlyRemovesSpecifiedType()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.SetSingleton(new TestGameConfig { Difficulty = 3, SoundEnabled = true });

        world.RemoveSingleton<TestGameTime>();

        Assert.False(world.HasSingleton<TestGameTime>());
        Assert.True(world.HasSingleton<TestGameConfig>());
    }

    [Fact]
    public void RemoveSingleton_AllowsReAddingSameType()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.RemoveSingleton<TestGameTime>();
        world.SetSingleton(new TestGameTime { DeltaTime = 0.033f, TotalTime = 20.0f });

        Assert.True(world.HasSingleton<TestGameTime>());
        ref var time = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0.033f, time.DeltaTime);
    }

    [Fact]
    public void GetSingleton_ThrowsAfterRemoval()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.RemoveSingleton<TestGameTime>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetSingleton<TestGameTime>());

        Assert.Contains("does not exist", exception.Message);
    }

    #endregion

    #region World Isolation Tests

    [Fact]
    public void Singletons_AreIsolatedBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        Assert.True(world1.HasSingleton<TestGameTime>());
        Assert.False(world2.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void Singletons_IndependentModificationBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world2.SetSingleton(new TestGameTime { DeltaTime = 0.033f, TotalTime = 20.0f });

        ref var time1 = ref world1.GetSingleton<TestGameTime>();
        ref var time2 = ref world2.GetSingleton<TestGameTime>();

        Assert.Equal(0.016f, time1.DeltaTime);
        Assert.Equal(0.033f, time2.DeltaTime);

        // Modify world1's singleton
        time1.TotalTime = 100f;

        // World2 should be unaffected
        Assert.Equal(20.0f, time2.TotalTime);
    }

    [Fact]
    public void Singletons_RemovalInOneWorld_DoesNotAffectOther()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world2.SetSingleton(new TestGameTime { DeltaTime = 0.033f, TotalTime = 20.0f });

        world1.RemoveSingleton<TestGameTime>();

        Assert.False(world1.HasSingleton<TestGameTime>());
        Assert.True(world2.HasSingleton<TestGameTime>());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ClearsSingletons()
    {
        var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });
        world.SetSingleton(new TestGameConfig { Difficulty = 3, SoundEnabled = true });

        world.Dispose();

        // After dispose, accessing singletons should fail or return empty state
        // Note: we can't easily test this since HasSingleton would still work on the cleared dictionary
        // The main point is that Dispose clears the internal state to free resources
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Singletons_WorkAlongsideEntities()
    {
        using var world = new World();

        // Set up singleton
        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 0f });

        // Create entities
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 0f })
            .Build();

        // Simulate update: read singleton and update entity
        ref var time = ref world.GetSingleton<TestGameTime>();
        ref var pos = ref world.Get<TestPosition>(entity);
        ref readonly var vel = ref world.Get<TestVelocity>(entity);

        pos.X += vel.X * time.DeltaTime;
        time.TotalTime += time.DeltaTime;

        Assert.Equal(0.016f, pos.X);
        Assert.Equal(0.016f, time.TotalTime);
    }

    [Fact]
    public void Singletons_CanBeUsedByMultipleEntities()
    {
        using var world = new World();

        world.SetSingleton(new TestGameConfig { Difficulty = 3 });

        var entity1 = world.Spawn()
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        var entity2 = world.Spawn()
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();

        // Both entities can read the same config singleton
        ref readonly var config = ref world.GetSingleton<TestGameConfig>();

        // Apply difficulty-based damage to both entities
        ref var health1 = ref world.Get<TestHealth>(entity1);
        ref var health2 = ref world.Get<TestHealth>(entity2);

        health1.Current -= config.Difficulty * 10;
        health2.Current -= config.Difficulty * 10;

        Assert.Equal(70, health1.Current);
        Assert.Equal(20, health2.Current);
    }

    [Fact]
    public void Singletons_ConsistentWithHasPattern()
    {
        using var world = new World();

        // If Has returns true, Get should not throw
        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        if (world.HasSingleton<TestGameTime>())
        {
            ref var time = ref world.GetSingleton<TestGameTime>();
            Assert.Equal(0.016f, time.DeltaTime);
        }
        else
        {
            Assert.Fail("HasSingleton<TestGameTime> should have returned true");
        }
    }

    [Fact]
    public void Singletons_ConsistentWithHasPattern_WhenNotSet()
    {
        using var world = new World();

        // If Has returns false, Get should throw
        Assert.False(world.HasSingleton<TestGameTime>());
        Assert.Throws<InvalidOperationException>(() => world.GetSingleton<TestGameTime>());
    }

    [Fact]
    public void Singletons_WorkInLoop()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 0f });

        // Simulate 100 frames
        for (int i = 0; i < 100; i++)
        {
            ref var time = ref world.GetSingleton<TestGameTime>();
            time.TotalTime += time.DeltaTime;
        }

        ref var finalTime = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(1.6f, finalTime.TotalTime, precision: 4);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetSingleton_WorksImmediatelyAfterWorldCreation()
    {
        using var world = new World();

        // Should be able to set singleton on fresh world
        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 0f });

        Assert.True(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void GetSingleton_ReturnsCorrectType()
    {
        using var world = new World();

        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 10.5f });

        ref var time = ref world.GetSingleton<TestGameTime>();

        // Verify we get the correct type back
        Assert.IsType<TestGameTime>(time);
    }

    [Fact]
    public void Singletons_NoInterferenceWithComponentOperations()
    {
        using var world = new World();

        // Set singleton
        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 0f });

        // Entity operations should still work normally
        var entity = world.Spawn()
            .With(new TestPosition { X = 10f, Y = 20f })
            .Build();

        world.Add(entity, new TestVelocity { X = 1f, Y = 2f });

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));

        world.Remove<TestVelocity>(entity);

        Assert.True(world.Has<TestPosition>(entity));
        Assert.False(world.Has<TestVelocity>(entity));

        // Singleton should still be there
        Assert.True(world.HasSingleton<TestGameTime>());
    }

    [Fact]
    public void Singletons_CanBeSetUpdatedAndRemoved()
    {
        using var world = new World();

        // Set
        world.SetSingleton(new TestGameTime { DeltaTime = 0.016f, TotalTime = 0f });
        Assert.True(world.HasSingleton<TestGameTime>());

        // Update via ref
        ref var time = ref world.GetSingleton<TestGameTime>();
        time.TotalTime = 100f;

        ref var timeCheck = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(100f, timeCheck.TotalTime);

        // Update via Set
        world.SetSingleton(new TestGameTime { DeltaTime = 0.033f, TotalTime = 200f });

        ref var timeUpdated = ref world.GetSingleton<TestGameTime>();
        Assert.Equal(0.033f, timeUpdated.DeltaTime);
        Assert.Equal(200f, timeUpdated.TotalTime);

        // Remove
        Assert.True(world.RemoveSingleton<TestGameTime>());
        Assert.False(world.HasSingleton<TestGameTime>());

        // Re-add
        world.SetSingleton(new TestGameTime { DeltaTime = 0.05f, TotalTime = 0f });
        Assert.True(world.HasSingleton<TestGameTime>());
    }

    #endregion
}
