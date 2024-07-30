namespace LIA;

public class NameSpace(string name, bool builtin = false)
{
    private List<string> _instructions = new List<string>();
    private readonly string _head = $".namespace {name}";
    public readonly string Name = name;
    public readonly bool Builtin = builtin;
    
    public Dictionary<string, (ClassAttributes, ClassGen)> Classes =
        new Dictionary<string, (ClassAttributes, ClassGen)>();

    public void WriteToFile(string path)
    {
        Utils.WriteStringTo(path, Get());
    }

    public void WriteToFile() => WriteToFile(name + ".il");

    private void AppendRaw(string inst)
    {
        _instructions.Add(inst);
    }

    public void Assembly(string name)
    {
        AppendRaw($".assembly {name} " + "{}");
    }

    public void ExternAssembly(string name)
    {
        Assembly($"extern {name}");
    }
    
    public ClassGen SpawnClass(bool isPublic, string name1, string? coverName = null)
    {
        var classA = new ClassAttributes(name, isPublic, name1, Builtin, coverName);
        var classG = new ClassGen(classA);
        Classes.Add(name1, (classA, classG));
        return classG;
    }

    public string Get()
    {
        string total = _head + " {";
        
        Assembly(name);
        
        if (_instructions.Count < 1) total = " }";
        else total += "\n";
        
        foreach (var instruction in _instructions)
        {
            total += $"  {instruction}\n";
        }
        
        _instructions.RemoveAt(_instructions.Count - 1);

        foreach (var classG in Classes)
        {
            total += $"{Utils.FormatLines(classG.Value.Item2.Get())}\n";
        }

        total += "}";
        
        return total;
    }
}