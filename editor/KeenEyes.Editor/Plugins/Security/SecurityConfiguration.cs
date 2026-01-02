// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Configuration for plugin security features.
/// </summary>
public sealed class SecurityConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the default security configuration (permissive).
    /// </summary>
    public static SecurityConfiguration Default { get; } = new();

    /// <summary>
    /// Gets or sets whether to require plugins to be signed.
    /// </summary>
    [JsonPropertyName("requireSignature")]
    public bool RequireSignature { get; init; }

    /// <summary>
    /// Gets or sets whether to run static analysis on plugins.
    /// </summary>
    [JsonPropertyName("enableAnalysis")]
    public bool EnableAnalysis { get; init; } = true;

    /// <summary>
    /// Gets or sets the analysis configuration.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because <see cref="AnalysisConfiguration"/>
    /// contains complex types that don't round-trip well. Default configuration is used when loading.
    /// </remarks>
    [JsonIgnore]
    public AnalysisConfiguration AnalysisConfig { get; init; } = AnalysisConfiguration.Default;

    /// <summary>
    /// Gets or sets whether to allow unsigned plugins from trusted paths.
    /// </summary>
    [JsonPropertyName("allowUnsignedFromTrustedPaths")]
    public bool AllowUnsignedFromTrustedPaths { get; init; } = true;

    /// <summary>
    /// Gets or sets paths where unsigned plugins are allowed.
    /// </summary>
    [JsonPropertyName("trustedPaths")]
    public IReadOnlyList<string> TrustedPaths { get; init; } = [];

    /// <summary>
    /// Gets or sets whether to enable the permission system.
    /// </summary>
    [JsonPropertyName("enablePermissions")]
    public bool EnablePermissions { get; init; } = true;

    /// <summary>
    /// Gets or sets the default permissions granted to plugins without explicit declarations.
    /// </summary>
    [JsonPropertyName("defaultPermissions")]
    public IReadOnlyList<string> DefaultPermissions { get; init; } =
    [
        "FileSystemProject",
        "ClipboardAccess"
    ];

    /// <summary>
    /// Gets or sets whether to prompt users for permission consent.
    /// </summary>
    [JsonPropertyName("promptForConsent")]
    public bool PromptForConsent { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to remember permission decisions.
    /// </summary>
    [JsonPropertyName("rememberPermissionDecisions")]
    public bool RememberPermissionDecisions { get; init; } = true;

    /// <summary>
    /// Gets the default configuration file path.
    /// </summary>
    public static string GetDefaultConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".keeneyes", "security-config.json");
    }

    /// <summary>
    /// Loads configuration from the default location.
    /// </summary>
    public static SecurityConfiguration Load()
    {
        return Load(GetDefaultConfigPath());
    }

    /// <summary>
    /// Loads configuration from a file.
    /// </summary>
    public static SecurityConfiguration Load(string path)
    {
        if (!File.Exists(path))
        {
            return Default;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SecurityConfiguration>(json, JsonOptions) ?? Default;
        }
        catch (JsonException)
        {
            return Default;
        }
    }

    /// <summary>
    /// Saves configuration to the default location.
    /// </summary>
    public void Save()
    {
        Save(GetDefaultConfigPath());
    }

    /// <summary>
    /// Saves configuration to a file.
    /// </summary>
    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Checks if a path is in the trusted paths list.
    /// </summary>
    public bool IsPathTrusted(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        foreach (var trustedPath in TrustedPaths)
        {
            var normalizedTrusted = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(trustedPath));

            if (normalizedPath.StartsWith(normalizedTrusted, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a strict security configuration.
    /// </summary>
    public static SecurityConfiguration Strict { get; } = new()
    {
        RequireSignature = true,
        EnableAnalysis = true,
        AnalysisConfig = AnalysisConfiguration.Strict,
        AllowUnsignedFromTrustedPaths = false,
        TrustedPaths = [],
        EnablePermissions = true,
        DefaultPermissions = [],
        PromptForConsent = true,
        RememberPermissionDecisions = false
    };

    /// <summary>
    /// Creates a permissive security configuration for development.
    /// </summary>
    public static SecurityConfiguration Development { get; } = new()
    {
        RequireSignature = false,
        EnableAnalysis = true,
        AnalysisConfig = AnalysisConfiguration.Permissive,
        AllowUnsignedFromTrustedPaths = true,
        TrustedPaths =
        [
            "%USERPROFILE%/.nuget/packages",
            "~/.nuget/packages"
        ],
        EnablePermissions = false,
        DefaultPermissions = [],
        PromptForConsent = false,
        RememberPermissionDecisions = true
    };
}
