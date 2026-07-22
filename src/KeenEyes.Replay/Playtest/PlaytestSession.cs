using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using KeenEyes.Logging;

namespace KeenEyes.Replay.Playtest;

/// <summary>
/// Orchestrates a local playtest session, bundling a crash replay, structured feedback, and
/// optionally captured logs into a single manifest'd archive.
/// </summary>
/// <remarks>
/// <para>
/// A session composes with a world's existing <see cref="ReplayRecorder"/> rather than
/// creating its own. The recorder is already wired into the world's frame loop by
/// <see cref="ReplayPlugin"/>, so reusing it is the only way the bundled replay reflects
/// actual gameplay. For crash replays, configure that recorder's
/// <see cref="ReplayOptions.UseRingBuffer"/> and <see cref="ReplayOptions.MaxFrames"/> so the
/// last N frames before a crash are retained; the session flushes that ring buffer when a
/// bundle is produced.
/// </para>
/// <para>
/// Crash capture is explicit: the game calls <see cref="CaptureCrashBundle(Exception)"/> from
/// its own exception handler. KeenEyes deliberately does not install a global
/// <see cref="AppDomain.UnhandledException"/> handler, keeping crash handling traceable and
/// under the application's control.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // During startup: install the replay plugin configured as a crash ring buffer.
/// var options = new ReplayOptions { UseRingBuffer = true, MaxFrames = 600 };
/// world.InstallPlugin(new ReplayPlugin(serializer, options));
/// var recorder = world.GetExtension&lt;ReplayRecorder&gt;();
///
/// var session = new PlaytestSession(recorder, "playtests", new DirectoryUploadTarget("//share/playtests"));
/// session.StartSession("tester-42");
///
/// try
/// {
///     // ... run the game loop; frames flow into the recorder automatically ...
///     session.RecordFeedback("bug", "The door on level 2 will not open.");
///     var result = await session.EndSessionAsync();
/// }
/// catch (Exception ex)
/// {
///     var bundlePath = session.CaptureCrashBundle(ex);
///     throw;
/// }
/// </code>
/// </example>
public sealed class PlaytestSession
{
    private const string ManifestEntryName = "manifest.json";
    private const string ReplayEntryName = "replay.kreplay";
    private const string FeedbackEntryName = "feedback.json";
    private const string LogsEntryName = "logs.json";
    private const string CrashEntryName = "crash.json";

    private readonly ReplayRecorder recorder;
    private readonly string outputDirectory;
    private readonly IPlaytestUploadTarget? uploadTarget;
    private readonly ILogQueryable? logSource;
    private readonly ReplayFileOptions? replayFileOptions;
    private readonly List<PlaytestFeedback> feedback = [];

    private Guid sessionId;
    private string? playtesterId;
    private IReadOnlyDictionary<string, string>? metadata;
    private DateTimeOffset startedUtc;
    private SessionState state = SessionState.NotStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaytestSession"/> class.
    /// </summary>
    /// <param name="recorder">
    /// The world's replay recorder, typically obtained via
    /// <c>world.GetExtension&lt;ReplayRecorder&gt;()</c> after installing <see cref="ReplayPlugin"/>.
    /// </param>
    /// <param name="outputDirectory">
    /// The directory where bundle archives are written. Created if it does not exist.
    /// </param>
    /// <param name="uploadTarget">
    /// An optional destination that <see cref="EndSessionAsync"/> uploads the bundle to.
    /// </param>
    /// <param name="logSource">
    /// An optional log source whose entries are captured into the bundle at write time.
    /// </param>
    /// <param name="replayFileOptions">
    /// Optional options controlling how the replay is encoded into the bundle.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="recorder"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="outputDirectory"/> is null or whitespace.
    /// </exception>
    public PlaytestSession(
        ReplayRecorder recorder,
        string outputDirectory,
        IPlaytestUploadTarget? uploadTarget = null,
        ILogQueryable? logSource = null,
        ReplayFileOptions? replayFileOptions = null)
    {
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        this.recorder = recorder;
        this.outputDirectory = outputDirectory;
        this.uploadTarget = uploadTarget;
        this.logSource = logSource;
        this.replayFileOptions = replayFileOptions;
    }

    /// <summary>
    /// Gets the unique identifier of the current session.
    /// </summary>
    /// <remarks>Meaningful only after <see cref="StartSession"/> has been called.</remarks>
    public Guid SessionId => sessionId;

    /// <summary>
    /// Gets a value indicating whether a session is currently active.
    /// </summary>
    public bool IsActive => state == SessionState.Active;

    /// <summary>
    /// Starts a new playtest session and begins replay recording.
    /// </summary>
    /// <param name="playtesterId">The identifier of the playtester producing the session.</param>
    /// <param name="metadata">Optional metadata recorded in the bundle manifest.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="playtesterId"/> is null or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a session is already active.
    /// </exception>
    public void StartSession(string playtesterId, IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playtesterId);

        if (state == SessionState.Active)
        {
            throw new InvalidOperationException("A playtest session is already active. Call EndSessionAsync first.");
        }

        sessionId = Guid.NewGuid();
        this.playtesterId = playtesterId;
        this.metadata = metadata;
        startedUtc = DateTimeOffset.UtcNow;
        feedback.Clear();
        state = SessionState.Active;

