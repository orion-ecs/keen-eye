namespace KeenEyes.Graph.Kesl.Nodes.Math;

/// <summary>Absolute value.</summary>
public sealed class AbsNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Abs;

    /// <inheritdoc />
    public override string Name => "Abs";
}

/// <summary>Negate value (-x).</summary>
public sealed class NegateNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Negate;

    /// <inheritdoc />
    public override string Name => "Negate";
}

/// <summary>Floor function.</summary>
public sealed class FloorNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Floor;

    /// <inheritdoc />
    public override string Name => "Floor";
}

/// <summary>Ceiling function.</summary>
public sealed class CeilNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Ceil;

    /// <inheritdoc />
    public override string Name => "Ceil";
}

/// <summary>Round to nearest integer.</summary>
public sealed class RoundNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Round;

    /// <inheritdoc />
    public override string Name => "Round";
}

/// <summary>Sign function (-1, 0, or 1).</summary>
public sealed class SignNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Sign;

    /// <inheritdoc />
    public override string Name => "Sign";
}

/// <summary>Fractional part of a value.</summary>
public sealed class FracNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Frac;

    /// <inheritdoc />
    public override string Name => "Frac";
}

/// <summary>Square root.</summary>
public sealed class SqrtNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Sqrt;

    /// <inheritdoc />
    public override string Name => "Sqrt";
}

/// <summary>Exponential function (e^x).</summary>
public sealed class ExpNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Exp;

    /// <inheritdoc />
    public override string Name => "Exp";
}

/// <summary>Natural logarithm.</summary>
public sealed class LogNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Log;

    /// <inheritdoc />
    public override string Name => "Log";
}

/// <summary>Base-2 logarithm.</summary>
public sealed class Log2Node : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Log2;

    /// <inheritdoc />
    public override string Name => "Log2";
}

/// <summary>Sine function.</summary>
public sealed class SinNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Sin;

    /// <inheritdoc />
    public override string Name => "Sin";
}

/// <summary>Cosine function.</summary>
public sealed class CosNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Cos;

    /// <inheritdoc />
    public override string Name => "Cos";
}

/// <summary>Tangent function.</summary>
public sealed class TanNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Tan;

    /// <inheritdoc />
    public override string Name => "Tan";
}

/// <summary>Arc sine function.</summary>
public sealed class AsinNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Asin;

    /// <inheritdoc />
    public override string Name => "Asin";
}

/// <summary>Arc cosine function.</summary>
public sealed class AcosNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Acos;

    /// <inheritdoc />
    public override string Name => "Acos";
}

/// <summary>Arc tangent function.</summary>
public sealed class AtanNode : UnaryMathNodeBase
{
    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Atan;

    /// <inheritdoc />
    public override string Name => "Atan";
}
