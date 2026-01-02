// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Performs static analysis on plugin assemblies before loading.
/// Uses System.Reflection.Metadata for AOT-compatible IL analysis.
/// </summary>
internal sealed class AssemblyAnalyzer
{
    private readonly IEditorPluginLogger? logger;
    private readonly AnalysisConfiguration configuration;

    // Known dangerous type references to detect
    private static readonly HashSet<string> ReflectionTypes =
    [
        "System.Type",
        "System.Reflection.MethodInfo",
        "System.Reflection.FieldInfo",
        "System.Reflection.PropertyInfo",
        "System.Reflection.Assembly",
        "System.Activator"
    ];

    private static readonly HashSet<string> ReflectionMethods =
    [
        "GetMethod",
        "GetMethods",
        "GetField",
        "GetFields",
        "GetProperty",
        "GetProperties",
        "GetType",
        "GetTypes",
        "CreateInstance",
        "InvokeMember",
        "Invoke"
    ];

    private static readonly HashSet<string> FileSystemTypes =
    [
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileStream",
        "System.IO.StreamReader",
        "System.IO.StreamWriter",
        "System.IO.Path",
        "System.IO.FileInfo",
        "System.IO.DirectoryInfo"
    ];

    private static readonly HashSet<string> NetworkTypes =
    [
        "System.Net.Http.HttpClient",
        "System.Net.WebClient",
        "System.Net.Sockets.Socket",
        "System.Net.Sockets.TcpClient",
        "System.Net.Sockets.TcpListener",
        "System.Net.Sockets.UdpClient"
    ];

    private static readonly HashSet<string> ProcessTypes =
    [
        "System.Diagnostics.Process"
    ];

    private static readonly HashSet<string> EnvironmentTypes =
    [
        "System.Environment"
    ];

    private static readonly HashSet<string> AssemblyLoadTypes =
    [
        "System.Reflection.Assembly",
        "System.Runtime.Loader.AssemblyLoadContext"
    ];

    private static readonly HashSet<string> AssemblyLoadMethods =
    [
        "Load",
        "LoadFrom",
        "LoadFile",
        "LoadFromAssemblyPath",
        "LoadFromAssemblyName"
    ];

    /// <summary>
    /// Creates a new assembly analyzer.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <param name="configuration">Analysis configuration. Uses defaults if null.</param>
    public AssemblyAnalyzer(
        IEditorPluginLogger? logger = null,
        AnalysisConfiguration? configuration = null)
    {
        this.logger = logger;
        this.configuration = configuration ?? AnalysisConfiguration.Default;
    }

    /// <summary>
    /// Analyzes an assembly and returns security findings.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>Analysis result containing any security findings.</returns>
    public AnalysisResult Analyze(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return AnalysisResult.Error(assemblyPath, $"File not found: {assemblyPath}");
        }

        try
        {
            var hash = ComputeHash(assemblyPath);
            var findings = new List<SecurityFinding>();

            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                return AnalysisResult.Error(assemblyPath, "Assembly has no metadata");
            }

            var metadataReader = peReader.GetMetadataReader();

            // Get assembly info
            string? assemblyName = null;
            string? assemblyVersion = null;

            if (metadataReader.IsAssembly)
            {
                var assemblyDef = metadataReader.GetAssemblyDefinition();
                assemblyName = metadataReader.GetString(assemblyDef.Name);
                assemblyVersion = assemblyDef.Version.ToString();
            }

            // Check for P/Invoke methods
            if (configuration.IsPatternEnabled(DetectionPattern.PInvoke))
            {
                findings.AddRange(DetectPInvoke(metadataReader));
            }

            // Check member references for dangerous APIs
            findings.AddRange(DetectDangerousReferences(metadataReader));

            // Determine if passed based on configuration
            var passed = DeterminePassStatus(findings);

            logger?.LogInfo($"Analyzed {assemblyPath}: {findings.Count} findings, passed={passed}");

