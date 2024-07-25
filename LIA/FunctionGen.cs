using System.Reflection.Emit;
namespace LIA;

public class FunctionGen : ICtxBGenBp
{
    private string _head;
    public List<string> Instructions { get; set; } = new List<string>();
    public List<(string, Type)> LocalVariables = new List<(string, Type)>();
    private List<Segment> _segments = new List<Segment>();

    // Track stack size to dynamically set 'maxstack'
    private int _stacksize = 0;
    private int _maxstacksize = 0;
    
    public FunctionGen(FunctionAttributes attrs)
    {
        _head = attrs.Generate();
    }

    public Segment SpawnSegment(string name)
    {
        var segment = new Segment(this, name);
        _segments.Add(segment);
        return segment;
    }

    public (string, Type)? GetLocalVariable(string name) =>
        LocalVariables.Find(x => x.Item1 == name);

    public Segment SpawnStartSegment() => SpawnSegment("Start");

    public void IncStackSize(int v = 1)
    {
        _stacksize += v;
        if (_stacksize > _maxstacksize) _maxstacksize = _stacksize;
    }

    public void DecStackSize(int v = 1) => _stacksize -= v;
    public void EntryPoint() => AppendRaw(".entrypoint");

    public string Get()
    {
        string total = ".method " + _head + " { ";
        total += $"\n  .maxstack {_maxstacksize}";
        if (Instructions.Count != 0 || LocalVariables.Count != 0) total += "\n";
        var locals = new List<string>();
        if (LocalVariables.Count != 0) locals.Add(".locals init (");

        for (var index = 0; index < LocalVariables.Count; index++)
        {
            var local = LocalVariables[index];
            var fmt = (index + 1) >= LocalVariables.Count ? String.Empty : ",";
            locals.Add($"  {local.Item2.Get()} {local.Item1}{fmt}");
        }

        if (LocalVariables.Count != 0) locals.Add(")");
        foreach (var instruction in locals.Concat(Instructions))
        {
            total += "  " + instruction + "\n";
        }

        total += "\n";
        
        foreach (var segment in _segments)
        {
            total += "  " + segment.Get() + "\n";
        }
        total += "}";
        return total;
    }

    private void AppendRaw(string operation) => Instructions.Add(operation);
    public void AddComment(string comment) => Instructions[^1] += ("    // " + comment);
}

public class Segment : ICtxBGenBp
{
    private string _name;
    private FunctionGen _function;
    public List<string> Instructions { get; set; } = new List<string>();

    public Segment(FunctionGen function, string name)
    {
        _name = name;
        _function = function;
    }
    
    private void AppendRaw(string operation) => Instructions.Add(operation);
    public void AddComment(string comment) => Instructions[^1] += ("    // " + comment);

    public string Get()
    {
        string total = $"{_name}:\n";
        foreach (var instruction in Instructions)
        {
            total += "  " + instruction + "\n";
        }

        return total;
    }
    
    public void Emit(OpCode opCode)
    {
        if (opCode.Name == null) throw new Exception();
        AppendRaw(opCode.Name);
    }

    public void Emit(OpCode opCode, object value)
    {
        if (opCode.Name == null) throw new Exception();
        AppendRaw($"{opCode.Name} {value}");
    }

    public void LoadArg(int n)
    {
        _function.IncStackSize();
        switch (n)
        {
            case 0: Emit(OpCodes.Ldarg_0); break;
            case 1: Emit(OpCodes.Ldarg_1); break;
            case 2: Emit(OpCodes.Ldarg_2); break;
            case 3: Emit(OpCodes.Ldarg_3); break;
            default: Emit(OpCodes.Ldarg, n); break;
        }
    }

    public void LoadInt(long num, bool longform)
    {
        _function.IncStackSize();
        if (longform) Emit(OpCodes.Ldc_I8, num);
        else Emit(OpCodes.Ldc_I4, num);
    }

    public void Discard() {
        _function.DecStackSize();
        Emit(OpCodes.Pop);
    }

    public void DiscardArg(int arg)
    {
        LoadArg(arg);
        Discard();
    }

    public void LoadFloat(double num, bool longform)
    {
        _function.IncStackSize();
        if (longform) Emit(OpCodes.Ldc_R8, num);
        else Emit(OpCodes.Ldc_R4, num);
    }
    
    public void Ret() => Emit(OpCodes.Ret);
    public void Dup()
    {
        _function.IncStackSize();
        Emit(OpCodes.Dup);
    }

