using KeenEyes.Lsp.Kesl.Protocol;
using KeenEyes.Lsp.Kesl.Services;

namespace KeenEyes.Lsp.Kesl.Handlers;

/// <summary>
/// Handles completion requests.
/// </summary>
/// <param name="documentManager">The document manager.</param>
public sealed class CompletionHandler(DocumentManager documentManager)
{
    // Declaration keywords
    private static readonly CompletionItem[] declarationKeywords =
    [
        new() { Label = "component", Kind = CompletionItemKind.Keyword, Detail = "Component declaration", InsertText = "component ${1:Name} {\n\t$0\n}" },
        new() { Label = "compute", Kind = CompletionItemKind.Keyword, Detail = "Compute shader declaration", InsertText = "compute ${1:Name} {\n\tquery {\n\t\t$0\n\t}\n\texecute() {\n\t}\n}" },
        new() { Label = "vertex", Kind = CompletionItemKind.Keyword, Detail = "Vertex shader declaration", InsertText = "vertex ${1:Name} {\n\tin {\n\t\t$0\n\t}\n\tout {\n\t}\n\texecute() {\n\t}\n}" },
        new() { Label = "fragment", Kind = CompletionItemKind.Keyword, Detail = "Fragment shader declaration", InsertText = "fragment ${1:Name} {\n\tin {\n\t\t$0\n\t}\n\tout {\n\t\tfragColor: float4 @ 0\n\t}\n\texecute() {\n\t}\n}" },
        new() { Label = "geometry", Kind = CompletionItemKind.Keyword, Detail = "Geometry shader declaration", InsertText = "geometry ${1:Name} {\n\tlayout {\n\t\tinput: triangles\n\t\toutput: triangle_strip\n\t\tmax_vertices: 3\n\t}\n\tin {\n\t\t$0\n\t}\n\tout {\n\t}\n\texecute() {\n\t}\n}" },
        new() { Label = "pipeline", Kind = CompletionItemKind.Keyword, Detail = "Pipeline declaration", InsertText = "pipeline ${1:Name} {\n\tvertex: ${2:VertexShader}\n\tfragment: ${3:FragmentShader}\n}" }
    ];

    // Block keywords
    private static readonly CompletionItem[] blockKeywords =
    [
        new() { Label = "query", Kind = CompletionItemKind.Keyword, Detail = "Query block (compute shaders)" },
        new() { Label = "params", Kind = CompletionItemKind.Keyword, Detail = "Parameters block" },
        new() { Label = "in", Kind = CompletionItemKind.Keyword, Detail = "Input block" },
        new() { Label = "out", Kind = CompletionItemKind.Keyword, Detail = "Output block" },
        new() { Label = "textures", Kind = CompletionItemKind.Keyword, Detail = "Textures block" },
        new() { Label = "samplers", Kind = CompletionItemKind.Keyword, Detail = "Samplers block" },
        new() { Label = "layout", Kind = CompletionItemKind.Keyword, Detail = "Layout block (geometry shaders)" },
        new() { Label = "execute", Kind = CompletionItemKind.Keyword, Detail = "Execute block" }
    ];

    // Access modifiers
    private static readonly CompletionItem[] accessModifiers =
    [
        new() { Label = "read", Kind = CompletionItemKind.Keyword, Detail = "Read-only access" },
        new() { Label = "write", Kind = CompletionItemKind.Keyword, Detail = "Read-write access" },
        new() { Label = "optional", Kind = CompletionItemKind.Keyword, Detail = "Optional component" },
        new() { Label = "without", Kind = CompletionItemKind.Keyword, Detail = "Exclude entities with component" }
    ];

    // types
    private static readonly CompletionItem[] types =
    [
        new() { Label = "float", Kind = CompletionItemKind.TypeParameter, Detail = "32-bit floating point" },
        new() { Label = "float2", Kind = CompletionItemKind.TypeParameter, Detail = "2D float vector" },
        new() { Label = "float3", Kind = CompletionItemKind.TypeParameter, Detail = "3D float vector" },
        new() { Label = "float4", Kind = CompletionItemKind.TypeParameter, Detail = "4D float vector" },
        new() { Label = "int", Kind = CompletionItemKind.TypeParameter, Detail = "32-bit signed integer" },
        new() { Label = "int2", Kind = CompletionItemKind.TypeParameter, Detail = "2D int vector" },
        new() { Label = "int3", Kind = CompletionItemKind.TypeParameter, Detail = "3D int vector" },
        new() { Label = "int4", Kind = CompletionItemKind.TypeParameter, Detail = "4D int vector" },
        new() { Label = "uint", Kind = CompletionItemKind.TypeParameter, Detail = "32-bit unsigned integer" },
        new() { Label = "bool", Kind = CompletionItemKind.TypeParameter, Detail = "Boolean" },
        new() { Label = "mat4", Kind = CompletionItemKind.TypeParameter, Detail = "4x4 matrix" },
        new() { Label = "texture2D", Kind = CompletionItemKind.TypeParameter, Detail = "2D texture" },
        new() { Label = "textureCube", Kind = CompletionItemKind.TypeParameter, Detail = "Cubemap texture" },
        new() { Label = "texture3D", Kind = CompletionItemKind.TypeParameter, Detail = "3D texture" },
        new() { Label = "sampler", Kind = CompletionItemKind.TypeParameter, Detail = "Texture sampler" }
    ];

