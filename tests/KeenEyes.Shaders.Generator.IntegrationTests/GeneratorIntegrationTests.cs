using KeenEyes.Shaders;

namespace KeenEyes.Shaders.Generator.IntegrationTests;

/// <summary>
/// Integration tests verifying that the KESL source generator produces valid C# code.
/// </summary>
public class GeneratorIntegrationTests
{
    [Fact]
    public void GeneratedShaderClass_Exists()
    {
        // The source generator should create UpdatePhysicsShader from Physics.kesl
        var shaderType = typeof(UpdatePhysicsShader);

        Assert.NotNull(shaderType);
        Assert.Equal("UpdatePhysicsShader", shaderType.Name);
    }

    [Fact]
    public void GeneratedGlslSource_ContainsShaderCode()
    {
        // The source generator should create UpdatePhysicsGlslSource with the GLSL code
        var glslSource = UpdatePhysicsGlslSource.Source;

        Assert.NotNull(glslSource);
        Assert.Contains("#version 450", glslSource);
        Assert.Contains("Position", glslSource);
        Assert.Contains("Velocity", glslSource);
        Assert.Contains("deltaTime", glslSource);
    }

    [Fact]
    public void GeneratedShaderClass_HasExpectedMembers()
    {
        // Verify the shader class has the expected methods/properties
        var shaderType = typeof(UpdatePhysicsShader);

        // Should have a constructor that takes IGpuDevice
        var constructor = shaderType.GetConstructor([typeof(IGpuDevice)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void GeneratedShaderClass_CanBeInstantiated()
    {
        // Verify we can create an instance of the generated shader
        var device = new MockGpuDevice();
        var shader = new UpdatePhysicsShader(device);
        Assert.NotNull(shader);
    }

    [Fact]
    public void GeneratedShaderClass_ImplementsIGpuComputeSystem()
    {
        // Verify the shader implements the GPU compute system interface
        var shaderType = typeof(UpdatePhysicsShader);
        Assert.True(typeof(IGpuComputeSystem).IsAssignableFrom(shaderType));
    }
}
