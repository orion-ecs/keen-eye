// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Assets;
using KeenEyes.Editor.Commands;
using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.BuiltIn;
using KeenEyes.Editor.Plugins.Capabilities;
using KeenEyes.Editor.Selection;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests that the editor installs its built-in gizmo plugins during startup and that their
/// gizmo renderers are reachable through the viewport capability and removed on shutdown.
/// </summary>
public sealed class BuiltInGizmoPluginsTests : IDisposable
{
    private readonly World editorWorld = new();
    private readonly EditorWorldManager worldManager = new();
    private readonly SelectionManager selection = new();
    private readonly UndoRedoManager undoRedo = new();
    private readonly AssetDatabase assets;
    private readonly DirectoryInfo tempProjectRoot;

    public BuiltInGizmoPluginsTests()
    {
        tempProjectRoot = Directory.CreateTempSubdirectory("keeneyes-gizmo-plugins-");
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

    [Fact]
    public void Install_AfterBootInit_InstallsNavigationAndAnimationPlugins()
    {
        using var manager = CreateBootEquivalentManager(out _);

        BuiltInGizmoPlugins.Install(manager);

        Assert.True(manager.HasPlugin("Navigation"));
        Assert.True(manager.HasPlugin("Animation"));
    }

    [Fact]
    public void Install_AfterBootInit_RegistersAllGizmoRenderersInViewportCapability()
    {
        using var manager = CreateBootEquivalentManager(out var viewport);

        BuiltInGizmoPlugins.Install(manager);

        var rendererIds = viewport.GetGizmoRenderers().Select(r => r.Id).ToList();

        Assert.Equal(3, rendererIds.Count);
        Assert.Contains("navmesh-visualizer", rendererIds);
        Assert.Contains("skeleton-gizmo", rendererIds);
        Assert.Contains("ik-gizmo", rendererIds);
    }

    [Fact]
    public void ShutdownAll_RemovesGizmoRenderersFromViewportCapability()
    {
        using var manager = CreateBootEquivalentManager(out var viewport);
        BuiltInGizmoPlugins.Install(manager);
        Assert.NotEmpty(viewport.GetGizmoRenderers());

        manager.ShutdownAll();

        Assert.Empty(viewport.GetGizmoRenderers());
        Assert.False(manager.HasPlugin("Navigation"));
        Assert.False(manager.HasPlugin("Animation"));
    }

    [Fact]
    public void Dispose_RemovesGizmoRenderersFromViewportCapability()
    {
        var manager = CreateBootEquivalentManager(out var viewport);
        BuiltInGizmoPlugins.Install(manager);
        Assert.NotEmpty(viewport.GetGizmoRenderers());

        manager.Dispose();

        Assert.Empty(viewport.GetGizmoRenderers());
    }

    [Fact]
    public void Install_WithNullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BuiltInGizmoPlugins.Install(null!));
    }
}
