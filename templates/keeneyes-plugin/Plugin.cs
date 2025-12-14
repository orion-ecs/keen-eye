using KeenEyes;

namespace KeenEyesPlugin;

/// <summary>
/// Example plugin that provides custom functionality.
/// </summary>
/// <remarks>
/// Plugins encapsulate related systems, components, and extensions.
/// They are installed per-world, maintaining isolation between worlds.
/// </remarks>
public class KeenEyesPluginPlugin : IWorldPlugin
{
    /// <inheritdoc />
    public string Name => "KeenEyesPlugin";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Register systems through the context (tracked for auto-cleanup)
        context.AddSystem<ExampleSystem>(SystemPhase.Update, order: 0);

        // Expose an extension API for application code
        context.SetExtension(new PluginExtension());
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Remove our extension (systems are auto-removed)
        context.RemoveExtension<PluginExtension>();
    }
}

/// <summary>
/// Extension class that the plugin exposes to application code.
/// </summary>
/// <remarks>
/// Extensions allow plugins to provide custom APIs accessible via World.GetExtension&lt;T&gt;().
/// The [PluginExtension] attribute generates a typed property on World for convenient access.
/// </remarks>
[PluginExtension("KeenEyesPluginExtension")]
public sealed class PluginExtension
{
    /// <summary>
    /// Example property that application code can access.
    /// </summary>
    public int ExampleValue { get; set; }
}
