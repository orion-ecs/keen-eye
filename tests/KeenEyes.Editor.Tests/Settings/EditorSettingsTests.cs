using KeenEyes.Editor.Settings;

namespace KeenEyes.Editor.Tests.Settings;

public class EditorSettingsTests
{
    #region General Settings Tests

    [Fact]
    public void AutoSaveEnabled_DefaultValue_IsTrue()
    {
        ResetSettingsForTest();

        Assert.True(EditorSettings.AutoSaveEnabled);
    }

    [Fact]
    public void AutoSaveEnabled_SetValue_UpdatesValue()
    {
        ResetSettingsForTest();

        EditorSettings.AutoSaveEnabled = false;

        Assert.False(EditorSettings.AutoSaveEnabled);
    }

    [Fact]
    public void AutoSaveIntervalSeconds_DefaultValue_Is300()
    {
        ResetSettingsForTest();

        Assert.Equal(300, EditorSettings.AutoSaveIntervalSeconds);
    }

    [Fact]
    public void AutoSaveIntervalSeconds_SetBelowMinimum_ClampedTo30()
    {
        ResetSettingsForTest();

        EditorSettings.AutoSaveIntervalSeconds = 10;

        Assert.Equal(30, EditorSettings.AutoSaveIntervalSeconds);
    }

    [Fact]
    public void UndoHistoryLimit_DefaultValue_Is100()
    {
        ResetSettingsForTest();

        Assert.Equal(100, EditorSettings.UndoHistoryLimit);
    }

    [Fact]
    public void UndoHistoryLimit_SetBelowMinimum_ClampedTo1()
    {
        ResetSettingsForTest();

        EditorSettings.UndoHistoryLimit = 0;

        Assert.Equal(1, EditorSettings.UndoHistoryLimit);
    }

    [Fact]
    public void RecentFilesCount_ClampedTo1Through50()
    {
        ResetSettingsForTest();

        EditorSettings.RecentFilesCount = 0;
        Assert.Equal(1, EditorSettings.RecentFilesCount);

        EditorSettings.RecentFilesCount = 100;
        Assert.Equal(50, EditorSettings.RecentFilesCount);

        EditorSettings.RecentFilesCount = 25;
        Assert.Equal(25, EditorSettings.RecentFilesCount);
    }

    #endregion

    #region Appearance Settings Tests

    [Fact]
    public void Theme_DefaultValue_IsDark()
    {
        ResetSettingsForTest();

        Assert.Equal("Dark", EditorSettings.Theme);
    }

    [Fact]
    public void Theme_SetNull_DefaultsToDark()
    {
        ResetSettingsForTest();

        EditorSettings.Theme = null!;

        Assert.Equal("Dark", EditorSettings.Theme);
    }

    [Fact]
    public void FontSize_ClampedTo8Through32()
    {
        ResetSettingsForTest();

        EditorSettings.FontSize = 4;
        Assert.Equal(8, EditorSettings.FontSize);

        EditorSettings.FontSize = 50;
        Assert.Equal(32, EditorSettings.FontSize);

        EditorSettings.FontSize = 16;
        Assert.Equal(16, EditorSettings.FontSize);
    }

    [Fact]
    public void UiScale_ClampedTo0_5Through3_0()
    {
        ResetSettingsForTest();

        EditorSettings.UiScale = 0.1f;
        Assert.Equal(0.5f, EditorSettings.UiScale);

        EditorSettings.UiScale = 5.0f;
        Assert.Equal(3.0f, EditorSettings.UiScale);

        EditorSettings.UiScale = 1.5f;
        Assert.Equal(1.5f, EditorSettings.UiScale);
    }

    #endregion

    #region Viewport Settings Tests

    [Fact]
    public void GridVisible_DefaultValue_IsTrue()
    {
        ResetSettingsForTest();

        Assert.True(EditorSettings.GridVisible);
    }

    [Fact]
    public void GridSize_DefaultValue_Is1()
    {
        ResetSettingsForTest();

        Assert.Equal(1.0f, EditorSettings.GridSize);
    }

    [Fact]
    public void GridSize_SetBelowMinimum_ClampedTo0_1()
    {
        ResetSettingsForTest();

        EditorSettings.GridSize = 0.01f;

        Assert.Equal(0.1f, EditorSettings.GridSize);
    }

