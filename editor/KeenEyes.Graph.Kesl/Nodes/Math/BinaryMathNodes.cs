namespace KeenEyes.Graph.Kesl.Nodes.Math;

/// <summary>Add two values (A + B).</summary>
public sealed class AddNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Add;

    /// <inheritdoc />
    public override string Name => "Add";
}

/// <summary>Subtract two values (A - B).</summary>
public sealed class SubtractNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Subtract;

    /// <inheritdoc />
    public override string Name => "Subtract";
}

/// <summary>Multiply two values (A * B).</summary>
public sealed class MultiplyNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Multiply;

    /// <inheritdoc />
    public override string Name => "Multiply";
}

/// <summary>Divide two values (A / B).</summary>
public sealed class DivideNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Divide;

    /// <inheritdoc />
    public override string Name => "Divide";
}

/// <summary>Modulo operation (A % B).</summary>
public sealed class ModuloNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Modulo;

    /// <inheritdoc />
    public override string Name => "Modulo";
}

/// <summary>Minimum of two values.</summary>
public sealed class MinNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Min;

    /// <inheritdoc />
    public override string Name => "Min";
}

/// <summary>Maximum of two values.</summary>
public sealed class MaxNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Max;

    /// <inheritdoc />
    public override string Name => "Max";
}

/// <summary>Power function (base^exponent).</summary>
public sealed class PowNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Pow;

    /// <inheritdoc />
    public override string Name => "Power";
}

/// <summary>Two-argument arc tangent (atan2).</summary>
public sealed class Atan2Node : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Atan2;

    /// <inheritdoc />
    public override string Name => "Atan2";
}

/// <summary>Step function (0 if x &lt; edge, else 1).</summary>
public sealed class StepNode : BinaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Step;

    /// <inheritdoc />
    public override string Name => "Step";
}
