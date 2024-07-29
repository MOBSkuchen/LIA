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

    public static string WriteStringTo(string path, string text)
    {
        File.WriteAllText(path, text);
        return path;
    }

    public static (int, int) GetLineNumber(string code, int position)
    {
        int newCharCounter = 0;
        int tempCounter = 0;
        int lineNumber = 0;
        for (int i = 0; i < position; i++)
        {
            tempCounter++;
            if (code[i] == '\n')
            {
                lineNumber++;
                tempCounter = 0;
            }
        }
        newCharCounter += tempCounter;
        return (lineNumber, newCharCounter);
    }
}

public class CodeFile
{
    public readonly string Filepath;
    public readonly string Text;
    public CodeFile(string filepath)
    {
        Filepath = filepath;
        Text = File.ReadAllText(Filepath);
    }
}

public struct CodeLocation(int startPosition, int endPosition, CodeFile codeFile)
{
    public int StartPosition = startPosition;
    public int EndPosition = endPosition;
    public CodeFile CodeFile = codeFile;
}