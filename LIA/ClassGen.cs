namespace LIA;

public class ClassGen
{
    private static readonly List<string> PossibleClassMethods =
    [
        "opadd",
        "opsub",
        "opmul",
        "opdiv",
        "opgreater",
        "opgreaterequals",
        "oplesser",
        "oplesserequals",
        "opfalse",
        "optrue",
        "oprem",
        "opxor",
        "opequals",
        "opnot",
        "init"
    ];

    public static readonly List<string> SpecialMethods = [
        ".ctor",
        ".cctor",
        "init"
    ];
    
    private readonly string _head;
    public ClassAttributes ClassAttributes;
    public RealType GetRealType => new (this);

    public Dictionary<string, (FunctionAttributes, FunctionGen)> Functions =
        new ();

    public Dictionary<string, Field> Fields = new ();
    
    public Dictionary<string, string> ClassMethodAccess = new ();

    public ClassGen(ClassAttributes classAttributes)
    {
        ClassAttributes = classAttributes;
        _head = classAttributes.Generate();
    }

    public FunctionGen SpawnFunction(string name, bool isStatic, bool isPublic, bool isClassMethod, TypeEm typeEm, List<(string, TypeEm)>? args, bool isBuiltin)
    {
        bool specialMethod = SpecialMethods.Contains(name);
        var functionAttributes = new FunctionAttributes($"{ClassAttributes.NameSpace}.{ClassAttributes.Name}",
            ClassAttributes.Name, name, isStatic, isPublic, isClassMethod, typeEm, args, specialMethod, isBuiltin);
        var function = new FunctionGen(functionAttributes, this);
        if (isClassMethod && !specialMethod)
        {
            if (PossibleClassMethods.Contains(name.ToLower())) ClassMethodAccess.Add(name.ToLower(), name);
            else
            {
                Errors.Warning(WarningCodes.InvalidClassMethod,
                    $"Invalid class method name '{name}', use one of {string.Join(", ", PossibleClassMethods)}");
                functionAttributes.IsClass = false;
            }
        }
        Functions.Add(name, (functionAttributes, function));
        return function;
    }

    public void AddField(Field field) => Fields[field.Name] = field;
    
    public string Get()
    {
        string total = $".class {_head} " + "{";
        if (Fields.Count != 0) total += "\n";
        foreach (var field in Fields)
        {
            total += $"{field.Value.Get}\n";
        }
        if (Functions.Count == 0) total += "}";
        else total += "\n";

        foreach (var function in Functions)
        {
            total += $"{Utils.FormatLines(function.Value.Item2.Get())}\n";
        }
        if (Functions.Count >= 1) total += "}";

        return total;
    }
}