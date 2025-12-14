using KeenEyes.Common;

namespace KeenEyes.Physics.Components;

/// <summary>
/// Component that controls which other bodies this entity can collide with.
/// </summary>
/// <remarks>
/// <para>
/// Collision filtering uses a layer/mask system where each body belongs to one or more
/// layers (defined by <see cref="Layer"/>) and specifies which layers it can collide
/// with (defined by <see cref="Mask"/>).
/// </para>
/// <para>
/// Two bodies can collide only if each body's layer is included in the other's mask.
/// This is a bidirectional check: (A.Layer &amp; B.Mask) != 0 AND (B.Layer &amp; A.Mask) != 0.
/// </para>
/// <para>
/// By default, all entities are in layer 1 and collide with all layers (mask = 0xFFFFFFFF).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define collision layers as bit flags
/// const uint PlayerLayer = 1 &lt;&lt; 0;    // 0x00000001
/// const uint EnemyLayer = 1 &lt;&lt; 1;     // 0x00000002
/// const uint BulletLayer = 1 &lt;&lt; 2;    // 0x00000004
/// const uint WallLayer = 1 &lt;&lt; 3;      // 0x00000008
///
/// // Player collides with enemies and walls, but not own bullets
/// var player = world.Spawn()
///     .With(new CollisionFilter
///     {
///         Layer = PlayerLayer,
///         Mask = EnemyLayer | WallLayer
///     })
///     // ... other components
///     .Build();
///
/// // Enemy bullets collide with player and walls
/// var bullet = world.Spawn()
///     .With(new CollisionFilter
///     {
///         Layer = BulletLayer,
///         Mask = PlayerLayer | WallLayer
///     })
///     // ... other components
///     .Build();
/// </code>
/// </example>
public struct CollisionFilter : IComponent
{
    /// <summary>
    /// The collision layer(s) this entity belongs to.
    /// </summary>
    /// <remarks>
    /// Use bit flags to assign an entity to multiple layers.
    /// Default is 1 (first layer).
    /// </remarks>
    public uint Layer;

    /// <summary>
    /// The collision mask specifying which layers this entity can collide with.
    /// </summary>
    /// <remarks>
    /// Use bit flags to specify multiple layers.
    /// Default is 0xFFFFFFFF (all layers).
    /// </remarks>
    public uint Mask;

    /// <summary>
    /// When true, this body acts as a trigger/sensor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Trigger bodies detect overlaps but don't generate physical collision responses.
    /// They still fire collision events, but objects pass through them.
    /// </para>
    /// <para>
    /// Common uses include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Pickup items</description></item>
    /// <item><description>Area triggers (entering a room)</description></item>
    /// <item><description>Damage zones</description></item>
    /// <item><description>Checkpoints</description></item>
    /// </list>
    /// </remarks>
    public bool IsTrigger;

    /// <summary>
    /// Creates a collision filter with the specified layer and mask.
    /// </summary>
    /// <param name="layer">The collision layer(s) this entity belongs to.</param>
    /// <param name="mask">The layers this entity can collide with.</param>
    /// <param name="isTrigger">Whether this is a trigger (no physical response).</param>
    public CollisionFilter(uint layer, uint mask, bool isTrigger = false)
    {
        Layer = layer;
        Mask = mask;
        IsTrigger = isTrigger;
    }

    /// <summary>
    /// Creates a default collision filter that collides with everything.
    /// </summary>
    public CollisionFilter()
    {
        Layer = 1;
        Mask = 0xFFFFFFFF;
        IsTrigger = false;
    }

    /// <summary>
    /// Default filter that collides with everything.
    /// </summary>
    public static CollisionFilter Default => new(layer: 1, mask: 0xFFFFFFFF);

    /// <summary>
    /// Creates a trigger filter that detects overlaps but has no physical response.
    /// </summary>
    /// <param name="layer">The collision layer(s) this trigger belongs to.</param>
    /// <param name="mask">The layers this trigger can detect.</param>
    /// <returns>A collision filter configured as a trigger.</returns>
    public static CollisionFilter Trigger(uint layer = 1, uint mask = 0xFFFFFFFF)
        => new(layer, mask, isTrigger: true);

    /// <summary>
    /// Checks if two collision filters allow collision between their entities.
    /// </summary>
    /// <param name="other">The other collision filter to check against.</param>
    /// <returns>True if collision is allowed between the two filters.</returns>
    public readonly bool CanCollideWith(in CollisionFilter other)
    {
        // Bidirectional check: each body's layer must be in the other's mask
        return (Layer & other.Mask) != 0 && (other.Layer & Mask) != 0;
    }

    /// <summary>
    /// Checks if either filter is a trigger.
    /// </summary>
    /// <param name="other">The other collision filter to check.</param>
    /// <returns>True if either filter is a trigger.</returns>
    public readonly bool IsTriggerCollision(in CollisionFilter other)
        => IsTrigger || other.IsTrigger;
}
