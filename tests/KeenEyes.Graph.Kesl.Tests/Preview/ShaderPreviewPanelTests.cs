using KeenEyes.Graph.Kesl.Preview;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests.Preview;

/// <summary>
/// Tests for <see cref="ShaderPreviewPanel"/>.
/// </summary>
public class ShaderPreviewPanelTests
{
    #region SetCanvas Tests

    [Fact]
    public void SetCanvas_MarksAsDirty()
    {
        using var builder = new TestGraphBuilder();
        var panel = new ShaderPreviewPanel();

        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        // With empty canvas (no compute shader), should have error
        Assert.True(panel.HasError);
    }

    [Fact]
    public void SetCanvas_SameCanvas_DoesNotMarkDirty()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("Test");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        // Get the initial before state
        var initialBeforeCount = panel.BeforeState.Count;

        // Set same canvas again
        panel.SetCanvas(builder.Canvas, builder.World);

        // Should still have same state (not cleared by re-setting same canvas)
        Assert.Equal(initialBeforeCount, panel.BeforeState.Count);
    }

    #endregion

    #region Regenerate Tests

    [Fact]
    public void Regenerate_WithValidShader_PopulatesState()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);
        builder.CreateQueryBinding("Velocity", AccessMode.Read);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        Assert.False(panel.HasError);
        Assert.Equal(3, panel.BeforeState.Count);
        Assert.Equal(3, panel.AfterState.Count);

        // Check that entities have the expected components
        Assert.All(panel.BeforeState, entity =>
        {
            Assert.Contains("Position", entity.Components.Keys);
            Assert.Contains("Velocity", entity.Components.Keys);
        });
    }

    [Fact]
    public void Regenerate_WithEmptyGraph_HasError()
    {
        using var builder = new TestGraphBuilder();
        // No nodes created

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        Assert.True(panel.HasError);
        Assert.Empty(panel.BeforeState);
        Assert.Empty(panel.AfterState);
    }

    [Fact]
    public void Regenerate_WithNoQueryBindings_HasEmptyComponents()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        // No query bindings

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        Assert.False(panel.HasError);
        // Entities are created but have no components
        Assert.All(panel.BeforeState, entity => Assert.Empty(entity.Components));
    }

    #endregion

    #region EntityCount Tests

    [Fact]
    public void EntityCount_Change_AffectsPreviewEntities()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel { EntityCount = 5 };
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        Assert.Equal(5, panel.BeforeState.Count);
        Assert.Equal(5, panel.AfterState.Count);
    }

    [Fact]
    public void EntityCount_Default_IsThree()
    {
        var panel = new ShaderPreviewPanel();

        Assert.Equal(3, panel.EntityCount);
    }

    #endregion

    #region DeltaTime Tests

    [Fact]
    public void DeltaTime_Default_Is60FpsEquivalent()
    {
        var panel = new ShaderPreviewPanel();

        // ~60fps is 0.016 seconds per frame
        Assert.Equal(0.016f, panel.DeltaTime, precision: 3);
    }

    [Fact]
    public void DeltaTime_CanBeChanged()
    {
        var panel = new ShaderPreviewPanel { DeltaTime = 1.0f };

        Assert.Equal(1.0f, panel.DeltaTime);
    }

    #endregion

    #region Update Debounce Tests

    [Fact]
    public void Update_WithinDebounceDelay_DoesNotRegenerate()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);

        // Call update immediately - should not regenerate due to debounce
        panel.Update();

        // State should be empty (not regenerated yet)
        Assert.Empty(panel.BeforeState);
    }

    [Fact]
    public async Task Update_AfterDebounceDelay_Regenerates()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);

        // Wait for debounce delay to pass
        await Task.Delay(350, TestContext.Current.CancellationToken); // 300ms debounce + buffer

        panel.Update();

        Assert.NotEmpty(panel.BeforeState);
    }

    #endregion

    #region ForceRegenerate Tests

    [Fact]
    public void ForceRegenerate_BypassesDebounce()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);

        // Force regenerate immediately
        panel.ForceRegenerate();

        Assert.NotEmpty(panel.BeforeState);
    }

    #endregion

    #region ResetAndExecute Tests

    [Fact]
    public void ResetAndExecute_RestoresInitialValuesAndReruns()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        // Get initial X values
        var initialX0 = panel.BeforeState[0].Components["Position"].Fields["X"];

        // Reset and execute again
        panel.ResetAndExecute();

        // Before state should be reset to initial values
        Assert.Equal(initialX0, panel.BeforeState[0].Components["Position"].Fields["X"]);
    }

    #endregion

    #region GetFormattedOutput Tests

    [Fact]
    public void GetFormattedOutput_WithError_ShowsError()
    {
        using var builder = new TestGraphBuilder();
        // Empty graph causes error

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        var output = panel.GetFormattedOutput();

        Assert.StartsWith("Error:", output);
    }

    [Fact]
    public void GetFormattedOutput_WithNoBindings_ShowsMessage()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        // No query bindings

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        var output = panel.GetFormattedOutput();

        Assert.Contains("No preview available", output);
    }

    [Fact]
    public void GetFormattedOutput_WithValidShader_ShowsEntityData()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);

        var panel = new ShaderPreviewPanel();
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        var output = panel.GetFormattedOutput();

        Assert.Contains("Entity 0:", output);
        Assert.Contains("Position:", output);
        Assert.Contains("X:", output);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_CreateShaderAndPreview()
    {
        using var builder = new TestGraphBuilder();

        // Create a physics update shader
        var shader = builder.CreateComputeShader("PhysicsUpdate");
        var posBinding = builder.CreateQueryBinding("Position", AccessMode.Write);
        var velBinding = builder.CreateQueryBinding("Velocity", AccessMode.Read);

        var panel = new ShaderPreviewPanel { DeltaTime = 1.0f };
        panel.SetCanvas(builder.Canvas, builder.World);
        panel.ForceRegenerate();

        // Verify preview was generated
        Assert.False(panel.HasError, $"Expected no error but got: {panel.CompilationError}");
        Assert.Equal(3, panel.BeforeState.Count);
        Assert.Equal(3, panel.AfterState.Count);

        // Verify each entity has both components
        for (int i = 0; i < 3; i++)
        {
            Assert.Contains("Position", panel.BeforeState[i].Components.Keys);
            Assert.Contains("Velocity", panel.BeforeState[i].Components.Keys);
        }
    }

    #endregion
}
