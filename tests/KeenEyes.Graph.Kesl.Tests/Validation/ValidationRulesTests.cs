using KeenEyes.Graph.Kesl.Validation;
using KeenEyes.Graph.Kesl.Validation.Rules;

namespace KeenEyes.Graph.Kesl.Tests.Validation;

public class ValidationRulesTests
{
    #region SingleRootRule Tests

    [Fact]
    public void SingleRootRule_NoComputeShader_ReportsError()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateAddNode(); // Not a ComputeShader
        var rule = new SingleRootRule();
        var result = new ValidationResult();

        rule.Validate(builder.Canvas, builder.World, result);

        Assert.Contains(result.Messages, r => r.Code == "KESL001" && r.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void SingleRootRule_OneComputeShader_NoErrors()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader();
        var rule = new SingleRootRule();
        var result = new ValidationResult();

        rule.Validate(builder.Canvas, builder.World, result);

        Assert.DoesNotContain(result.Messages, r => r.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void SingleRootRule_MultipleComputeShaders_ReportsError()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("Shader1");
        builder.CreateComputeShader("Shader2");
        var rule = new SingleRootRule();
        var result = new ValidationResult();

        rule.Validate(builder.Canvas, builder.World, result);

        Assert.Contains(result.Messages, r => r.Code == "KESL002" && r.Severity == ValidationSeverity.Error);
    }

    #endregion

    #region NoCyclesRule Tests

    [Fact]
    public void NoCyclesRule_NoCycles_NoErrors()
    {
        using var builder = new TestGraphBuilder();
        var const1 = builder.CreateFloatConstant(1.0f);
        var add = builder.CreateAddNode();
        builder.Connect(const1, 0, add, 0);
        var rule = new NoCyclesRule();
        var result = new ValidationResult();

        rule.Validate(builder.Canvas, builder.World, result);

        Assert.DoesNotContain(result.Messages, r => r.Code == "KESL030");
    }

    [Fact]
    public void NoCyclesRule_DirectCycle_ReportsError()
    {
        using var builder = new TestGraphBuilder();
        var add1 = builder.CreateAddNode();
        var add2 = builder.CreateAddNode();
        builder.Connect(add1, 0, add2, 0);
        builder.Connect(add2, 0, add1, 0); // Create cycle
        var rule = new NoCyclesRule();
        var result = new ValidationResult();

        rule.Validate(builder.Canvas, builder.World, result);

        Assert.Contains(result.Messages, r => r.Code == "KESL030" && r.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void NoCyclesRule_IndirectCycle_ReportsError()
    {
        using var builder = new TestGraphBuilder();
        var add1 = builder.CreateAddNode();
        var add2 = builder.CreateAddNode();
        var add3 = builder.CreateAddNode();
        builder.Connect(add1, 0, add2, 0);
        builder.Connect(add2, 0, add3, 0);
        builder.Connect(add3, 0, add1, 0); // Create cycle: add1 -> add2 -> add3 -> add1
        var rule = new NoCyclesRule();
        var result = new ValidationResult();

        rule.Validate(builder.Canvas, builder.World, result);

        Assert.Contains(result.Messages, r => r.Code == "KESL030" && r.Severity == ValidationSeverity.Error);
    }

    #endregion
}
