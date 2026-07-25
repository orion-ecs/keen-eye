using KeenEyes.Graphics.Silk.Backend;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="GlVersionString"/>, the fallback that reads the OpenGL version out of
/// the driver's <c>GL_VERSION</c> string.
/// </summary>
/// <remarks>
/// This parser is the only version source available on drivers older than OpenGL 3.0, which are
/// exactly the ones the capability check exists to reject, so it has to cope with every shape
/// real drivers emit: bare numbers, vendor suffixes, build tags, and API-name prefixes.
/// </remarks>
public class GlVersionStringTests
{
    [Theory]
    // Desktop OpenGL with a vendor suffix.
    [InlineData("4.6.0 NVIDIA 551.86", 4, 6)]
    [InlineData("4.6.0 Compatibility Profile Context 23.20.15017.4003", 4, 6)]
    [InlineData("2.1 Mesa 20.0.8", 2, 1)]
    [InlineData("4.6 (Core Profile) Mesa 24.0.3", 4, 6)]
    // The software fallback this whole feature exists to diagnose.
    [InlineData("1.1.0", 1, 1)]
    // Intel drivers append a build tag whose numbers must not win over the leading version.
    [InlineData("3.3.0 - Build 31.0.101.2111", 3, 3)]
    // OpenGL ES prefixes the numbers with the API name.
    [InlineData("OpenGL ES 3.2 NVIDIA 551.86", 3, 2)]
    [InlineData("OpenGL ES 2.0 (WebGL 1.0)", 2, 0)]
    // Two-digit components must not be truncated.
    [InlineData("10.12.0 Some Future Driver", 10, 12)]
    // A digit run with no ".<digit>" after it is skipped rather than mistaken for a version.
    [InlineData("WebGL2 3.1 Emulated", 3, 1)]
    public void Parse_WithRealWorldVersionString_ReturnsLeadingVersion(string versionString, int expectedMajor, int expectedMinor)
    {
        var (major, minor) = GlVersionString.Parse(versionString);

        Assert.Equal(expectedMajor, major);
        Assert.Equal(expectedMinor, minor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no digits at all")]
    [InlineData("4")]
    [InlineData("4.")]
    [InlineData(".6")]
    // Larger than int.MaxValue: unparseable rather than silently wrapped.
    [InlineData("99999999999999.1")]
    public void Parse_WithUnusableVersionString_ReturnsZeroSoEveryCheckFails(string? versionString)
    {
        var (major, minor) = GlVersionString.Parse(versionString);

        Assert.Equal(0, major);
        Assert.Equal(0, minor);
    }
}
