namespace LIA;

public class Parser(Lexer lexer)
{
    private CodeFile CodeFile = lexer.CodeFile;
    private List<Token> _tokens = lexer.Tokens;
    private int _current = 0;

    private Token? CurrentToken => _current < _tokens.Count ? _tokens[_current] : 
        ThrowInvalidTokenError(TokenType.Token, TokenType.EndOfFile, "Expected more", true);
    private Token? PreviousToken => _current > 0 ? _tokens[_current - 1] : null;

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return PreviousToken;
    }

    private bool IsAtEnd() => _current >= _tokens.Count;

    public Token? ThrowInvalidTokenError(TokenType expected, TokenType? got, string? context, bool isEof = false)
    {
        string message = $"Expected {expected.ToString()}";
        if (got != null) message += $", but got {got.ToString()}";
        message += "!";
        if (context != null) message += " " + context + "!";
        GenCodeError(message, ((!isEof) ? ErrorCodes.InvalidToken : ErrorCodes.EndOfFile), isEof);
        return null;
    }

    public void ThrowSyntaxError(int startPos, int endPos, string message)
    {
        Errors.ThrowCodeError(new CodeLocation(startPos, endPos, lexer.CodeFile), message, ErrorCodes.SyntaxError);
    } 

    private Token Consume(TokenType type, string? context)
    {
        if (Check(type)) return Advance();
        ThrowInvalidTokenError(type, CurrentToken.Type, context);
        return null;
    }

    private bool Check(TokenType type) => !IsAtEnd() && CurrentToken.Type == type;

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    public CodeLocation MergeCodeLocation(CodeLocation startTok, CodeLocation endTok) => new CodeLocation(
        startTok.StartPosition, endTok.StartPosition, endTok.CodeFile);
    
    private void GenCodeError(string message, ErrorCodes errNum, bool isEof=false) => Errors.ThrowCodeError(!isEof ? CurrentToken!.CodeLocation : PreviousToken!.CodeLocation, message, errNum);

    private int GetOperatorPrecedence(TokenType type)
    {
        switch (type)
        {
            case TokenType.As:
            case TokenType.Plus:
            case TokenType.Minus:
                return 2;
            case TokenType.Star:
            case TokenType.Slash:
                return 3;
            case TokenType.GreaterThan:
            case TokenType.LessThan:
            case TokenType.GreaterThanEquals:
            case TokenType.LessThanEquals:
                return 4;
            case TokenType.DoubleEquals:
                return 1;
            case TokenType.And:
                return 5;
            case TokenType.Or:
                return 7;
            default:
                return 0;  // Lowest precedence
        }
    }

    private Expr? ParseExpression(int precedence = 0)
    {
        Expr? left = ParseUnary();
        if (left == null) ThrowSyntaxError(PreviousToken!.CodeLocation.StartPosition, PreviousToken.CodeLocation.EndPosition, "Expected an expression after this but got nothing");

        while (true)
        {
            TokenType? currentOp = CurrentToken?.Type;
            if (currentOp == null || GetOperatorPrecedence(currentOp.Value) <= precedence)
            {
                break;
            }

            Advance(); // Move past the operator
            if (currentOp == TokenType.As)
            {
                Consume(TokenType.Identifier, $"Expected a type for a Cast-Expression");
                var typeTok = new IdentifierExpr(PreviousToken!.Content, PreviousToken.CodeLocation);
                return new CastExpr(left!, typeTok, MergeCodeLocation(left!.CodeLocation, typeTok.CodeLocation));
            }
            int nextPrecedence = GetOperatorPrecedence(currentOp.Value);
            Expr? right = ParseExpression(nextPrecedence);
            if (right == null)
            {
                ThrowInvalidTokenError(TokenType.ExpressionLike, CurrentToken!.Type, "Missing right-side expression");
            }

            left = new BinaryExpr(left!, currentOp.Value, right!, MergeCodeLocation(left!.CodeLocation, right!.CodeLocation));
        }

        return left;
    }

    private Expr? ParseUnary()
    {
        if (Match(TokenType.Plus, TokenType.Minus))
        {
            TokenType unaryOp = PreviousToken!.Type;
            Expr? operand = ParseUnary();
            if (operand == null)
            {
                ThrowInvalidTokenError(TokenType.Operand, CurrentToken!.Type, "Missing operand");
            }
            return new UnaryExpr(unaryOp, operand!, MergeCodeLocation(PreviousToken.CodeLocation, operand!.CodeLocation));
        }

        return ParsePrimary();
    }

    private List<Expr>? ParseArguments()
    {
        List<Expr>? arguments = new List<Expr>();
        while (!IsAtEnd())
        {
            var arg = ParseExpression();
            arguments.Add(arg);
            if (Match(TokenType.Comma)) continue;
            Consume(TokenType.CloseParen, "Expected a closing Parenthesis");
            break;
        }

        if (arguments.Count == 0) return null; 

        return arguments;
    }

    private Expr? ParsePrimary()
    {
        if (Match(TokenType.Number))
        {
            if (!long.TryParse(PreviousToken!.Content, out var num))
            {
                if (!double.TryParse(PreviousToken.Content, out var flt)) throw new Exception("Lexer fucked up");
                return new FloatExpr(flt, PreviousToken.CodeLocation);
            }
            return new IntegerExpr(num, PreviousToken.CodeLocation);
        }
        if (Match(TokenType.Identifier))
        {
            var ident = new IdentifierExpr(PreviousToken!.Content, PreviousToken.CodeLocation);
            if (Match(TokenType.OpenParen))
            {
                return new FunctionCallExpr(ident, ParseArguments(), MergeCodeLocation(ident.CodeLocation, CurrentToken!.CodeLocation));
            }
            return ident;
        }

        if (Match(TokenType.String))
        {
            return new StringExpr(PreviousToken!.Content, PreviousToken.CodeLocation);
        }
        
        if (Match(TokenType.OpenParen))
        {
            Expr? expr = ParseExpression();
            Consume(TokenType.CloseParen, "Expected a closing parenthesis");
            return expr;
        }

        return null;
    }

    private ReturnStmt ParseReturn()
    {
        var startPos = PreviousToken.CodeLocation;
        var toReturn = ParseExpression();
        return new ReturnStmt(toReturn!, MergeCodeLocation(startPos, PreviousToken.CodeLocation));
    }

    private WhileLoop ParseWhile()
    {
        var startPos = PreviousToken!.CodeLocation;
        var condition = ParseExpression();
        Consume(TokenType.Colon, "Missing body");
        var body = ParseBody();
        return new WhileLoop(condition!, body, MergeCodeLocation(startPos, CurrentToken!.CodeLocation));
    }

    private IfStmt ParseIf()
    {
        List<(Expr, BlockStmt)> elifBranches = new List<(Expr, BlockStmt)>();
        BlockStmt? elseBranch = null;
        var startPos = PreviousToken!.CodeLocation;
        var condition = ParseExpression();
        Consume(TokenType.Colon, "Missing body");
        var body = ParseBody();
        while (Match(TokenType.Elif))
        {
            var elifCondition = ParseExpression();
            Consume(TokenType.Colon, "Missing body");
            elifBranches.Add((elifCondition!, ParseBody()));
        }
        if (elifBranches.Count == 0) elifBranches = null;
        if (Match(TokenType.Else))
        {
            Consume(TokenType.Colon, "Missing body");
            elseBranch = ParseBody();
        }
        return new IfStmt(condition!, body, elseBranch, elifBranches, MergeCodeLocation(startPos, CurrentToken!.CodeLocation));
    }

    private AssignmentStmt ParseAssignment()
    {
        IdentifierExpr name = new IdentifierExpr(PreviousToken!.Content, PreviousToken.CodeLocation);
        IdentifierExpr? type = null;
        if (Match(TokenType.Colon))
        {
            var token = Consume(TokenType.Identifier, "Expected a type annotation due to previous colon, " +
                                                      "which means that a type (identifier) must follow");
            type = new IdentifierExpr(token.Content, token.CodeLocation);
        }

        if (Match(TokenType.Equals))
        {
            var expr = ParseExpression();
            return new AssignmentStmt(name, expr, type, MergeCodeLocation(name.CodeLocation, CurrentToken!.CodeLocation));
        } 
        if (type == null) ThrowSyntaxError(name.StartPos, name.EndPos, $"Expected a value or type annotation for the declared variable '{name.Name}'");
        else return new AssignmentStmt(name, null, type, MergeCodeLocation(name.CodeLocation, CurrentToken!.CodeLocation));
        return null;
    }

    private Stmt? ParseStatement()
    {
        var startPos = CurrentToken.CodeLocation;
        if (Match(TokenType.Return)) return ParseReturn();
        if (Match(TokenType.While)) return ParseWhile();
        if (Match(TokenType.If)) return ParseIf();
        if (Match(TokenType.Identifier) && Match(TokenType.Colon, TokenType.Equals))
        {
            _current--;
            return ParseAssignment();
        }

        if (Match(TokenType.Pause)) return null;

        if (Match(TokenType.Identifier, TokenType.Number,
                TokenType.ExclamationMark, TokenType.Minus, TokenType.String,
                TokenType.OpenParen)) return new ExprStmt(ParseExpression()!, MergeCodeLocation(startPos, CurrentToken.CodeLocation));

        ThrowInvalidTokenError(TokenType.Statement, CurrentToken.Type, "Expected a statement");
        return null;
    }

    private BlockStmt ParseBody()
    {
        List<Stmt> statements = new List<Stmt>();
        var startPos = CurrentToken!.CodeLocation;
        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt == null) continue;
            statements.Add(stmt);
            if (Match(TokenType.Semicolon)) return new BlockStmt(statements, MergeCodeLocation(startPos, PreviousToken!.CodeLocation));
        }

        ThrowInvalidTokenError(TokenType.Semicolon, CurrentToken.Type, "Expected a closing semicolon");
        return null;
    }

    public IdentifierExpr CreateIdent(Token token) =>
        new (token.Content, token.CodeLocation);
    
    private List<ParameterExpr> ParseParameters()
    {
        List<ParameterExpr> parameters = new List<ParameterExpr>();
        while (!IsAtEnd())
        {
            if (Match(TokenType.CloseParen)) break;
            var name = Consume(TokenType.Identifier, 
                "Expected a parameter declaration or closing parenthesis");
            Consume(TokenType.Colon, "Expected a colon here");
            var type = Consume(TokenType.Identifier, "Parameters must have types");
            parameters.Add(new ParameterExpr(CreateIdent(name), CreateIdent(type), MergeCodeLocation(name.CodeLocation, type.CodeLocation)));
            if (Match(TokenType.Comma)) continue;
            if (Match(TokenType.CloseParen)) break;
            ThrowInvalidTokenError(TokenType.CloseParen, CurrentToken!.Type, "Expected a closing parenthesis");
        }

        return parameters;
    }
    
    private FunctionDecl ParseFunction()
    {
        var startPos = PreviousToken!.CodeLocation;
        bool isPublic = Match(TokenType.Public);
        bool isClass = Match(TokenType.Class);
        bool isStatic = !isClass && Match(TokenType.Static);
        if (!isPublic) Consume(TokenType.Private, "Must declare public or private function");
        var type = Consume(TokenType.Identifier, "Must declare a return type");
        var name = Consume(TokenType.Identifier, "Must declare the name");
        List<ParameterExpr> parameters = new List<ParameterExpr>();
        if (Match(TokenType.OpenParen)) parameters = ParseParameters();
        Consume(TokenType.Colon, "Expected colon after function-head declaration");
        var body = ParseBody();
        return new FunctionDecl(CreateIdent(name), CreateIdent(type), parameters, body, isPublic, isStatic, isClass, MergeCodeLocation(startPos, CurrentToken!.CodeLocation));
    }

    private FieldStmt ParseField()
    {
        var startPos = PreviousToken!.CodeLocation;
        bool isPublic = Match(TokenType.Public);
        bool isStatic = Match(TokenType.Static);
        if (!isPublic) Consume(TokenType.Private, "Must declare public or private field");
        var type = CreateIdent(Consume(TokenType.Identifier, "Must declare a field type"));
        var name = CreateIdent(Consume(TokenType.Identifier, "Must declare the name"));
        Expr? value = Match(TokenType.Equals) ? ParseExpression() : null;
        return new FieldStmt(name, type, isStatic, isPublic, value, MergeCodeLocation(startPos, PreviousToken.CodeLocation));
    }

    private ClassDecl ParseClass()
    {
        var startPos = PreviousToken!.CodeLocation;
        bool isPublic = Match(TokenType.Public);
        if (!isPublic) Consume(TokenType.Private, "Must declare public or private class");
        var name = Consume(TokenType.Identifier, "Must declare the name").Content;
        Consume(TokenType.Colon, "Expected colon after class-head declaration");
        List<FunctionDecl> methods = new List<FunctionDecl>();
        List<FieldStmt> fields = new List<FieldStmt>();
        while (!IsAtEnd())
        {
            if (Match(TokenType.Def))
            {
                methods.Add(ParseFunction());
            } else if (Match(TokenType.Field))
            {
                fields.Add(ParseField());
            }
            else if (Match(TokenType.Semicolon))
                return new ClassDecl(name, isPublic, methods, fields, MergeCodeLocation(startPos, PreviousToken.CodeLocation));
            else break;
        }
        ThrowInvalidTokenError(TokenType.ClassLevel, CurrentToken!.Type, "For example a function definition or terminating semicolon");
        return null;
    }

    public List<Stmt> ParseTopLevel()
    {
        List<Stmt> statements = new List<Stmt>();
        while (!IsAtEnd())
        {
            if (Match(TokenType.Namespace))
            {
                var curTok = Consume(TokenType.Identifier, "Namespace must have a name");
                statements.Add(new NamespaceDecl(curTok.Content, MergeCodeLocation(PreviousToken!.CodeLocation, CurrentToken!.CodeLocation)));
            }
            else if (Match(TokenType.Class))
            {
                statements.Add(ParseClass());
            }
            else ThrowInvalidTokenError(TokenType.TopLevel, CurrentToken!.Type, "For example a namespace declaration or class definition");
        }

        return statements;
    }
}