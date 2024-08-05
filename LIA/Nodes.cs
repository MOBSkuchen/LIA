namespace LIA
{
    // Base AST Node
    public abstract class AstNode(CodeLocation codeLocation)
    {
        public int StartPos { get; } = codeLocation.StartPosition;
        public int EndPos { get; } = codeLocation.EndPosition;
        public CodeLocation CodeLocation = codeLocation;
    }

    // Expression Nodes
    public abstract class Expr(CodeLocation codeLocation) : AstNode(codeLocation);

    public class BinaryExpr(Expr left, TokenType @operator, Expr right, CodeLocation codeLocation)
        : Expr(codeLocation)
    {
        public Expr Left { get; } = left;
        public Expr Right { get; } = right;
        public TokenType Operator { get; } = @operator;
    }

    public class UnaryExpr(TokenType @operator, Expr operand, CodeLocation codeLocation)
        : Expr(codeLocation)
    {
        public Expr Operand { get; } = operand;
        public TokenType Operator { get; } = @operator;
    }

    public class IntegerExpr(long number, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public long Value { get; } = number;
    }
    
    public class FloatExpr(double number, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public double Value { get; } = number;
    }

    public class StringExpr(string value, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public string Value { get; } = value;
    }

    public class FunctionCallExpr(IdentifierExpr name, List<Expr>? arguments, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public IdentifierExpr Name { get; } = name;
        public List<Expr>? Arguments { get; } = arguments;
    }

    public class IdentifierExpr(string name, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public string Name { get; } = name;
    }

    public class AssignmentStmt(IdentifierExpr name, Expr? value, IdentifierExpr? type, CodeLocation codeLocation)
        : Stmt(codeLocation)
    {
        public IdentifierExpr Name { get; } = name;
        public IdentifierExpr? Type { get; set; } = type;
        public Expr? Value { get; } = value;
    }

    // Statement Nodes
    public abstract class Stmt(CodeLocation codeLocation) : AstNode(codeLocation);

    public class ExprStmt(Expr expression, CodeLocation codeLocation) : Stmt(codeLocation)
    {
        public Expr Expression { get; } = expression;
    }

    public class ReturnStmt(Expr value, CodeLocation codeLocation) : Stmt(codeLocation)
    {
        public Expr Value { get; } = value;
    }

    public class IfStmt(
        Expr condition,
        BlockStmt thenBranch,
        BlockStmt? elseBranch,
        List<(Expr, BlockStmt)>? elifBranches,
        CodeLocation codeLocation)
        : Stmt(codeLocation)
    {
        public Expr Condition { get; } = condition;
        public BlockStmt ThenBranch { get; } = thenBranch;
        public List<(Expr, BlockStmt)>? ElifBranches { get; } = elifBranches;
        public BlockStmt? ElseBranch { get; } = elseBranch;
    }

    // Block Statement
    public class BlockStmt(List<Stmt> statements, CodeLocation codeLocation) : Stmt(codeLocation)
    {
        public List<Stmt> Statements { get; } = statements;
    }
    
    // While loop
    public class WhileLoop(
        Expr condition,
        BlockStmt body,
        CodeLocation codeLocation) : Stmt(codeLocation)
    {
        public Expr Condition { get; } = condition;
        public BlockStmt Body { get; } = body;
    }

    public class ParameterExpr(IdentifierExpr name, IdentifierExpr type, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public IdentifierExpr Name = name;
        public IdentifierExpr Type = type;
    }

    public class CastExpr(Expr prevExpr, IdentifierExpr destType, CodeLocation codeLocation) : Expr(codeLocation)
    {
        public Expr PrevExpr = prevExpr;
        public IdentifierExpr DestType = destType;
    }

    public class FieldStmt(
        IdentifierExpr name,
        IdentifierExpr type,
        bool isStatic,
        bool isPublic,
        Expr? value,
        CodeLocation codeLocation) : Expr(codeLocation)
    {
        public IdentifierExpr Name = name;
        public IdentifierExpr Type = type;
        public bool IsStatic = isStatic;
        public bool IsPublic = isPublic;
        public Expr? Value = value;
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
        CodeLocation codeLocation)
        : Stmt(codeLocation)
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
    public class ClassDecl(string name, bool @public, List<FunctionDecl> methods, List<FieldStmt> fields, CodeLocation codeLocation)
        : Stmt(codeLocation)
    {
        public string Name { get; } = name;
        public bool Public { get; } = @public;
        public List<FunctionDecl> Methods { get; } = methods;
        public List<FieldStmt> Field { get; } = fields;
    }

    public class NamespaceDecl(string name, CodeLocation codeLocation) : Stmt(codeLocation)
    {
        public string Name { get; } = name;
    }

}
