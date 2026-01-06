namespace KeenEyes.Graph.Kesl;

/// <summary>
/// Reserved type IDs for KESL (KeenEyes Shader Language) node types.
/// </summary>
/// <remarks>
/// <para>
/// KESL node IDs start at 1001 to avoid conflicts with built-in graph node types (1-100)
/// and user-defined node types (101-1000).
/// </para>
/// <para>
/// IDs are organized by category:
/// <list type="bullet">
/// <item>1001-1099: Shader nodes (ComputeShader, QueryBinding, Parameter)</item>
/// <item>1100-1199: Math nodes (Add, Subtract, Multiply, etc.)</item>
/// <item>1200-1299: Vector nodes (Construct, Split, Normalize, etc.)</item>
/// <item>1300-1399: Logic nodes (And, Or, Not, Compare, etc.)</item>
/// <item>1400-1499: Flow nodes (If, ForLoop, SetVariable)</item>
/// </list>
/// </para>
/// </remarks>
public static class KeslNodeIds
{
    #region Shader Nodes (1001-1099)

    /// <summary>
    /// Root compute shader node - defines shader name and aggregates query, params, execute.
    /// </summary>
    public const int ComputeShader = 1001;

    /// <summary>
    /// Query binding node - binds ECS component fields to shader inputs.
    /// </summary>
    public const int QueryBinding = 1002;

    /// <summary>
    /// Parameter node - defines uniform shader parameters.
    /// </summary>
    public const int Parameter = 1003;

    #endregion

    #region Math Nodes (1100-1199)

    /// <summary>Add two values (A + B).</summary>
    public const int Add = 1100;

    /// <summary>Subtract two values (A - B).</summary>
    public const int Subtract = 1101;

    /// <summary>Multiply two values (A * B).</summary>
    public const int Multiply = 1102;

    /// <summary>Divide two values (A / B).</summary>
    public const int Divide = 1103;

    /// <summary>Modulo operation (A % B).</summary>
    public const int Modulo = 1104;

    /// <summary>Absolute value.</summary>
    public const int Abs = 1105;

    /// <summary>Floor function.</summary>
    public const int Floor = 1106;

    /// <summary>Ceiling function.</summary>
    public const int Ceil = 1107;

    /// <summary>Round to nearest integer.</summary>
    public const int Round = 1108;

    /// <summary>Minimum of two values.</summary>
    public const int Min = 1109;

    /// <summary>Maximum of two values.</summary>
    public const int Max = 1110;

    /// <summary>Clamp value between min and max.</summary>
    public const int Clamp = 1111;

    /// <summary>Linear interpolation between two values.</summary>
    public const int Lerp = 1112;

    /// <summary>Sine function.</summary>
    public const int Sin = 1113;

    /// <summary>Cosine function.</summary>
    public const int Cos = 1114;

    /// <summary>Tangent function.</summary>
    public const int Tan = 1115;

    /// <summary>Power function (base^exponent).</summary>
    public const int Pow = 1116;

    /// <summary>Square root.</summary>
    public const int Sqrt = 1117;

    /// <summary>Exponential function (e^x).</summary>
    public const int Exp = 1118;

    /// <summary>Negate value (-x).</summary>
    public const int Negate = 1119;

    /// <summary>Arc sine function.</summary>
    public const int Asin = 1120;

    /// <summary>Arc cosine function.</summary>
    public const int Acos = 1121;

    /// <summary>Arc tangent function.</summary>
    public const int Atan = 1122;

    /// <summary>Two-argument arc tangent.</summary>
    public const int Atan2 = 1123;

    /// <summary>Natural logarithm.</summary>
    public const int Log = 1124;

    /// <summary>Base-2 logarithm.</summary>
    public const int Log2 = 1125;

    /// <summary>Sign function (-1, 0, or 1).</summary>
    public const int Sign = 1126;

    /// <summary>Fractional part of a value.</summary>
    public const int Frac = 1127;

    /// <summary>Step function (0 if x &lt; edge, else 1).</summary>
    public const int Step = 1128;

    /// <summary>Smooth Hermite interpolation.</summary>
    public const int Smoothstep = 1129;

    #endregion

    #region Vector Nodes (1200-1299)

    /// <summary>Construct a Vector2 from X, Y components.</summary>
    public const int ConstructVector2 = 1200;

    /// <summary>Construct a Vector3 from X, Y, Z components.</summary>
    public const int ConstructVector3 = 1201;

    /// <summary>Construct a Vector4 from X, Y, Z, W components.</summary>
    public const int ConstructVector4 = 1202;

    /// <summary>Split a Vector2 into X, Y components.</summary>
    public const int SplitVector2 = 1210;

    /// <summary>Split a Vector3 into X, Y, Z components.</summary>
    public const int SplitVector3 = 1211;

    /// <summary>Split a Vector4 into X, Y, Z, W components.</summary>
    public const int SplitVector4 = 1212;

    /// <summary>Normalize a vector to unit length.</summary>
    public const int Normalize = 1220;

    /// <summary>Dot product of two vectors.</summary>
    public const int DotProduct = 1221;

    /// <summary>Cross product of two Vector3s.</summary>
    public const int CrossProduct = 1222;

    /// <summary>Length (magnitude) of a vector.</summary>
    public const int Length = 1223;

    /// <summary>Distance between two points.</summary>
    public const int Distance = 1224;

    /// <summary>Reflect a vector about a normal.</summary>
    public const int Reflect = 1225;

    /// <summary>Refract a vector through a surface.</summary>
    public const int Refract = 1226;

    /// <summary>Squared length of a vector (faster than Length).</summary>
    public const int LengthSquared = 1227;

    /// <summary>Squared distance between two points (faster than Distance).</summary>
    public const int DistanceSquared = 1228;

    #endregion

    #region Logic Nodes (1300-1399)

    /// <summary>Logical AND of two boolean values.</summary>
    public const int And = 1300;

    /// <summary>Logical OR of two boolean values.</summary>
    public const int Or = 1301;

    /// <summary>Logical NOT of a boolean value.</summary>
    public const int Not = 1302;

    /// <summary>Compare two values with configurable operator.</summary>
    public const int Compare = 1303;

    /// <summary>Select between two values based on condition (ternary).</summary>
    public const int Select = 1304;

    /// <summary>Logical XOR of two boolean values.</summary>
    public const int Xor = 1305;

    #endregion

    #region Flow Nodes (1400-1499)

    /// <summary>Conditional branching (if-then-else).</summary>
    public const int If = 1400;

    /// <summary>For loop iteration.</summary>
    public const int ForLoop = 1401;

    /// <summary>Set/create a named variable.</summary>
    public const int SetVariable = 1402;

    /// <summary>Get a named variable's value.</summary>
    public const int GetVariable = 1403;

    #endregion

    #region Constant Nodes (1500-1599)

    /// <summary>Float constant value.</summary>
    public const int FloatConstant = 1500;

    /// <summary>Integer constant value.</summary>
    public const int IntConstant = 1501;

    /// <summary>Boolean constant value.</summary>
    public const int BoolConstant = 1502;

    /// <summary>Vector2 constant value.</summary>
    public const int Vector2Constant = 1503;

    /// <summary>Vector3 constant value.</summary>
    public const int Vector3Constant = 1504;

    /// <summary>Vector4 constant value.</summary>
    public const int Vector4Constant = 1505;

    #endregion

    /// <summary>
    /// The first available ID for additional KESL node types.
    /// </summary>
    public const int ExtensionStart = 2000;
}
