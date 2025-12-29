namespace KeenEyes.Network.Components;

/// <summary>
/// Tag component marking an entity for network replication.
/// </summary>
/// <remarks>
/// Add this tag to entities that should be synchronized across the network.
/// Entities without this tag are local-only.
/// </remarks>
public readonly record struct Networked : ITagComponent;

/// <summary>
/// Tag component marking an entity for interpolation.
/// </summary>
/// <remarks>
/// <para>
/// Entities with this tag render slightly behind server time and interpolate
/// between received snapshots for smooth visual movement.
/// </para>
/// <para>
/// Typically applied to remote player entities and other moving objects
/// that the local player doesn't control.
/// </para>
/// </remarks>
public readonly record struct Interpolated : ITagComponent;

/// <summary>
/// Tag component marking an entity for client-side prediction.
/// </summary>
/// <remarks>
/// <para>
/// Entities with this tag run simulation ahead of the server for responsive
/// controls. When server state diverges from predictions, the entity is
/// rolled back and re-simulated.
/// </para>
/// <para>
/// Typically applied only to the local player entity.
/// </para>
/// </remarks>
public readonly record struct Predicted : ITagComponent;

/// <summary>
/// Tag component marking an entity as locally owned.
/// </summary>
/// <remarks>
/// Applied by the network plugin when the local client owns this entity.
/// Useful for queries that should only affect the local player.
/// </remarks>
public readonly record struct LocallyOwned : ITagComponent;

/// <summary>
/// Tag component marking an entity as remotely owned.
/// </summary>
/// <remarks>
/// Applied by the network plugin when a remote client or server owns this entity.
/// Useful for queries that should only affect other players.
/// </remarks>
public readonly record struct RemotelyOwned : ITagComponent;
