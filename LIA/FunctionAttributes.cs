namespace LIA;

public struct FunctionAttributes(string ns, string @class, string name, 
    bool isStatic, bool isPublic, Type type, List<(string, Type)>? args, string? library = null)
{
    public string Namespace = ns;
    public string Class = @class;
    public string? Library = library;
    public bool IsStatic = isStatic;
    public bool IsPublic = isPublic;
    public string Name = name;
    public Type Type = type;
    public List<(string, Type)>? Arguments = args;

    public string Generate()
    {
        List<string> stack = new List<string>();
        
        if (IsPublic) stack.Add("public");
        else stack.Add("private");
        if (IsStatic) stack.Add("static");
        
        stack.Add(Type.Get());
        stack.Add(Name);
        stack.Add("(");

        if (Arguments != null)
        {
            for (var index = 0; index < Arguments.Count; index++)
            {
                var valueTuple = Arguments[index];
                var fmt = (index + 1) >= Arguments.Count ? String.Empty : ",";
                stack.Add($"{valueTuple.Item2.Get()} {valueTuple.Item1}{fmt}");
            }
        }

        stack.Add(")");
        return string.Join(" ", stack);
    }
}