    public void StoreLoc(int n)
    {
        _function.DecStackSize();
        switch (n)
        {
            case 0: Emit(OpCodes.Stloc_0); break;
            case 1: Emit(OpCodes.Stloc_1); break;
            case 2: Emit(OpCodes.Stloc_2); break;
            case 3: Emit(OpCodes.Stloc_3); break;
            default: Emit(OpCodes.Stloc, n); break;
        }
    }

    public void LoadString(string str)
    {
        _function.IncStackSize();
        Emit(OpCodes.Ldstr, $"{Utils.Quote()}{str}{Utils.Quote()}");
    }

    public void StoreArg(int n)
    {
        _function.DecStackSize();
        Emit(OpCodes.Starg, n);
    }

    /*
     * <branch> is the difference between B*** / C***
     * aka if a branch should be done or if the value should be pushed onto the stack
     * aka the difference between:
     * if (a == b) ...
     * and
     * a == b
     */
    private OpCode ConvCmpOpCode(Operation cmpOps, bool branch)
    {
        if (branch)
        {
            switch (cmpOps) {
                case Operation.Equals: return OpCodes.Beq;
                case Operation.GreaterThan: return OpCodes.Bgt;
                case Operation.GreaterThanEquals: return OpCodes.Bge;
                case Operation.LesserThan: return OpCodes.Blt;
                case Operation.LesserThanEquals: return OpCodes.Ble;
                case Operation.IsFalse: return OpCodes.Brfalse;
                case Operation.IsTrue: return OpCodes.Brtrue;
            }
        }
        else
        {
            switch (cmpOps) {
                case Operation.Equals: return OpCodes.Ceq;
                case Operation.GreaterThan: return OpCodes.Cgt;
                case Operation.LesserThan: return OpCodes.Clt;
                case Operation.Not: return OpCodes.Not;
            }
        }
        throw new Exception("Invalid cmp-op");
    }

    private OpCode ConvMathOpCode(Operation mathOps)
    {
        switch (mathOps)
        {
            case Operation.Add: return OpCodes.Add;
            case Operation.Sub: return OpCodes.Sub;
            case Operation.Mul: return OpCodes.Mul;
            case Operation.Div: return OpCodes.Div;
            case Operation.Rem: return OpCodes.Rem;
            default: throw new Exception("Invalid math-op");
        }
    }

    public void PerformOp(Operation operation)
    {
        _function.DecStackSize();
        Emit(ConvMathOpCode(operation));
    }

    public void PerformOpBranch(Operation cmpOps, string branchLabel)
    {
        _function.DecStackSize(2);
        Emit(ConvCmpOpCode(cmpOps, true), branchLabel);
    }

    public void PerformOpBranch(Operation cmpOps, Segment branchSegment) => PerformOpBranch(cmpOps, branchSegment._name);

    public void Branch(string label)
    {
        Emit(OpCodes.Br, label);
    }

    public void Branch(Segment segment) => Branch(segment._name);
    
    public void Loop() => Branch(this);
    
    public void Loop(Operation cmpOps) => PerformOpBranch(cmpOps, _name);
    
    public int InitVar(string name, Type type)
    {
        _function.LocalVariables.Add((name, type));
        return _function.LocalVariables.Count - 1;
    }

    public void LoadLoc(int n)
    {
        _function.IncStackSize();
        switch (n)
        {
            case 0: Emit(OpCodes.Ldloc_0); break;
            case 1: Emit(OpCodes.Ldloc_1); break;
            case 2: Emit(OpCodes.Ldloc_2); break;
            case 3: Emit(OpCodes.Ldloc_3); break;
            default: Emit(OpCodes.Ldloc, n); break;
        }
        // Regenerative store
        Dup();
        StoreLoc(n);
    }
    
    public void Call(FunctionAttributes funcAttrs)
    {
        List<string> stack = new List<string>();
        List<string> args = new List<string>();
        
        stack.Add("call");
        stack.Add(funcAttrs.Type.Get());
        if (funcAttrs.Library != null)
        {
            stack.Add($"[{funcAttrs.Library}]");
        }
        stack.Add($"{funcAttrs.Namespace}.{funcAttrs.Class}::{funcAttrs.Name}");

        if (funcAttrs.Arguments != null)
        {
            foreach (var argument in funcAttrs.Arguments)
            {
                args.Add(argument.Item2.Get());
            }
        }
        
        stack.Add($"({string.Join(", ", args)})");
        
        AppendRaw(string.Join(" ", stack));
    }
}