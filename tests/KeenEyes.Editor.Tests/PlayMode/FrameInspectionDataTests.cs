using KeenEyes.Editor.PlayMode;
using KeenEyes.Replay;

namespace KeenEyes.Editor.Tests.PlayMode;

public class FrameInspectionDataTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullFrame()
    {
        Assert.Throws<ArgumentNullException>(() => new FrameInspectionData(null!));
    }

    [Fact]
    public void Constructor_ExtractsBasicProperties()
    {
        var frame = CreateFrameWithBasicEvents();

        var data = new FrameInspectionData(frame);

        Assert.Equal(frame.FrameNumber, data.FrameNumber);
        Assert.Equal(frame.DeltaTime, data.DeltaTime);
        Assert.Equal(frame.ElapsedTime, data.ElapsedTime);
        Assert.Equal(frame.Events, data.Events);
    }

    #endregion

    #region Created Entities Tests

    [Fact]
    public void CreatedEntities_ExtractsEntityCreatedEvents()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = 1 },
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = 2 },
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = 3 }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Equal(3, data.CreatedEntities.Count);
        Assert.Contains(1, data.CreatedEntities);
        Assert.Contains(2, data.CreatedEntities);
        Assert.Contains(3, data.CreatedEntities);
    }

    [Fact]
    public void CreatedEntities_IgnoresEventsWithoutEntityId()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = 1 },
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = null }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.CreatedEntities);
        Assert.Equal(1, data.CreatedEntities[0]);
    }

    #endregion

    #region Destroyed Entities Tests

    [Fact]
    public void DestroyedEntities_ExtractsEntityDestroyedEvents()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent { Type = ReplayEventType.EntityDestroyed, EntityId = 5 },
            new ReplayEvent { Type = ReplayEventType.EntityDestroyed, EntityId = 6 }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Equal(2, data.DestroyedEntities.Count);
        Assert.Contains(5, data.DestroyedEntities);
        Assert.Contains(6, data.DestroyedEntities);
    }

    #endregion

    #region Component Changes Tests

    [Fact]
    public void ComponentChanges_ExtractsComponentAddedEvents()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent
            {
                Type = ReplayEventType.ComponentAdded,
                EntityId = 1,
                ComponentTypeName = "TestComponent"
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.ComponentChanges);
        var change = data.ComponentChanges[0];
        Assert.Equal(1, change.EntityId);
        Assert.Equal("TestComponent", change.ComponentTypeName);
        Assert.Equal(ComponentChangeType.Added, change.ChangeType);
    }

    [Fact]
    public void ComponentChanges_ExtractsComponentRemovedEvents()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent
            {
                Type = ReplayEventType.ComponentRemoved,
                EntityId = 2,
                ComponentTypeName = "Position"
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.ComponentChanges);
        var change = data.ComponentChanges[0];
        Assert.Equal(2, change.EntityId);
        Assert.Equal("Position", change.ComponentTypeName);
        Assert.Equal(ComponentChangeType.Removed, change.ChangeType);
    }

    [Fact]
    public void ComponentChanges_ExtractsComponentChangedEvents()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent
            {
                Type = ReplayEventType.ComponentChanged,
                EntityId = 3,
                ComponentTypeName = "Velocity"
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.ComponentChanges);
        var change = data.ComponentChanges[0];
        Assert.Equal(3, change.EntityId);
        Assert.Equal("Velocity", change.ComponentTypeName);
        Assert.Equal(ComponentChangeType.Modified, change.ChangeType);
    }

    [Fact]
    public void ComponentChanges_IgnoresEventsWithMissingData()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent { Type = ReplayEventType.ComponentAdded, EntityId = 1 },
            new ReplayEvent { Type = ReplayEventType.ComponentAdded, ComponentTypeName = "Test" },
            new ReplayEvent
            {
                Type = ReplayEventType.ComponentAdded,
                EntityId = 1,
                ComponentTypeName = "Valid"
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.ComponentChanges);
        Assert.Equal("Valid", data.ComponentChanges[0].ComponentTypeName);
    }

    #endregion

    #region System Executions Tests

    [Fact]
    public void SystemExecutions_ExtractsSystemStartEndPairs()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent
            {
                Type = ReplayEventType.SystemStart,
                SystemTypeName = "MovementSystem",
                Timestamp = TimeSpan.FromMilliseconds(1)
            },
            new ReplayEvent
            {
                Type = ReplayEventType.SystemEnd,
                SystemTypeName = "MovementSystem",
                Timestamp = TimeSpan.FromMilliseconds(3)
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.SystemExecutions);
        var execution = data.SystemExecutions[0];
        Assert.Equal("MovementSystem", execution.SystemTypeName);
        Assert.Equal(TimeSpan.FromMilliseconds(1), execution.StartTime);
        Assert.Equal(TimeSpan.FromMilliseconds(2), execution.Duration);
    }

    [Fact]
    public void SystemExecutions_HandlesMultipleSystems()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent
            {
                Type = ReplayEventType.SystemStart,
                SystemTypeName = "SystemA",
                Timestamp = TimeSpan.FromMilliseconds(0)
            },
            new ReplayEvent
            {
                Type = ReplayEventType.SystemEnd,
                SystemTypeName = "SystemA",
                Timestamp = TimeSpan.FromMilliseconds(2)
            },
            new ReplayEvent
            {
                Type = ReplayEventType.SystemStart,
                SystemTypeName = "SystemB",
                Timestamp = TimeSpan.FromMilliseconds(3)
            },
            new ReplayEvent
            {
                Type = ReplayEventType.SystemEnd,
                SystemTypeName = "SystemB",
                Timestamp = TimeSpan.FromMilliseconds(5)
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Equal(2, data.SystemExecutions.Count);
        Assert.Equal("SystemA", data.SystemExecutions[0].SystemTypeName);
        Assert.Equal("SystemB", data.SystemExecutions[1].SystemTypeName);
    }

    #endregion

    #region Custom Events Tests

    [Fact]
    public void CustomEvents_ExtractsCustomEvents()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent
            {
                Type = ReplayEventType.Custom,
                CustomType = "PlayerJumped"
            },
            new ReplayEvent
            {
                Type = ReplayEventType.Custom,
                CustomType = "ItemPickedUp"
            }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Equal(2, data.CustomEvents.Count);
        Assert.Equal("PlayerJumped", data.CustomEvents[0].CustomType);
        Assert.Equal("ItemPickedUp", data.CustomEvents[1].CustomType);
    }

    #endregion

    #region Mixed Events Tests

    [Fact]
    public void MixedEvents_CorrectlyCategorizes()
    {
        var frame = CreateFrameWithEvents([
            new ReplayEvent { Type = ReplayEventType.FrameStart },
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = 1 },
            new ReplayEvent
            {
                Type = ReplayEventType.ComponentAdded,
                EntityId = 1,
                ComponentTypeName = "Position"
            },
            new ReplayEvent
            {
                Type = ReplayEventType.SystemStart,
                SystemTypeName = "TestSystem",
                Timestamp = TimeSpan.FromMilliseconds(1)
            },
            new ReplayEvent
            {
                Type = ReplayEventType.SystemEnd,
                SystemTypeName = "TestSystem",
                Timestamp = TimeSpan.FromMilliseconds(5)
            },
            new ReplayEvent { Type = ReplayEventType.Custom, CustomType = "TestEvent" },
            new ReplayEvent { Type = ReplayEventType.EntityDestroyed, EntityId = 2 },
            new ReplayEvent { Type = ReplayEventType.FrameEnd }
        ]);

        var data = new FrameInspectionData(frame);

        Assert.Single(data.CreatedEntities);
        Assert.Single(data.DestroyedEntities);
        Assert.Single(data.ComponentChanges);
        Assert.Single(data.SystemExecutions);
        Assert.Single(data.CustomEvents);
        Assert.Equal(8, data.Events.Count);
    }

    #endregion

    #region Test Helpers

    private static ReplayFrame CreateFrameWithBasicEvents()
    {
        return new ReplayFrame
        {
            FrameNumber = 0,
            DeltaTime = TimeSpan.FromMilliseconds(16.67),
            ElapsedTime = TimeSpan.Zero,
            Events = [
                new ReplayEvent { Type = ReplayEventType.FrameStart },
                new ReplayEvent { Type = ReplayEventType.FrameEnd }
            ]
        };
    }

    private static ReplayFrame CreateFrameWithEvents(IReadOnlyList<ReplayEvent> events)
    {
        return new ReplayFrame
        {
            FrameNumber = 0,
            DeltaTime = TimeSpan.FromMilliseconds(16.67),
            ElapsedTime = TimeSpan.Zero,
            Events = events
        };
    }

    #endregion
}
