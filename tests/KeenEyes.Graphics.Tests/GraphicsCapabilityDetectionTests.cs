using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Silk.Backend;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

// This project carries a local mock of the same name; the shared testing double is the one
// whose reported driver version can be spoofed.
using MockGraphicsDevice = KeenEyes.Testing.Graphics.MockGraphicsDevice;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests that graphics initialization detects the driver's OpenGL version and refuses to run on
/// one below the engine's minimum.
/// </summary>
/// <remarks>
/// A machine with no vendor GPU driver installed falls back to the Microsoft Basic Display
/// Adapter, which reports OpenGL 1.1. Every built-in shader is <c>#version 330 core</c>, so such
/// a driver cannot work - but before this check the failure surfaced as an opaque shader
/// compilation error deep inside initialization, with the actual OpenGL version never reported
/// anywhere.
/// </remarks>
public class GraphicsCapabilityDetectionTests
{
    /// <summary>
    /// The driver a Windows machine with no vendor GPU driver installed reports.
    /// </summary>
    private static GraphicsDeviceInfo BasicRenderDriver => new(
        Vendor: "Microsoft Corporation",
        Renderer: "Microsoft Basic Render Driver",
        Version: "1.1.0",
        ShadingLanguageVersion: "",
        MajorVersion: 1,
        MinorVersion: 1);

    /// <summary>
    /// Builds a world with the graphics plugin installed but its device not yet bound, which is
    /// the state a real game is in the instant its window finishes loading.
    /// </summary>
    private static (World World, SilkGraphicsContext Context) CreateUnboundGraphics()
    {
        var world = new World();
        world.SetExtension<ISilkWindowProvider>(new MockSilkWindowProvider());
        world.InstallPlugin(new SilkGraphicsPlugin());

        return (world, world.GetExtension<SilkGraphicsContext>());
    }

    #region Minimum version gate

    [Fact]
    public void InitializeResources_WithDriverBelowMinimum_ThrowsUnsupportedGraphicsDeviceException()
    {
        var (world, context) = CreateUnboundGraphics();
        using (world)
        {
            using var device = new MockGraphicsDevice { DeviceInfo = BasicRenderDriver };

            var exception = Assert.Throws<UnsupportedGraphicsDeviceException>(
                () => context.InitializeResources(device));

            Assert.Equal(BasicRenderDriver, exception.DeviceInfo);
        }
    }

    [Fact]
    public void InitializeResources_WithDriverBelowMinimum_MessageNamesDetectedAndRequiredVersions()
    {
        var (world, context) = CreateUnboundGraphics();
        using (world)
        {
            using var device = new MockGraphicsDevice { DeviceInfo = BasicRenderDriver };

            var exception = Assert.Throws<UnsupportedGraphicsDeviceException>(
                () => context.InitializeResources(device));

            // What the machine has, verbatim and parsed.
            Assert.Contains("1.1", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Microsoft Basic Render Driver", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Microsoft Corporation", exception.Message, StringComparison.Ordinal);

            // What it needs, and why.
            Assert.Contains("3.3", exception.Message, StringComparison.Ordinal);
            Assert.Contains("#version 330 core", exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void InitializeResources_WithDriverBelowMinimum_MessageNamesTheLikelyRemediation()
    {
        var (world, context) = CreateUnboundGraphics();
        using (world)
        {
            using var device = new MockGraphicsDevice { DeviceInfo = BasicRenderDriver };

            var exception = Assert.Throws<UnsupportedGraphicsDeviceException>(
                () => context.InitializeResources(device));

            Assert.Contains("Install the graphics driver", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Microsoft Basic Display Adapter", exception.Message, StringComparison.Ordinal);
            Assert.Contains("remote-desktop", exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void InitializeResources_WithDriverBelowMinimum_FailsBeforeCreatingAnyShader()
    {
        var (world, context) = CreateUnboundGraphics();
        using (world)
        {
            using var device = new MockGraphicsDevice { DeviceInfo = BasicRenderDriver };

            Assert.Throws<UnsupportedGraphicsDeviceException>(() => context.InitializeResources(device));

            // The whole point of the check: the driver is rejected by name instead of failing
            // later as an unexplained shader compilation error.
            Assert.Empty(device.Shaders);
            Assert.Empty(device.Programs);
            Assert.False(context.IsInitialized);
        }
    }

    [Fact]
    public void InitializeResources_WithModernDriver_InitializesNormally()
    {
        var (world, context) = CreateUnboundGraphics();
        using (world)
        {
            // The mock defaults to OpenGL 4.6, so this is the ordinary path - the guard must not
            // pass by rejecting everything.
            using var device = new MockGraphicsDevice();

            context.InitializeResources(device);

            Assert.True(context.IsInitialized);
            Assert.NotEmpty(device.Programs);
        }
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(3, 4)]
    [InlineData(4, 6)]
    public void EnsureMinimumVersion_AtOrAboveMinimum_DoesNotThrow(int major, int minor)
    {
        var info = new GraphicsDeviceInfo("Vendor", "Renderer", $"{major}.{minor}.0", "3.30", major, minor);

        Assert.Null(Record.Exception(() => GlCapabilities.EnsureMinimumVersion(info)));
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(3, 0)]
    [InlineData(2, 1)]
    [InlineData(1, 1)]
    // An unparseable version string leaves 0.0, which must fail rather than pass by accident.
    [InlineData(0, 0)]
    public void EnsureMinimumVersion_BelowMinimum_Throws(int major, int minor)
    {
        var info = new GraphicsDeviceInfo("Vendor", "Renderer", $"{major}.{minor}.0", "", major, minor);

        Assert.Throws<UnsupportedGraphicsDeviceException>(() => GlCapabilities.EnsureMinimumVersion(info));
    }

    #endregion

    #region GraphicsDeviceInfo

    [Theory]
    [InlineData(4, 6, 3, 3, true)]
    [InlineData(3, 3, 3, 3, true)]
    [InlineData(3, 2, 3, 3, false)]
    [InlineData(2, 9, 3, 3, false)]
    [InlineData(4, 0, 3, 3, true)]
    public void IsAtLeast_ComparesMajorBeforeMinor(int major, int minor, int requiredMajor, int requiredMinor, bool expected)
    {
        var info = new GraphicsDeviceInfo("Vendor", "Renderer", "irrelevant", "irrelevant", major, minor);

        Assert.Equal(expected, info.IsAtLeast(requiredMajor, requiredMinor));
    }

    [Fact]
    public void ToString_ReportsEveryDriverStringForLogsAndBugReports()
    {
        var info = new GraphicsDeviceInfo(
            "NVIDIA Corporation", "NVIDIA GeForce RTX 4070", "4.6.0 NVIDIA 551.86", "4.60", 4, 6);

        var summary = info.ToString();

        Assert.Contains("4.6.0 NVIDIA 551.86", summary, StringComparison.Ordinal);
        Assert.Contains("4.60", summary, StringComparison.Ordinal);
        Assert.Contains("NVIDIA GeForce RTX 4070", summary, StringComparison.Ordinal);
        Assert.Contains("NVIDIA Corporation", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_WithStringTheDriverDoesNotReport_SaysSoRatherThanShowingABlank()
    {
        // A pre-shader driver reports no shading language version at all.
        var summary = BasicRenderDriver.ToString();

        Assert.Contains("shading language unreported", summary, StringComparison.Ordinal);
    }

    #endregion
}
