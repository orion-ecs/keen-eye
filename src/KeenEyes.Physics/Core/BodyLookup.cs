using BepuPhysics;

namespace KeenEyes.Physics.Core;

/// <summary>
/// Maintains bidirectional mapping between ECS entities and BepuPhysics bodies.
/// </summary>
internal sealed class BodyLookup
{
    private readonly Dictionary<Entity, BodyHandle> entityToBody = [];
    private readonly Dictionary<BodyHandle, Entity> bodyToEntity = [];
    private readonly Dictionary<Entity, StaticHandle> entityToStatic = [];
    private readonly Dictionary<StaticHandle, Entity> staticToEntity = [];

    /// <summary>
    /// Registers a mapping between an entity and a dynamic/kinematic body.
    /// </summary>
    /// <param name="entity">The ECS entity.</param>
    /// <param name="bodyHandle">The Bepu body handle.</param>
    public void RegisterBody(Entity entity, BodyHandle bodyHandle)
    {
        entityToBody[entity] = bodyHandle;
        bodyToEntity[bodyHandle] = entity;
    }

    /// <summary>
    /// Registers a mapping between an entity and a static body.
    /// </summary>
    /// <param name="entity">The ECS entity.</param>
    /// <param name="staticHandle">The Bepu static handle.</param>
    public void RegisterStatic(Entity entity, StaticHandle staticHandle)
    {
        entityToStatic[entity] = staticHandle;
        staticToEntity[staticHandle] = entity;
    }

    /// <summary>
    /// Removes all mappings for an entity.
    /// </summary>
    /// <param name="entity">The entity to unregister.</param>
    /// <returns>True if the entity was found and removed; false otherwise.</returns>
    public bool Unregister(Entity entity)
    {
        if (entityToBody.TryGetValue(entity, out var bodyHandle))
        {
            entityToBody.Remove(entity);
            bodyToEntity.Remove(bodyHandle);
            return true;
        }

        if (entityToStatic.TryGetValue(entity, out var staticHandle))
        {
            entityToStatic.Remove(entity);
            staticToEntity.Remove(staticHandle);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the body handle for an entity.
    /// </summary>
    /// <param name="entity">The entity to look up.</param>
    /// <param name="bodyHandle">The body handle if found.</param>
    /// <returns>True if found; false otherwise.</returns>
    public bool TryGetBody(Entity entity, out BodyHandle bodyHandle)
    {
        return entityToBody.TryGetValue(entity, out bodyHandle);
    }

    /// <summary>
    /// Gets the static handle for an entity.
    /// </summary>
    /// <param name="entity">The entity to look up.</param>
    /// <param name="staticHandle">The static handle if found.</param>
    /// <returns>True if found; false otherwise.</returns>
    public bool TryGetStatic(Entity entity, out StaticHandle staticHandle)
    {
        return entityToStatic.TryGetValue(entity, out staticHandle);
    }

    /// <summary>
    /// Gets the entity for a body handle.
    /// </summary>
    /// <param name="bodyHandle">The body handle to look up.</param>
    /// <param name="entity">The entity if found.</param>
    /// <returns>True if found; false otherwise.</returns>
    public bool TryGetEntity(BodyHandle bodyHandle, out Entity entity)
    {
        return bodyToEntity.TryGetValue(bodyHandle, out entity);
    }

    /// <summary>
    /// Gets the entity for a static handle.
    /// </summary>
    /// <param name="staticHandle">The static handle to look up.</param>
    /// <param name="entity">The entity if found.</param>
    /// <returns>True if found; false otherwise.</returns>
    public bool TryGetEntity(StaticHandle staticHandle, out Entity entity)
    {
        return staticToEntity.TryGetValue(staticHandle, out entity);
    }

    /// <summary>
    /// Checks if an entity has a physics body (dynamic or kinematic).
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has a body; false otherwise.</returns>
    public bool HasBody(Entity entity) => entityToBody.ContainsKey(entity);

    /// <summary>
    /// Checks if an entity has a static body.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has a static body; false otherwise.</returns>
    public bool HasStatic(Entity entity) => entityToStatic.ContainsKey(entity);

    /// <summary>
    /// Gets all entities with dynamic or kinematic bodies.
    /// </summary>
    public IEnumerable<Entity> DynamicEntities => entityToBody.Keys;

    /// <summary>
    /// Gets all entities with static bodies.
    /// </summary>
    public IEnumerable<Entity> StaticEntities => entityToStatic.Keys;

    /// <summary>
    /// Gets the total number of tracked bodies (dynamic + static).
    /// </summary>
    public int Count => entityToBody.Count + entityToStatic.Count;

    /// <summary>
    /// Clears all mappings.
    /// </summary>
    public void Clear()
    {
        entityToBody.Clear();
        bodyToEntity.Clear();
        entityToStatic.Clear();
        staticToEntity.Clear();
    }
}
