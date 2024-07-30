namespace LIA
{
    // Base AST Node
    public abstract class AstNode(int startPos, int endPos)
    {
        public int StartPos { get; } = startPos;
        public int EndPos { get; } = endPos;
    }

    // Expression Nodes
    public abstract class Expr(int startPos, int endPos) : AstNode(startPos, endPos);

    public class BinaryExpr(Expr left, TokenType @operator, Expr right, int startPos, int endPos)
        : Expr(startPos, endPos)
    {
        public Expr Left { get; } = left;
        public Expr Right { get; } = right;
        public TokenType Operator { get; } = @operator;
    }

    public class UnaryExpr(TokenType @operator, Expr operand, int startPos, int endPos)
        : Expr(startPos, endPos)
    {
        public Expr Operand { get; } = operand;
        public TokenType Operator { get; } = @operator;
    }

    public class IntegerExpr(long number, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public long Value { get; } = number;
    }
    
    public class FloatExpr(double number, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public double Value { get; } = number;
    }

    public class StringExpr(string value, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public string Value { get; } = value;
    }

    public class FunctionCallExpr(IdentifierExpr name, List<Expr>? arguments, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public IdentifierExpr Name { get; } = name;
        public List<Expr>? Arguments { get; } = arguments;
    }

    public class IdentifierExpr(string name, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public string Name { get; } = name;
    }

    public class AssignmentStmt(IdentifierExpr name, Expr? value, IdentifierExpr type, int startPos, int endPos)
        : Stmt(startPos, endPos)
    {
        public IdentifierExpr Name { get; } = name;
        public IdentifierExpr? Type { get; set; } = type;
        public Expr? Value { get; } = value;
    }

    // Statement Nodes
    public abstract class Stmt(int startPos, int endPos) : AstNode(startPos, endPos);

    public class ExprStmt(Expr expression, int startPos, int endPos) : Stmt(startPos, endPos)
    {
        public Expr Expression { get; } = expression;
    }

    public class ReturnStmt(Expr value, int startPos, int endPos) : Stmt(startPos, endPos)
    {
        public Expr Value { get; } = value;
    }

    public class IfStmt(
        Expr condition,
        BlockStmt thenBranch,
        BlockStmt? elseBranch,
        List<(Expr, BlockStmt)>? elifBranches,
        int startPos,
        int endPos)
        : Stmt(startPos, endPos)
    {
        public Expr Condition { get; } = condition;
        public BlockStmt ThenBranch { get; } = thenBranch;
        public List<(Expr, BlockStmt)>? ElifBranches { get; } = elifBranches;
        public BlockStmt? ElseBranch { get; } = elseBranch;
    }

    // Block Statement
    public class BlockStmt(List<Stmt> statements, int startPos, int endPos) : Stmt(startPos, endPos)
    {
        public List<Stmt> Statements { get; } = statements;
    }
    
    // While loop
    public class WhileLoop(
        Expr condition,
        BlockStmt body,
        int startPos,
        int endPos) : Stmt(startPos, endPos)
    {
        public Expr Condition { get; } = condition;
        public BlockStmt Body { get; } = body;
    }

    public class ParameterExpr(IdentifierExpr name, IdentifierExpr type, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public IdentifierExpr Name = name;
        public IdentifierExpr Type = type;
    }

    public class CastExpr(Expr prevExpr, IdentifierExpr destType, int startPos, int endPos) : Expr(startPos, endPos)
    {
        public Expr PrevExpr = prevExpr;
        public IdentifierExpr DestType = destType;
    }

    // Function Declaration
    public class FunctionDecl(
        IdentifierExpr name,
        IdentifierExpr returnType,
        List<ParameterExpr> parameters,
        BlockStmt body,
        bool @public,
        bool @static,
        bool @class,
        int startPos,
        int endPos)
        : Stmt(startPos, endPos)
    {
        public IdentifierExpr Name { get; } = name;
        public IdentifierExpr ReturnType { get; } = returnType;
        public readonly bool Public = @public;
        public readonly bool Static = @static;
        public readonly bool Class = @class;
        public List<ParameterExpr> Parameters { get; } = parameters;
        public BlockStmt Body { get; } = body;
    }

    // Class Declaration
    public class ClassDecl(string name, bool @public, List<FunctionDecl> methods, int startPos, int endPos)
        : Stmt(startPos, endPos)
    {
        public string Name { get; } = name;
        public bool Public { get; } = @public;
        public List<FunctionDecl> Methods { get; } = methods;
    }

    public class NamespaceDecl(string name, int startPos, int endPos) : Stmt(startPos, endPos)
    {
        public string Name { get; } = name;
    }

}