            return new AnalysisResult
            {
                AssemblyPath = assemblyPath,
                Passed = passed,
                Findings = findings,
                AnalysisTimestamp = DateTime.UtcNow,
                AssemblyHash = hash,
                AssemblyName = assemblyName,
                AssemblyVersion = assemblyVersion
            };
        }
        catch (Exception ex)
        {
            logger?.LogError($"Analysis failed for {assemblyPath}: {ex.Message}");
            return AnalysisResult.Error(assemblyPath, ex.Message);
        }
    }

    /// <summary>
    /// Detects patterns in an assembly and returns findings.
    /// </summary>
    public IReadOnlyList<SecurityFinding> DetectPatterns(string assemblyPath)
    {
        var result = Analyze(assemblyPath);
        return result.Findings;
    }

    private IEnumerable<SecurityFinding> DetectPInvoke(MetadataReader reader)
    {
        foreach (var methodHandle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var import = method.GetImport();

            if (import.Module.IsNil)
            {
                continue;
            }

            var moduleRef = reader.GetModuleReference(import.Module);
            var dllName = reader.GetString(moduleRef.Name);
            var entryPoint = reader.GetString(import.Name);
            var declaringType = GetDeclaringTypeName(reader, method);
            var methodName = reader.GetString(method.Name);

            yield return SecurityFinding.PInvoke(
                $"{declaringType}.{methodName}",
                dllName,
                entryPoint);
        }
    }

    private IEnumerable<SecurityFinding> DetectDangerousReferences(MetadataReader reader)
    {
        foreach (var memberRefHandle in reader.MemberReferences)
        {
            var memberRef = reader.GetMemberReference(memberRefHandle);
            var memberName = reader.GetString(memberRef.Name);

            // Get the declaring type
            string? typeName = null;
            if (memberRef.Parent.Kind == HandleKind.TypeReference)
            {
                var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                var typeNamespace = reader.GetString(typeRef.Namespace);
                var simpleTypeName = reader.GetString(typeRef.Name);
                typeName = string.IsNullOrEmpty(typeNamespace)
                    ? simpleTypeName
                    : $"{typeNamespace}.{simpleTypeName}";
            }

            if (typeName == null)
            {
                continue;
            }

            var fullRef = $"{typeName}.{memberName}";

            // Check for reflection
            if (configuration.IsPatternEnabled(DetectionPattern.Reflection))
            {
                if (ReflectionTypes.Contains(typeName) && ReflectionMethods.Contains(memberName))
                {
                    yield return SecurityFinding.Reflection("MemberReference", fullRef);
                }
            }

            // Check for file system access
            if (configuration.IsPatternEnabled(DetectionPattern.FileSystemAccess))
            {
                if (FileSystemTypes.Contains(typeName))
                {
                    yield return SecurityFinding.FileSystem("MemberReference", fullRef);
                }
            }

            // Check for network access
            if (configuration.IsPatternEnabled(DetectionPattern.NetworkAccess))
            {
                if (NetworkTypes.Contains(typeName))
                {
                    yield return SecurityFinding.Network("MemberReference", fullRef);
                }
            }

            // Check for process execution
            if (configuration.IsPatternEnabled(DetectionPattern.ProcessExecution))
            {
                if (ProcessTypes.Contains(typeName) && memberName == "Start")
                {
                    yield return SecurityFinding.ProcessExec("MemberReference", fullRef);
                }
            }

            // Check for environment access
            if (configuration.IsPatternEnabled(DetectionPattern.EnvironmentAccess))
            {
                if (EnvironmentTypes.Contains(typeName))
                {
                    yield return SecurityFinding.Environment("MemberReference", fullRef);
                }
            }

            // Check for assembly loading
            if (configuration.IsPatternEnabled(DetectionPattern.AssemblyLoading))
            {
                if (AssemblyLoadTypes.Contains(typeName) && AssemblyLoadMethods.Contains(memberName))
                {
                    yield return SecurityFinding.AssemblyLoad("MemberReference", fullRef);
                }
            }
        }
    }

    private static string GetDeclaringTypeName(MetadataReader reader, MethodDefinition method)
    {
        var typeHandle = method.GetDeclaringType();
        var typeDef = reader.GetTypeDefinition(typeHandle);
        var typeNamespace = reader.GetString(typeDef.Namespace);
        var typeName = reader.GetString(typeDef.Name);

        return string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";
    }

    private bool DeterminePassStatus(List<SecurityFinding> findings)
    {
        if (findings.Count == 0)
        {
            return true;
        }

        return configuration.Mode switch
        {
            AnalysisMode.WarnOnly => true,
            AnalysisMode.Block => !findings.Any(f => f.Severity >= configuration.BlockingSeverity),
            AnalysisMode.PromptUser => true, // Pass for now, prompt happens later
            _ => true
        };
    }

    private static string ComputeHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
