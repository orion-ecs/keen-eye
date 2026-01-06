using System.Numerics;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Manages multiple simultaneous ghost recordings for playback.
/// </summary>
/// <remarks>
/// <para>
/// The GhostManager provides a centralized way to manage multiple ghosts,
/// such as "Personal Best", "World Record", and "Friend's Ghost" in a
/// racing game. Each ghost can have its own visual configuration.
/// </para>
/// <para>
/// Basic usage:
/// <list type="number">
/// <item><description>Create a GhostManager instance.</description></item>
/// <item><description>Add ghosts using <see cref="AddGhost"/>.</description></item>
/// <item><description>Call <see cref="PlayAll"/> to start all ghosts.</description></item>
/// <item><description>Call <see cref="Update"/> each frame to advance playback.</description></item>
/// <item><description>Enumerate <see cref="ActiveGhosts"/> to render each ghost.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var manager = new GhostManager();
///
/// // Add personal best ghost (green tint)
/// var pbConfig = new GhostVisualConfig
/// {
///     TintColor = new Vector4(0, 1, 0, 1),
///     Opacity = 0.5f,
///     Label = "Personal Best"
/// };
/// manager.AddGhost("pb", personalBestData, pbConfig);
///
/// // Add world record ghost (gold tint)
/// var wrConfig = new GhostVisualConfig
/// {
///     TintColor = new Vector4(1, 0.84f, 0, 1),
///     Opacity = 0.5f,
///     Label = "World Record"
/// };
/// manager.AddGhost("wr", worldRecordData, wrConfig);
///
/// // Start all ghosts
/// manager.PlayAll();
///
/// // In game loop
/// manager.Update(deltaTime);
///
/// foreach (var ghost in manager.ActiveGhosts)
/// {
///     RenderGhost(ghost.Player.Position, ghost.Player.Rotation, ghost.Config);
/// }
/// </code>
/// </example>
public sealed class GhostManager : IDisposable
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<string, ManagedGhost> ghosts = new();
    private bool disposed;

    /// <summary>
    /// Gets or sets the default synchronization mode for new ghosts.
    /// </summary>
    public GhostSyncMode DefaultSyncMode { get; set; } = GhostSyncMode.TimeSynced;

    /// <summary>
    /// Gets the number of registered ghosts.
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncRoot)
            {
                return ghosts.Count;
            }
        }
    }

    /// <summary>
    /// Gets all currently active ghosts for rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property returns a snapshot of all ghosts with their current
    /// positions and visual configurations. Use this to render ghosts in
    /// your game loop.
    /// </para>
    /// </remarks>
    public IEnumerable<GhostInstance> ActiveGhosts
    {
        get
        {
            lock (syncRoot)
            {
                return ghosts.Values
                    .Select(g => new GhostInstance(g.Id, g.Player, g.Config))
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Adds a ghost to the manager.
    /// </summary>
    /// <param name="id">A unique identifier for this ghost.</param>
    /// <param name="data">The ghost data to add.</param>
    /// <param name="config">The visual configuration for this ghost.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or already exists.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void AddGhost(string id, GhostData data, GhostVisualConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        ThrowIfDisposed();

        config ??= GhostVisualConfig.Default;

        lock (syncRoot)
        {
            if (ghosts.ContainsKey(id))
            {
                throw new ArgumentException($"A ghost with ID '{id}' already exists.", nameof(id));
            }

            var player = new GhostPlayer();
            player.Load(data);
            player.SyncMode = DefaultSyncMode;

            ghosts[id] = new ManagedGhost(id, player, config);
        }
    }

    /// <summary>
    /// Adds a ghost from a file.
    /// </summary>
    /// <param name="id">A unique identifier for this ghost.</param>
    /// <param name="path">The path to the .keghost file.</param>
    /// <param name="config">The visual configuration for this ghost.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or already exists.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="GhostFormatException">Thrown when the file format is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void AddGhostFromFile(string id, string path, GhostVisualConfig? config = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(path);

        var (_, data) = GhostFileFormat.ReadFromFile(path);
        AddGhost(id, data, config);
    }

    /// <summary>
    /// Removes a ghost from the manager.
    /// </summary>
    /// <param name="id">The identifier of the ghost to remove.</param>
    /// <returns>True if the ghost was removed; false if it didn't exist.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public bool RemoveGhost(string id)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            if (ghosts.TryGetValue(id, out var ghost))
            {
                ghost.Player.Dispose();
                ghosts.Remove(id);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Removes all ghosts from the manager.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Clear()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.Dispose();
            }
            ghosts.Clear();
        }
    }

    /// <summary>
    /// Gets the player for a specific ghost.
    /// </summary>
    /// <param name="id">The ghost identifier.</param>
    /// <returns>The ghost player, or null if not found.</returns>
    public GhostPlayer? GetPlayer(string id)
    {
        lock (syncRoot)
        {
            return ghosts.TryGetValue(id, out var ghost) ? ghost.Player : null;
        }
    }

    /// <summary>
    /// Gets the visual configuration for a specific ghost.
    /// </summary>
    /// <param name="id">The ghost identifier.</param>
    /// <returns>The visual configuration, or null if not found.</returns>
    public GhostVisualConfig? GetConfig(string id)
    {
        lock (syncRoot)
        {
            return ghosts.TryGetValue(id, out var ghost) ? ghost.Config : null;
        }
    }

    /// <summary>
    /// Updates the visual configuration for a ghost.
    /// </summary>
    /// <param name="id">The ghost identifier.</param>
    /// <param name="config">The new visual configuration.</param>
    /// <returns>True if the ghost was found and updated; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public bool SetConfig(string id, GhostVisualConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        lock (syncRoot)
        {
            if (ghosts.TryGetValue(id, out var ghost))
            {
                ghosts[id] = ghost with { Config = config };
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Checks if a ghost with the specified ID exists.
    /// </summary>
    /// <param name="id">The ghost identifier.</param>
    /// <returns>True if the ghost exists; false otherwise.</returns>
    public bool ContainsGhost(string id)
    {
        lock (syncRoot)
        {
            return ghosts.ContainsKey(id);
        }
    }

    /// <summary>
    /// Starts playback for all ghosts.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void PlayAll()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.Play();
            }
        }
    }

    /// <summary>
    /// Pauses playback for all ghosts.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void PauseAll()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.Pause();
            }
        }
    }

    /// <summary>
    /// Stops playback for all ghosts and resets to the beginning.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void StopAll()
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.Stop();
            }
        }
    }

    /// <summary>
    /// Updates all ghosts with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Update(float deltaTime)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.Update(deltaTime);
            }
        }
    }

    /// <summary>
    /// Updates all distance-synced ghosts with the player's traveled distance.
    /// </summary>
    /// <param name="distance">The current distance traveled by the player.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void UpdateByDistance(float distance)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                if (ghost.Player.SyncMode == GhostSyncMode.DistanceSynced)
                {
                    ghost.Player.UpdateByDistance(distance);
                }
            }
        }
    }

    /// <summary>
    /// Seeks all ghosts to the specified time.
    /// </summary>
    /// <param name="time">The time to seek to.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void SeekAllToTime(TimeSpan time)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                try
                {
                    ghost.Player.SeekToTime(time);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Some ghosts may be shorter than the target time
                    // Seek to end instead
                    if (ghost.Player.TotalFrames > 0)
                    {
                        ghost.Player.SeekToFrame(ghost.Player.TotalFrames - 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sets the sync mode for all ghosts.
    /// </summary>
    /// <param name="syncMode">The sync mode to set.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void SetAllSyncMode(GhostSyncMode syncMode)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            DefaultSyncMode = syncMode;
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.SyncMode = syncMode;
            }
        }
    }

    /// <summary>
    /// Sets the playback speed for all ghosts.
    /// </summary>
    /// <param name="speed">The playback speed (0.25 to 4.0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void SetAllPlaybackSpeed(float speed)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.PlaybackSpeed = speed;
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="GhostManager"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            foreach (var ghost in ghosts.Values)
            {
                ghost.Player.Dispose();
            }
            ghosts.Clear();
            disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    /// <summary>
    /// Internal record for managing a ghost with its player and config.
    /// </summary>
    private sealed record ManagedGhost(string Id, GhostPlayer Player, GhostVisualConfig Config);
}

