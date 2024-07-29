﻿using System.Diagnostics;
using CommandLine;
using LIA;
using Parser = LIA.Parser;

namespace Liac;


public class Liac
{
    private class Options
    {
        
        [Verb("compile", HelpText = "Compile file")]
        public class CompileOptions
        {
            [Value(1, MetaName = "input", Required = true, HelpText = "Input file to be processed.")]
            public string? InputFile { get; set; }
        
            [Option('b', "bt", Required = false, Default = "exe", HelpText = "Build type, like for example exe or dll (or nil)")]
            public string? BuildType { get; set; }
        
            [Option(
                Default = false,
                HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }
        
            [Option('o', "output", Required = false, Default = null, HelpText = "Output file to be generated")]
            public string? OutputFile { get; set; }
        
            [Option('e', "emit", Required = false, Default = null, HelpText = "Extra output info")]
            public IEnumerable<string>? EmitType { get; set; }
        
            [Option('l', "ilcompiler", Required = false, Default = null, HelpText = "Specify ilasm compiler path")]
            public string? IlCompilerPath { get; set; }

            [Option('a', "arch", Required = false, Default = "amd/64", HelpText = "Architecture, 64bit AMD")]
            public string? Arch { get; set; }
            
            [Option('t', "test", Required = false, Default = false, HelpText = "Auto execute produced file, requires exe to be built")]
            public bool Test { get; set; }
            
            [Option('ö', "dev-debug", Default = false, Required = false, HelpText = "Will throw a (real) error upon program error")]
            public bool DevDebug { get; set; }
        }
    }

    static int Main(string[] args)
    {
        var res = CommandLine.Parser.Default.ParseArguments<Options.CompileOptions>(args);
        int exitCode = res.MapResult(
                CompileFile,
                HandleParseError);
        Errors.Exit((ErrorCodes) exitCode);
        return 0;
    }

    static int HandleParseError(IEnumerable<Error> errs)
    {
        foreach (var err in errs)
        {
            Console.WriteLine(err);
        }

        return (int)ErrorCodes.None;
    }
    
    static string FindIlasm(Options.CompileOptions options)
    {
        // TODO: Check validity
        if (options.IlCompilerPath != null) return options.IlCompilerPath;
        // TODO: Do some actual finding
        return "C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\ilasm.exe";
    }

    static List<String> CompileIl(Options.CompileOptions options, string ilPath, string outputFile)
    {
        // TODO: Check this shit
        bool b64 = true;
        string other;
        options.Arch = options.Arch!.ToLower();
        if (options.Arch!.Contains("/"))
        {
            var splitA = options.Arch!.Split("/");
            b64 = splitA[1] != "32";
            other = splitA[0];
        }
        else other = options.Arch!;
        var processArguments = new List<string> {FindIlasm(options), ilPath, "/QUIET", "/NOLOGO"};
        if (b64) processArguments.Add("/PE64");
        else processArguments.Add("/PE32");
        switch (other)
        {
            case "arm":
                processArguments.Add(b64 ? "/ARM64" : "/ARM");
                break;
            case "itanium":
                processArguments.Add("/ITANIUM");
                break;
            default:
                processArguments.Add("/X64");
                break;
        }
        processArguments.Add($"/OUTPUT={outputFile}");
        return processArguments;
    }

    static int CompileFile(Options.CompileOptions options)
    {
        GlobalContext.DevDebug = options.DevDebug;
        
        if (!File.Exists(options.InputFile)) Errors.Error(ErrorCodes.UnaccessibleFile, $"Input file '{options.InputFile}' does not exist");
        
        var codeFile = new CodeFile(options.InputFile!);
        var lexer = new Lexer(codeFile);
        lexer.Lex();
        
        if (options.EmitType!.Contains("tokens"))
        {
            foreach (var token in lexer.Tokens)
            {
                Console.WriteLine($"{token.StartPos}-{token.EndPos} : {token.Type} = '{token.Content}'");
            }
        }
        
        var parser = new Parser(lexer);
        
        var compiler = new Compiler(codeFile);
        
        compiler.ParseTopLevel(parser.ParseTopLevel());

        if (options.BuildType! != "nil")
        {
            var outputFile = options.OutputFile ?? compiler.GetNewPath(options.BuildType == "exe" ? "exe" : "dll");
            var ilPath = compiler.WriteToPath(options.EmitType!.Contains("il") ? compiler.GetNewPath() : Path.GetTempFileName());
            var emitTimes = options.EmitType!.Contains("time");     // Handle this
            var processArgs = CompileIl(options, ilPath, outputFile);
            var exePath = processArgs[0]; processArgs.RemoveAt(0);
        
            Console.WriteLine("Compiling IL");
            var proc = Process.Start(exePath, processArgs);
            proc.WaitForExit();

            Console.WriteLine($"Done. IL-Compile finished with exit code {proc.ExitCode}");

            if (options.Test)
            {
                Console.WriteLine($"Testing build ({outputFile})");
                var procExe = Process.Start(outputFile);
                procExe.WaitForExit();

                Console.WriteLine($"Done. Test finished with exit code {procExe.ExitCode}");
            }
        }

        if (options.EmitType!.Contains("il")) compiler.WriteToPath(compiler.GetNewPath());

        return 0;
    }
}