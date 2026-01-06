// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.AI;

/// <summary>
/// Editor plugin for AI debugging and visualization.
/// </summary>
/// <remarks>
/// <para>
/// This plugin adds AI-related tooling to the editor:
/// - AI Debug panel for visualizing FSM, Behavior Trees, and Utility AI
/// - Real-time state inspection and blackboard viewing
/// - Visual debugger for AI decision-making
/// </para>
/// <para>
/// The plugin integrates with KeenEyes.AI systems to provide
/// comprehensive AI debugging capabilities.
/// </para>
/// </remarks>
public sealed class AIEditorPlugin : EditorPluginBase
{
    /// <inheritdoc/>
    public override string Name => "AI Debug";

    /// <inheritdoc/>
    public override string Description => "AI debugging and visualization tools";

    /// <inheritdoc/>
    protected override void OnInitialize(IEditorContext context)
    {
        // Register the AI debug panel
        if (context.TryGetCapability<IPanelCapability>(out var panels) && panels != null)
        {
            panels.RegisterPanel<AIDebugPanel>(new PanelDescriptor
            {
                Id = "ai-debug",
                Title = "AI Debug",
                Icon = "brain",
                DefaultLocation = PanelDockLocation.Right,
                OpenByDefault = false,
                MinWidth = 300,
                DefaultWidth = 350,
                DefaultHeight = 500,
                Category = "AI",
                ToggleShortcut = "Ctrl+Shift+A"
            });
        }

        // Register menu items
        if (context.TryGetCapability<IMenuCapability>(out var menu) && menu != null)
        {
            RegisterMenuItems(menu);
        }

        // Register keyboard shortcuts
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts != null)
        {
            RegisterShortcuts(shortcuts);
        }
    }

    /// <inheritdoc/>
    protected override void OnShutdown()
    {
        // Cleanup is handled automatically by the editor plugin system
    }

    private void RegisterMenuItems(IMenuCapability menu)
    {
        // Window menu item to open AI debug panel
        menu.AddMenuItem(
            "Window/AI/AI Debug",
            new EditorCommand
            {
                Id = "window.ai-debug",
                DisplayName = "AI Debug",
                Execute = OpenAIDebugPanel
            });

        // AI menu for quick actions
        menu.AddMenuItem(
            "AI/Pause All AI",
            new EditorCommand
            {
                Id = "ai.pause-all",
                DisplayName = "Pause All AI",
                Execute = PauseAllAI
            });

        menu.AddMenuItem(
            "AI/Resume All AI",
            new EditorCommand
            {
                Id = "ai.resume-all",
                DisplayName = "Resume All AI",
                Execute = ResumeAllAI
            });
    }

    private void RegisterShortcuts(IShortcutCapability shortcuts)
    {
        shortcuts.RegisterShortcut(
            "ai.toggle-debug-panel",
            "Toggle AI Debug Panel",
            ShortcutCategories.View,
            "Ctrl+Shift+A",
            OpenAIDebugPanel,
            () => true);
    }

    private void OpenAIDebugPanel()
    {
        if (Context?.TryGetCapability<IPanelCapability>(out var panels) == true && panels != null)
        {
            panels.OpenPanel("ai-debug");
        }
    }

    private void PauseAllAI()
    {
        // Implementation would pause all AI systems in the scene world
        // This would be done through the scene world's AI context
    }

    private void ResumeAllAI()
    {
        // Implementation would resume all AI systems in the scene world
        // This would be done through the scene world's AI context
    }
}
