using System.Reflection;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Pins the four font-discovery call sites to the shared <see cref="SystemFonts"/> locator.
/// </summary>
/// <remarks>
/// <para>
/// Regression coverage for #1365. The same existence-only <c>FindSystemFont</c> helper was copied
/// into four files - <c>Sample.UI</c>, <c>Sample.Showcase</c>, <c>Sample.NovaFall</c>, and
/// <c>EditorApplication</c> - so the macOS bug had to be fixed four times or not at all. These
/// tests fail if a copy comes back.
/// </para>
/// <para>
/// The call sites live in projects this one cannot reference (two samples and the editor app), so
/// their sources are embedded at build time. That keeps the tests hermetic - MSBuild fails if a
/// path drifts, and the test cannot silently read a stale copy.
/// </para>
/// </remarks>
public class FontDiscoveryCallSiteTests
{
    /// <summary>
    /// The logical names of the embedded call-site sources, matching the test project's
    /// <c>EmbeddedResource</c> items.
    /// </summary>
    public static TheoryData<string> CallSites =>
    [
        "KeenEyes.Graphics.Tests.CallSites.Sample.UI.Program.cs.txt",
        "KeenEyes.Graphics.Tests.CallSites.Sample.Showcase.Program.cs.txt",
        "KeenEyes.Graphics.Tests.CallSites.Sample.NovaFall.Program.cs.txt",
        "KeenEyes.Graphics.Tests.CallSites.EditorApplication.cs.txt",
    ];

    [Theory]
    [MemberData(nameof(CallSites))]
    public void CallSite_ResolvesItsFontThroughTheSharedLocator(string resourceName)
    {
        var code = ReadEmbeddedSource(resourceName);

        Assert.Contains("SystemFonts.FindFirstUsable()", code, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CallSites))]
    public void CallSite_NoLongerDeclaresItsOwnFontSearch(string resourceName)
    {
        var code = ReadEmbeddedSource(resourceName);

        Assert.DoesNotContain(
            "FindSystemFont",
            code,
            StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CallSites))]
    public void CallSite_NoLongerHardcodesTheMacOsFontCollection(string resourceName)
    {
        var code = ReadEmbeddedSource(resourceName);

        // The exact path from the #1365 report. Any copy of it outside SystemFonts is a
        // duplicated candidate list growing back.
        Assert.DoesNotContain(
            "/System/Library/Fonts/Helvetica.ttc",
            code,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Proves the shared locator resolves on the machine running the suite, which is what all
    /// four call sites now depend on.
    /// </summary>
    [Fact]
    public void SharedLocator_ResolvesAFontOnAMachineWithSystemFontsInstalled()
    {
        var found = SystemFonts.FindFirstUsable();
        Assert.SkipWhen(found is null, "No system font is installed on this machine.");

        Assert.True(
            SystemFonts.IsUsable(found!),
            $"'{found}' was returned by the locator but does not pass its own usability check.");
    }

    private static string ReadEmbeddedSource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded source '{resourceName}' was not found. Check the EmbeddedResource "
                + "items in KeenEyes.Graphics.Tests.csproj.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