    [Fact]
    public void CameraFieldOfView_ClampedTo10Through170()
    {
        ResetSettingsForTest();

        EditorSettings.CameraFieldOfView = 5f;
        Assert.Equal(10f, EditorSettings.CameraFieldOfView);

        EditorSettings.CameraFieldOfView = 180f;
        Assert.Equal(170f, EditorSettings.CameraFieldOfView);

        EditorSettings.CameraFieldOfView = 90f;
        Assert.Equal(90f, EditorSettings.CameraFieldOfView);
    }

    [Fact]
    public void WireframeMode_DefaultValue_IsFalse()
    {
        ResetSettingsForTest();

        Assert.False(EditorSettings.WireframeMode);
    }

    [Fact]
    public void WireframeMode_SetTrue_UpdatesValue()
    {
        ResetSettingsForTest();

        EditorSettings.WireframeMode = true;

        Assert.True(EditorSettings.WireframeMode);
    }

    [Fact]
    public void WireframeMode_SetFalse_UpdatesValue()
    {
        ResetSettingsForTest();
        EditorSettings.WireframeMode = true;

        EditorSettings.WireframeMode = false;

        Assert.False(EditorSettings.WireframeMode);
    }

    [Fact]
    public void WireframeMode_SettingChanged_RaisesEvent()
    {
        ResetSettingsForTest();
        EditorSettings.Load(GetTestSettingsPath());

        SettingChangedEventArgs? eventArgs = null;
        EditorSettings.SettingChanged += (_, e) => eventArgs = e;

        EditorSettings.WireframeMode = true;

        Assert.NotNull(eventArgs);
        Assert.Equal("WireframeMode", eventArgs.SettingName);
        Assert.Equal("Viewport", eventArgs.Category);
        Assert.Equal(false, eventArgs.OldValue);
        Assert.Equal(true, eventArgs.NewValue);
    }

