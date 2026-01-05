using KeenEyes.Scenes;

namespace KeenEyes;

/// <summary>
/// Manages scene spawning, unloading, and entity scene membership.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles scene lifecycle operations.
/// The public API is exposed through <see cref="World.Scenes"/>.
/// </para>
/// <para>
/// Scenes are entity hierarchies with a root entity marked by <see cref="SceneRootTag"/>.
/// Entities can belong to multiple scenes through reference counting via
/// <see cref="SceneMembership.ReferenceCount"/>.
/// </para>
/// <para>
/// This class is thread-safe: all operations can be called concurrently
/// from multiple threads.
/// </para>
/// </remarks>
public sealed class SceneManager
{
    private readonly Lock syncRoot = new();
    private readonly World world;

    // sceneName -> sceneRootEntity (most recent instance with that name)
    private readonly Dictionary<string, Entity> loadedScenesByName = [];

    // sceneRootId -> sceneName (reverse lookup for Unload)
    private readonly Dictionary<int, string> sceneNamesByRootId = [];

    // All loaded scene root entities (supports multiple instances of same scene)
    private readonly HashSet<int> allLoadedSceneRootIds = [];

    // sceneRootId -> set of entity IDs that belong to this scene
    private readonly Dictionary<int, HashSet<int>> sceneEntityClaims = [];

