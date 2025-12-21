namespace KeenEyes.Replay;

/// <summary>
/// Represents the type of a replay event.
/// </summary>
/// <remarks>
/// Event types are used to categorize and filter events during playback.
/// Custom event types can be added by plugins using values starting at <see cref="Custom"/>.
/// </remarks>
public enum ReplayEventType
{
    /// <summary>
    /// A custom event type defined by the application.
    /// </summary>
    Custom = 0,

    /// <summary>
    /// Marks the start of a frame.
    /// </summary>
    FrameStart = 1,

    /// <summary>
    /// Marks the end of a frame.
    /// </summary>
    FrameEnd = 2,

    /// <summary>
    /// A system began execution.
    /// </summary>
    SystemStart = 3,

    /// <summary>
    /// A system finished execution.
    /// </summary>
    SystemEnd = 4,

    /// <summary>
    /// An entity was created.
    /// </summary>
    EntityCreated = 5,

    /// <summary>
    /// An entity was destroyed.
    /// </summary>
    EntityDestroyed = 6,

    /// <summary>
    /// A component was added to an entity.
    /// </summary>
    ComponentAdded = 7,

    /// <summary>
    /// A component was removed from an entity.
    /// </summary>
    ComponentRemoved = 8,

    /// <summary>
    /// A component value was changed.
    /// </summary>
    ComponentChanged = 9,

    /// <summary>
    /// A world snapshot was captured.
    /// </summary>
    Snapshot = 10,
}

/// <summary>
/// Represents a single recorded event within a replay frame.
/// </summary>
/// <remarks>
/// <para>
/// Events capture discrete actions that occurred during gameplay, such as
/// entity creation, component modifications, or custom game events. Each
/// event has a timestamp relative to the frame start and optional payload data.
/// </para>
/// <para>
/// The replay system is framework-agnostic and does not prescribe specific
/// event types. Applications define their own event types by using
/// <see cref="ReplayEventType.Custom"/> and storing event-specific data
/// in the <see cref="Data"/> property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record a custom game event
/// recorder.RecordEvent(new ReplayEvent
/// {
///     Type = ReplayEventType.Custom,
///     CustomType = "PlayerJumped",
///     Timestamp = TimeSpan.FromSeconds(1.5),
///     Data = new Dictionary&lt;string, object&gt; { ["height"] = 2.5f }
/// });
/// </code>
/// </example>
public sealed record ReplayEvent
{
    /// <summary>
    /// Gets or sets the type of this event.
    /// </summary>
    public required ReplayEventType Type { get; init; }

    /// <summary>
    /// Gets or sets the custom event type name for <see cref="ReplayEventType.Custom"/> events.
    /// </summary>
    /// <remarks>
    /// This field is only used when <see cref="Type"/> is <see cref="ReplayEventType.Custom"/>.
    /// It allows applications to define their own event types without modifying the enum.
    /// </remarks>
    public string? CustomType { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of this event relative to the frame start.
    /// </summary>
    /// <remarks>
    /// The timestamp represents the offset from the beginning of the frame
    /// when this event occurred. This enables accurate event ordering and
    /// interpolation during playback.
    /// </remarks>
    public TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the entity ID associated with this event, if applicable.
    /// </summary>
    /// <remarks>
    /// For entity-related events (<see cref="ReplayEventType.EntityCreated"/>,
    /// <see cref="ReplayEventType.ComponentAdded"/>, etc.), this contains the
    /// entity ID that was affected.
    /// </remarks>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets or sets the system type name for system execution events.
    /// </summary>
    /// <remarks>
    /// For <see cref="ReplayEventType.SystemStart"/> and <see cref="ReplayEventType.SystemEnd"/>
    /// events, this contains the fully qualified type name of the system.
    /// </remarks>
    public string? SystemTypeName { get; init; }

    /// <summary>
    /// Gets or sets the component type name for component events.
    /// </summary>
    /// <remarks>
    /// For component-related events (<see cref="ReplayEventType.ComponentAdded"/>,
    /// <see cref="ReplayEventType.ComponentRemoved"/>, <see cref="ReplayEventType.ComponentChanged"/>),
    /// this contains the fully qualified type name of the component.
    /// </remarks>
    public string? ComponentTypeName { get; init; }

    /// <summary>
    /// Gets or sets optional event-specific data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dictionary can store any serializable data associated with the event.
    /// For component events, this may contain the component value. For custom
    /// events, applications can store any relevant data.
    /// </para>
    /// <para>
    /// Data values must be JSON-serializable types (strings, numbers, booleans,
    /// or nested dictionaries/arrays of these types).
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}
