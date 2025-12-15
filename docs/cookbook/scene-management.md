# Scene Management

## Problem

You need to load, unload, and transition between different game scenes (menus, levels, etc.) while managing entity lifecycles.

## Solution

### Scene Definition

```csharp
public interface IScene
{
    string Name { get; }
    void Load(World world);
    void Unload(World world);
}

public abstract class SceneBase : IScene
{
    public abstract string Name { get; }

    protected List<Entity> sceneEntities = new();

    public virtual void Load(World world)
    {
        // Override in derived classes
    }

    public virtual void Unload(World world)
    {
        // Despawn all entities created by this scene
        var buffer = world.GetCommandBuffer();
        foreach (var entity in sceneEntities)
        {
            if (world.IsAlive(entity))
            {
                buffer.Despawn(entity);
            }
        }
        buffer.Execute();
        sceneEntities.Clear();
    }

    protected Entity SpawnSceneEntity(World world)
    {
        var entity = world.Spawn().Build();
        sceneEntities.Add(entity);
        return entity;
    }

    protected EntityBuilder CreateSceneEntity(World world)
    {
        return new TrackedEntityBuilder(world.Spawn(), sceneEntities);
    }
}
```

### Scene Manager

```csharp
public sealed class SceneManager
{
    private readonly World world;
    private readonly Dictionary<string, IScene> scenes = new();
    private IScene? currentScene;
    private IScene? nextScene;
    private SceneTransition? activeTransition;

    public string? CurrentSceneName => currentScene?.Name;

    public SceneManager(World world)
    {
        this.world = world;
    }

    public void RegisterScene(IScene scene)
    {
        scenes[scene.Name] = scene;
    }

    public void LoadScene(string name)
    {
        if (!scenes.TryGetValue(name, out var scene))
            throw new ArgumentException($"Scene '{name}' not found");

        nextScene = scene;
    }

    public void LoadSceneWithTransition(string name, SceneTransition transition)
    {
        if (!scenes.TryGetValue(name, out var scene))
            throw new ArgumentException($"Scene '{name}' not found");

        nextScene = scene;
        activeTransition = transition;
        activeTransition.Start(TransitionPhase.Out);
    }

    public void Update(float deltaTime)
    {
        if (activeTransition != null)
        {
            activeTransition.Update(deltaTime);

            if (activeTransition.Phase == TransitionPhase.Out && activeTransition.IsComplete)
            {
                // Transition out complete - swap scenes
                PerformSceneSwap();
                activeTransition.Start(TransitionPhase.In);
            }
            else if (activeTransition.Phase == TransitionPhase.In && activeTransition.IsComplete)
            {
                // Transition complete
                activeTransition = null;
            }
        }
        else if (nextScene != null)
        {
            // Immediate swap (no transition)
            PerformSceneSwap();
        }
    }

    private void PerformSceneSwap()
    {
        currentScene?.Unload(world);
        currentScene = nextScene;
        nextScene = null;
        currentScene?.Load(world);
    }
}
```

### Scene Transitions

```csharp
public enum TransitionPhase { None, Out, In }

public abstract class SceneTransition
{
    public TransitionPhase Phase { get; private set; }
    public float Progress { get; private set; }
    public bool IsComplete => Progress >= 1f;

    protected float duration;

    protected SceneTransition(float duration)
    {
        this.duration = duration;
    }

    public void Start(TransitionPhase phase)
    {
        Phase = phase;
        Progress = 0f;
    }

    public void Update(float deltaTime)
    {
        Progress = MathF.Min(1f, Progress + deltaTime / duration);
        OnUpdate(Progress, Phase);
    }

    protected abstract void OnUpdate(float progress, TransitionPhase phase);
}

public class FadeTransition : SceneTransition
{
    private readonly World world;

    public FadeTransition(World world, float duration = 0.5f) : base(duration)
    {
        this.world = world;
    }

    protected override void OnUpdate(float progress, TransitionPhase phase)
    {
        float alpha = phase == TransitionPhase.Out ? progress : 1f - progress;
        // Set screen overlay alpha
        ref var overlay = ref world.GetSingleton<ScreenOverlay>();
        overlay.Color = new Color(0, 0, 0, alpha);
    }
}

public class SlideTransition : SceneTransition
{
    private readonly World world;
    private readonly Direction direction;

    public SlideTransition(World world, Direction direction, float duration = 0.3f) : base(duration)
    {
        this.world = world;
        this.direction = direction;
    }

    protected override void OnUpdate(float progress, TransitionPhase phase)
    {
        // Calculate slide offset based on direction and progress
        ref var camera = ref world.GetSingleton<Camera>();
        // Slide camera or UI elements
    }
}
```

