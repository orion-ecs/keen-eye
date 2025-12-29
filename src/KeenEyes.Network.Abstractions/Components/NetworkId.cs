namespace KeenEyes.Network.Components;

/// <summary>
/// Component that identifies an entity across the network.
/// </summary>
/// <remarks>
/// <para>
/// Every networked entity has a NetworkId assigned by the server. Clients use this
/// ID to map server entities to their local representations. The same server entity
/// may have different local Entity IDs on each client.
/// </para>
/// <para>
/// Network IDs are stable for the lifetime of the entity on the server. When an
/// entity is destroyed and recreated, it gets a new NetworkId.
/// </para>
/// </remarks>
public readonly record struct NetworkId : IComponent
{
    /// <summary>
    /// Gets the network-wide unique identifier for this entity.
    /// </summary>
    /// <remarks>
    /// Assigned by the server. Clients receive this ID when the entity is replicated.
    /// </remarks>
    public required uint Value { get; init; }

    /// <summary>
    /// Gets an invalid/unassigned network ID.
    /// </summary>
    public static NetworkId Invalid => new() { Value = 0 };

    /// <summary>
    /// Gets whether this network ID is valid (non-zero).
    /// </summary>
    public bool IsValid => Value != 0;

    /// <inheritdoc/>
    public override string ToString() => $"NetworkId({Value})";
}
