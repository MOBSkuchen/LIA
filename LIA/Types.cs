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
    public readonly string Literal = classGen.ClassAttributes.Builtin ? classGen.ClassAttributes.Name : $"{classGen.ClassAttributes.NameSpace}.{classGen.ClassAttributes.Name}";
}