    // Built-in functions
    private static readonly CompletionItem[] functions =
    [
        // Math
        new() { Label = "abs", Kind = CompletionItemKind.Function, Detail = "Absolute value" },
        new() { Label = "sign", Kind = CompletionItemKind.Function, Detail = "Sign of value (-1, 0, or 1)" },
        new() { Label = "floor", Kind = CompletionItemKind.Function, Detail = "Floor rounding" },
        new() { Label = "ceil", Kind = CompletionItemKind.Function, Detail = "Ceiling rounding" },
        new() { Label = "round", Kind = CompletionItemKind.Function, Detail = "Round to nearest integer" },
        new() { Label = "fract", Kind = CompletionItemKind.Function, Detail = "Fractional part" },
        new() { Label = "mod", Kind = CompletionItemKind.Function, Detail = "Modulo operation" },
        new() { Label = "min", Kind = CompletionItemKind.Function, Detail = "Minimum of two values" },
        new() { Label = "max", Kind = CompletionItemKind.Function, Detail = "Maximum of two values" },
        new() { Label = "clamp", Kind = CompletionItemKind.Function, Detail = "Clamp value to range" },
        new() { Label = "mix", Kind = CompletionItemKind.Function, Detail = "Linear interpolation" },
        new() { Label = "step", Kind = CompletionItemKind.Function, Detail = "Step function" },
        new() { Label = "smoothstep", Kind = CompletionItemKind.Function, Detail = "Smooth step function" },
        new() { Label = "sqrt", Kind = CompletionItemKind.Function, Detail = "Square root" },
        new() { Label = "pow", Kind = CompletionItemKind.Function, Detail = "Power function" },
        new() { Label = "exp", Kind = CompletionItemKind.Function, Detail = "e^x" },
        new() { Label = "log", Kind = CompletionItemKind.Function, Detail = "Natural logarithm" },
        new() { Label = "exp2", Kind = CompletionItemKind.Function, Detail = "2^x" },
        new() { Label = "log2", Kind = CompletionItemKind.Function, Detail = "Base-2 logarithm" },

        // Trigonometry
        new() { Label = "sin", Kind = CompletionItemKind.Function, Detail = "Sine" },
        new() { Label = "cos", Kind = CompletionItemKind.Function, Detail = "Cosine" },
        new() { Label = "tan", Kind = CompletionItemKind.Function, Detail = "Tangent" },
        new() { Label = "asin", Kind = CompletionItemKind.Function, Detail = "Arc sine" },
        new() { Label = "acos", Kind = CompletionItemKind.Function, Detail = "Arc cosine" },
        new() { Label = "atan", Kind = CompletionItemKind.Function, Detail = "Arc tangent" },
        new() { Label = "atan2", Kind = CompletionItemKind.Function, Detail = "Two-argument arc tangent" },

        // Vector operations
        new() { Label = "length", Kind = CompletionItemKind.Function, Detail = "Vector length" },
        new() { Label = "distance", Kind = CompletionItemKind.Function, Detail = "Distance between points" },
        new() { Label = "dot", Kind = CompletionItemKind.Function, Detail = "Dot product" },
        new() { Label = "cross", Kind = CompletionItemKind.Function, Detail = "Cross product" },
        new() { Label = "normalize", Kind = CompletionItemKind.Function, Detail = "Normalize vector" },
        new() { Label = "reflect", Kind = CompletionItemKind.Function, Detail = "Reflection vector" },
        new() { Label = "refract", Kind = CompletionItemKind.Function, Detail = "Refraction vector" },

        // Texture
        new() { Label = "sample", Kind = CompletionItemKind.Function, Detail = "Sample texture" },
        new() { Label = "has", Kind = CompletionItemKind.Function, Detail = "Check if optional component exists" },

        // Geometry shader
        new() { Label = "emit", Kind = CompletionItemKind.Function, Detail = "Emit vertex (geometry shader)" },
        new() { Label = "endPrimitive", Kind = CompletionItemKind.Function, Detail = "End primitive (geometry shader)" }
    ];

    // Control flow
    private static readonly CompletionItem[] controlFlow =
    [
        new() { Label = "if", Kind = CompletionItemKind.Keyword, Detail = "Conditional statement", InsertText = "if ($1) {\n\t$0\n}" },
        new() { Label = "else", Kind = CompletionItemKind.Keyword, Detail = "Else branch" },
        new() { Label = "for", Kind = CompletionItemKind.Keyword, Detail = "For loop", InsertText = "for (${1:i}: ${2:0}..${3:10}) {\n\t$0\n}" },
        new() { Label = "while", Kind = CompletionItemKind.Keyword, Detail = "While loop", InsertText = "while ($1) {\n\t$0\n}" },
        new() { Label = "return", Kind = CompletionItemKind.Keyword, Detail = "Return statement" },
        new() { Label = "break", Kind = CompletionItemKind.Keyword, Detail = "Break statement" },
        new() { Label = "continue", Kind = CompletionItemKind.Keyword, Detail = "Continue statement" }
    ];

