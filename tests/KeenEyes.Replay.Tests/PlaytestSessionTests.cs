using System.IO.Compression;
using System.Text.Json;
using KeenEyes.Logging;
using KeenEyes.Logging.Providers;
using KeenEyes.Replay.Playtest;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for <see cref="PlaytestSession"/> and its bundle format.
/// </summary>
public sealed class PlaytestSessionTests
{
    private static readonly JsonSerializerOptions readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region Test Infrastructure

    /// <summary>
    /// Creates and manages a unique temporary directory, deleting it on disposal.
    /// </summary>
    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "keeneyes-playtest-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup; leaked temp files are acceptable in tests.
            }
        }
    }

    private static ReplayRecorder CreateRecorder(World world, ReplayOptions? options = null)
        => new(world, new MockComponentSerializer(), options);

    private static void DriveFrames(ReplayRecorder recorder, int count)
    {
        for (var i = 0; i < count; i++)
        {
            recorder.BeginFrame(0.016f);
            recorder.EndFrame(0.016f);
        }
    }

    private static byte[] ReadEntryBytes(string bundlePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(bundlePath);
        var entry = archive.GetEntry(entryName);
        Assert.NotNull(entry);

        using var entryStream = entry!.Open();
        using var buffer = new MemoryStream();
        entryStream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static bool EntryExists(string bundlePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(bundlePath);
        return archive.GetEntry(entryName) is not null;
    }

    private static T ReadEntryJson<T>(string bundlePath, string entryName)
    {
        var bytes = ReadEntryBytes(bundlePath, entryName);
        var value = JsonSerializer.Deserialize<T>(bytes, readOptions);
        Assert.NotNull(value);
        return value!;
    }

    #endregion

    #region Constructor and Guard Tests

    [Fact]
    public void Constructor_WithNullRecorder_ThrowsArgumentNullException()
    {
        using var temp = new TempDirectory();

        Assert.Throws<ArgumentNullException>(() => new PlaytestSession(null!, temp.Path));
    }

    [Fact]
    public void Constructor_WithBlankOutputDirectory_ThrowsArgumentException()
    {
        using var world = new World();
        var recorder = CreateRecorder(world);

        Assert.Throws<ArgumentException>(() => new PlaytestSession(recorder, "   "));
    }

    [Fact]
    public void StartSession_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var session = new PlaytestSession(CreateRecorder(world), temp.Path);

        session.StartSession("tester-1");

        Assert.Throws<InvalidOperationException>(() => session.StartSession("tester-2"));
    }

    [Fact]
    public void RecordFeedback_WhenNotStarted_ThrowsInvalidOperationException()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var session = new PlaytestSession(CreateRecorder(world), temp.Path);

        Assert.Throws<InvalidOperationException>(() => session.RecordFeedback("bug", "message"));
    }

    [Fact]
    public void CaptureCrashBundle_WithNullException_ThrowsArgumentNullException()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var session = new PlaytestSession(CreateRecorder(world), temp.Path);
        session.StartSession("tester-1");

        Assert.Throws<ArgumentNullException>(() => session.CaptureCrashBundle(null!));
    }

    #endregion

    #region Bundle Content Tests

    [Fact]
    public async Task EndSessionAsync_WithRecordedSession_ProducesWellFormedBundle()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var recorder = CreateRecorder(world);
        var session = new PlaytestSession(recorder, temp.Path);

        var metadata = new Dictionary<string, string> { ["build"] = "0.1.0-alpha" };
        session.StartSession("tester-42", metadata);
        DriveFrames(recorder, 10);
        session.RecordFeedback("bug", "The door on level 2 will not open.");

        var result = await session.EndSessionAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(result.BundlePath));

        // Manifest is well-formed and describes the session.
        var manifest = ReadEntryJson<PlaytestManifest>(result.BundlePath, "manifest.json");
        Assert.Equal(session.SessionId, manifest.SessionId);
        Assert.Equal("tester-42", manifest.PlaytesterId);
        Assert.False(manifest.HasCrash);
        Assert.True(manifest.EndedUtc >= manifest.StartedUtc);
        Assert.Equal("0.1.0-alpha", manifest.Metadata!["build"]);
        Assert.Contains("manifest.json", manifest.Entries);
        Assert.Contains("replay.kreplay", manifest.Entries);
        Assert.Contains("feedback.json", manifest.Entries);

        // Replay entry is a readable .kreplay file with the driven frames.
        var replayBytes = ReadEntryBytes(result.BundlePath, "replay.kreplay");
        Assert.True(ReplayFileFormat.IsValidFormat(replayBytes));
        var (_, replayData) = ReplayFileFormat.Read(replayBytes);
        Assert.Equal(10, replayData.FrameCount);

        // Feedback entry round-trips.
        var feedback = ReadEntryJson<List<PlaytestFeedback>>(result.BundlePath, "feedback.json");
        var entry = Assert.Single(feedback);
        Assert.Equal("bug", entry.Category);
        Assert.Equal("The door on level 2 will not open.", entry.Message);
    }

    [Fact]
    public async Task EndSessionAsync_WithNoData_ProducesValidMinimalBundle()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var session = new PlaytestSession(CreateRecorder(world), temp.Path);

        session.StartSession("tester-1");
        var result = await session.EndSessionAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(result.BundlePath));

        var manifest = ReadEntryJson<PlaytestManifest>(result.BundlePath, "manifest.json");
        Assert.Equal("tester-1", manifest.PlaytesterId);
        Assert.False(manifest.HasCrash);

        // Feedback entry is present but empty.
        var feedback = ReadEntryJson<List<PlaytestFeedback>>(result.BundlePath, "feedback.json");
        Assert.Empty(feedback);

        // No crash or logs entries in a no-op session.
        Assert.False(EntryExists(result.BundlePath, "crash.json"));
        Assert.False(EntryExists(result.BundlePath, "logs.json"));
    }

    [Fact]
    public async Task RecordFeedback_MultipleEntries_PreservesOrderAndTimestamps()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var session = new PlaytestSession(CreateRecorder(world), temp.Path);

        session.StartSession("tester-1");
        session.RecordFeedback("bug", "first");
        session.RecordFeedback("suggestion", "second");
        session.RecordFeedback("praise", "third");

        var result = await session.EndSessionAsync(TestContext.Current.CancellationToken);

        var feedback = ReadEntryJson<List<PlaytestFeedback>>(result.BundlePath, "feedback.json");
        Assert.Equal(3, feedback.Count);
        Assert.Equal(["first", "second", "third"], feedback.Select(f => f.Message));
        Assert.Equal(["bug", "suggestion", "praise"], feedback.Select(f => f.Category));

        // Timestamps are recorded in non-decreasing order.
        Assert.True(feedback[0].Timestamp <= feedback[1].Timestamp);
        Assert.True(feedback[1].Timestamp <= feedback[2].Timestamp);
    }

    [Fact]
    public async Task EndSessionAsync_WithLogSource_IncludesCapturedLogs()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var logProvider = new RingBufferLogProvider();
        logProvider.Log(LogLevel.Warning, "Gameplay", "low health", null);
        logProvider.Log(LogLevel.Error, "Physics", "penetration detected", null);

        var session = new PlaytestSession(CreateRecorder(world), temp.Path, logSource: logProvider);
        session.StartSession("tester-1");
        var result = await session.EndSessionAsync(TestContext.Current.CancellationToken);

        Assert.True(EntryExists(result.BundlePath, "logs.json"));
        var logs = ReadEntryJson<List<PlaytestLogEntry>>(result.BundlePath, "logs.json");
        Assert.Equal(2, logs.Count);
        Assert.Equal("Warning", logs[0].Level);
        Assert.Equal("low health", logs[0].Message);
        Assert.Equal("Physics", logs[1].Category);
    }

    #endregion

    #region Crash Capture Tests

    [Fact]
    public void CaptureCrashBundle_WithRingBuffer_FlushesBufferAndIncludesCrashDetails()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var options = new ReplayOptions { UseRingBuffer = true, MaxFrames = 5 };
        var recorder = CreateRecorder(world, options);
        var session = new PlaytestSession(recorder, temp.Path);

        session.StartSession("tester-1");

        // Drive more frames than the ring buffer holds so flushing keeps only the last 5.
        DriveFrames(recorder, 20);

        InvalidOperationException captured;
        try
        {
            throw new InvalidOperationException("boom during playtest");
        }
        catch (InvalidOperationException ex)
        {
            captured = ex;
        }

        var bundlePath = session.CaptureCrashBundle(captured);

        Assert.True(File.Exists(bundlePath));

        var manifest = ReadEntryJson<PlaytestManifest>(bundlePath, "manifest.json");
        Assert.True(manifest.HasCrash);
        Assert.Contains("crash.json", manifest.Entries);

        // Crash details carry the exception type, message, and stack trace.
        var crash = ReadEntryJson<PlaytestCrashInfo>(bundlePath, "crash.json");
        Assert.Equal(typeof(InvalidOperationException).FullName, crash.ExceptionType);
        Assert.Equal("boom during playtest", crash.Message);
        Assert.False(string.IsNullOrEmpty(crash.StackTrace));

        // The flushed ring buffer contains only the retained window of frames.
        var replayBytes = ReadEntryBytes(bundlePath, "replay.kreplay");
        var (_, replayData) = ReplayFileFormat.Read(replayBytes);
        Assert.Equal(5, replayData.FrameCount);
    }

    [Fact]
    public void CaptureCrashBundle_WithInnerException_CapturesInnerDetails()
    {
        using var world = new World();
        using var temp = new TempDirectory();
        var session = new PlaytestSession(CreateRecorder(world), temp.Path);
        session.StartSession("tester-1");

        var exception = new InvalidOperationException("outer", new ArgumentException("inner"));
        var bundlePath = session.CaptureCrashBundle(exception);

        var crash = ReadEntryJson<PlaytestCrashInfo>(bundlePath, "crash.json");
        Assert.NotNull(crash.InnerException);
        Assert.Equal(typeof(ArgumentException).FullName, crash.InnerException!.ExceptionType);
        Assert.Equal("inner", crash.InnerException.Message);
    }

    #endregion

    #region Upload Tests

    [Fact]
    public async Task EndSessionAsync_WithDirectoryUploadTarget_DeliversBundle()
    {
        using var world = new World();
        using var sourceDir = new TempDirectory();
        using var targetDir = new TempDirectory();

        var target = new DirectoryUploadTarget(targetDir.Path);
        var session = new PlaytestSession(CreateRecorder(world), sourceDir.Path, target);

        session.StartSession("tester-1");
        var result = await session.EndSessionAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Uploaded);
        Assert.Null(result.UploadError);

        var delivered = Path.Combine(targetDir.Path, Path.GetFileName(result.BundlePath));
        Assert.True(File.Exists(delivered));
    }

    [Fact]
    public async Task EndSessionAsync_WithNonexistentUploadDirectory_KeepsBundleAndSurfacesError()
    {
        using var world = new World();
        using var sourceDir = new TempDirectory();

        var missingDir = Path.Combine(Path.GetTempPath(), "keeneyes-playtest-tests", Guid.NewGuid().ToString("N"));
        var target = new DirectoryUploadTarget(missingDir);
        var session = new PlaytestSession(CreateRecorder(world), sourceDir.Path, target);

        session.StartSession("tester-1");
        var result = await session.EndSessionAsync(TestContext.Current.CancellationToken);

        // Upload failed, but the local bundle survives and the error is surfaced.
        Assert.False(result.Uploaded);
        Assert.NotNull(result.UploadError);
        Assert.IsType<DirectoryNotFoundException>(result.UploadError);
        Assert.True(File.Exists(result.BundlePath));
        Assert.False(Directory.Exists(missingDir));
    }

    [Fact]
    public async Task DirectoryUploadTarget_UploadAsync_CopiesBundlePreservingFileName()
    {
        using var sourceDir = new TempDirectory();
        using var targetDir = new TempDirectory();

        var bundlePath = Path.Combine(sourceDir.Path, "playtest-sample.zip");
        await File.WriteAllTextAsync(bundlePath, "bundle-contents", TestContext.Current.CancellationToken);

        var manifest = new PlaytestManifest
        {
            SessionId = Guid.NewGuid(),
            PlaytesterId = "tester-1",
            StartedUtc = DateTimeOffset.UtcNow,
            EndedUtc = DateTimeOffset.UtcNow,
            Entries = ["manifest.json"]
        };

        var target = new DirectoryUploadTarget(targetDir.Path);
        await target.UploadAsync(bundlePath, manifest, TestContext.Current.CancellationToken);

        var delivered = Path.Combine(targetDir.Path, "playtest-sample.zip");
        Assert.True(File.Exists(delivered));
        Assert.Equal("bundle-contents", await File.ReadAllTextAsync(delivered, TestContext.Current.CancellationToken));
    }

    #endregion
}
