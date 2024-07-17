namespace LIA;

public class ClassGen
{
    private string _head;
    private ClassAttributes _classAttributes;
    private List<FunctionGen> _functions = new List<FunctionGen>();
    
    public ClassGen(ClassAttributes classAttributes)
    {
        _head = classAttributes.Generate();
    }

    public FunctionGen SpawnFunction(string name, bool isStatic, bool isPublic, Type type, List<(string, Type)>? args)
    {
        var function = new FunctionGen(new FunctionAttributes($"{_classAttributes.NameSpace}.{_classAttributes.Name}"
            , name, isStatic, isPublic, type, args));
        _functions.Add(function);
        return function;
    }
    
    public string Get()
    {
        string total = $".class {_head} " + "{";
        if (_functions.Count == 0) total += " }";
        else total += "\n";

        foreach (var function in _functions)
        {
            total += $"{Utils.FormatLines(function.Get())}\n";
        }
        
        total += "}";

        return total;
    }
}