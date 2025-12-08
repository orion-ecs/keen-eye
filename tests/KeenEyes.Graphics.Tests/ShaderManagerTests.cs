using System.Numerics;
using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the ShaderManager class.
/// </summary>
public class ShaderManagerTests : IDisposable
{
    private readonly MockGraphicsDevice device;
    private readonly ShaderManager manager;

    private const string VertexShader = @"#version 330 core
layout (location = 0) in vec3 aPosition;
void main() { gl_Position = vec4(aPosition, 1.0); }";

    private const string FragmentShader = @"#version 330 core
out vec4 FragColor;
void main() { FragColor = vec4(1.0); }";

    public ShaderManagerTests()
    {
        device = new MockGraphicsDevice();
        manager = new ShaderManager { Device = device };
    }

    public void Dispose()
    {
        manager.Dispose();
        device.Dispose();
    }

    #region CreateShader Tests

    [Fact]
    public void CreateShader_WithValidSource_ReturnsPositiveId()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        Assert.True(shaderId > 0);
    }

    [Fact]
    public void CreateShader_CreatesProgram()
    {
        manager.CreateShader(VertexShader, FragmentShader);

        Assert.Single(device.GeneratedPrograms);
    }

    [Fact]
    public void CreateShader_CreatesVertexAndFragmentShaders()
    {
        manager.CreateShader(VertexShader, FragmentShader);

        Assert.Equal(2, device.GeneratedShaders.Count);
    }

    [Fact]
    public void CreateShader_CompilesShaders()
    {
        manager.CreateShader(VertexShader, FragmentShader);

        var compileCalls = device.Calls.Count(c => c.StartsWith("CompileShader"));
        Assert.Equal(2, compileCalls);
    }

    [Fact]
    public void CreateShader_LinksProgram()
    {
        manager.CreateShader(VertexShader, FragmentShader);

        Assert.Contains(device.Calls, c => c.StartsWith("LinkProgram"));
    }

    [Fact]
    public void CreateShader_DetachesShaders()
    {
        manager.CreateShader(VertexShader, FragmentShader);

        var detachCalls = device.Calls.Count(c => c.StartsWith("DetachShader"));
        Assert.Equal(2, detachCalls);
    }

    [Fact]
    public void CreateShader_DeletesShaderObjects()
    {
        manager.CreateShader(VertexShader, FragmentShader);

        Assert.Equal(2, device.DeletedShaders.Count);
    }

    [Fact]
    public void CreateShader_WithCompileError_ThrowsException()
    {
        device.ShaderCompileSuccess = false;
        device.InfoLog = "Error: syntax error";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            manager.CreateShader(VertexShader, FragmentShader));

        Assert.Contains("vertex", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void CreateShader_WithLinkError_ThrowsException()
    {
        device.ProgramLinkSuccess = false;
        device.InfoLog = "Error: link error";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            manager.CreateShader(VertexShader, FragmentShader));

        Assert.Contains("link", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void CreateShader_WithoutDevice_ThrowsInvalidOperationException()
    {
        var managerWithoutDevice = new ShaderManager();

        Assert.Throws<InvalidOperationException>(() =>
            managerWithoutDevice.CreateShader(VertexShader, FragmentShader));
    }

    [Fact]
    public void CreateShader_MultipleCalls_ReturnsUniqueIds()
    {
        int id1 = manager.CreateShader(VertexShader, FragmentShader);
        int id2 = manager.CreateShader(VertexShader, FragmentShader);

        Assert.NotEqual(id1, id2);
    }

    #endregion

    #region GetShader Tests

    [Fact]
    public void GetShader_WithValidId_ReturnsShaderData()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);
        var shaderData = manager.GetShader(shaderId);

        Assert.NotNull(shaderData);
    }

    [Fact]
    public void GetShader_ReturnsCorrectHandle()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);
        var shaderData = manager.GetShader(shaderId);

        Assert.NotNull(shaderData);
        Assert.True(shaderData.Handle > 0);
    }

    [Fact]
    public void GetShader_WithInvalidId_ReturnsNull()
    {
        var shaderData = manager.GetShader(999);

        Assert.Null(shaderData);
    }

    [Fact]
    public void GetShader_WithZeroId_ReturnsNull()
    {
        var shaderData = manager.GetShader(0);

        Assert.Null(shaderData);
    }

    #endregion

    #region SetUniform Tests

    [Fact]
    public void SetUniform_WithFloat_SetsValue()
    {
        device.UniformLocations["uFloat"] = 0;
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        manager.SetUniform(shaderId, "uFloat", 1.5f);

        Assert.Contains(device.Calls, c => c.Contains("Uniform1") && c.Contains("1.5"));
    }

    [Fact]
    public void SetUniform_WithInt_SetsValue()
    {
        device.UniformLocations["uInt"] = 1;
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        manager.SetUniform(shaderId, "uInt", 42);

        Assert.Contains(device.Calls, c => c.Contains("Uniform1") && c.Contains("42"));
    }

    [Fact]
    public void SetUniform_WithVector3_SetsValue()
    {
        device.UniformLocations["uVec3"] = 2;
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        manager.SetUniform(shaderId, "uVec3", new Vector3(1f, 2f, 3f));

        Assert.Contains(device.Calls, c => c.Contains("Uniform3") && c.Contains("1") && c.Contains("2") && c.Contains("3"));
    }

    [Fact]
    public void SetUniform_WithVector4_SetsValue()
    {
        device.UniformLocations["uVec4"] = 3;
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        manager.SetUniform(shaderId, "uVec4", new Vector4(1f, 2f, 3f, 4f));

        Assert.Contains(device.Calls, c => c.Contains("Uniform4"));
    }

    [Fact]
    public void SetUniform_WithMatrix4x4_SetsValue()
    {
        device.UniformLocations["uMat4"] = 4;
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        manager.SetUniform(shaderId, "uMat4", Matrix4x4.Identity);

        Assert.Contains(device.Calls, c => c.Contains("UniformMatrix4"));
    }

    [Fact]
    public void SetUniform_WithUnknownName_DoesNotThrow()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);

        // Unknown uniform location returns -1, should be silently ignored
        var exception = Record.Exception(() =>
            manager.SetUniform(shaderId, "uUnknown", 1.0f));

        Assert.Null(exception);
    }

    [Fact]
    public void SetUniform_CachesUniformLocations()
    {
        device.UniformLocations["uFloat"] = 0;
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);
        device.Calls.Clear();

        // Set same uniform twice
        manager.SetUniform(shaderId, "uFloat", 1.0f);
        manager.SetUniform(shaderId, "uFloat", 2.0f);

        // Should only look up location once
        var locationCalls = device.Calls.Count(c => c.Contains("GetUniformLocation"));
        Assert.Equal(1, locationCalls);
    }

    #endregion

    #region DeleteShader Tests

    [Fact]
    public void DeleteShader_WithValidId_ReturnsTrue()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);
        bool deleted = manager.DeleteShader(shaderId);

        Assert.True(deleted);
    }

    [Fact]
    public void DeleteShader_WithInvalidId_ReturnsFalse()
    {
        bool deleted = manager.DeleteShader(999);

        Assert.False(deleted);
    }

    [Fact]
    public void DeleteShader_DeletesGPUProgram()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);
        manager.DeleteShader(shaderId);

        Assert.Single(device.DeletedPrograms);
    }

    [Fact]
    public void DeleteShader_MakesShaderUnavailable()
    {
        int shaderId = manager.CreateShader(VertexShader, FragmentShader);
        manager.DeleteShader(shaderId);

        Assert.Null(manager.GetShader(shaderId));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DeletesAllShaders()
    {
        using var testManager = new ShaderManager { Device = device };
        testManager.CreateShader(VertexShader, FragmentShader);
        testManager.CreateShader(VertexShader, FragmentShader);
        device.Reset();

        using var testManager2 = new ShaderManager { Device = device };
        testManager2.CreateShader(VertexShader, FragmentShader);
        testManager2.CreateShader(VertexShader, FragmentShader);
        testManager2.Dispose();

        Assert.Equal(2, device.DeletedPrograms.Count);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var testManager = new ShaderManager { Device = device };
        testManager.CreateShader(VertexShader, FragmentShader);

        // Should not throw
        testManager.Dispose();
        testManager.Dispose();
    }

    #endregion
}
