namespace KeenEyes.Network;

/// <summary>
/// Context passed to the server's owner-authoritative state validation hook.
/// </summary>
/// <remarks>
/// <para>
/// The server invokes the validator once per component contained in an
/// <c>OwnerStateUpdate</c> message, after it has
/// confirmed that the sending client owns the target entity and that the
/// component uses <see cref="SyncStrategy.OwnerAuthoritative"/>.
/// </para>
/// <para>
/// Returning <see langword="false"/> rejects the update for that component,
/// leaving the server's authoritative state unchanged.
/// </para>
/// </remarks>
public readonly struct OwnerStateValidationContext
{
    /// <summary>
    /// Gets the client ID that sent the state update.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the server-side entity the update targets.
    /// </summary>
    public required Entity Entity { get; init; }

    /// <summary>
    /// Gets the component type being updated.
    /// </summary>
    public required Type ComponentType { get; init; }

    /// <summary>
    /// Gets the proposed component value (boxed) sent by the client.
    /// </summary>
    public required object Value { get; init; }
}

/// <summary>
/// Context passed to the server's ownership request policy hook.
/// </summary>
/// <remarks>
/// The server invokes the policy when a client sends an
/// <c>OwnershipRequest</c>. Returning
/// <see langword="true"/> grants ownership; returning <see langword="false"/>
/// denies it. When no policy is configured, requests are denied by default.
/// </remarks>
public readonly struct OwnershipRequestContext
{
    /// <summary>
    /// Gets the client ID requesting ownership.
    /// </summary>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets the server-side entity ownership is being requested for.
    /// </summary>
    public required Entity Entity { get; init; }

    /// <summary>
    /// Gets the network ID of the entity ownership is being requested for.
    /// </summary>
    public required uint NetworkId { get; init; }
}
