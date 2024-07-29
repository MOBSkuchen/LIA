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
        Errors.ThrowCodeError(GenCodeLocationForLoc(startPos, endPos), message, ErrorCodes.SyntaxError);
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

    private CodeLocation GenCodeLocationForLoc(int startPos, int endPos) => new(startPos, endPos, CodeFile);
    private CodeLocation GenCodeLocationForToken(Token token) => new (token.StartPos, token.EndPos, CodeFile);
    private CodeLocation GenCodeLocationForCurrent(bool isEof) => !isEof ? GenCodeLocationForToken(CurrentToken) : new CodeLocation(PreviousToken.EndPos, PreviousToken.EndPos, CodeFile);

    private void GenCodeError(string message, ErrorCodes errNum, bool isEof=false) => Errors.ThrowCodeError(GenCodeLocationForCurrent(isEof), message, errNum);

    private int GetOperatorPrecedence(TokenType type)
    {
        switch (type)
        {
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
        if (left == null) ThrowSyntaxError(PreviousToken.StartPos, PreviousToken.EndPos, $"Expected an expression after this but got nothing");

        while (true)
        {
            TokenType? currentOp = CurrentToken?.Type;
            if (currentOp == null || GetOperatorPrecedence(currentOp.Value) <= precedence)
            {
                break;
            }

            Advance(); // Move past the operator
            int nextPrecedence = GetOperatorPrecedence(currentOp.Value);
            Expr? right = ParseExpression(nextPrecedence);
            if (right == null)
            {
                ThrowInvalidTokenError(TokenType.ExpressionLike, CurrentToken.Type, "Missing right-side expression");
            }

            left = new BinaryExpr(left, currentOp.Value, right, left.StartPos, right.EndPos);
        }

        return left;
    }

    private Expr? ParseUnary()
    {
        if (Match(TokenType.Plus, TokenType.Minus))
        {
            TokenType unaryOp = PreviousToken.Type;
            Expr? operand = ParseUnary();
            if (operand == null)
            {
                ThrowInvalidTokenError(TokenType.Operand, CurrentToken.Type, "Missing operand");
            }
            return new UnaryExpr(unaryOp, operand, PreviousToken.StartPos, operand.EndPos);
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
            if (!long.TryParse(PreviousToken.Content, out var num))
            {
                if (!double.TryParse(PreviousToken.Content, out var flt)) throw new Exception("Lexer fucked up");
                return new FloatExpr(flt, PreviousToken.StartPos, PreviousToken.EndPos);
            }
            return new IntegerExpr(num, PreviousToken.StartPos, PreviousToken.EndPos);
        }
        if (Match(TokenType.Identifier))
        {
            var ident = new IdentifierExpr(PreviousToken.Content, PreviousToken.StartPos, PreviousToken.EndPos);
            if (Match(TokenType.OpenParen))
            {
                return new FunctionCallExpr(ident, ParseArguments(), ident.StartPos, CurrentToken.EndPos);
            }
            return ident;
        }

        if (Match(TokenType.String))
        {
            return new StringExpr(PreviousToken.Content, PreviousToken.StartPos, PreviousToken.EndPos);
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
        int startPos = PreviousToken.StartPos;
        var toReturn = ParseExpression();
        return new ReturnStmt(toReturn, startPos, PreviousToken.EndPos);
    }

    private WhileLoop ParseWhile()
    {
        int startPos = PreviousToken.StartPos;
        var condition = ParseExpression();
        Consume(TokenType.Colon, "Missing body");
        var body = ParseBody();
        return new WhileLoop(condition, body, startPos, CurrentToken.EndPos);
    }

    private IfStmt ParseIf()
    {
        List<(Expr, BlockStmt)> elifBranches = new List<(Expr, BlockStmt)>();
        BlockStmt? elseBranch = null;
        int startPos = PreviousToken!.StartPos;
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
        return new IfStmt(condition!, body, elseBranch, elifBranches, startPos, CurrentToken!.EndPos);
    }

    private AssignmentStmt ParseAssignment()
    {
        IdentifierExpr name = new IdentifierExpr(PreviousToken!.Content, PreviousToken.StartPos, PreviousToken.EndPos);
        IdentifierExpr? type = null;
        if (Match(TokenType.Colon))
        {
            var token = Consume(TokenType.Identifier, "Expected a type annotation due to previous colon, " +
                                                      "which means that a type (identifier) must follow");
            type = new IdentifierExpr(token.Content, token.StartPos, token.EndPos);
        }

        if (Match(TokenType.Equals))
        {
            var expr = ParseExpression();
            return new AssignmentStmt(name, expr, type, name.StartPos, CurrentToken!.EndPos);
        } 
        if (type == null) ThrowSyntaxError(name.StartPos, name.EndPos, $"Expected a value or type annotation for the declared variable '{name.Name}'");
        else return new AssignmentStmt(name, null, type, name.StartPos, CurrentToken!.EndPos);
        return null;
    }

    private Stmt? ParseStatement()
    {
        int startPos = CurrentToken.StartPos;
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
                TokenType.OpenParen)) return new ExprStmt(ParseExpression()!, startPos, CurrentToken.EndPos);

        ThrowInvalidTokenError(TokenType.Statement, CurrentToken.Type, "Expected a statement");
        return null;
    }

    private BlockStmt ParseBody()
    {
        List<Stmt> statements = new List<Stmt>();
        int startPos = CurrentToken!.StartPos;
        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt == null) continue;
            statements.Add(stmt);
            if (Match(TokenType.Semicolon)) return new BlockStmt(statements, startPos, PreviousToken!.EndPos);
        }

        ThrowInvalidTokenError(TokenType.Semicolon, CurrentToken.Type, "Expected a closing semicolon");
        return null;
    }

    public IdentifierExpr CreateIdent(Token token) =>
        new (token.Content, token.StartPos, token.EndPos);
    
    private List<ParameterExpr> ParseParameters()
    {
        List<ParameterExpr> parameters = new List<ParameterExpr>();
        while (!IsAtEnd())
        {
            var name = Consume(TokenType.Identifier, 
                "Expected a parameter declaration or closing parenthesis");
            Consume(TokenType.Colon, "Expected a colon here");
            var type = Consume(TokenType.Identifier, "Parameters must have types");
            parameters.Add(new ParameterExpr(CreateIdent(name), CreateIdent(type), name.StartPos, type.EndPos));
            if (Match(TokenType.CloseParen)) break;
            if (Match(TokenType.Comma)) continue;
            ThrowInvalidTokenError(TokenType.CloseParen, CurrentToken.Type, "Expected a closing parenthesis");
        }

        return parameters;
    }
    
    private FunctionDecl ParseFunction()
    {
        int startPos = PreviousToken.StartPos;
        bool isPublic = Match(TokenType.Public);
        if (!isPublic) Consume(TokenType.Private, "Must declare public or private function");
        var type = Consume(TokenType.Identifier, "Must declare a return type");
        var name = Consume(TokenType.Identifier, "Must declare the name");
        List<ParameterExpr> parameters = new List<ParameterExpr>();
        if (Match(TokenType.OpenParen)) parameters = ParseParameters();
        Consume(TokenType.Colon, "Expected colon after function-head declaration");
        var body = ParseBody();
        return new FunctionDecl(CreateIdent(name), CreateIdent(type), parameters, body, isPublic, startPos, CurrentToken.EndPos);
    }

    private ClassDecl ParseClass()
    {
        int startPos = PreviousToken.StartPos;
        bool isPublic = Match(TokenType.Public);
        if (!isPublic) Consume(TokenType.Private, "Must declare public or private class");
        var name = Consume(TokenType.Identifier, "Must declare the name").Content;
        Consume(TokenType.Colon, "Expected colon after class-head declaration");
        List<FunctionDecl> methods = new List<FunctionDecl>();
        while (!IsAtEnd())
        {
            if (Match(TokenType.Def))
            {
                methods.Add(ParseFunction());
            }
            else if (Match(TokenType.Semicolon))
                return new ClassDecl(name, isPublic, methods, startPos, PreviousToken.EndPos);
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
                statements.Add(new NamespaceDecl(curTok.Content, PreviousToken!.StartPos, CurrentToken!.EndPos));
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