### Example Scenes

```csharp
public class MainMenuScene : SceneBase
{
    public override string Name => "MainMenu";

    public override void Load(World world)
    {
        // Create UI entities
        CreateSceneEntity(world)
            .With(new UIElement { Type = UIType.Button, Text = "Play" })
            .With(new Position { X = 400, Y = 300 })
            .With(new MenuAction { Action = MenuActionType.StartGame })
            .Build();

        CreateSceneEntity(world)
            .With(new UIElement { Type = UIType.Button, Text = "Options" })
            .With(new Position { X = 400, Y = 350 })
            .With(new MenuAction { Action = MenuActionType.OpenOptions })
            .Build();

        CreateSceneEntity(world)
            .With(new UIElement { Type = UIType.Button, Text = "Quit" })
            .With(new Position { X = 400, Y = 400 })
            .With(new MenuAction { Action = MenuActionType.Quit })
            .Build();

        // Background
        CreateSceneEntity(world)
            .With(new Sprite { Texture = "menu_background" })
            .With(new Position { X = 0, Y = 0 })
            .WithTag<Background>()
            .Build();
    }
}

public class GameplayScene : SceneBase
{
    private readonly string levelName;

    public override string Name => $"Level_{levelName}";

    public GameplayScene(string levelName)
    {
        this.levelName = levelName;
    }

    public override void Load(World world)
    {
        // Load level data
        var levelData = LevelLoader.Load(levelName);

        // Create player
        var player = CreateSceneEntity(world)
            .With(new Position { X = levelData.PlayerStart.X, Y = levelData.PlayerStart.Y })
            .With(new Health { Current = 100, Max = 100 })
            .With(new PlayerInput())
            .WithTag<Player>()
            .Build();

        // Create level geometry
        foreach (var tile in levelData.Tiles)
        {
            CreateSceneEntity(world)
                .With(new Position { X = tile.X, Y = tile.Y })
                .With(new Sprite { Texture = tile.TextureName })
                .With(new TileData { Type = tile.Type })
                .Build();
        }

        // Create enemies
        foreach (var spawn in levelData.EnemySpawns)
        {
            var prefab = world.GetPrefab(spawn.PrefabName);
            var entity = world.SpawnPrefab(prefab)
                .With(new Position { X = spawn.X, Y = spawn.Y })
                .Build();
            sceneEntities.Add(entity);
        }

        // Set up camera to follow player
        ref var camera = ref world.GetSingleton<Camera>();
        camera.Target = player;
    }

    public override void Unload(World world)
    {
        // Save progress before unloading
        SaveManager.SaveCheckpoint(world);

        base.Unload(world);
    }
}
```

### Scene Persistence (Don't Destroy on Load)

```csharp
[TagComponent]
public partial struct Persistent : ITagComponent { }

public class PersistenceAwareSceneBase : SceneBase
{
    public override void Unload(World world)
    {
        var buffer = world.GetCommandBuffer();

        foreach (var entity in sceneEntities)
        {
            // Don't despawn persistent entities
            if (world.IsAlive(entity) && !world.Has<Persistent>(entity))
            {
                buffer.Despawn(entity);
            }
        }

        buffer.Execute();
        sceneEntities.Clear();
    }
}
```

## Why This Works

### Explicit Entity Ownership

Each scene tracks which entities it created:
- Clean unload: Despawn only your entities
- Persistent entities survive scene changes
- No orphaned entities

### Transition Abstraction

Separating transition from scene loading:
- Reusable transitions across scenes
- Visual continuity during load
- Async loading possible during transition

### Scene Registration

Explicit scene registration instead of reflection:
- AOT compatible
- No magic strings
- Clear dependencies

## Variations

### Additive Scenes

```csharp
public class AdditiveSceneManager
{
    private readonly List<IScene> activeScenes = new();

    public void LoadAdditive(string name)
    {
        var scene = scenes[name];
        scene.Load(world);
        activeScenes.Add(scene);
    }

    public void UnloadScene(string name)
    {
        var scene = activeScenes.Find(s => s.Name == name);
        if (scene != null)
        {
            scene.Unload(world);
            activeScenes.Remove(scene);
        }
    }
}

// Usage: Load UI scene on top of gameplay
sceneManager.LoadScene("Level1");
sceneManager.LoadAdditive("HUD");
sceneManager.LoadAdditive("PauseMenu");  // Paused state
```

