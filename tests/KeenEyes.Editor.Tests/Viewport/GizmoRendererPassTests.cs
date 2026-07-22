// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;

using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Assets;
using KeenEyes.Editor.Commands;
using KeenEyes.Editor.Navigation;
using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.BuiltIn;
using KeenEyes.Editor.Plugins.Capabilities;
using KeenEyes.Editor.Selection;
using KeenEyes.Editor.Viewport;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Editor.Tests.Viewport;

/// <summary>
/// Tests for the viewport gizmo render pass that enumerates capability-registered
/// gizmo renderers each frame.
/// </summary>
public sealed class GizmoRendererPassTests : IDisposable
{
    private readonly World editorWorld = new();
    private readonly EditorWorldManager worldManager = new();
    private readonly SelectionManager selection = new();
    private readonly UndoRedoManager undoRedo = new();
    private readonly AssetDatabase assets;
    private readonly DirectoryInfo tempProjectRoot;

    public GizmoRendererPassTests()
    {
        tempProjectRoot = Directory.CreateTempSubdirectory("keeneyes-gizmo-pass-");
        assets = new AssetDatabase(tempProjectRoot.FullName);
    }

    public void Dispose()
    {
        assets.Dispose();
        worldManager.Dispose();
        editorWorld.Dispose();

        try
        {
            tempProjectRoot.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of the temporary project directory.
        }
    }

    #region Built-In Renderer Tests

    [Fact]
    public void Render_WithBuiltInPluginsAndIKNavMeshContent_RecordsBuiltInDrawCalls()
    {
        using var manager = CreateBootEquivalentManager(out var viewport);
        BuiltInGizmoPlugins.Install(manager);

        // Scene world containing a skeleton with an IK chain and target
        using var sceneWorld = CreateIKWorld(new Vector3(1.5f, 0f, 0f));

        // Give the navmesh visualizer a baked navmesh so it has content to draw
        var visualizer = Assert.IsType<NavMeshVisualizer>(
            viewport.GetGizmoRenderers().Single(r => r.Id == "navmesh-visualizer"));
        visualizer.SetNavMesh(BuildTestNavMesh());

        var drawer = new RecordingGizmoDrawer();

        GizmoRendererPass.Render(viewport.GetGizmoRenderers(), CreateContext(sceneWorld, drawer));

        // NavMesh surfaces arrive as triangles, skeleton bones as points and wire
        // spheres, and bone/IK chain connections as lines.
        Assert.NotEmpty(drawer.Triangles);
        Assert.NotEmpty(drawer.Points);
        Assert.NotEmpty(drawer.Spheres);
        Assert.NotEmpty(drawer.Lines);
    }

    [Fact]
    public void Render_WithBuiltInPluginsAndEmptyScene_RecordsNoDrawCalls()
    {
        using var manager = CreateBootEquivalentManager(out var viewport);
        BuiltInGizmoPlugins.Install(manager);

        using var sceneWorld = new World();
        sceneWorld.Spawn().With(Transform3D.Identity).Build();

        var drawer = new RecordingGizmoDrawer();

        GizmoRendererPass.Render(viewport.GetGizmoRenderers(), CreateContext(sceneWorld, drawer));

        Assert.Empty(drawer.Triangles);
        Assert.Empty(drawer.Points);
        Assert.Empty(drawer.Spheres);
        Assert.Empty(drawer.Lines);
    }

    #endregion

    #region Enumeration Behavior Tests

    [Fact]
    public void Render_WithEnabledMatchingRenderer_InvokesRenderer()
    {
        using var sceneWorld = new World();
        sceneWorld.Spawn().With(Transform3D.Identity).Build();

        var renderer = new FakeGizmoRenderer("fake", order: 0);

        GizmoRendererPass.Render([renderer], CreateContext(sceneWorld, new RecordingGizmoDrawer()));

        Assert.Equal(1, renderer.RenderCount);
    }

    [Fact]
    public void Render_WithDisabledRenderer_SkipsRenderer()
    {
        using var sceneWorld = new World();
        sceneWorld.Spawn().With(Transform3D.Identity).Build();

        var renderer = new FakeGizmoRenderer("fake", order: 0) { IsEnabled = false };

        GizmoRendererPass.Render([renderer], CreateContext(sceneWorld, new RecordingGizmoDrawer()));

        Assert.Equal(0, renderer.RenderCount);
    }

    [Fact]
    public void Render_WithNoMatchingEntity_SkipsRenderer()
    {
        using var sceneWorld = new World();
        sceneWorld.Spawn().With(Transform3D.Identity).Build();

        var renderer = new FakeGizmoRenderer("fake", order: 0) { ShouldRenderResult = false };

        GizmoRendererPass.Render([renderer], CreateContext(sceneWorld, new RecordingGizmoDrawer()));

        Assert.Equal(0, renderer.RenderCount);
    }

