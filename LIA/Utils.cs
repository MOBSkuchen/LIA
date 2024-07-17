namespace LIA;

public class Utils
{
    public static char Quote()
    {
        return '"';
    }
    public static string FormatLines(string lns)
    {
        List<string> stack = new List<string>();
        
        foreach (var ln in lns.Split("\n"))
        {
            stack.Add($"  {ln}");
        }

        return string.Join("\n", stack);
    }

    public static void WriteStringTo(string path, string text)
    {
        File.WriteAllText(path, text);
    }
}