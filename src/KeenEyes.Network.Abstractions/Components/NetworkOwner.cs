namespace KeenEyes.Network.Components;

/// <summary>
/// Component that tracks which client owns/controls an entity.
/// </summary>
/// <remarks>
/// <para>
/// Ownership determines authority: the owner's inputs are applied to this entity.
/// For server-owned entities (like NPCs), <see cref="ClientId"/> is 0.
/// </para>
/// <para>
/// Ownership can be transferred between clients, for example when a player
/// picks up an item or enters a vehicle.
/// </para>
/// </remarks>
public readonly record struct NetworkOwner : IComponent
{
    /// <summary>
    /// The server's client ID (server-owned entities).
    /// </summary>
    public const int ServerClientId = 0;

    /// <summary>
    /// Gets the client ID that owns this entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// - 0: Server-owned (NPCs, world objects)
    /// - 1+: Client-owned (player entities, client-spawned objects)
    /// </para>
    /// </remarks>
    public required int ClientId { get; init; }

    /// <summary>
    /// Gets whether this entity is owned by the server.
    /// </summary>
    public bool IsServerOwned => ClientId == ServerClientId;

    /// <summary>
    /// Creates a server-owned network owner.
    /// </summary>
    public static NetworkOwner Server => new() { ClientId = ServerClientId };

    /// <summary>
    /// Creates a client-owned network owner.
    /// </summary>
    /// <param name="clientId">The owning client's ID.</param>
    public static NetworkOwner Client(int clientId) => new() { ClientId = clientId };

    /// <inheritdoc/>
    public override string ToString() =>
        ClientId == ServerClientId ? "NetworkOwner(Server)" : $"NetworkOwner(Client:{ClientId})";
}
