// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Exception thrown when a plugin attempts an operation it does not have permission for.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by <see cref="IEditorContext"/> implementations when a plugin
/// attempts to access a capability that requires a permission it has not been granted.
/// </para>
/// <para>
/// Plugins should catch this exception if they have optional functionality that depends
/// on permissions that may not be granted.
/// </para>
/// </remarks>
public sealed class PermissionDeniedException : Exception
{
    /// <summary>
    /// Gets the ID of the plugin that was denied permission.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the permission that was required but not granted.
    /// </summary>
    public PluginPermission RequiredPermission { get; }

    /// <summary>
    /// Gets the capability that the plugin attempted to access.
    /// </summary>
    public Type? CapabilityType { get; }

    /// <summary>
    /// Creates a new permission denied exception.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="requiredPermission">The required permission.</param>
    /// <param name="capabilityType">The capability type being accessed.</param>
    public PermissionDeniedException(
        string pluginId,
        PluginPermission requiredPermission,
        Type? capabilityType = null)
        : base(FormatMessage(pluginId, requiredPermission, capabilityType))
    {
        PluginId = pluginId;
        RequiredPermission = requiredPermission;
        CapabilityType = capabilityType;
    }

    /// <summary>
    /// Creates a new permission denied exception with a custom message.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="requiredPermission">The required permission.</param>
    /// <param name="message">The error message.</param>
    public PermissionDeniedException(
        string pluginId,
        PluginPermission requiredPermission,
        string message)
        : base(message)
    {
        PluginId = pluginId;
        RequiredPermission = requiredPermission;
    }

    /// <summary>
    /// Creates a new permission denied exception with an inner exception.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="requiredPermission">The required permission.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PermissionDeniedException(
        string pluginId,
        PluginPermission requiredPermission,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        PluginId = pluginId;
        RequiredPermission = requiredPermission;
    }

    private static string FormatMessage(
        string pluginId,
        PluginPermission permission,
        Type? capabilityType)
    {
        var permissionName = permission.GetDisplayName();

        if (capabilityType != null)
        {
            return $"Plugin '{pluginId}' requires '{permissionName}' permission to access {capabilityType.Name}";
        }

        return $"Plugin '{pluginId}' does not have the required '{permissionName}' permission";
    }
}
