using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk.Rendering2D;
using KeenEyes.Graphics.Silk.Resources;
using KeenEyes.Graphics.Silk.Text;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests that shader compilation and linking failures report the driver's OpenGL and shading
/// language versions alongside the info log.
/// </summary>
/// <remarks>
/// A driver too old for the engine's <c>#version 330 core</c> shaders fails with an ordinary
/// compile error whose wording varies by vendor, so the info log alone rarely identifies the
/// cause. Naming the versions in the message makes a version mismatch self-evident even when the
/// minimum-version check has been bypassed - for example by a renderer constructed directly.
/// </remarks>
public class ShaderFailureDiagnosticsTests
{
    /// <summary>
    /// A driver whose OpenGL version predates GLSL 3.30: old enough that every built-in shader
    /// fails to compile, new enough to reach the compiler at all.
    /// </summary>
    private static MockGraphicsDevice CreateDeviceReportingGlsl130() => new()
    {
        DeviceInfo = new GraphicsDeviceInfo(
            Vendor: "Intel",
            Renderer: "Intel HD Graphics 3000",
            Version: "3.0.0 - Build 9.17.10.4459",
            ShadingLanguageVersion: "1.30",
            MajorVersion: 3,
            MinorVersion: 0)
    };

    private static void AssertNamesDriverVersions(string message)
    {
        Assert.Contains("3.0.0 - Build 9.17.10.4459", message, StringComparison.Ordinal);
        Assert.Contains("1.30", message, StringComparison.Ordinal);
        Assert.Contains("Intel HD Graphics 3000", message, StringComparison.Ordinal);
    }

    #region ShaderManager

    [Fact]
    public void CreateShader_WhenCompilationFails_MessageNamesDriverVersions()
    {
        using var device = CreateDeviceReportingGlsl130();
        device.ShouldFailShaderCompile = true;
        using var manager = new ShaderManager { Device = device };

        var exception = Assert.Throws<InvalidOperationException>(
            () => manager.CreateShader("#version 330 core\nvoid main() {}", "#version 330 core\nvoid main() {}"));

        Assert.Contains("Shader compilation failed", exception.Message, StringComparison.Ordinal);
        AssertNamesDriverVersions(exception.Message);
    }

    [Fact]
    public void CreateShader_WhenLinkingFails_MessageNamesDriverVersions()
    {
        using var device = CreateDeviceReportingGlsl130();
        device.ShouldFailProgramLink = true;
        using var manager = new ShaderManager { Device = device };

        var exception = Assert.Throws<InvalidOperationException>(
            () => manager.CreateShader("#version 330 core\nvoid main() {}", "#version 330 core\nvoid main() {}"));

        Assert.Contains("linking failed", exception.Message, StringComparison.Ordinal);
        AssertNamesDriverVersions(exception.Message);
    }

    #endregion

    #region Silk2DRenderer

    [Fact]
    public void Silk2DRenderer_WhenShaderCompilationFails_MessageNamesDriverVersions()
    {
        using var device = CreateDeviceReportingGlsl130();
        device.ShouldFailShaderCompile = true;
        using var textureManager = new TextureManager { Device = device };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new Silk2DRenderer(device, textureManager, 800f, 600f));

        Assert.Contains("2D vertex shader", exception.Message, StringComparison.Ordinal);
        AssertNamesDriverVersions(exception.Message);
    }

    [Fact]
    public void Silk2DRenderer_WhenShaderLinkingFails_MessageNamesDriverVersions()
    {
        using var device = CreateDeviceReportingGlsl130();
        device.ShouldFailProgramLink = true;
        using var textureManager = new TextureManager { Device = device };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new Silk2DRenderer(device, textureManager, 800f, 600f));

        Assert.Contains("link 2D shader program", exception.Message, StringComparison.Ordinal);
        AssertNamesDriverVersions(exception.Message);
    }

    #endregion

    #region FontStashRenderer

    [Fact]
    public void FontStashRenderer_WhenShaderCompilationFails_MessageNamesDriverVersions()
    {
        using var device = CreateDeviceReportingGlsl130();
        device.ShouldFailShaderCompile = true;

        var exception = Assert.Throws<InvalidOperationException>(
            () => new FontStashRenderer(device, new FontStashTextureManager(device), 800f, 600f));

        Assert.Contains("text vertex shader", exception.Message, StringComparison.Ordinal);
        AssertNamesDriverVersions(exception.Message);
    }

    #endregion
}
