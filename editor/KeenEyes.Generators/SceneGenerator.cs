// Copyright (c) KeenEyes Contributors. Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KeenEyes.Generators.AssetModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates static scene loading methods from .kescene files.
/// </summary>
[Generator]
public sealed class SceneGenerator : IIncrementalGenerator
{
    private const string SceneExtension = ".kescene";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .kescene files marked as AdditionalFiles
        var sceneFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(SceneExtension, StringComparison.OrdinalIgnoreCase));

        // Combine with compilation to validate component types
        var compilationAndFiles = context.CompilationProvider
            .Combine(sceneFiles.Collect());

        // Generate C# for each .kescene file
        context.RegisterSourceOutput(compilationAndFiles, static (ctx, source) =>
        {
            var (compilation, files) = source;
            var rootNamespace = GetRootNamespace(compilation);
            var sceneInfos = new List<SceneInfo>();

            foreach (var file in files)
            {
                var sceneInfo = ProcessSceneFile(ctx, file, compilation, rootNamespace);
                if (sceneInfo != null)
                {
                    sceneInfos.Add(sceneInfo);
                }
            }

            // Generate the combined Scenes class
            if (sceneInfos.Count > 0)
            {
                var combinedSource = GenerateScenesClass(sceneInfos, rootNamespace);
                ctx.AddSource("Scenes.g.cs", SourceText.From(combinedSource, Encoding.UTF8));
            }
        });
    }

    private static string GetRootNamespace(Compilation compilation)
    {
        return compilation.AssemblyName ?? "Generated";
    }

    private static SceneInfo? ProcessSceneFile(
        SourceProductionContext context,
        AdditionalText file,
        Compilation compilation,
        string rootNamespace)
    {
        var sourceText = file.GetText(context.CancellationToken);
        if (sourceText is null)
        {
            return null;
        }

        var json = sourceText.ToString();
        var filePath = file.Path;
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Parse the JSON
        var scene = JsonParser.ParseScene(json);
        if (scene is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.SceneInvalidJson,
                Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                filePath, "Failed to parse JSON"));
            return null;
        }

        // Use file name if scene name is empty
        var sceneName = string.IsNullOrWhiteSpace(scene.Name) ? fileName : scene.Name;

        // Validate entity IDs are unique
        var entityIds = new HashSet<string>();
        foreach (var entity in scene.Entities)
        {
            if (!string.IsNullOrEmpty(entity.Id) && !entityIds.Add(entity.Id))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.SceneDuplicateEntityId,
                    Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                    entity.Id, filePath));
            }
        }

        // Validate parent references exist
        foreach (var entity in scene.Entities)
        {
            if (!string.IsNullOrEmpty(entity.Parent) && !entityIds.Contains(entity.Parent!))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.SceneEntityReferenceNotFound,
                    Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                    entity.Parent, filePath));
            }
        }

        // Validate component types exist
        foreach (var entity in scene.Entities)
        {
            foreach (var componentName in entity.Components.Keys)
            {
                var componentType = FindComponentType(compilation, componentName);
                if (componentType is null)
                {
                    var suggestion = FindSimilarType(compilation, componentName);
                    if (suggestion != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.SceneComponentTypeNotFoundWithSuggestion,
                            Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                            componentName, suggestion));
                    }
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.SceneComponentTypeNotFound,
                            Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                            componentName));
                    }
                }
            }
        }

        return new SceneInfo(sceneName, scene, filePath);
    }

    private static INamedTypeSymbol? FindComponentType(Compilation compilation, string typeName)
    {
        // Try to find the type by simple name first
        var candidates = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.TypeKind == TypeKind.Struct)
            .ToList();

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // If multiple or none found, try fully qualified lookup
        var type = compilation.GetTypeByMetadataName(typeName);
        if (type != null)
        {
            return type;
        }

        return candidates.FirstOrDefault();
    }

    private static string? FindSimilarType(Compilation compilation, string typeName)
    {
        // Simple Levenshtein-based suggestion
        var allTypes = compilation.GetSymbolsWithName(
            name => true,
            SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.TypeKind == TypeKind.Struct)
            .Select(t => t.Name)
            .ToList();

        var loweredTypeName = typeName.ToLowerInvariant();
        return allTypes
            .Where(t => t.ToLowerInvariant().Contains(loweredTypeName) ||
                       loweredTypeName.Contains(t.ToLowerInvariant()) ||
                       LevenshteinDistance(t.ToLowerInvariant(), loweredTypeName) <= 2)
            .FirstOrDefault();
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }

    private static string GenerateScenesClass(List<SceneInfo> scenes, string rootNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Provides methods to load scenes defined in .kescene files.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class Scenes");
        sb.AppendLine("{");

        // Generate All property
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// All scene names available in this project.");
        sb.AppendLine("    /// </summary>");
        sb.Append("    public static IReadOnlyList<string> All { get; } = new string[] { ");
        sb.Append(string.Join(", ", scenes.Select(s => $"\"{s.Name}\"")));
        sb.AppendLine(" };");
        sb.AppendLine();

        // Generate load method for each scene
        foreach (var scene in scenes)
        {
            GenerateLoadMethod(sb, scene);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateLoadMethod(StringBuilder sb, SceneInfo sceneInfo)
    {
        var scene = sceneInfo.Model;
        var methodName = SanitizeIdentifier(sceneInfo.Name);

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Loads the {sceneInfo.Name} scene into the world.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"world\">The world to load entities into.</param>");
        sb.AppendLine($"    public static void Load{methodName}(global::KeenEyes.World world)");
        sb.AppendLine("    {");

        // Build topological order (entities without parents first, then children)
        var orderedEntities = TopologicalSort(scene.Entities);

        // Track which entities have variables for parenting
        var variableNames = new Dictionary<string, string>();

        foreach (var entity in orderedEntities)
        {
            var varName = GenerateVariableName(entity, variableNames);
            variableNames[entity.Id] = varName;

            // Generate entity spawn code
            sb.AppendLine($"        var {varName} = world.Spawn(\"{EscapeString(entity.Name)}\")");

            foreach (var kvp in entity.Components)
            {
                var componentName = kvp.Key;
                var componentData = kvp.Value;

                sb.Append($"            .With(new global::{componentName}");
                if (componentData.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("            {");
                    GenerateComponentInitializer(sb, componentData, "                ");
                    sb.Append("            }");
                }
                else
                {
                    sb.Append("()");
                }
                sb.AppendLine(")");
            }

            sb.AppendLine("            .Build();");

            // Set parent if specified
            if (!string.IsNullOrEmpty(entity.Parent))
            {
                string? parentVar;
                if (variableNames.TryGetValue(entity.Parent!, out parentVar))
                {
                    sb.AppendLine($"        world.SetParent({varName}, {parentVar});");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static List<EntityModel> TopologicalSort(List<EntityModel> entities)
    {
        var result = new List<EntityModel>();

        // Build dictionary, handling duplicates by keeping first occurrence
        var entityById = new Dictionary<string, EntityModel>();
        foreach (var entity in entities)
        {
            if (!string.IsNullOrEmpty(entity.Id) && !entityById.ContainsKey(entity.Id))
            {
                entityById[entity.Id] = entity;
            }
        }

        var visited = new HashSet<string>();

        void Visit(EntityModel entity)
        {
            if (visited.Contains(entity.Id))
            {
                return;
            }

            // Visit parent first if it exists
            if (!string.IsNullOrEmpty(entity.Parent))
            {
                EntityModel? parent;
                if (entityById.TryGetValue(entity.Parent!, out parent))
                {
                    Visit(parent);
                }
            }

            visited.Add(entity.Id);
            result.Add(entity);
        }

        foreach (var entity in entities)
        {
            Visit(entity);
        }

        return result;
    }

    private static string GenerateVariableName(EntityModel entity, Dictionary<string, string> existingNames)
    {
        var baseName = SanitizeIdentifier(entity.Name);
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = "entity";
        }

        // Make camelCase
        baseName = char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);

        var name = baseName;
        var counter = 1;
        while (existingNames.Values.Contains(name))
        {
            name = $"{baseName}{counter++}";
        }

        return name;
    }

    private static void GenerateComponentInitializer(
        StringBuilder sb,
        Dictionary<string, object?> data,
        string indent)
    {
        var isFirst = true;
        foreach (var kvp in data)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (!isFirst)
            {
                sb.AppendLine(",");
            }
            isFirst = false;

            sb.Append($"{indent}{key} = ");
            GenerateValue(sb, value, indent);
        }
        sb.AppendLine();
    }

    private static void GenerateValue(StringBuilder sb, object? value, string indent)
    {
        switch (value)
        {
            case null:
                sb.Append("default");
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case int i:
                sb.Append(i);
                break;
            case long l:
                sb.Append($"{l}L");
                break;
            case double d:
                sb.Append($"{d}f");
                break;
            case string s:
                sb.Append($"\"{EscapeString(s)}\"");
                break;
            case Dictionary<string, object?> nested:
                // This could be a Vector3, Vector2, etc.
                GenerateNestedValue(sb, nested, indent);
                break;
            case List<object?> list:
                sb.Append("new[] { ");
                for (var i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    GenerateValue(sb, list[i], indent);
                }
                sb.Append(" }");
                break;
            default:
                sb.Append($"{value}");
                break;
        }
    }

    private static void GenerateNestedValue(StringBuilder sb, Dictionary<string, object?> nested, string indent)
    {
        // Check for common vector types
        if (nested.ContainsKey("X") && nested.ContainsKey("Y"))
        {
            if (nested.ContainsKey("Z"))
            {
                if (nested.ContainsKey("W"))
                {
                    // Vector4 or Quaternion
                    sb.Append($"new global::System.Numerics.Vector4({FormatFloat(nested["X"])}, {FormatFloat(nested["Y"])}, {FormatFloat(nested["Z"])}, {FormatFloat(nested["W"])})");
                }
                else
                {
                    // Vector3
                    sb.Append($"new global::System.Numerics.Vector3({FormatFloat(nested["X"])}, {FormatFloat(nested["Y"])}, {FormatFloat(nested["Z"])})");
                }
            }
            else
            {
                // Vector2
                sb.Append($"new global::System.Numerics.Vector2({FormatFloat(nested["X"])}, {FormatFloat(nested["Y"])})");
            }
        }
        else
        {
            // Generic nested object initialization
            sb.AppendLine("new()");
            sb.AppendLine($"{indent}{{");
            GenerateComponentInitializer(sb, nested, indent + "    ");
            sb.Append($"{indent}}}");
        }
    }

    private static string FormatFloat(object? value)
    {
        return value switch
        {
            int i => $"{i}f",
            long l => $"{l}f",
            double d => $"{d}f",
            float f => $"{f}f",
            _ => "0f"
        };
    }

    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();

        // Ensure it doesn't start with a digit
        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return result;
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private sealed class SceneInfo
    {
        public string Name { get; }
        public SceneModel Model { get; }
        public string FilePath { get; }

        public SceneInfo(string name, SceneModel model, string filePath)
        {
            Name = name;
            Model = model;
            FilePath = filePath;
        }
    }
}

// Diagnostics for scene generator (KEEN060-KEEN069 range)
internal static partial class Diagnostics
{
    /// <summary>
    /// KEEN060: Component type not found.
    /// </summary>
    public static readonly DiagnosticDescriptor SceneComponentTypeNotFound = new(
        id: "KEEN060",
        title: "Component type not found",
        messageFormat: "Component type '{0}' not found",
        category: "KeenEyes.Scene",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified component type could not be found in the compilation.");

    /// <summary>
    /// KEEN060: Component type not found (with suggestion).
    /// </summary>
    public static readonly DiagnosticDescriptor SceneComponentTypeNotFoundWithSuggestion = new(
        id: "KEEN060",
        title: "Component type not found",
        messageFormat: "Component type '{0}' not found - did you mean '{1}'?",
        category: "KeenEyes.Scene",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified component type could not be found. A similar type was suggested.");

    /// <summary>
    /// KEEN061: Entity reference not found.
    /// </summary>
    public static readonly DiagnosticDescriptor SceneEntityReferenceNotFound = new(
        id: "KEEN061",
        title: "Entity reference not found",
        messageFormat: "Entity reference '{0}' not found in '{1}'",
        category: "KeenEyes.Scene",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified parent entity ID does not exist in the scene.");

    /// <summary>
    /// KEEN062: Invalid JSON in asset file.
    /// </summary>
    public static readonly DiagnosticDescriptor SceneInvalidJson = new(
        id: "KEEN062",
        title: "Invalid JSON",
        messageFormat: "Invalid JSON in {0}: {1}",
        category: "KeenEyes.Scene",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The asset file contains invalid JSON that could not be parsed.");

    /// <summary>
    /// KEEN063: Duplicate entity ID.
    /// </summary>
    public static readonly DiagnosticDescriptor SceneDuplicateEntityId = new(
        id: "KEEN063",
        title: "Duplicate entity ID",
        messageFormat: "Duplicate entity ID '{0}' in '{1}'",
        category: "KeenEyes.Scene",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each entity in a scene must have a unique ID.");
}