        recorder.StartRecording($"Playtest {sessionId}");
    }

    /// <summary>
    /// Records a timestamped feedback entry for the active session.
    /// </summary>
    /// <param name="category">The caller-defined category (for example "bug" or "suggestion").</param>
    /// <param name="message">The free-form feedback message.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="category"/> or <paramref name="message"/> is null or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no session is active.
    /// </exception>
    public void RecordFeedback(string category, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        EnsureActive();

        feedback.Add(new PlaytestFeedback
        {
            Timestamp = DateTimeOffset.UtcNow,
            Category = category,
            Message = message
        });
    }

    /// <summary>
    /// Ends the active session, writes its bundle, and uploads it if an upload target was
    /// configured.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>
    /// A result describing the local bundle path and the upload outcome. The local bundle is
    /// always written before any upload is attempted; an upload failure is surfaced via
    /// <see cref="PlaytestBundleResult.UploadError"/> without discarding the local bundle.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    public async Task<PlaytestBundleResult> EndSessionAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();

        var replay = recorder.StopRecording();
        var manifest = WriteBundle(replay, crash: null, out var bundlePath);
        state = SessionState.Ended;

        if (uploadTarget is null)
        {
            return new PlaytestBundleResult { BundlePath = bundlePath, Manifest = manifest };
        }

        try
        {
            await uploadTarget.UploadAsync(bundlePath, manifest, cancellationToken).ConfigureAwait(false);
            return new PlaytestBundleResult { BundlePath = bundlePath, Manifest = manifest, Uploaded = true };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PlaytestBundleResult { BundlePath = bundlePath, Manifest = manifest, UploadError = ex };
        }
    }

    /// <summary>
    /// Flushes the crash replay ring buffer and writes a bundle capturing the specified
    /// exception, then ends the session.
    /// </summary>
    /// <param name="exception">The exception that terminated the session.</param>
    /// <returns>The path to the local crash bundle archive.</returns>
    /// <remarks>
    /// <para>
    /// Call this from the application's exception handler. Crash bundles are written locally
    /// and, unlike <see cref="EndSessionAsync"/>, are not uploaded: uploading is best deferred
    /// to a subsequent process launch rather than attempted while the process is unwinding
    /// from an unhandled exception. The returned path can be handed to an
    /// <see cref="IPlaytestUploadTarget"/> later.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no session is active.</exception>
    public string CaptureCrashBundle(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        EnsureActive();

        var replay = recorder.StopRecording();
        WriteBundle(replay, crash: ToCrashInfo(exception), out var bundlePath);
        state = SessionState.Ended;

        return bundlePath;
    }

    private void EnsureActive()
    {
        if (state != SessionState.Active)
        {
            throw new InvalidOperationException("No playtest session is active. Call StartSession first.");
        }
    }

    private PlaytestManifest WriteBundle(ReplayData? replay, PlaytestCrashInfo? crash, out string bundlePath)
    {
        Directory.CreateDirectory(outputDirectory);
        bundlePath = Path.Combine(outputDirectory, $"playtest-{sessionId}.zip");

        var logs = CaptureLogs();

        // Build the entry inventory up front so the manifest can describe the whole archive.
        var entries = new List<string> { ManifestEntryName };
        if (replay is not null)
        {
            entries.Add(ReplayEntryName);
        }

        entries.Add(FeedbackEntryName);
        if (logs is not null)
        {
            entries.Add(LogsEntryName);
        }

        if (crash is not null)
        {
            entries.Add(CrashEntryName);
        }

        var manifest = new PlaytestManifest
        {
            SessionId = sessionId,
            PlaytesterId = playtesterId!,
            StartedUtc = startedUtc,
            EndedUtc = DateTimeOffset.UtcNow,
            EngineVersion = EngineVersion,
            GameVersion = GameVersion,
            HasCrash = crash is not null,
            Metadata = metadata,
            Entries = entries
        };

        using var stream = new FileStream(bundlePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        WriteJsonEntry(archive, ManifestEntryName, manifest, PlaytestJsonContext.Default.PlaytestManifest);

        if (replay is not null)
        {
            var replayBytes = ReplayFileFormat.Write(replay, replayFileOptions);
            WriteBytesEntry(archive, ReplayEntryName, replayBytes);
        }

        WriteJsonEntry<IReadOnlyList<PlaytestFeedback>>(archive, FeedbackEntryName, feedback, PlaytestJsonContext.Default.FeedbackList);

        if (logs is not null)
        {
            WriteJsonEntry(archive, LogsEntryName, logs, PlaytestJsonContext.Default.LogEntryList);
        }

        if (crash is not null)
        {
            WriteJsonEntry(archive, CrashEntryName, crash, PlaytestJsonContext.Default.PlaytestCrashInfo);
        }

        return manifest;
    }

    private IReadOnlyList<PlaytestLogEntry>? CaptureLogs()
    {
        if (logSource is null)
        {
            return null;
        }

        var entries = logSource.GetEntries();
        var projected = new List<PlaytestLogEntry>(entries.Count);
        foreach (var entry in entries)
        {
            projected.Add(new PlaytestLogEntry
            {
                Timestamp = entry.Timestamp,
                Level = entry.Level.ToString(),
                Category = entry.Category,
                Message = entry.Message
            });
        }

        return projected;
    }

    private static PlaytestCrashInfo ToCrashInfo(Exception exception)
    {
        return new PlaytestCrashInfo
        {
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException is { } inner ? ToCrashInfo(inner) : null
        };
    }

    private static void WriteJsonEntry<T>(ZipArchive archive, string entryName, T value, JsonTypeInfo<T> typeInfo)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
        entryStream.Write(bytes);
    }

    private static void WriteBytesEntry(ZipArchive archive, string entryName, byte[] bytes)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(bytes);
    }

    private static string? EngineVersion => typeof(PlaytestSession).Assembly.GetName().Version?.ToString();

    private static string? GameVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

    private enum SessionState
    {
        NotStarted,
        Active,
        Ended
    }
}
