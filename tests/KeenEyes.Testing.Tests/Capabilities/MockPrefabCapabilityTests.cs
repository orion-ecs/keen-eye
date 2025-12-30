using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockPrefabCapabilityTests
{
    #region Constructor

    [Fact]
    public void Constructor_Default_CreatesWithoutWorld()
    {
        var capability = new MockPrefabCapability();

        Assert.Empty(capability.SpawnLog);
        Assert.Empty(capability.RegistrationOrder);
    }

    [Fact]
    public void Constructor_WithWorld_CreatesWithWorld()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);

        Assert.Empty(capability.SpawnLog);
    }

    #endregion

    #region RegisterPrefab

    [Fact]
    public void RegisterPrefab_AddsPrefab()
    {
        var capability = new MockPrefabCapability();
        var prefab = new EntityPrefab();

        capability.RegisterPrefab("Enemy", prefab);

        Assert.True(capability.HasPrefab("Enemy"));
    }

    [Fact]
    public void RegisterPrefab_TracksRegistrationOrder()
    {
        var capability = new MockPrefabCapability();

        capability.RegisterPrefab("First", new EntityPrefab());
        capability.RegisterPrefab("Second", new EntityPrefab());
        capability.RegisterPrefab("Third", new EntityPrefab());

        Assert.Equal(3, capability.RegistrationOrder.Count);
        Assert.Equal("First", capability.RegistrationOrder[0]);
        Assert.Equal("Second", capability.RegistrationOrder[1]);
        Assert.Equal("Third", capability.RegistrationOrder[2]);
    }

    [Fact]
    public void RegisterPrefab_WithDuplicateName_ThrowsArgumentException()
    {
        var capability = new MockPrefabCapability();
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        var ex = Assert.Throws<ArgumentException>(() =>
            capability.RegisterPrefab("Enemy", new EntityPrefab()));
        Assert.Contains("Enemy", ex.Message);
    }

    [Fact]
    public void RegisterPrefab_WithNullName_ThrowsArgumentNullException()
    {
        var capability = new MockPrefabCapability();

        Assert.Throws<ArgumentNullException>(() =>
            capability.RegisterPrefab(null!, new EntityPrefab()));
    }

    [Fact]
    public void RegisterPrefab_WithNullPrefab_ThrowsArgumentNullException()
    {
        var capability = new MockPrefabCapability();

        Assert.Throws<ArgumentNullException>(() =>
            capability.RegisterPrefab("Test", null!));
    }

    #endregion

    #region HasPrefab

    [Fact]
    public void HasPrefab_WhenExists_ReturnsTrue()
    {
        var capability = new MockPrefabCapability();
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        Assert.True(capability.HasPrefab("Enemy"));
    }

    [Fact]
    public void HasPrefab_WhenNotExists_ReturnsFalse()
    {
        var capability = new MockPrefabCapability();

        Assert.False(capability.HasPrefab("Enemy"));
    }

    [Fact]
    public void HasPrefab_WithNullName_ThrowsArgumentNullException()
    {
        var capability = new MockPrefabCapability();

        Assert.Throws<ArgumentNullException>(() => capability.HasPrefab(null!));
    }

    #endregion

    #region UnregisterPrefab

    [Fact]
    public void UnregisterPrefab_WhenExists_ReturnsTrue()
    {
        var capability = new MockPrefabCapability();
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        var result = capability.UnregisterPrefab("Enemy");

        Assert.True(result);
        Assert.False(capability.HasPrefab("Enemy"));
    }

    [Fact]
    public void UnregisterPrefab_WhenNotExists_ReturnsFalse()
    {
        var capability = new MockPrefabCapability();

        var result = capability.UnregisterPrefab("Enemy");

        Assert.False(result);
    }

    [Fact]
    public void UnregisterPrefab_WithNullName_ThrowsArgumentNullException()
    {
        var capability = new MockPrefabCapability();

        Assert.Throws<ArgumentNullException>(() => capability.UnregisterPrefab(null!));
    }

    #endregion

    #region GetAllPrefabNames

    [Fact]
    public void GetAllPrefabNames_ReturnsAllNames()
    {
        var capability = new MockPrefabCapability();
        capability.RegisterPrefab("Enemy", new EntityPrefab());
        capability.RegisterPrefab("Player", new EntityPrefab());

        var names = capability.GetAllPrefabNames().ToList();

        Assert.Equal(2, names.Count);
        Assert.Contains("Enemy", names);
        Assert.Contains("Player", names);
    }

    [Fact]
    public void GetAllPrefabNames_WhenEmpty_ReturnsEmpty()
    {
        var capability = new MockPrefabCapability();

        var names = capability.GetAllPrefabNames();

        Assert.Empty(names);
    }

    #endregion

    #region GetPrefab

    [Fact]
    public void GetPrefab_WhenExists_ReturnsPrefab()
    {
        var capability = new MockPrefabCapability();
        var prefab = new EntityPrefab();
        capability.RegisterPrefab("Enemy", prefab);

        var retrieved = capability.GetPrefab("Enemy");

        Assert.Same(prefab, retrieved);
    }

    [Fact]
    public void GetPrefab_WhenNotExists_ReturnsNull()
    {
        var capability = new MockPrefabCapability();

        var prefab = capability.GetPrefab("Enemy");

        Assert.Null(prefab);
    }

    #endregion

    #region SpawnFromPrefab

    [Fact]
    public void SpawnFromPrefab_WithoutWorld_ThrowsInvalidOperationException()
    {
        var capability = new MockPrefabCapability();
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            capability.SpawnFromPrefab("Enemy"));
        Assert.Contains("World", ex.Message);
    }

    [Fact]
    public void SpawnFromPrefab_WithNonexistentPrefab_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            capability.SpawnFromPrefab("Unknown"));
        Assert.Contains("Unknown", ex.Message);
    }

    [Fact]
    public void SpawnFromPrefab_LogsSpawn()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        capability.SpawnFromPrefab("Enemy");

        Assert.Single(capability.SpawnLog);
        Assert.Equal("Enemy", capability.SpawnLog[0].PrefabName);
        Assert.Null(capability.SpawnLog[0].EntityName);
    }

    [Fact]
    public void SpawnFromPrefab_WithEntityName_LogsName()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        capability.SpawnFromPrefab("Enemy", "Boss");

        Assert.Single(capability.SpawnLog);
        Assert.Equal("Enemy", capability.SpawnLog[0].PrefabName);
        Assert.Equal("Boss", capability.SpawnLog[0].EntityName);
    }

    [Fact]
    public void SpawnFromPrefab_WithNullPrefabName_ThrowsArgumentNullException()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);

        Assert.Throws<ArgumentNullException>(() =>
            capability.SpawnFromPrefab(null!));
    }

    [Fact]
    public void SpawnFromPrefab_ReturnsEntityBuilder()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);
        capability.RegisterPrefab("Enemy", new EntityPrefab());

        var builder = capability.SpawnFromPrefab("Enemy");

        Assert.NotNull(builder);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllPrefabsAndLogs()
    {
        using var world = new World();
        var capability = new MockPrefabCapability(world);
        capability.RegisterPrefab("Enemy", new EntityPrefab());
        capability.SpawnFromPrefab("Enemy");

        capability.Clear();

        Assert.False(capability.HasPrefab("Enemy"));
        Assert.Empty(capability.SpawnLog);
        Assert.Empty(capability.RegistrationOrder);
    }

    #endregion
}
