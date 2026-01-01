using KeenEyes.Editor.Layout;

namespace KeenEyes.Editor.Tests.Layout;

public class LayoutManagerTests
{
    #region EditorLayout Tests

    [Fact]
    public void CreateDefault_ReturnsValidLayout()
    {
        var layout = EditorLayout.CreateDefault();

        Assert.Equal("Default", layout.Name);
        Assert.Equal(EditorLayout.CurrentVersion, layout.Version);
        Assert.NotNull(layout.Window);
        Assert.NotNull(layout.DockLayout);
        Assert.NotEmpty(layout.Panels);
    }

    [Fact]
    public void CreateDefault_HasExpectedWindowState()
    {
        var layout = EditorLayout.CreateDefault();

        Assert.Equal(1600, layout.Window.Width);
        Assert.Equal(900, layout.Window.Height);
        Assert.False(layout.Window.Maximized);
    }

    [Fact]
    public void CreateDefault_HasExpectedPanels()
    {
        var layout = EditorLayout.CreateDefault();

        Assert.True(layout.Panels.ContainsKey("hierarchy"));
        Assert.True(layout.Panels.ContainsKey("inspector"));
        Assert.True(layout.Panels.ContainsKey("viewport"));
        Assert.True(layout.Panels.ContainsKey("console"));
    }

    [Fact]
    public void CreateTall_ReturnsValidLayout()
    {
        var layout = EditorLayout.CreateTall();

        Assert.Equal("Tall", layout.Name);
        Assert.Equal(1200, layout.Window.Width);
        Assert.Equal(1000, layout.Window.Height);
    }

    [Fact]
    public void CreateWide_ReturnsValidLayout()
    {
        var layout = EditorLayout.CreateWide();

        Assert.Equal("Wide", layout.Name);
        Assert.Equal(1920, layout.Window.Width);
        Assert.Equal(800, layout.Window.Height);
    }

    [Fact]
    public void Create2Column_ReturnsValidLayout()
    {
        var layout = EditorLayout.Create2Column();

        Assert.Equal("2-Column", layout.Name);
    }

    [Fact]
    public void Create3Column_ReturnsValidLayout()
    {
        var layout = EditorLayout.Create3Column();

        Assert.Equal("3-Column", layout.Name);
    }

    [Fact]
    public void Create4Column_ReturnsValidLayout()
    {
        var layout = EditorLayout.Create4Column();

        Assert.Equal("4-Column", layout.Name);
        Assert.True(layout.Window.Maximized);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void ToJson_ReturnsValidJson()
    {
        var layout = EditorLayout.CreateDefault();

        var json = layout.ToJson();

        Assert.NotEmpty(json);
        Assert.Contains("\"name\":", json);
        Assert.Contains("\"version\":", json);
        Assert.Contains("\"window\":", json);
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsLayout()
    {
        var original = EditorLayout.CreateDefault();
        var json = original.ToJson();

        var restored = EditorLayout.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.Window.Width, restored.Window.Width);
        Assert.Equal(original.Window.Height, restored.Window.Height);
    }

    [Fact]
    public void FromJson_WithInvalidJson_ReturnsNull()
    {
        var result = EditorLayout.FromJson("not valid json");

        Assert.Null(result);
    }

    [Fact]
    public void FromJson_WithEmptyJson_ReturnsNull()
    {
        var result = EditorLayout.FromJson("");

        Assert.Null(result);
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var original = EditorLayout.CreateDefault();
        original.Name = "CustomTest";
        original.Window.X = 200;
        original.Window.Y = 150;
        original.Panels["hierarchy"]!.Collapsed = true;

        var json = original.ToJson();
        var restored = EditorLayout.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal("CustomTest", restored.Name);
        Assert.Equal(200, restored.Window.X);
        Assert.Equal(150, restored.Window.Y);
        Assert.True(restored.Panels["hierarchy"].Collapsed);
    }

    [Fact]
    public void RoundTrip_PreservesDockLayout()
    {
        var original = EditorLayout.CreateDefault();

        var json = original.ToJson();
        var restored = EditorLayout.FromJson(json);

        Assert.NotNull(restored?.DockLayout);
        Assert.Equal(original.DockLayout!.Type, restored.DockLayout.Type);
        Assert.Equal(original.DockLayout.Direction, restored.DockLayout.Direction);
    }

    #endregion

    #region LayoutManager Persistence Tests

    [Fact]
    public void Save_CreatesFile()
    {
        var path = GetTestLayoutPath();

        try
        {
            var manager = CreateTestManager();
            manager.Save(path);

            Assert.True(File.Exists(path));
        }
        finally
        {
            CleanupTestFile(path);
        }
    }

    [Fact]
    public void SaveAndLoad_Roundtrips()
    {
        var path = GetTestLayoutPath();

        try
        {
            var manager = CreateTestManager();
            manager.ApplyPreset(LayoutPreset.Tall);
            manager.Save(path);

            // Manually modify state to verify Load restores saved data
            // (can't create a new LayoutManager since it's a singleton)
            manager.CurrentLayout.Name = "ModifiedToVerifyLoad";

            // Load should restore the saved layout
            manager.Load(path);

            Assert.Equal("Tall", manager.CurrentLayout.Name);
            Assert.Equal(1000, manager.CurrentLayout.Window.Height);
        }
        finally
        {
            CleanupTestFile(path);
        }
    }

    [Fact]
    public void Load_NonExistentFile_UsesDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");

        var manager = CreateTestManager();
        manager.Load(path);

        Assert.True(manager.IsLoaded);
        Assert.Equal("Default", manager.CurrentLayout.Name);
    }

