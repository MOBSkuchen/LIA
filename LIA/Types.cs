namespace LIA;

public class TypeEm
{
    public RealType RealType;
    private string _literal;
    public TypeEm(RealType realType, bool isRef = false, bool goesOut = false)
    {
        _literal = (goesOut ? "[out] " : "") + _literal;
        _literal = isRef ? realType.Literal + "&" : realType.Literal;
        RealType = realType;
    }

    public string Get() => _literal;
}

public class RealType(ClassGen classGen)
{
    public ClassGen ClassGen = classGen;
    public ClassAttributes ClassAttributes = classGen.ClassAttributes;
    public readonly string Literal = classGen.ClassAttributes.Builtin ? classGen.ClassAttributes.Name : $"class {classGen.ClassAttributes.NameSpace}.{classGen.ClassAttributes.Name}";
}

public class Field(RealType realType, string name, bool isStatic, bool isPublic, Expr? @default = null)
{
    public RealType RealType = realType;
    public bool IsStatic = isStatic;
    public bool IsPublic = isPublic;
    public string Name = name;
    public Expr? Default = @default;

    public string Get { get; } = ".field " + (isPublic ? "public " : " ") + (isStatic ? "static " : " ") + realType.Literal + " " + name;
}