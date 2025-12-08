using KeenEyes.Logging.Providers;

namespace KeenEyes.Logging.Tests.Providers;

public class FileLogProviderTests : IDisposable
{
    private readonly string testDirectory;

    public FileLogProviderTests()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"KeenEyes_FileLogTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void Name_ReturnsFile()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        using var provider = new FileLogProvider(filePath);

        provider.Name.ShouldBe("File");
    }

    [Fact]
    public void MinimumLevel_DefaultsToTrace()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        using var provider = new FileLogProvider(filePath);

        provider.MinimumLevel.ShouldBe(LogLevel.Trace);
    }

    [Fact]
    public void FilePath_ReturnsFullPath()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        using var provider = new FileLogProvider(filePath);

        provider.FilePath.ShouldBe(Path.GetFullPath(filePath));
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileLogProvider(null!));
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileLogProvider(""));
    }

    [Fact]
    public void Constructor_WithWhitespacePath_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new FileLogProvider("   "));
    }

    [Fact]
    public void Log_CreatesFileAndWritesMessage()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "TestCategory", "Test message", null);
            provider.Flush();
        }

        File.Exists(filePath).ShouldBeTrue();
        var content = File.ReadAllText(filePath);
        content.ShouldContain("INF");
        content.ShouldContain("[TestCategory]");
        content.ShouldContain("Test message");
    }

    [Fact]
    public void Log_CreatesDirectoryIfNotExists()
    {
        var subDir = Path.Combine(testDirectory, "subdir", "nested");
        var filePath = Path.Combine(subDir, "test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Message", null);
            provider.Flush();
        }

        File.Exists(filePath).ShouldBeTrue();
    }

    [Fact]
    public void Log_AppendsToExistingFile()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        File.WriteAllText(filePath, "Existing content\n");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "New message", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Existing content");
        content.ShouldContain("New message");
    }

    [Fact]
    public void Log_WithProperties_IncludesPropertiesInOutput()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Message", properties);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Key=Value");
    }

    [Fact]
    public void Log_WithPropertiesDisabled_ExcludesProperties()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        var properties = new Dictionary<string, object?> { ["Key"] = "Value" };

        using (var provider = new FileLogProvider(filePath) { IncludeProperties = false })
        {
            provider.Log(LogLevel.Info, "Test", "Message", properties);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldNotContain("Key=Value");
    }

    [Fact]
    public void Log_BelowMinimumLevel_DoesNotWrite()
    {
        var filePath = Path.Combine(testDirectory, "test.log");

        using (var provider = new FileLogProvider(filePath) { MinimumLevel = LogLevel.Warning })
        {
            provider.Log(LogLevel.Debug, "Test", "Debug message", null);
            provider.Flush();
        }

        // File may not exist or be empty
        if (File.Exists(filePath))
        {
            File.ReadAllText(filePath).ShouldBeEmpty();
        }
    }

    [Fact]
    public void Log_WithRotation_CreatesNewFileWhenSizeExceeded()
    {
        var filePath = Path.Combine(testDirectory, "rotating.log");
        var maxSize = 100L; // Very small for testing

        using (var provider = new FileLogProvider(filePath) { MaxFileSizeBytes = maxSize })
        {
            // Write enough to trigger rotation
            for (int i = 0; i < 10; i++)
            {
                provider.Log(LogLevel.Info, "Test", $"Message {i} with some extra text to increase size", null);
            }

            provider.Flush();
        }

        // Should have rotated files
        var files = Directory.GetFiles(testDirectory, "rotating*.log");
        files.Length.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void Dispose_FlushesAndClosesFile()
    {
        var filePath = Path.Combine(testDirectory, "test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Message", null);
        }

        // File should be readable after dispose (not locked)
        var content = File.ReadAllText(filePath);
        content.ShouldContain("Message");
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        var provider = new FileLogProvider(filePath);
        provider.Log(LogLevel.Info, "Test", "Message", null);

        Should.NotThrow(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    [Fact]
    public void Log_AfterDispose_DoesNotThrow()
    {
        var filePath = Path.Combine(testDirectory, "test.log");
        var provider = new FileLogProvider(filePath);
        provider.Dispose();

        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Message", null));
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRC")]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Info, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Fatal, "FTL")]
    public void Log_FormatsLevelCorrectly(LogLevel level, string expectedAbbreviation)
    {
        var filePath = Path.Combine(testDirectory, "level_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(level, "Test", "Message", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain(expectedAbbreviation);
    }
}
