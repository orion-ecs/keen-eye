using KeenEyes.Editor.HotReload;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Editor.Serialization;
using KeenEyes.Editor.Settings;

namespace KeenEyes.Editor.Tests.HotReload;

public class HotReloadServiceTests : IDisposable
{
    public HotReloadServiceTests()
    {
        // EditorSettings is process-global static state that HotReloadService reads/writes and
        // subscribes to (SettingChanged). Reset it to defaults and detach any leaked
        // subscribers so this class starts from clean, isolated state regardless of what other
        // editor tests ran before it (see #1203). Defaults already match the values these
        // tests expect (empty GameProjectPath, HotReload enabled, 500ms debounce, auto-reload).
        EditorSettings.ResetForTesting();
    }

    public void Dispose()
    {
        // Leave no global state (or leaked SettingChanged subscriptions) behind for the next test.
        EditorSettings.ResetForTesting();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidWorld_Succeeds()
    {
        using var world = new World();
        using var service = new HotReloadService(world);

        Assert.NotNull(service);
        Assert.Equal(HotReloadStatus.Disabled, service.CurrentStatus);
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void Constructor_WithNullWorld_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HotReloadService(null!));
    }

    #endregion

    #region Initialize Tests

    [Fact]
    public void Initialize_WithNoProject_StaysDisabled()
    {
        using var world = new World();
        using var service = new HotReloadService(world);

        service.Initialize();

        Assert.Equal(HotReloadStatus.Disabled, service.CurrentStatus);
    }

    [Fact]
    public void Initialize_WithInvalidProjectPath_SetsFailed()
    {
        using var world = new World();
        using var service = new HotReloadService(world);

        EditorSettings.GameProjectPath = "/nonexistent/path/project.csproj";
        service.Initialize();

        Assert.Equal(HotReloadStatus.Failed, service.CurrentStatus);
        Assert.Contains("not found", service.LastMessage);
    }

    [Fact]
    public void Initialize_WithValidProject_ConfiguresSuccessfully()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var service = new HotReloadService(world);

            EditorSettings.GameProjectPath = tempProject;
            service.Initialize();

