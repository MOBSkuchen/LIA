namespace LIA;

public class NameSpace(string name)
{
    private List<string> _instructions = new List<string>();
    public readonly List<ClassGen> Classes = new List<ClassGen>();
    private readonly string _head = $".namespace {name}";
    public readonly string Name = name;

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
    
    public ClassGen SpawnClass(bool isPublic, string name1)
    {
        var classG = new ClassGen(new ClassAttributes(name, isPublic, name1));
        Classes.Add(classG);
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
            total += $"{Utils.FormatLines(classG.Get())}\n";
        }

        total += "}";
        
        return total;
    }
}