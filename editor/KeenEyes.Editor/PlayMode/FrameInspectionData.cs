using KeenEyes.Replay;

namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Provides detailed inspection data for a replay frame during debugging.
/// </summary>
/// <remarks>
/// <para>
/// This class aggregates information about a single frame in a replay,
/// including timing, events, and entity changes. It is used by the editor
/// to display frame-by-frame debugging information.
/// </para>
/// <para>
/// The data is extracted from <see cref="ReplayFrame"/> events and categorized
/// for easy inspection in the editor's debugging UI.
/// </para>
/// </remarks>
public sealed class FrameInspectionData
{
    /// <summary>
    /// Gets the sequential frame number.
    /// </summary>
    public int FrameNumber { get; }

    /// <summary>
    /// Gets the delta time for this frame.
    /// </summary>
    public TimeSpan DeltaTime { get; }

    /// <summary>
    /// Gets the elapsed time since the start of the replay.
    /// </summary>
    public TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets all events that occurred during this frame.
    /// </summary>
    public IReadOnlyList<ReplayEvent> Events { get; }

    /// <summary>
    /// Gets the entity IDs that were created during this frame.
    /// </summary>
    public IReadOnlyList<int> CreatedEntities { get; }

    /// <summary>
    /// Gets the entity IDs that were destroyed during this frame.
    /// </summary>
    public IReadOnlyList<int> DestroyedEntities { get; }

    /// <summary>
    /// Gets the component changes that occurred during this frame.
    /// </summary>
    /// <remarks>
    /// Each tuple contains the entity ID and the component type name.
    /// </remarks>
    public IReadOnlyList<ComponentChange> ComponentChanges { get; }

    /// <summary>
    /// Gets the system executions that occurred during this frame.
    /// </summary>
    public IReadOnlyList<SystemExecution> SystemExecutions { get; }

    /// <summary>
    /// Gets the custom events that occurred during this frame.
    /// </summary>
    public IReadOnlyList<ReplayEvent> CustomEvents { get; }

    /// <summary>
    /// Creates a new instance of <see cref="FrameInspectionData"/> from a replay frame.
    /// </summary>
    /// <param name="frame">The replay frame to inspect.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="frame"/> is null.</exception>
    public FrameInspectionData(ReplayFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        FrameNumber = frame.FrameNumber;
        DeltaTime = frame.DeltaTime;
        ElapsedTime = frame.ElapsedTime;
        Events = frame.Events;

        // Extract entity creations
        var createdEntities = new List<int>();
        var destroyedEntities = new List<int>();
        var componentChanges = new List<ComponentChange>();
        var systemExecutions = new List<SystemExecution>();
        var customEvents = new List<ReplayEvent>();

        SystemExecution? currentSystemExecution = null;

        foreach (var evt in frame.Events)
        {
            switch (evt.Type)
            {
                case ReplayEventType.EntityCreated when evt.EntityId.HasValue:
                    createdEntities.Add(evt.EntityId.Value);
                    break;

                case ReplayEventType.EntityDestroyed when evt.EntityId.HasValue:
                    destroyedEntities.Add(evt.EntityId.Value);
                    break;

                case ReplayEventType.ComponentAdded when evt.EntityId.HasValue && evt.ComponentTypeName is not null:
                    componentChanges.Add(new ComponentChange(
                        evt.EntityId.Value,
                        evt.ComponentTypeName,
                        ComponentChangeType.Added));
                    break;

                case ReplayEventType.ComponentRemoved when evt.EntityId.HasValue && evt.ComponentTypeName is not null:
                    componentChanges.Add(new ComponentChange(
                        evt.EntityId.Value,
                        evt.ComponentTypeName,
                        ComponentChangeType.Removed));
                    break;

                case ReplayEventType.ComponentChanged when evt.EntityId.HasValue && evt.ComponentTypeName is not null:
                    componentChanges.Add(new ComponentChange(
                        evt.EntityId.Value,
                        evt.ComponentTypeName,
                        ComponentChangeType.Modified));
                    break;

                case ReplayEventType.SystemStart when evt.SystemTypeName is not null:
                    currentSystemExecution = new SystemExecution(evt.SystemTypeName, evt.Timestamp);
                    break;

                case ReplayEventType.SystemEnd when evt.SystemTypeName is not null && currentSystemExecution.HasValue:
                    var execution = currentSystemExecution.Value;
                    if (execution.SystemTypeName == evt.SystemTypeName)
                    {
                        systemExecutions.Add(execution with
                        {
                            Duration = evt.Timestamp - execution.StartTime
                        });
                        currentSystemExecution = null;
                    }
                    break;

                case ReplayEventType.Custom:
                    customEvents.Add(evt);
                    break;
            }
        }

        CreatedEntities = createdEntities;
        DestroyedEntities = destroyedEntities;
        ComponentChanges = componentChanges;
        SystemExecutions = systemExecutions;
        CustomEvents = customEvents;
    }
}

/// <summary>
/// Represents a component change during a replay frame.
/// </summary>
/// <param name="EntityId">The entity ID affected.</param>
/// <param name="ComponentTypeName">The fully qualified name of the component type.</param>
/// <param name="ChangeType">The type of change that occurred.</param>
public readonly record struct ComponentChange(
    int EntityId,
    string ComponentTypeName,
    ComponentChangeType ChangeType);

/// <summary>
/// The type of component change.
/// </summary>
public enum ComponentChangeType
{
    /// <summary>
    /// A component was added to an entity.
    /// </summary>
    Added,

    /// <summary>
    /// A component was removed from an entity.
    /// </summary>
    Removed,

    /// <summary>
    /// A component value was modified.
    /// </summary>
    Modified
}

/// <summary>
/// Represents a system execution during a replay frame.
/// </summary>
/// <param name="SystemTypeName">The fully qualified name of the system type.</param>
/// <param name="StartTime">The time offset when the system started executing.</param>
/// <param name="Duration">The duration of the system execution, or null if not yet complete.</param>
public readonly record struct SystemExecution(
    string SystemTypeName,
    TimeSpan StartTime,
    TimeSpan? Duration = null);
