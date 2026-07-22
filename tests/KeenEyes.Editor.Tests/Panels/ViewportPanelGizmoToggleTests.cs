// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Panels;

namespace KeenEyes.Editor.Tests.Panels;

/// <summary>
/// Tests for the per-renderer gizmo visibility toggles surfaced in the viewport header.
/// </summary>
public sealed class ViewportPanelGizmoToggleTests
{
    [Fact]
    public void AddGizmoToggle_WithRenderer_CreatesToggleMappedToRenderer()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("skeleton-gizmo");

        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);

        ref readonly var state = ref world.Get<ViewportPanelState>(panel);
        var pair = Assert.Single(state.GizmoToggles);
        Assert.True(world.IsAlive(pair.Key));
        Assert.Same(renderer, pair.Value);
    }

    [Fact]
    public void AddGizmoToggle_WithSameRendererTwice_CreatesSingleToggle()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("skeleton-gizmo");

        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);
        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);

        ref readonly var state = ref world.Get<ViewportPanelState>(panel);
        Assert.Single(state.GizmoToggles);
    }

    [Fact]
    public void HandleGizmoToggleClick_OnToggleEntity_DisablesRenderer()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("ik-gizmo");
        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);
        var toggle = GetToggleEntity(world, panel);

        ViewportPanel.HandleGizmoToggleClick(world, panel, toggle);

        Assert.False(renderer.IsEnabled);
    }

    [Fact]
    public void HandleGizmoToggleClick_Twice_RestoresEnabledState()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("ik-gizmo");
        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);
        var toggle = GetToggleEntity(world, panel);

        ViewportPanel.HandleGizmoToggleClick(world, panel, toggle);
        ViewportPanel.HandleGizmoToggleClick(world, panel, toggle);

        Assert.True(renderer.IsEnabled);
    }

    [Fact]
    public void HandleGizmoToggleClick_OnUnrelatedEntity_DoesNotChangeRenderer()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("navmesh-visualizer");
        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);
        var unrelated = world.Spawn().Build();

        ViewportPanel.HandleGizmoToggleClick(world, panel, unrelated);

        Assert.True(renderer.IsEnabled);
    }

    [Fact]
    public void RemoveGizmoToggle_WithExistingToggle_DespawnsToggleEntity()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("skeleton-gizmo");
        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);
        var toggle = GetToggleEntity(world, panel);

        ViewportPanel.RemoveGizmoToggle(world, panel, renderer);

        Assert.False(world.IsAlive(toggle));
        ref readonly var state = ref world.Get<ViewportPanelState>(panel);
        Assert.Empty(state.GizmoToggles);
    }

    [Fact]
    public void RemoveGizmoToggle_WithUnknownRenderer_LeavesExistingToggles()
    {
        using var world = new World();
        var panel = CreatePanelWithState(world);
        var renderer = new FakeGizmoRenderer("skeleton-gizmo");
        ViewportPanel.AddGizmoToggle(world, panel, renderer, default);

        ViewportPanel.RemoveGizmoToggle(world, panel, new FakeGizmoRenderer("other"));

        ref readonly var state = ref world.Get<ViewportPanelState>(panel);
        Assert.Single(state.GizmoToggles);
    }

    #region Test Helpers

    private static Entity CreatePanelWithState(World world)
    {
        var container = world.Spawn().Build();
        var panel = world.Spawn().Build();

        world.Add(panel, new ViewportPanelState
        {
            GizmoToggleContainer = container,
            GizmoToggles = []
        });

        return panel;
    }

    private static Entity GetToggleEntity(World world, Entity panel)
    {
        ref readonly var state = ref world.Get<ViewportPanelState>(panel);
        return state.GizmoToggles.Keys.Single();
    }

    private sealed class FakeGizmoRenderer(string id) : IGizmoRenderer
    {
        public string Id { get; } = id;

        public string DisplayName => Id;

        public bool IsEnabled { get; set; } = true;

        public int Order => 0;

        public void Render(GizmoRenderContext context)
        {
        }

        public bool ShouldRender(Entity entity, IWorld sceneWorld) => true;
    }

    #endregion
}
