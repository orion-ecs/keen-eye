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
}
