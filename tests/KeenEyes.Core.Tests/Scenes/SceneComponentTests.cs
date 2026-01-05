using KeenEyes.Scenes;

namespace KeenEyes.Tests.Scenes;

/// <summary>
/// Tests for scene-related components and types from KeenEyes.Abstractions.
/// </summary>
public class SceneComponentTests
{
    #region SceneRootTag Tests

    [Fact]
    public void SceneRootTag_IsTagComponent()
    {
        var tag = new SceneRootTag();

        Assert.IsAssignableFrom<ITagComponent>(tag);
    }

    #endregion

    #region PersistentTag Tests

    [Fact]
    public void PersistentTag_IsTagComponent()
    {
        var tag = new PersistentTag();

        Assert.IsAssignableFrom<ITagComponent>(tag);
    }

    #endregion

    #region SceneState Tests

    [Fact]
    public void SceneState_HasLoadedValue()
    {
        var state = SceneState.Loaded;

        Assert.Equal(0, (int)state);
    }

    [Fact]
    public void SceneState_HasUnloadingValue()
    {
        var state = SceneState.Unloading;

        Assert.Equal(1, (int)state);
    }

    #endregion

    #region SceneMetadata Tests

    [Fact]
    public void SceneMetadata_IsComponent()
    {
        var metadata = new SceneMetadata { Name = "TestScene" };

        Assert.IsAssignableFrom<IComponent>(metadata);
    }

    [Fact]
    public void SceneMetadata_StoresName()
    {
        var metadata = new SceneMetadata { Name = "ForestLevel" };

        Assert.Equal("ForestLevel", metadata.Name);
    }

    [Fact]
    public void SceneMetadata_StoresSceneId()
    {
        var id = Guid.NewGuid();
        var metadata = new SceneMetadata { Name = "Test", SceneId = id };

        Assert.Equal(id, metadata.SceneId);
    }

    [Fact]
    public void SceneMetadata_StoresState()
    {
        var metadata = new SceneMetadata
        {
            Name = "Test",
            State = SceneState.Unloading
        };

        Assert.Equal(SceneState.Unloading, metadata.State);
    }

    [Fact]
    public void SceneMetadata_DefaultState_IsLoaded()
    {
        var metadata = new SceneMetadata { Name = "Test" };

        Assert.Equal(SceneState.Loaded, metadata.State);
    }

    #endregion

    #region SceneMembership Tests

    [Fact]
    public void SceneMembership_IsComponent()
    {
        var membership = new SceneMembership();

        Assert.IsAssignableFrom<IComponent>(membership);
    }

    [Fact]
    public void SceneMembership_StoresOriginScene()
    {
        var sceneRoot = new Entity(42, 1);
        var membership = new SceneMembership { OriginScene = sceneRoot };

        Assert.Equal(sceneRoot, membership.OriginScene);
    }

    [Fact]
    public void SceneMembership_StoresReferenceCount()
    {
        var membership = new SceneMembership { ReferenceCount = 3 };

        Assert.Equal(3, membership.ReferenceCount);
    }

    [Fact]
    public void SceneMembership_DefaultReferenceCount_IsZero()
    {
        var membership = new SceneMembership();

        Assert.Equal(0, membership.ReferenceCount);
    }

    [Fact]
    public void SceneMembership_DefaultOriginScene_IsDefaultEntity()
    {
        var membership = new SceneMembership();

        // Default struct initialization gives (0, 0)
        Assert.Equal(default, membership.OriginScene);
    }

    #endregion
}
