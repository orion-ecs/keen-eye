namespace KeenEyes.Tests;

/// <summary>
/// Tests for extension ownership semantics (<c>owned</c> flag on <see cref="World.SetExtension{T}"/>).
/// </summary>
/// <remarks>
/// Extensions default to manager-owned (disposed on replace/remove/world teardown).
/// Registering with <c>owned: false</c> keeps the caller responsible for disposal,
/// which is how shared or externally-supplied instances survive plugin uninstall
/// (see issue #1171).
/// </remarks>
public class ExtensionOwnershipTests
{
    [Fact]
    public void RemoveExtension_WhenCallerOwned_DoesNotDisposeExtension()
    {
        using var world = new World();
        var extension = new DisposableTestExtension();
        world.SetExtension(extension, owned: false);

        var removed = world.RemoveExtension<DisposableTestExtension>();

        Assert.True(removed);
        Assert.False(extension.IsDisposed);
        Assert.Equal(0, extension.DisposeCount);
        Assert.False(world.HasExtension<DisposableTestExtension>());
    }

    [Fact]
    public void SetExtension_ReplacingCallerOwnedWithDifferentInstance_DoesNotDisposeOld()
    {
        using var world = new World();
        var original = new DisposableTestExtension();
        world.SetExtension(original, owned: false);

        world.SetExtension(new DisposableTestExtension());

        Assert.False(original.IsDisposed);
        Assert.Equal(0, original.DisposeCount);
    }

    [Fact]
    public void WorldDispose_WithCallerOwnedExtension_DoesNotDisposeIt()
    {
        var extension = new DisposableTestExtension();
        var world = new World();
        world.SetExtension(extension, owned: false);

        world.Dispose();

        Assert.False(extension.IsDisposed);
    }

    [Fact]
    public void SetExtension_ReRegisteringCallerOwnedAsOwned_ThenRemoving_DisposesIt()
    {
        // Ownership follows the most recent registration: re-registering a caller-owned
        // instance as owned makes the manager responsible for disposing it on removal.
        using var world = new World();
        var extension = new DisposableTestExtension();

        world.SetExtension(extension, owned: false);
        world.SetExtension(extension, owned: true); // Same instance, now owned.

        // Re-setting the same instance never disposes it, regardless of ownership change.
        Assert.False(extension.IsDisposed);

        world.RemoveExtension<DisposableTestExtension>();

        Assert.True(extension.IsDisposed);
        Assert.Equal(1, extension.DisposeCount);
    }

    #region Aliased Registration Tests

    [Fact]
    public void SetExtension_ReplacingAliasOfMultiRegisteredInstance_DoesNotDisposeIt()
    {
        // Plugins routinely register one object under both its interface and its concrete
        // type (SilkInputPlugin does exactly this). Replacing the interface alias must not
        // dispose the instance while the concrete registration still resolves to it —
        // doing so left NOVAFALL with a disposed input context that never initialized.
        using var world = new World();
        var shared = new AliasedTestExtension();
        world.SetExtension<IAliasedTestExtension>(shared);
        world.SetExtension(shared);

        world.SetExtension<IAliasedTestExtension>(new AliasedTestExtension());

        Assert.False(shared.IsDisposed);
        Assert.Same(shared, world.GetExtension<AliasedTestExtension>());
    }

    [Fact]
    public void RemoveExtension_WhenInstanceStillAliased_DoesNotDisposeIt()
    {
        using var world = new World();
        var shared = new AliasedTestExtension();
        world.SetExtension<IAliasedTestExtension>(shared);
        world.SetExtension(shared);

        var removed = world.RemoveExtension<IAliasedTestExtension>();

        Assert.True(removed);
        Assert.False(shared.IsDisposed);
        Assert.Same(shared, world.GetExtension<AliasedTestExtension>());
    }

    [Fact]
    public void RemoveExtension_WhenLastAliasRemoved_DisposesInstance()
    {
        // The flip side: once no registration resolves to the instance any more, the
        // manager still owns it and must dispose it exactly once.
        using var world = new World();
        var shared = new AliasedTestExtension();
        world.SetExtension<IAliasedTestExtension>(shared);
        world.SetExtension(shared);

        world.RemoveExtension<IAliasedTestExtension>();
        world.RemoveExtension<AliasedTestExtension>();

        Assert.True(shared.IsDisposed);
        Assert.Equal(1, shared.DisposeCount);
    }

    [Fact]
    public void WorldDispose_WithInstanceAliasedUnderSeveralTypes_DisposesItExactlyOnce()
    {
        // World.Dispose() clears the extension manager, which used to dispose once per
        // registration rather than once per instance. SilkGraphicsPlugin aliases a single
        // context under five keys, so teardown called Dispose() on it five times; that only
        // stayed harmless because those implementations happen to guard on a disposed flag.
        var shared = new AliasedTestExtension();

        using (var world = new World())
        {
            world.SetExtension<IAliasedTestExtension>(shared);
            world.SetExtension(shared);
        }

        Assert.True(shared.IsDisposed);
        Assert.Equal(1, shared.DisposeCount);
    }

    [Fact]
    public void WorldDispose_WithAliasedInstanceOwnedUnderOnlyOneType_StillDisposesItOnce()
    {
        // Ownership is per-registration, so an instance can be owned under one key and
        // caller-owned under another. Any owned registration means the manager disposes it,
        // and it must still happen exactly once regardless of which key is seen first.
        var shared = new AliasedTestExtension();

        using (var world = new World())
        {
            world.SetExtension<IAliasedTestExtension>(shared, owned: false);
            world.SetExtension(shared);
        }

        Assert.True(shared.IsDisposed);
        Assert.Equal(1, shared.DisposeCount);
    }

    [Fact]
    public void WorldDispose_WithAliasedInstanceCallerOwnedThroughout_NeverDisposesIt()
    {
        // No registration claimed ownership, so disposal stays the caller's job.
        var shared = new AliasedTestExtension();

        using (var world = new World())
        {
            world.SetExtension<IAliasedTestExtension>(shared, owned: false);
            world.SetExtension(shared, owned: false);
        }

        Assert.False(shared.IsDisposed);
        Assert.Equal(0, shared.DisposeCount);
    }

    #endregion
}

/// <summary>
/// Interface used to register a single instance under two extension keys.
/// </summary>
public interface IAliasedTestExtension;

/// <summary>
/// A disposable extension registered under both its interface and its concrete type.
/// </summary>
public sealed class AliasedTestExtension : IAliasedTestExtension, IDisposable
{
    /// <summary>Gets whether <see cref="Dispose"/> has been called.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Gets how many times <see cref="Dispose"/> has been called.</summary>
    public int DisposeCount { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeCount++;
        IsDisposed = true;
    }
}
