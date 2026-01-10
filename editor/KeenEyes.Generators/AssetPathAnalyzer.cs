using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that validates asset paths passed to AssetManager.Load() and LoadAsync() calls.
/// </summary>
/// <remarks>
/// <para>
/// This analyzer detects common asset loading errors at compile time:
/// </para>
/// <list type="bullet">
/// <item><description>KEEN120: Asset file does not exist in the project</description></item>
/// <item><description>KEEN121: Asset extension is not supported for the requested type</description></item>
/// <item><description>KEEN122: Possible typo - did you mean a similar asset path?</description></item>
/// <item><description>KEEN123: Consider using generated constant instead of string literal</description></item>
/// </list>
/// <para>
/// The analyzer requires assets to be included as AdditionalFiles in the project and
/// GenerateAssetConstants to be enabled for full functionality.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssetPathAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// KEEN120: Asset file not found in project.
    /// </summary>
    public static readonly DiagnosticDescriptor AssetNotFound = new(
        id: "KEEN120",
        title: "Asset file not found",
        messageFormat: "Asset file '{0}' not found in project",
        category: "KeenEyes.Assets",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The specified asset path does not match any file included as AdditionalFiles. " +
                     "Ensure the asset exists and is properly included in the project.");

    /// <summary>
    /// KEEN121: Asset extension not supported for the requested type.
    /// </summary>
    public static readonly DiagnosticDescriptor ExtensionMismatch = new(
        id: "KEEN121",
        title: "Asset extension not supported for type",
        messageFormat: "Extension '{0}' is not supported for {1}; expected one of: {2}",
        category: "KeenEyes.Assets",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The asset file extension does not match any extension supported by the requested asset type. " +
                     "Use the correct file format or load with the appropriate asset type.");

    /// <summary>
    /// KEEN122: Possible typo in asset path - similar asset exists.
    /// </summary>
    public static readonly DiagnosticDescriptor PossibleTypo = new(
        id: "KEEN122",
        title: "Possible typo in asset path",
        messageFormat: "Asset '{0}' not found; did you mean '{1}'?",
        category: "KeenEyes.Assets",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The specified asset path was not found, but a similar path exists in the project. " +
                     "This may indicate a typo in the asset path.");

    /// <summary>
    /// KEEN123: Consider using generated constant instead of string literal.
    /// </summary>
    public static readonly DiagnosticDescriptor UseGeneratedConstant = new(
        id: "KEEN123",
        title: "Use generated asset constant",
        messageFormat: "Consider using the generated constant instead of string literal",
        category: "KeenEyes.Assets",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "When GenerateAssetConstants is enabled, prefer using the generated constant " +
                     "for compile-time safety and IDE autocompletion.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(AssetNotFound, ExtensionMismatch, PossibleTypo, UseGeneratedConstant);

    // Extension to asset type mapping
    private static readonly Dictionary<string, string[]> TypeToExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TextureAsset"] = [".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd", ".hdr", ".webp", ".dds"],
        ["AudioClipAsset"] = [".wav", ".ogg", ".mp3", ".flac"],
        ["FontAsset"] = [".ttf", ".otf"],
        ["MeshAsset"] = [".gltf", ".glb"],
        ["ModelAsset"] = [".gltf", ".glb"],
        ["AnimationAsset"] = [".keanim"],
        ["SpriteAtlasAsset"] = [".atlas", ".json"],
        ["RawAsset"] = [".bin", ".dat", ".raw", ".bytes"]
    };

    private static readonly HashSet<string> KnownAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Textures
        ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd", ".hdr", ".webp", ".dds",
        // Audio
        ".wav", ".ogg", ".mp3", ".flac",
        // Fonts
        ".ttf", ".otf",
        // Data
        ".json", ".xml", ".yaml", ".yml",
        // Atlases and animations
        ".atlas", ".keanim",
        // Models
        ".glb", ".gltf",
        // Shaders (raw)
        ".glsl", ".vert", ".frag",
        // KeenEyes specific
        ".kescene", ".keprefab", ".keworld",
        // Raw data
        ".bin", ".dat", ".raw", ".bytes"
    };

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for method invocations
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Collect asset files from AdditionalFiles
            var assetFiles = compilationContext.Options.AdditionalFiles
                .Where(f => IsAssetFile(f.Path))
                .Select(f => NormalizePath(f.Path))
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

            // Check if asset constants generation is enabled
            compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                "build_property.GenerateAssetConstants", out var generateConstantsStr);
            var generateConstants = string.Equals(generateConstantsStr, "true", StringComparison.OrdinalIgnoreCase);

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeInvocation(ctx, assetFiles, generateConstants),
                OperationKind.Invocation);
        });
    }

    private static bool IsAssetFile(string path)
    {
        var extension = Path.GetExtension(path);
        return !string.IsNullOrEmpty(extension) && KnownAssetExtensions.Contains(extension);
    }

    private static string NormalizePath(string path)
    {
        // Normalize to forward slashes for consistent comparison
        var normalized = path.Replace('\\', '/');

        // Try to extract relative path from common asset folders
        var assetFolders = new[] { "/Assets/", "/assets/", "/Content/", "/content/", "/Resources/", "/resources/" };
        foreach (var folder in assetFolders)
        {
            var idx = normalized.IndexOf(folder, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                return normalized.Substring(idx + 1);
            }
        }

        // Return filename only if no common folder found
        return Path.GetFileName(normalized);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ImmutableHashSet<string> assetFiles,
        bool generateConstants)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Check if this is AssetManager.Load<T>() or LoadAsync<T>()
        var methodName = invocation.TargetMethod.Name;
        if (methodName != "Load" && methodName != "LoadAsync")
        {
            return;
        }

        // Check if the containing type is AssetManager or IAssetManager
        var containingType = invocation.TargetMethod.ContainingType;
        if (containingType == null)
        {
            return;
        }

        var typeName = containingType.Name;
        if (typeName != "AssetManager" && typeName != "IAssetManager")
        {
            return;
        }

        // Get the type argument (T in Load<T>)
        if (invocation.TargetMethod.TypeArguments.Length != 1)
        {
            return;
        }

        var assetType = invocation.TargetMethod.TypeArguments[0];
        var assetTypeName = assetType.Name;

        // Get the first argument (the path string)
        if (invocation.Arguments.Length < 1)
        {
            return;
        }

        var pathArg = invocation.Arguments[0];
        if (pathArg.Value is not ILiteralOperation literalOp ||
            literalOp.ConstantValue.Value is not string path)
        {
            return;
        }

        // Normalize the path for comparison
        var normalizedPath = path.Replace('\\', '/');

        // Check if file exists in known assets
        var fileExists = assetFiles.Any(f =>
            f.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith("/" + normalizedPath, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.EndsWith("/" + f, StringComparison.OrdinalIgnoreCase));

        if (!fileExists && assetFiles.Count > 0)
        {
            // Look for similar paths (possible typo)
            var similarPath = FindSimilarPath(normalizedPath, assetFiles);

            if (similarPath != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PossibleTypo,
                    pathArg.Syntax.GetLocation(),
                    normalizedPath,
                    similarPath));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AssetNotFound,
                    pathArg.Syntax.GetLocation(),
                    normalizedPath));
            }
        }

        // Check extension matches type
        var extension = Path.GetExtension(normalizedPath);
        if (!string.IsNullOrEmpty(extension) &&
            TypeToExtensions.TryGetValue(assetTypeName, out var validExtensions) &&
            !validExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ExtensionMismatch,
                pathArg.Syntax.GetLocation(),
                extension,
                assetTypeName,
                string.Join(", ", validExtensions)));
        }

        // Suggest using generated constant if enabled
        if (generateConstants && fileExists)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UseGeneratedConstant,
                pathArg.Syntax.GetLocation()));
        }
    }

    private static string? FindSimilarPath(string path, ImmutableHashSet<string> assetFiles)
    {
        // Get filename for comparison (without extension for typo matching)
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);

        string? bestMatch = null;
        var bestDistance = int.MaxValue;

        foreach (var assetFile in assetFiles)
        {
            var assetFileNameWithoutExt = Path.GetFileNameWithoutExtension(assetFile);

            // Check filename-only distance (most common typo case)
            var distance = LevenshteinDistance(
                fileNameWithoutExt.ToLowerInvariant(),
                assetFileNameWithoutExt.ToLowerInvariant());

            // Only suggest if distance is reasonable (≤ 3 for filenames)
            if (distance <= 3 && distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = assetFile;
            }
        }

        return bestMatch;
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
}
