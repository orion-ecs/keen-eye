// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Permissions that can be granted to editor plugins.
/// </summary>
/// <remarks>
/// <para>
/// Plugins declare required and optional permissions in their manifest.
/// The permission manager validates access before allowing capability usage.
/// </para>
/// <para>
/// Permissions are organized by category:
/// </para>
/// <list type="bullet">
/// <item><description>File system: Read, write, and project-scoped access</description></item>
/// <item><description>Network: Client and server capabilities</description></item>
/// <item><description>System: Process execution, environment access</description></item>
/// <item><description>Code: Reflection, native code, assembly loading</description></item>
/// <item><description>Editor: Clipboard, notifications, dialogs</description></item>
/// </list>
/// </remarks>
[Flags]
public enum PluginPermission : long
{
    /// <summary>
    /// No permissions granted.
    /// </summary>
    None = 0,

    #region File System Permissions (bits 0-7)

    /// <summary>
    /// Read files anywhere on the file system.
    /// </summary>
    FileSystemRead = 1L << 0,

    /// <summary>
    /// Write files anywhere on the file system.
    /// </summary>
    FileSystemWrite = 1L << 1,

    /// <summary>
    /// Access files within the project directory only.
    /// </summary>
    /// <remarks>
    /// This is the most common permission for typical plugins.
    /// Allows reading and writing project assets and configuration.
    /// </remarks>
    FileSystemProject = 1L << 2,

    /// <summary>
    /// Access the user's configuration directory (~/.keeneyes/).
    /// </summary>
    FileSystemConfig = 1L << 3,

    /// <summary>
    /// Access the system's temp directory.
    /// </summary>
    FileSystemTemp = 1L << 4,

    #endregion

    #region Network Permissions (bits 8-15)

    /// <summary>
    /// Make outgoing network connections (HTTP client, sockets, etc.).
    /// </summary>
    NetworkClient = 1L << 8,

    /// <summary>
    /// Listen for incoming network connections (HTTP server, socket listeners).
    /// </summary>
    NetworkServer = 1L << 9,

    /// <summary>
    /// Access to localhost only (useful for local dev servers).
    /// </summary>
    NetworkLocalhost = 1L << 10,

    #endregion

    #region System Permissions (bits 16-23)

    /// <summary>
    /// Execute external processes.
    /// </summary>
    ProcessExecution = 1L << 16,

    /// <summary>
    /// Read environment variables.
    /// </summary>
    EnvironmentRead = 1L << 17,

    /// <summary>
    /// Access system clipboard.
    /// </summary>
    ClipboardAccess = 1L << 18,

    /// <summary>
    /// Access system notifications.
    /// </summary>
    Notifications = 1L << 19,

    #endregion

    #region Code Permissions (bits 24-31)

    /// <summary>
    /// Use reflection APIs.
    /// </summary>
    Reflection = 1L << 24,

    /// <summary>
    /// Call native code via P/Invoke.
    /// </summary>
    NativeCode = 1L << 25,

    /// <summary>
    /// Load additional assemblies at runtime.
    /// </summary>
    AssemblyLoading = 1L << 26,

    /// <summary>
    /// Use unsafe code and pointers.
    /// </summary>
    UnsafeCode = 1L << 27,

    #endregion

    #region Editor Permissions (bits 32-39)

    /// <summary>
    /// Register menu items.
    /// </summary>
    MenuAccess = 1L << 32,

    /// <summary>
    /// Register keyboard shortcuts.
    /// </summary>
    ShortcutAccess = 1L << 33,

    /// <summary>
    /// Create dockable panels.
    /// </summary>
    PanelAccess = 1L << 34,

    /// <summary>
    /// Register viewport gizmos and overlays.
    /// </summary>
    ViewportAccess = 1L << 35,

    /// <summary>
    /// Register inspector property drawers.
    /// </summary>
    InspectorAccess = 1L << 36,

    /// <summary>
    /// Access the undo/redo system.
    /// </summary>
    UndoAccess = 1L << 37,

    /// <summary>
    /// Access the selection manager.
    /// </summary>
    SelectionAccess = 1L << 38,

    /// <summary>
    /// Access the asset database.
    /// </summary>
    AssetDatabaseAccess = 1L << 39,

    #endregion

    #region Composite Permissions

    /// <summary>
    /// Standard editor plugin permissions.
    /// Includes project file access, clipboard, and basic editor capabilities.
    /// </summary>
    StandardEditor = FileSystemProject | ClipboardAccess |
                     MenuAccess | ShortcutAccess | PanelAccess |
                     InspectorAccess | UndoAccess | SelectionAccess,

    /// <summary>
    /// Full file system access (read and write).
    /// </summary>
    FileSystemFull = FileSystemRead | FileSystemWrite,

    /// <summary>
    /// Full network access (client and server).
    /// </summary>
    NetworkFull = NetworkClient | NetworkServer,

    /// <summary>
    /// All editor UI permissions.
    /// </summary>
    EditorUI = MenuAccess | ShortcutAccess | PanelAccess |
               ViewportAccess | InspectorAccess,

    /// <summary>
    /// Full trust - all permissions granted.
    /// </summary>
    /// <remarks>
    /// Use with extreme caution. Only grant to fully trusted plugins.
    /// </remarks>
    FullTrust = ~None

