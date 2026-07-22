// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Plugins.Capabilities;

namespace KeenEyes.Editor.Tests.Plugins.Capabilities;

/// <summary>
/// Tests for <see cref="ViewportCapability"/>, the registry the editor exposes to plugins
/// for viewport gizmo renderers, overlays, and pick handlers.
/// </summary>
public sealed class ViewportCapabilityTests
{
    #region Gizmo Renderer Tests

    [Fact]
    public void AddGizmoRenderer_RegistersRendererAndRaisesEvent()
    {
        var capability = new ViewportCapability();
        var renderer = new FakeGizmoRenderer("a");
        IGizmoRenderer? added = null;
        capability.GizmoRendererAdded += r => added = r;

        capability.AddGizmoRenderer(renderer);

        Assert.Same(renderer, Assert.Single(capability.GetGizmoRenderers()));
        Assert.Same(renderer, added);
    }

    [Fact]
    public void AddGizmoRenderer_SameRendererTwice_RegistersOnce()
    {
        var capability = new ViewportCapability();
        var renderer = new FakeGizmoRenderer("a");
        var addedCount = 0;
        capability.GizmoRendererAdded += _ => addedCount++;

        capability.AddGizmoRenderer(renderer);
        capability.AddGizmoRenderer(renderer);

        Assert.Single(capability.GetGizmoRenderers());
        Assert.Equal(1, addedCount);
    }

    [Fact]
    public void RemoveGizmoRenderer_RemovesRendererAndRaisesEvent()
    {
        var capability = new ViewportCapability();
        var renderer = new FakeGizmoRenderer("a");
        capability.AddGizmoRenderer(renderer);
        IGizmoRenderer? removed = null;
        capability.GizmoRendererRemoved += r => removed = r;

        capability.RemoveGizmoRenderer(renderer);

        Assert.Empty(capability.GetGizmoRenderers());
        Assert.Same(renderer, removed);
    }

    [Fact]
    public void RemoveGizmoRenderer_NotRegistered_DoesNotRaiseEvent()
    {
        var capability = new ViewportCapability();
        var removedCount = 0;
        capability.GizmoRendererRemoved += _ => removedCount++;

        capability.RemoveGizmoRenderer(new FakeGizmoRenderer("a"));

        Assert.Equal(0, removedCount);
    }

    [Fact]
    public void GetGizmoRenderers_ReturnsSnapshotSafeToMutateDuring()
    {
        var capability = new ViewportCapability();
        capability.AddGizmoRenderer(new FakeGizmoRenderer("a"));
        capability.AddGizmoRenderer(new FakeGizmoRenderer("b"));

        // Enumerating the snapshot while mutating the registry must not throw.
        foreach (var renderer in capability.GetGizmoRenderers())
        {
            capability.RemoveGizmoRenderer(renderer);
        }

        Assert.Empty(capability.GetGizmoRenderers());
    }

    #endregion

    #region Overlay Tests

    [Fact]
    public void AddOverlay_ThenSetVisible_TogglesOverlayVisibility()
    {
        var capability = new ViewportCapability();
        var overlay = new FakeOverlay { IsVisible = false };

        capability.AddOverlay("grid", overlay);
        capability.SetOverlayVisible("grid", true);

        Assert.True(capability.IsOverlayVisible("grid"));
        Assert.Contains("grid", capability.GetOverlayIds());
    }

    [Fact]
    public void RemoveOverlay_RemovesRegisteredOverlay()
    {
        var capability = new ViewportCapability();
        capability.AddOverlay("grid", new FakeOverlay());

        Assert.True(capability.RemoveOverlay("grid"));
        Assert.False(capability.RemoveOverlay("grid"));
        Assert.Empty(capability.GetOverlayIds());
    }

    [Fact]
    public void IsOverlayVisible_UnknownOverlay_ReturnsFalse()
    {
        var capability = new ViewportCapability();

        Assert.False(capability.IsOverlayVisible("missing"));
    }

    #endregion

    #region Pick Handler Tests

    [Fact]
    public void GetPickHandlers_ReturnsHandlersOrderedByPriorityDescending()
    {
        var capability = new ViewportCapability();
        var low = new FakePickHandler(1);
        var high = new FakePickHandler(10);
        capability.AddPickHandler(low);
        capability.AddPickHandler(high);

        var ordered = capability.GetPickHandlers().ToList();

        Assert.Equal(new IPickHandler[] { high, low }, ordered);
    }

    [Fact]
    public void RemovePickHandler_RemovesRegisteredHandler()
    {
        var capability = new ViewportCapability();
        var handler = new FakePickHandler(1);
        capability.AddPickHandler(handler);

        capability.RemovePickHandler(handler);

        Assert.Empty(capability.GetPickHandlers());
    }

    #endregion

    #region Fakes

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

    private sealed class FakeOverlay : IViewportOverlay
    {
        public bool IsVisible { get; set; }
        public int Order => 0;

        public void Render(OverlayRenderContext context)
        {
        }
    }

    private sealed class FakePickHandler(int priority) : IPickHandler
    {
        public int Priority { get; } = priority;

        public PickResult? TryPick(PickContext context) => null;
    }

    #endregion
}