    [Fact]
    public void ApplyPreset_ChangesLayout()
    {
        var manager = CreateTestManager();

        manager.ApplyPreset(LayoutPreset.Wide);

        Assert.Equal("Wide", manager.CurrentLayout.Name);
    }

    [Fact]
    public void ResetToDefault_RestoresDefaultLayout()
    {
        var manager = CreateTestManager();
        manager.ApplyPreset(LayoutPreset.Tall);

        manager.ResetToDefault();

        Assert.Equal("Default", manager.CurrentLayout.Name);
    }

    [Fact]
    public void UpdateWindowState_UpdatesCurrentLayout()
    {
        var manager = CreateTestManager();

        manager.UpdateWindowState(50, 75, 1280, 720, true, 1);

        Assert.Equal(50, manager.CurrentLayout.Window.X);
        Assert.Equal(75, manager.CurrentLayout.Window.Y);
        Assert.Equal(1280, manager.CurrentLayout.Window.Width);
        Assert.Equal(720, manager.CurrentLayout.Window.Height);
        Assert.True(manager.CurrentLayout.Window.Maximized);
        Assert.Equal(1, manager.CurrentLayout.Window.Monitor);
    }

    [Fact]
    public void UpdatePanelState_UpdatesExistingPanel()
    {
        var manager = CreateTestManager();

        manager.UpdatePanelState("hierarchy", visible: true, collapsed: true, width: 250);

        var state = manager.GetPanelState("hierarchy");
        Assert.NotNull(state);
        Assert.True(state.Visible);
        Assert.True(state.Collapsed);
        Assert.Equal(250f, state.Width);
    }

    [Fact]
    public void UpdatePanelState_CreatesNewPanel()
    {
        var manager = CreateTestManager();

        manager.UpdatePanelState("newpanel", visible: true, collapsed: false);

        var state = manager.GetPanelState("newpanel");
        Assert.NotNull(state);
        Assert.True(state.Visible);
        Assert.False(state.Collapsed);
    }

    [Fact]
    public void GetPanelState_NonExistentPanel_ReturnsNull()
    {
        var manager = CreateTestManager();

        var state = manager.GetPanelState("doesnotexist");

        Assert.Null(state);
    }

    [Fact]
    public void TogglePanelVisibility_TogglesExistingPanel()
    {
        var manager = CreateTestManager();
        var initialState = manager.GetPanelState("hierarchy")?.Visible ?? true;

        manager.TogglePanelVisibility("hierarchy");

        Assert.Equal(!initialState, manager.GetPanelState("hierarchy")?.Visible);
    }

    [Fact]
    public void TogglePanelVisibility_CreatesNewPanelIfNotExists()
    {
        var manager = CreateTestManager();

        manager.TogglePanelVisibility("brandnewpanel");

        Assert.NotNull(manager.GetPanelState("brandnewpanel"));
    }

    #endregion

    #region Custom Layout Tests

    [Fact]
    public void SaveCustomLayout_CreatesFile()
    {
        var manager = CreateTestManager();
        var customLayoutsFolder = GetTestCustomLayoutsFolder();

        try
        {
            Directory.CreateDirectory(customLayoutsFolder);
            SaveCustomLayoutToPath(manager, "TestLayout", customLayoutsFolder);

            var expectedPath = Path.Combine(customLayoutsFolder, "TestLayout.json");
            Assert.True(File.Exists(expectedPath));
        }
        finally
        {
            CleanupDirectory(customLayoutsFolder);
        }
    }

