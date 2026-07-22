// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Panels;
using KeenEyes.Editor.Plugins.Capabilities;
using KeenEyes.Editor.Viewport;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;
using KeenEyes.Testing.Input;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.Editor.Tests.Application;

/// <summary>
/// Tests for the editor's per-frame composition order: framebuffer clear, then the
/// 3D viewport pass (scene content followed by the gizmo pass), then the UI pass.
/// The ordering is verified against a recording mock graphics context shared with
/// a recording 2D renderer so cross-pass ordering is observable in a single log.
/// </summary>
public sealed class EditorFrameCompositionTests
{
    private const int SceneMeshId = 777;

    #region Frame Ordering Tests

    [Fact]
    public void ComposeFrame_WithOpenSceneAndGizmoRenderer_DrawsSceneThenGizmosThenUi()
    {
        using var fixture = new FrameFixture();

        EditorApplication.ComposeFrame(fixture.Graphics, fixture.EditorWorld, fixture.Panel, 0.016f);

        var log = fixture.Graphics.CallLog;
        var sceneDraw = log.IndexOf($"DrawMesh({SceneMeshId})");
        var gizmoDraw = log.IndexOf($"DrawMesh({fixture.GizmoCubeMeshId})");
        var firstUiDraw = log.FindIndex(entry => entry.StartsWith("UI:", StringComparison.Ordinal));

        Assert.True(sceneDraw >= 0, "Scene mesh draw call was not recorded");
        Assert.True(gizmoDraw > sceneDraw, "Gizmo pass did not execute after scene content");
        Assert.True(firstUiDraw > gizmoDraw, "UI pass did not execute after the gizmo pass");
    }

    [Fact]
    public void ComposeFrame_WithOpenScene_DoesNotClearAfterUiDrawCalls()
    {
        using var fixture = new FrameFixture();

        EditorApplication.ComposeFrame(fixture.Graphics, fixture.EditorWorld, fixture.Panel, 0.016f);

        var log = fixture.Graphics.CallLog;
        var lastClear = log.FindLastIndex(entry => entry.StartsWith("Clear(", StringComparison.Ordinal));
        var firstUiDraw = log.FindIndex(entry => entry.StartsWith("UI:", StringComparison.Ordinal));

        Assert.True(lastClear >= 0, "No framebuffer clear was recorded");
        Assert.True(firstUiDraw >= 0, "No UI draw calls were recorded");
        Assert.True(lastClear < firstUiDraw, "The framebuffer was cleared after UI draw calls");
    }

    [Fact]
    public void ComposeFrame_AfterViewportPass_RestoresFullWindowStateBeforeUiPass()
    {
        using var fixture = new FrameFixture();

        EditorApplication.ComposeFrame(fixture.Graphics, fixture.EditorWorld, fixture.Panel, 0.016f);

        var log = fixture.Graphics.CallLog;
        var gizmoDraw = log.IndexOf($"DrawMesh({fixture.GizmoCubeMeshId})");
        var restoreViewport = log.LastIndexOf("SetViewport(0, 0, 800, 600)");
        var firstUiDraw = log.FindIndex(entry => entry.StartsWith("UI:", StringComparison.Ordinal));

        Assert.True(restoreViewport > gizmoDraw, "Full-window viewport was not restored after the gizmo pass");
        Assert.True(restoreViewport < firstUiDraw, "Full-window viewport was not restored before the UI pass");
        Assert.False(fixture.Graphics.RenderState.DepthTestEnabled);
        Assert.False(fixture.Graphics.RenderState.CullingEnabled);
    }

    [Fact]
    public void ComposeFrame_WithoutViewportPanel_ClearsOnceBeforeUiPass()
    {
        using var fixture = new FrameFixture(createViewportPanel: false);

        EditorApplication.ComposeFrame(fixture.Graphics, fixture.EditorWorld, Entity.Null, 0.016f);

        var log = fixture.Graphics.CallLog;
        var clears = log.Where(entry => entry.StartsWith("Clear(", StringComparison.Ordinal)).ToList();
        var firstUiDraw = log.FindIndex(entry => entry.StartsWith("UI:", StringComparison.Ordinal));

        Assert.Single(clears);
        Assert.True(firstUiDraw > log.FindIndex(entry => entry.StartsWith("Clear(", StringComparison.Ordinal)));
        Assert.DoesNotContain(log, entry => entry.StartsWith("DrawMesh(", StringComparison.Ordinal));
    }

