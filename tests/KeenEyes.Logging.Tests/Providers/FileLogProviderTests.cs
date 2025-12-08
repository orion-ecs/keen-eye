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

    #region Sanitization Tests

    [Fact]
    public void Log_WithNewlineInMessage_SanitizesNewlines()
    {
        var filePath = Path.Combine(testDirectory, "sanitize_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Line1\nLine2", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Line1\\nLine2");
    }

    [Fact]
    public void Log_WithCarriageReturnInMessage_SanitizesCarriageReturns()
    {
        var filePath = Path.Combine(testDirectory, "sanitize_cr_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Line1\rLine2", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Line1\\rLine2");
    }

    [Fact]
    public void Log_WithCrLfInMessage_SanitizesCrLf()
    {
        var filePath = Path.Combine(testDirectory, "sanitize_crlf_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Line1\r\nLine2", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Line1\\r\\nLine2");
    }

    [Fact]
    public void Log_WithNewlineInCategory_SanitizesCategory()
    {
        var filePath = Path.Combine(testDirectory, "sanitize_cat_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Cat\negory", "Message", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("[Cat\\negory]");
    }

    [Fact]
    public void Log_WithNewlineInPropertyValue_SanitizesPropertyValue()
    {
        var filePath = Path.Combine(testDirectory, "sanitize_prop_test.log");
        var properties = new Dictionary<string, object?> { ["Key"] = "Value\nWith\nNewlines" };

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Message", properties);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Key=Value\\nWith\\nNewlines");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Log_WithEmptyMessage_HandlesGracefully()
    {
        var filePath = Path.Combine(testDirectory, "empty_msg_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "", null));
            provider.Flush();
        }
    }

    [Fact]
    public void Log_WithMultipleProperties_FormatsAllProperties()
    {
        var filePath = Path.Combine(testDirectory, "multi_prop_test.log");
        var properties = new Dictionary<string, object?>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Message", properties);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("Key1=Value1");
        content.ShouldContain("Key2=Value2");
    }

    [Fact]
    public void Log_WithNullPropertyValue_FormatsAsNull()
    {
        var filePath = Path.Combine(testDirectory, "null_prop_test.log");
        var properties = new Dictionary<string, object?> { ["NullKey"] = null };

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log(LogLevel.Info, "Test", "Message", properties);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("NullKey=null");
    }

    [Fact]
    public void Log_WithUnknownLevel_FormatsAsQuestionMarks()
    {
        var filePath = Path.Combine(testDirectory, "unknown_level_test.log");

        using (var provider = new FileLogProvider(filePath))
        {
            provider.Log((LogLevel)99, "Test", "Message", null);
            provider.Flush();
        }

        var content = File.ReadAllText(filePath);
        content.ShouldContain("???");
    }

    #endregion

    #region Race Condition and Concurrent Access Tests

    [Fact]
    public void Log_ConcurrentWithDispose_HandlesRaceCondition()
    {
        var filePath = Path.Combine(testDirectory, "race_test.log");
        var provider = new FileLogProvider(filePath);

        // Log a message first to initialize the writer
        provider.Log(LogLevel.Info, "Test", "Initial message", null);

        // Start logging in parallel while disposing
        // We intentionally don't use TestContext.Current.CancellationToken here because
        // this test needs to control its own task lifecycle to test race conditions
        var loggingComplete = false;
#pragma warning disable xUnit1051 // Test intentionally controls its own task lifecycle
        var loggingTask = Task.Run(() =>
        {
            while (!loggingComplete)
            {
                try
                {
                    provider.Log(LogLevel.Info, "Test", "Concurrent message", null);
                }
                catch
                {
                    // Expected during disposal
                }
            }
        });
#pragma warning restore xUnit1051

        // Small delay then dispose
        Thread.Sleep(10);
        provider.Dispose();
        loggingComplete = true;

        // Should complete without throwing
        Should.NotThrow(() => loggingTask.Wait(1000));
    }

    [Fact]
    public void Log_AfterDisposeInsideLock_DoesNotWrite()
    {
        // This tests the disposed check inside the lock (line 95-97)
        var filePath = Path.Combine(testDirectory, "dispose_lock_test.log");
        var provider = new FileLogProvider(filePath);

        // Initialize writer
        provider.Log(LogLevel.Info, "Test", "First message", null);
        provider.Flush();

        // Dispose
        provider.Dispose();

        // Try to log after dispose - should not throw, and should not write
        Should.NotThrow(() => provider.Log(LogLevel.Info, "Test", "Should not appear", null));

        var content = File.ReadAllText(filePath);
        content.ShouldNotContain("Should not appear");
    }

    #endregion

    #region File Rotation Edge Cases

    [Fact]
    public void Log_WithRotation_MultipleRotationsInSameSecond_UsesCounterSuffix()
    {
        var filePath = Path.Combine(testDirectory, "multi_rotate.log");
        var maxSize = 50L; // Very small to trigger multiple rotations quickly

        using (var provider = new FileLogProvider(filePath) { MaxFileSizeBytes = maxSize })
        {
            // Write many messages rapidly to trigger multiple rotations in the same second
            for (int i = 0; i < 50; i++)
            {
                provider.Log(LogLevel.Info, "Test", $"Message {i} padding to exceed size limit", null);
            }

            provider.Flush();
        }

        // Should have multiple rotated files, some with counter suffixes
        var files = Directory.GetFiles(testDirectory, "multi_rotate*.log");
        files.Length.ShouldBeGreaterThan(2); // Original + at least 2 rotated

        // Check that counter suffixes exist (files like multi_rotate_20251208_123456_1.log)
        var filesWithCounters = files.Where(f =>
        {
            var name = Path.GetFileNameWithoutExtension(f);
            // Pattern: name_YYYYMMDD_HHMMSS_N where N is counter
            var parts = name.Split('_');
            return parts.Length >= 4 && int.TryParse(parts[^1], out _);
        }).ToList();

        // At least some files should have counter suffixes due to rapid rotation
        filesWithCounters.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Log_WithRotation_PreExistingRotatedFile_UsesCounterToAvoidCollision()
    {
        var filePath = Path.Combine(testDirectory, "collision_test.log");

        // Create a pre-existing rotated file with today's timestamp
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var preExistingRotated = Path.Combine(testDirectory, $"collision_test_{timestamp}.log");
        File.WriteAllText(preExistingRotated, "Pre-existing rotated file");

        // Now create provider with rotation
        using (var provider = new FileLogProvider(filePath) { MaxFileSizeBytes = 50 })
        {
            // Write enough to trigger rotation
            for (int i = 0; i < 20; i++)
            {
                provider.Log(LogLevel.Info, "Test", $"Message {i} with extra padding text", null);
            }

            provider.Flush();
        }

        // The new rotated file should have a counter suffix to avoid collision
        var files = Directory.GetFiles(testDirectory, "collision_test*.log");
        files.Length.ShouldBeGreaterThanOrEqualTo(2);

        // Pre-existing file should still exist and be unchanged
        File.Exists(preExistingRotated).ShouldBeTrue();
        File.ReadAllText(preExistingRotated).ShouldBe("Pre-existing rotated file");
    }

    #endregion
}
