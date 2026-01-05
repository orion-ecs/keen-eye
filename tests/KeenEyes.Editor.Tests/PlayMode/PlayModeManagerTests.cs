using System.Text.Json;
using KeenEyes;
using KeenEyes.Capabilities;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Serialization;

namespace KeenEyes.Editor.Tests.PlayMode;

public class PlayModeManagerTests : IDisposable
{
    private readonly World world;
    private readonly TestComponentSerializer serializer;
    private readonly PlayModeManager manager;

    public PlayModeManagerTests()
    {
        world = new World();
        serializer = new TestComponentSerializer();
        manager = new PlayModeManager(world, serializer);
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesToEditingState()
    {
        Assert.Equal(PlayModeState.Editing, manager.CurrentState);
        Assert.True(manager.IsEditing);
        Assert.False(manager.IsPlaying);
        Assert.False(manager.IsPaused);
        Assert.False(manager.IsInPlayMode);
    }

    [Fact]
    public void Constructor_ThrowsOnNullWorld()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayModeManager(null!, serializer));
    }

    [Fact]
    public void Constructor_ThrowsOnNullSerializer()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayModeManager(world, null!));
    }

    #endregion

    #region Play Tests

    [Fact]
    public void Play_FromEditing_TransitionsToPlaying()
    {
        var result = manager.Play();

        Assert.True(result);
        Assert.Equal(PlayModeState.Playing, manager.CurrentState);
        Assert.True(manager.IsPlaying);
        Assert.True(manager.IsInPlayMode);
        Assert.False(manager.IsEditing);
    }

    [Fact]
    public void Play_FromPlaying_ReturnsFalse()
    {
        manager.Play();

        var result = manager.Play();

        Assert.False(result);
        Assert.Equal(PlayModeState.Playing, manager.CurrentState);
    }

    [Fact]
    public void Play_FromPaused_ReturnsFalse()
    {
        manager.Play();
        manager.Pause();

        var result = manager.Play();

        Assert.False(result);
        Assert.Equal(PlayModeState.Paused, manager.CurrentState);
    }

    [Fact]
    public void Play_RaisesStateChangedEvent()
    {
        PlayModeStateChangedEventArgs? eventArgs = null;
        manager.StateChanged += (_, e) => eventArgs = e;

        manager.Play();

        Assert.NotNull(eventArgs);
        Assert.Equal(PlayModeState.Editing, eventArgs.PreviousState);
        Assert.Equal(PlayModeState.Playing, eventArgs.CurrentState);
    }

    #endregion

    #region Pause Tests

    [Fact]
    public void Pause_FromPlaying_TransitionsToPaused()
    {
        manager.Play();

        var result = manager.Pause();

        Assert.True(result);
        Assert.Equal(PlayModeState.Paused, manager.CurrentState);
        Assert.True(manager.IsPaused);
        Assert.True(manager.IsInPlayMode);
        Assert.False(manager.IsPlaying);
    }

    [Fact]
    public void Pause_FromEditing_ReturnsFalse()
    {
        var result = manager.Pause();

        Assert.False(result);
        Assert.Equal(PlayModeState.Editing, manager.CurrentState);
    }

    [Fact]
    public void Pause_FromPaused_ReturnsFalse()
    {
        manager.Play();
        manager.Pause();

        var result = manager.Pause();

        Assert.False(result);
        Assert.Equal(PlayModeState.Paused, manager.CurrentState);
    }

    [Fact]
    public void Pause_RaisesStateChangedEvent()
    {
        manager.Play();
        PlayModeStateChangedEventArgs? eventArgs = null;
        manager.StateChanged += (_, e) => eventArgs = e;

        manager.Pause();

        Assert.NotNull(eventArgs);
        Assert.Equal(PlayModeState.Playing, eventArgs.PreviousState);
        Assert.Equal(PlayModeState.Paused, eventArgs.CurrentState);
    }

    #endregion

    #region Resume Tests

    [Fact]
    public void Resume_FromPaused_TransitionsToPlaying()
    {
        manager.Play();
        manager.Pause();

        var result = manager.Resume();

        Assert.True(result);
        Assert.Equal(PlayModeState.Playing, manager.CurrentState);
        Assert.True(manager.IsPlaying);
    }

    [Fact]
    public void Resume_FromEditing_ReturnsFalse()
    {
        var result = manager.Resume();

        Assert.False(result);
        Assert.Equal(PlayModeState.Editing, manager.CurrentState);
    }

    [Fact]
    public void Resume_FromPlaying_ReturnsFalse()
    {
        manager.Play();

        var result = manager.Resume();

        Assert.False(result);
        Assert.Equal(PlayModeState.Playing, manager.CurrentState);
    }

    [Fact]
    public void Resume_RaisesStateChangedEvent()
    {
        manager.Play();
        manager.Pause();
        PlayModeStateChangedEventArgs? eventArgs = null;
        manager.StateChanged += (_, e) => eventArgs = e;

        manager.Resume();

        Assert.NotNull(eventArgs);
        Assert.Equal(PlayModeState.Paused, eventArgs.PreviousState);
        Assert.Equal(PlayModeState.Playing, eventArgs.CurrentState);
    }

    #endregion

    #region Stop Tests

    [Fact]
    public void Stop_FromPlaying_TransitionsToEditing()
    {
        manager.Play();

        var result = manager.Stop();

        Assert.True(result);
        Assert.Equal(PlayModeState.Editing, manager.CurrentState);
        Assert.True(manager.IsEditing);
        Assert.False(manager.IsInPlayMode);
    }

    [Fact]
    public void Stop_FromPaused_TransitionsToEditing()
    {
        manager.Play();
        manager.Pause();

        var result = manager.Stop();

        Assert.True(result);
        Assert.Equal(PlayModeState.Editing, manager.CurrentState);
    }

    [Fact]
    public void Stop_FromEditing_ReturnsFalse()
    {
        var result = manager.Stop();

        Assert.False(result);
        Assert.Equal(PlayModeState.Editing, manager.CurrentState);
    }

    [Fact]
    public void Stop_RaisesStateChangedEvent()
    {
        manager.Play();
        PlayModeStateChangedEventArgs? eventArgs = null;
        manager.StateChanged += (_, e) => eventArgs = e;

        manager.Stop();

        Assert.NotNull(eventArgs);
        Assert.Equal(PlayModeState.Playing, eventArgs.PreviousState);
        Assert.Equal(PlayModeState.Editing, eventArgs.CurrentState);
    }

    #endregion

    #region TogglePlayPause Tests

    [Fact]
    public void TogglePlayPause_FromEditing_TransitionsToPlaying()
    {
        manager.TogglePlayPause();

        Assert.Equal(PlayModeState.Playing, manager.CurrentState);
    }

    [Fact]
    public void TogglePlayPause_FromPlaying_TransitionsToPaused()
    {
        manager.Play();

        manager.TogglePlayPause();

        Assert.Equal(PlayModeState.Paused, manager.CurrentState);
    }

    [Fact]
    public void TogglePlayPause_FromPaused_TransitionsToPlaying()
    {
        manager.Play();
        manager.Pause();

        manager.TogglePlayPause();

        Assert.Equal(PlayModeState.Playing, manager.CurrentState);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void Stop_RestoresEntityCount()
    {
        // Create initial entities
        world.Spawn("Entity1").Build();
        world.Spawn("Entity2").Build();
        var initialCount = world.EntityCount;

        manager.Play();

        // Add more entities during play mode
        world.Spawn("Entity3").Build();
        world.Spawn("Entity4").Build();
        Assert.True(world.EntityCount > initialCount);

        manager.Stop();

        // World should be restored to initial entity count
        Assert.Equal(initialCount, world.EntityCount);
    }

    [Fact]
    public void Stop_RestoresAfterDespawningEntities()
    {
        // Create initial entities
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();
        var initialCount = world.EntityCount;

        manager.Play();

        // Despawn entities during play mode
        world.Despawn(entity1);
        world.Despawn(entity2);
        Assert.Equal(0, world.EntityCount);

        manager.Stop();

        // World should be restored to initial entity count
        Assert.Equal(initialCount, world.EntityCount);
    }

    [Fact]
    public void Stop_CanPlayAgainAfterStop()
    {
        world.Spawn("Entity1").Build();
        var initialCount = world.EntityCount;

        // First play/stop cycle
        manager.Play();
        world.Spawn("Entity2").Build();
        manager.Stop();
        Assert.Equal(initialCount, world.EntityCount);

        // Second play/stop cycle
        manager.Play();
        world.Spawn("Entity3").Build();
        world.Spawn("Entity4").Build();
        manager.Stop();
        Assert.Equal(initialCount, world.EntityCount);
    }

    #endregion

    #region State Property Tests

    [Fact]
    public void IsInPlayMode_TrueWhenPlaying()
    {
        manager.Play();

        Assert.True(manager.IsInPlayMode);
    }

    [Fact]
    public void IsInPlayMode_TrueWhenPaused()
    {
        manager.Play();
        manager.Pause();

        Assert.True(manager.IsInPlayMode);
    }

    [Fact]
    public void IsInPlayMode_FalseWhenEditing()
    {
        Assert.False(manager.IsInPlayMode);
    }

    #endregion

    #region Test Helper

    /// <summary>
    /// A minimal test implementation of IComponentSerializer for testing.
    /// This serializer supports no types, but the snapshot manager will still
    /// capture and restore entity structure and names.
    /// </summary>
    private sealed class TestComponentSerializer : IComponentSerializer
    {
        public bool IsSerializable(Type type) => false;
        public bool IsSerializable(string typeName) => false;
        public object? Deserialize(string typeName, JsonElement json) => null;
        public JsonElement? Serialize(Type type, object value) => null;
        public Type? GetType(string typeName) => null;
        public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag) => null;
        public bool SetSingleton(ISerializationCapability serialization, string typeName, object value) => false;
        public object? CreateDefault(string typeName) => null;
        public int GetVersion(string typeName) => 1;
        public int GetVersion(Type type) => 1;
    }

    #endregion
}
