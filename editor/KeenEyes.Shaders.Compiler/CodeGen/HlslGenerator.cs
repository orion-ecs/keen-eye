using System.Text;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.CodeGen;

/// <summary>
/// Generates HLSL compute shader code from KESL AST.
/// </summary>
/// <remarks>
/// <para>
/// HLSL (High-Level Shader Language) is used for DirectX platforms including Windows.
/// Key differences from GLSL include:
/// </para>
/// <list type="bullet">
/// <item><description>vec2/3/4 → float2/3/4</description></item>
/// <item><description>mat4 → float4x4</description></item>
/// <item><description>StructuredBuffer/RWStructuredBuffer instead of std430 layout</description></item>
/// <item><description>register(t#) for read-only, register(u#) for read-write</description></item>
/// <item><description>[numthreads(x,y,z)] attribute instead of layout(local_size_x = n)</description></item>
/// </list>
/// </remarks>
public sealed class HlslGenerator : IShaderGenerator
{
    private readonly StringBuilder sb = new();
    private int indent;
    private int srvIndex; // Shader Resource View (read-only buffers)
    private int uavIndex; // Unordered Access View (read-write buffers)

    /// <inheritdoc />
    public ShaderBackend Backend => ShaderBackend.HLSL;

    /// <inheritdoc />
    public string FileExtension => "hlsl";

