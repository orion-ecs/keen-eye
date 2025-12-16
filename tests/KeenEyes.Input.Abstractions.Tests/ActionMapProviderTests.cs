namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="ActionMapProvider"/> class.
/// </summary>
public class ActionMapProviderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_StartsWithEmptyMaps()
    {
        var provider = new ActionMapProvider();

        Assert.Empty(provider.ActionMaps);
    }

    [Fact]
    public void Constructor_ActiveMapIsNull()
    {
        var provider = new ActionMapProvider();

        Assert.Null(provider.ActiveMap);
    }

    #endregion

    #region AddActionMap Tests

    [Fact]
    public void AddActionMap_AddsToCollection()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay");

        provider.AddActionMap(map);

        Assert.Single(provider.ActionMaps);
        Assert.Contains(map, provider.ActionMaps);
    }

    [Fact]
    public void AddActionMap_NullMap_ThrowsArgumentNullException()
    {
        var provider = new ActionMapProvider();

        Assert.Throws<ArgumentNullException>(() => provider.AddActionMap(null!));
    }

    [Fact]
    public void AddActionMap_DuplicateName_ThrowsArgumentException()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));

        Assert.Throws<ArgumentException>(() => provider.AddActionMap(new InputActionMap("Gameplay")));
    }

    [Fact]
    public void AddActionMap_DuplicateName_CaseInsensitive_ThrowsArgumentException()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));

        Assert.Throws<ArgumentException>(() => provider.AddActionMap(new InputActionMap("GAMEPLAY")));
    }

    [Fact]
    public void AddActionMap_MultipleMaps_AllAdded()
    {
        var provider = new ActionMapProvider();
        var map1 = new InputActionMap("Gameplay");
        var map2 = new InputActionMap("Menu");

        provider.AddActionMap(map1);
        provider.AddActionMap(map2);

        Assert.Equal(2, provider.ActionMaps.Count);
    }

    #endregion

    #region RemoveActionMap Tests

    [Fact]
    public void RemoveActionMap_ExistingMap_ReturnsTrue()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));

        bool result = provider.RemoveActionMap("Gameplay");

        Assert.True(result);
    }

    [Fact]
    public void RemoveActionMap_ExistingMap_RemovesFromCollection()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));

        provider.RemoveActionMap("Gameplay");

        Assert.Empty(provider.ActionMaps);
    }

    [Fact]
    public void RemoveActionMap_NonExistentMap_ReturnsFalse()
    {
        var provider = new ActionMapProvider();

        bool result = provider.RemoveActionMap("Gameplay");

        Assert.False(result);
    }

    [Fact]
    public void RemoveActionMap_CaseInsensitive()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));

        bool result = provider.RemoveActionMap("GAMEPLAY");

        Assert.True(result);
    }

    [Fact]
    public void RemoveActionMap_ActiveMap_SetsActiveMapToNull()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));
        provider.SetActiveMap("Gameplay");

        provider.RemoveActionMap("Gameplay");

        Assert.Null(provider.ActiveMap);
    }

    #endregion

    #region SetActiveMap Tests

    [Fact]
    public void SetActiveMap_ExistingMap_SetsActiveMap()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay");
        provider.AddActionMap(map);

        provider.SetActiveMap("Gameplay");

        Assert.Same(map, provider.ActiveMap);
    }

    [Fact]
    public void SetActiveMap_EnablesTargetMap()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay") { Enabled = false };
        provider.AddActionMap(map);

        provider.SetActiveMap("Gameplay");

        Assert.True(map.Enabled);
    }

    [Fact]
    public void SetActiveMap_DisablesOtherMaps()
    {
        var provider = new ActionMapProvider();
        var map1 = new InputActionMap("Gameplay") { Enabled = true };
        var map2 = new InputActionMap("Menu") { Enabled = true };
        provider.AddActionMap(map1);
        provider.AddActionMap(map2);

        provider.SetActiveMap("Menu");

        Assert.False(map1.Enabled);
        Assert.True(map2.Enabled);
    }

    [Fact]
    public void SetActiveMap_NonExistentMap_ThrowsArgumentException()
    {
        var provider = new ActionMapProvider();

        Assert.Throws<ArgumentException>(() => provider.SetActiveMap("Gameplay"));
    }

    [Fact]
    public void SetActiveMap_NullName_DeactivatesAllMaps()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay") { Enabled = true };
        provider.AddActionMap(map);
        provider.SetActiveMap("Gameplay");

        provider.SetActiveMap(null);

        Assert.Null(provider.ActiveMap);
        Assert.False(map.Enabled);
    }

    [Fact]
    public void SetActiveMap_EmptyName_DeactivatesAllMaps()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay") { Enabled = true };
        provider.AddActionMap(map);
        provider.SetActiveMap("Gameplay");

        provider.SetActiveMap("");

        Assert.Null(provider.ActiveMap);
        Assert.False(map.Enabled);
    }

    [Fact]
    public void SetActiveMap_CaseInsensitive()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay");
        provider.AddActionMap(map);

        provider.SetActiveMap("GAMEPLAY");

        Assert.Same(map, provider.ActiveMap);
    }

    #endregion

    #region GetActionMap Tests

    [Fact]
    public void GetActionMap_ExistingMap_ReturnsMap()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay");
        provider.AddActionMap(map);

        var result = provider.GetActionMap("Gameplay");

        Assert.Same(map, result);
    }

    [Fact]
    public void GetActionMap_NonExistentMap_ReturnsNull()
    {
        var provider = new ActionMapProvider();

        var result = provider.GetActionMap("Gameplay");

        Assert.Null(result);
    }

    [Fact]
    public void GetActionMap_CaseInsensitive()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay");
        provider.AddActionMap(map);

        var result = provider.GetActionMap("GAMEPLAY");

        Assert.Same(map, result);
    }

    #endregion

    #region TryGetActionMap Tests

    [Fact]
    public void TryGetActionMap_ExistingMap_ReturnsTrue()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));

        bool result = provider.TryGetActionMap("Gameplay", out _);

        Assert.True(result);
    }

    [Fact]
    public void TryGetActionMap_ExistingMap_OutputsMap()
    {
        var provider = new ActionMapProvider();
        var map = new InputActionMap("Gameplay");
        provider.AddActionMap(map);

        provider.TryGetActionMap("Gameplay", out var result);

        Assert.Same(map, result);
    }

    [Fact]
    public void TryGetActionMap_NonExistentMap_ReturnsFalse()
    {
        var provider = new ActionMapProvider();

        bool result = provider.TryGetActionMap("Gameplay", out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetActionMap_NonExistentMap_OutputsNull()
    {
        var provider = new ActionMapProvider();

        provider.TryGetActionMap("Gameplay", out var result);

        Assert.Null(result);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllMaps()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));
        provider.AddActionMap(new InputActionMap("Menu"));

        provider.Clear();

        Assert.Empty(provider.ActionMaps);
    }

    [Fact]
    public void Clear_SetsActiveMapToNull()
    {
        var provider = new ActionMapProvider();
        provider.AddActionMap(new InputActionMap("Gameplay"));
        provider.SetActiveMap("Gameplay");

        provider.Clear();

        Assert.Null(provider.ActiveMap);
    }

    #endregion
}
