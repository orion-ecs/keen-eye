using KeenEyes.Graph.Kesl.Preview;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests.Preview;

/// <summary>
/// Tests for <see cref="ShaderExecutor"/>.
/// </summary>
public class ShaderExecutorTests
{
    private static readonly SourceLocation testLocation = new("test", 0, 0);

    #region Compile Tests

    [Fact]
    public void Compile_ValidGraph_ReturnsTrue()
    {
        using var builder = new TestGraphBuilder();
        var shader = builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);
        builder.CreateQueryBinding("Velocity", AccessMode.Read);

        var executor = new ShaderExecutor();
        var result = executor.Compile(builder.Canvas, builder.World);

        Assert.True(result);
        Assert.False(executor.HasError);
        Assert.Equal(2, executor.CurrentBindings.Count);
    }

    [Fact]
    public void Compile_EmptyGraph_ReturnsFalse()
    {
        using var builder = new TestGraphBuilder();
        // No nodes created

        var executor = new ShaderExecutor();
        var result = executor.Compile(builder.Canvas, builder.World);

        Assert.False(result);
        Assert.True(executor.HasError);
        Assert.NotEmpty(executor.LastError);
    }

    [Fact]
    public void Compile_ExtractsQueryBindings()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("PhysicsUpdate");
        builder.CreateQueryBinding("Position", AccessMode.Write);
        builder.CreateQueryBinding("Velocity", AccessMode.Read);
        builder.CreateQueryBinding("Frozen", AccessMode.Without);

        var executor = new ShaderExecutor();
        executor.Compile(builder.Canvas, builder.World);

        // Without bindings are still included in the AST
        Assert.Equal(3, executor.CurrentBindings.Count);
    }

    #endregion

    #region Execute Tests - Direct AST Interpretation

    [Fact]
    public void Execute_SimpleAssignment_SetsFieldValue()
    {
        // Test: Position.X = 5.0
        var entityManager = CreateEntityManagerWithPosition();
        var executor = CreateExecutorWithStatements(
            new AssignmentStatement(
                CreateMemberAccess("Position", "X"),
                new FloatLiteralExpression(5.0f, testLocation),
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(5.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_CompoundAssignment_AddsToField()
    {
        // Test: Position.X += 1.0
        var entityManager = CreateEntityManagerWithPosition();
        entityManager.Entities[0].Components["Position"].Fields["X"] = 10f;

        var executor = CreateExecutorWithStatements(
            new CompoundAssignmentStatement(
                CreateMemberAccess("Position", "X"),
                CompoundOperator.PlusEquals,
                new FloatLiteralExpression(1.0f, testLocation),
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(11.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_MultiplyAssignment_MultipliesField()
    {
        // Test: Position.X *= 2.0
        var entityManager = CreateEntityManagerWithPosition();
        entityManager.Entities[0].Components["Position"].Fields["X"] = 5f;

        var executor = CreateExecutorWithStatements(
            new CompoundAssignmentStatement(
                CreateMemberAccess("Position", "X"),
                CompoundOperator.StarEquals,
                new FloatLiteralExpression(2.0f, testLocation),
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(10.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_PhysicsPattern_UpdatesPositionWithVelocity()
    {
        // Test: Position.X += Velocity.X * deltaTime
        var entityManager = CreateEntityManagerWithPositionAndVelocity();
        entityManager.Entities[0].Components["Position"].Fields["X"] = 0f;
        entityManager.Entities[0].Components["Velocity"].Fields["X"] = 10f;

        var executor = CreateExecutorWithStatements(
            new CompoundAssignmentStatement(
                CreateMemberAccess("Position", "X"),
                CompoundOperator.PlusEquals,
                new BinaryExpression(
                    CreateMemberAccess("Velocity", "X"),
                    BinaryOperator.Multiply,
                    new IdentifierExpression("deltaTime", testLocation),
                    testLocation),
                testLocation));

        executor.Execute(entityManager, 1.0f); // 1 second

        Assert.Equal(10.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_IfStatement_TrueBranch()
    {
        // Test: if (true) { Position.X = 100.0 }
        var entityManager = CreateEntityManagerWithPosition();

        var executor = CreateExecutorWithStatements(
            new IfStatement(
                new BoolLiteralExpression(true, testLocation),
                new Statement[]
                {
                    new AssignmentStatement(
                        CreateMemberAccess("Position", "X"),
                        new FloatLiteralExpression(100.0f, testLocation),
                        testLocation)
                },
                null,
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(100.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_IfStatement_FalseBranch()
    {
        // Test: if (false) { ... } else { Position.X = 200.0 }
        var entityManager = CreateEntityManagerWithPosition();

        var executor = CreateExecutorWithStatements(
            new IfStatement(
                new BoolLiteralExpression(false, testLocation),
                new Statement[]
                {
                    new AssignmentStatement(
                        CreateMemberAccess("Position", "X"),
                        new FloatLiteralExpression(100.0f, testLocation),
                        testLocation)
                },
                new Statement[]
                {
                    new AssignmentStatement(
                        CreateMemberAccess("Position", "X"),
                        new FloatLiteralExpression(200.0f, testLocation),
                        testLocation)
                },
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(200.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_ForLoop_AccumulatesValues()
    {
        // Test: for i in 0..3 { Position.X += 1.0 }
        var entityManager = CreateEntityManagerWithPosition();
        entityManager.Entities[0].Components["Position"].Fields["X"] = 0f;

        var executor = CreateExecutorWithStatements(
            new ForStatement(
                "i",
                new IntLiteralExpression(0, testLocation),
                new IntLiteralExpression(3, testLocation),
                new Statement[]
                {
                    new CompoundAssignmentStatement(
                        CreateMemberAccess("Position", "X"),
                        CompoundOperator.PlusEquals,
                        new FloatLiteralExpression(1.0f, testLocation),
                        testLocation)
                },
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(3.0f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_BinaryExpressions_EvaluatesCorrectly()
    {
        // Test various binary operations
        var entityManager = CreateEntityManagerWithPosition();

        // Position.X = 10 + 5
        var executor1 = CreateExecutorWithStatements(
            new AssignmentStatement(
                CreateMemberAccess("Position", "X"),
                new BinaryExpression(
                    new FloatLiteralExpression(10f, testLocation),
                    BinaryOperator.Add,
                    new FloatLiteralExpression(5f, testLocation),
                    testLocation),
                testLocation));
        executor1.Execute(entityManager, 0.016f);
        Assert.Equal(15f, entityManager.Entities[0].Components["Position"].Fields["X"]);

        // Position.Y = 10 - 3
        var executor2 = CreateExecutorWithStatements(
            new AssignmentStatement(
                CreateMemberAccess("Position", "Y"),
                new BinaryExpression(
                    new FloatLiteralExpression(10f, testLocation),
                    BinaryOperator.Subtract,
                    new FloatLiteralExpression(3f, testLocation),
                    testLocation),
                testLocation));
        executor2.Execute(entityManager, 0.016f);
        Assert.Equal(7f, entityManager.Entities[0].Components["Position"].Fields["Y"]);
    }

    [Fact]
    public void Execute_MathFunctions_EvaluatesCorrectly()
    {
        var entityManager = CreateEntityManagerWithPosition();

        // Position.X = abs(-5)
        var executor = CreateExecutorWithStatements(
            new AssignmentStatement(
                CreateMemberAccess("Position", "X"),
                new CallExpression(
                    "abs",
                    new Expression[] { new FloatLiteralExpression(-5f, testLocation) },
                    testLocation),
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(5f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_ClampFunction_ClampsValue()
    {
        var entityManager = CreateEntityManagerWithPosition();

        // Position.X = clamp(15, 0, 10)
        var executor = CreateExecutorWithStatements(
            new AssignmentStatement(
                CreateMemberAccess("Position", "X"),
                new CallExpression(
                    "clamp",
                    new Expression[]
                    {
                        new FloatLiteralExpression(15f, testLocation),
                        new FloatLiteralExpression(0f, testLocation),
                        new FloatLiteralExpression(10f, testLocation)
                    },
                    testLocation),
                testLocation));

        executor.Execute(entityManager, 0.016f);

        Assert.Equal(10f, entityManager.Entities[0].Components["Position"].Fields["X"]);
    }

    [Fact]
    public void Execute_AllEntities_ProcessesEach()
    {
        var entityManager = CreateEntityManagerWithPosition();

        // Set different initial values
        entityManager.Entities[0].Components["Position"].Fields["X"] = 0f;
        entityManager.Entities[1].Components["Position"].Fields["X"] = 0f;
        entityManager.Entities[2].Components["Position"].Fields["X"] = 0f;

        // Position.X += 10
        var executor = CreateExecutorWithStatements(
            new CompoundAssignmentStatement(
                CreateMemberAccess("Position", "X"),
                CompoundOperator.PlusEquals,
                new FloatLiteralExpression(10f, testLocation),
                testLocation));

        executor.Execute(entityManager, 0.016f);

        // All entities should have been updated
        Assert.Equal(10f, entityManager.Entities[0].Components["Position"].Fields["X"]);
        Assert.Equal(10f, entityManager.Entities[1].Components["Position"].Fields["X"]);
        Assert.Equal(10f, entityManager.Entities[2].Components["Position"].Fields["X"]);
    }

    #endregion

    #region Helpers

    private static MemberAccessExpression CreateMemberAccess(string obj, string member)
    {
        return new MemberAccessExpression(
            new IdentifierExpression(obj, testLocation),
            member,
            testLocation);
    }

    private static PreviewEntityManager CreateEntityManagerWithPosition()
    {
        var manager = new PreviewEntityManager();
        manager.RebuildFromBindings(new[]
        {
            new QueryBinding(AccessMode.Write, "Position", testLocation)
        });
        return manager;
    }

    private static PreviewEntityManager CreateEntityManagerWithPositionAndVelocity()
    {
        var manager = new PreviewEntityManager();
        manager.RebuildFromBindings(new[]
        {
            new QueryBinding(AccessMode.Write, "Position", testLocation),
            new QueryBinding(AccessMode.Read, "Velocity", testLocation)
        });
        return manager;
    }

    private static ShaderExecutor CreateExecutorWithStatements(params Statement[] statements)
    {
        var executor = new ShaderExecutor();

        // Set up a minimal AST
        var executeBlock = new ExecuteBlock(statements, testLocation);
        var queryBlock = new QueryBlock(Array.Empty<QueryBinding>(), testLocation);
        var declaration = new ComputeDeclaration(
            "Test",
            queryBlock,
            null,
            executeBlock,
            testLocation);

        // Use reflection to set the private field for testing
        var astField = typeof(ShaderExecutor).GetField("currentAst",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        astField?.SetValue(executor, declaration);

        return executor;
    }

    #endregion
}
