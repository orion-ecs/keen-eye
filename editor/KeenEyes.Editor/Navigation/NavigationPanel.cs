// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Editor panel for navigation mesh settings and baking.
/// </summary>
/// <remarks>
/// <para>
/// The Navigation panel provides a unified interface for:
/// - Managing agent types and their settings
/// - Configuring navmesh baking parameters
/// - Triggering navmesh bakes
/// - Viewing bake results and statistics
/// - Controlling debug visualization
/// </para>
/// </remarks>
public sealed class NavigationPanel : IEditorPanel
{
    private PanelContext? context;
    private Entity rootEntity;

    private readonly List<NavMeshBakeConfig> agentConfigs = [];
    private int selectedAgentIndex;
    private NavMeshVisualizer? visualizer;
    private NavMeshBakeResult? lastBakeResult;
    private bool isBaking;

    /// <summary>
    /// Creates a new navigation panel.
    /// </summary>
    public NavigationPanel()
    {
        // Add default agent type
        agentConfigs.Add(NavMeshBakeConfig.Humanoid);
    }

    /// <inheritdoc/>
    public Entity RootEntity => rootEntity;

    /// <inheritdoc/>
    public void Initialize(PanelContext panelContext)
    {
        context = panelContext;

        // Create the root entity for the panel UI
        rootEntity = panelContext.EditorWorld.Spawn("NavigationPanel").Build();

        // Try to get the visualizer from the viewport capability
        if (panelContext.EditorContext.TryGetCapability<IViewportCapability>(out var viewport) && viewport != null)
        {
            foreach (var renderer in viewport.GetGizmoRenderers())
            {
                if (renderer is NavMeshVisualizer vis)
                {
                    visualizer = vis;
                    break;
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        // Panel UI updates would happen here
        // In a real implementation, this would render the panel's UI
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (context != null && rootEntity.IsValid)
        {
            context.EditorWorld.Despawn(rootEntity);
        }
    }

    /// <summary>
    /// Gets the list of configured agent types.
    /// </summary>
    public IReadOnlyList<NavMeshBakeConfig> AgentConfigs => agentConfigs;

    /// <summary>
    /// Gets or sets the selected agent configuration index.
    /// </summary>
    public int SelectedAgentIndex
    {
        get => selectedAgentIndex;
        set => selectedAgentIndex = Math.Clamp(value, 0, Math.Max(0, agentConfigs.Count - 1));
    }

    /// <summary>
    /// Gets the currently selected agent configuration.
    /// </summary>
    public NavMeshBakeConfig? SelectedAgentConfig =>
        selectedAgentIndex >= 0 && selectedAgentIndex < agentConfigs.Count
            ? agentConfigs[selectedAgentIndex]
            : null;

    /// <summary>
    /// Gets whether a bake operation is in progress.
    /// </summary>
    public bool IsBaking => isBaking;

    /// <summary>
    /// Gets the result of the last bake operation.
    /// </summary>
    public NavMeshBakeResult? LastBakeResult => lastBakeResult;

    /// <summary>
    /// Adds a new agent configuration.
    /// </summary>
    /// <param name="name">The name for the new agent type.</param>
    /// <returns>The new configuration.</returns>
    public NavMeshBakeConfig AddAgentConfig(string name)
    {
        var config = NavMeshBakeConfig.Humanoid.Clone();
        config.AgentTypeName = name;
        agentConfigs.Add(config);
        return config;
    }

    /// <summary>
    /// Adds an agent configuration from a preset.
    /// </summary>
    /// <param name="preset">The preset to use.</param>
    /// <returns>The new configuration.</returns>
    public NavMeshBakeConfig AddAgentConfigFromPreset(AgentPreset preset)
    {
        var config = preset switch
        {
            AgentPreset.Humanoid => NavMeshBakeConfig.Humanoid.Clone(),
            AgentPreset.Small => NavMeshBakeConfig.Small.Clone(),
            AgentPreset.Large => NavMeshBakeConfig.Large.Clone(),
            _ => NavMeshBakeConfig.Humanoid.Clone()
        };

        agentConfigs.Add(config);
        return config;
    }

    /// <summary>
    /// Removes an agent configuration.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    public void RemoveAgentConfig(int index)
    {
        if (index >= 0 && index < agentConfigs.Count && agentConfigs.Count > 1)
        {
            agentConfigs.RemoveAt(index);
            if (selectedAgentIndex >= agentConfigs.Count)
            {
                selectedAgentIndex = agentConfigs.Count - 1;
            }
        }
    }

    /// <summary>
    /// Bakes the navmesh for the currently selected agent type.
    /// </summary>
    /// <param name="sceneWorld">The scene world to bake from.</param>
    /// <returns>The bake result.</returns>
    public NavMeshBakeResult? BakeSelectedAgent(World sceneWorld)
    {
        var config = SelectedAgentConfig;
        if (config == null)
        {
            return NavMeshBakeResult.Failure("No agent type selected");
        }

        return BakeNavMesh(sceneWorld, config);
    }

    /// <summary>
    /// Bakes a navmesh with the specified configuration.
    /// </summary>
    /// <param name="sceneWorld">The scene world to bake from.</param>
    /// <param name="config">The bake configuration.</param>
    /// <returns>The bake result.</returns>
    public NavMeshBakeResult? BakeNavMesh(World sceneWorld, NavMeshBakeConfig config)
    {
        if (isBaking)
        {
            return NavMeshBakeResult.Failure("A bake is already in progress");
        }

        isBaking = true;

        try
        {
            var command = new NavMeshBakeCommand(sceneWorld, config, OnBakeProgress);
            command.Execute();
            lastBakeResult = command.Result;

            // Update visualizer with new navmesh
            if (lastBakeResult?.IsSuccess == true && command.BakedNavMesh != null)
            {
                visualizer?.SetNavMesh(command.BakedNavMesh);
            }

            return lastBakeResult;
        }
        finally
        {
            isBaking = false;
        }
    }

    /// <summary>
    /// Bakes navmeshes for all configured agent types.
    /// </summary>
    /// <param name="sceneWorld">The scene world to bake from.</param>
    /// <returns>Results for each agent type.</returns>
    public IEnumerable<NavMeshBakeResult> BakeAllAgents(World sceneWorld)
    {
        foreach (var config in agentConfigs)
        {
            var result = BakeNavMesh(sceneWorld, config);
            if (result != null)
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Clears the current navmesh from the visualizer.
    /// </summary>
    public void ClearNavMesh()
    {
        visualizer?.SetNavMesh(null);
        lastBakeResult = null;
    }

    /// <summary>
    /// Loads a navmesh from a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>True if loading succeeded.</returns>
    public bool LoadNavMesh(string path)
    {
        if (NavMeshSerializer.TryLoad(path, out var result) && result != null)
        {
            visualizer?.SetNavMesh(result.NavMesh);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets or sets whether the navmesh visualization is enabled.
    /// </summary>
    public bool IsVisualizationEnabled
    {
        get => visualizer?.IsEnabled ?? false;
        set
        {
            if (visualizer != null)
            {
                visualizer.IsEnabled = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the visualization display mode.
    /// </summary>
    public NavMeshDisplayMode DisplayMode
    {
        get => visualizer?.DisplayMode ?? NavMeshDisplayMode.SurfacesAndEdges;
        set
        {
            if (visualizer != null)
            {
                visualizer.DisplayMode = value;
            }
        }
    }

    private void OnBakeProgress(NavMeshBakeProgress progress)
    {
        // Log progress or update UI
        // In a real implementation, this would update a progress bar
    }

    /// <summary>
    /// Gets the panel layout information for creating the UI.
    /// </summary>
    /// <returns>The panel layout data.</returns>
    public NavigationPanelLayout GetLayout()
    {
        return new NavigationPanelLayout
        {
            AgentConfigs = agentConfigs.ToArray(),
            SelectedAgentIndex = selectedAgentIndex,
            IsBaking = isBaking,
            LastBakeResult = lastBakeResult,
            IsVisualizationEnabled = IsVisualizationEnabled,
            DisplayMode = DisplayMode
        };
    }
}

/// <summary>
/// Layout data for the navigation panel UI.
/// </summary>
public sealed class NavigationPanelLayout
{
    /// <summary>
    /// Gets the configured agent types.
    /// </summary>
    public required NavMeshBakeConfig[] AgentConfigs { get; init; }

    /// <summary>
    /// Gets the selected agent index.
    /// </summary>
    public int SelectedAgentIndex { get; init; }

    /// <summary>
    /// Gets whether a bake is in progress.
    /// </summary>
    public bool IsBaking { get; init; }

    /// <summary>
    /// Gets the last bake result.
    /// </summary>
    public NavMeshBakeResult? LastBakeResult { get; init; }

    /// <summary>
    /// Gets whether visualization is enabled.
    /// </summary>
    public bool IsVisualizationEnabled { get; init; }

    /// <summary>
    /// Gets the current display mode.
    /// </summary>
    public NavMeshDisplayMode DisplayMode { get; init; }
}

/// <summary>
/// Preset agent types for common use cases.
/// </summary>
public enum AgentPreset
{
    /// <summary>
    /// Standard humanoid agent (2m tall, 0.5m radius).
    /// </summary>
    Humanoid,

    /// <summary>
    /// Small agent like a critter or drone (1m tall, 0.25m radius).
    /// </summary>
    Small,

    /// <summary>
    /// Large agent like a vehicle or giant (3m tall, 1m radius).
    /// </summary>
    Large
}