            Assert.Equal(HotReloadStatus.Idle, service.CurrentStatus);
            Assert.True(service.IsEnabled);
            Assert.True(service.IsWatching);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void Initialize_AfterDispose_ThrowsObjectDisposedException()
    {
        using var world = new World();
        var service = new HotReloadService(world);
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.Initialize());
    }

    #endregion

    #region SetGameProject Tests

    [Fact]
    public void SetGameProject_WithValidPath_UpdatesSettings()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var service = new HotReloadService(world);
            service.Initialize();

            HotReloadService.SetGameProject(tempProject);

            Assert.Equal(tempProject, EditorSettings.GameProjectPath);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void SetGameProject_WithNull_ClearsSettings()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var service = new HotReloadService(world);

            EditorSettings.GameProjectPath = tempProject;
            service.Initialize();
            HotReloadService.SetGameProject(null);

            Assert.Equal(string.Empty, EditorSettings.GameProjectPath);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region Status Tests

    [Fact]
    public void StatusChanged_RaisesWhenStatusChanges()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var service = new HotReloadService(world);

            HotReloadStatus? receivedStatus = null;
            service.StatusChanged += (s, e) => receivedStatus = e.Status;

            EditorSettings.GameProjectPath = tempProject;
            service.Initialize();

            Assert.NotNull(receivedStatus);
            Assert.Equal(HotReloadStatus.Idle, receivedStatus);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void IsReloading_InitiallyFalse()
    {
        using var world = new World();
        using var service = new HotReloadService(world);

        Assert.False(service.IsReloading);
    }

    [Fact]
    public void RegisteredSystemCount_InitiallyZero()
    {
        using var world = new World();
        using var service = new HotReloadService(world);

        Assert.Equal(0, service.RegisteredSystemCount);
    }

    #endregion

    #region ConnectPlayMode Tests

    [Fact]
    public void ConnectPlayMode_WithNull_DoesNotThrow()
    {
        using var world = new World();
        using var service = new HotReloadService(world);

        var exception = Record.Exception(() => service.ConnectPlayMode(null));

        Assert.Null(exception);
    }

    [Fact]
    public void ConnectPlayMode_WithValidManager_Connects()
    {
        using var world = new World();
        using var service = new HotReloadService(world);
        var serializer = new EditorComponentSerializer();
        var playMode = new PlayModeManager(world, serializer);

        var exception = Record.Exception(() => service.ConnectPlayMode(playMode));

        Assert.Null(exception);
    }

    [Fact]
    public void ConnectPlayMode_MultipleTimes_ReplacesPrevious()
    {
        using var world = new World();
        using var service = new HotReloadService(world);
        var serializer = new EditorComponentSerializer();
        var playMode1 = new PlayModeManager(world, serializer);
        var playMode2 = new PlayModeManager(world, serializer);

        service.ConnectPlayMode(playMode1);
        var exception = Record.Exception(() => service.ConnectPlayMode(playMode2));

        Assert.Null(exception);
    }

    #endregion

    #region ReloadAsync Tests

    [Fact]
    public async Task ReloadAsync_WithNoProject_ReturnsFailed()
    {
        using var world = new World();
        using var service = new HotReloadService(world);
        service.Initialize();

        var result = await service.ReloadAsync();

        Assert.False(result.Success);
        Assert.Contains("not configured", result.Message);
    }

    [Fact]
    public async Task ReloadAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        using var world = new World();
        var service = new HotReloadService(world);
        service.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.ReloadAsync());
    }

    #endregion

    #region TryDetectGameProject Tests

    [Fact]
    public void TryDetectGameProject_ReturnsNullWhenNoProjectFound()
    {
        // This test may pass or fail depending on the current directory
        // We mainly want to ensure it doesn't throw
        var result = HotReloadService.TryDetectGameProject();

        // Result may or may not be null depending on environment
        Assert.True(result == null || File.Exists(result));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_StopsWatching()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var service = new HotReloadService(world);

            EditorSettings.GameProjectPath = tempProject;
            service.Initialize();
            Assert.True(service.IsWatching);

            service.Dispose();

            Assert.False(service.IsWatching);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        using var world = new World();
        var service = new HotReloadService(world);

        service.Dispose();
        var exception = Record.Exception(() => service.Dispose());

        Assert.Null(exception);
    }

    #endregion

    #region PlayMode Deferred Reload Tests

    [Fact]
    public void PlayModeExit_WithSourceEditDuringPlayMode_TriggersDeferredReload()
    {
        var tempProject = CreateTempProjectWithSource(out var sourceFile);
        try
        {
            using var world = new World();
            using var service = new HotReloadService(world);
            var serializer = new EditorComponentSerializer();
            var playMode = new PlayModeManager(world, serializer);

            EditorSettings.GameProjectPath = tempProject;
            service.Initialize();
            service.ConnectPlayMode(playMode);

            var buildingObserved = false;
            using var terminalReached = new ManualResetEventSlim(false);
            service.StatusChanged += (_, e) =>
            {
                if (e.Status == HotReloadStatus.Building)
                {
                    buildingObserved = true;
                }
                else if (e.Status is HotReloadStatus.Ready or HotReloadStatus.Failed)
                {
                    terminalReached.Set();
                }
            };

            // Enter play mode: the file watcher is disabled for the whole duration.
            Assert.True(playMode.Play());

            // Simulate an edit made while in play mode. The write time is
            // deterministically after the moment play mode was entered, so no
            // reliance on wall-clock timing or the (disabled) watcher.
            File.SetLastWriteTimeUtc(sourceFile, DateTime.UtcNow.AddMinutes(1));

            // Exit play mode: the deferred-reload path must detect the queued edit
            // and initiate a reload. Before #1178 this path was unreachable.
            Assert.True(playMode.Stop());

            // The reload is kicked off synchronously as play mode exits, so the
            // status transitions to Building within the Stop() call chain.
            Assert.True(buildingObserved);

            // Let the fire-and-forget reload finish (the stub project build fails
            // fast) so disposal does not race an in-flight reload.
            terminalReached.Wait(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region Helpers

    private static string CreateTempProjectWithSource(out string sourceFile)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "HotReloadServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var projectPath = Path.Combine(tempDir, "TestProject.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        sourceFile = Path.Combine(tempDir, "Game.cs");
        File.WriteAllText(sourceFile, "// game source");
        return projectPath;
    }

    private static string CreateTempProjectFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "HotReloadServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var projectPath = Path.Combine(tempDir, "TestProject.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        return projectPath;
    }

    private static void CleanupTempProject(string projectPath)
    {
        var dir = Path.GetDirectoryName(projectPath);
        if (dir != null && Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }
    }

    #endregion
}
