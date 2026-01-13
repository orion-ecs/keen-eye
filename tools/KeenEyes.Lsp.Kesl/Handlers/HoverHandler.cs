using KeenEyes.Lsp.Kesl.Protocol;
using KeenEyes.Lsp.Kesl.Services;

namespace KeenEyes.Lsp.Kesl.Handlers;

/// <summary>
/// Handles hover requests.
/// </summary>
/// <param name="documentManager">The document manager.</param>
public sealed class HoverHandler(DocumentManager documentManager)
{
    // documentation for KESL keywords, types, and functions
    private static readonly Dictionary<string, string> documentation = new(StringComparer.OrdinalIgnoreCase)
    {
        // Declaration keywords
        ["component"] = "Defines an ECS component with typed fields.\n\n```kesl\ncomponent Position {\n    x: float\n    y: float\n}\n```",
        ["compute"] = "Defines a compute shader that processes entities with matching components.\n\n```kesl\ncompute UpdatePhysics {\n    query { write Position, read Velocity }\n    execute() {\n        Position.x += Velocity.x;\n    }\n}\n```",
        ["vertex"] = "Defines a vertex shader stage with inputs, outputs, and uniforms.\n\n```kesl\nvertex TransformVertex {\n    in { position: float3 @ 0 }\n    out { worldPos: float3 }\n    execute() {\n        worldPos = position;\n    }\n}\n```",
        ["fragment"] = "Defines a fragment (pixel) shader stage.\n\n```kesl\nfragment LitSurface {\n    in { worldNormal: float3 }\n    out { fragColor: float4 @ 0 }\n    execute() {\n        fragColor = float4(worldNormal, 1.0);\n    }\n}\n```",
        ["geometry"] = "Defines a geometry shader stage for vertex manipulation.\n\n```kesl\ngeometry Wireframe {\n    layout {\n        input: triangles\n        output: line_strip\n        max_vertices: 6\n    }\n    in { position: float3 }\n    out { color: float4 }\n    execute() {\n        emit(position);\n    }\n}\n```",
        ["pipeline"] = "Composes vertex, geometry (optional), and fragment shaders into a rendering pipeline.\n\n```kesl\npipeline DeferredLit {\n    vertex: TransformVertex\n    fragment: LitSurface\n}\n```",

        // Block keywords
        ["query"] = "Defines which components to access in a compute shader.\n\nAccess modes:\n- `read` - Read-only access\n- `write` - Read-write access\n- `optional` - Component may not be present\n- `without` - Exclude entities with component",
        ["params"] = "Defines uniform parameters passed from CPU to shader.",
        ["in"] = "Defines input attributes/varyings for a shader stage.",
        ["out"] = "Defines output attributes/varyings from a shader stage.",
        ["textures"] = "Defines texture resources with binding slots.",
        ["samplers"] = "Defines sampler resources with binding slots.",
        ["layout"] = "Defines geometry shader input/output topology and max vertices.",
        ["execute"] = "Contains the shader's executable code.",

        // Access modifiers
        ["read"] = "Read-only access to a component. The component must exist on the entity.",
        ["write"] = "Read-write access to a component. The component must exist on the entity.",
        ["optional"] = "Optional component access. Use `has(ComponentName)` to check existence.",
        ["without"] = "Excludes entities that have this component from the query.",

        // Types
        ["float"] = "32-bit floating-point number.\n\nEquivalent to `float` in GLSL/HLSL.",
        ["float2"] = "2-component floating-point vector.\n\nEquivalent to `vec2` in GLSL or `float2` in HLSL.",
        ["float3"] = "3-component floating-point vector.\n\nEquivalent to `vec3` in GLSL or `float3` in HLSL.",
        ["float4"] = "4-component floating-point vector.\n\nEquivalent to `vec4` in GLSL or `float4` in HLSL.",
        ["int"] = "32-bit signed integer.\n\nEquivalent to `int` in GLSL/HLSL.",
        ["int2"] = "2-component signed integer vector.\n\nEquivalent to `ivec2` in GLSL or `int2` in HLSL.",
        ["int3"] = "3-component signed integer vector.\n\nEquivalent to `ivec3` in GLSL or `int3` in HLSL.",
        ["int4"] = "4-component signed integer vector.\n\nEquivalent to `ivec4` in GLSL or `int4` in HLSL.",
        ["uint"] = "32-bit unsigned integer.\n\nEquivalent to `uint` in GLSL/HLSL.",
        ["bool"] = "Boolean type.\n\nEquivalent to `bool` in GLSL/HLSL.",
        ["mat4"] = "4x4 floating-point matrix.\n\nEquivalent to `mat4` in GLSL or `float4x4` in HLSL.",
        ["texture2D"] = "2D texture resource.\n\nUse with `sample(texture, sampler, uv)` to read texels.",
        ["textureCube"] = "Cubemap texture resource.\n\nUse with `sample(texture, sampler, direction)` to read texels.",
        ["texture3D"] = "3D volume texture resource.\n\nUse with `sample(texture, sampler, uvw)` to read texels.",
        ["sampler"] = "Texture sampler with filtering and addressing modes.",

        // Math functions
        ["abs"] = "Returns the absolute value of the input.\n\n```kesl\nabs(x)\nabs(vec)\n```",
        ["sign"] = "Returns the sign of the input (-1, 0, or 1).\n\n```kesl\nsign(x)\n```",
        ["floor"] = "Rounds down to the nearest integer.\n\n```kesl\nfloor(x)\n```",
        ["ceil"] = "Rounds up to the nearest integer.\n\n```kesl\nceil(x)\n```",
        ["round"] = "Rounds to the nearest integer.\n\n```kesl\nround(x)\n```",
        ["fract"] = "Returns the fractional part of the input.\n\n```kesl\nfract(x)\n```",
        ["mod"] = "Returns the remainder of division.\n\n```kesl\nmod(x, y)\n```",
        ["min"] = "Returns the minimum of two values.\n\n```kesl\nmin(a, b)\n```",
        ["max"] = "Returns the maximum of two values.\n\n```kesl\nmax(a, b)\n```",
        ["clamp"] = "Clamps a value to the range [min, max].\n\n```kesl\nclamp(x, minVal, maxVal)\n```",
        ["mix"] = "Linear interpolation between two values.\n\n```kesl\nmix(a, b, t)  // Returns a * (1 - t) + b * t\n```",
        ["step"] = "Step function. Returns 0 if x < edge, 1 otherwise.\n\n```kesl\nstep(edge, x)\n```",
        ["smoothstep"] = "Smooth Hermite interpolation.\n\n```kesl\nsmoothstep(edge0, edge1, x)\n```",
        ["sqrt"] = "Returns the square root.\n\n```kesl\nsqrt(x)\n```",
        ["pow"] = "Returns x raised to the power y.\n\n```kesl\npow(x, y)\n```",
        ["exp"] = "Returns e raised to the power x.\n\n```kesl\nexp(x)\n```",
        ["log"] = "Returns the natural logarithm of x.\n\n```kesl\nlog(x)\n```",
        ["exp2"] = "Returns 2 raised to the power x.\n\n```kesl\nexp2(x)\n```",
        ["log2"] = "Returns the base-2 logarithm of x.\n\n```kesl\nlog2(x)\n```",

        // Trigonometry
        ["sin"] = "Returns the sine of an angle (in radians).\n\n```kesl\nsin(angle)\n```",
        ["cos"] = "Returns the cosine of an angle (in radians).\n\n```kesl\ncos(angle)\n```",
        ["tan"] = "Returns the tangent of an angle (in radians).\n\n```kesl\ntan(angle)\n```",
        ["asin"] = "Returns the arc sine. Output is in radians.\n\n```kesl\nasin(x)\n```",
        ["acos"] = "Returns the arc cosine. Output is in radians.\n\n```kesl\nacos(x)\n```",
        ["atan"] = "Returns the arc tangent. Output is in radians.\n\n```kesl\natan(x)\natan(y, x)  // Two-argument form\n```",
        ["atan2"] = "Returns the arc tangent of y/x, handling quadrants correctly.\n\n```kesl\natan2(y, x)\n```",

        // Vector operations
        ["length"] = "Returns the length of a vector.\n\n```kesl\nlength(vec)\n```",
        ["distance"] = "Returns the distance between two points.\n\n```kesl\ndistance(p1, p2)\n```",
        ["dot"] = "Returns the dot product of two vectors.\n\n```kesl\ndot(a, b)\n```",
        ["cross"] = "Returns the cross product of two 3D vectors.\n\n```kesl\ncross(a, b)\n```",
        ["normalize"] = "Returns a unit vector in the same direction.\n\n```kesl\nnormalize(vec)\n```",
        ["reflect"] = "Returns the reflection direction.\n\n```kesl\nreflect(incident, normal)\n```",
        ["refract"] = "Returns the refraction direction.\n\n```kesl\nrefract(incident, normal, eta)\n```",

        // Texture sampling
        ["sample"] = "Samples a texture at the given coordinates.\n\n```kesl\nfloat4 color = sample(texture, sampler, uv);\n```",
        ["has"] = "Checks if an optional component exists on the current entity.\n\n```kesl\nif (has(OptionalComponent)) {\n    // Component exists\n}\n```",

        // Geometry shader
        ["emit"] = "Emits a vertex with the current output values (geometry shader only).\n\n```kesl\nemit(position);\n```",
        ["endPrimitive"] = "Ends the current primitive and starts a new one (geometry shader only).\n\n```kesl\nendPrimitive();\n```",

        // Topology
        ["points"] = "Point primitive topology. Each vertex is a separate point.",
        ["lines"] = "Line primitive topology. Every two vertices form a line.",
        ["lines_adjacency"] = "Lines with adjacency information. Every 4 vertices form a line with neighbors.",
        ["triangles"] = "Triangle primitive topology. Every three vertices form a triangle.",
        ["triangles_adjacency"] = "Triangles with adjacency information. Every 6 vertices form a triangle with neighbors.",
        ["line_strip"] = "Connected line strip output.",
        ["triangle_strip"] = "Connected triangle strip output.",

        // Control flow
        ["if"] = "Conditional statement.\n\n```kesl\nif (condition) {\n    // then branch\n} else {\n    // else branch\n}\n```",
        ["else"] = "Else branch of a conditional statement.",
        ["for"] = "Range-based for loop.\n\n```kesl\nfor (i: 0..10) {\n    // Loop body\n}\n```",
        ["while"] = "While loop.\n\n```kesl\nwhile (condition) {\n    // Loop body\n}\n```",
        ["return"] = "Returns from the current function.",
        ["break"] = "Breaks out of the current loop.",
        ["continue"] = "Continues to the next iteration of the current loop."
    };

    /// <summary>
    /// Handles a hover request.
    /// </summary>
    /// <param name="params">The hover parameters.</param>
    /// <returns>The hover information, or null if no hover available.</returns>
    public Hover? Handle(HoverParams @params)
    {
        var document = documentManager.GetDocument(@params.TextDocument.Uri);
        if (document == null)
        {
            return null;
        }

        var word = document.GetWordAtPosition(@params.Position.Line, @params.Position.Character);
        if (string.IsNullOrEmpty(word))
        {
            return null;
        }

        if (documentation.TryGetValue(word, out var doc))
        {
            return new Hover
            {
                Contents = MarkupContent.Markdown(doc)
            };
        }

        return null;
    }
}