    [Fact]
    public void Render_WithMultipleRenderers_InvokesInAscendingOrder()
    {
        using var sceneWorld = new World();
        sceneWorld.Spawn().With(Transform3D.Identity).Build();

        var callOrder = new List<string>();
        var late = new FakeGizmoRenderer("late", order: 100) { CallLog = callOrder };
        var early = new FakeGizmoRenderer("early", order: -5) { CallLog = callOrder };

        GizmoRendererPass.Render([late, early], CreateContext(sceneWorld, new RecordingGizmoDrawer()));

        Assert.Equal(["early", "late"], callOrder);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Builds a plugin host wired the same way <see cref="EditorApplication"/> wires it at
    /// startup: a manager backed by the editor services with the viewport capability registered.
    /// </summary>
    private EditorPluginManager CreateBootEquivalentManager(out ViewportCapability viewport)
    {
        var manager = new EditorPluginManager(worldManager, selection, undoRedo, assets, editorWorld, logProvider: null);
        viewport = new ViewportCapability();
        manager.RegisterCapability<IViewportCapability>(viewport);
        return manager;
    }

    /// <summary>
    /// Builds a world with the AnimationPlugin (IK enabled), a three-bone arm chain whose
    /// end effector references a target placed at <paramref name="targetPosition"/>.
    /// </summary>
    private static World CreateIKWorld(Vector3 targetPosition)
    {
        var world = new World();
        world.InstallPlugin(new AnimationPlugin(new AnimationConfig { EnableIK = true }));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(IKRig.Default)
            .Build();

        var upper = CreateBone(world, "Upper", root, Vector3.Zero, root.Id);
        var lower = CreateBone(world, "Lower", upper, Vector3.UnitX, root.Id);
        var hand = CreateBone(world, "Hand", lower, Vector3.UnitX, root.Id);

        var targetEntity = world.Spawn()
            .With(IKTarget.AtPosition(targetPosition))
            .With(new Transform3D(targetPosition, Quaternion.Identity, Vector3.One))
            .Build();

        var manager = world.GetExtension<IKManager>();
        var chainId = manager.RegisterChain(
            IKChainDefinition.TwoBone("Arm", "Upper", "Lower", "Hand", Vector3.UnitZ));

        world.Add(hand, IKChainReference.ForChain(chainId, targetEntity.Id));

        return world;
    }

    private static Entity CreateBone(World world, string name, Entity parent, Vector3 localPosition, int skeletonRootId)
    {
        var bone = world.Spawn()
            .With(new Transform3D(localPosition, Quaternion.Identity, Vector3.One))
            .With(BoneReference.Create(name, skeletonRootId))
            .Build();

        world.SetParent(bone, parent);
        return bone;
    }

    /// <summary>
    /// Bakes a small box navmesh with settings sized for test geometry.
    /// </summary>
    private static NavMeshData BuildTestNavMesh()
    {
        var config = new NavMeshConfig
        {
            CellSize = 0.3f,
            CellHeight = 0.2f,
            AgentHeight = 2.0f,
            AgentRadius = 0.6f,
            MaxClimbHeight = 0.9f,
            MaxSlopeAngle = 45.0f,
            MinRegionArea = 8,
            MergeRegionArea = 20,
            MaxEdgeLength = 12,
            MaxSimplificationError = 1.3f,
            MaxVertsPerPoly = 6,
            DetailSampleDistance = 6.0f,
            DetailSampleMaxError = 1.0f,
            UseTiles = false
        };

        var builder = new DotRecastMeshBuilder(config);
        return builder.BuildBox(new Vector3(0f, 0f, 0f), new Vector3(20f, 0.5f, 20f));
    }

    private static GizmoRenderContext CreateContext(World sceneWorld, IGizmoDrawer drawer) => new()
    {
        SceneWorld = sceneWorld,
        SelectedEntities = [],
        ViewMatrix = Matrix4x4.Identity,
        ProjectionMatrix = Matrix4x4.Identity,
        CameraPosition = Vector3.Zero,
        Bounds = new ViewportBounds(0f, 0f, 800f, 600f),
        Drawer = drawer,
    };

    private sealed class FakeGizmoRenderer(string id, int order) : IGizmoRenderer
    {
        public string Id { get; } = id;

        public string DisplayName => Id;

        public bool IsEnabled { get; set; } = true;

        public int Order { get; } = order;

        public bool ShouldRenderResult { get; init; } = true;

        public int RenderCount { get; private set; }

        public List<string>? CallLog { get; init; }

        public void Render(GizmoRenderContext context)
        {
            RenderCount++;
            CallLog?.Add(Id);
        }

        public bool ShouldRender(Entity entity, IWorld sceneWorld) => ShouldRenderResult;
    }

    private sealed class RecordingGizmoDrawer : IGizmoDrawer
    {
        public List<(Vector3 V0, Vector3 V1, Vector3 V2, Vector4 Color)> Triangles { get; } = [];

        public List<(Vector3 Start, Vector3 End, Vector4 Color)> Lines { get; } = [];

        public List<(Vector3 Position, Vector4 Color)> Points { get; } = [];

        public List<(Vector3 Center, float Radius, Vector4 Color)> Spheres { get; } = [];

        public void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector4 color)
            => Triangles.Add((v0, v1, v2, color));

        public void DrawLine(Vector3 start, Vector3 end, Vector4 color, float width = 1f)
            => Lines.Add((start, end, color));

        public void DrawPoint(Vector3 position, Vector4 color, float size = 4f)
            => Points.Add((position, color));

        public void DrawWireBox(Vector3 min, Vector3 max, Vector4 color, float lineWidth = 1f)
        {
        }

        public void DrawWireSphere(Vector3 center, float radius, Vector4 color, int segments = 16)
            => Spheres.Add((center, radius, color));

        public void DrawText(Vector3 position, string text, Vector4 color)
        {
        }
    }

    #endregion
}
