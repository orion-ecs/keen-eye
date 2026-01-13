namespace KeenEyes.Tests;

/// <summary>
/// Disposable extension for testing disposal behavior.
/// </summary>
public sealed class DisposableTestExtension : IDisposable
{
    public bool IsDisposed { get; private set; }
    public int DisposeCount { get; private set; }

    public void Dispose()
    {
        DisposeCount++;
        IsDisposed = true;
    }
}

/// <summary>
/// Another disposable extension for testing multiple disposals.
/// </summary>
public sealed class DisposableTestExtension2 : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

/// <summary>
/// Tests for World extension API.
/// </summary>
public class WorldExtensionTests
{
    [Fact]
    public void SetExtension_StoresValue()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld());

        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void SetExtension_ReplacesExisting()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world.SetExtension(new TestPhysicsWorld { Gravity = -10.0f });

        var physics = world.GetExtension<TestPhysicsWorld>();
        Assert.Equal(-10.0f, physics.Gravity);
    }

    [Fact]
    public void SetExtension_Null_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.SetExtension<TestPhysicsWorld>(null!));
    }

    [Fact]
    public void GetExtension_ReturnsStoredValue()
    {
        using var world = new World();
        var original = new TestPhysicsWorld { Gravity = -15.0f };

        world.SetExtension(original);
        var retrieved = world.GetExtension<TestPhysicsWorld>();

        Assert.Same(original, retrieved);
        Assert.Equal(-15.0f, retrieved.Gravity);
    }

    [Fact]
    public void GetExtension_ThrowsInvalidOperationException_WhenNotSet()
    {
        using var world = new World();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetExtension<TestPhysicsWorld>());

        Assert.Contains("TestPhysicsWorld", exception.Message);
    }

    [Fact]
    public void TryGetExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });

        var result = world.TryGetExtension<TestPhysicsWorld>(out var physics);

        Assert.True(result);
        Assert.NotNull(physics);
        Assert.Equal(-9.81f, physics.Gravity);
    }

    [Fact]
    public void TryGetExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        var result = world.TryGetExtension<TestPhysicsWorld>(out var physics);

        Assert.False(result);
        Assert.Null(physics);
    }

    [Fact]
    public void HasExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void HasExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void RemoveExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        var result = world.RemoveExtension<TestPhysicsWorld>();

        Assert.True(result);
        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void RemoveExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        var result = world.RemoveExtension<TestPhysicsWorld>();

        Assert.False(result);
    }

    [Fact]
    public void Extensions_MultipleTypes_Independent()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world.SetExtension(new TestRenderer { DrawCalls = 100 });

        var physics = world.GetExtension<TestPhysicsWorld>();
        var renderer = world.GetExtension<TestRenderer>();

        Assert.Equal(-9.81f, physics.Gravity);
        Assert.Equal(100, renderer.DrawCalls);
    }

    #region Disposal Tests

    [Fact]
    public void SetExtension_WhenReplacing_DisposesOldExtension()
    {
        using var world = new World();
        var original = new DisposableTestExtension();

        world.SetExtension(original);
        world.SetExtension(new DisposableTestExtension());

        Assert.True(original.IsDisposed);
        Assert.Equal(1, original.DisposeCount);
    }

    [Fact]
    public void SetExtension_WhenReplacingNonDisposable_DoesNotThrow()
    {
        using var world = new World();
        var original = new TestPhysicsWorld { Gravity = -9.81f };

        world.SetExtension(original);

        // Replacing a non-disposable extension should not throw
        var exception = Record.Exception(() =>
            world.SetExtension(new TestPhysicsWorld { Gravity = -10.0f }));

        Assert.Null(exception);
    }

    [Fact]
    public void RemoveExtension_WithDisposable_DisposesExtension()
    {
        using var world = new World();
        var extension = new DisposableTestExtension();

        world.SetExtension(extension);
        world.RemoveExtension<DisposableTestExtension>();

        Assert.True(extension.IsDisposed);
        Assert.Equal(1, extension.DisposeCount);
    }

    [Fact]
    public void RemoveExtension_WithNonDisposable_DoesNotThrow()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        var exception = Record.Exception(() =>
            world.RemoveExtension<TestPhysicsWorld>());

        Assert.Null(exception);
    }

    [Fact]
    public void WorldDispose_WithDisposableExtensions_DisposesAll()
    {
        var extension1 = new DisposableTestExtension();
        var extension2 = new DisposableTestExtension2();

        var world = new World();
        world.SetExtension(extension1);
        world.SetExtension(extension2);

        world.Dispose();

        Assert.True(extension1.IsDisposed);
        Assert.True(extension2.IsDisposed);
    }

    [Fact]
    public void WorldDispose_WithMixedExtensions_DisposesOnlyDisposable()
    {
        var disposable = new DisposableTestExtension();
        var nonDisposable = new TestPhysicsWorld();

        var world = new World();
        world.SetExtension(disposable);
        world.SetExtension(nonDisposable);

        // Should not throw and should dispose the disposable extension
        var exception = Record.Exception(() => world.Dispose());

        Assert.Null(exception);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void SetExtension_ReplacingMultipleTimes_DisposesEachPrevious()
    {
        using var world = new World();
        var first = new DisposableTestExtension();
        var second = new DisposableTestExtension();
        var third = new DisposableTestExtension();

        world.SetExtension(first);
        world.SetExtension(second);
        world.SetExtension(third);

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.False(third.IsDisposed);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public void SetExtension_ConcurrentAccess_ThreadSafe()
    {
        using var world = new World();
        const int iterations = 1000;
        var exceptions = new List<Exception>();

        Parallel.For(0, iterations, i =>
        {
            try
            {
                world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f * i });
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Assert.Empty(exceptions);
        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void GetExtension_ConcurrentAccess_ThreadSafe()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        const int iterations = 1000;
        var exceptions = new List<Exception>();

        Parallel.For(0, iterations, _ =>
        {
            try
            {
                var extension = world.GetExtension<TestPhysicsWorld>();
                Assert.NotNull(extension);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void MixedExtensionOperations_ConcurrentAccess_ThreadSafe()
    {
        using var world = new World();
        const int iterations = 100;
        var exceptions = new List<Exception>();

        // Start with an extension
        world.SetExtension(new TestPhysicsWorld());

        Parallel.For(0, iterations, i =>
        {
            try
            {
                switch (i % 4)
                {
                    case 0:
                        world.SetExtension(new TestPhysicsWorld { Gravity = i });
                        break;
                    case 1:
                        world.TryGetExtension<TestPhysicsWorld>(out _);
                        break;
                    case 2:
                        world.HasExtension<TestPhysicsWorld>();
                        break;
                    case 3:
                        // Set then remove
                        world.SetExtension(new TestRenderer { DrawCalls = i });
                        world.RemoveExtension<TestRenderer>();
                        break;
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Assert.Empty(exceptions);
    }

    #endregion
}

/// <summary>
/// Tests for plugin extensions via PluginContext.
/// </summary>
public class PluginExtensionTests
{
    [Fact]
    public void Plugin_SetExtension_AvailableOnWorld()
    {
        using var world = new World();
        world.InstallPlugin<TestExtensionPlugin>();

        var physics = world.GetExtension<TestPhysicsWorld>();

        Assert.NotNull(physics);
        Assert.Equal(-10.0f, physics.Gravity);
    }

    [Fact]
    public void Plugin_Uninstall_RemovesExtension()
    {
        using var world = new World();
        world.InstallPlugin<TestExtensionPlugin>();

        world.UninstallPlugin<TestExtensionPlugin>();

        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }
}
