namespace LIA;

// Interface Context-based Generation Blueprint
public interface ICtxBGenBp
{
    List<string> Instructions { get; set; }

    private void AppendRaw(string str) => Instructions.Add(str);

    public void AddComment(string comment) => Instructions[^1] += ("    // " + comment);

    public string Get();
}