    #endregion

    #region Viewport Area Compositing Tests

    [Fact]
    public void Create_ViewportArea_HasTransparentBackground()
    {
        using var editorWorld = new World();
        using var worldManager = new EditorWorldManager();
        using var graphics = new MockGraphicsContext();
        var inputContext = new MockInputContext();
        var capability = new ViewportCapability();
        var root = editorWorld.Spawn().Build();

        var panel = ViewportPanel.Create(
            editorWorld, root, default, worldManager, graphics, inputContext, capability);

        ref readonly var state = ref editorWorld.Get<ViewportPanelState>(panel);
        ref readonly var style = ref editorWorld.Get<UIStyle>(state.ViewportArea);
        Assert.True(
            style.BackgroundColor.W.IsApproximatelyZero(),
            "Viewport area background must be transparent so the scene pass beneath the UI stays visible");
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Builds a headless editor frame: an editor world running <see cref="UIRenderSystem"/>
    /// with a recording 2D renderer, a UI canvas with an opaque panel, and a viewport panel
    /// whose scene world contains one renderable entity and one capability gizmo renderer.
    /// All draw and state calls are recorded, in order, into the mock graphics call log.
    /// </summary>
    private sealed class FrameFixture : IDisposable
    {
        public World EditorWorld { get; }

        public EditorWorldManager WorldManager { get; }

        public MockGraphicsContext Graphics { get; }

        public Entity Panel { get; }

        public int GizmoCubeMeshId =>
            Graphics.Meshes.Single(pair => pair.Value.IsCube).Key.Id;

        public FrameFixture(bool createViewportPanel = true)
        {
            EditorWorld = new World();
            WorldManager = new EditorWorldManager();
            Graphics = new MockGraphicsContext();

            // UI pass: render system plus a recording renderer that appends into the
            // same ordered log as the graphics context.
            EditorWorld.SetExtension<I2DRenderer>(new RecordingUiRenderer(Graphics.CallLog));
            EditorWorld.AddSystem(new UIRenderSystem());
            CreateUiCanvas();

            if (!createViewportPanel)
            {
                Panel = Entity.Null;
                return;
            }

            // Scene world with one renderable entity so the scene pass emits a draw call.
            WorldManager.NewScene();
            var cube = WorldManager.CreateEntity("Cube");
            WorldManager.World.Add(cube, Transform3D.Identity);
            WorldManager.World.Add(cube, new Renderable(SceneMeshId, 0));

            // Capability with one gizmo renderer that draws through the real drawer.
            var capability = new ViewportCapability();
            capability.AddGizmoRenderer(new LineGizmoRenderer());

            Panel = CreateViewportPanelEntity(capability);
        }

        private void CreateUiCanvas()
        {
            var canvas = EditorWorld.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Stretch())
                .With(new UIRootTag())
                .With(new UIStyle { BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f) })
                .Build();

            ref var rect = ref EditorWorld.Get<UIRect>(canvas);
            rect.ComputedBounds = new Rectangle(0, 0, 800, 600);
        }

        private Entity CreateViewportPanelEntity(ViewportCapability capability)
        {
            var viewportArea = EditorWorld.Spawn()
                .With(new UIRect { ComputedBounds = new Rectangle(10, 10, 400, 300) })
                .Build();

            var cameraController = new EditorCameraController();
            cameraController.Reset();

            var panel = EditorWorld.Spawn().Build();
            EditorWorld.Add(panel, new ViewportPanelState
            {
                ViewportArea = viewportArea,
                WorldManager = WorldManager,
                GraphicsContext = Graphics,
                InputProvider = new EditorInputProvider(new MockInputContext()),
                CameraController = cameraController,
                TransformGizmo = new TransformGizmo(),
                Capability = capability,
                GizmoDrawer = new ViewportGizmoDrawer(),
                GizmoToggleContainer = Entity.Null,
                GizmoToggles = [],
                Font = default,
                IsHovered = false,
                LastViewportSize = Vector2.Zero
            });

            return panel;
        }

