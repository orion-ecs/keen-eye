using KeenEyes.Events;
using Health = KeenEyes.Tests.TestHealth;
using Position = KeenEyes.Tests.TestPosition;
using Velocity = KeenEyes.Tests.TestVelocity;

namespace KeenEyes.Tests;

/// <summary>
/// Behavior-preservation tests for the hot-path allocation/boxing reductions (issue #1238):
/// the archetype-identity fast path, the entity-creation type extraction, and the
/// copy-on-write event snapshots. These assert that the optimized paths preserve the exact
/// semantics of the code they replaced (identity, hash, dispatch ordering, mid-dispatch safety).
/// </summary>
public class HotPathAllocationTests
{
    #region ArchetypeId Fast Path

    [Fact]
    public void FromUnsortedTypes_MatchesEnumerableCtor_IdentityAndHash()
    {
        // Every permutation of the same type set must yield an identity that is bit-for-bit
        // equal (equality + hash) to the LINQ-sorting IEnumerable constructor it replaces.
        Type[][] orderings =
        [
            [typeof(Position), typeof(Velocity), typeof(Health)],
            [typeof(Health), typeof(Position), typeof(Velocity)],
            [typeof(Velocity), typeof(Health), typeof(Position)],
        ];

        var reference = new ArchetypeId([typeof(Position), typeof(Velocity), typeof(Health)]);

        foreach (var ordering in orderings)
        {
            var fast = ArchetypeId.FromUnsortedTypes(ordering.AsSpan());

            Assert.Equal(reference, fast);
            Assert.Equal(reference.GetHashCode(), fast.GetHashCode());
            Assert.Equal(reference.ComponentTypes, fast.ComponentTypes);
        }
    }

    [Fact]
    public void FromUnsortedTypes_SingleType_MatchesEnumerableCtor()
    {
        var reference = new ArchetypeId([typeof(Position)]);
        var fast = ArchetypeId.FromUnsortedTypes([typeof(Position)]);

        Assert.Equal(reference, fast);
        Assert.Equal(reference.GetHashCode(), fast.GetHashCode());
    }

    [Fact]
    public void FromUnsortedTypes_Empty_MatchesEnumerableCtor()
    {
        var reference = new ArchetypeId([]);
        var fast = ArchetypeId.FromUnsortedTypes([]);

        Assert.Equal(reference, fast);
        Assert.Equal(reference.GetHashCode(), fast.GetHashCode());
    }

