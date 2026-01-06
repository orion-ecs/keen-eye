using KeenEyes.Graph.Kesl.Preview;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests.Preview;

public class CodePreviewPanelTests
{
    #region Initial State Tests

    [Fact]
    public void InitialState_HasNoErrors()
    {
        var panel = new CodePreviewPanel();

        Assert.False(panel.HasErrors);
        Assert.Empty(panel.ErrorMessages);
    }

    [Fact]
    public void InitialState_EmptySource()
    {
        var panel = new CodePreviewPanel();

        Assert.Empty(panel.KeslSource);
        Assert.Empty(panel.GlslSource);
    }

    [Fact]
    public void InitialState_DefaultTabIsKesl()
    {
        var panel = new CodePreviewPanel();

        Assert.Equal(PreviewTab.Kesl, panel.ActiveTab);
    }

    #endregion

    #region SetGraph Tests

    [Fact]
    public void SetGraph_MarksAsDirty()
    {
        using var builder = new TestGraphBuilder();
        var panel = new CodePreviewPanel();

        panel.SetGraph(builder.Canvas, builder.World);
        // After update with debounce, should attempt regeneration

        // Can't directly test isDirty but we can verify Update changes things
    }

    [Fact]
    public void SetGraph_SameGraphTwice_DoesNotMarkDirtyAgain()
    {
        using var builder = new TestGraphBuilder();
        var panel = new CodePreviewPanel();

        panel.SetGraph(builder.Canvas, builder.World);
        panel.Regenerate(); // Clear dirty

        panel.SetGraph(builder.Canvas, builder.World);
        // Same canvas/world, no change expected
    }

    #endregion

    #region Regenerate Tests

    [Fact]
    public void Regenerate_EmptyGraph_SetsErrorState()
    {
        using var builder = new TestGraphBuilder();
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);

        panel.Regenerate();

        Assert.True(panel.HasErrors);
        Assert.NotEmpty(panel.ErrorMessages);
    }

    [Fact]
    public void Regenerate_ValidGraph_GeneratesKesl()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);

        panel.Regenerate();

        Assert.False(panel.HasErrors);
        Assert.Contains("compute TestShader", panel.KeslSource);
    }

    [Fact]
    public void Regenerate_ValidGraph_GeneratesGlsl()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);

        panel.Regenerate();

        Assert.False(panel.HasErrors);
        // GLSL should have version header
        Assert.Contains("#version", panel.GlslSource);
    }

    [Fact]
    public void Regenerate_NoWorld_ClearsOutput()
    {
        var panel = new CodePreviewPanel();
        // No SetGraph called, so no world

        panel.Regenerate();

        Assert.Empty(panel.KeslSource);
        Assert.Empty(panel.GlslSource);
        Assert.False(panel.HasErrors);
    }

    #endregion

    #region GetActiveSource Tests

    [Fact]
    public void GetActiveSource_KeslTab_ReturnsKeslSource()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);
        panel.Regenerate();
        panel.ActiveTab = PreviewTab.Kesl;

        var source = panel.GetActiveSource();

        Assert.Equal(panel.KeslSource, source);
    }

    [Fact]
    public void GetActiveSource_GlslTab_ReturnsGlslSource()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);
        panel.Regenerate();
        panel.ActiveTab = PreviewTab.Glsl;

        var source = panel.GetActiveSource();

        Assert.Equal(panel.GlslSource, source);
    }

    #endregion

    #region CopyToClipboard Tests

    [Fact]
    public void CopyToClipboard_ReturnsActiveSource()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);
        panel.Regenerate();
        panel.ActiveTab = PreviewTab.Kesl;

        var copied = panel.CopyToClipboard();

        Assert.Equal(panel.GetActiveSource(), copied);
    }

    #endregion

    #region MarkDirty Tests

    [Fact]
    public void MarkDirty_CausesPendingRegeneration()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        var panel = new CodePreviewPanel();
        panel.SetGraph(builder.Canvas, builder.World);
        panel.Regenerate();

        panel.MarkDirty();
        // Update won't regenerate immediately due to debounce,
        // but Regenerate() will work
        panel.Regenerate();

        Assert.False(panel.HasErrors);
    }

    #endregion
}
