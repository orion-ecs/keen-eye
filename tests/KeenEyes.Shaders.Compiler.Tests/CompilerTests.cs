namespace KeenEyes.Shaders.Compiler.Tests;

public class CompilerTests
{
    [Fact]
    public void CompileAndGenerate_SimpleComputeShader_GeneratesGlslAndCSharp()
    {
        var source = @"
            compute UpdatePhysics {
                query {
                    write Position
                    read Velocity
                }
                params {
                    deltaTime: float
                }
                execute() {
                    Position.x += Velocity.x * deltaTime;
                    Position.y += Velocity.y * deltaTime;
                    Position.z += Velocity.z * deltaTime;
                }
            }
        ";

        var compiler = new KeslCompiler { Namespace = "MyGame.Shaders" };
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors, $"Compilation errors: {string.Join(", ", output.Diagnostics.Select(d => d.Message))}");
        Assert.Single(output.Shaders);

        var shader = output.Shaders[0];
        Assert.Equal("UpdatePhysics", shader.ShaderName);
        Assert.Equal("UpdatePhysics.comp.glsl", shader.GlslFileName);
        Assert.Equal("UpdatePhysicsShader.g.cs", shader.CSharpFileName);

        // Check GLSL output
        Assert.Contains("#version 450", shader.GlslCode);
        Assert.Contains("layout(std430, binding = 0)", shader.GlslCode);
        Assert.Contains("layout(std430, binding = 1) readonly", shader.GlslCode);
        Assert.Contains("uniform float deltaTime", shader.GlslCode);
        Assert.Contains("uniform uint entityCount", shader.GlslCode);
        // Binary expressions are wrapped in parens
        Assert.Contains("Position[idx].x += (Velocity[idx].x * deltaTime)", shader.GlslCode);

        // Check C# output
        Assert.Contains("namespace MyGame.Shaders", shader.CSharpCode);
        Assert.Contains("public sealed partial class UpdatePhysicsShader", shader.CSharpCode);
        Assert.Contains("IGpuComputeSystem", shader.CSharpCode);
        Assert.Contains("public void Execute(World world, float deltaTime)", shader.CSharpCode);
        Assert.Contains(".With<Position>()", shader.CSharpCode);
        Assert.Contains(".With<Velocity>()", shader.CSharpCode);
    }

    [Fact]
    public void CompileAndGenerate_WithControlFlow_GeneratesCorrectGlsl()
    {
        var source = @"
            compute ConditionalUpdate {
                query {
                    write Position
                }
                execute() {
                    if (Position.x > 100) {
                        Position.x = 100;
                    }
                }
            }
        ";

        var compiler = new KeslCompiler();
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors);
        // Binary expressions are wrapped in parens
        Assert.Contains("if ((Position[idx].x > 100))", output.Shaders[0].GlslCode);
        Assert.Contains("Position[idx].x = 100", output.Shaders[0].GlslCode);
    }

    [Fact]
    public void CompileAndGenerate_WithForLoop_GeneratesCorrectGlsl()
    {
        var source = @"
            compute LoopUpdate {
                query {
                    write Position
                }
                execute() {
                    for (i: 0..10) {
                        Position.x = Position.x + 1;
                    }
                }
            }
        ";

        var compiler = new KeslCompiler();
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors);
        Assert.Contains("for (int i = 0; i < 10; i++)", output.Shaders[0].GlslCode);
    }

    [Fact]
    public void CompileAndGenerate_WithMathFunctions_GeneratesCorrectGlsl()
    {
        var source = @"
            compute MathShader {
                query {
                    write Position
                }
                execute() {
                    Position.x = sqrt(Position.y);
                    Position.y = max(Position.x, 0);
                    Position.z = clamp(Position.z, 0, 1);
                }
            }
        ";

        var compiler = new KeslCompiler();
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors);
        Assert.Contains("sqrt(Position[idx].y)", output.Shaders[0].GlslCode);
        Assert.Contains("max(Position[idx].x, 0)", output.Shaders[0].GlslCode);
        Assert.Contains("clamp(Position[idx].z, 0, 1)", output.Shaders[0].GlslCode);
    }

    [Fact]
    public void CompileAndGenerate_WithWithout_GeneratesCorrectQuery()
    {
        var source = @"
            compute FilteredUpdate {
                query {
                    write Position
                    without Frozen
                }
                execute() {
                    Position.x = 0;
                }
            }
        ";

        var compiler = new KeslCompiler();
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors);

        // Check that 'without' doesn't generate a buffer
        Assert.DoesNotContain("FrozenBuffer", output.Shaders[0].GlslCode);

        // Check that C# includes the Without clause
        Assert.Contains(".Without<Frozen>()", output.Shaders[0].CSharpCode);
    }

    [Fact]
    public void CompileAndGenerate_MultipleTypes_GeneratesCorrectGlslTypes()
    {
        var source = @"
            compute TypeTest {
                query {
                    write Position
                }
                params {
                    floatParam: float
                    vec2Param: float2
                    vec3Param: float3
                    vec4Param: float4
                    intParam: int
                    uintParam: uint
                    boolParam: bool
                }
                execute() {
                    Position.x = floatParam;
                }
            }
        ";

        var compiler = new KeslCompiler();
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors);
        Assert.Contains("uniform float floatParam", output.Shaders[0].GlslCode);
        Assert.Contains("uniform vec2 vec2Param", output.Shaders[0].GlslCode);
        Assert.Contains("uniform vec3 vec3Param", output.Shaders[0].GlslCode);
        Assert.Contains("uniform vec4 vec4Param", output.Shaders[0].GlslCode);
        Assert.Contains("uniform int intParam", output.Shaders[0].GlslCode);
        Assert.Contains("uniform uint uintParam", output.Shaders[0].GlslCode);
        Assert.Contains("uniform bool boolParam", output.Shaders[0].GlslCode);
    }

    [Fact]
    public void Compile_SyntaxError_ReturnsErrors()
    {
        var source = @"
            compute Broken {
                query {
                    write Position
                // Missing closing brace
                execute() {
                }
            }
        ";

        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void CompileAndGenerate_MultipleShaders_GeneratesAll()
    {
        var source = @"
            compute ShaderA {
                query { write Position }
                execute() { Position.x = 0; }
            }
            compute ShaderB {
                query { write Velocity }
                execute() { Velocity.x = 0; }
            }
        ";

        var compiler = new KeslCompiler();
        var output = compiler.CompileAndGenerate(source);

        Assert.False(output.HasErrors);
        Assert.Equal(2, output.Shaders.Count);
        Assert.Equal("ShaderA", output.Shaders[0].ShaderName);
        Assert.Equal("ShaderB", output.Shaders[1].ShaderName);
    }
}
