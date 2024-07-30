namespace LIA;

public struct FunctionAttributes(string ns, string @class, string name, 
    bool isStatic, bool isPublic, bool isClass, TypeEm typeEm, List<(string, TypeEm)>? args,
    bool isBuiltin = false, string? library = null)
{
    public string Namespace = ns;
    public string Class = @class;
    public string? Library = library;
    public bool IsStatic = isStatic;
    public bool IsPublic = isPublic;
    public bool IsClass = isClass;
    public bool IsBuiltin = isBuiltin;
    public string Name = name;
    public TypeEm TypeEm = typeEm;
    public List<(string, TypeEm)>? Arguments = args;

    public string Generate()
    {
        List<string> stack = new List<string>();
        
        if (IsPublic) stack.Add("public");
        else stack.Add("private");
        if (IsStatic) stack.Add("static");
        
        stack.Add(TypeEm.Get());
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