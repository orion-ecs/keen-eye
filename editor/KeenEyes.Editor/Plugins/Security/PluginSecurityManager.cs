// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Coordinates security checks for plugin loading.
/// </summary>
internal sealed class PluginSecurityManager
{
    private readonly AssemblyAnalyzer analyzer;
    private readonly PluginSignatureVerifier signatureVerifier;
    private readonly TrustedPublisherStore trustedStore;
    private readonly SecurityConfiguration configuration;
    private readonly IEditorPluginLogger? logger;

    // Cache analysis results by assembly hash
    private readonly Dictionary<string, AnalysisResult> analysisCache = [];

    /// <summary>
    /// Creates a new plugin security manager.
    /// </summary>
    public PluginSecurityManager(
        SecurityConfiguration? configuration = null,
        IEditorPluginLogger? logger = null)
    {
        this.configuration = configuration ?? SecurityConfiguration.Default;
        this.logger = logger;

        trustedStore = new TrustedPublisherStore();
        trustedStore.Load();

        signatureVerifier = new PluginSignatureVerifier(trustedStore, logger);
        analyzer = new AssemblyAnalyzer(logger, this.configuration.AnalysisConfig);
    }

    /// <summary>
    /// Creates a new plugin security manager with custom components.
    /// </summary>
    internal PluginSecurityManager(
        AssemblyAnalyzer analyzer,
        PluginSignatureVerifier signatureVerifier,
        TrustedPublisherStore trustedStore,
        SecurityConfiguration configuration,
        IEditorPluginLogger? logger = null)
    {
        this.analyzer = analyzer;
        this.signatureVerifier = signatureVerifier;
        this.trustedStore = trustedStore;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Raised when user consent is required for a security decision.
    /// </summary>
    public event Func<SecurityPromptInfo, Task<SecurityDecision>>? OnSecurityPrompt;

    /// <summary>
    /// Performs all security checks before a plugin can be loaded.
    /// </summary>
    public async Task<SecurityCheckResult> CheckPluginAsync(LoadedPlugin plugin)
    {
        var assemblyPath = plugin.GetAssemblyPath();
        var blockingReasons = new List<string>();
        var warnings = new List<string>();

        SignatureVerificationResult? signatureResult = null;
        AnalysisResult? analysisResult = null;

        // 1. Signature verification
        if (configuration.RequireSignature || plugin.Manifest.Security?.PublicKeyToken != null)
        {
            signatureResult = signatureVerifier.Verify(assemblyPath, plugin.Manifest);

            if (configuration.RequireSignature && !signatureResult.IsSigned)
            {
                // Check if path is trusted for unsigned plugins
                if (!configuration.AllowUnsignedFromTrustedPaths ||
                    !configuration.IsPathTrusted(plugin.BasePath))
                {
                    blockingReasons.Add("Plugin is not signed and signature is required");
                }
                else
                {
                    warnings.Add("Plugin is not signed but loaded from trusted path");
                }
            }
            else if (signatureResult.IsSigned && !signatureResult.IsValid)
            {
                blockingReasons.Add($"Invalid signature: {signatureResult.ErrorMessage}");
            }
            else if (signatureResult.IsSigned && !signatureResult.IsTrusted)
            {
                warnings.Add($"Plugin signed by untrusted publisher: {signatureResult.PublicKeyToken}");

                // Prompt user for untrusted publishers?
                if (OnSecurityPrompt != null)
                {
                    var decision = await PromptForUntrustedPublisher(plugin, signatureResult);
                    if (decision == SecurityDecision.Block)
                    {
                        blockingReasons.Add("User rejected untrusted publisher");
                    }
                    else if (decision == SecurityDecision.TrustAlways)
                    {
                        await TrustPublisher(signatureResult);
                    }
                }
            }
        }

        // 2. Static analysis
        if (configuration.EnableAnalysis)
        {
            analysisResult = GetOrAnalyze(assemblyPath);

            if (!analysisResult.AnalysisCompleted)
            {
                warnings.Add($"Analysis failed: {analysisResult.ErrorMessage}");
            }
            else if (analysisResult.Findings.Count > 0)
            {
                foreach (var finding in analysisResult.Findings)
                {
                    var message = $"[{finding.Severity}] {finding.Pattern}: {finding.Description}";

                    if (finding.Severity >= configuration.AnalysisConfig.BlockingSeverity &&
                        configuration.AnalysisConfig.Mode == AnalysisMode.Block)
                    {
                        blockingReasons.Add(message);
                    }
                    else
                    {
                        warnings.Add(message);
                    }
                }

                // Prompt user for suspicious patterns?
                if (configuration.AnalysisConfig.Mode == AnalysisMode.PromptUser &&
                    analysisResult.HasFindingsAtOrAbove(SecuritySeverity.Medium) &&
                    OnSecurityPrompt != null)
                {
                    var decision = await PromptForAnalysisFindings(plugin, analysisResult);
                    if (decision == SecurityDecision.Block)
                    {
                        blockingReasons.Add("User rejected plugin due to security findings");
                    }
                }
            }
        }

        var canLoad = blockingReasons.Count == 0;

        if (!canLoad)
        {
            logger?.LogWarning($"Plugin '{plugin.Manifest.Id}' blocked: {string.Join(", ", blockingReasons)}");
        }
        else if (warnings.Count > 0)
        {
            foreach (var warning in warnings)
            {
                logger?.LogWarning($"Plugin '{plugin.Manifest.Id}': {warning}");
            }
        }

        return new SecurityCheckResult
        {
            CanLoad = canLoad,
            AnalysisResult = analysisResult,
            SignatureResult = signatureResult,
            BlockingReasons = blockingReasons,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Synchronous version of CheckPluginAsync for simpler use cases.
    /// </summary>
    public SecurityCheckResult CheckPlugin(LoadedPlugin plugin)
    {
        return CheckPluginAsync(plugin).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Clears the analysis cache.
    /// </summary>
    public void ClearCache()
    {
        analysisCache.Clear();
    }

    private AnalysisResult GetOrAnalyze(string assemblyPath)
    {
        var result = analyzer.Analyze(assemblyPath);

        // Cache by hash
        if (result.AnalysisCompleted && !string.IsNullOrEmpty(result.AssemblyHash))
        {
            analysisCache[result.AssemblyHash] = result;
        }

        return result;
    }

    private async Task<SecurityDecision> PromptForUntrustedPublisher(
        LoadedPlugin plugin,
        SignatureVerificationResult signatureResult)
    {
        if (OnSecurityPrompt == null)
        {
            return SecurityDecision.Allow;
        }

        var prompt = new SecurityPromptInfo
        {
            PluginId = plugin.Manifest.Id,
            PluginName = plugin.Manifest.Name,
            PromptType = SecurityPromptType.UntrustedPublisher,
            Message = $"Plugin '{plugin.Manifest.Name}' is signed by an untrusted publisher.",
            Details = $"Publisher: {signatureResult.SignerName ?? "Unknown"}\n" +
                      $"Public Key Token: {signatureResult.PublicKeyToken}",
            Options = [SecurityDecision.Allow, SecurityDecision.TrustAlways, SecurityDecision.Block]
        };

        return await OnSecurityPrompt(prompt);
    }

    private async Task<SecurityDecision> PromptForAnalysisFindings(
        LoadedPlugin plugin,
        AnalysisResult analysisResult)
    {
        if (OnSecurityPrompt == null)
        {
            return SecurityDecision.Allow;
        }

        var summary = analysisResult.GetFindingSummary();
        var details = string.Join("\n", summary.Select(kv => $"- {kv.Key}: {kv.Value} finding(s)"));

        var prompt = new SecurityPromptInfo
        {
            PluginId = plugin.Manifest.Id,
            PluginName = plugin.Manifest.Name,
            PromptType = SecurityPromptType.SuspiciousCode,
            Message = $"Plugin '{plugin.Manifest.Name}' contains potentially dangerous code patterns.",
            Details = details,
            Options = [SecurityDecision.Allow, SecurityDecision.Block]
        };

        return await OnSecurityPrompt(prompt);
    }

    private Task TrustPublisher(SignatureVerificationResult signatureResult)
    {
        if (string.IsNullOrEmpty(signatureResult.PublicKeyToken))
        {
            return Task.CompletedTask;
        }

        var publisher = new TrustedPublisher
        {
            Name = signatureResult.SignerName ?? "Unknown",
            PublicKeyToken = signatureResult.PublicKeyToken,
            TrustedSince = DateTime.UtcNow,
            PublicKey = signatureResult.PublicKey
        };

        trustedStore.AddTrusted(publisher);
        trustedStore.Save();

        logger?.LogInfo($"Added trusted publisher: {publisher.Name} ({publisher.PublicKeyToken})");

        return Task.CompletedTask;
    }
}

/// <summary>
/// Result of security checks for a plugin.
/// </summary>
public sealed class SecurityCheckResult
{
    /// <summary>
    /// Gets a value indicating whether the plugin can be loaded.
    /// </summary>
    public required bool CanLoad { get; init; }

    /// <summary>
    /// Gets the analysis result if analysis was performed.
    /// </summary>
    public AnalysisResult? AnalysisResult { get; init; }

    /// <summary>
    /// Gets the signature verification result if verification was performed.
    /// </summary>
    public SignatureVerificationResult? SignatureResult { get; init; }

    /// <summary>
    /// Gets the reasons why loading was blocked.
    /// </summary>
    public required IReadOnlyList<string> BlockingReasons { get; init; }

    /// <summary>
    /// Gets warning messages that don't block loading.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Creates a result indicating the plugin passed all checks.
    /// </summary>
    public static SecurityCheckResult Passed() => new()
    {
        CanLoad = true,
        BlockingReasons = [],
        Warnings = []
    };

    /// <summary>
    /// Creates a result indicating the plugin was blocked.
    /// </summary>
    public static SecurityCheckResult Blocked(params string[] reasons) => new()
    {
        CanLoad = false,
        BlockingReasons = reasons,
        Warnings = []
    };
}

/// <summary>
/// Information for a security prompt.
/// </summary>
public sealed class SecurityPromptInfo
{
    /// <summary>
    /// Gets the plugin ID.
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// Gets the plugin name.
    /// </summary>
    public required string PluginName { get; init; }

    /// <summary>
    /// Gets the type of prompt.
    /// </summary>
    public required SecurityPromptType PromptType { get; init; }

    /// <summary>
    /// Gets the main message to display.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets additional details.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Gets the available options.
    /// </summary>
    public required IReadOnlyList<SecurityDecision> Options { get; init; }
}

/// <summary>
/// Type of security prompt.
/// </summary>
public enum SecurityPromptType
{
    /// <summary>
    /// Plugin is signed by an untrusted publisher.
    /// </summary>
    UntrustedPublisher,

    /// <summary>
    /// Plugin contains suspicious code patterns.
    /// </summary>
    SuspiciousCode,

    /// <summary>
    /// Plugin requests elevated permissions.
    /// </summary>
    PermissionRequest
}

/// <summary>
/// User's decision for a security prompt.
/// </summary>
public enum SecurityDecision
{
    /// <summary>
    /// Allow this time only.
    /// </summary>
    Allow,

    /// <summary>
    /// Block the plugin.
    /// </summary>
    Block,

    /// <summary>
    /// Trust this publisher/permission always.
    /// </summary>
    TrustAlways
}
