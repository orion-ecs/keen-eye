using KeenEyes.Editor.HotReload;

namespace KeenEyes.Editor.Tests.HotReload;

public class HotReloadManagerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidPath_Succeeds()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);

            Assert.NotNull(manager);
            Assert.False(manager.IsReloading);
            Assert.False(manager.IsWatching);
            Assert.Null(manager.GameAssembly);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void Constructor_WithNullWorld_ThrowsArgumentNullException()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            Assert.Throws<ArgumentNullException>(() => new HotReloadManager(tempProject, null!));
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        using var world = new World();
        Assert.Throws<ArgumentException>(() => new HotReloadManager("", world));
    }

    [Fact]
    public void Constructor_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        using var world = new World();
        Assert.Throws<FileNotFoundException>(() =>
            new HotReloadManager("/nonexistent/path/project.csproj", world));
    }

    #endregion

    #region File Watching Tests

    [Fact]
    public void StartWatching_EnablesWatching()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);

            manager.StartWatching();

            Assert.True(manager.IsWatching);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void StopWatching_DisablesWatching()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);

            manager.StartWatching();
            manager.StopWatching();

            Assert.False(manager.IsWatching);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void StartWatching_CalledTwice_IsIdempotent()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);

            manager.StartWatching();
            manager.StartWatching(); // Should not throw

            Assert.True(manager.IsWatching);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void StartWatching_AfterDispose_ThrowsObjectDisposedException()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            var manager = new HotReloadManager(tempProject, world);
            manager.Dispose();

            Assert.Throws<ObjectDisposedException>(() => manager.StartWatching());
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region LoadGameAssembly Tests

    [Fact]
    public void LoadGameAssembly_WithNonExistentPath_ReturnsFalse()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);

            var result = manager.LoadGameAssembly("/nonexistent/assembly.dll");

            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void LoadGameAssembly_AfterDispose_ThrowsObjectDisposedException()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            var manager = new HotReloadManager(tempProject, world);
            manager.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                manager.LoadGameAssembly("/some/path.dll"));
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region Reload Tests

    [Fact]
    public async Task ReloadAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            var manager = new HotReloadManager(tempProject, world);
            manager.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.ReloadAsync());
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public async Task ReloadAsync_RaisesReloadStartedEvent()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);
            var startedEventRaised = false;

            manager.ReloadStarted += () => startedEventRaised = true;

            await manager.ReloadAsync();

            Assert.True(startedEventRaised);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public async Task ReloadAsync_WithInvalidProject_RaisesCompilationFailedEvent()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);
            BuildResult? failedResult = null;

            manager.CompilationFailed += result => failedResult = result;

            await manager.ReloadAsync();

            // The temp project is not a valid .NET project, so build should fail
            Assert.NotNull(failedResult);
            Assert.False(failedResult.Success);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region Event Tests

    [Fact]
    public void SourceFileChanged_RaisesEvent()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world, TimeSpan.FromSeconds(10));
            string? changedFile = null;

            manager.SourceFileChanged += path => changedFile = path;
            manager.StartWatching();

            // Create a .cs file to trigger the event
            var csFile = Path.Combine(Path.GetDirectoryName(tempProject)!, "Test.cs");
            File.WriteAllText(csFile, "// test");

            // Give the watcher time to detect the change
            Thread.Sleep(100);

            // Note: Due to debouncing, the actual reload won't happen immediately
            // We just verify the event was raised
            Assert.NotNull(changedFile);
            Assert.Contains("Test.cs", changedFile);

            File.Delete(csFile);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region RegisteredSystems Tests

    [Fact]
    public void RegisteredSystems_InitiallyEmpty()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            using var manager = new HotReloadManager(tempProject, world);

            Assert.Empty(manager.RegisteredSystems);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
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
            var manager = new HotReloadManager(tempProject, world);
            manager.StartWatching();
            manager.Dispose();

            Assert.False(manager.IsWatching);
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    [Fact]
    public void Dispose_CalledTwice_IsIdempotent()
    {
        var tempProject = CreateTempProjectFile();
        try
        {
            using var world = new World();
            var manager = new HotReloadManager(tempProject, world);

            manager.Dispose();
            manager.Dispose(); // Should not throw
        }
        finally
        {
            CleanupTempProject(tempProject);
        }
    }

    #endregion

    #region Helpers

    private static string CreateTempProjectFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "HotReloadTests_" + Guid.NewGuid().ToString("N"));
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
