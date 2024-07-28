using CommandLine;
using LIA;
using Parser = LIA.Parser;

namespace Liac;


public class Liac
{
    private class Options
    {
        
        [Value(0, MetaName = "mode", HelpText = "Process mode, i.e. 'com'")]
        public string Mode { get; set; }

        [Value(1, MetaName = "input", Required = true, HelpText = "Input file to be processed.")]
        public string? InputFile { get; set; }
        
        [Option(
            Default = false,
            HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
        
        [Option('o', "output", Required = false, Default = null, HelpText = "Output file to be generated")]
        public string? OutputFile { get; set; }
        
        [Option('e', "emit", Required = false, Default = "exe", HelpText = "Output type: exe, dll, net-print, net-file")]
        public string? EmitType { get; set; }
    }

    static void Main(string[] args)
    {
        CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
    }
    static void RunOptions(Options options)
    {
        switch (options.Mode)
        {
            case "com": CompileFile(options); break;
        }
    }
    static void HandleParseError(IEnumerable<Error> errs)
    {
        foreach (var err in errs)
        {
            Console.WriteLine(err);
        }
    }

    static void CompileFile(Options options)
    {
        if (!File.Exists(options.InputFile)) Errors.Exit(ErrorCodes.Unknown);
        var codeFile = new CodeFile(options.InputFile!);
        var lexer = new Lexer(codeFile);
        lexer.Lex();
        var parser = new Parser(lexer);
        var compiler = new Compiler(codeFile);
        compiler.ParseTopLevel(parser.ParseTopLevel());
        
        switch (options.EmitType!)
        {
            case "net-print": Console.WriteLine(compiler.Get()); break;
            case "net-file": compiler.WriteToFile(options.OutputFile!); break;
        }
    }
}