    // Topology keywords
    private static readonly CompletionItem[] topologies =
    [
        new() { Label = "points", Kind = CompletionItemKind.Keyword, Detail = "Point topology" },
        new() { Label = "lines", Kind = CompletionItemKind.Keyword, Detail = "Line topology" },
        new() { Label = "lines_adjacency", Kind = CompletionItemKind.Keyword, Detail = "Lines with adjacency" },
        new() { Label = "triangles", Kind = CompletionItemKind.Keyword, Detail = "Triangle topology" },
        new() { Label = "triangles_adjacency", Kind = CompletionItemKind.Keyword, Detail = "Triangles with adjacency" },
        new() { Label = "line_strip", Kind = CompletionItemKind.Keyword, Detail = "Line strip output" },
        new() { Label = "triangle_strip", Kind = CompletionItemKind.Keyword, Detail = "Triangle strip output" }
    ];

    /// <summary>
    /// Handles a completion request.
    /// </summary>
    /// <param name="params">The completion parameters.</param>
    /// <returns>The completion list.</returns>
    public CompletionList Handle(CompletionParams @params)
    {
        var document = documentManager.GetDocument(@params.TextDocument.Uri);
        if (document == null)
        {
            return new CompletionList { Items = [] };
        }

        var line = @params.Position.Line;
        var character = @params.Position.Character;

        // Get the current word being typed
        var word = document.GetWordAtPosition(line, character);

        // Get context to determine appropriate completions
        var context = DetermineContext(document, line, character);

        var items = GetCompletionsForContext(context, word);

        return new CompletionList
        {
            IsIncomplete = false,
            Items = items
        };
    }

    private static CompletionContext DetermineContext(DocumentState document, int line, int character)
    {
        // Simple context detection based on text before cursor
        var lines = document.Lines;
        if (line >= lines.Length)
        {
            return CompletionContext.TopLevel;
        }

        var currentLine = lines[line].TrimEnd('\r');
        var textBeforeCursor = character <= currentLine.Length ? currentLine[..character] : currentLine;

        // Check for specific patterns
        if (textBeforeCursor.TrimStart().StartsWith(':'))
        {
            return CompletionContext.TypeAnnotation;
        }

        if (textBeforeCursor.Contains("query"))
        {
            return CompletionContext.QueryBlock;
        }

        if (textBeforeCursor.Contains("layout"))
        {
            return CompletionContext.LayoutBlock;
        }

        if (textBeforeCursor.Contains("execute"))
        {
            return CompletionContext.ExecuteBlock;
        }

        // Check previous lines for context
        for (var i = line; i >= 0; i--)
        {
            var lineText = lines[i].TrimEnd('\r').Trim();
            if (lineText.Contains("execute()") || lineText.Contains("execute ()"))
            {
                return CompletionContext.ExecuteBlock;
            }
            if (lineText.StartsWith("query"))
            {
                return CompletionContext.QueryBlock;
            }
            if (lineText.StartsWith("layout"))
            {
                return CompletionContext.LayoutBlock;
            }
            if (lineText.StartsWith("compute") || lineText.StartsWith("vertex") ||
                lineText.StartsWith("fragment") || lineText.StartsWith("geometry") ||
                lineText.StartsWith("pipeline") || lineText.StartsWith("component"))
            {
                return CompletionContext.InsideDeclaration;
            }
        }

        return CompletionContext.TopLevel;
    }

    private static IReadOnlyList<CompletionItem> GetCompletionsForContext(CompletionContext context, string? word)
    {
        var items = new List<CompletionItem>();

        switch (context)
        {
            case CompletionContext.TopLevel:
                items.AddRange(declarationKeywords);
                break;

            case CompletionContext.InsideDeclaration:
                items.AddRange(blockKeywords);
                items.AddRange(types);
                break;

            case CompletionContext.QueryBlock:
                items.AddRange(accessModifiers);
                break;

            case CompletionContext.TypeAnnotation:
                items.AddRange(types);
                break;

            case CompletionContext.ExecuteBlock:
                items.AddRange(functions);
                items.AddRange(controlFlow);
                items.AddRange(types);
                break;

            case CompletionContext.LayoutBlock:
                items.AddRange(topologies);
                items.Add(new CompletionItem { Label = "input", Kind = CompletionItemKind.Keyword, Detail = "Input topology" });
                items.Add(new CompletionItem { Label = "output", Kind = CompletionItemKind.Keyword, Detail = "Output topology" });
                items.Add(new CompletionItem { Label = "max_vertices", Kind = CompletionItemKind.Keyword, Detail = "Maximum output vertices" });
                break;
        }

        // Filter by word prefix if available
        if (!string.IsNullOrEmpty(word))
        {
            items = items.Where(i => i.Label.StartsWith(word, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items;
    }

    private enum CompletionContext
    {
        TopLevel,
        InsideDeclaration,
        QueryBlock,
        TypeAnnotation,
        ExecuteBlock,
        LayoutBlock
    }
}
