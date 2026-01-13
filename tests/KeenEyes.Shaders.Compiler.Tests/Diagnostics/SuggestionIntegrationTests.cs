using KeenEyes.Shaders.Compiler.Diagnostics;
using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Shaders.Compiler.Tests.Diagnostics;

/// <summary>
/// Integration tests verifying that the compiler provides "Did you mean?" suggestions
/// when encountering typos in source code.
/// </summary>
public class SuggestionIntegrationTests
{
    #region Declaration Keyword Suggestions

    [Fact]
    public void Compile_MisspelledComponent_SuggestsComponent()
    {
        var result = KeslCompiler.Compile("compnent Position { x: float }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("component", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledCompute_SuggestsCompute()
    {
        var result = KeslCompiler.Compile("compue Test { query { write Position } execute() {} }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("compute", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledVertex_SuggestsVertex()
    {
        var result = KeslCompiler.Compile("vertx Test { in { pos: float3 @ 0 } out { color: float4 } execute() {} }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("vertex", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledFragment_SuggestsFragment()
    {
        var result = KeslCompiler.Compile("fragmnt Test { in { color: float4 } out { fragColor: float4 @ 0 } execute() {} }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("fragment", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledGeometry_SuggestsGeometry()
    {
        var result = KeslCompiler.Compile("geomety Test { layout { input: triangles output: triangle_strip max_vertices: 3 } in { pos: float3 } out { color: float4 } execute() {} }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("geometry", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledPipeline_SuggestsPipeline()
    {
        var result = KeslCompiler.Compile("pipelin Test { vertex: MyVertex fragment: MyFragment }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("pipeline", diagnostic.Suggestions);
    }

    #endregion

    #region Binding Mode Suggestions

    [Fact]
    public void Compile_MisspelledRead_SuggestsRead()
    {
        var source = @"
            compute Test {
                query {
                    reed Position
                }
                execute() {
                }
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("read", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledWrite_SuggestsWrite()
    {
        var source = @"
            compute Test {
                query {
                    writ Position
                }
                execute() {
                }
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("write", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledOptional_SuggestsOptional()
    {
        var source = @"
            compute Test {
                query {
                    read Position
                    optinal Velocity
                }
                execute() {
                }
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("optional", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledWithout_SuggestsWithout()
    {
        var source = @"
            compute Test {
                query {
                    write Position
                    witout Frozen
                }
                execute() {
                }
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("without", diagnostic.Suggestions);
    }

    #endregion

    #region Type Suggestions

    [Fact]
    public void Compile_MisspelledFloat_SuggestsFloat()
    {
        var result = KeslCompiler.Compile("component Position { x: flot }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("float", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledFloat3_SuggestsFloat3()
    {
        var result = KeslCompiler.Compile("component Position { pos: floa3 }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("float3", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledMat4_SuggestsMat4()
    {
        var result = KeslCompiler.Compile("component Transform { m: mat }");

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("mat4", diagnostic.Suggestions);
    }

    #endregion

    #region Texture Type Suggestions

    [Fact]
    public void Compile_MisspelledTexture2D_SuggestsTexture2D()
    {
        var source = @"
            fragment Test {
                in {
                    uv: float2
                }
                out {
                    fragColor: float4 @ 0
                }
                textures {
                    diffuse: textur2D @ 0
                }
                execute() {
                    fragColor = uv;
                }
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("texture2D", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledTextureCube_SuggestsTextureCube()
    {
        var source = @"
            fragment Test {
                in {
                    direction: float3
                }
                out {
                    fragColor: float4 @ 0
                }
                textures {
                    skybox: texturCube @ 0
                }
                execute() {
                    fragColor = direction;
                }
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("textureCube", diagnostic.Suggestions);
    }

    #endregion

    #region Topology Suggestions

    [Fact]
    public void Compile_MisspelledInputTopology_SuggestsTriangles()
    {
        var source = @"
            geometry Test {
                layout {
                    input: triangls
                    output: triangle_strip
                    max_vertices: 3
                }
                in { pos: float3 }
                out { color: float4 }
                execute() {}
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("triangles", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledOutputTopology_SuggestsTriangleStrip()
    {
        var source = @"
            geometry Test {
                layout {
                    input: triangles
                    output: triangle_stri
                    max_vertices: 3
                }
                in { pos: float3 }
                out { color: float4 }
                execute() {}
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("triangle_strip", diagnostic.Suggestions);
    }

    #endregion

    #region Pipeline Stage Suggestions

    [Fact]
    public void Compile_MisspelledVertexStage_SuggestsVertex()
    {
        var source = @"
            pipeline Test {
                vertx: MyVertex
                fragment: MyFragment
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("vertex", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledFragmentStage_SuggestsFragment()
    {
        var source = @"
            pipeline Test {
                vertex: MyVertex
                fragmnt: MyFragment
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("fragment", diagnostic.Suggestions);
    }

    [Fact]
    public void Compile_MisspelledGeometryStage_SuggestsGeometry()
    {
        var source = @"
            pipeline Test {
                vertex: MyVertex
                geomety: MyGeometry
                fragment: MyFragment
            }
        ";
        var result = KeslCompiler.Compile(source);

        Assert.True(result.HasErrors);
        Assert.NotEmpty(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.NotNull(diagnostic.Suggestions);
        Assert.Contains("geometry", diagnostic.Suggestions);
    }

    #endregion

    #region Diagnostic Formatter Integration

    [Fact]
    public void DiagnosticFormatter_FormatsSuggestions()
    {
        const string source = "compnent Position { x: float }";
        var diagnostic = Diagnostic.Error(
            KeslErrorCodes.UnexpectedToken,
            "Unexpected token",
            new SourceSpan(new SourceLocation("<test>", 1, 1), new SourceLocation("<test>", 1, 8)),
            "<test>",
            ["component", "compute"]);

        var formatter = new DiagnosticFormatter(source);
        var formatted = formatter.Format(diagnostic, new DiagnosticFormatOptions { UseColors = false });

        Assert.Contains("Did you mean:", formatted);
        Assert.Contains("component", formatted);
        Assert.Contains("compute", formatted);
    }

    #endregion
}
