using System.Text.Json;

namespace KeenEyes.TestBridge.Mutation;

/// <summary>
/// Controller interface for world mutation operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides runtime entity and component manipulation capabilities
/// for debugging, testing, and tooling scenarios. Operations include entity spawning,
/// cloning, despawning, component manipulation, and tag management.
/// </para>
/// <para>
/// <strong>Warning:</strong> Mutation operations can affect game state and should be
/// used carefully. Consider disabling mutation in production builds.
/// </para>
/// </remarks>
public interface IMutationController
{
    #region Entity Management

    /// <summary>
    /// Spawns a new entity with optional name and components.
    /// </summary>
    /// <param name="name">Optional name for the entity.</param>
    /// <param name="components">Optional components to add to the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the created entity's ID and version on success.</returns>
    Task<EntityResult> SpawnAsync(
        string? name = null,
        IReadOnlyList<ComponentData>? components = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Despawns (destroys) an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to despawn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the entity was despawned; <c>false</c> if it didn't exist.</returns>
    Task<bool> DespawnAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones an existing entity, duplicating all its components and tags.
    /// </summary>
    /// <param name="entityId">The ID of the entity to clone.</param>
    /// <param name="name">Optional name for the cloned entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the cloned entity's ID and version on success.</returns>
    /// <remarks>
    /// Parent-child relationships are not copied. The cloned entity will have no parent.
    /// </remarks>
    Task<EntityResult> CloneAsync(
        int entityId,
        string? name = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the name of an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to rename.</param>
    /// <param name="name">The new name for the entity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the name was set; <c>false</c> if the entity doesn't exist or name is taken.</returns>
    Task<bool> SetNameAsync(int entityId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears (removes) the name from an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to clear the name from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the name was cleared; <c>false</c> if the entity doesn't exist.</returns>
    Task<bool> ClearNameAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion

    #region Hierarchy

    /// <summary>
    /// Sets or clears the parent of an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to reparent.</param>
    /// <param name="parentId">The parent entity ID, or null to clear the parent (make root).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the parent was set; <c>false</c> if the entity doesn't exist or operation failed.</returns>
    Task<bool> SetParentAsync(int entityId, int? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all root entities (entities without parents).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have no parent.</returns>
    Task<IReadOnlyList<int>> GetRootEntitiesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Components

    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to add the component to.</param>
    /// <param name="componentType">The component type name.</param>
    /// <param name="data">Optional JSON data for component fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the component was added; <c>false</c> if entity doesn't exist or already has component.</returns>
    Task<bool> AddComponentAsync(
        int entityId,
        string componentType,
        JsonElement? data = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to remove the component from.</param>
    /// <param name="componentType">The component type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the component was removed; <c>false</c> if entity or component doesn't exist.</returns>
    Task<bool> RemoveComponentAsync(
        int entityId,
        string componentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all fields of a component on an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="componentType">The component type name.</param>
    /// <param name="data">JSON data containing field values to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the component was updated; <c>false</c> if entity or component doesn't exist.</returns>
    Task<bool> SetComponentAsync(
        int entityId,
        string componentType,
        JsonElement data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a single field of a component on an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="componentType">The component type name.</param>
    /// <param name="fieldName">The name of the field to set.</param>
    /// <param name="value">The JSON value to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the field was set; <c>false</c> if entity, component, or field doesn't exist.</returns>
    Task<bool> SetFieldAsync(
        int entityId,
        string componentType,
        string fieldName,
        JsonElement value,
        CancellationToken cancellationToken = default);

    #endregion

    #region Tags

    /// <summary>
    /// Adds a string tag to an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to add the tag to.</param>
    /// <param name="tag">The tag to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the tag was added; <c>false</c> if entity doesn't exist or already has tag.</returns>
    Task<bool> AddTagAsync(int entityId, string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a string tag from an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to remove the tag from.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the tag was removed; <c>false</c> if entity doesn't exist or didn't have tag.</returns>
    Task<bool> RemoveTagAsync(int entityId, string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unique tags currently in use across all entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all unique tag names.</returns>
    Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default);

    #endregion
}
