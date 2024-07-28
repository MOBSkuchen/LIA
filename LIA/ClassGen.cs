namespace LIA;

public class ClassGen
{
    private string _head;
    private ClassAttributes _classAttributes;

    public Dictionary<string, (FunctionAttributes, FunctionGen)> Functions =
        new Dictionary<string, (FunctionAttributes, FunctionGen)>();

    public ClassGen(ClassAttributes classAttributes)
    {
        _head = classAttributes.Generate();
    }

    public FunctionGen SpawnFunction(string name, bool isStatic, bool isPublic, TypeEm typeEm, List<(string, TypeEm)>? args)
    {
        var functionAttributes = new FunctionAttributes($"{_classAttributes.NameSpace}.{_classAttributes.Name}",
            _classAttributes.Name, name, isStatic, isPublic, typeEm, args);
        var function = new FunctionGen(functionAttributes, this);
        Functions.Add(name, (functionAttributes, function));
        return function;
    }
    
    public string Get()
    {
        string total = $".class {_head} " + "{";
        if (Functions.Count == 0) total += " }";
        else total += "\n";

        foreach (var function in Functions)
        {
            total += $"{Utils.FormatLines(function.Value.Item2.Get())}\n";
        }
        
        total += "}";

        return total;
    }
}