    [Fact]
    public void LoadCustomLayout_LoadsSavedLayout()
    {
        var customLayoutsFolder = GetTestCustomLayoutsFolder();

        try
        {
            Directory.CreateDirectory(customLayoutsFolder);

            // Create a custom layout file
            var layout = EditorLayout.CreateWide();
            layout.Name = "MyCustom";
            var json = layout.ToJson();
            File.WriteAllText(Path.Combine(customLayoutsFolder, "MyCustom.json"), json);

            var manager = CreateTestManager();
            LoadCustomLayoutFromPath(manager, "MyCustom", customLayoutsFolder);

            Assert.Equal("MyCustom", manager.CurrentLayout.Name);
        }
        finally
        {
            CleanupDirectory(customLayoutsFolder);
        }
    }

    #endregion

    #region Event Tests

    [Fact]
    public void ApplyPreset_RaisesLayoutChanged()
    {
        var manager = CreateTestManager();
        LayoutChangedEventArgs? eventArgs = null;
        manager.LayoutChanged += (_, e) => eventArgs = e;

        manager.ApplyPreset(LayoutPreset.Tall);

        Assert.NotNull(eventArgs);
        Assert.Equal(LayoutChangeReason.PresetApplied, eventArgs.Reason);
        Assert.Equal("Tall", eventArgs.Layout.Name);
    }

    [Fact]
    public void ResetToDefault_RaisesLayoutChanged()
    {
        var manager = CreateTestManager();
        manager.ApplyPreset(LayoutPreset.Wide);

        LayoutChangedEventArgs? eventArgs = null;
        manager.LayoutChanged += (_, e) => eventArgs = e;

        manager.ResetToDefault();

        Assert.NotNull(eventArgs);
        Assert.Equal(LayoutChangeReason.Reset, eventArgs.Reason);
    }

    [Fact]
    public void TogglePanelVisibility_RaisesLayoutChanged()
    {
        var manager = CreateTestManager();
        LayoutChangedEventArgs? eventArgs = null;
        manager.LayoutChanged += (_, e) => eventArgs = e;

        manager.TogglePanelVisibility("hierarchy");

        Assert.NotNull(eventArgs);
        Assert.Equal(LayoutChangeReason.PanelToggled, eventArgs.Reason);
    }

    #endregion

    #region DockLayoutNode Tests

    [Fact]
    public void DockLayoutNode_DefaultValues()
    {
        var node = new DockLayoutNode();

        Assert.Equal(DockNodeType.Panel, node.Type);
        Assert.Null(node.PanelId);
        Assert.Equal(SplitDirection.Horizontal, node.Direction);
        Assert.Equal(0.5f, node.Ratio);
        Assert.Null(node.Children);
    }

    [Fact]
    public void DockLayoutNode_SplitNode_HasChildren()
    {
        var layout = EditorLayout.CreateDefault();
        var root = layout.DockLayout!;

        Assert.Equal(DockNodeType.Split, root.Type);
        Assert.NotNull(root.Children);
        Assert.True(root.Children.Count >= 2);
    }

    #endregion

    #region Helpers

    private static LayoutManager CreateTestManager()
    {
        // We need to use a new instance for testing, but LayoutManager is a singleton
        // For testing purposes, we access the singleton and reset it
        var manager = LayoutManager.Instance;
        manager.ResetToDefault();
        return manager;
    }

    private static string GetTestLayoutPath()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "KeenEyesLayoutTests");
        Directory.CreateDirectory(testDir);
        return Path.Combine(testDir, $"layout_{Guid.NewGuid()}.json");
    }

    private static string GetTestCustomLayoutsFolder()
    {
        return Path.Combine(Path.GetTempPath(), "KeenEyesLayoutTests", "custom_layouts");
    }

    private static void CleanupTestFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void CleanupDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    // Helper methods to work around the singleton's fixed paths
    private static void SaveCustomLayoutToPath(LayoutManager manager, string name, string folder)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(folder, $"{safeName}.json");
        manager.CurrentLayout.Name = name;

        var json = manager.CurrentLayout.ToJson();
        File.WriteAllText(path, json);
    }

    private static void LoadCustomLayoutFromPath(LayoutManager manager, string name, string folder)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(folder, $"{safeName}.json");

        if (File.Exists(path))
        {
            manager.Load(path);
        }
    }

    #endregion
}
