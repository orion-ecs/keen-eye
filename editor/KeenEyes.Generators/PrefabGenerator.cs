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
/// Generates static prefab spawn methods from .keprefab files.
/// </summary>
[Generator]
public sealed class PrefabGenerator : IIncrementalGenerator
{
    private const string PrefabExtension = ".keprefab";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .keprefab files marked as AdditionalFiles
        var prefabFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(PrefabExtension, StringComparison.OrdinalIgnoreCase));

        // Combine with compilation to validate component types
        var compilationAndFiles = context.CompilationProvider
            .Combine(prefabFiles.Collect());

        // Generate C# for each .keprefab file
        context.RegisterSourceOutput(compilationAndFiles, static (ctx, source) =>
        {
            var (compilation, files) = source;
            var rootNamespace = GetRootNamespace(compilation);
            var prefabInfos = new List<PrefabInfo>();

            foreach (var file in files)
            {
                var prefabInfo = ProcessPrefabFile(ctx, file, compilation, rootNamespace);
                if (prefabInfo != null)
                {
                    prefabInfos.Add(prefabInfo);
                }
            }

            // Generate the combined Prefabs class
            if (prefabInfos.Count > 0)
            {
                var combinedSource = GeneratePrefabsClass(prefabInfos, rootNamespace);
                ctx.AddSource("Prefabs.g.cs", SourceText.From(combinedSource, Encoding.UTF8));
            }
        });
    }

    private static string GetRootNamespace(Compilation compilation)
    {
        return compilation.AssemblyName ?? "Generated";
    }

    private static PrefabInfo? ProcessPrefabFile(
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
        var prefab = JsonParser.ParsePrefab(json);
        if (prefab is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PrefabInvalidJson,
                Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                filePath, "Failed to parse JSON"));
            return null;
        }

        // Use file name if prefab name is empty
        var prefabName = string.IsNullOrWhiteSpace(prefab.Name) ? fileName : prefab.Name;

        // Validate component types in root entity
        if (prefab.Root != null)
        {
            ValidateEntityComponents(context, prefab.Root, compilation, filePath);
        }

        // Validate component types in all children (recursively)
        foreach (var child in prefab.Children)
        {
            ValidateEntityComponentsRecursive(context, child, compilation, filePath);
        }

        // Validate overridable fields reference valid component.field paths
        foreach (var fieldPath in prefab.OverridableFields)
        {
            ValidateOverrideField(context, prefab, fieldPath, compilation, filePath);
        }

        return new PrefabInfo(prefabName, prefab, filePath);
    }

    private static void ValidateEntityComponentsRecursive(
        SourceProductionContext context,
        PrefabEntityModel entity,
        Compilation compilation,
        string filePath)
    {
        ValidateEntityComponents(context, entity, compilation, filePath);

        foreach (var child in entity.Children)
        {
            ValidateEntityComponentsRecursive(context, child, compilation, filePath);
        }
    }

    private static void ValidateEntityComponents(
        SourceProductionContext context,
        PrefabEntityModel entity,
        Compilation compilation,
        string filePath)
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
                        Diagnostics.PrefabComponentTypeNotFoundWithSuggestion,
                        Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                        componentName, suggestion));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.PrefabComponentTypeNotFound,
                        Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                        componentName));
                }
            }
        }
    }

    private static void ValidateOverrideField(
        SourceProductionContext context,
        PrefabModel prefab,
        string fieldPath,
        Compilation compilation,
        string filePath)
    {
        // Field path format: "ComponentType.FieldName" e.g., "Transform.Position"
        var parts = fieldPath.Split('.');
        if (parts.Length != 2)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PrefabInvalidOverrideField,
                Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                fieldPath));
            return;
        }

        var componentName = parts[0];
        // Note: fieldName (parts[1]) validation would require reflection on the component type,
        // which is deferred to runtime. We only validate the component exists here.

        // Check if the component exists in the root or any entity
        var found = false;
        if (prefab.Root != null && prefab.Root.Components.ContainsKey(componentName))
        {
            found = true;
        }

        if (!found)
        {
            foreach (var child in prefab.Children)
            {
                if (HasComponent(child, componentName))
                {
                    found = true;
                    break;
                }
            }
        }

        if (!found)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PrefabOverrideFieldNotFound,
                Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                fieldPath, componentName));
        }
    }

    private static bool HasComponent(PrefabEntityModel entity, string componentName)
    {
        if (entity.Components.ContainsKey(componentName))
        {
            return true;
        }

        foreach (var child in entity.Children)
        {
            if (HasComponent(child, componentName))
            {
                return true;
            }
        }

        return false;
    }

    private static INamedTypeSymbol? FindComponentType(Compilation compilation, string typeName)
    {
        var candidates = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.TypeKind == TypeKind.Struct)
            .ToList();

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var type = compilation.GetTypeByMetadataName(typeName);
        if (type != null)
        {
            return type;
        }

        return candidates.FirstOrDefault();
    }

    private static string? FindSimilarType(Compilation compilation, string typeName)
    {
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

    private static string GeneratePrefabsClass(List<PrefabInfo> prefabs, string rootNamespace)
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
        sb.AppendLine("/// Provides methods to spawn prefabs defined in .keprefab files.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class Prefabs");
        sb.AppendLine("{");

        // Generate All property
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// All prefab names available in this project.");
        sb.AppendLine("    /// </summary>");
        sb.Append("    public static IReadOnlyList<string> All { get; } = new string[] { ");
        sb.Append(string.Join(", ", prefabs.Select(p => $"\"{p.Name}\"")));
        sb.AppendLine(" };");
        sb.AppendLine();

        // Generate spawn method for each prefab
        foreach (var prefab in prefabs)
        {
            GenerateSpawnMethod(sb, prefab);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateSpawnMethod(StringBuilder sb, PrefabInfo prefabInfo)
    {
        var prefab = prefabInfo.Model;
        var methodName = SanitizeIdentifier(prefabInfo.Name);

        // Parse overridable fields into typed parameters
        var overrideParams = ParseOverrideParameters(prefab);

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Spawns a {prefabInfo.Name} prefab instance.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"world\">The world to spawn the prefab into.</param>");
        foreach (var param in overrideParams)
        {
            sb.AppendLine($"    /// <param name=\"{param.ParameterName}\">Optional override for {param.ComponentName}.{param.FieldName}.</param>");
        }
        sb.AppendLine($"    /// <returns>The root entity of the spawned prefab.</returns>");

        // Generate method signature with optional parameters
        sb.Append($"    public static global::KeenEyes.Entity Spawn{methodName}(");
        sb.AppendLine();
        sb.Append("        global::KeenEyes.World world");

        foreach (var param in overrideParams)
        {
            sb.AppendLine(",");
            sb.Append($"        {param.TypeName}? {param.ParameterName} = null");
        }
        sb.AppendLine(")");
        sb.AppendLine("    {");

        // Track entity variable names
        var variableNames = new Dictionary<string, string>();

        // Spawn root entity
        if (prefab.Root != null)
        {
            var rootVarName = GenerateVariableName(prefab.Root.Name, "root", variableNames);
            variableNames[prefab.Root.Id] = rootVarName;

            GenerateEntitySpawn(sb, prefab.Root, rootVarName, overrideParams, "        ");
            sb.AppendLine();
        }

        // Spawn top-level children
        foreach (var child in prefab.Children)
        {
            GeneratePrefabEntityRecursive(sb, child, prefab.Root != null ? variableNames[prefab.Root.Id] : null, variableNames, overrideParams, "        ");
        }

        // Return root entity
        var rootVar = prefab.Root != null ? variableNames[prefab.Root.Id] : "default";
        sb.AppendLine($"        return {rootVar};");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GeneratePrefabEntityRecursive(
        StringBuilder sb,
        PrefabEntityModel entity,
        string? parentVar,
        Dictionary<string, string> variableNames,
        List<OverrideParameter> overrideParams,
        string indent)
    {
        var varName = GenerateVariableName(entity.Name, entity.Id, variableNames);
        variableNames[entity.Id] = varName;

        GenerateEntitySpawn(sb, entity, varName, overrideParams, indent);

        // Set parent if specified
        if (parentVar != null)
        {
            sb.AppendLine($"{indent}world.SetParent({varName}, {parentVar});");
        }

        sb.AppendLine();

        // Process nested children
        foreach (var child in entity.Children)
        {
            GeneratePrefabEntityRecursive(sb, child, varName, variableNames, overrideParams, indent);
        }
    }

    private static void GenerateEntitySpawn(
        StringBuilder sb,
        PrefabEntityModel entity,
        string varName,
        List<OverrideParameter> overrideParams,
        string indent)
    {
        sb.AppendLine($"{indent}var {varName} = world.Spawn(\"{EscapeString(entity.Name)}\")");

        foreach (var kvp in entity.Components)
        {
            var componentName = kvp.Key;
            var componentData = kvp.Value;

            sb.Append($"{indent}    .With(new global::{componentName}");

            // Check if any fields in this component have overrides
            var componentOverrides = overrideParams
                .Where(p => p.ComponentName == componentName)
                .ToList();

            if (componentData.Count > 0 || componentOverrides.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent}    {{");
                GenerateComponentInitializerWithOverrides(sb, componentName, componentData, componentOverrides, indent + "        ");
                sb.Append($"{indent}    }}");
            }
            else
            {
                sb.Append("()");
            }

            sb.AppendLine(")");
        }

        sb.AppendLine($"{indent}    .Build();");
    }

    private static void GenerateComponentInitializerWithOverrides(
        StringBuilder sb,
        string componentName,
        Dictionary<string, object?> data,
        List<OverrideParameter> overrides,
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

            // Check if this field has an override
            var overrideParam = overrides.FirstOrDefault(o => o.FieldName == key);
            if (overrideParam != null)
            {
                sb.Append($"{indent}{key} = {overrideParam.ParameterName} ?? ");
                GenerateValue(sb, value, indent);
            }
            else
            {
                sb.Append($"{indent}{key} = ");
                GenerateValue(sb, value, indent);
            }
        }

        // Add any override fields that weren't in the original data
        foreach (var overrideParam in overrides)
        {
            if (!data.ContainsKey(overrideParam.FieldName))
            {
                if (!isFirst)
                {
                    sb.AppendLine(",");
                }
                isFirst = false;

                sb.Append($"{indent}{overrideParam.FieldName} = {overrideParam.ParameterName} ?? default");
            }
        }

        sb.AppendLine();
    }

    private static List<OverrideParameter> ParseOverrideParameters(PrefabModel prefab)
    {
        var result = new List<OverrideParameter>();

        foreach (var fieldPath in prefab.OverridableFields)
        {
            var parts = fieldPath.Split('.');
            if (parts.Length != 2)
            {
                continue;
            }

            var componentName = parts[0];
            var fieldName = parts[1];

            // Try to find the default value from the prefab data
            object? defaultValue = null;
            string typeName = "object";

            if (prefab.Root != null)
            {
                Dictionary<string, object?>? componentData;
                if (prefab.Root.Components.TryGetValue(componentName, out componentData))
                {
                    object? val;
                    if (componentData.TryGetValue(fieldName, out val))
                    {
                        defaultValue = val;
                        typeName = InferTypeName(val);
                    }
                }
            }

            // Generate parameter name (camelCase: Transform.Position -> transformPosition)
            var paramName = char.ToLowerInvariant(componentName[0]) +
                           componentName.Substring(1) +
                           fieldName;

            result.Add(new OverrideParameter(componentName, fieldName, typeName, paramName, defaultValue));
        }

        return result;
    }

    private static string InferTypeName(object? value)
    {
        return value switch
        {
            null => "object",
            bool => "bool",
            int => "int",
            long => "long",
            double => "float",
            string => "string",
            Dictionary<string, object?> nested when IsVector3(nested) => "global::System.Numerics.Vector3",
            Dictionary<string, object?> nested when IsVector2(nested) => "global::System.Numerics.Vector2",
            Dictionary<string, object?> nested when IsVector4(nested) => "global::System.Numerics.Vector4",
            _ => "object"
        };
    }

    private static bool IsVector2(Dictionary<string, object?> nested)
    {
        return nested.ContainsKey("X") && nested.ContainsKey("Y") && !nested.ContainsKey("Z");
    }

    private static bool IsVector3(Dictionary<string, object?> nested)
    {
        return nested.ContainsKey("X") && nested.ContainsKey("Y") && nested.ContainsKey("Z") && !nested.ContainsKey("W");
    }

    private static bool IsVector4(Dictionary<string, object?> nested)
    {
        return nested.ContainsKey("X") && nested.ContainsKey("Y") && nested.ContainsKey("Z") && nested.ContainsKey("W");
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
        if (nested.ContainsKey("X") && nested.ContainsKey("Y"))
        {
            if (nested.ContainsKey("Z"))
            {
                if (nested.ContainsKey("W"))
                {
                    sb.Append($"new global::System.Numerics.Vector4({FormatFloat(nested["X"])}, {FormatFloat(nested["Y"])}, {FormatFloat(nested["Z"])}, {FormatFloat(nested["W"])})");
                }
                else
                {
                    sb.Append($"new global::System.Numerics.Vector3({FormatFloat(nested["X"])}, {FormatFloat(nested["Y"])}, {FormatFloat(nested["Z"])})");
                }
            }
            else
            {
                sb.Append($"new global::System.Numerics.Vector2({FormatFloat(nested["X"])}, {FormatFloat(nested["Y"])})");
            }
        }
        else
        {
            sb.AppendLine("new()");
            sb.AppendLine($"{indent}{{");
            GenerateComponentInitializer(sb, nested, indent + "    ");
            sb.Append($"{indent}}}");
        }
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

    private static string GenerateVariableName(string name, string fallback, Dictionary<string, string> existingNames)
    {
        var baseName = SanitizeIdentifier(name);
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = fallback;
        }

        baseName = char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);

        var varName = baseName;
        var counter = 1;
        while (existingNames.Values.Contains(varName))
        {
            varName = $"{baseName}{counter++}";
        }

        return varName;
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

    private sealed class PrefabInfo
    {
        public string Name { get; }
        public PrefabModel Model { get; }
        public string FilePath { get; }

        public PrefabInfo(string name, PrefabModel model, string filePath)
        {
            Name = name;
            Model = model;
            FilePath = filePath;
        }
    }

    private sealed class OverrideParameter
    {
        public string ComponentName { get; }
        public string FieldName { get; }
        public string TypeName { get; }
        public string ParameterName { get; }
        public object? DefaultValue { get; }

        public OverrideParameter(string componentName, string fieldName, string typeName, string parameterName, object? defaultValue)
        {
            ComponentName = componentName;
            FieldName = fieldName;
            TypeName = typeName;
            ParameterName = parameterName;
            DefaultValue = defaultValue;
        }
    }
}

// Diagnostics for prefab generator (KEEN070-KEEN079 range)
internal static partial class Diagnostics
{
    /// <summary>
    /// KEEN070: Component type not found in prefab.
    /// </summary>
    public static readonly DiagnosticDescriptor PrefabComponentTypeNotFound = new(
        id: "KEEN070",
        title: "Component type not found",
        messageFormat: "Component type '{0}' not found",
        category: "KeenEyes.Prefab",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified component type could not be found in the compilation.");

    /// <summary>
    /// KEEN070: Component type not found in prefab (with suggestion).
    /// </summary>
    public static readonly DiagnosticDescriptor PrefabComponentTypeNotFoundWithSuggestion = new(
        id: "KEEN070",
        title: "Component type not found",
        messageFormat: "Component type '{0}' not found - did you mean '{1}'?",
        category: "KeenEyes.Prefab",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified component type could not be found. A similar type was suggested.");

    /// <summary>
    /// KEEN071: Invalid JSON in prefab file.
    /// </summary>
    public static readonly DiagnosticDescriptor PrefabInvalidJson = new(
        id: "KEEN071",
        title: "Invalid JSON",
        messageFormat: "Invalid JSON in {0}: {1}",
        category: "KeenEyes.Prefab",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The prefab file contains invalid JSON that could not be parsed.");

    /// <summary>
    /// KEEN072: Invalid override field path.
    /// </summary>
    public static readonly DiagnosticDescriptor PrefabInvalidOverrideField = new(
        id: "KEEN072",
        title: "Invalid override field",
        messageFormat: "Invalid override field path '{0}' - expected format 'ComponentType.FieldName'",
        category: "KeenEyes.Prefab",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Override field paths must be in the format 'ComponentType.FieldName'.");

    /// <summary>
    /// KEEN073: Override field not found.
    /// </summary>
    public static readonly DiagnosticDescriptor PrefabOverrideFieldNotFound = new(
        id: "KEEN073",
        title: "Override field not found",
        messageFormat: "Override field '{0}' references component '{1}' which is not present in the prefab",
        category: "KeenEyes.Prefab",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The override field references a component that does not exist in the prefab.");
}