    [Fact]
    public void WireframeMode_SameValue_DoesNotRaiseEvent()
    {
        ResetSettingsForTest();
        EditorSettings.Load(GetTestSettingsPath());

        var eventCount = 0;
        EditorSettings.SettingChanged += (_, _) => eventCount++;

        // Set to same value (default is false)
        EditorSettings.WireframeMode = false;

        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void WireframeMode_Persists_AfterSaveAndLoad()
    {
        var path = GetTestSettingsPath();
        var path2 = GetTestSettingsPath();

        try
        {
            EditorSettings.Load(path);
            EditorSettings.WireframeMode = true;
            EditorSettings.Save();

            File.Copy(path, path2);

            ResetSettingsForTest();
            EditorSettings.Load(path2);

            Assert.True(EditorSettings.WireframeMode);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (File.Exists(path2))
            {
                File.Delete(path2);
            }
        }
    }

    #endregion

    #region Play Mode Settings Tests

    [Fact]
    public void MaximizeOnPlay_DefaultValue_IsFalse()
    {
        ResetSettingsForTest();

        Assert.False(EditorSettings.MaximizeOnPlay);
    }

    [Fact]
    public void DefaultTimeScale_DefaultValue_Is1()
    {
        ResetSettingsForTest();

        Assert.Equal(1.0f, EditorSettings.DefaultTimeScale);
    }

    [Fact]
    public void DefaultTimeScale_ClampedTo0Through100()
    {
        ResetSettingsForTest();

        EditorSettings.DefaultTimeScale = -1f;
        Assert.Equal(0f, EditorSettings.DefaultTimeScale);

        EditorSettings.DefaultTimeScale = 150f;
        Assert.Equal(100f, EditorSettings.DefaultTimeScale);
    }

    #endregion

    #region External Tools Settings Tests

    [Fact]
    public void ScriptEditorPath_DefaultValue_IsEmpty()
    {
        ResetSettingsForTest();

        Assert.Equal(string.Empty, EditorSettings.ScriptEditorPath);
    }

    [Fact]
    public void ScriptEditorPath_SetNull_DefaultsToEmpty()
    {
        ResetSettingsForTest();

        EditorSettings.ScriptEditorPath = null!;

        Assert.Equal(string.Empty, EditorSettings.ScriptEditorPath);
    }

    #endregion

    #region Change Event Tests

    [Fact]
    public void SettingChanged_RaisedWhenSettingChanges()
    {
        ResetSettingsForTest();
        EditorSettings.Load(GetTestSettingsPath());

        SettingChangedEventArgs? eventArgs = null;
        EditorSettings.SettingChanged += (_, e) => eventArgs = e;

        EditorSettings.Theme = "Light";

        Assert.NotNull(eventArgs);
        Assert.Equal("Theme", eventArgs.SettingName);
        Assert.Equal("Appearance", eventArgs.Category);
        Assert.Equal("Dark", eventArgs.OldValue);
        Assert.Equal("Light", eventArgs.NewValue);
    }

    [Fact]
    public void SettingChanged_NotRaisedWhenValueSame()
    {
        ResetSettingsForTest();
        EditorSettings.Load(GetTestSettingsPath());
        EditorSettings.Theme = "TestTheme";

        var eventCount = 0;
        EditorSettings.SettingChanged += (_, _) => eventCount++;

        // Set same value again
        EditorSettings.Theme = "TestTheme";

        Assert.Equal(0, eventCount);
    }

    #endregion

    #region Categories Tests

    [Fact]
    public void Categories_ContainsAllCategories()
    {
        var categories = EditorSettings.Categories;

        Assert.Contains("General", categories);
        Assert.Contains("Appearance", categories);
        Assert.Contains("Viewport", categories);
        Assert.Contains("PlayMode", categories);
        Assert.Contains("ExternalTools", categories);
        Assert.Contains("Shortcuts", categories);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void Load_NonExistentFile_UsesDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        ResetSettingsForTest();

        EditorSettings.Load(path);

        Assert.True(EditorSettings.IsLoaded);
        Assert.True(EditorSettings.AutoSaveEnabled); // Default value
    }

    [Fact]
    public void SaveAndLoad_Roundtrips()
    {
        var path = GetTestSettingsPath();
        var path2 = GetTestSettingsPath(); // Different path for second load

        try
        {
            // First, load with default settings (file doesn't exist yet)
            EditorSettings.Load(path);

            // Modify some settings
            EditorSettings.Theme = "CustomTheme";
            EditorSettings.FontSize = 18;
            EditorSettings.GridVisible = false;

            // Force save (happens automatically but let's be explicit)
            EditorSettings.Save();

            // Verify the file was created
            Assert.True(File.Exists(path));

            // Copy the saved file to a new location before we reset
            File.Copy(path, path2);

            // Reset to defaults - this will save defaults to 'path'
            EditorSettings.ResetToDefaults();

            // Verify defaults are restored
            Assert.Equal("Dark", EditorSettings.Theme);

            // Now reload from the backup file - this should restore our custom settings
            EditorSettings.Load(path2);

            Assert.Equal("CustomTheme", EditorSettings.Theme);
            Assert.Equal(18, EditorSettings.FontSize);
            Assert.False(EditorSettings.GridVisible);
        }
        finally
        {
            // Cleanup
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (File.Exists(path2))
            {
                File.Delete(path2);
            }
        }
    }

    [Fact]
    public void ResetToDefaults_RestoresAllDefaults()
    {
        ResetSettingsForTest();
        EditorSettings.Load(GetTestSettingsPath());

        // Modify settings
        EditorSettings.Theme = "Light";
        EditorSettings.FontSize = 24;
        EditorSettings.AutoSaveEnabled = false;

        EditorSettings.ResetToDefaults();

        Assert.Equal("Dark", EditorSettings.Theme);
        Assert.Equal(14, EditorSettings.FontSize);
        Assert.True(EditorSettings.AutoSaveEnabled);
    }

    [Fact]
    public void ResetCategory_RestoresOnlyThatCategory()
    {
        ResetSettingsForTest();
        EditorSettings.Load(GetTestSettingsPath());

        // Modify settings in different categories
        EditorSettings.Theme = "Light";
        EditorSettings.AutoSaveEnabled = false;

        EditorSettings.ResetCategory("Appearance");

        Assert.Equal("Dark", EditorSettings.Theme); // Reset
        Assert.False(EditorSettings.AutoSaveEnabled); // Not reset (different category)
    }

    #endregion

    #region Helpers

    private static void ResetSettingsForTest()
    {
        // Force reset by loading defaults
        EditorSettings.ResetToDefaults();
    }

    private static string GetTestSettingsPath()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "KeenEyesTests");
        Directory.CreateDirectory(testDir);
        return Path.Combine(testDir, $"settings_{Guid.NewGuid()}.json");
    }

    #endregion
}
