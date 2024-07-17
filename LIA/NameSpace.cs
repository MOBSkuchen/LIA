namespace LIA;

public class NameSpace(string name)
{
    private List<string> _instructions = new List<string>();
    private List<ClassGen> _classes = new List<ClassGen>();
    private string _head = $".namespace {name}";

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
        _classes.Add(classG);
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

        foreach (var classG in _classes)
        {
            total += $"{Utils.FormatLines(classG.Get())}\n";
        }

        total += "}";
        
        return total;
    }
}