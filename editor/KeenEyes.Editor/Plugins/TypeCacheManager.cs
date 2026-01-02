// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Manages type caches that need clearing when plugins are unloaded.
/// </summary>
/// <remarks>
/// <para>
/// When a plugin is unloaded (hot reload), any cached <see cref="Type"/> references
/// from that plugin's assembly become stale. This manager coordinates cache clearing
/// across all registered capability implementations.
/// </para>
/// <para>
/// Capability implementations (e.g., panel registry, property drawer cache) register
/// their clearing callbacks during initialization. When a plugin unloads, all callbacks
/// are invoked with the plugin ID, allowing each cache to remove entries associated
/// with that plugin.
/// </para>
/// </remarks>
internal sealed class TypeCacheManager
{
    private readonly List<Action<string>> clearCallbacks = [];
    private readonly Lock lockObject = new();

    /// <summary>
    /// Registers a callback to be invoked when a plugin is unloading.
    /// </summary>
    /// <param name="callback">
    /// A callback that receives the plugin ID and should clear any cached
    /// entries associated with that plugin.
    /// </param>
    /// <returns>A disposable that removes the callback when disposed.</returns>
    public IDisposable RegisterClearCallback(Action<string> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        lock (lockObject)
        {
            clearCallbacks.Add(callback);
        }

        return new CallbackRegistration(this, callback);
    }

    /// <summary>
    /// Notifies all registered callbacks that a plugin is unloading.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin being unloaded.</param>
    /// <remarks>
    /// This should be called before the plugin's <see cref="PluginLoadContext"/> is unloaded.
    /// </remarks>
    public void NotifyPluginUnloading(string pluginId)
    {
        ArgumentException.ThrowIfNullOrEmpty(pluginId);

        Action<string>[] callbacksCopy;
        lock (lockObject)
        {
            callbacksCopy = [.. clearCallbacks];
        }

        foreach (var callback in callbacksCopy)
        {
            try
            {
                callback(pluginId);
            }
            catch (Exception)
            {
                // Log but don't throw - we need to notify all callbacks
                // In production, this would use the editor's logging system
            }
        }
    }

    /// <summary>
    /// Gets the number of registered callbacks.
    /// </summary>
    internal int CallbackCount
    {
        get
        {
            lock (lockObject)
            {
                return clearCallbacks.Count;
            }
        }
    }

    private void RemoveCallback(Action<string> callback)
    {
        lock (lockObject)
        {
            clearCallbacks.Remove(callback);
        }
    }

    private sealed class CallbackRegistration(TypeCacheManager manager, Action<string> callback) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (!disposed)
            {
                manager.RemoveCallback(callback);
                disposed = true;
            }
        }
    }
}
