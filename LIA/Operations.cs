namespace LIA;

public enum Operation
{
    // Math ops -> i32, i64 / f32, f64
    Add,
    Sub,
    Mul,
    Div,
    Rem,
    Xor,
    // Cmd Ops -> bool
    GreaterThan,
    GreaterThanEquals,
    LesserThan,
    LesserThanEquals,
    Equals,
    IsFalse,
    IsTrue,
    // Something -> bool
    Not,
    // Can not implement via class method, uses standard cast => ((BinaryExpr) expr)
    Cast
}