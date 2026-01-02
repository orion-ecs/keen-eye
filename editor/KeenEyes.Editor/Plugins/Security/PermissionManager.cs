// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Manages plugin permissions and grants.
/// </summary>
internal sealed class PermissionManager
{
    private readonly string storePath;
    private readonly Dictionary<string, PluginPermission> grants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginPermission> declaredRequired = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginPermission> declaredOptional = new(StringComparer.OrdinalIgnoreCase);
    private readonly IEditorPluginLogger? logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Event raised when a permission is requested.
    /// </summary>
    public event Func<PermissionRequest, Task<PermissionResponse>>? OnPermissionRequest;

    /// <summary>
    /// Creates a new permission manager.
    /// </summary>
    /// <param name="customPath">Optional custom path for the grants file.</param>
    /// <param name="logger">Optional logger.</param>
    public PermissionManager(string? customPath = null, IEditorPluginLogger? logger = null)
    {
        storePath = customPath ?? GetDefaultStorePath();
        this.logger = logger;
    }

    /// <summary>
    /// Gets the default store path.
    /// </summary>
    public static string GetDefaultStorePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".keeneyes", "plugin-permissions.json");
    }

    /// <summary>
    /// Loads persisted permission grants.
    /// </summary>
    public void Load()
    {
        grants.Clear();

        if (!File.Exists(storePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(storePath);
            var data = JsonSerializer.Deserialize<PermissionStore>(json, JsonOptions);

            if (data?.Grants != null)
            {
                foreach (var grant in data.Grants)
                {
                    if (TryParsePermissions(grant.Permissions, out var permission))
                    {
                        grants[grant.PluginId] = permission;
                    }
                }
            }

            logger?.LogInfo($"Loaded {grants.Count} permission grants");
        }
        catch (JsonException ex)
        {
            logger?.LogWarning($"Failed to load permission grants: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves permission grants to disk.
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(storePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new PermissionStore
        {
            Version = 1,
            Grants = grants.Select(kv => new PermissionGrant
            {
                PluginId = kv.Key,
                Permissions = FormatPermissions(kv.Value),
                GrantedAt = DateTime.UtcNow
            }).ToList()
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(storePath, json);

        logger?.LogInfo($"Saved {grants.Count} permission grants");
    }

    /// <summary>
    /// Registers a plugin's declared permissions from its manifest.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="manifest">The plugin manifest.</param>
    public void RegisterPlugin(string pluginId, PluginManifest manifest)
    {
        var required = ParsePermissionList(manifest.Permissions?.Required ?? []);
        var optional = ParsePermissionList(manifest.Permissions?.Optional ?? []);

        declaredRequired[pluginId] = required;
        declaredOptional[pluginId] = optional;

        logger?.LogInfo($"Registered plugin '{pluginId}' with required={required}, optional={optional}");
    }

    /// <summary>
    /// Unregisters a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    public void UnregisterPlugin(string pluginId)
    {
        declaredRequired.Remove(pluginId);
        declaredOptional.Remove(pluginId);
    }

    /// <summary>
    /// Gets the permissions granted to a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The granted permissions.</returns>
    public PluginPermission GetGrantedPermissions(string pluginId)
    {
        return grants.GetValueOrDefault(pluginId, PluginPermission.None);
    }

    /// <summary>
    /// Gets the required permissions declared by a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The required permissions.</returns>
    public PluginPermission GetRequiredPermissions(string pluginId)
    {
        return declaredRequired.GetValueOrDefault(pluginId, PluginPermission.None);
    }

    /// <summary>
    /// Gets the optional permissions declared by a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The optional permissions.</returns>
    public PluginPermission GetOptionalPermissions(string pluginId)
    {
        return declaredOptional.GetValueOrDefault(pluginId, PluginPermission.None);
    }

    /// <summary>
    /// Checks if a plugin has a specific permission.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the permission is granted.</returns>
    public bool HasPermission(string pluginId, PluginPermission permission)
    {
        var granted = GetGrantedPermissions(pluginId);
        return granted.HasAll(permission);
    }

    /// <summary>
    /// Demands a permission, throwing if not granted.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permission">The required permission.</param>
    /// <param name="capabilityType">Optional capability type being accessed.</param>
    /// <exception cref="PermissionDeniedException">Thrown if permission is not granted.</exception>
    public void DemandPermission(string pluginId, PluginPermission permission, Type? capabilityType = null)
    {
        if (!HasPermission(pluginId, permission))
        {
            logger?.LogWarning($"Plugin '{pluginId}' denied '{permission}' for {capabilityType?.Name ?? "unknown"}");
            throw new PermissionDeniedException(pluginId, permission, capabilityType);
        }
    }

    /// <summary>
    /// Grants permissions to a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permissions">The permissions to grant.</param>
    public void GrantPermissions(string pluginId, PluginPermission permissions)
    {
        var current = GetGrantedPermissions(pluginId);
        grants[pluginId] = current | permissions;

        logger?.LogInfo($"Granted '{permissions}' to plugin '{pluginId}'");
    }

    /// <summary>
    /// Revokes permissions from a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permissions">The permissions to revoke.</param>
    public void RevokePermissions(string pluginId, PluginPermission permissions)
    {
        var current = GetGrantedPermissions(pluginId);
        grants[pluginId] = current & ~permissions;

        logger?.LogInfo($"Revoked '{permissions}' from plugin '{pluginId}'");
    }

    /// <summary>
    /// Clears all grants for a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    public void ClearGrants(string pluginId)
    {
        grants.Remove(pluginId);
        logger?.LogInfo($"Cleared all grants for plugin '{pluginId}'");
    }

    /// <summary>
    /// Requests permission from the user.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permission">The requested permission.</param>
    /// <param name="reason">Optional reason for the request.</param>
    /// <returns>True if permission was granted.</returns>
    public async Task<bool> RequestPermissionAsync(
        string pluginId,
        PluginPermission permission,
        string? reason = null)
    {
        if (HasPermission(pluginId, permission))
        {
            return true;
        }

        if (OnPermissionRequest == null)
        {
            return false;
        }

        var request = new PermissionRequest
        {
            PluginId = pluginId,
            RequestedPermission = permission,
            Reason = reason
        };

        var response = await OnPermissionRequest(request);

        if (response.Granted)
        {
            GrantPermissions(pluginId, permission);

            if (response.Remember)
            {
                Save();
            }
        }

        return response.Granted;
    }

    /// <summary>
    /// Validates that a plugin's required permissions can be satisfied.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>A validation result.</returns>
    public PermissionValidationResult ValidatePlugin(string pluginId)
    {
        var required = GetRequiredPermissions(pluginId);
        var granted = GetGrantedPermissions(pluginId);
        var missing = required & ~granted;

        return new PermissionValidationResult
        {
            PluginId = pluginId,
            RequiredPermissions = required,
            GrantedPermissions = granted,
            MissingPermissions = missing,
            IsValid = missing == PluginPermission.None
        };
    }

    private static PluginPermission ParsePermissionList(IEnumerable<string> names)
    {
        var result = PluginPermission.None;

        foreach (var name in names)
        {
            if (PluginPermissionExtensions.TryParse(name, out var permission))
            {
                result |= permission;
            }
        }

        return result;
    }

    private static bool TryParsePermissions(IEnumerable<string> names, out PluginPermission permissions)
    {
        permissions = ParsePermissionList(names);
        return true;
    }

    private static List<string> FormatPermissions(PluginPermission permission)
    {
        var result = new List<string>();

        foreach (var value in Enum.GetValues<PluginPermission>())
        {
            if (value == PluginPermission.None)
            {
                continue;
            }

            // Skip composite permissions
            if (!IsSingleBit((long)value))
            {
                continue;
            }

            if (permission.HasAll(value))
            {
                result.Add(value.ToString());
            }
        }

        return result;
    }

    private static bool IsSingleBit(long value)
    {
        return value != 0 && (value & (value - 1)) == 0;
    }

    private sealed class PermissionStore
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("grants")]
        public List<PermissionGrant> Grants { get; set; } = [];
    }

    private sealed class PermissionGrant
    {
        [JsonPropertyName("pluginId")]
        public required string PluginId { get; set; }

        [JsonPropertyName("permissions")]
        public required List<string> Permissions { get; set; }

        [JsonPropertyName("grantedAt")]
        public DateTime GrantedAt { get; set; }
    }
}

/// <summary>
/// A request for permission from a plugin.
/// </summary>
public sealed class PermissionRequest
{
    /// <summary>
    /// Gets the plugin ID.
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// Gets the requested permission.
    /// </summary>
    public required PluginPermission RequestedPermission { get; init; }

    /// <summary>
    /// Gets an optional reason for the request.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Response to a permission request.
/// </summary>
public sealed class PermissionResponse
{
    /// <summary>
    /// Gets or sets whether the permission was granted.
    /// </summary>
    public bool Granted { get; set; }

    /// <summary>
    /// Gets or sets whether to remember this decision.
    /// </summary>
    public bool Remember { get; set; }
}

/// <summary>
/// Result of validating a plugin's permission requirements.
/// </summary>
public sealed class PermissionValidationResult
{
    /// <summary>
    /// Gets the plugin ID.
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// Gets the required permissions.
    /// </summary>
    public required PluginPermission RequiredPermissions { get; init; }

    /// <summary>
    /// Gets the granted permissions.
    /// </summary>
    public required PluginPermission GrantedPermissions { get; init; }

    /// <summary>
    /// Gets the missing permissions.
    /// </summary>
    public required PluginPermission MissingPermissions { get; init; }

    /// <summary>
    /// Gets whether the plugin has all required permissions.
    /// </summary>
    public required bool IsValid { get; init; }
}
