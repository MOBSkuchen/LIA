namespace LIA;

public struct ClassAttributes(string ns, bool isPublic, string name, bool builtin = false, string? coverName = null)
{
    public string NameSpace = ns;
    public string Name = name;
    public string CoverName = coverName ?? name;
    public bool IsPublic = isPublic;
    public bool Builtin = builtin;
    
    public string Generate()
    {
        List<string> stack = new List<string>();
        
        if (IsPublic) stack.Add("public");
        else stack.Add("private");
        
        stack.Add(Name);

        return string.Join(" ", stack);
    }
}