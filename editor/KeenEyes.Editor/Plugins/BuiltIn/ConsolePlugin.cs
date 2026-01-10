// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Logging;
using KeenEyes.Editor.Panels;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the console panel for log output.
/// </summary>
/// <remarks>
/// <para>
/// The console panel displays log messages, warnings, and errors from the
/// game runtime and editor. It supports filtering by log level and searching.
/// </para>
/// </remarks>
internal sealed class ConsolePlugin : EditorPluginBase
{
    private const string PanelId = "console";

    /// <inheritdoc />
    public override string Name => "Console";

    /// <inheritdoc />
    public override string? Description => "Console panel for log output and debugging";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the console panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Console",
                Icon = "console",
                DefaultLocation = PanelDockLocation.Bottom,
                OpenByDefault = true,
                MinWidth = 300,
                MinHeight = 100,
                DefaultWidth = 600,
                DefaultHeight = 200,
                Category = "Debug",
                ToggleShortcut = "Ctrl+Shift+C"
            },
            () => new ConsolePanelImpl());

        // Register shortcut for toggling the console panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "console.toggle",
                "Toggle Console",
                ShortcutCategories.View,
                "Ctrl+Shift+C",
                () =>
                {
                    if (context.TryGetCapability<IPanelCapability>(out var p) && p is not null)
                    {
                        if (p.IsPanelOpen(PanelId))
                        {
                            p.ClosePanel(PanelId);
                        }
                        else
                        {
                            p.OpenPanel(PanelId);
                        }
                    }
                });

            shortcuts.RegisterShortcut(
                "console.clear",
                "Clear Console",
                ShortcutCategories.View,
                "Ctrl+L",
                () =>
                {
                    // Clear console output via the log queryable
                    context.Log?.Clear();
                });
        }
    }
}

/// <summary>
/// Implementation of the console panel.
/// </summary>
internal sealed class ConsolePanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private IWorld? editorWorld;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        editorWorld = context.EditorWorld;

        // Get the log provider (must be EditorLogProvider for UI creation)
        if (context.EditorContext.Log is EditorLogProvider logProvider)
        {
            // Create console UI using the existing ConsolePanel helper
            // This creates the full UI with log list, filter buttons, and subscribes to log events
            rootEntity = ConsolePanel.Create(
                context.EditorWorld,
                context.Parent,
                context.Font,
                logProvider);
        }
        else
        {
            // Fallback: just use parent if no log provider available
            rootEntity = context.Parent;
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Log updates are handled via event subscriptions set up in ConsolePanel.Create()
        // No per-frame polling needed
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }
}