    #endregion
}

/// <summary>
/// Extension methods for <see cref="PluginPermission"/>.
/// </summary>
public static class PluginPermissionExtensions
{
    /// <summary>
    /// Checks if all specified permissions are granted.
    /// </summary>
    /// <param name="granted">The granted permissions.</param>
    /// <param name="required">The required permissions.</param>
    /// <returns>True if all required permissions are granted.</returns>
    public static bool HasAll(this PluginPermission granted, PluginPermission required)
    {
        return (granted & required) == required;
    }

    /// <summary>
    /// Checks if any of the specified permissions are granted.
    /// </summary>
    /// <param name="granted">The granted permissions.</param>
    /// <param name="any">The permissions to check.</param>
    /// <returns>True if any of the permissions are granted.</returns>
    public static bool HasAny(this PluginPermission granted, PluginPermission any)
    {
        return (granted & any) != PluginPermission.None;
    }

    /// <summary>
    /// Gets the display name for a permission.
    /// </summary>
    /// <param name="permission">The permission.</param>
    /// <returns>A human-readable name for the permission.</returns>
    public static string GetDisplayName(this PluginPermission permission)
    {
        return permission switch
        {
            PluginPermission.FileSystemRead => "File System (Read)",
            PluginPermission.FileSystemWrite => "File System (Write)",
            PluginPermission.FileSystemProject => "Project Files",
            PluginPermission.FileSystemConfig => "Configuration Files",
            PluginPermission.FileSystemTemp => "Temporary Files",
            PluginPermission.NetworkClient => "Network (Outgoing)",
            PluginPermission.NetworkServer => "Network (Incoming)",
            PluginPermission.NetworkLocalhost => "Localhost Only",
            PluginPermission.ProcessExecution => "Execute Processes",
            PluginPermission.EnvironmentRead => "Environment Variables",
            PluginPermission.ClipboardAccess => "Clipboard",
            PluginPermission.Notifications => "System Notifications",
            PluginPermission.Reflection => "Reflection",
            PluginPermission.NativeCode => "Native Code (P/Invoke)",
            PluginPermission.AssemblyLoading => "Assembly Loading",
            PluginPermission.UnsafeCode => "Unsafe Code",
            PluginPermission.MenuAccess => "Menu Items",
            PluginPermission.ShortcutAccess => "Keyboard Shortcuts",
            PluginPermission.PanelAccess => "Panels",
            PluginPermission.ViewportAccess => "Viewport",
            PluginPermission.InspectorAccess => "Inspector",
            PluginPermission.UndoAccess => "Undo/Redo",
            PluginPermission.SelectionAccess => "Selection",
            PluginPermission.AssetDatabaseAccess => "Asset Database",
            _ => permission.ToString()
        };
    }

    /// <summary>
    /// Gets a description of what the permission allows.
    /// </summary>
    /// <param name="permission">The permission.</param>
    /// <returns>A description of the permission.</returns>
    public static string GetDescription(this PluginPermission permission)
    {
        return permission switch
        {
            PluginPermission.FileSystemRead => "Read files from anywhere on the file system",
            PluginPermission.FileSystemWrite => "Write files anywhere on the file system",
            PluginPermission.FileSystemProject => "Read and write files within the project directory",
            PluginPermission.FileSystemConfig => "Access configuration files in ~/.keeneyes/",
            PluginPermission.FileSystemTemp => "Create and access temporary files",
            PluginPermission.NetworkClient => "Make outgoing network connections",
            PluginPermission.NetworkServer => "Listen for incoming network connections",
            PluginPermission.NetworkLocalhost => "Connect to localhost services only",
            PluginPermission.ProcessExecution => "Start and interact with external processes",
            PluginPermission.EnvironmentRead => "Read system environment variables",
            PluginPermission.ClipboardAccess => "Read and write to the system clipboard",
            PluginPermission.Notifications => "Display system notifications",
            PluginPermission.Reflection => "Use .NET reflection to inspect types at runtime",
            PluginPermission.NativeCode => "Call native libraries using P/Invoke",
            PluginPermission.AssemblyLoading => "Load additional .NET assemblies at runtime",
            PluginPermission.UnsafeCode => "Use unsafe code and raw memory pointers",
            PluginPermission.MenuAccess => "Add items to the editor menu bar",
            PluginPermission.ShortcutAccess => "Register keyboard shortcuts",
            PluginPermission.PanelAccess => "Create dockable editor panels",
            PluginPermission.ViewportAccess => "Add gizmos and overlays to the viewport",
            PluginPermission.InspectorAccess => "Register custom property drawers",
            PluginPermission.UndoAccess => "Record and replay undo operations",
            PluginPermission.SelectionAccess => "Read and modify the editor selection",
            PluginPermission.AssetDatabaseAccess => "Query and modify assets in the project",
            _ => $"Permission: {permission}"
        };
    }

    /// <summary>
    /// Parses a permission name string to a <see cref="PluginPermission"/>.
    /// </summary>
    /// <param name="name">The permission name (e.g., "FileSystemProject").</param>
    /// <param name="permission">The parsed permission.</param>
    /// <returns>True if parsing succeeded.</returns>
    public static bool TryParse(string name, out PluginPermission permission)
    {
        return Enum.TryParse(name, ignoreCase: true, out permission);
    }
}
