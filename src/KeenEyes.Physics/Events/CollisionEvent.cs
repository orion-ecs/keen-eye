using System.Numerics;

namespace KeenEyes.Physics.Events;

/// <summary>
/// Event raised when a collision occurs between two physics bodies.
/// </summary>
/// <remarks>
/// <para>
/// This event is published through the ECS world's messaging system when physics bodies
/// collide. Subscribe to this event to respond to collisions in your game logic.
/// </para>
/// <para>
/// Collision events are published during the physics step, after contact manifolds are
/// processed. Events include detailed contact information for physics-based responses.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Subscribe to collision events
/// var subscription = world.Subscribe&lt;CollisionEvent&gt;(collision =>
/// {
///     if (world.Has&lt;PlayerTag&gt;(collision.EntityA) &amp;&amp; world.Has&lt;EnemyTag&gt;(collision.EntityB))
///     {
///         // Player hit an enemy
///         ApplyDamage(collision.EntityA);
///     }
/// });
/// </code>
/// </example>
/// <param name="EntityA">The first entity involved in the collision.</param>
/// <param name="EntityB">The second entity involved in the collision.</param>
/// <param name="ContactNormal">The contact normal pointing from EntityA to EntityB.</param>
/// <param name="ContactPoint">The world-space position of the contact point.</param>
/// <param name="PenetrationDepth">The penetration depth (overlap amount). Positive when objects overlap.</param>
/// <param name="IsTrigger">True if this is a trigger/sensor collision (no physics response).</param>
public readonly record struct CollisionEvent(
    Entity EntityA,
    Entity EntityB,
    Vector3 ContactNormal,
    Vector3 ContactPoint,
    float PenetrationDepth,
    bool IsTrigger);

/// <summary>
/// Event raised when a collision between two physics bodies begins.
/// </summary>
/// <remarks>
/// <para>
/// This event is published once when two bodies first come into contact.
/// Use this for triggering effects when collisions start, such as:
/// </para>
/// <list type="bullet">
/// <item><description>Playing impact sounds</description></item>
/// <item><description>Spawning particle effects</description></item>
/// <item><description>Starting damage-over-time effects</description></item>
/// </list>
/// </remarks>
/// <param name="EntityA">The first entity involved in the collision.</param>
/// <param name="EntityB">The second entity involved in the collision.</param>
/// <param name="ContactNormal">The contact normal pointing from EntityA to EntityB.</param>
/// <param name="ContactPoint">The world-space position of the initial contact point.</param>
/// <param name="PenetrationDepth">The initial penetration depth.</param>
/// <param name="IsTrigger">True if this is a trigger/sensor collision.</param>
public readonly record struct CollisionStartedEvent(
    Entity EntityA,
    Entity EntityB,
    Vector3 ContactNormal,
    Vector3 ContactPoint,
    float PenetrationDepth,
    bool IsTrigger);

/// <summary>
/// Event raised when a collision between two physics bodies ends.
/// </summary>
/// <remarks>
/// <para>
/// This event is published when two bodies that were previously in contact
/// separate. Use this for:
/// </para>
/// <list type="bullet">
/// <item><description>Stopping continuous effects</description></item>
/// <item><description>Ending damage-over-time</description></item>
/// <item><description>Triggering exit animations</description></item>
/// </list>
/// </remarks>
/// <param name="EntityA">The first entity that was involved in the collision.</param>
/// <param name="EntityB">The second entity that was involved in the collision.</param>
/// <param name="WasTrigger">True if this was a trigger/sensor collision.</param>
public readonly record struct CollisionEndedEvent(
    Entity EntityA,
    Entity EntityB,
    bool WasTrigger);
