using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that detects direct equality comparisons (== and !=) with floating-point numbers
/// and suggests using tolerance-based comparisons instead.
/// </summary>
/// <remarks>
/// <para>
/// Direct equality comparisons with floating-point numbers are unreliable due to precision limitations.
/// Small rounding errors can cause seemingly equal values to compare as unequal.
/// </para>
/// <para>
/// This analyzer detects:
/// <list type="bullet">
/// <item><description><c>float == float</c> - Direct equality comparison</description></item>
/// <item><description><c>float != float</c> - Direct inequality comparison</description></item>
/// <item><description><c>float == 0</c> or <c>0 == float</c> - Zero comparison</description></item>
/// </list>
/// </para>
/// <para>
/// Instead of direct comparisons, use the extension methods from <c>KeenEyes.Common.FloatExtensions</c>:
/// <list type="bullet">
/// <item><description><c>value.IsApproximatelyZero()</c> - Check if value is close to zero</description></item>
/// <item><description><c>value.ApproximatelyEquals(other)</c> - Compare two floats for near-equality</description></item>
/// </list>
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FloatEqualityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// KEEN040: Direct float equality comparison detected.
    /// </summary>
    public static readonly DiagnosticDescriptor FloatEqualityDetected = new(
        id: "KEEN040",
        title: "Avoid direct floating-point equality comparison",
        messageFormat: "Avoid using '{0}' for floating-point comparison; use {1} instead",
        category: "KeenEyes.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Direct equality comparisons (== and !=) with floating-point numbers are unreliable " +
                     "due to precision limitations. Use the extension methods from KeenEyes.Common.FloatExtensions: " +
                     "IsApproximatelyZero() to check if a value is close to zero, or " +
                     "ApproximatelyEquals() to compare two floats for near-equality.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(FloatEqualityDetected);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Detect binary operations (==, !=)
        context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
    }

    private static void AnalyzeBinaryOperation(OperationAnalysisContext context)
    {
        var binaryOperation = (IBinaryOperation)context.Operation;

        // Only check equality and inequality operators
        if (binaryOperation.OperatorKind != BinaryOperatorKind.Equals &&
            binaryOperation.OperatorKind != BinaryOperatorKind.NotEquals)
        {
            return;
        }

        var leftType = binaryOperation.LeftOperand.Type;
        var rightType = binaryOperation.RightOperand.Type;

        // Check if either operand is a float (System.Single)
        if (!IsFloatType(leftType) && !IsFloatType(rightType))
        {
            return;
        }

        // Skip literal-to-literal comparisons - these are always safe as they're compile-time constants
        // e.g., "0.0f == 0.0f" is a valid constant expression
        if (IsLiteralExpression(binaryOperation.LeftOperand) &&
            IsLiteralExpression(binaryOperation.RightOperand))
        {
            return;
        }

        // Skip if in FloatExtensions class (it legitimately compares floats)
        var containingType = GetContainingType(context.Operation);
        if (IsFloatExtensionsClass(containingType))
        {
            return;
        }

        // Determine the appropriate suggestion
        var isEquality = binaryOperation.OperatorKind == BinaryOperatorKind.Equals;
        var operatorText = isEquality ? "==" : "!=";
        var suggestion = GetSuggestion(binaryOperation, isEquality);

        context.ReportDiagnostic(Diagnostic.Create(
            FloatEqualityDetected,
            binaryOperation.Syntax.GetLocation(),
            operatorText,
            suggestion));
    }

    private static bool IsFloatType(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        // Check for float (System.Single)
        return type.SpecialType == SpecialType.System_Single;
    }

    /// <summary>
    /// Checks if an operation is a literal expression (possibly wrapped in a conversion).
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <returns>True if the operation is a literal expression.</returns>
    private static bool IsLiteralExpression(IOperation operation)
    {
        // Unwrap implicit conversions (e.g., int -> float)
        if (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation is ILiteralOperation;
    }

    private static bool IsZeroLiteral(IOperation operation)
    {
        if (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is ILiteralOperation literal && literal.ConstantValue.HasValue)
        {
            var value = literal.ConstantValue.Value;
            return value switch
            {
                int i => i == 0,
                float f => Math.Abs(f) < 1e-6f,
                double d => Math.Abs(d) < 1e-9,
                long l => l == 0L,
                short s => s == 0,
                byte b => b == 0,
                _ => false
            };
        }

        return false;
    }

    private static string GetSuggestion(IBinaryOperation operation, bool isEquality)
    {
        var isLeftZero = IsZeroLiteral(operation.LeftOperand);
        var isRightZero = IsZeroLiteral(operation.RightOperand);

        if (isLeftZero || isRightZero)
        {
            // One operand is zero - suggest IsApproximatelyZero()
            return isEquality
                ? "IsApproximatelyZero()"
                : "!IsApproximatelyZero()";
        }

        // Both operands are non-zero floats - suggest ApproximatelyEquals()
        return isEquality
            ? "ApproximatelyEquals()"
            : "!ApproximatelyEquals()";
    }

    private static INamedTypeSymbol? GetContainingType(IOperation operation)
    {
        var symbol = operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart);
        while (symbol != null)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return namedType;
            }

            symbol = symbol.ContainingSymbol;
        }

        return null;
    }

    private static bool IsFloatExtensionsClass(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        // Check if this is the KeenEyes.Common.FloatExtensions class
        return typeSymbol.ToDisplayString() == "KeenEyes.Common.FloatExtensions";
    }
}
