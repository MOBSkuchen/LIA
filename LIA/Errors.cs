namespace LIA;

public static class Errors
{
    public static void ThrowCodeError(CodeLocation codeLocation, string message, ErrorCodes errNum)
    {
        ViewCodeLocation(codeLocation, errNum == ErrorCodes.EndOfFile);
        Error(errNum, message);
    }

    private static void ViewCodeLocation(CodeLocation codeLocation, bool isEof)
    {
        
        var lines = codeLocation.CodeFile.Text.Split("\n").ToList();
        
        var startPos = Utils.GetLineNumber(codeLocation.CodeFile.Text, codeLocation.StartPosition);
        var endPos = Utils.GetLineNumber(codeLocation.CodeFile.Text, codeLocation.EndPosition);

        if (isEof)
        {
            lines[startPos.Item1] += " ";
            startPos.Item2 = lines[startPos.Item1].Length - 1;
            endPos.Item2 = lines[startPos.Item1].Length;
        }
        
        string position = "line ";
        position += startPos.Item1 == endPos.Item1 ? $"{startPos.Item1}" : $"{startPos.Item1}-{endPos.Item1}";
        position += ":";
        position += startPos.Item2 == endPos.Item2 ? $"{startPos.Item2}" : $"{startPos.Item2}-{endPos.Item2}";
        
        Console.WriteLine($"In file " +
                          $"'{ConsoleLib.As(codeLocation.CodeFile.Filepath, ConsoleLib.TextBold, ConsoleLib.FgCyan)}'" +
                          $" at {ConsoleLib.As(position, ConsoleLib.TextBold, ConsoleLib.FgBlue)} :");
        
        if (startPos.Item1 != 0) Console.WriteLine($"{startPos.Item1-1} | {lines[startPos.Item1-1]}");
        Console.WriteLine($"{startPos.Item1} > " +
                          ConsoleLib.As($"{lines[startPos.Item1].Substring(0, startPos.Item2)}" +
                                        $"{ConsoleLib.MakeColor(ConsoleLib.TextUnderline, ConsoleLib.FgRed)}" +
                                        $"{lines[startPos.Item1].Substring(startPos.Item2, endPos.Item2 - startPos.Item2)}" + ConsoleLib.ResetColor() +
                                        $"{ConsoleLib.MakeColor(ConsoleLib.TextItalic, ConsoleLib.FgMagenta)}{lines[startPos.Item1].Substring(endPos.Item2, lines[startPos.Item1].Length - endPos.Item2)}", ConsoleLib.TextItalic, ConsoleLib.FgMagenta));
        if (startPos.Item1 != (lines.Count - 1)) Console.WriteLine($"{startPos.Item1+1} | {lines[startPos.Item1+1]}");
    }

    public static void Warning(WarningCodes warningCodes, string message)
    {
        Console.WriteLine($"{ConsoleLib.As("Warning", ConsoleLib.TextBold, ConsoleLib.FgBrightYellow)}" +
                          $" {ConsoleLib.As(warningCodes.ToString(), ConsoleLib.TextBold, ConsoleLib.FgBrightYellow)}" +
                          $" [{ConsoleLib.As(((int)warningCodes).ToString(), 0, ConsoleLib.FgCyan)}] : {message}");
    }

    public static void CodeWarning(CodeLocation codeLocation, WarningCodes warningCodes, string message)
    {
        ViewCodeLocation(codeLocation, false);
        Warning(warningCodes, message);
    }

    public static void Error(ErrorCodes errNum, string message)
    {
        Console.WriteLine($"{ConsoleLib.As(errNum.ToString(), ConsoleLib.TextBold, ConsoleLib.FgBrightRed)} [{ConsoleLib.As(((int)errNum).ToString(), 0, ConsoleLib.FgCyan)}] : {message}");
        Exit(errNum);
    }

    public static void Exit(ErrorCodes errorCode)
    {
        if ((errorCode != ErrorCodes.None) && GlobalContext.DevDebug) throw new Exception($"Exit with error {errorCode}! (Dev debug is enabled)");
        int exitCode = (int)errorCode;
        Console.WriteLine($"Exited with code {ConsoleLib.As(exitCode.ToString(), ConsoleLib.TextBold, errorCode == ErrorCodes.None ? ConsoleLib.FgCyan : ConsoleLib.FgRed)}");
        Environment.Exit(exitCode);
    }
}

public enum ErrorCodes
{
    None,
    Unknown,
    
    UnknownArgument,
    UnaccessibleFile,
    IlCompFailed,
    IlasmNotFound,
    
    Unintelligeble,
    InvalidToken,
    EndOfFile,
    SyntaxError,
    MissingValue,
    InvalidType,
    TypeConflict,
    UnknownVariable,
    UnknownFunction,
    UnimplementedClassMethod,
}

public enum WarningCodes
{
    None,
    Unknown,
    MainNotDefined,
    UselessCode,
    UnreachableCode,
    InvalidClassMethod,
    InvalidArchitecture,
}