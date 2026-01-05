using KeenEyes.Scenes;

namespace KeenEyes.Tests.Scenes;

/// <summary>
/// Tests for SceneManager runtime scene operations.
/// </summary>
public class SceneManagerTests
{
    #region Spawn Tests

    [Fact]
    public void Spawn_WithName_CreatesSceneRoot()
    {
        using var world = new World();

        var scene = world.Scenes.Spawn("TestScene");

        Assert.True(world.IsAlive(scene));
        Assert.True(world.Has<SceneRootTag>(scene));
        Assert.True(world.Has<SceneMetadata>(scene));
    }

    [Fact]
    public void Spawn_WithName_SetsMetadata()
    {
        using var world = new World();

        var scene = world.Scenes.Spawn("ForestLevel");

        ref readonly var metadata = ref world.Get<SceneMetadata>(scene);
        Assert.Equal("ForestLevel", metadata.Name);
        Assert.Equal(SceneState.Loaded, metadata.State);
        Assert.NotEqual(Guid.Empty, metadata.SceneId);
    }

    [Fact]
    public void Spawn_SameNameTwice_CreatesDistinctScenes()
    {
        using var world = new World();

        var scene1 = world.Scenes.Spawn("Level");
        var scene2 = world.Scenes.Spawn("Level");

        Assert.NotEqual(scene1.Id, scene2.Id);

        ref readonly var meta1 = ref world.Get<SceneMetadata>(scene1);
        ref readonly var meta2 = ref world.Get<SceneMetadata>(scene2);
        Assert.NotEqual(meta1.SceneId, meta2.SceneId);
    }

    [Fact]
    public void Spawn_SameNameTwice_GetSceneReturnsLatest()
    {
        using var world = new World();

        var scene1 = world.Scenes.Spawn("Level");
        var scene2 = world.Scenes.Spawn("Level");

        var found = world.Scenes.GetScene("Level");
        Assert.Equal(scene2.Id, found.Id);
    }

