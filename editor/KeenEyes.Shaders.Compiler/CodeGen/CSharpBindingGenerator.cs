using System.Text;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.CodeGen;

/// <summary>
/// Generates C# binding code for KESL shaders (compute, vertex, fragment).
/// </summary>
public sealed class CSharpBindingGenerator
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    /// <summary>
    /// Gets or sets the namespace for generated code.
    /// </summary>
    public string Namespace { get; set; } = "Generated";

    /// <summary>
    /// Generates C# binding code for a vertex shader declaration.
    /// </summary>
    /// <param name="vertex">The vertex shader AST.</param>
    /// <returns>The generated C# code.</returns>
    public string Generate(VertexDeclaration vertex)
    {
        _sb.Clear();
        _indent = 0;

        GenerateFileHeader();
        GenerateVertexShaderClass(vertex);

        return _sb.ToString();
    }

    /// <summary>
    /// Generates C# binding code for a fragment shader declaration.
    /// </summary>
    /// <param name="fragment">The fragment shader AST.</param>
    /// <returns>The generated C# code.</returns>
    public string Generate(FragmentDeclaration fragment)
    {
        _sb.Clear();
        _indent = 0;

        GenerateFileHeader();
        GenerateFragmentShaderClass(fragment);

        return _sb.ToString();
    }

    /// <summary>
    /// Generates C# binding code for a geometry shader declaration.
    /// </summary>
    /// <param name="geometry">The geometry shader AST.</param>
    /// <returns>The generated C# code.</returns>
    public string Generate(GeometryDeclaration geometry)
    {
        _sb.Clear();
        _indent = 0;

        GenerateFileHeader();
        GenerateGeometryShaderClass(geometry);

        return _sb.ToString();
    }

    /// <summary>
    /// Generates C# binding code for a compute shader declaration.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated C# code.</returns>
    public string Generate(ComputeDeclaration compute)
    {
        _sb.Clear();
        _indent = 0;

        GenerateFileHeader();

        // Class declaration
        var className = $"{compute.Name}Shader";
        AppendLine($"/// <summary>");
        AppendLine($"/// GPU compute system for {compute.Name}.");
        AppendLine($"/// </summary>");
        AppendLine($"public sealed partial class {className} : IGpuComputeSystem, IDisposable");
        AppendLine("{");
        _indent++;

        // Fields
        GenerateFields(compute);
        AppendLine();

        // Query descriptor
        GenerateQueryDescriptor(compute);
        AppendLine();

        // Constructor
        GenerateConstructor(compute, className);
        AppendLine();

        // Execute method
        GenerateExecuteMethod(compute);
        AppendLine();

        // Dispose method
        GenerateDisposeMethod(compute);

        _indent--;
        AppendLine("}");

        return _sb.ToString();
    }

    private void GenerateFields(ComputeDeclaration compute)
    {
        AppendLine("private readonly IGpuDevice _device;");
        AppendLine("private readonly CompiledShader _shader;");

        // Generate buffer fields for each component binding
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }

            AppendLine($"private GpuBuffer<{binding.ComponentName}>? _{ToCamelCase(binding.ComponentName)}Buffer;");
        }
    }

    private void GenerateQueryDescriptor(ComputeDeclaration compute)
    {
        AppendLine("/// <summary>");
        AppendLine("/// The query descriptor matching the shader's component requirements.");
        AppendLine("/// </summary>");
        Append(GenerateIndent());
        _sb.Append("private static readonly QueryDescriptor Query = QueryDescriptor.Create()");

        foreach (var binding in compute.Query.Bindings)
        {
            switch (binding.AccessMode)
            {
                case AccessMode.Read:
                case AccessMode.Write:
                    _sb.AppendLine();
                    Append(GenerateIndent());
                    _sb.Append($"    .With<{binding.ComponentName}>()");
                    break;
                case AccessMode.Optional:
                    _sb.AppendLine();
                    Append(GenerateIndent());
                    _sb.Append($"    .WithOptional<{binding.ComponentName}>()");
                    break;
                case AccessMode.Without:
                    _sb.AppendLine();
                    Append(GenerateIndent());
                    _sb.Append($"    .Without<{binding.ComponentName}>()");
                    break;
            }
        }

        _sb.AppendLine(";");
    }

    private void GenerateConstructor(ComputeDeclaration compute, string className)
    {
        AppendLine("/// <summary>");
        AppendLine($"/// Creates a new {className} instance.");
        AppendLine("/// </summary>");
        AppendLine("/// <param name=\"device\">The GPU device to use.</param>");
        AppendLine($"public {className}(IGpuDevice device)");
        AppendLine("{");
        _indent++;
        AppendLine("_device = device ?? throw new ArgumentNullException(nameof(device));");
        AppendLine($"_shader = device.CompileComputeShader(EmbeddedShaders.{compute.Name});");
        _indent--;
        AppendLine("}");
    }

    private void GenerateExecuteMethod(ComputeDeclaration compute)
    {
        // Build method signature
        var parameters = new List<string> { "World world" };
        if (compute.Params != null)
        {
            foreach (var param in compute.Params.Parameters)
            {
                parameters.Add($"{ToCSharpType(param.Type)} {param.Name}");
            }
        }
        var paramString = string.Join(", ", parameters);

        AppendLine("/// <summary>");
        AppendLine("/// Executes the compute shader on all matching entities.");
        AppendLine("/// </summary>");
        AppendLine("/// <param name=\"world\">The world containing entities to process.</param>");
        if (compute.Params != null)
        {
            foreach (var param in compute.Params.Parameters)
            {
                AppendLine($"/// <param name=\"{param.Name}\">Shader parameter.</param>");
            }
        }
        AppendLine($"public void Execute({paramString})");
        AppendLine("{");
        _indent++;

        AppendLine("foreach (var archetype in world.QueryArchetypes(Query))");
        AppendLine("{");
        _indent++;

        AppendLine("int count = archetype.EntityCount;");
        AppendLine("if (count == 0) continue;");
        AppendLine();

        // Get component spans
        var writeBindings = new List<QueryBinding>();
        var readBindings = new List<QueryBinding>();

        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }

            var varName = ToCamelCase(binding.ComponentName);
            AppendLine($"var {varName}Span = archetype.GetComponentSpan<{binding.ComponentName}>();");

            if (binding.AccessMode == AccessMode.Write)
            {
                writeBindings.Add(binding);
            }
            else
            {
                readBindings.Add(binding);
            }
        }

        AppendLine();

        // Ensure buffer capacity
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }

            var varName = ToCamelCase(binding.ComponentName);
            AppendLine($"EnsureBufferCapacity(ref _{varName}Buffer, count);");
        }

        AppendLine();

        // Upload to GPU
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }

            var varName = ToCamelCase(binding.ComponentName);
            AppendLine($"_{varName}Buffer!.Upload({varName}Span);");
        }

        AppendLine();

        // Create and execute command buffer
        AppendLine("var cmd = _device.CreateCommandBuffer();");
        AppendLine("cmd.BindComputeShader(_shader);");
        AppendLine();

        // Bind buffers
        int bindingIndex = 0;
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }

            var varName = ToCamelCase(binding.ComponentName);
            AppendLine($"cmd.BindBuffer({bindingIndex}, _{varName}Buffer!);");
            bindingIndex++;
        }

        AppendLine();

        // Set uniforms
        if (compute.Params != null)
        {
            foreach (var param in compute.Params.Parameters)
            {
                AppendLine($"cmd.SetUniform(\"{param.Name}\", {param.Name});");
            }
        }
        AppendLine("cmd.SetUniform(\"entityCount\", (uint)count);");

        AppendLine();

        // Dispatch
        AppendLine("cmd.Dispatch((count + 63) / 64, 1, 1);");
        AppendLine("cmd.Execute();");

        AppendLine();

        // Download modified components
        foreach (var binding in writeBindings)
        {
            var varName = ToCamelCase(binding.ComponentName);
            AppendLine($"_{varName}Buffer!.Download({varName}Span);");
        }

        _indent--;
        AppendLine("}");

        _indent--;
        AppendLine("}");

        AppendLine();

        // Helper method for buffer capacity
        AppendLine("private void EnsureBufferCapacity<T>(ref GpuBuffer<T>? buffer, int count) where T : unmanaged");
        AppendLine("{");
        _indent++;
        AppendLine("if (buffer == null || buffer.Count < count)");
        AppendLine("{");
        _indent++;
        AppendLine("buffer?.Dispose();");
        AppendLine("buffer = _device.CreateBuffer<T>(count);");
        _indent--;
        AppendLine("}");
        _indent--;
        AppendLine("}");
    }

    private void GenerateDisposeMethod(ComputeDeclaration compute)
    {
        AppendLine("/// <summary>");
        AppendLine("/// Releases GPU resources.");
        AppendLine("/// </summary>");
        AppendLine("public void Dispose()");
        AppendLine("{");
        _indent++;

        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }

            var varName = ToCamelCase(binding.ComponentName);
            AppendLine($"_{varName}Buffer?.Dispose();");
        }

        AppendLine("_shader.Dispose();");

        _indent--;
        AppendLine("}");
    }

    private static string ToCSharpType(TypeRef type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Float2 => "System.Numerics.Vector2",
                PrimitiveTypeKind.Float3 => "System.Numerics.Vector3",
                PrimitiveTypeKind.Float4 => "System.Numerics.Vector4",
                PrimitiveTypeKind.Int => "int",
                PrimitiveTypeKind.Int2 => "(int, int)",
                PrimitiveTypeKind.Int3 => "(int, int, int)",
                PrimitiveTypeKind.Int4 => "(int, int, int, int)",
                PrimitiveTypeKind.Uint => "uint",
                PrimitiveTypeKind.Bool => "bool",
                PrimitiveTypeKind.Mat4 => "System.Numerics.Matrix4x4",
                _ => throw new ArgumentOutOfRangeException(nameof(type), pt.Kind, "Unsupported primitive type")
            };
        }

        throw new NotSupportedException($"Type {type.GetType().Name} not supported");
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

