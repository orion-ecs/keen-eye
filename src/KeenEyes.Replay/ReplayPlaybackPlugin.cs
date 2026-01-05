namespace KeenEyes.Replay;

/// <summary>
/// Plugin that enables replay playback in a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// The ReplayPlaybackPlugin provides runtime replay playback functionality for
/// scenarios like demo/attract mode, killcams, instant replay, tutorial demonstrations,
/// and bug reproduction from QA reports.
/// </para>
/// <para>
/// After installation, access the <see cref="ReplayPlayer"/> through the world's
/// extension API to control playback.
/// </para>
/// <para>
/// <strong>Important:</strong> This plugin cannot be installed on a world that already
/// has <see cref="ReplayPlugin"/> installed. A world can either record or play back
/// replays, but not both simultaneously.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the plugin
/// using var world = new World();
/// world.InstallPlugin(new ReplayPlaybackPlugin());
///
/// // Get the player and load a replay
/// var player = world.GetExtension&lt;ReplayPlayer&gt;();
/// player.LoadReplay("demo.kreplay");
/// player.Play();
///
/// // In your game loop
/// while (player.State == PlaybackState.Playing)
/// {
///     player.Update(deltaTime);
///
///     // Process the current frame
///     var frame = player.GetCurrentFrame();
///     // ... handle frame events ...
/// }
/// </code>
/// </example>
public sealed class ReplayPlaybackPlugin : IWorldPlugin
{
    private ReplayPlayer? player;

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name => "ReplayPlayback";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Check for mutual exclusion with ReplayPlugin
        // A world cannot record and play back simultaneously
        if (context.TryGetExtension<ReplayRecorder>(out _))
        {
            throw new InvalidOperationException(
                "Cannot install ReplayPlaybackPlugin on a world that has ReplayPlugin installed. " +
                "A world can either record or play back replays, but not both simultaneously. " +
                "Uninstall ReplayPlugin first if you want to play back a replay.");
        }

        // Create and register the player
        player = new ReplayPlayer();
        context.SetExtension(player);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        if (player is not null)
        {
            // Stop any active playback
            if (player.IsLoaded && player.State == PlaybackState.Playing)
            {
                player.Stop();
            }

            // Unload any loaded replay
            if (player.IsLoaded)
            {
                player.UnloadReplay();
            }

            // Dispose the player
            player.Dispose();
        }

        // Remove extension
        context.RemoveExtension<ReplayPlayer>();

        player = null;
    }
}
