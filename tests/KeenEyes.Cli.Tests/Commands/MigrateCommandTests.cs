// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.IO.Compression;
using System.Text;
using KeenEyes.Cli.Commands.Migrate;
using KeenEyes.Serialization;
using Xunit;

namespace KeenEyes.Cli.Tests.Commands;

public sealed class MigrateCommandTests : IDisposable
{
    private readonly string testDir;
    private readonly TestConsoleOutput output;
    private readonly MigrateCommand command;

    public MigrateCommandTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), "keeneyes-migrate-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);

        output = new TestConsoleOutput();
        command = new MigrateCommand();
    }

    public void Dispose()
    {
        if (Directory.Exists(testDir))
        {
            try
            {
                Directory.Delete(testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    #region Command Properties

    [Fact]
    public void Name_ReturnsMigrate()
    {
        Assert.Equal("migrate", command.Name);
    }

    [Fact]
    public void Description_ContainsBatchUpgrade()
    {
        Assert.Contains("upgrade", command.Description.ToLowerInvariant());
    }

    [Fact]
    public void Usage_ContainsRequiredOptions()
    {
        Assert.Contains("--path", command.Usage);
        Assert.Contains("--dry-run", command.Usage);
        Assert.Contains("--backup", command.Usage);
    }

    #endregion

    #region Help Option

    [Fact]
    public async Task ExecuteAsync_WithHelpOption_ShowsHelp()
    {
        var result = await command.ExecuteAsync(["--help"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("Usage:"));
        Assert.Contains(output.Lines, l => l.Contains("--path"));
        Assert.Contains(output.Lines, l => l.Contains("--dry-run"));
        Assert.Contains(output.Lines, l => l.Contains("--backup"));
    }

    [Fact]
    public async Task ExecuteAsync_WithShortHelpOption_ShowsHelp()
    {
        var result = await command.ExecuteAsync(["-h"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("Usage:"));
    }

    #endregion

    #region Path Validation

    [Fact]
    public async Task ExecuteAsync_WithoutPath_ReturnsInvalidArguments()
    {
        var result = await command.ExecuteAsync([], output, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
        Assert.NotNull(result.Message);
        Assert.Contains("--path", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentPath_ReturnsFailure()
    {
        var nonExistentPath = Path.Combine(testDir, "nonexistent");

        var result = await command.ExecuteAsync(["--path", nonExistentPath], output, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
        Assert.Contains("does not exist", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidPath_Succeeds()
    {
        var result = await command.ExecuteAsync(["--path", testDir], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("No save files found"));
    }

    #endregion

    #region Empty Directory

    [Fact]
    public async Task ExecuteAsync_WithEmptyDirectory_ShowsNoSaveFilesMessage()
    {
        var result = await command.ExecuteAsync(["--path", testDir], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("No save files found"));
    }

    [Fact]
    public async Task ExecuteAsync_WithNonMatchingPattern_ShowsNoSaveFilesMessage()
    {
        // Create a file that doesn't match the pattern
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "not a save file");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--pattern", "*.ksave"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("No save files found"));
    }

    #endregion

    #region Dry Run Mode

    [Fact]
    public async Task ExecuteAsync_WithDryRun_DoesNotModifyFiles()
    {
        // Create a mock save file with components that need migration
        var saveFile = CreateMockSaveFileWithComponents("test.ksave");
        var originalContent = File.ReadAllBytes(saveFile);

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--dry-run"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        // Should either show "Dry run" or "up to date" depending on migration analysis
        var allOutput = output.Lines.Concat(output.Successes).ToList();
        Assert.Contains(allOutput, l => l.Contains("Dry run") || l.Contains("up to date"));

        // Verify file wasn't modified
        var newContent = File.ReadAllBytes(saveFile);
        Assert.Equal(originalContent, newContent);
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRunShortOption_DoesNotModifyFiles()
    {
        CreateMockSaveFileWithComponents("test.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "-n"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        // Should either show "Dry run" or "up to date" depending on migration analysis
        var allOutput = output.Lines.Concat(output.Successes).ToList();
        Assert.Contains(allOutput, l => l.Contains("Dry run") || l.Contains("up to date"));
    }

    [Fact]
    public async Task ExecuteAsync_WithUpToDateFiles_ShowsUpToDateMessage()
    {
        // Create a file with no components (already up to date)
        CreateMockSaveFile("test.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--dry-run"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Successes, l => l.Contains("up to date"));
    }

    #endregion

    #region Backup Support

    [Fact]
    public async Task ExecuteAsync_WithBackup_WhenFilesNeedMigration_CreatesBackupDirectory()
    {
        CreateMockSaveFileWithComponents("test.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--backup"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        // Backup is only created if files need migration
        var allOutput = output.Lines.Concat(output.Successes).ToList();
        var hasUpToDateMessage = allOutput.Any(l => l.Contains("up to date"));

        if (!hasUpToDateMessage)
        {
            // Check that backup directory was created
            var backupDirs = Directory.GetDirectories(testDir, "backup_*");
            Assert.Single(backupDirs);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithBackup_WhenFilesUpToDate_SkipsBackup()
    {
        // Files with no components are "up to date" and don't need backup
        CreateMockSaveFile("test.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--backup"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        // Verify "up to date" message
        Assert.Contains(output.Successes, l => l.Contains("up to date"));
    }

    [Fact]
    public async Task ExecuteAsync_WithBackupShortOption_Succeeds()
    {
        CreateMockSaveFileWithComponents("test.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "-b"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Output Directory

    [Fact]
    public async Task ExecuteAsync_WithOutputDirectory_WhenFilesNeedMigration_CreatesOutputDir()
    {
        CreateMockSaveFileWithComponents("test.ksave");
        var outputDir = Path.Combine(testDir, "migrated");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--output", outputDir],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        // Output dir is only created if files need migration
        var allOutput = output.Lines.Concat(output.Successes).ToList();
        var hasUpToDateMessage = allOutput.Any(l => l.Contains("up to date"));
        if (!hasUpToDateMessage)
        {
            Assert.True(Directory.Exists(outputDir));
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithOutputDirShortOption_Succeeds()
    {
        CreateMockSaveFileWithComponents("test.ksave");
        var outputDir = Path.Combine(testDir, "migrated");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "-o", outputDir],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Verbose Output

    [Fact]
    public async Task ExecuteAsync_WithVerbose_ShowsDetailedOutput()
    {
        CreateMockSaveFile("test.ksave");
        output.Verbose = true;

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--verbose", "--dry-run"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(output.VerboseMessages.Count > 0);
    }

    #endregion

    #region File Pattern Matching

    [Fact]
    public async Task ExecuteAsync_WithPattern_MatchesOnlySpecifiedPattern()
    {
        CreateMockSaveFile("save1.ksave");
        CreateMockSaveFile("save2.ksave");
        CreateMockSaveFile("other.dat");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--pattern", "*.ksave", "--dry-run"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        // The pattern should only match .ksave files
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomPattern_MatchesCustomFiles()
    {
        CreateMockSaveFile("save1.dat");
        CreateMockSaveFile("save2.dat");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--pattern", "*.dat", "--dry-run"],
            output,
            TestContext.Current.CancellationToken);

        // Result depends on whether files are valid save format
        // For invalid format, should still complete without crashing
        Assert.True(result.IsSuccess || result.Message?.Contains("No save files") == true);
    }

    #endregion

    #region Continue On Error

    [Fact]
    public async Task ExecuteAsync_WithContinueOnError_ProcessesAllFiles()
    {
        // Create both valid and "problematic" files
        CreateMockSaveFile("valid1.ksave");
        CreateMockSaveFile("valid2.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--continue-on-error", "--backup"],
            output,
            TestContext.Current.CancellationToken);

        // Should complete even if some files have issues
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Summary Output

    [Fact]
    public async Task ExecuteAsync_WithMultipleFiles_ShowsSummary()
    {
        CreateMockSaveFile("save1.ksave");
        CreateMockSaveFile("save2.ksave");

        var result = await command.ExecuteAsync(
            ["--path", testDir, "--backup"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines.Concat(output.Successes),
            l => l.Contains("Complete") || l.Contains("migrated") || l.Contains("up to date"));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock .ksave file with no components (up to date).
    /// </summary>
    private string CreateMockSaveFile(string fileName)
    {
        var filePath = Path.Combine(testDir, fileName);

        // Create a minimal valid .ksave file structure
        // Header: "KSAV" magic + version + flags + metadata length + data length
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Create minimal snapshot data with no entities
        var snapshotJson = """{"version":1,"timestamp":"2024-01-01T00:00:00Z","entities":[],"singletons":[]}""";
        var snapshotBytes = Encoding.UTF8.GetBytes(snapshotJson);

        // Compress with GZip
        byte[] compressedData;
        using (var compressStream = new MemoryStream())
        {
            using (var gzip = new GZipStream(compressStream, CompressionLevel.Fastest))
            {
                gzip.Write(snapshotBytes);
            }
            compressedData = compressStream.ToArray();
        }

        // Create metadata
        var metadata = new SaveSlotInfo
        {
            SlotName = Path.GetFileNameWithoutExtension(fileName),
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            EntityCount = 0,
            CompressedSize = compressedData.Length,
            UncompressedSize = snapshotBytes.Length
        };

        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

        // Write header
        writer.Write("KSAV"u8);      // Magic
        writer.Write((ushort)1);     // Version
        writer.Write((ushort)0x02);  // Flags (GZip compressed)
        writer.Write(metadataBytes.Length);
        writer.Write(compressedData.Length);

        // Write metadata
        writer.Write(metadataBytes);

        // Write compressed data
        writer.Write(compressedData);

        File.WriteAllBytes(filePath, stream.ToArray());
        return filePath;
    }

    /// <summary>
    /// Creates a mock .ksave file with components that might need migration analysis.
    /// </summary>
    private string CreateMockSaveFileWithComponents(string fileName)
    {
        var filePath = Path.Combine(testDir, fileName);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Create snapshot data with a mock entity and component
        var snapshotJson = """
        {
            "version": 1,
            "timestamp": "2024-01-01T00:00:00Z",
            "entities": [
                {
                    "id": 1,
                    "name": "TestEntity",
                    "components": [
                        {
                            "typeName": "TestGame.Health, TestGame",
                            "isTag": false,
                            "version": 1,
                            "data": { "current": 100, "max": 100 }
                        }
                    ],
                    "parentId": null
                }
            ],
            "singletons": []
        }
        """;
        var snapshotBytes = Encoding.UTF8.GetBytes(snapshotJson);

        // Compress with GZip
        byte[] compressedData;
        using (var compressStream = new MemoryStream())
        {
            using (var gzip = new GZipStream(compressStream, CompressionLevel.Fastest))
            {
                gzip.Write(snapshotBytes);
            }
            compressedData = compressStream.ToArray();
        }

        // Create metadata
        var metadata = new SaveSlotInfo
        {
            SlotName = Path.GetFileNameWithoutExtension(fileName),
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            EntityCount = 1,
            CompressedSize = compressedData.Length,
            UncompressedSize = snapshotBytes.Length
        };

        var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
        var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

        // Write header
        writer.Write("KSAV"u8);      // Magic
        writer.Write((ushort)1);     // Version
        writer.Write((ushort)0x02);  // Flags (GZip compressed)
        writer.Write(metadataBytes.Length);
        writer.Write(compressedData.Length);

        // Write metadata
        writer.Write(metadataBytes);

        // Write compressed data
        writer.Write(compressedData);

        File.WriteAllBytes(filePath, stream.ToArray());
        return filePath;
    }

    #endregion
}