    /// <summary>
    /// Creates a new scene manager for the specified world.
    /// </summary>
    /// <param name="world">The world that owns this scene manager.</param>
    internal SceneManager(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// Spawns a new scene instance with the given name.
    /// </summary>
    /// <param name="name">The name of the scene to spawn.</param>
    /// <returns>The root entity of the spawned scene.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Creates a new scene root entity with <see cref="SceneRootTag"/> and
    /// <see cref="SceneMetadata"/>. Multiple instances of the same scene name
    /// can be spawned; each gets a unique <see cref="SceneMetadata.SceneId"/>.
    /// </para>
    /// <para>
    /// The most recently spawned instance with a given name is returned by
    /// <see cref="GetScene(string)"/>.
    /// </para>
    /// </remarks>
    public Entity Spawn(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var sceneId = Guid.NewGuid();

        // Create the scene root entity with a unique name including the GUID
        var root = world.Spawn($"Scene:{name}:{sceneId:N}")
            .WithTag<SceneRootTag>()
            .With(new SceneMetadata
            {
                Name = name,
                SceneId = sceneId,
                State = SceneState.Loaded
            })
            .Build();

        lock (syncRoot)
        {
            loadedScenesByName[name] = root;
            sceneNamesByRootId[root.Id] = name;
            allLoadedSceneRootIds.Add(root.Id);
            sceneEntityClaims[root.Id] = [];
        }

        return root;
    }

    /// <summary>
    /// Unloads a scene and despawns its entities.
    /// </summary>
    /// <param name="sceneRoot">The root entity of the scene to unload.</param>
    /// <returns><c>true</c> if the scene was unloaded; <c>false</c> if it was not a valid scene root.</returns>
    /// <remarks>
    /// <para>
    /// For each entity with <see cref="SceneMembership"/> referencing this scene:
    /// </para>
    /// <list type="bullet">
    /// <item>If the entity has <see cref="PersistentTag"/>, it is skipped (never despawned).</item>
    /// <item>Otherwise, <see cref="SceneMembership.ReferenceCount"/> is decremented.</item>
    /// <item>If the reference count reaches zero, the entity and its descendants are despawned.</item>
    /// </list>
    /// <para>
    /// The scene root entity itself is always despawned.
    /// </para>
    /// </remarks>
    public bool Unload(Entity sceneRoot)
    {
        if (!world.IsAlive(sceneRoot))
        {
            return false;
        }

        if (!world.Has<SceneRootTag>(sceneRoot))
        {
            return false;
        }

        // Set state to Unloading
        ref var metadata = ref world.Get<SceneMetadata>(sceneRoot);
        metadata.State = SceneState.Unloading;

        // Get claimed entities for this scene
        int[] claimedEntityIds;
        lock (syncRoot)
        {
            if (sceneEntityClaims.TryGetValue(sceneRoot.Id, out var claims))
            {
                claimedEntityIds = [.. claims];
            }
            else
            {
                claimedEntityIds = [];
            }
        }

        // Process each claimed entity
        foreach (var entityId in claimedEntityIds)
        {
            var version = world.EntityPool.GetVersion(entityId);
            if (version < 0)
            {
                continue;
            }

            var entity = new Entity(entityId, version);
            if (!world.IsAlive(entity))
            {
                continue;
            }

            if (!world.Has<SceneMembership>(entity))
            {
                continue;
            }

            // Skip persistent entities
            if (world.Has<PersistentTag>(entity))
            {
                continue;
            }

            // Decrement reference count
            ref var membership = ref world.Get<SceneMembership>(entity);
            membership.ReferenceCount--;

            // Despawn if reference count reaches zero
            if (membership.ReferenceCount <= 0)
            {
                world.DespawnRecursive(entity);
            }
        }

        // Remove from tracking
        lock (syncRoot)
        {
            if (sceneNamesByRootId.TryGetValue(sceneRoot.Id, out var sceneName))
            {
                sceneNamesByRootId.Remove(sceneRoot.Id);

                // Only remove from loadedScenesByName if this is the current instance
                if (loadedScenesByName.TryGetValue(sceneName, out var currentRoot) &&
                    currentRoot.Id == sceneRoot.Id)
                {
                    loadedScenesByName.Remove(sceneName);
                }
            }
            allLoadedSceneRootIds.Remove(sceneRoot.Id);
            sceneEntityClaims.Remove(sceneRoot.Id);
        }

        // Despawn the scene root and its children
        world.DespawnRecursive(sceneRoot);

        return true;
    }

    /// <summary>
    /// Marks an entity as persistent across scene unloads.
    /// </summary>
    /// <param name="entity">The entity to mark as persistent.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity is not alive.</exception>
    /// <remarks>
    /// Entities with <see cref="PersistentTag"/> are never despawned by
    /// <see cref="Unload(Entity)"/> operations, regardless of their
    /// <see cref="SceneMembership.ReferenceCount"/>.
    /// </remarks>
    public void MarkPersistent(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (!world.Has<PersistentTag>(entity))
        {
            world.Add(entity, new PersistentTag());
        }
    }

    /// <summary>
    /// Transitions an entity to another scene, incrementing its reference count.
    /// </summary>
    /// <param name="entity">The entity to transition.</param>
    /// <param name="toScene">The scene root to transition to.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either entity is not alive or when <paramref name="toScene"/>
    /// is not a valid scene root.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If the entity already has <see cref="SceneMembership"/>, its
    /// <see cref="SceneMembership.ReferenceCount"/> is incremented.
    /// </para>
    /// <para>
    /// If the entity does not have <see cref="SceneMembership"/>, it is added
    /// with the target scene as origin and reference count of 1.
    /// </para>
    /// <para>
    /// Use this when an entity needs to exist in multiple scenes simultaneously
    /// (e.g., an NPC following the player between areas).
    /// </para>
    /// </remarks>
    public void TransitionEntity(Entity entity, Entity toScene)
    {
        if (!world.IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (!world.IsAlive(toScene))
        {
            throw new InvalidOperationException($"Scene root entity {toScene} is not alive.");
        }

        if (!world.Has<SceneRootTag>(toScene))
        {
            throw new InvalidOperationException($"Entity {toScene} is not a scene root.");
        }

        if (world.Has<SceneMembership>(entity))
        {
            // Increment reference count
            ref var membership = ref world.Get<SceneMembership>(entity);
            membership.ReferenceCount++;
        }
        else
        {
            // Add new membership
            world.Add(entity, new SceneMembership
            {
                OriginScene = toScene,
                ReferenceCount = 1
            });
        }

        // Register the claim for the target scene
        lock (syncRoot)
        {
            if (sceneEntityClaims.TryGetValue(toScene.Id, out var claims))
            {
                claims.Add(entity.Id);
            }
        }
    }

    /// <summary>
    /// Adds an entity to a scene with an initial reference count of 1.
    /// </summary>
    /// <param name="entity">The entity to add to the scene.</param>
    /// <param name="scene">The scene root to add the entity to.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either entity is not alive, when <paramref name="scene"/>
    /// is not a valid scene root, or when the entity already has scene membership.
    /// </exception>
    /// <remarks>
    /// Use this to associate an entity with a scene for the first time.
    /// For entities that need to be in multiple scenes, use
    /// <see cref="TransitionEntity(Entity, Entity)"/> after the initial add.
    /// </remarks>
    public void AddToScene(Entity entity, Entity scene)
    {
        if (!world.IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity} is not alive.");
        }

        if (!world.IsAlive(scene))
        {
            throw new InvalidOperationException($"Scene root entity {scene} is not alive.");
        }

        if (!world.Has<SceneRootTag>(scene))
        {
            throw new InvalidOperationException($"Entity {scene} is not a scene root.");
        }

        if (world.Has<SceneMembership>(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity} already has scene membership. Use TransitionEntity to add to additional scenes.");
        }

        world.Add(entity, new SceneMembership
        {
            OriginScene = scene,
            ReferenceCount = 1
        });

        // Register the claim
        lock (syncRoot)
        {
            if (sceneEntityClaims.TryGetValue(scene.Id, out var claims))
            {
                claims.Add(entity.Id);
            }
        }
    }

    /// <summary>
    /// Removes an entity from a scene by decrementing its reference count.
    /// </summary>
    /// <param name="entity">The entity to remove from the scene.</param>
    /// <param name="scene">The scene to remove the entity from.</param>
    /// <returns>
    /// <c>true</c> if the entity was removed; <c>false</c> if the entity is not alive,
    /// has no scene membership, or is not in the specified scene.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the reference count reaches zero and the entity does not have
    /// <see cref="PersistentTag"/>, the entity and its descendants are despawned.
    /// </para>
    /// </remarks>
    public bool RemoveFromScene(Entity entity, Entity scene)
    {
        if (!world.IsAlive(entity))
        {
            return false;
        }

        if (!world.Has<SceneMembership>(entity))
        {
            return false;
        }

        ref var membership = ref world.Get<SceneMembership>(entity);
        if (membership.OriginScene.Id != scene.Id)
        {
            return false;
        }

        membership.ReferenceCount--;

        // Despawn if reference count reaches zero and not persistent
        if (membership.ReferenceCount <= 0 && !world.Has<PersistentTag>(entity))
        {
            world.DespawnRecursive(entity);
        }

        return true;
    }

    /// <summary>
    /// Gets all currently loaded scene root entities.
    /// </summary>
    /// <returns>An enumerable of all loaded scene root entities.</returns>
    /// <remarks>
    /// Returns a snapshot to allow safe iteration while scenes may be loaded/unloaded.
    /// </remarks>
    public IEnumerable<Entity> GetLoaded()
    {
        int[] rootIds;
        lock (syncRoot)
        {
            rootIds = [.. allLoadedSceneRootIds];
        }

        return GetLoadedCore(rootIds);
    }

    private IEnumerable<Entity> GetLoadedCore(int[] rootIds)
    {
        foreach (var rootId in rootIds)
        {
            var version = world.EntityPool.GetVersion(rootId);
            if (version < 0)
            {
                continue;
            }

            var entity = new Entity(rootId, version);
            if (world.IsAlive(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets a loaded scene by name.
    /// </summary>
    /// <param name="name">The name of the scene to find.</param>
    /// <returns>
    /// The scene root entity, or <see cref="Entity.Null"/> if no scene with
    /// that name is currently loaded.
    /// </returns>
    /// <remarks>
    /// If multiple instances of a scene with the same name are loaded,
    /// returns the most recently spawned instance.
    /// </remarks>
    public Entity GetScene(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Entity root;
        lock (syncRoot)
        {
            if (!loadedScenesByName.TryGetValue(name, out root))
            {
                return Entity.Null;
            }
        }

        // Verify the scene is still alive
        if (!world.IsAlive(root))
        {
            return Entity.Null;
        }

        return root;
    }

    /// <summary>
    /// Checks if a scene with the given name is currently loaded.
    /// </summary>
    /// <param name="name">The name of the scene to check.</param>
    /// <returns><c>true</c> if the scene is loaded; otherwise, <c>false</c>.</returns>
    public bool IsLoaded(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return GetScene(name).IsValid;
    }

    /// <summary>
    /// Gets the number of currently loaded scenes.
    /// </summary>
    public int LoadedCount
    {
        get
        {
            lock (syncRoot)
            {
                return allLoadedSceneRootIds.Count;
            }
        }
    }

    /// <summary>
    /// Clears all scene tracking data.
    /// </summary>
    /// <remarks>
    /// Called during <see cref="World.Dispose"/>. Does not despawn entities;
    /// those are handled by the world's normal cleanup.
    /// </remarks>
    internal void Clear()
    {
        lock (syncRoot)
        {
            loadedScenesByName.Clear();
            sceneNamesByRootId.Clear();
            allLoadedSceneRootIds.Clear();
            sceneEntityClaims.Clear();
        }
    }
}
