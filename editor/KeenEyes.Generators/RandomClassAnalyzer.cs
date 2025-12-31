using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that detects usage of System.Random and suggests using World.Next* methods instead.
/// </summary>
/// <remarks>
/// <para>
/// In an ECS architecture, random number generation should be centralized through the World instance
/// to ensure deterministic behavior for replays, testing, and debugging. Each World maintains its
/// own random state that can be seeded for reproducibility.
/// </para>
/// <para>
/// This analyzer detects:
/// <list type="bullet">
/// <item><description>Object creation expressions: <c>new Random()</c></description></item>
/// <item><description>Field declarations of type <c>System.Random</c></description></item>
/// <item><description>Parameter declarations of type <c>System.Random</c></description></item>
/// </list>
/// </para>
/// <para>
/// Instead of using <c>System.Random</c> directly, use the World's random methods:
/// <list type="bullet">
/// <item><description><c>world.NextInt(maxValue)</c> - Random integer [0, maxValue)</description></item>
/// <item><description><c>world.NextInt(min, max)</c> - Random integer [min, max)</description></item>
/// <item><description><c>world.NextFloat()</c> - Random float [0.0, 1.0)</description></item>
/// <item><description><c>world.NextDouble()</c> - Random double [0.0, 1.0)</description></item>
/// <item><description><c>world.NextBool()</c> - Random boolean</description></item>
/// <item><description><c>world.NextBool(probability)</c> - Boolean with specified probability</description></item>
/// </list>
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RandomClassAnalyzer : DiagnosticAnalyzer
{
    private const string SystemRandomType = "System.Random";

    /// <summary>
    /// KEEN030: System.Random usage detected.
    /// </summary>
    public static readonly DiagnosticDescriptor RandomUsageDetected = new(
        id: "KEEN030",
        title: "Avoid using System.Random directly",
        messageFormat: "Avoid using System.Random directly; use World.NextInt(), World.NextFloat(), World.NextDouble(), or World.NextBool() instead for deterministic random number generation",
        category: "KeenEyes.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In an ECS architecture, random number generation should be centralized through the World instance. " +
                     "This ensures deterministic behavior for replays, testing, and debugging. Each World maintains its " +
                     "own random state that can be seeded for reproducibility. Use world.NextInt(), world.NextFloat(), " +
                     "world.NextDouble(), or world.NextBool() instead of creating Random instances directly.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RandomUsageDetected);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Detect new Random() object creation
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);

        // Detect field declarations of type Random
        context.RegisterSymbolAction(AnalyzeFieldDeclaration, SymbolKind.Field);

        // Detect parameters of type Random
        context.RegisterSymbolAction(AnalyzeParameterDeclaration, SymbolKind.Parameter);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        var objectCreation = (IObjectCreationOperation)context.Operation;

        if (objectCreation.Type is INamedTypeSymbol typeSymbol &&
            typeSymbol.ToDisplayString() == SystemRandomType)
        {
            // Skip object creation in the World class itself (it legitimately uses Random)
            var containingType = GetContainingType(context.Operation);
            if (IsWorldClass(containingType))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                RandomUsageDetected,
                objectCreation.Syntax.GetLocation()));
        }
    }

    private static INamedTypeSymbol? GetContainingType(IOperation operation)
    {
        // Walk up the containing symbols to find the type
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

    private static void AnalyzeFieldDeclaration(SymbolAnalysisContext context)
    {
        var fieldSymbol = (IFieldSymbol)context.Symbol;

        // Skip compiler-generated fields
        if (fieldSymbol.IsImplicitlyDeclared)
        {
            return;
        }

        // Skip fields in the World class itself (it legitimately uses Random)
        if (IsWorldClass(fieldSymbol.ContainingType))
        {
            return;
        }

        if (fieldSymbol.Type.ToDisplayString() == SystemRandomType)
        {
            var location = fieldSymbol.Locations.FirstOrDefault();
            if (location != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RandomUsageDetected,
                    location));
            }
        }
    }

    private static void AnalyzeParameterDeclaration(SymbolAnalysisContext context)
    {
        var parameterSymbol = (IParameterSymbol)context.Symbol;

        // Skip parameters in the World class itself
        if (IsWorldClass(parameterSymbol.ContainingType))
        {
            return;
        }

        if (parameterSymbol.Type.ToDisplayString() == SystemRandomType)
        {
            var location = parameterSymbol.Locations.FirstOrDefault();
            if (location != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RandomUsageDetected,
                    location));
            }
        }
    }

    private static bool IsWorldClass(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        // Check if this is the KeenEyes.World class
        return typeSymbol.ToDisplayString() == "KeenEyes.World";
    }
}
