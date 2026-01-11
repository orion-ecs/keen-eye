using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the LodSystem.
/// </summary>
public class LodSystemTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_WithWorld_DoesNotThrow()
    {
        world = new World();
        var system = new LodSystem();

        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void Update_WithNoCamera_DoesNotCrash()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create entity with LOD but no camera
        world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f)))
            .Build();

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        var system = new LodSystem();

        Assert.True(system.Enabled);
    }

    [Fact]
    public void HysteresisFactor_DefaultsToFivePercent()
    {
        var system = new LodSystem();

        Assert.Equal(0.05f, system.HysteresisFactor);
    }

    [Fact]
    public void GlobalBias_DefaultsToZero()
    {
        var system = new LodSystem();

        Assert.Equal(0f, system.GlobalBias);
    }

    #endregion

    #region Distance-Based LOD Selection Tests

    [Fact]
    public void Update_EntityAtCameraPosition_SelectsLOD0()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity at same position
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(0, lodGroup.CurrentLevel);
    }

    [Fact]
    public void Update_EntityNearCamera_SelectsHighDetailLOD()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity 5 units away (within first threshold of 10)
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(0, lodGroup.CurrentLevel);
    }

    [Fact]
    public void Update_EntityFarFromCamera_SelectsLowerDetailLOD()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity 15 units away (past first threshold of 10, within second of 20)
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(1, lodGroup.CurrentLevel);
    }

    [Fact]
    public void Update_EntityVeryFar_SelectsLowestDetailLOD()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity 50 units away (past all thresholds)
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(50, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(2, lodGroup.CurrentLevel);
    }

    [Fact]
    public void Update_LODChange_UpdatesRenderableMeshId()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity far away (should select LOD1)
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(100, 1)) // Start with mesh 100
            .With(LodGroup.Create(
                new LodLevel(100, 0f),   // High detail mesh 100
                new LodLevel(200, 10f),  // Medium detail mesh 200
                new LodLevel(300, 20f))) // Low detail mesh 300
            .Build();

        system.Update(1f / 60f);

        ref readonly var renderable = ref world.Get<Renderable>(entity);
        Assert.Equal(200, renderable.MeshId); // Should have switched to medium detail mesh
    }

    #endregion

    #region MainCameraTag Tests

    [Fact]
    public void Update_WithMainCameraTag_PrefersTaggedCamera()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera without tag at position (100, 0, 0)
        world.Spawn()
            .With(new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create main camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .WithTag<MainCameraTag>()
            .Build();

        // Create entity at (5, 0, 0) - close to main camera, far from other camera
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        // If using main camera at origin: distance is 5, should be LOD0
        // If using other camera at (100,0,0): distance is 95, would be LOD1
        Assert.Equal(0, lodGroup.CurrentLevel);
    }

    [Fact]
    public void Update_WithoutMainCameraTag_UsesFirstCamera()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin (no tag)
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity 5 units away
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(0, lodGroup.CurrentLevel);
    }

    #endregion

    #region Single Level LOD Tests

    [Fact]
    public void Update_SingleLevelLOD_AlwaysReturnsLevel0()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity very far away but with only 1 LOD level
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(1000, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(new LodLevel(1, 0f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(0, lodGroup.CurrentLevel);
    }

    #endregion

    #region Bias Tests

    [Fact]
    public void Update_PositiveGlobalBias_PrefersHigherDetail()
    {
        world = new World();
        var system = new LodSystem { GlobalBias = 10f }; // 10 unit bias towards higher detail
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity at 15 units (normally LOD1, but bias brings effective distance to 5)
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(0, lodGroup.CurrentLevel); // Bias should keep it at LOD0
    }

    [Fact]
    public void Update_NegativeGlobalBias_PrefersLowerDetail()
    {
        world = new World();
        var system = new LodSystem { GlobalBias = -10f }; // 10 unit bias towards lower detail
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity at 5 units (normally LOD0, but bias brings effective distance to 15)
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(1, lodGroup.CurrentLevel); // Bias should push it to LOD1
    }

    [Fact]
    public void Update_PerEntityBias_AppliesCorrectly()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity at 15 units with positive bias
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f));
        lodGroup.Bias = 10f; // Bias towards higher detail

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(lodGroup)
            .Build();

        system.Update(1f / 60f);

        ref readonly var resultLodGroup = ref world.Get<LodGroup>(entity);
        Assert.Equal(0, resultLodGroup.CurrentLevel); // Per-entity bias should keep it at LOD0
    }

    #endregion

    #region Multiple Entities Tests

    [Fact]
    public void Update_MultipleEntities_AllProcessed()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entities at different distances
        var entityNear = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        var entityMedium = world.Spawn()
            .With(new Transform3D(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        var entityFar = world.Spawn()
            .With(new Transform3D(new Vector3(50, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Renderable(1, 1))
            .With(LodGroup.Create(
                new LodLevel(1, 0f),
                new LodLevel(2, 10f),
                new LodLevel(3, 20f)))
            .Build();

        system.Update(1f / 60f);

        ref readonly var lodNear = ref world.Get<LodGroup>(entityNear);
        ref readonly var lodMedium = ref world.Get<LodGroup>(entityMedium);
        ref readonly var lodFar = ref world.Get<LodGroup>(entityFar);

        Assert.Equal(0, lodNear.CurrentLevel);
        Assert.Equal(1, lodMedium.CurrentLevel);
        Assert.Equal(2, lodFar.CurrentLevel);
    }

    #endregion

    #region Screen-Size Mode Tests

    [Fact]
    public void Update_ScreenSizeMode_NearEntitySelectsHighDetail()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create perspective camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity with screen-size mode, very close (large screen coverage)
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),      // Highest detail when screen size >= 0.5
            new LodLevel(2, 0.5f),    // Medium detail when screen size >= 0.2
            new LodLevel(3, 0.2f));   // Low detail when screen size >= 0.1
        lodGroup.SelectionMode = LodSelectionMode.ScreenSize;
        lodGroup.BoundingSphereRadius = 1f;

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 2), Quaternion.Identity, Vector3.One)) // Very close
            .With(new Renderable(1, 1))
            .With(lodGroup)
            .Build();

        system.Update(1f / 60f);

        ref readonly var resultLodGroup = ref world.Get<LodGroup>(entity);
        // Very close = large screen size, should select LOD0
        Assert.Equal(0, resultLodGroup.CurrentLevel);
    }

    [Fact]
    public void Update_ScreenSizeMode_FarEntitySelectsLowDetail()
    {
        world = new World();
        var system = new LodSystem();
        world.AddSystem(system);

        // Create perspective camera at origin
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(Camera.CreatePerspective(60f, 16f / 9f, 0.1f, 1000f))
            .Build();

        // Create entity with screen-size mode, very far (small screen coverage)
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),      // Highest detail
            new LodLevel(2, 0.5f),    // Medium
            new LodLevel(3, 0.1f));   // Low
        lodGroup.SelectionMode = LodSelectionMode.ScreenSize;
        lodGroup.BoundingSphereRadius = 1f;

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 100), Quaternion.Identity, Vector3.One)) // Very far
            .With(new Renderable(1, 1))
            .With(lodGroup)
            .Build();

        system.Update(1f / 60f);

        ref readonly var resultLodGroup = ref world.Get<LodGroup>(entity);
        // Very far = small screen size, should select lower LOD
        Assert.True(resultLodGroup.CurrentLevel >= 1);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var system = new LodSystem();

        // Should not throw
        system.Dispose();
    }

    #endregion
}
