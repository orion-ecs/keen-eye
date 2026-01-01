using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that detects LINQ usage in hot paths and suggests avoiding allocations.
/// </summary>
/// <remarks>
/// <para>
/// In performance-critical code paths like <c>SystemBase.Update()</c>, LINQ operations can cause
/// heap allocations due to closures, iterators, and delegate allocations. This analyzer warns
/// when LINQ methods are used in methods that are automatically identified as hot paths or
/// explicitly marked with <c>[HotPath]</c>.
/// </para>
/// <para>
/// Hot paths are automatically detected in:
/// <list type="bullet">
/// <item><description><c>Update(float)</c> overrides in classes deriving from SystemBase</description></item>
/// <item><description><c>OnBeforeUpdate(float)</c> overrides in classes deriving from SystemBase</description></item>
/// <item><description><c>OnAfterUpdate(float)</c> overrides in classes deriving from SystemBase</description></item>
/// <item><description>Methods marked with <c>[HotPath]</c> attribute</description></item>
/// </list>
/// </para>
/// <para>
/// The analyzer detects common LINQ methods including:
/// <list type="bullet">
/// <item><description>Query operators: Select, Where, SelectMany, OrderBy, GroupBy, Join</description></item>
/// <item><description>Aggregation: Count, Sum, Average, Min, Max, Aggregate</description></item>
/// <item><description>Element access: First, FirstOrDefault, Single, Last, ElementAt</description></item>
/// <item><description>Quantifiers: Any, All, Contains</description></item>
/// <item><description>Materialization: ToList, ToArray, ToDictionary, ToHashSet</description></item>
/// <item><description>Query comprehension syntax: <c>from x in collection where ... select ...</c></description></item>
/// </list>
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LinqHotPathAnalyzer : DiagnosticAnalyzer
{
    private const string SystemBaseType = "KeenEyes.SystemBase";
    private const string HotPathAttribute = "KeenEyes.HotPathAttribute";

    /// <summary>
    /// LINQ methods that typically cause allocations.
    /// </summary>
    private static readonly ImmutableHashSet<string> linqMethods = ImmutableHashSet.Create(
        // Query operators
        "Select",
        "SelectMany",
        "Where",
        "OrderBy",
        "OrderByDescending",
        "ThenBy",
        "ThenByDescending",
        "GroupBy",
        "Join",
        "GroupJoin",
        "Distinct",
        "DistinctBy",
        "Union",
        "UnionBy",
        "Intersect",
        "IntersectBy",
        "Except",
        "ExceptBy",
        "Concat",
        "Zip",
        "Skip",
        "SkipWhile",
        "Take",
        "TakeWhile",
        "Reverse",
        "Chunk",
        "Prepend",
        "Append",

        // Aggregation
        "Count",
        "LongCount",
        "Sum",
        "Average",
        "Min",
        "MinBy",
        "Max",
        "MaxBy",
        "Aggregate",

        // Element access
        "First",
        "FirstOrDefault",
        "Single",
        "SingleOrDefault",
        "Last",
        "LastOrDefault",
        "ElementAt",
        "ElementAtOrDefault",

        // Quantifiers
        "Any",
        "All",
        "Contains",
        "SequenceEqual",

        // Materialization
        "ToList",
        "ToArray",
        "ToDictionary",
        "ToHashSet",
        "ToLookup",

        // Set operations that materialize
        "DefaultIfEmpty",
        "Cast",
        "OfType"
    );

    /// <summary>
    /// LINQ-related namespaces for type checking.
    /// </summary>
    private static readonly ImmutableHashSet<string> linqNamespaces = ImmutableHashSet.Create(
        "System.Linq",
        "System.Linq.Enumerable",
        "System.Linq.Queryable"
    );

    /// <summary>
    /// KEEN031: LINQ usage detected in hot path.
    /// </summary>
    public static readonly DiagnosticDescriptor LinqInHotPath = new(
        id: "KEEN031",
        title: "Avoid LINQ in hot paths",
        messageFormat: "LINQ method '{0}' used in hot path '{1}' may cause allocations; consider using a foreach loop or caching the result",
        category: "KeenEyes.Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "LINQ operations can cause heap allocations due to closures, iterators, and delegate allocations. " +
                     "In hot paths like Update() methods that run every frame, these allocations can cause GC pressure " +
                     "and performance degradation. Consider using foreach loops, caching query results, or pre-allocating " +
                     "collections instead.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(LinqInHotPath);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Detect method invocations (e.g., .Select(), .Where(), .ToList())
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);

        // Detect query comprehension syntax (e.g., from x in collection where x.Condition select x)
        context.RegisterSyntaxNodeAction(AnalyzeQueryExpression, SyntaxKind.QueryExpression);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Check if this is a LINQ method
        if (!IsLinqMethod(method))
        {
            return;
        }

        // Check if we're in a hot path (walk up all containing methods for nested lambdas)
        var hotPathReason = GetHotPathReasonFromEnclosingScopes(context.Operation);
        if (hotPathReason == null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            LinqInHotPath,
            invocation.Syntax.GetLocation(),
            method.Name,
            hotPathReason));
    }

    private static void AnalyzeQueryExpression(SyntaxNodeAnalysisContext context)
    {
        var queryExpression = (QueryExpressionSyntax)context.Node;

        // Check if we're in a hot path
        var hotPathReason = GetHotPathReasonFromSyntax(queryExpression, context.SemanticModel);
        if (hotPathReason == null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            LinqInHotPath,
            queryExpression.GetLocation(),
            "query comprehension",
            hotPathReason));
    }

    private static string? GetHotPathReasonFromSyntax(SyntaxNode node, SemanticModel? semanticModel)
    {
        if (semanticModel == null)
        {
            return null;
        }

        var symbol = semanticModel.GetEnclosingSymbol(node.SpanStart);
        while (symbol != null)
        {
            if (symbol is IMethodSymbol methodSymbol)
            {
                var reason = GetHotPathReason(methodSymbol);
                if (reason != null)
                {
                    return reason;
                }
            }

            symbol = symbol.ContainingSymbol;
        }

        return null;
    }

    private static string? GetHotPathReasonFromEnclosingScopes(IOperation operation)
    {
        var symbol = operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart);
        while (symbol != null)
        {
            if (symbol is IMethodSymbol methodSymbol)
            {
                var reason = GetHotPathReason(methodSymbol);
                if (reason != null)
                {
                    return reason;
                }
            }

            symbol = symbol.ContainingSymbol;
        }

        return null;
    }

    private static bool IsLinqMethod(IMethodSymbol method)
    {
        // Check if the method name is a known LINQ method
        if (!linqMethods.Contains(method.Name))
        {
            return false;
        }

        // Check multiple ways to identify LINQ methods since Roslyn represents
        // extension methods differently depending on how they're called

        // 1. Direct check on containing type
        if (IsLinqContainingType(method.ContainingType))
        {
            return true;
        }

        // 2. Check ReducedFrom for extension methods called in reduced form (e.g., arr.ToList())
        if (method.ReducedFrom != null && IsLinqContainingType(method.ReducedFrom.ContainingType))
        {
            return true;
        }

        // 3. Check OriginalDefinition for generic methods (e.g., ToList<T>)
        if (!SymbolEqualityComparer.Default.Equals(method.OriginalDefinition, method) &&
            IsLinqContainingType(method.OriginalDefinition.ContainingType))
        {
            return true;
        }

        // 4. Check ReducedFrom's OriginalDefinition for generic extension methods
        if (method.ReducedFrom?.OriginalDefinition != null &&
            IsLinqContainingType(method.ReducedFrom.OriginalDefinition.ContainingType))
        {
            return true;
        }

        return false;
    }

    private static bool IsLinqContainingType(INamedTypeSymbol? containingType)
    {
        if (containingType == null)
        {
            return false;
        }

        // Check the containing namespace directly
        var containingNamespace = GetFullNamespace(containingType.ContainingNamespace);
        if (containingNamespace == "System.Linq")
        {
            return true;
        }

        // Also check the full type name for types like System.Linq.Enumerable
        var fullName = containingType.ToDisplayString();
        return linqNamespaces.Any(ns => fullName.StartsWith(ns));
    }

    private static string GetFullNamespace(INamespaceSymbol? ns)
    {
        if (ns == null || ns.IsGlobalNamespace)
        {
            return string.Empty;
        }

        var parts = new System.Collections.Generic.List<string>();
        var current = ns;
        while (current != null && !current.IsGlobalNamespace)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingNamespace;
        }

        return string.Join(".", parts);
    }

    private static string? GetHotPathReason(IMethodSymbol method)
    {
        // Check for [HotPath] attribute
        var hotPathAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == HotPathAttribute);
        if (hotPathAttr != null)
        {
            // Get the Reason property if specified
            var reasonArg = hotPathAttr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Reason");
            if (reasonArg.Value.Value is string reason && !string.IsNullOrEmpty(reason))
            {
                return $"{method.Name} ({reason})";
            }

            return method.Name;
        }

        // Check if this is a known hot path method in SystemBase
        if (!IsSystemBaseMethod(method))
        {
            return null;
        }

        // Check for Update, OnBeforeUpdate, OnAfterUpdate overrides
        return method.Name switch
        {
            "Update" when IsUpdateOverride(method) => "Update",
            "OnBeforeUpdate" when IsLifecycleOverride(method, "OnBeforeUpdate") => "OnBeforeUpdate",
            "OnAfterUpdate" when IsLifecycleOverride(method, "OnAfterUpdate") => "OnAfterUpdate",
            _ => null
        };
    }

    private static bool IsSystemBaseMethod(IMethodSymbol method)
    {
        // Walk up the inheritance chain to find SystemBase
        var containingType = method.ContainingType;
        while (containingType != null)
        {
            if (containingType.ToDisplayString() == SystemBaseType)
            {
                return true;
            }

            containingType = containingType.BaseType;
        }

        return false;
    }

    private static bool IsUpdateOverride(IMethodSymbol method)
    {
        // Update method signature: public override void Update(float deltaTime)
        return method.IsOverride &&
               method.ReturnsVoid &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Single;
    }

    private static bool IsLifecycleOverride(IMethodSymbol method, string expectedName)
    {
        // Lifecycle method signature: protected override void OnBeforeUpdate/OnAfterUpdate(float deltaTime)
        return method.Name == expectedName &&
               method.IsOverride &&
               method.ReturnsVoid &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Single;
    }
}