/// <summary>
/// Represents a ghost instance for rendering purposes.
/// </summary>
/// <remarks>
/// This struct provides access to a ghost's current state and visual configuration
/// for use in rendering code.
/// </remarks>
/// <param name="Id">The unique identifier of this ghost.</param>
/// <param name="Player">The ghost player containing current position and state.</param>
/// <param name="Config">The visual configuration for rendering.</param>
public readonly record struct GhostInstance(
    string Id,
    GhostPlayer Player,
    GhostVisualConfig Config)
{
    /// <summary>
    /// Gets the current position of this ghost.
    /// </summary>
    public Vector3 Position => Player.Position;

    /// <summary>
    /// Gets the current rotation of this ghost.
    /// </summary>
    public Quaternion Rotation => Player.Rotation;

    /// <summary>
    /// Gets the current scale of this ghost.
    /// </summary>
    public Vector3 Scale => Player.Scale;

    /// <summary>
    /// Gets the current distance traveled by this ghost.
    /// </summary>
    public float Distance => Player.Distance;

    /// <summary>
    /// Gets the playback state of this ghost.
    /// </summary>
    public GhostPlaybackState State => Player.State;

    /// <summary>
    /// Gets the configured opacity for this ghost.
    /// </summary>
    public float Opacity => Config.Opacity;

    /// <summary>
    /// Gets the configured tint color for this ghost.
    /// </summary>
    public Vector4 TintColor => Config.TintColor;

    /// <summary>
    /// Gets the optional label for this ghost.
    /// </summary>
    public string? Label => Config.Label;
}