### Async Scene Loading

```csharp
public class AsyncSceneLoader
{
    public async Task<IScene> LoadSceneAsync(string name, IProgress<float>? progress = null)
    {
        var scene = scenes[name];

        // Load assets in background
        var assets = GetRequiredAssets(scene);
        float totalAssets = assets.Count;
        float loaded = 0;

        foreach (var asset in assets)
        {
            await AssetManager.LoadAsync(asset);
            loaded++;
            progress?.Report(loaded / totalAssets);
        }

        return scene;
    }
}

// Usage with loading screen
public async void TransitionToScene(string name)
{
    sceneManager.LoadScene("LoadingScreen");

    var progress = new Progress<float>(p =>
    {
        ref var loading = ref world.GetSingleton<LoadingProgress>();
        loading.Percent = p;
    });

    var scene = await loader.LoadSceneAsync(name, progress);

    sceneManager.LoadSceneWithTransition(name, new FadeTransition(world));
}
```

### Scene State Serialization

```csharp
public interface ISerializableScene : IScene
{
    SceneState SaveState(World world);
    void LoadState(World world, SceneState state);
}

public class SceneState
{
    public string SceneName { get; set; } = "";
    public Dictionary<string, object> Data { get; set; } = new();
}

// Save game state
public void SaveGame()
{
    if (currentScene is ISerializableScene serializable)
    {
        var state = serializable.SaveState(world);
        SaveSystem.Save("savegame.json", state);
    }
}

// Load game state
public void LoadGame()
{
    var state = SaveSystem.Load<SceneState>("savegame.json");
    sceneManager.LoadScene(state.SceneName);

    if (currentScene is ISerializableScene serializable)
    {
        serializable.LoadState(world, state);
    }
}
```

### Scene Events

```csharp
public class SceneManager
{
    public event Action<string>? SceneLoading;
    public event Action<string>? SceneLoaded;
    public event Action<string>? SceneUnloading;
    public event Action<string>? SceneUnloaded;

    private void PerformSceneSwap()
    {
        if (currentScene != null)
        {
            SceneUnloading?.Invoke(currentScene.Name);
            currentScene.Unload(world);
            SceneUnloaded?.Invoke(currentScene.Name);
        }

        currentScene = nextScene;
        nextScene = null;

        if (currentScene != null)
        {
            SceneLoading?.Invoke(currentScene.Name);
            currentScene.Load(world);
            SceneLoaded?.Invoke(currentScene.Name);
        }
    }
}

// Subscribe to events
sceneManager.SceneLoaded += sceneName =>
{
    Analytics.TrackEvent("scene_loaded", sceneName);
};
```

### Level Streaming

```csharp
public class StreamingSceneManager
{
    private readonly Dictionary<Vector2Int, IScene> chunks = new();
    private readonly HashSet<Vector2Int> loadedChunks = new();
    private const int ChunkSize = 100;
    private const int LoadRadius = 2;

    public void Update(Vector2 playerPosition)
    {
        var playerChunk = new Vector2Int(
            (int)(playerPosition.X / ChunkSize),
            (int)(playerPosition.Y / ChunkSize)
        );

        // Determine which chunks should be loaded
        var shouldBeLoaded = new HashSet<Vector2Int>();
        for (int x = -LoadRadius; x <= LoadRadius; x++)
        {
            for (int y = -LoadRadius; y <= LoadRadius; y++)
            {
                shouldBeLoaded.Add(playerChunk + new Vector2Int(x, y));
            }
        }

        // Unload chunks that are too far
        foreach (var chunk in loadedChunks.ToList())
        {
            if (!shouldBeLoaded.Contains(chunk))
            {
                chunks[chunk].Unload(world);
                loadedChunks.Remove(chunk);
            }
        }

        // Load new chunks
        foreach (var chunk in shouldBeLoaded)
        {
            if (!loadedChunks.Contains(chunk) && chunks.ContainsKey(chunk))
            {
                chunks[chunk].Load(world);
                loadedChunks.Add(chunk);
            }
        }
    }
}
```

## See Also

- [Serialization Guide](../serialization.md) - Saving scene state
- [Prefabs Guide](../prefabs.md) - Entity templates for scenes
- [Entity Spawning](entity-spawning.md) - Creating scene entities
