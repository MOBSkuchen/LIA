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

public class RealType(ClassAttributes classAttributes)
{
    public ClassAttributes ClassAttributes = classAttributes;
    public readonly string Literal = classAttributes.Builtin ? classAttributes.Name : $"{classAttributes.NameSpace}.{classAttributes.Name}";
}