    [Fact]
    public void Spawn_WithDifferentComponentOrders_LandsInSameArchetype()
    {
        using var world = new World();

        // Two entities whose components are supplied in different orders must resolve to the
        // same archetype identity (the AddEntity hot path now uses the sorted fast path).
        var a = world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 3, Y = 4 })
            .Build();

        var b = world.Spawn()
            .With(new Velocity { X = 5, Y = 6 })
            .With(new Position { X = 7, Y = 8 })
            .Build();

        var locA = world.ArchetypeManager.GetEntityLocation(a);
        var locB = world.ArchetypeManager.GetEntityLocation(b);

        Assert.Same(locA.Archetype, locB.Archetype);
        Assert.Equal(1, world.ArchetypeManager.ArchetypeCount);

        // Component values remain intact through the co-located write.
        Assert.Equal(1f, world.Get<Position>(a).X);
        Assert.Equal(6f, world.Get<Velocity>(b).Y);
    }

    #endregion

    #region EventBus copy-on-write snapshot

    private readonly record struct TestEvent(int Value);

    [Fact]
    public void Publish_HandlerSubscribingMidDispatch_DoesNotInvokeNewHandlerThisDispatch()
    {
        var bus = new EventBus();
        var firstInvoked = 0;
        var lateInvoked = 0;

        bus.Subscribe<TestEvent>(_ =>
        {
            firstInvoked++;
            // Subscribe a new handler during dispatch; it must not run for the current publish.
            bus.Subscribe<TestEvent>(_ => lateInvoked++);
        });

        bus.Publish(new TestEvent(1));

        Assert.Equal(1, firstInvoked);
        Assert.Equal(0, lateInvoked);

        // The late handler is now part of the set for subsequent publishes.
        bus.Publish(new TestEvent(2));
        Assert.Equal(2, firstInvoked);
        Assert.Equal(1, lateInvoked);
    }

    [Fact]
    public void Publish_HandlerUnsubscribingMidDispatch_StillInvokesAllForCurrentDispatch()
    {
        var bus = new EventBus();
        var order = new List<int>();
        EventSubscription? second = null;

        bus.Subscribe<TestEvent>(_ =>
        {
            order.Add(1);
            // Remove the second handler mid-dispatch; the current dispatch must still call it.
            second?.Dispose();
        });
        second = bus.Subscribe<TestEvent>(_ => order.Add(2));

        bus.Publish(new TestEvent(1));

        Assert.Equal([1, 2], order);

        // After removal, subsequent publish only calls the first handler.
        order.Clear();
        bus.Publish(new TestEvent(2));
        Assert.Equal([1], order);
    }

    [Fact]
    public void Publish_PreservesRegistrationOrder()
    {
        var bus = new EventBus();
        var order = new List<int>();

        bus.Subscribe<TestEvent>(_ => order.Add(1));
        bus.Subscribe<TestEvent>(_ => order.Add(2));
        bus.Subscribe<TestEvent>(_ => order.Add(3));

        bus.Publish(new TestEvent(0));

        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public void Publish_SteadyState_DoesNotAllocate()
    {
        // Allocation regression guard: with a fixed handler set, the old code snapshotted the
        // handler list ([.. list]) on EVERY publish. The copy-on-write snapshot allocates only
        // on subscribe/unsubscribe, so a steady-state publish loop must allocate nothing.
        var bus = new EventBus();
        var sum = 0;
        bus.Subscribe<TestEvent>(e => sum += e.Value);

        // Warm up JIT/tiering so measurement covers only steady-state execution.
        for (int i = 0; i < 512; i++)
        {
            bus.Publish(new TestEvent(1));
        }

        const int iterations = 4096;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            bus.Publish(new TestEvent(1));
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Old behavior allocated one array per publish (tens of KB across the loop); new
        // behavior allocates nothing. A small allowance absorbs incidental runtime noise.
        Assert.True(allocated < 1024, $"Expected ~0 steady-state allocation, saw {allocated} bytes.");
        Assert.NotEqual(0, sum);
    }

    #endregion

    #region Component lifecycle copy-on-write snapshot

    [Fact]
    public void OnComponentAdded_HandlerSubscribingMidDispatch_DoesNotRunNewHandlerThisDispatch()
    {
        using var world = new World();
        var firstInvoked = 0;
        var lateInvoked = 0;

        world.OnComponentAdded<Health>((_, _) =>
        {
            firstInvoked++;
            world.OnComponentAdded<Health>((_, _) => lateInvoked++);
        });

        world.Spawn().With(new Health { Current = 1, Max = 1 }).Build();

        Assert.Equal(1, firstInvoked);
        Assert.Equal(0, lateInvoked);

        world.Spawn().With(new Health { Current = 2, Max = 2 }).Build();
        Assert.Equal(2, firstInvoked);
        Assert.Equal(1, lateInvoked);
    }

    [Fact]
    public void OnComponentAdded_InvokesHandlersInReverseRegistrationOrder()
    {
        using var world = new World();
        var order = new List<int>();

        world.OnComponentAdded<Health>((_, _) => order.Add(1));
        world.OnComponentAdded<Health>((_, _) => order.Add(2));
        world.OnComponentAdded<Health>((_, _) => order.Add(3));

        world.Spawn().With(new Health { Current = 1, Max = 1 }).Build();

        // ComponentEventHandlers dispatches in reverse registration order; preserved by the
        // copy-on-write snapshot.
        Assert.Equal([3, 2, 1], order);
    }

    #endregion
}
