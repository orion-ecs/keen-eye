using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Events;

/// <summary>
/// Event sent through the world messaging system when an agent begins
/// traversing an off-mesh link.
/// </summary>
/// <remarks>
/// <para>
/// Subscribe to this event to trigger traversal animations (jump, climb,
/// teleport) or gameplay logic when an agent leaves the walkable surface.
/// The matching <see cref="OffMeshLinkTraversalCompleted"/> event is sent
/// when the agent reaches the far endpoint.
/// </para>
/// <example>
/// <code>
/// var subscription = world.Subscribe&lt;OffMeshLinkTraversalStarted&gt;(evt =>
/// {
///     PlayJumpAnimation(evt.Entity);
/// });
/// </code>
/// </example>
/// </remarks>
/// <param name="Entity">The agent entity traversing the link.</param>
/// <param name="Start">The world-space position where the traversal begins.</param>
/// <param name="End">The world-space position where the traversal ends.</param>
/// <param name="AreaType">The navigation area type of the link.</param>
public readonly record struct OffMeshLinkTraversalStarted(
    Entity Entity,
    Vector3 Start,
    Vector3 End,
    NavAreaType AreaType);

/// <summary>
/// Event sent through the world messaging system when an agent finishes
/// traversing an off-mesh link and resumes normal path following.
/// </summary>
/// <param name="Entity">The agent entity that traversed the link.</param>
/// <param name="Start">The world-space position where the traversal began.</param>
/// <param name="End">The world-space position where the traversal ended.</param>
/// <param name="AreaType">The navigation area type of the link.</param>
public readonly record struct OffMeshLinkTraversalCompleted(
    Entity Entity,
    Vector3 Start,
    Vector3 End,
    NavAreaType AreaType);
