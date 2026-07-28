using System.Reflection;

namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Pins down the startup ordering the windowed entry point depends on: nothing between process
/// start and the game loop may be awaited.
/// </summary>
/// <remarks>
/// <para>
/// Regression coverage for #1364. An <c>await</c> in an async <c>Main</c> resumes the remainder of
/// the method on a thread-pool thread, and the remainder here includes <c>Run()</c>, which creates
/// the OS window. macOS/AppKit only permits window creation on the process main thread and
/// terminates the process otherwise, so on a Mac the sample died right after starting the
/// TestBridge.
/// </para>
/// <para>
/// The property cannot be observed at runtime - reaching <c>Run()</c> requires a display - so it is
/// asserted against the entry point's source, which the test project embeds. Comments are stripped
/// first so that prose about <c>await</c> and <c>Run()</c> (including the comment that explains why
/// this ordering matters) cannot decide the outcome.
/// </para>
/// </remarks>
public class StartupOrderingTests
{
    [Fact]
    public void WindowedStartup_DoesNotAwaitBeforeTheGameLoopStarts()
    {
        var code = StripComments(ReadEntryPointSource());

        var runLine = IndexOfLineContaining(code, ".Run()");
        Assert.True(runLine >= 0, "Expected the entry point to start the loop with '.Run()'.");

        var awaitLine = IndexOfLineWithAwait(code);

        Assert.True(
            awaitLine < 0 || awaitLine > runLine,
            $"An 'await' on source line {awaitLine + 1} precedes the '.Run()' call on line "
            + $"{runLine + 1}. The continuation after that await resumes on a thread-pool thread, "
            + "so the window would be created off the process main thread and macOS would abort "
            + "the process. Block on the work instead (GetAwaiter().GetResult()), or move it after "
            + "Run() returns.");
    }

    [Fact]
    public void WindowedStartup_StillStartsTheTestBridgeBeforeTheGameLoop()
    {
        var code = StripComments(ReadEntryPointSource());

        var startLine = IndexOfLineContaining(code, "StartAsync()");
        var runLine = IndexOfLineContaining(code, ".Run()");

        // The fix must not have been "delete the bridge": it still starts, just synchronously.
        Assert.True(startLine >= 0, "Expected the entry point to start the TestBridge server.");
        Assert.True(
            startLine < runLine,
            "Expected the TestBridge to start before the game loop, as it did before #1364.");
    }

    [Fact]
    public void WindowedStartup_StopsTheTestBridgeAfterTheGameLoop()
    {
        var code = StripComments(ReadEntryPointSource());

        var runLine = IndexOfLineContaining(code, ".Run()");
        var stopLine = IndexOfLineContaining(code, "StopAsync()");

        // Teardown may still be awaited: by then Run() has returned, so a thread-pool
        // continuation can no longer reach window creation.
        Assert.True(stopLine >= 0, "Expected the entry point to stop the TestBridge server.");
        Assert.True(
            stopLine > runLine,
            "Expected the TestBridge to be stopped after the game loop returns.");
    }

    private static string[] ReadEntryPointSource()
    {
        const string ResourceName = "KeenEyes.Sample.NovaFall.Tests.Program.cs.txt";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded entry-point source '{ResourceName}' is missing from the test assembly.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n');
    }

    /// <summary>
    /// Blanks out line comments so prose cannot satisfy or break a code-ordering assertion.
    /// </summary>
    private static string[] StripComments(string[] lines)
    {
        var stripped = new string[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            var commentStart = lines[i].IndexOf("//", StringComparison.Ordinal);
            stripped[i] = commentStart >= 0 ? lines[i][..commentStart] : lines[i];
        }

        return stripped;
    }

    private static int IndexOfLineContaining(string[] lines, string token)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(token, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the first line using <c>await</c> as a keyword rather than as part of an identifier.
    /// </summary>
    private static int IndexOfLineWithAwait(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var word in lines[i].Split(
                [' ', '\t', '(', ')', ';', '=', '{', '}', ','],
                StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Equals(word, "await", StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
