namespace LIA;

public class Type
{
    private string _literal;
    public Type(string literal, bool isRef = false, bool goesOut = false)
    {
        _literal = (goesOut ? "[out] " : "") + _literal;
        _literal = isRef ? literal + "&" : literal;
    }

    public string Get() => _literal;
}