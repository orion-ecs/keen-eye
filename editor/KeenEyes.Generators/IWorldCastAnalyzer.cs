using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that detects attempts to cast IWorld to World and reports an error.
/// </summary>
/// <remarks>
/// <para>
/// The <c>IWorld</c> interface is the public contract for interacting with an ECS world.
/// Casting to the concrete <c>World</c> class bypasses this abstraction and creates tight coupling
/// to implementation details that may change.
/// </para>
/// <para>
/// This analyzer detects:
/// <list type="bullet">
/// <item><description>Direct casts: <c>(World)iWorld</c></description></item>
/// <item><description>Safe casts: <c>iWorld as World</c></description></item>
/// <item><description>Type checks: <c>iWorld is World</c></description></item>
/// <item><description>Pattern matching: <c>iWorld is World w</c></description></item>
/// </list>
/// </para>
/// <para>
/// Instead of casting, use the <c>IWorld</c> interface methods directly, or use
/// <c>IWorld.GetExtension&lt;T&gt;()</c> if you need additional functionality.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WorldCastAnalyzer : DiagnosticAnalyzer
{
    private const string IWorldTypeName = "KeenEyes.IWorld";
    private const string WorldTypeName = "KeenEyes.World";

    /// <summary>
    /// KEEN050: IWorld to World cast detected.
    /// </summary>
    public static readonly DiagnosticDescriptor IWorldCastDetected = new(
        id: "KEEN050",
        title: "Avoid casting IWorld to World",
        messageFormat: "Avoid casting IWorld to World; use the IWorld interface methods or GetExtension<T>() instead",
        category: "KeenEyes.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Casting IWorld to World bypasses the interface abstraction and creates tight coupling " +
                     "to implementation details. Use the IWorld interface methods directly, or use " +
                     "IWorld.GetExtension<T>() if you need additional functionality provided by plugins.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(IWorldCastDetected);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Detect explicit casts: (World)iWorld and iWorld as World
        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);

        // Detect type checks: iWorld is World
        context.RegisterOperationAction(AnalyzeIsType, OperationKind.IsType);

        // Detect pattern matching: iWorld is World w
        context.RegisterOperationAction(AnalyzeIsPattern, OperationKind.IsPattern);

        // Detect switch expression arms: world switch { World w => ... }
        context.RegisterOperationAction(AnalyzeSwitchExpressionArm, OperationKind.SwitchExpressionArm);
    }

    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        var conversion = (IConversionOperation)context.Operation;

        // Only check explicit conversions (casts)
        if (conversion.IsImplicit)
        {
            return;
        }

        // Check if converting from IWorld to World
        if (IsIWorldToWorldCast(conversion.Operand.Type, conversion.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                IWorldCastDetected,
                conversion.Syntax.GetLocation()));
        }
    }

    private static void AnalyzeIsType(OperationAnalysisContext context)
    {
        var isType = (IIsTypeOperation)context.Operation;

        // Check if checking IWorld is World
        if (IsIWorldToWorldCast(isType.ValueOperand.Type, isType.TypeOperand))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                IWorldCastDetected,
                isType.Syntax.GetLocation()));
        }
    }

    private static void AnalyzeIsPattern(OperationAnalysisContext context)
    {
        var isPattern = (IIsPatternOperation)context.Operation;

        // Check if the pattern involves casting IWorld to World
        if (IsIWorldType(isPattern.Value.Type) && IsWorldPatternTarget(isPattern.Pattern))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                IWorldCastDetected,
                isPattern.Syntax.GetLocation()));
        }
    }

    private static void AnalyzeSwitchExpressionArm(OperationAnalysisContext context)
    {
        var arm = (ISwitchExpressionArmOperation)context.Operation;

        // Get the switch expression to check the input type
        if (arm.Parent is ISwitchExpressionOperation switchExpr &&
            IsIWorldType(switchExpr.Value.Type) &&
            IsWorldPatternTarget(arm.Pattern))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                IWorldCastDetected,
                arm.Pattern.Syntax.GetLocation()));
        }
    }

    private static bool IsIWorldToWorldCast(ITypeSymbol? sourceType, ITypeSymbol? targetType)
    {
        return IsIWorldType(sourceType) && IsWorldType(targetType);
    }

    private static bool IsIWorldType(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        // Check the type itself
        if (type.ToDisplayString() == IWorldTypeName)
        {
            return true;
        }

        // Check if any interface is IWorld
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString() == IWorldTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWorldType(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        return type.ToDisplayString() == WorldTypeName;
    }

    private static bool IsWorldPatternTarget(IPatternOperation pattern)
    {
        // Handle declaration pattern: is World w
        if (pattern is IDeclarationPatternOperation declarationPattern)
        {
            return IsWorldType(declarationPattern.MatchedType);
        }

        // Handle type pattern: is World (C# 9+)
        if (pattern is ITypePatternOperation typePattern)
        {
            return IsWorldType(typePattern.MatchedType);
        }

        // Handle negated pattern: is not World
        if (pattern is INegatedPatternOperation negatedPattern)
        {
            return IsWorldPatternTarget(negatedPattern.Pattern);
        }

        return false;
    }
}
