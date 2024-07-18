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
        foreach (var local in LocalVariables)
        {
            locals.Add($"  {local.Item2.Get()} {local.Item1}");
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
    private OpCode ConvCmpOpCode(CmpOps cmpOps, bool branch)
    {
        if (branch)
        {
            switch (cmpOps) {
                case CmpOps.Equals: return OpCodes.Beq;
                case CmpOps.GreaterThan: return OpCodes.Bgt;
                case CmpOps.GreaterThanEquals: return OpCodes.Bge;
                case CmpOps.LesserThan: return OpCodes.Blt;
                case CmpOps.LesserThanEquals: return OpCodes.Ble;
                case CmpOps.IsFalse: return OpCodes.Brfalse;
                case CmpOps.IsTrue: return OpCodes.Brtrue;
            }
        }
        else
        {
            switch (cmpOps) {
                case CmpOps.Equals: return OpCodes.Ceq;
                case CmpOps.GreaterThan: return OpCodes.Cgt;
                case CmpOps.LesserThan: return OpCodes.Clt;
            }
        }
        throw new Exception("Invalid cmp-op");
    }

    private OpCode ConvMathOpCode(MathOps mathOps)
    {
        switch (mathOps)
        {
            case MathOps.Add: return OpCodes.Add;
            case MathOps.Sub: return OpCodes.Sub;
            case MathOps.Mul: return OpCodes.Mul;
            case MathOps.Div: return OpCodes.Div;
            case MathOps.Rem: return OpCodes.Rem;
            default: throw new Exception("Invalid math-op");
        }
    }

    public void PerformOp(MathOps operation)
    {
        _function.DecStackSize();
        Emit(ConvMathOpCode(operation));
    }
    public void PerformOp(CmpOps operation)
    {
        _function.DecStackSize();
        Emit(ConvCmpOpCode(operation, false));
    }

    public void PerformOpBranch(CmpOps cmpOps, string branchLabel)
    {
        _function.DecStackSize(2);
        Emit(ConvCmpOpCode(cmpOps, true), branchLabel);
    }

    public void PerformOpBranch(CmpOps cmpOps, Segment branchSegment) => PerformOpBranch(cmpOps, branchSegment._name);

    public void Branch(string label)
    {
        Emit(OpCodes.Br, label);
    }

    public void Branch(Segment segment) => Branch(segment._name);
    
    public void Loop() => Branch(this);
    
    public void Loop(CmpOps cmpOps) => PerformOpBranch(cmpOps, _name);
    
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
        stack.Add($"{funcAttrs.Namespace}::{funcAttrs.Name}");

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