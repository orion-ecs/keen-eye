using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Viewport;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// The viewport panel displays the 3D scene view and handles camera controls.
/// </summary>
/// <remarks>
/// <para>
/// The viewport provides:
/// <list type="bullet">
/// <item>3D rendering of the current scene</item>
/// <item>Camera orbit, pan, and zoom controls</item>
/// <item>Entity selection via clicking</item>
/// <item>Transform gizmo overlays (when entities are selected)</item>
/// </list>
/// </para>
/// </remarks>
public static class ViewportPanel
{
    private const float DefaultFieldOfView = 60f;
    private const float DefaultNearPlane = 0.1f;
    private const float DefaultFarPlane = 1000f;

    /// <summary>
    /// Creates the viewport panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for overlays.</param>
    /// <param name="worldManager">The world manager for scene access.</param>
    /// <param name="graphicsContext">The graphics context for rendering.</param>
    /// <param name="inputContext">The input context for camera controls.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        EditorWorldManager worldManager,
        IGraphicsContext graphicsContext,
        IInputContext inputContext)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "ViewportPanel", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(panel);
        panelRect.WidthMode = UISizeMode.Fill;
        panelRect.HeightMode = UISizeMode.Fill;

        // Create header with toolbar
        CreateHeader(editorWorld, panel, font);

        // Create the viewport content area
        var viewportArea = CreateViewportArea(editorWorld, panel);

        // Create input provider for camera control
        var inputProvider = new EditorInputProvider(inputContext);

        // Create camera controller
        var cameraController = new EditorCameraController();
        cameraController.Reset();

        // Create transform gizmo
        var transformGizmo = new TransformGizmo();

        // Store references for later updates
        editorWorld.Add(panel, new ViewportPanelState
        {
            ViewportArea = viewportArea,
            WorldManager = worldManager,
            GraphicsContext = graphicsContext,
            InputProvider = inputProvider,
            CameraController = cameraController,
            TransformGizmo = transformGizmo,
            Font = font,
            IsHovered = false,
            LastViewportSize = Vector2.Zero
        });

        // Subscribe to scene events
        worldManager.SceneOpened += scene => OnSceneOpened(editorWorld, panel, scene);
        worldManager.SceneClosed += () => OnSceneClosed(editorWorld, panel);
        worldManager.EntitySelected += entity => OnEntitySelected(editorWorld, panel, entity);
        worldManager.SelectionCleared += () => OnSelectionCleared(editorWorld, panel);

        // Subscribe to viewport pointer events for hover detection
        editorWorld.Subscribe<UIPointerEnterEvent>(e =>
        {
            if (e.Element == viewportArea && editorWorld.Has<ViewportPanelState>(panel))
            {
                ref var state = ref editorWorld.Get<ViewportPanelState>(panel);
                state.IsHovered = true;
            }
        });

        editorWorld.Subscribe<UIPointerExitEvent>(e =>
        {
            if (e.Element == viewportArea && editorWorld.Has<ViewportPanelState>(panel))
            {
                ref var state = ref editorWorld.Get<ViewportPanelState>(panel);
                state.IsHovered = false;
            }
        });

        // Subscribe to click for entity picking
        editorWorld.Subscribe<UIClickEvent>(e =>
        {
            if (e.Element == viewportArea && e.Button == MouseButton.Left)
            {
                OnViewportClick(editorWorld, panel, e.Position);
            }
        });

        return panel;
    }

    /// <summary>
    /// Updates the viewport panel each frame.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The viewport panel entity.</param>
    /// <param name="deltaTime">Time since last update.</param>
    public static void Update(IWorld editorWorld, Entity panel, float deltaTime)
    {
        if (!editorWorld.Has<ViewportPanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<ViewportPanelState>(panel);

        // Update input provider
        state.InputProvider.Update();

        // Check for gizmo mode shortcuts
        ProcessGizmoShortcuts(ref state);

        // Get viewport bounds for gizmo interaction
        var viewportBounds = Rectangle.Empty;
        if (editorWorld.Has<UIRect>(state.ViewportArea))
        {
            ref readonly var viewportRect = ref editorWorld.Get<UIRect>(state.ViewportArea);
            viewportBounds = viewportRect.ComputedBounds;
        }

        // Calculate projection matrix
        var aspectRatio = viewportBounds.Width > 0 ? viewportBounds.Width / viewportBounds.Height : 1f;
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI * DefaultFieldOfView / 180f,
            aspectRatio,
            DefaultNearPlane,
            DefaultFarPlane);

        // Process gizmo input first if we have a selection
        var gizmoConsumedInput = false;
        var sceneWorld = state.WorldManager.CurrentSceneWorld;
        if (state.IsHovered && sceneWorld is not null && state.WorldManager.SelectedEntity.IsValid)
        {
            gizmoConsumedInput = state.TransformGizmo.Update(
                state.InputProvider,
                state.CameraController,
                sceneWorld,
                state.WorldManager.SelectedEntity,
                viewportBounds,
                projectionMatrix);
        }

        // Process camera input when viewport is hovered and gizmo didn't consume input
        if (!gizmoConsumedInput)
        {
            state.CameraController.ProcessInput(state.InputProvider, deltaTime, state.IsHovered);
        }

        // Reset scroll delta after processing
        state.InputProvider.ResetScrollDelta();
    }

    private static void ProcessGizmoShortcuts(ref ViewportPanelState state)
    {
        // W = Translate, E = Rotate, R = Scale
        if (state.InputProvider.IsKeyDown(Key.W))
        {
            state.TransformGizmo.Mode = GizmoMode.Translate;
        }
        else if (state.InputProvider.IsKeyDown(Key.E))
        {
            state.TransformGizmo.Mode = GizmoMode.Rotate;
        }
        else if (state.InputProvider.IsKeyDown(Key.R))
        {
            state.TransformGizmo.Mode = GizmoMode.Scale;
        }
    }

    /// <summary>
    /// Renders the viewport scene.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The viewport panel entity.</param>
    public static void Render(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<ViewportPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ViewportPanelState>(panel);
        var sceneWorld = state.WorldManager.CurrentSceneWorld;
        var graphics = state.GraphicsContext;

        if (graphics is null || !graphics.IsInitialized)
        {
            return;
        }

        // Get viewport bounds
        if (!editorWorld.Has<UIRect>(state.ViewportArea))
        {
            return;
        }

        ref readonly var viewportRect = ref editorWorld.Get<UIRect>(state.ViewportArea);
        var viewportX = (int)viewportRect.ComputedBounds.X;
        var viewportY = (int)viewportRect.ComputedBounds.Y;
        var viewportWidth = (int)viewportRect.ComputedBounds.Width;
        var viewportHeight = (int)viewportRect.ComputedBounds.Height;

        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        // Set viewport and clear
        graphics.SetViewport(viewportX, viewportY, viewportWidth, viewportHeight);
        graphics.SetClearColor(EditorColors.ViewportBackground);
        graphics.Clear(ClearMask.ColorBuffer | ClearMask.DepthBuffer);
        graphics.SetDepthTest(true);
        graphics.SetCulling(true, CullFaceMode.Back);

        // Calculate projection matrix
        var aspectRatio = (float)viewportWidth / viewportHeight;
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI * DefaultFieldOfView / 180f,
            aspectRatio,
            DefaultNearPlane,
            DefaultFarPlane);

        // Get view matrix from camera controller
        var viewMatrix = state.CameraController.GetViewMatrix();

        // Render grid
        RenderGrid(graphics, viewMatrix, projectionMatrix);

        // Render scene entities if a scene is open
        if (sceneWorld is not null)
        {
            RenderSceneEntities(sceneWorld, graphics, viewMatrix, projectionMatrix);
        }

        // Render selection highlight and transform gizmo
        if (state.WorldManager.SelectedEntity.IsValid && sceneWorld is not null)
        {
            RenderSelectionHighlight(sceneWorld, graphics, viewMatrix, projectionMatrix, state.WorldManager.SelectedEntity);

            // Render transform gizmo
            state.TransformGizmo.Render(
                graphics,
                sceneWorld,
                state.WorldManager.SelectedEntity,
                viewMatrix,
                projectionMatrix,
                state.CameraController.Position);
        }
    }

    /// <summary>
    /// Focuses the viewport camera on a specific entity.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The viewport panel entity.</param>
    /// <param name="entity">The entity to focus on.</param>
    public static void FocusOnEntity(IWorld editorWorld, Entity panel, Entity entity)
    {
        if (!editorWorld.Has<ViewportPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ViewportPanelState>(panel);
        var sceneWorld = state.WorldManager.CurrentSceneWorld;

        if (sceneWorld is null || !sceneWorld.Has<Transform3D>(entity))
        {
            return;
        }

        ref readonly var transform = ref sceneWorld.Get<Transform3D>(entity);
        state.CameraController.FocusOn(transform.Position, 5f);
    }

    /// <summary>
    /// Sets the camera to a preset view.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The viewport panel entity.</param>
    /// <param name="preset">The view preset.</param>
    public static void SetPresetView(IWorld editorWorld, Entity panel, ViewPreset preset)
    {
        if (!editorWorld.Has<ViewportPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ViewportPanelState>(panel);
        state.CameraController.SetPresetView(preset);
    }

    private static void CreateHeader(IWorld world, Entity panel, FontHandle font)
    {
        var header = WidgetFactory.CreatePanel(world, panel, "ViewportHeader", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref world.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(world, header, "ViewportTitle", "Scene", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        // Toolbar area for camera mode buttons (future enhancement)
        var toolbar = WidgetFactory.CreatePanel(world, header, "ViewportToolbar", new PanelConfig(
            Direction: LayoutDirection.Horizontal,
            Spacing: 4,
            BackgroundColor: Vector4.Zero
        ));

        // Camera mode indicator label
        WidgetFactory.CreateLabel(world, toolbar, "CameraModeLabel", "Orbit", font, new LabelConfig(
            FontSize: 11,
            TextColor: EditorColors.TextLight,
            HorizontalAlign: TextAlignH.Right
        ));
    }

    private static Entity CreateViewportArea(IWorld world, Entity panel)
    {
        var viewportArea = WidgetFactory.CreatePanel(world, panel, "ViewportArea", new PanelConfig(
            BackgroundColor: EditorColors.ViewportBackground
        ));

        ref var viewportRect = ref world.Get<UIRect>(viewportArea);
        viewportRect.WidthMode = UISizeMode.Fill;
        viewportRect.HeightMode = UISizeMode.Fill;

        // Mark as interactive for pointer events
        world.Add(viewportArea, UIInteractable.Clickable());

        return viewportArea;
    }

    private static void RenderGrid(IGraphicsContext graphics, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        // Use solid shader for grid lines
        graphics.BindShader(graphics.SolidShader);
        graphics.SetUniform("uView", viewMatrix);
        graphics.SetUniform("uProjection", projectionMatrix);

        // Grid rendering would require line drawing support
        // For now, this is a placeholder for future implementation
        // The actual grid would draw lines in the XZ plane
    }

    private static void RenderSceneEntities(
        World sceneWorld,
        IGraphicsContext graphics,
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix)
    {
        // Query entities with Transform3D and Renderable components
        foreach (var entity in sceneWorld.Query<Transform3D, Renderable>())
        {
            ref readonly var transform = ref sceneWorld.Get<Transform3D>(entity);
            ref readonly var renderable = ref sceneWorld.Get<Renderable>(entity);

            // Calculate model matrix
            var modelMatrix = transform.Matrix();

            // Bind shader - for now use unlit shader
            graphics.BindShader(graphics.UnlitShader);
            graphics.SetUniform("uModel", modelMatrix);
            graphics.SetUniform("uView", viewMatrix);
            graphics.SetUniform("uProjection", projectionMatrix);

            // Bind white texture as default
            graphics.BindTexture(graphics.WhiteTexture);

            // Draw mesh if valid
            if (renderable.MeshId > 0)
            {
                var meshHandle = new MeshHandle(renderable.MeshId);
                graphics.BindMesh(meshHandle);
                graphics.DrawMesh(meshHandle);
            }
        }
    }

    private static void RenderSelectionHighlight(
        World sceneWorld,
        IGraphicsContext graphics,
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix,
        Entity selectedEntity)
    {
        if (!sceneWorld.Has<Transform3D>(selectedEntity))
        {
            return;
        }

        // Selection highlight rendering will be implemented with TransformGizmo
        // For now, this is a placeholder
    }

    private static void OnSceneOpened(IWorld editorWorld, Entity panel, World scene)
    {
        if (!editorWorld.Has<ViewportPanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<ViewportPanelState>(panel);

        // Reset camera to default position when opening a new scene
        state.CameraController.Reset();
    }

    private static void OnSceneClosed(IWorld editorWorld, Entity panel)
    {
        // Scene closed - nothing specific to do for viewport
    }

    private static void OnEntitySelected(IWorld editorWorld, Entity panel, Entity entity)
    {
        // Could implement auto-focus on selected entity here
        // For now, just update state if needed
    }

    private static void OnSelectionCleared(IWorld editorWorld, Entity panel)
    {
        // Selection cleared - nothing specific to do for viewport
    }

    private static void OnViewportClick(IWorld editorWorld, Entity panel, Vector2 clickPosition)
    {
        if (!editorWorld.Has<ViewportPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ViewportPanelState>(panel);
        var sceneWorld = state.WorldManager.CurrentSceneWorld;

        if (sceneWorld is null)
        {
            return;
        }

        // Get viewport bounds
        if (!editorWorld.Has<UIRect>(state.ViewportArea))
        {
            return;
        }

        ref readonly var viewportRect = ref editorWorld.Get<UIRect>(state.ViewportArea);

        // Convert click position to viewport-relative coordinates
        var localX = clickPosition.X - viewportRect.ComputedBounds.X;
        var localY = clickPosition.Y - viewportRect.ComputedBounds.Y;

        // Normalize to 0-1 range
        var normalizedX = localX / viewportRect.ComputedBounds.Width;
        var normalizedY = localY / viewportRect.ComputedBounds.Height;

        // Perform raycast for entity picking
        var pickedEntity = PickEntity(state, sceneWorld, normalizedX, normalizedY);

        if (pickedEntity.IsValid)
        {
            state.WorldManager.Select(pickedEntity);
        }
        else
        {
            state.WorldManager.ClearSelection();
        }
    }

    private static Entity PickEntity(
        in ViewportPanelState state,
        World sceneWorld,
        float normalizedX,
        float normalizedY)
    {
        // Create ray from camera through screen point
        var cameraPos = state.CameraController.Position;

        // Calculate ray direction from normalized screen coordinates
        var rayDirection = CalculateRayDirection(
            state.CameraController,
            normalizedX,
            normalizedY);

        // Check intersection with scene entities
        Entity closestEntity = Entity.Null;
        float closestDistance = float.MaxValue;

        foreach (var entity in sceneWorld.Query<Transform3D>())
        {
            ref readonly var transform = ref sceneWorld.Get<Transform3D>(entity);

            // Simple sphere-based picking (assumes unit sphere around transform origin)
            var entityPos = transform.Position;
            var toEntity = entityPos - cameraPos;
            var distance = Vector3.Dot(toEntity, rayDirection);

            if (distance > 0)
            {
                var closestPoint = cameraPos + rayDirection * distance;
                var distanceToCenter = Vector3.Distance(closestPoint, entityPos);

                // Check if ray passes within a reasonable distance of the entity center
                // Using a fixed picking radius for now
                const float pickRadius = 1f;
                if (distanceToCenter < pickRadius && distance < closestDistance)
                {
                    closestEntity = entity;
                    closestDistance = distance;
                }
            }
        }

        return closestEntity;
    }

    private static Vector3 CalculateRayDirection(
        EditorCameraController camera,
        float normalizedX,
        float normalizedY)
    {
        // Convert normalized coordinates to clip space (-1 to 1)
        var clipX = normalizedX * 2f - 1f;
        var clipY = 1f - normalizedY * 2f; // Flip Y

        // Use camera orientation to calculate ray direction
        var forward = camera.Forward;
        var right = camera.Right;
        var up = camera.Up;

        // Calculate field of view adjustment
        var fovRad = MathF.PI * DefaultFieldOfView / 180f;
        var tanHalfFov = MathF.Tan(fovRad * 0.5f);

        // Calculate ray direction in world space
        var rayDir = Vector3.Normalize(
            forward +
            right * (clipX * tanHalfFov) +
            up * (clipY * tanHalfFov));

        return rayDir;
    }
}

/// <summary>
/// Component storing the state of the viewport panel.
/// </summary>
internal struct ViewportPanelState : IComponent
{
    public Entity ViewportArea;
    public EditorWorldManager WorldManager;
    public IGraphicsContext GraphicsContext;
    public EditorInputProvider InputProvider;
    public EditorCameraController CameraController;
    public TransformGizmo TransformGizmo;
    public FontHandle Font;
    public bool IsHovered;
    public Vector2 LastViewportSize;
}
