using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.Tests;

public class ParserTests
{
    private static SourceFile Parse(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var result = parser.Parse();

        if (parser.HasErrors)
        {
            throw new Exception($"Parse errors: {string.Join(", ", parser.Errors)}");
        }

        return result;
    }

    [Fact]
    public void Parse_EmptySource_ReturnsEmptySourceFile()
    {
        var result = Parse("");

        Assert.Empty(result.Declarations);
    }

    [Fact]
    public void Parse_ComponentDeclaration_ParsesCorrectly()
    {
        var source = @"
            component Position {
                x: float
                y: float
                z: float
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var component = Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.Equal("Position", component.Name);
        Assert.Equal(3, component.Fields.Count);
        Assert.Equal("x", component.Fields[0].Name);
        Assert.Equal("y", component.Fields[1].Name);
        Assert.Equal("z", component.Fields[2].Name);
    }

    [Fact]
    public void Parse_ComputeShader_ParsesQueryBlock()
    {
        var source = @"
            compute UpdatePhysics {
                query {
                    write Position
                    read Velocity
                    without Frozen
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        Assert.Equal("UpdatePhysics", compute.Name);

        Assert.Equal(3, compute.Query.Bindings.Count);
        Assert.Equal(AccessMode.Write, compute.Query.Bindings[0].AccessMode);
        Assert.Equal("Position", compute.Query.Bindings[0].ComponentName);
        Assert.Equal(AccessMode.Read, compute.Query.Bindings[1].AccessMode);
        Assert.Equal("Velocity", compute.Query.Bindings[1].ComponentName);
        Assert.Equal(AccessMode.Without, compute.Query.Bindings[2].AccessMode);
        Assert.Equal("Frozen", compute.Query.Bindings[2].ComponentName);
    }

    [Fact]
    public void Parse_ComputeShader_ParsesParamsBlock()
    {
        var source = @"
            compute UpdatePhysics {
                query {
                    write Position
                }
                params {
                    deltaTime: float
                    gravity: float
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);

        Assert.NotNull(compute.Params);
        Assert.Equal(2, compute.Params.Parameters.Count);
        Assert.Equal("deltaTime", compute.Params.Parameters[0].Name);
        Assert.Equal("gravity", compute.Params.Parameters[1].Name);
    }

    [Fact]
    public void Parse_ComputeShader_ParsesExecuteBlock()
    {
        var source = @"
            compute UpdatePhysics {
                query {
                    write Position
                    read Velocity
                }
                params {
                    deltaTime: float
                }
                execute() {
                    Position.x += Velocity.x * deltaTime;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        Assert.Single(compute.Execute.Body);

        var stmt = Assert.IsType<CompoundAssignmentStatement>(compute.Execute.Body[0]);
        Assert.Equal(CompoundOperator.PlusEquals, stmt.Operator);
    }

    [Fact]
    public void Parse_IfStatement_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    if (Position.x > 0) {
                        Position.x = 0;
                    } else {
                        Position.x = 1;
                    }
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var ifStmt = Assert.IsType<IfStatement>(compute.Execute.Body[0]);

        Assert.Single(ifStmt.ThenBranch);
        Assert.NotNull(ifStmt.ElseBranch);
        Assert.Single(ifStmt.ElseBranch);
    }

    [Fact]
    public void Parse_ForStatement_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    for (i: 0..10) {
                        Position.x = Position.x + 1;
                    }
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var forStmt = Assert.IsType<ForStatement>(compute.Execute.Body[0]);

        Assert.Equal("i", forStmt.VariableName);
        Assert.IsType<IntLiteralExpression>(forStmt.Start);
        Assert.IsType<IntLiteralExpression>(forStmt.End);
    }

    [Fact]
    public void Parse_BinaryExpression_ParsesWithCorrectPrecedence()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = 1 + 2 * 3;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);
        var binary = Assert.IsType<BinaryExpression>(assignStmt.Value);

        // Should be parsed as 1 + (2 * 3)
        Assert.Equal(BinaryOperator.Add, binary.Operator);
        Assert.IsType<IntLiteralExpression>(binary.Left);
        var right = Assert.IsType<BinaryExpression>(binary.Right);
        Assert.Equal(BinaryOperator.Multiply, right.Operator);
    }

    [Fact]
    public void Parse_UnaryExpression_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = -Position.y;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);
        var unary = Assert.IsType<UnaryExpression>(assignStmt.Value);

        Assert.Equal(UnaryOperator.Negate, unary.Operator);
    }

    [Fact]
    public void Parse_FunctionCall_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = sqrt(Position.y);
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);
        var call = Assert.IsType<CallExpression>(assignStmt.Value);

        Assert.Equal("sqrt", call.FunctionName);
        Assert.Single(call.Arguments);
    }

    [Fact]
    public void Parse_MemberAccess_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = Position.y;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);

        var target = Assert.IsType<MemberAccessExpression>(assignStmt.Target);
        Assert.Equal("x", target.MemberName);

        var value = Assert.IsType<MemberAccessExpression>(assignStmt.Value);
        Assert.Equal("y", value.MemberName);
    }
}