    /// <summary>
    /// Generates HLSL code for a compute shader declaration.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated HLSL code.</returns>
    public string Generate(ComputeDeclaration compute)
    {
        sb.Clear();
        indent = 0;
        srvIndex = 0;
        uavIndex = 0;

        // Generate struct definitions for component data
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }
            GenerateStructDefinition(binding);
        }
        AppendLine();

        // Generate buffer declarations
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }
            GenerateBufferDeclaration(binding);
        }
        AppendLine();

        // Generate constant buffer for params and entity count
        GenerateConstantBuffer(compute);
        AppendLine();

        // Main compute shader function
        AppendLine("[numthreads(64, 1, 1)]");
        AppendLine("void CSMain(uint3 DTid : SV_DispatchThreadID)");
        AppendLine("{");
        indent++;

        // Entity index with bounds check
        AppendLine("uint idx = DTid.x;");
        AppendLine("if (idx >= entityCount) return;");
        AppendLine();

        // Generate execute block statements
        foreach (var stmt in compute.Execute.Body)
        {
            GenerateStatement(stmt, compute.Query.Bindings);
        }

        indent--;
        AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates HLSL code for a vertex shader declaration.
    /// </summary>
    /// <param name="vertex">The vertex shader AST.</param>
    /// <returns>The generated HLSL code.</returns>
    /// <remarks>
    /// HLSL vertex shaders use:
    /// - Semantic annotations (POSITION, NORMAL, TEXCOORD, etc.)
    /// - struct VS_INPUT/VS_OUTPUT for input/output data
    /// - cbuffer for uniform parameters
    /// </remarks>
    public string Generate(VertexDeclaration vertex)
    {
        sb.Clear();
        indent = 0;

        // Generate texture declarations (HLSL uses separate texture and sampler)
        if (vertex.Textures != null && vertex.Textures.Textures.Count > 0)
        {
            foreach (var tex in vertex.Textures.Textures)
            {
                AppendLine($"{ToHlslTextureType(tex.TextureKind)} {tex.Name} : register(t{tex.BindingSlot});");
            }
            AppendLine();
        }

        // Generate sampler declarations
        if (vertex.Samplers != null && vertex.Samplers.Samplers.Count > 0)
        {
            foreach (var sampler in vertex.Samplers.Samplers)
            {
                AppendLine($"SamplerState {sampler.Name} : register(s{sampler.BindingSlot});");
            }
            AppendLine();
        }

        // Generate cbuffer for uniform parameters
        if (vertex.Params != null && vertex.Params.Parameters.Count > 0)
        {
            AppendLine("cbuffer VertexParams : register(b0)");
            AppendLine("{");
            indent++;
            foreach (var param in vertex.Params.Parameters)
            {
                AppendLine($"{ToHlslType(param.Type)} {param.Name};");
            }
            indent--;
            AppendLine("};");
            AppendLine();
        }

        // Generate VS_INPUT struct
        AppendLine("struct VS_INPUT");
        AppendLine("{");
        indent++;
        foreach (var attr in vertex.Inputs.Attributes)
        {
            var semantic = GetInputSemantic(attr.Name, attr.LocationIndex);
            AppendLine($"{ToHlslType(attr.Type)} {attr.Name} : {semantic};");
        }
        indent--;
        AppendLine("};");
        AppendLine();

        // Generate VS_OUTPUT struct
        AppendLine("struct VS_OUTPUT");
        AppendLine("{");
        indent++;
        AppendLine("float4 position : SV_POSITION;");
        var texcoordIndex = 0;
        foreach (var attr in vertex.Outputs.Attributes)
        {
            AppendLine($"{ToHlslType(attr.Type)} {attr.Name} : TEXCOORD{texcoordIndex};");
            texcoordIndex++;
        }
        indent--;
        AppendLine("};");
        AppendLine();

        // Main vertex shader function
        AppendLine("VS_OUTPUT VSMain(VS_INPUT input)");
        AppendLine("{");
        indent++;
        AppendLine("VS_OUTPUT output = (VS_OUTPUT)0;");
        AppendLine();

        // Generate execute block statements
        foreach (var stmt in vertex.Execute.Body)
        {
            GenerateVertexStatement(stmt, vertex.Inputs.Attributes, vertex.Outputs.Attributes);
        }

        AppendLine();
        AppendLine("return output;");
        indent--;
        AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates HLSL code for a fragment shader declaration.
    /// </summary>
    /// <param name="fragment">The fragment shader AST.</param>
    /// <returns>The generated HLSL code.</returns>
    /// <remarks>
    /// HLSL pixel shaders (fragment shaders) use:
    /// - struct PS_INPUT for input data from vertex shader
    /// - SV_Target semantic for output color
    /// - cbuffer for uniform parameters
    /// </remarks>
    public string Generate(FragmentDeclaration fragment)
    {
        sb.Clear();
        indent = 0;

        // Generate texture declarations (HLSL uses separate texture and sampler)
        if (fragment.Textures != null && fragment.Textures.Textures.Count > 0)
        {
            foreach (var tex in fragment.Textures.Textures)
            {
                AppendLine($"{ToHlslTextureType(tex.TextureKind)} {tex.Name} : register(t{tex.BindingSlot});");
            }
            AppendLine();
        }

        // Generate sampler declarations
        if (fragment.Samplers != null && fragment.Samplers.Samplers.Count > 0)
        {
            foreach (var sampler in fragment.Samplers.Samplers)
            {
                AppendLine($"SamplerState {sampler.Name} : register(s{sampler.BindingSlot});");
            }
            AppendLine();
        }

        // Generate cbuffer for uniform parameters
        if (fragment.Params != null && fragment.Params.Parameters.Count > 0)
        {
            AppendLine("cbuffer PixelParams : register(b0)");
            AppendLine("{");
            indent++;
            foreach (var param in fragment.Params.Parameters)
            {
                AppendLine($"{ToHlslType(param.Type)} {param.Name};");
            }
            indent--;
            AppendLine("};");
            AppendLine();
        }

        // Generate PS_INPUT struct (matches VS_OUTPUT)
        AppendLine("struct PS_INPUT");
        AppendLine("{");
        indent++;
        AppendLine("float4 position : SV_POSITION;");
        var texcoordIndex = 0;
        foreach (var attr in fragment.Inputs.Attributes)
        {
            AppendLine($"{ToHlslType(attr.Type)} {attr.Name} : TEXCOORD{texcoordIndex};");
            texcoordIndex++;
        }
        indent--;
        AppendLine("};");
        AppendLine();

        // Generate output struct if multiple outputs, otherwise use return type
        if (fragment.Outputs.Attributes.Count > 1)
        {
            AppendLine("struct PS_OUTPUT");
            AppendLine("{");
            indent++;
            foreach (var attr in fragment.Outputs.Attributes)
            {
                var target = attr.LocationIndex.HasValue ? $"SV_TARGET{attr.LocationIndex.Value}" : "SV_TARGET";
                AppendLine($"{ToHlslType(attr.Type)} {attr.Name} : {target};");
            }
            indent--;
            AppendLine("};");
            AppendLine();

            // Main pixel shader function with multiple outputs
            AppendLine("PS_OUTPUT PSMain(PS_INPUT input)");
            AppendLine("{");
            indent++;
            AppendLine("PS_OUTPUT output = (PS_OUTPUT)0;");
            AppendLine();

            foreach (var stmt in fragment.Execute.Body)
            {
                GenerateFragmentStatement(stmt, fragment.Inputs.Attributes, fragment.Outputs.Attributes, useOutputStruct: true);
            }

            AppendLine();
            AppendLine("return output;");
            indent--;
            AppendLine("}");
        }
        else
        {
            // Single output - use direct return type
            var outputAttr = fragment.Outputs.Attributes[0];
            var target = outputAttr.LocationIndex.HasValue ? $"SV_TARGET{outputAttr.LocationIndex.Value}" : "SV_TARGET";
            AppendLine($"{ToHlslType(outputAttr.Type)} PSMain(PS_INPUT input) : {target}");
            AppendLine("{");
            indent++;
            AppendLine($"{ToHlslType(outputAttr.Type)} {outputAttr.Name} = ({ToHlslType(outputAttr.Type)})0;");
            AppendLine();

            foreach (var stmt in fragment.Execute.Body)
            {
                GenerateFragmentStatement(stmt, fragment.Inputs.Attributes, fragment.Outputs.Attributes, useOutputStruct: false);
            }

            AppendLine();
            AppendLine($"return {outputAttr.Name};");
            indent--;
            AppendLine("}");
        }

        return sb.ToString();
    }

    private static string GetInputSemantic(string name, int? locationIndex)
    {
        // Map common attribute names to HLSL semantics
        var lowerName = name.ToLowerInvariant();
        return lowerName switch
        {
            "position" or "pos" => "POSITION",
            "normal" => "NORMAL",
            "tangent" => "TANGENT",
            "binormal" or "bitangent" => "BINORMAL",
            "color" or "colour" => "COLOR",
            "texcoord" or "uv" or "texcoord0" => "TEXCOORD0",
            "texcoord1" or "uv1" => "TEXCOORD1",
            "texcoord2" or "uv2" => "TEXCOORD2",
            "texcoord3" or "uv3" => "TEXCOORD3",
            _ => locationIndex.HasValue ? $"TEXCOORD{locationIndex.Value}" : "TEXCOORD0"
        };
    }

    private void GenerateVertexStatement(Statement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                sb.Append(GenerateVertexExpression(exprStmt.Expression, inputs, outputs));
                sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                sb.Append(GenerateVertexExpression(assignStmt.Target, inputs, outputs));
                sb.Append(" = ");
                sb.Append(GenerateVertexExpression(assignStmt.Value, inputs, outputs));
                sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                sb.Append(GenerateVertexExpression(compoundStmt.Target, inputs, outputs));
                sb.Append(' ');
                sb.Append(compoundStmt.Operator switch
                {
                    CompoundOperator.PlusEquals => "+=",
                    CompoundOperator.MinusEquals => "-=",
                    CompoundOperator.StarEquals => "*=",
                    CompoundOperator.SlashEquals => "/=",
                    _ => throw new InvalidOperationException()
                });
                sb.Append(' ');
                sb.Append(GenerateVertexExpression(compoundStmt.Value, inputs, outputs));
                sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateVertexIfStatement(ifStmt, inputs, outputs);
                break;

            case ForStatement forStmt:
                GenerateVertexForStatement(forStmt, inputs, outputs);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateVertexStatement(s, inputs, outputs);
                }
                indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateFragmentStatement(Statement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                sb.Append(GenerateFragmentExpression(exprStmt.Expression, inputs, outputs, useOutputStruct));
                sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                sb.Append(GenerateFragmentExpression(assignStmt.Target, inputs, outputs, useOutputStruct));
                sb.Append(" = ");
                sb.Append(GenerateFragmentExpression(assignStmt.Value, inputs, outputs, useOutputStruct));
                sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                sb.Append(GenerateFragmentExpression(compoundStmt.Target, inputs, outputs, useOutputStruct));
                sb.Append(' ');
                sb.Append(compoundStmt.Operator switch
                {
                    CompoundOperator.PlusEquals => "+=",
                    CompoundOperator.MinusEquals => "-=",
                    CompoundOperator.StarEquals => "*=",
                    CompoundOperator.SlashEquals => "/=",
                    _ => throw new InvalidOperationException()
                });
                sb.Append(' ');
                sb.Append(GenerateFragmentExpression(compoundStmt.Value, inputs, outputs, useOutputStruct));
                sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateFragmentIfStatement(ifStmt, inputs, outputs, useOutputStruct);
                break;

            case ForStatement forStmt:
                GenerateFragmentForStatement(forStmt, inputs, outputs, useOutputStruct);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateFragmentStatement(s, inputs, outputs, useOutputStruct);
                }
                indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateVertexIfStatement(IfStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        Append(GenerateIndent());
        sb.Append("if (");
        sb.Append(GenerateVertexExpression(stmt.Condition, inputs, outputs));
        sb.AppendLine(")");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateVertexStatement(s, inputs, outputs);
        }
        indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("}");
            AppendLine("else");
            AppendLine("{");
            indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateVertexStatement(s, inputs, outputs);
            }
            indent--;
        }

        AppendLine("}");
    }

    private void GenerateFragmentIfStatement(IfStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        Append(GenerateIndent());
        sb.Append("if (");
        sb.Append(GenerateFragmentExpression(stmt.Condition, inputs, outputs, useOutputStruct));
        sb.AppendLine(")");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateFragmentStatement(s, inputs, outputs, useOutputStruct);
        }
        indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("}");
            AppendLine("else");
            AppendLine("{");
            indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateFragmentStatement(s, inputs, outputs, useOutputStruct);
            }
            indent--;
        }

        AppendLine("}");
    }

    private void GenerateVertexForStatement(ForStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        Append(GenerateIndent());
        sb.Append($"for (int {stmt.VariableName} = ");
        sb.Append(GenerateVertexExpression(stmt.Start, inputs, outputs));
        sb.Append($"; {stmt.VariableName} < ");
        sb.Append(GenerateVertexExpression(stmt.End, inputs, outputs));
        sb.AppendLine($"; {stmt.VariableName}++)");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.Body)
        {
            GenerateVertexStatement(s, inputs, outputs);
        }
        indent--;

        AppendLine("}");
    }

    private void GenerateFragmentForStatement(ForStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        Append(GenerateIndent());
        sb.Append($"for (int {stmt.VariableName} = ");
        sb.Append(GenerateFragmentExpression(stmt.Start, inputs, outputs, useOutputStruct));
        sb.Append($"; {stmt.VariableName} < ");
        sb.Append(GenerateFragmentExpression(stmt.End, inputs, outputs, useOutputStruct));
        sb.AppendLine($"; {stmt.VariableName}++)");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.Body)
        {
            GenerateFragmentStatement(s, inputs, outputs, useOutputStruct);
        }
        indent--;

        AppendLine("}");
    }

    private string GenerateVertexExpression(Expression expr, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => TransformVertexIdentifier(id.Name, inputs, outputs),

            MemberAccessExpression member => GenerateVertexMemberAccess(member, inputs, outputs),

            BinaryExpression binary => $"({GenerateVertexExpression(binary.Left, inputs, outputs)} {GetBinaryOp(binary.Operator)} {GenerateVertexExpression(binary.Right, inputs, outputs)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateVertexExpression(unary.Operand, inputs, outputs)})",

            CallExpression call => GenerateVertexCall(call, inputs, outputs),

            HasExpression => throw new NotSupportedException("'has' expression not supported in vertex shaders"),

            ParenthesizedExpression paren => $"({GenerateVertexExpression(paren.Inner, inputs, outputs)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private string GenerateFragmentExpression(Expression expr, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => TransformFragmentIdentifier(id.Name, inputs, outputs, useOutputStruct),

            MemberAccessExpression member => GenerateFragmentMemberAccess(member, inputs, outputs, useOutputStruct),

            BinaryExpression binary => $"({GenerateFragmentExpression(binary.Left, inputs, outputs, useOutputStruct)} {GetBinaryOp(binary.Operator)} {GenerateFragmentExpression(binary.Right, inputs, outputs, useOutputStruct)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateFragmentExpression(unary.Operand, inputs, outputs, useOutputStruct)})",

            CallExpression call => GenerateFragmentCall(call, inputs, outputs, useOutputStruct),

            HasExpression => throw new NotSupportedException("'has' expression not supported in fragment shaders"),

            ParenthesizedExpression paren => $"({GenerateFragmentExpression(paren.Inner, inputs, outputs, useOutputStruct)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private static string TransformVertexIdentifier(string name, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Check if it's an input variable - transform to input.name
        if (inputs.Any(i => i.Name == name))
        {
            return $"input.{name}";
        }

        // Check if it's an output variable - transform to output.name
        if (outputs.Any(o => o.Name == name))
        {
            return $"output.{name}";
        }

        return name;
    }

    private static string TransformFragmentIdentifier(string name, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        // Check if it's an input variable - transform to input.name
        if (inputs.Any(i => i.Name == name))
        {
            return $"input.{name}";
        }

        // Check if it's an output variable
        if (outputs.Any(o => o.Name == name))
        {
            return useOutputStruct ? $"output.{name}" : name;
        }

        return name;
    }

    private string GenerateVertexMemberAccess(MemberAccessExpression member, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Check if the base is an input or output variable
        if (member.Object is IdentifierExpression id)
        {
            if (inputs.Any(i => i.Name == id.Name))
            {
                return $"input.{id.Name}.{member.MemberName}";
            }
            if (outputs.Any(o => o.Name == id.Name))
            {
                return $"output.{id.Name}.{member.MemberName}";
            }
        }

        return $"{GenerateVertexExpression(member.Object, inputs, outputs)}.{member.MemberName}";
    }

    private string GenerateFragmentMemberAccess(MemberAccessExpression member, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        // Check if the base is an input or output variable
        if (member.Object is IdentifierExpression id)
        {
            if (inputs.Any(i => i.Name == id.Name))
            {
                return $"input.{id.Name}.{member.MemberName}";
            }
            if (outputs.Any(o => o.Name == id.Name))
            {
                return useOutputStruct ? $"output.{id.Name}.{member.MemberName}" : $"{id.Name}.{member.MemberName}";
            }
        }

        return $"{GenerateFragmentExpression(member.Object, inputs, outputs, useOutputStruct)}.{member.MemberName}";
    }

    private string GenerateVertexCall(CallExpression call, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Handle sample() specially for HLSL: sample(texture, sampler, uv) -> texture.Sample(sampler, uv)
        if (call.FunctionName == "sample" && call.Arguments.Count >= 3)
        {
            var texture = GenerateVertexExpression(call.Arguments[0], inputs, outputs);
            var sampler = GenerateVertexExpression(call.Arguments[1], inputs, outputs);
            var uv = GenerateVertexExpression(call.Arguments[2], inputs, outputs);
            return $"{texture}.Sample({sampler}, {uv})";
        }

        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateVertexExpression(a, inputs, outputs)));
        return $"{funcName}({args})";
    }

    private string GenerateFragmentCall(CallExpression call, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs, bool useOutputStruct)
    {
        // Handle sample() specially for HLSL: sample(texture, sampler, uv) -> texture.Sample(sampler, uv)
        if (call.FunctionName == "sample" && call.Arguments.Count >= 3)
        {
            var texture = GenerateFragmentExpression(call.Arguments[0], inputs, outputs, useOutputStruct);
            var sampler = GenerateFragmentExpression(call.Arguments[1], inputs, outputs, useOutputStruct);
            var uv = GenerateFragmentExpression(call.Arguments[2], inputs, outputs, useOutputStruct);
            return $"{texture}.Sample({sampler}, {uv})";
        }

        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateFragmentExpression(a, inputs, outputs, useOutputStruct)));
        return $"{funcName}({args})";
    }

    private void GenerateStructDefinition(QueryBinding binding)
    {
        AppendLine($"struct {binding.ComponentName}Data");
        AppendLine("{");
        indent++;

        // Placeholder - actual fields would come from component metadata
        // Generate float X, Y, Z as common case for Position/Velocity
        AppendLine("float X;");
        AppendLine("float Y;");
        AppendLine("float Z;");

        indent--;
        AppendLine("};");
        AppendLine();
    }

    private void GenerateBufferDeclaration(QueryBinding binding)
    {
        if (binding.AccessMode == AccessMode.Read || binding.AccessMode == AccessMode.Optional)
        {
            // Read-only buffer uses StructuredBuffer and t# register
            AppendLine($"StructuredBuffer<{binding.ComponentName}Data> {binding.ComponentName} : register(t{srvIndex});");
            srvIndex++;
        }
        else // Write
        {
            // Read-write buffer uses RWStructuredBuffer and u# register
            AppendLine($"RWStructuredBuffer<{binding.ComponentName}Data> {binding.ComponentName} : register(u{uavIndex});");
            uavIndex++;
        }
    }

    private void GenerateConstantBuffer(ComputeDeclaration compute)
    {
        AppendLine("cbuffer Params : register(b0)");
        AppendLine("{");
        indent++;

        // Generate params
        if (compute.Params != null)
        {
            foreach (var param in compute.Params.Parameters)
            {
                AppendLine($"{ToHlslType(param.Type)} {param.Name};");
            }
        }

        // Entity count is always needed
        AppendLine("uint entityCount;");

        indent--;
        AppendLine("};");
    }

    private void GenerateStatement(Statement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                sb.Append(GenerateExpression(exprStmt.Expression, bindings));
                sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                sb.Append(GenerateExpression(assignStmt.Target, bindings));
                sb.Append(" = ");
                sb.Append(GenerateExpression(assignStmt.Value, bindings));
                sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                sb.Append(GenerateExpression(compoundStmt.Target, bindings));
                sb.Append(' ');
                sb.Append(compoundStmt.Operator switch
                {
                    CompoundOperator.PlusEquals => "+=",
                    CompoundOperator.MinusEquals => "-=",
                    CompoundOperator.StarEquals => "*=",
                    CompoundOperator.SlashEquals => "/=",
                    _ => throw new InvalidOperationException()
                });
                sb.Append(' ');
                sb.Append(GenerateExpression(compoundStmt.Value, bindings));
                sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateIfStatement(ifStmt, bindings);
                break;

            case ForStatement forStmt:
                GenerateForStatement(forStmt, bindings);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateStatement(s, bindings);
                }
                indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateIfStatement(IfStatement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        Append(GenerateIndent());
        sb.Append("if (");
        sb.Append(GenerateExpression(stmt.Condition, bindings));
        sb.AppendLine(")");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateStatement(s, bindings);
        }
        indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("}");
            AppendLine("else");
            AppendLine("{");
            indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateStatement(s, bindings);
            }
            indent--;
        }

        AppendLine("}");
    }

    private void GenerateForStatement(ForStatement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        Append(GenerateIndent());
        sb.Append($"for (int {stmt.VariableName} = ");
        sb.Append(GenerateExpression(stmt.Start, bindings));
        sb.Append($"; {stmt.VariableName} < ");
        sb.Append(GenerateExpression(stmt.End, bindings));
        sb.AppendLine($"; {stmt.VariableName}++)");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.Body)
        {
            GenerateStatement(s, bindings);
        }
        indent--;

        AppendLine("}");
    }

    private string GenerateExpression(Expression expr, IReadOnlyList<QueryBinding> bindings)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => IsComponentName(id.Name, bindings)
                ? $"{id.Name}[idx]"
                : id.Name,

            MemberAccessExpression member => GenerateMemberAccess(member, bindings),

            BinaryExpression binary => $"({GenerateExpression(binary.Left, bindings)} {GetBinaryOp(binary.Operator)} {GenerateExpression(binary.Right, bindings)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateExpression(unary.Operand, bindings)})",

            CallExpression call => GenerateCall(call, bindings),

            HasExpression has => $"has_{has.ComponentName}", // Placeholder

            ParenthesizedExpression paren => $"({GenerateExpression(paren.Inner, bindings)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private string GenerateMemberAccess(MemberAccessExpression member, IReadOnlyList<QueryBinding> bindings)
    {
        if (member.Object is IdentifierExpression id && IsComponentName(id.Name, bindings))
        {
            return $"{id.Name}[idx].{member.MemberName}";
        }
        return $"{GenerateExpression(member.Object, bindings)}.{member.MemberName}";
    }

    private string GenerateCall(CallExpression call, IReadOnlyList<QueryBinding> bindings)
    {
        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateExpression(a, bindings)));
        return $"{funcName}({args})";
    }

    private static string MapFunctionName(string name)
    {
        // HLSL function mapping - most are the same, some differ
        return name switch
        {
            // Math functions - same in HLSL
            "sqrt" => "sqrt",
            "abs" => "abs",
            "min" => "min",
            "max" => "max",
            "clamp" => "clamp",
            "sin" => "sin",
            "cos" => "cos",
            "tan" => "tan",
            "asin" => "asin",
            "acos" => "acos",
            "atan" => "atan",
            "pow" => "pow",
            "exp" => "exp",
            "log" => "log",
            "floor" => "floor",
            "ceil" => "ceil",
            "round" => "round",
            "step" => "step",
            "smoothstep" => "smoothstep",
            "length" => "length",
            "distance" => "distance",
            "dot" => "dot",
            "cross" => "cross",
            "normalize" => "normalize",
            "reflect" => "reflect",
            "refract" => "refract",

            // GLSL to HLSL differences
            "fract" => "frac", // fract() in GLSL is frac() in HLSL
            "mod" => "fmod",   // mod() in GLSL is fmod() in HLSL
            "mix" => "lerp",   // mix() in GLSL is lerp() in HLSL
            "atan2" => "atan2",

            _ => name
        };
    }

    private static bool IsComponentName(string name, IReadOnlyList<QueryBinding> bindings)
    {
        return bindings.Any(b => b.ComponentName == name && b.AccessMode != AccessMode.Without);
    }

    private static string GetBinaryOp(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Less => "<",
        BinaryOperator.LessEqual => "<=",
        BinaryOperator.Greater => ">",
        BinaryOperator.GreaterEqual => ">=",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.And => "&&",
        BinaryOperator.Or => "||",
        _ => throw new ArgumentOutOfRangeException(nameof(op))
    };

    private static string GetUnaryOp(UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => "-",
        UnaryOperator.Not => "!",
        _ => throw new ArgumentOutOfRangeException(nameof(op))
    };

    private static string ToHlslType(TypeRef type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Float2 => "float2",
                PrimitiveTypeKind.Float3 => "float3",
                PrimitiveTypeKind.Float4 => "float4",
                PrimitiveTypeKind.Int => "int",
                PrimitiveTypeKind.Int2 => "int2",
                PrimitiveTypeKind.Int3 => "int3",
                PrimitiveTypeKind.Int4 => "int4",
                PrimitiveTypeKind.Uint => "uint",
                PrimitiveTypeKind.Bool => "bool",
                PrimitiveTypeKind.Mat4 => "float4x4",
                _ => throw new ArgumentOutOfRangeException(nameof(type), pt.Kind, "Unsupported primitive type")
            };
        }

        if (type is TextureType tt)
        {
            return ToHlslTextureType(tt.Kind);
        }

        if (type is SamplerType)
        {
            return "SamplerState";
        }

        throw new NotSupportedException($"Type {type.GetType().Name} not supported");
    }

    private static string ToHlslTextureType(TextureKind kind)
    {
        return kind switch
        {
            TextureKind.Texture2D => "Texture2D",
            TextureKind.TextureCube => "TextureCube",
            TextureKind.Texture3D => "Texture3D",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported texture type")
        };
    }

    private static string FormatFloat(float value)
    {
        var str = value.ToString("G9");
        if (!str.Contains('.') && !str.Contains('E') && !str.Contains('e'))
        {
            str += ".0";
        }
        // HLSL uses 'f' suffix for float literals
        str += "f";
        return str;
    }

    private void AppendLine(string? text = null)
    {
        if (text != null)
        {
            sb.Append(GenerateIndent());
            sb.AppendLine(text);
        }
        else
        {
            sb.AppendLine();
        }
    }

    private void Append(string text)
    {
        sb.Append(text);
    }

    private string GenerateIndent()
    {
        return new string(' ', indent * 4);
    }
}