        public void Dispose()
        {
            WorldManager.Dispose();
            EditorWorld.Dispose();
            Graphics.Dispose();
        }
    }

    private sealed class LineGizmoRenderer : IGizmoRenderer
    {
        public string Id => "test-line-gizmo";

        public string DisplayName => "Test Line Gizmo";

        public bool IsEnabled { get; set; } = true;

        public int Order => 0;

        public void Render(GizmoRenderContext context)
            => context.Drawer.DrawLine(Vector3.Zero, Vector3.UnitX, new Vector4(1f, 0f, 0f, 1f));

        public bool ShouldRender(Entity entity, IWorld sceneWorld) => true;
    }

    /// <summary>
    /// An <see cref="I2DRenderer"/> that appends UI draw operations into the shared
    /// ordered call log so their ordering relative to graphics calls can be asserted.
    /// </summary>
    private sealed class RecordingUiRenderer(List<string> callLog) : I2DRenderer
    {
        public void Begin() => callLog.Add("UI:Begin");

        public void Begin(in Matrix4x4 projection) => callLog.Add("UI:Begin");

        public void End() => callLog.Add("UI:End");

        public void Flush()
        {
        }

        public void FillRect(float x, float y, float width, float height, Vector4 color)
            => callLog.Add("UI:FillRect");

        public void FillRect(in Rectangle rect, Vector4 color)
            => callLog.Add("UI:FillRect");

        public void DrawRect(float x, float y, float width, float height, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawRect");

        public void DrawRect(in Rectangle rect, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawRect");

        public void FillRoundedRect(float x, float y, float width, float height, float radius, Vector4 color)
            => callLog.Add("UI:FillRoundedRect");

        public void DrawRoundedRect(float x, float y, float width, float height, float radius, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawRoundedRect");

        public void DrawLine(float x1, float y1, float x2, float y2, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawLine");

        public void DrawLine(Vector2 start, Vector2 end, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawLine");

        public void DrawLineStrip(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawLineStrip");

        public void DrawPolygon(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f)
            => callLog.Add("UI:DrawPolygon");

        public void FillCircle(float centerX, float centerY, float radius, Vector4 color, int segments = 32)
            => callLog.Add("UI:FillCircle");

        public void DrawCircle(float centerX, float centerY, float radius, Vector4 color, float thickness = 1f, int segments = 32)
            => callLog.Add("UI:DrawCircle");

        public void FillEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, int segments = 32)
            => callLog.Add("UI:FillEllipse");

        public void DrawEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, float thickness = 1f, int segments = 32)
            => callLog.Add("UI:DrawEllipse");

        public void DrawTexture(TextureHandle texture, float x, float y, Vector4? tint = null)
            => callLog.Add("UI:DrawTexture");

        public void DrawTexture(TextureHandle texture, float x, float y, float width, float height, Vector4? tint = null)
            => callLog.Add("UI:DrawTexture");

        public void DrawTextureRegion(TextureHandle texture, in Rectangle destRect, in Rectangle sourceRect, Vector4? tint = null)
            => callLog.Add("UI:DrawTextureRegion");

        public void DrawTextureRotated(TextureHandle texture, in Rectangle destRect, float rotation, Vector2 origin, Vector4? tint = null)
            => callLog.Add("UI:DrawTextureRotated");

        public void DrawTextureRotated(TextureHandle texture, in Rectangle destRect, in Rectangle sourceRect, float rotation, Vector2 origin, Vector4? tint = null)
            => callLog.Add("UI:DrawTextureRotated");

        public void PushClip(in Rectangle rect)
        {
        }

        public void PopClip()
        {
        }

        public void ClearClip()
        {
        }

        public void SetBatchHint(int count)
        {
        }

        public void Dispose()
        {
        }
    }

    #endregion
}
