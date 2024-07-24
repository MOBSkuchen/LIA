﻿using System.Drawing;
using Pastel;

namespace LIA;

public class Errors
{
    public static void ThrowCodeError(CodeLocation codeLocation, string message, ErrorCodes errNum)
    {
        var lines = codeLocation.CodeFile.Text.Split("\n").ToList();
        
        var startPos = Utils.GetLineNumber(codeLocation.CodeFile.Text, codeLocation.StartPosition);
        var endPos = Utils.GetLineNumber(codeLocation.CodeFile.Text, codeLocation.EndPosition);

        if (errNum == ErrorCodes.EndOfFile)
        {
            lines[startPos.Item1] += " ";
            startPos.Item2 = lines[startPos.Item1].Length - 1;
            endPos.Item2 = lines[startPos.Item1].Length;
        }
        
        string position = "line ";
        position += startPos.Item1 == endPos.Item1 ? $"{startPos.Item1}" : $"{startPos.Item1}-{endPos.Item1}";
        position += ";";
        position += startPos.Item2 == endPos.Item2 ? $"{startPos.Item2}" : $"{startPos.Item2}:{endPos.Item2}";
        
        Console.WriteLine($"In file " +
                          $"'{ConsoleLib.As(codeLocation.CodeFile.Filepath, ConsoleLib.TextBold, ConsoleLib.FgCyan)}'" +
                          $" at {ConsoleLib.As(position, ConsoleLib.TextBold, ConsoleLib.FgBlue)} :");
        
        Console.WriteLine($"{ConsoleLib.As(errNum.ToString(), ConsoleLib.TextBold, ConsoleLib.FgBrightRed)} [{ConsoleLib.As(((int)errNum).ToString(), 0, ConsoleLib.FgCyan)}] : {message}");
        
        if (startPos.Item1 != 0) Console.WriteLine($"{startPos.Item1-1} | {lines[startPos.Item1-1]}");
        Console.WriteLine($"{startPos.Item1} > " +
        ConsoleLib.As($"{lines[startPos.Item1].Substring(0, startPos.Item2)}" +
        $"{ConsoleLib.MakeColor(ConsoleLib.TextUnderline, ConsoleLib.FgRed)}" +
        $"{lines[startPos.Item1].Substring(startPos.Item2, endPos.Item2 - startPos.Item2)}" + ConsoleLib.ResetColor() +
        $"{ConsoleLib.MakeColor(ConsoleLib.TextItalic, ConsoleLib.FgMagenta)}{lines[startPos.Item1].Substring(endPos.Item2, lines[startPos.Item1].Length - endPos.Item2)}", ConsoleLib.TextItalic, ConsoleLib.FgMagenta));
        if (startPos.Item1 != (lines.Count - 1)) Console.WriteLine($"{startPos.Item1+1} | {lines[startPos.Item1+1]}"); 
        
        Exit(errNum);
    }

    public static void Exit(ErrorCodes errorCode)
    {
        int exitCode = (int)errorCode;
        Console.WriteLine($"Exited with code {ConsoleLib.As(exitCode.ToString(), ConsoleLib.TextBold, errorCode == ErrorCodes.None ? ConsoleLib.FgCyan : ConsoleLib.FgRed)}");
        Environment.Exit(exitCode);
    }
}

public enum ErrorCodes
{
    None,
    Unknown,
    Unintelligeble,
    InvalidToken,
    EndOfFile,
    SyntaxError,
    MissingValue
}