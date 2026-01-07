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
/// Generates world configuration extension methods from .keworld files.
/// </summary>
[Generator]
public sealed class WorldConfigGenerator : IIncrementalGenerator
{
    private const string WorldExtension = ".keworld";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .keworld files marked as AdditionalFiles
        var worldFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(WorldExtension, StringComparison.OrdinalIgnoreCase));

        // Combine with compilation to validate types
        var compilationAndFiles = context.CompilationProvider
            .Combine(worldFiles.Collect());

        // Generate C# for each .keworld file
        context.RegisterSourceOutput(compilationAndFiles, static (ctx, source) =>
        {
            var (compilation, files) = source;
            var rootNamespace = GetRootNamespace(compilation);
            var configInfos = new List<WorldConfigInfo>();

            foreach (var file in files)
            {
                var configInfo = ProcessWorldConfigFile(ctx, file, compilation);
                if (configInfo != null)
                {
                    configInfos.Add(configInfo);
                }
            }

            // Generate the combined WorldConfigs class
            if (configInfos.Count > 0)
            {
                var combinedSource = GenerateWorldConfigsClass(configInfos, rootNamespace);
                ctx.AddSource("WorldConfigs.g.cs", SourceText.From(combinedSource, Encoding.UTF8));
            }
        });
    }

    private static string GetRootNamespace(Compilation compilation)
    {
        return compilation.AssemblyName ?? "Generated";
    }

    private static WorldConfigInfo? ProcessWorldConfigFile(
        SourceProductionContext context,
        AdditionalText file,
        Compilation compilation)
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
        var config = JsonParser.ParseWorldConfig(json);
        if (config is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WorldConfigInvalidJson,
                Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                filePath, "Failed to parse JSON"));
            return null;
        }

        // Use file name if config name is empty
        var configName = string.IsNullOrWhiteSpace(config.Name) ? fileName : config.Name;

        // Validate singleton component types
        foreach (var singletonName in config.Singletons.Keys)
        {
            var componentType = FindComponentType(compilation, singletonName);
            if (componentType is null)
            {
                var suggestion = FindSimilarType(compilation, singletonName);
                if (suggestion != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.WorldConfigComponentTypeNotFoundWithSuggestion,
                        Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                        singletonName, suggestion));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.WorldConfigComponentTypeNotFound,
                        Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                        singletonName));
                }
            }
        }

        // Validate plugin types
        foreach (var pluginName in config.Plugins)
        {
            var pluginType = FindClassType(compilation, pluginName);
            if (pluginType is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.WorldConfigPluginTypeNotFound,
                    Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                    pluginName));
            }
        }

        // Validate system types
        foreach (var system in config.Systems)
        {
            var systemType = FindClassType(compilation, system.Type);
            if (systemType is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.WorldConfigSystemTypeNotFound,
                    Location.Create(filePath, TextSpan.FromBounds(0, 0), default),
                    system.Type));
            }
        }

        return new WorldConfigInfo(configName, config, filePath);
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

    private static INamedTypeSymbol? FindClassType(Compilation compilation, string typeName)
    {
        // Look for classes (plugins, systems, etc.)
        var candidates = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(t => t.TypeKind == TypeKind.Class)
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
            .FirstOrDefault(t => t.ToLowerInvariant().Contains(loweredTypeName) ||
                       loweredTypeName.Contains(t.ToLowerInvariant()) ||
                       LevenshteinDistance(t.ToLowerInvariant(), loweredTypeName) <= 2);
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

    private static string GenerateWorldConfigsClass(List<WorldConfigInfo> configs, string rootNamespace)
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
        sb.AppendLine("/// Provides extension methods to configure worlds from .keworld files.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class WorldConfigs");
        sb.AppendLine("{");

        // Generate All property
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// All world configuration names available in this project.");
        sb.AppendLine("    /// </summary>");
        sb.Append("    public static IReadOnlyList<string> All { get; } = new string[] { ");
        sb.Append(string.Join(", ", configs.Select(c => $"\"{c.Name}\"")));
        sb.AppendLine(" };");
        sb.AppendLine();

        // Generate configure method for each world config
        foreach (var config in configs)
        {
            GenerateConfigureMethod(sb, config);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateConfigureMethod(StringBuilder sb, WorldConfigInfo configInfo)
    {
        var config = configInfo.Model;
        var methodName = SanitizeIdentifier(configInfo.Name);

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Configures a world with {configInfo.Name} settings.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"builder\">The world builder to configure.</param>");
        sb.AppendLine($"    /// <returns>The configured world builder for chaining.</returns>");
        sb.AppendLine($"    public static global::KeenEyes.WorldBuilder Configure{methodName}(this global::KeenEyes.WorldBuilder builder)");
        sb.AppendLine("    {");
        sb.AppendLine("        return builder");

        // Add settings
        if (config.Settings != null)
        {
            sb.AppendLine($"            .WithFixedTimeStep({config.Settings.FixedTimeStep}f)");
            sb.AppendLine($"            .WithMaxDeltaTime({config.Settings.MaxDeltaTime}f)");
        }

        // Add singletons
        foreach (var kvp in config.Singletons)
        {
            var singletonName = kvp.Key;
            var singletonData = kvp.Value;

            sb.Append($"            .WithSingleton(new global::{singletonName}");
            if (singletonData.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("            {");
                GenerateComponentInitializer(sb, singletonData, "                ");
                sb.Append("            }");
            }
            else
            {
                sb.Append("()");
            }
            sb.AppendLine(")");
        }

        // Add plugins
        foreach (var pluginName in config.Plugins)
        {
            sb.AppendLine($"            .WithPlugin<global::{pluginName}>()");
        }

        // Add systems
        foreach (var system in config.Systems)
        {
            var phase = MapPhaseToEnum(system.Phase);
            sb.AppendLine($"            .WithSystem<global::{system.Type}>(global::KeenEyes.SystemPhase.{phase}, order: {system.Order})");
        }

        // End method chain
        sb.AppendLine("            ;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Also generate a direct Apply method for existing worlds
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Applies {configInfo.Name} settings to an existing world.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    /// <param name=\"world\">The world to configure.</param>");
        sb.AppendLine($"    public static void Apply{methodName}(global::KeenEyes.World world)");
        sb.AppendLine("    {");

        // Add singletons to world
        foreach (var kvp in config.Singletons)
        {
            var singletonName = kvp.Key;
            var singletonData = kvp.Value;

            sb.Append($"        world.SetSingleton(new global::{singletonName}");
            if (singletonData.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("        {");
                GenerateComponentInitializer(sb, singletonData, "            ");
                sb.Append("        }");
            }
            else
            {
                sb.Append("()");
            }
            sb.AppendLine(");");
        }

        // Install plugins
        foreach (var pluginName in config.Plugins)
        {
            sb.AppendLine($"        world.InstallPlugin<global::{pluginName}>();");
        }

        // Register systems
        foreach (var system in config.Systems)
        {
            var phase = MapPhaseToEnum(system.Phase);
            sb.AppendLine($"        world.RegisterSystem<global::{system.Type}>(global::KeenEyes.SystemPhase.{phase}, order: {system.Order});");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string MapPhaseToEnum(string phase)
    {
        // Map common phase names to SystemPhase enum values
        return phase switch
        {
            "Update" => "Update",
            "FixedUpdate" => "FixedUpdate",
            "LateUpdate" => "LateUpdate",
            "PreUpdate" => "PreUpdate",
            "PostUpdate" => "PostUpdate",
            "Render" => "Render",
            _ => "Update"
        };
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

    private sealed class WorldConfigInfo
    {
        public string Name { get; }
        public WorldConfigModel Model { get; }
        public string FilePath { get; }

        public WorldConfigInfo(string name, WorldConfigModel model, string filePath)
        {
            Name = name;
            Model = model;
            FilePath = filePath;
        }
    }
}

// Diagnostics for world config generator (KEEN080-KEEN089 range)
internal static partial class Diagnostics
{
    /// <summary>
    /// KEEN080: Component type not found in world config.
    /// </summary>
    public static readonly DiagnosticDescriptor WorldConfigComponentTypeNotFound = new(
        id: "KEEN080",
        title: "Component type not found",
        messageFormat: "Singleton component type '{0}' not found",
        category: "KeenEyes.WorldConfig",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified singleton component type could not be found in the compilation.");

    /// <summary>
    /// KEEN080: Component type not found in world config (with suggestion).
    /// </summary>
    public static readonly DiagnosticDescriptor WorldConfigComponentTypeNotFoundWithSuggestion = new(
        id: "KEEN080",
        title: "Component type not found",
        messageFormat: "Singleton component type '{0}' not found - did you mean '{1}'?",
        category: "KeenEyes.WorldConfig",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified singleton component type could not be found. A similar type was suggested.");

    /// <summary>
    /// KEEN081: Invalid JSON in world config file.
    /// </summary>
    public static readonly DiagnosticDescriptor WorldConfigInvalidJson = new(
        id: "KEEN081",
        title: "Invalid JSON",
        messageFormat: "Invalid JSON in {0}: {1}",
        category: "KeenEyes.WorldConfig",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The world config file contains invalid JSON that could not be parsed.");

    /// <summary>
    /// KEEN082: Plugin type not found.
    /// </summary>
    public static readonly DiagnosticDescriptor WorldConfigPluginTypeNotFound = new(
        id: "KEEN082",
        title: "Plugin type not found",
        messageFormat: "Plugin type '{0}' not found",
        category: "KeenEyes.WorldConfig",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified plugin type could not be found in the compilation.");

    /// <summary>
    /// KEEN083: System type not found.
    /// </summary>
    public static readonly DiagnosticDescriptor WorldConfigSystemTypeNotFound = new(
        id: "KEEN083",
        title: "System type not found",
        messageFormat: "System type '{0}' not found",
        category: "KeenEyes.WorldConfig",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The specified system type could not be found in the compilation.");
}