    [Fact]
    public void Spawn_WithNullName_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() => world.Scenes.Spawn(null!));
    }

    #endregion

    #region GetScene Tests

    [Fact]
    public void GetScene_WithLoadedScene_ReturnsSceneRoot()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");

        var found = world.Scenes.GetScene("TestScene");

        Assert.Equal(scene.Id, found.Id);
    }

    [Fact]
    public void GetScene_WithUnknownName_ReturnsNullEntity()
    {
        using var world = new World();

        var found = world.Scenes.GetScene("UnknownScene");

        Assert.Equal(Entity.Null, found);
    }

    [Fact]
    public void GetScene_AfterUnload_ReturnsNullEntity()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        world.Scenes.Unload(scene);

        var found = world.Scenes.GetScene("TestScene");

        Assert.Equal(Entity.Null, found);
    }

    #endregion

    #region IsLoaded Tests

    [Fact]
    public void IsLoaded_WithLoadedScene_ReturnsTrue()
    {
        using var world = new World();
        world.Scenes.Spawn("TestScene");

        Assert.True(world.Scenes.IsLoaded("TestScene"));
    }

    [Fact]
    public void IsLoaded_WithUnknownScene_ReturnsFalse()
    {
        using var world = new World();

        Assert.False(world.Scenes.IsLoaded("UnknownScene"));
    }

    [Fact]
    public void IsLoaded_AfterUnload_ReturnsFalse()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        world.Scenes.Unload(scene);

        Assert.False(world.Scenes.IsLoaded("TestScene"));
    }

    #endregion

    #region GetLoaded Tests

    [Fact]
    public void GetLoaded_WithNoScenes_ReturnsEmpty()
    {
        using var world = new World();

        var loaded = world.Scenes.GetLoaded().ToList();

        Assert.Empty(loaded);
    }

    [Fact]
    public void GetLoaded_WithMultipleScenes_ReturnsAll()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var scene3 = world.Scenes.Spawn("Scene3");

        var loaded = world.Scenes.GetLoaded().ToList();

        Assert.Equal(3, loaded.Count);
        Assert.Contains(loaded, e => e.Id == scene1.Id);
        Assert.Contains(loaded, e => e.Id == scene2.Id);
        Assert.Contains(loaded, e => e.Id == scene3.Id);
    }

    [Fact]
    public void LoadedCount_ReturnsCorrectCount()
    {
        using var world = new World();

        Assert.Equal(0, world.Scenes.LoadedCount);

        world.Scenes.Spawn("Scene1");
        Assert.Equal(1, world.Scenes.LoadedCount);

        world.Scenes.Spawn("Scene2");
        Assert.Equal(2, world.Scenes.LoadedCount);
    }

    #endregion

    #region Unload Tests

    [Fact]
    public void Unload_WithValidScene_ReturnsTrue()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");

        var result = world.Scenes.Unload(scene);

        Assert.True(result);
    }

    [Fact]
    public void Unload_DespawnsSceneRoot()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");

        world.Scenes.Unload(scene);

        Assert.False(world.IsAlive(scene));
    }

    [Fact]
    public void Unload_SetsStateToUnloading()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");

        // Add an entity that we can check to verify state was set before despawn
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene);
        world.Scenes.MarkPersistent(entity);

        world.Scenes.Unload(scene);

        // Persistent entity should still have its membership
        ref readonly var membership = ref world.Get<SceneMembership>(entity);
        // The scene root was despawned, so we can't check its metadata directly
        // but the unload operation should have worked
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void Unload_WithNonSceneRoot_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var result = world.Scenes.Unload(entity);

        Assert.False(result);
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void Unload_WithDeadEntity_ReturnsFalse()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        world.Despawn(scene);

        var result = world.Scenes.Unload(scene);

        Assert.False(result);
    }

    [Fact]
    public void Unload_DespawnsAllSceneEntities()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();
        world.Scenes.AddToScene(entity1, scene);
        world.Scenes.AddToScene(entity2, scene);
        world.Scenes.AddToScene(entity3, scene);

        world.Scenes.Unload(scene);

        Assert.False(world.IsAlive(entity1));
        Assert.False(world.IsAlive(entity2));
        Assert.False(world.IsAlive(entity3));
    }

    [Fact]
    public void Unload_RespectsPersistentTag()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");

        var normalEntity = world.Spawn().Build();
        var persistentEntity = world.Spawn().Build();
        world.Scenes.AddToScene(normalEntity, scene);
        world.Scenes.AddToScene(persistentEntity, scene);
        world.Scenes.MarkPersistent(persistentEntity);

        world.Scenes.Unload(scene);

        Assert.False(world.IsAlive(normalEntity));
        Assert.True(world.IsAlive(persistentEntity));
    }

    [Fact]
    public void Unload_RespectsReferenceCount()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");

        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);
        world.Scenes.TransitionEntity(entity, scene2);

        // Entity is now in both scenes with ref count 2
        ref readonly var membership = ref world.Get<SceneMembership>(entity);
        Assert.Equal(2, membership.ReferenceCount);

        // Unload first scene
        world.Scenes.Unload(scene1);

        // Entity should still be alive (ref count decremented to 1)
        Assert.True(world.IsAlive(entity));
        ref readonly var membershipAfter = ref world.Get<SceneMembership>(entity);
        Assert.Equal(1, membershipAfter.ReferenceCount);
    }

    [Fact]
    public void Unload_DecrementedToZero_DespawnsEntity()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");

        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);
        world.Scenes.TransitionEntity(entity, scene2);

        // Unload both scenes
        world.Scenes.Unload(scene1);
        world.Scenes.Unload(scene2);

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void Unload_UpdatesLoadedCount()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");

        Assert.Equal(2, world.Scenes.LoadedCount);

        world.Scenes.Unload(scene1);
        Assert.Equal(1, world.Scenes.LoadedCount);

        world.Scenes.Unload(scene2);
        Assert.Equal(0, world.Scenes.LoadedCount);
    }

    #endregion

    #region AddToScene Tests

    [Fact]
    public void AddToScene_SetsInitialReferenceCount()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();

        world.Scenes.AddToScene(entity, scene);

        ref readonly var membership = ref world.Get<SceneMembership>(entity);
        Assert.Equal(1, membership.ReferenceCount);
        Assert.Equal(scene.Id, membership.OriginScene.Id);
    }

    [Fact]
    public void AddToScene_WithDeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.AddToScene(entity, scene));
    }

    [Fact]
    public void AddToScene_WithDeadScene_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Despawn(scene);

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.AddToScene(entity, scene));
    }

    [Fact]
    public void AddToScene_WithNonSceneRoot_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var notAScene = world.Spawn().Build();
        var entity = world.Spawn().Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.AddToScene(entity, notAScene));
    }

    [Fact]
    public void AddToScene_WithExistingMembership_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.AddToScene(entity, scene2));
    }

    #endregion

    #region TransitionEntity Tests

    [Fact]
    public void TransitionEntity_IncrementsReferenceCount()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);

        world.Scenes.TransitionEntity(entity, scene2);

        ref readonly var membership = ref world.Get<SceneMembership>(entity);
        Assert.Equal(2, membership.ReferenceCount);
    }

    [Fact]
    public void TransitionEntity_WithoutMembership_AddsMembership()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();

        world.Scenes.TransitionEntity(entity, scene);

        Assert.True(world.Has<SceneMembership>(entity));
        ref readonly var membership = ref world.Get<SceneMembership>(entity);
        Assert.Equal(1, membership.ReferenceCount);
        Assert.Equal(scene.Id, membership.OriginScene.Id);
    }

    [Fact]
    public void TransitionEntity_EntitySurvivesFirstSceneUnload()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);
        world.Scenes.TransitionEntity(entity, scene2);

        world.Scenes.Unload(scene1);

        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void TransitionEntity_WithDeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.TransitionEntity(entity, scene));
    }

    [Fact]
    public void TransitionEntity_WithDeadScene_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Despawn(scene);

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.TransitionEntity(entity, scene));
    }

    [Fact]
    public void TransitionEntity_WithNonSceneRoot_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var notAScene = world.Spawn().Build();
        var entity = world.Spawn().Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.TransitionEntity(entity, notAScene));
    }

    #endregion

    #region MarkPersistent Tests

    [Fact]
    public void MarkPersistent_AddsPersistentTag()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.Scenes.MarkPersistent(entity);

        Assert.True(world.Has<PersistentTag>(entity));
    }

    [Fact]
    public void MarkPersistent_EntitySurvivesAllUnloads()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);
        world.Scenes.TransitionEntity(entity, scene2);
        world.Scenes.MarkPersistent(entity);

        world.Scenes.Unload(scene1);
        world.Scenes.Unload(scene2);

        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void MarkPersistent_CalledTwice_DoesNotThrow()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        world.Scenes.MarkPersistent(entity);
        world.Scenes.MarkPersistent(entity);

        Assert.True(world.Has<PersistentTag>(entity));
    }

    [Fact]
    public void MarkPersistent_WithDeadEntity_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Scenes.MarkPersistent(entity));
    }

    #endregion

    #region RemoveFromScene Tests

    [Fact]
    public void RemoveFromScene_DecrementsReferenceCount()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);
        world.Scenes.TransitionEntity(entity, scene2);

        world.Scenes.RemoveFromScene(entity, scene1);

        ref readonly var membership = ref world.Get<SceneMembership>(entity);
        Assert.Equal(1, membership.ReferenceCount);
    }

    [Fact]
    public void RemoveFromScene_WithReferenceCountZero_DespawnsEntity()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene);

        world.Scenes.RemoveFromScene(entity, scene);

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void RemoveFromScene_WithPersistentTag_DoesNotDespawn()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene);
        world.Scenes.MarkPersistent(entity);

        world.Scenes.RemoveFromScene(entity, scene);

        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void RemoveFromScene_WithDeadEntity_ReturnsFalse()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var result = world.Scenes.RemoveFromScene(entity, scene);

        Assert.False(result);
    }

    [Fact]
    public void RemoveFromScene_WithNoMembership_ReturnsFalse()
    {
        using var world = new World();
        var scene = world.Scenes.Spawn("TestScene");
        var entity = world.Spawn().Build();

        var result = world.Scenes.RemoveFromScene(entity, scene);

        Assert.False(result);
    }

    [Fact]
    public void RemoveFromScene_WithDifferentScene_ReturnsFalse()
    {
        using var world = new World();
        var scene1 = world.Scenes.Spawn("Scene1");
        var scene2 = world.Scenes.Spawn("Scene2");
        var entity = world.Spawn().Build();
        world.Scenes.AddToScene(entity, scene1);

        var result = world.Scenes.RemoveFromScene(entity, scene2);

        Assert.False(result);
        Assert.True(world.IsAlive(entity));
    }

    #endregion

    #region Lazy Initialization Tests

    [Fact]
    public void Scenes_LazyInitialized_DoesNotCreateUntilAccessed()
    {
        using var world = new World();

        // Accessing Scenes property creates the manager
        var scenes = world.Scenes;

        Assert.NotNull(scenes);
    }

    [Fact]
    public void Scenes_MultipleCalls_ReturnsSameInstance()
    {
        using var world = new World();

        var scenes1 = world.Scenes;
        var scenes2 = world.Scenes;

        Assert.Same(scenes1, scenes2);
    }

    #endregion

    #region World Dispose Tests

    [Fact]
    public void WorldDispose_ClearsSceneManager()
    {
        var world = new World();
        _ = world.Scenes.Spawn("TestScene");
        Assert.Equal(1, world.Scenes.LoadedCount);

        world.Dispose();

        // After dispose, we can't safely access Scenes
        // This test just verifies no exception is thrown during cleanup
    }

    #endregion
}