#pragma warning disable IDE0057 // Substring can be simplified - not available in netstandard2.0
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
#pragma warning restore IDE0057
    }

    private void AppendLine(string? text = null)
    {
        if (text != null)
        {
            _sb.Append(GenerateIndent());
            _sb.AppendLine(text);
        }
        else
        {
            _sb.AppendLine();
        }
    }

    private void Append(string text)
    {
        _sb.Append(text);
    }

    private string GenerateIndent()
    {
        return new string(' ', _indent * 4);
    }

    private void GenerateFileHeader()
    {
        AppendLine("// <auto-generated>");
        AppendLine("// This code was generated by the KESL compiler.");
        AppendLine("// Do not modify this file directly.");
        AppendLine("// </auto-generated>");
        AppendLine();
        AppendLine("#nullable enable");
        AppendLine();

        AppendLine("using System;");
        AppendLine("using System.Collections.Generic;");
        AppendLine("using KeenEyes.Shaders;");
        AppendLine();

        AppendLine($"namespace {Namespace};");
        AppendLine();
    }

    private void GenerateVertexShaderClass(VertexDeclaration vertex)
    {
        var className = $"{vertex.Name}Shader";

        AppendLine("/// <summary>");
        AppendLine($"/// GPU vertex shader for {vertex.Name}.");
        AppendLine("/// </summary>");
        AppendLine($"public sealed partial class {className} : IGpuVertexShader");
        AppendLine("{");
        _indent++;

        // Name property
        AppendLine("/// <inheritdoc />");
        AppendLine($"public string Name => \"{vertex.Name}\";");
        AppendLine();

        // InputLayout property
        GenerateInputLayoutProperty(vertex.Inputs.Attributes);
        AppendLine();

        // Outputs property
        GenerateVertexOutputsProperty(vertex.Outputs.Attributes);
        AppendLine();

        // Uniforms property
        GenerateUniformsProperty(vertex.Params);
        AppendLine();

        // GetShaderSource method
        GenerateGetShaderSourceMethod(vertex.Name);
        AppendLine();

        // Embedded shader sources (placeholders for now)
        GenerateShaderSourceConstants(vertex.Name);

        _indent--;
        AppendLine("}");
    }

    private void GenerateFragmentShaderClass(FragmentDeclaration fragment)
    {
        var className = $"{fragment.Name}Shader";

        AppendLine("/// <summary>");
        AppendLine($"/// GPU fragment shader for {fragment.Name}.");
        AppendLine("/// </summary>");
        AppendLine($"public sealed partial class {className} : IGpuFragmentShader");
        AppendLine("{");
        _indent++;

        // Name property
        AppendLine("/// <inheritdoc />");
        AppendLine($"public string Name => \"{fragment.Name}\";");
        AppendLine();

        // Inputs property
        GenerateFragmentInputsProperty(fragment.Inputs.Attributes);
        AppendLine();

        // Outputs property
        GenerateFragmentOutputsProperty(fragment.Outputs.Attributes);
        AppendLine();

        // Uniforms property
        GenerateUniformsProperty(fragment.Params);
        AppendLine();

        // GetShaderSource method
        GenerateGetShaderSourceMethod(fragment.Name);
        AppendLine();

        // Embedded shader sources (placeholders for now)
        GenerateShaderSourceConstants(fragment.Name);

        _indent--;
        AppendLine("}");
    }

    private void GenerateGeometryShaderClass(GeometryDeclaration geometry)
    {
        var className = $"{geometry.Name}Shader";

        AppendLine("/// <summary>");
        AppendLine($"/// GPU geometry shader for {geometry.Name}.");
        AppendLine("/// </summary>");
        AppendLine($"public sealed partial class {className} : IGpuGeometryShader");
        AppendLine("{");
        _indent++;

        // Name property
        AppendLine("/// <inheritdoc />");
        AppendLine($"public string Name => \"{geometry.Name}\";");
        AppendLine();

        // InputTopology property
        var inputTopology = geometry.Layout.InputTopology switch
        {
            GeometryInputTopology.Points => "Points",
            GeometryInputTopology.Lines => "Lines",
            GeometryInputTopology.LinesAdjacency => "LinesAdjacency",
            GeometryInputTopology.Triangles => "Triangles",
            GeometryInputTopology.TrianglesAdjacency => "TrianglesAdjacency",
            _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry.Layout.InputTopology, "Unsupported input topology")
        };
        AppendLine("/// <inheritdoc />");
        AppendLine($"public GeometryInputTopology InputTopology => GeometryInputTopology.{inputTopology};");
        AppendLine();

        // OutputTopology property
        var outputTopology = geometry.Layout.OutputTopology switch
        {
            GeometryOutputTopology.Points => "Points",
            GeometryOutputTopology.LineStrip => "LineStrip",
            GeometryOutputTopology.TriangleStrip => "TriangleStrip",
            _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry.Layout.OutputTopology, "Unsupported output topology")
        };
        AppendLine("/// <inheritdoc />");
        AppendLine($"public GeometryOutputTopology OutputTopology => GeometryOutputTopology.{outputTopology};");
        AppendLine();

        // MaxVertices property
        AppendLine("/// <inheritdoc />");
        AppendLine($"public int MaxVertices => {geometry.Layout.MaxVertices};");
        AppendLine();

        // Inputs property
        GenerateFragmentInputsProperty(geometry.Inputs.Attributes);
        AppendLine();

        // Outputs property
        GenerateVertexOutputsProperty(geometry.Outputs.Attributes);
        AppendLine();

        // Uniforms property
        GenerateUniformsProperty(geometry.Params);
        AppendLine();

        // GetShaderSource method
        GenerateGetShaderSourceMethod(geometry.Name);
        AppendLine();

        // Embedded shader sources (placeholders for now)
        GenerateShaderSourceConstants(geometry.Name);

        _indent--;
        AppendLine("}");
    }

    private void GenerateInputLayoutProperty(IReadOnlyList<AttributeDeclaration> attributes)
    {
        AppendLine("/// <inheritdoc />");
        AppendLine("public InputLayoutDescriptor InputLayout { get; } = new([");
        _indent++;

        for (int i = 0; i < attributes.Count; i++)
        {
            var attr = attributes[i];
            var attrType = ToAttributeType(attr.Type);
            var location = attr.LocationIndex ?? i;
            var trailing = i < attributes.Count - 1 ? "," : "";
            AppendLine($"new InputAttribute(\"{attr.Name}\", AttributeType.{attrType}, {location}){trailing}");
        }

        _indent--;
        AppendLine("]);");
    }

    private void GenerateVertexOutputsProperty(IReadOnlyList<AttributeDeclaration> attributes)
    {
        AppendLine("/// <inheritdoc />");
        AppendLine("public IReadOnlyList<InputAttribute> Outputs { get; } = [");
        _indent++;

        for (int i = 0; i < attributes.Count; i++)
        {
            var attr = attributes[i];
            var attrType = ToAttributeType(attr.Type);
            var location = attr.LocationIndex ?? i;
            var trailing = i < attributes.Count - 1 ? "," : "";
            AppendLine($"new InputAttribute(\"{attr.Name}\", AttributeType.{attrType}, {location}){trailing}");
        }

        _indent--;
        AppendLine("];");
    }

    private void GenerateFragmentInputsProperty(IReadOnlyList<AttributeDeclaration> attributes)
    {
        AppendLine("/// <inheritdoc />");
        AppendLine("public IReadOnlyList<InputAttribute> Inputs { get; } = [");
        _indent++;

        for (int i = 0; i < attributes.Count; i++)
        {
            var attr = attributes[i];
            var attrType = ToAttributeType(attr.Type);
            var location = i;
            var trailing = i < attributes.Count - 1 ? "," : "";
            AppendLine($"new InputAttribute(\"{attr.Name}\", AttributeType.{attrType}, {location}){trailing}");
        }

        _indent--;
        AppendLine("];");
    }

    private void GenerateFragmentOutputsProperty(IReadOnlyList<AttributeDeclaration> attributes)
    {
        AppendLine("/// <inheritdoc />");
        AppendLine("public IReadOnlyList<OutputTarget> Outputs { get; } = [");
        _indent++;

        for (int i = 0; i < attributes.Count; i++)
        {
            var attr = attributes[i];
            var attrType = ToAttributeType(attr.Type);
            var location = attr.LocationIndex ?? i;
            var trailing = i < attributes.Count - 1 ? "," : "";
            AppendLine($"new OutputTarget(\"{attr.Name}\", AttributeType.{attrType}, {location}){trailing}");
        }

        _indent--;
        AppendLine("];");
    }

    private void GenerateUniformsProperty(ParamsBlock? paramsBlock)
    {
        AppendLine("/// <inheritdoc />");

        if (paramsBlock == null || paramsBlock.Parameters.Count == 0)
        {
            AppendLine("public IReadOnlyList<UniformDescriptor> Uniforms { get; } = [];");
            return;
        }

        AppendLine("public IReadOnlyList<UniformDescriptor> Uniforms { get; } = [");
        _indent++;

        for (int i = 0; i < paramsBlock.Parameters.Count; i++)
        {
            var param = paramsBlock.Parameters[i];
            var uniformType = ToUniformType(param.Type);
            var trailing = i < paramsBlock.Parameters.Count - 1 ? "," : "";
            AppendLine($"new UniformDescriptor(\"{param.Name}\", UniformType.{uniformType}){trailing}");
        }

        _indent--;
        AppendLine("];");
    }

    private void GenerateGetShaderSourceMethod(string shaderName)
    {
        AppendLine("/// <inheritdoc />");
        AppendLine("public string GetShaderSource(ShaderBackend backend) => backend switch");
        AppendLine("{");
        _indent++;
        AppendLine($"ShaderBackend.GLSL => {shaderName}GlslSource,");
        AppendLine($"ShaderBackend.HLSL => {shaderName}HlslSource,");
        AppendLine("_ => throw new NotSupportedException($\"Backend {backend} is not supported\")");
        _indent--;
        AppendLine("};");
    }

    private void GenerateShaderSourceConstants(string shaderName)
    {
        // These are placeholders - in a full implementation, these would be populated
        // by the compiler with the actual GLSL/HLSL source code
        AppendLine($"private const string {shaderName}GlslSource = \"\";");
        AppendLine($"private const string {shaderName}HlslSource = \"\";");
    }

    private static string ToAttributeType(TypeRef type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                PrimitiveTypeKind.Float => "Float",
                PrimitiveTypeKind.Float2 => "Float2",
                PrimitiveTypeKind.Float3 => "Float3",
                PrimitiveTypeKind.Float4 => "Float4",
                PrimitiveTypeKind.Int => "Int",
                PrimitiveTypeKind.Int2 => "Int2",
                PrimitiveTypeKind.Int3 => "Int3",
                PrimitiveTypeKind.Int4 => "Int4",
                PrimitiveTypeKind.Uint => "UInt",
                PrimitiveTypeKind.Mat4 => "Mat4",
                _ => throw new ArgumentOutOfRangeException(nameof(type), pt.Kind, "Unsupported attribute type")
            };
        }

        throw new NotSupportedException($"Type {type.GetType().Name} not supported for attributes");
    }

    private static string ToUniformType(TypeRef type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                PrimitiveTypeKind.Float => "Float",
                PrimitiveTypeKind.Float2 => "Float2",
                PrimitiveTypeKind.Float3 => "Float3",
                PrimitiveTypeKind.Float4 => "Float4",
                PrimitiveTypeKind.Int => "Int",
                PrimitiveTypeKind.Int2 => "Int2",
                PrimitiveTypeKind.Int3 => "Int3",
                PrimitiveTypeKind.Int4 => "Int4",
                PrimitiveTypeKind.Uint => "UInt",
                PrimitiveTypeKind.Bool => "Bool",
                PrimitiveTypeKind.Mat4 => "Matrix4",
                _ => throw new ArgumentOutOfRangeException(nameof(type), pt.Kind, "Unsupported uniform type")
            };
        }

        throw new NotSupportedException($"Type {type.GetType().Name} not supported for uniforms");
    }
}
