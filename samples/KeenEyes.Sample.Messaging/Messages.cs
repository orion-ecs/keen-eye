namespace KeenEyes.Sample.Messaging;

// =============================================================================
// MESSAGE TYPES
// =============================================================================
// Messages are lightweight data structures for inter-system communication.
// Using readonly record struct provides:
// - Immutability (safe to pass around)
// - Value equality (easy comparison)
// - Zero heap allocations (struct)
// - Concise syntax (record)
// =============================================================================

/// <summary>
/// Sent when an entity attacks another.
/// </summary>
/// <param name="Attacker">The attacking entity.</param>
/// <param name="Target">The target entity.</param>
/// <param name="Damage">The damage amount.</param>
public readonly record struct AttackMessage(Entity Attacker, Entity Target, int Damage);

/// <summary>
/// Sent when an entity takes damage.
/// </summary>
/// <param name="Target">The entity that took damage.</param>
/// <param name="Amount">The damage amount.</param>
/// <param name="Source">The source of the damage.</param>
public readonly record struct DamageMessage(Entity Target, int Amount, Entity Source);

/// <summary>
/// Sent when an entity dies.
/// </summary>
/// <param name="Entity">The entity that died.</param>
/// <param name="Cause">The cause of death.</param>
public readonly record struct DeathMessage(Entity Entity, string Cause);

/// <summary>
/// Sent when a collision is detected between two entities.
/// </summary>
/// <param name="Entity1">The first entity in the collision.</param>
/// <param name="Entity2">The second entity in the collision.</param>
public readonly record struct CollisionMessage(Entity Entity1, Entity Entity2);

/// <summary>
/// Sent to request spawning an enemy.
/// </summary>
/// <param name="X">The X position to spawn at.</param>
/// <param name="Y">The Y position to spawn at.</param>
/// <param name="Health">The initial health of the enemy.</param>
public readonly record struct SpawnEnemyRequest(float X, float Y, int Health);

/// <summary>
/// Sent when an enemy is successfully spawned.
/// </summary>
/// <param name="Enemy">The spawned enemy entity.</param>
public readonly record struct EnemySpawnedMessage(Entity Enemy);

/// <summary>
/// Sent when an item is picked up.
/// </summary>
/// <param name="Picker">The entity that picked up the item.</param>
/// <param name="ItemType">The type of item picked up.</param>
/// <param name="Value">The value of the item.</param>
public readonly record struct ItemPickupMessage(Entity Picker, string ItemType, int Value);

/// <summary>
/// Sent when the player's score changes.
/// </summary>
/// <param name="OldScore">The previous score.</param>
/// <param name="NewScore">The new score.</param>
public readonly record struct ScoreChangedMessage(int OldScore, int NewScore);
