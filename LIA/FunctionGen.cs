using System.Reflection.Emit;
namespace LIA;

public class FunctionGen : ICtxBGenBp
{
    private string _head;
    public List<string> Instructions { get; set; } = new List<string>();
    public List<(string, TypeEm)> LocalVariables = new List<(string, TypeEm)>();
    private List<Segment> _segments = new List<Segment>();

    // Track stack size to dynamically set 'maxstack'
    private int _stacksize = 0;
    private int _maxstacksize = 0;

    public ClassGen Class;
    
    public FunctionGen(FunctionAttributes attrs, ClassGen classGen)
    {
        _head = attrs.Generate();
        Class = classGen;
    }

    public Segment SpawnSegment(string name)
    {
        var segment = new Segment(this, name);
        _segments.Add(segment);
        return segment;
    }

    public (string, TypeEm)? GetLocalVariable(string name) =>
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
    public FunctionGen Function;
    public List<string> Instructions { get; set; } = new List<string>();

    public Segment(FunctionGen function, string name)
    {
        _name = name;
        Function = function;
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
        Function.IncStackSize();
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
        Function.IncStackSize();
        if (longform) Emit(OpCodes.Ldc_I8, num);
        else Emit(OpCodes.Ldc_I4, num);
    }

    public void Discard() {
        Function.DecStackSize();
        Emit(OpCodes.Pop);
    }

    public void DiscardArg(int arg)
    {
        LoadArg(arg);
        Discard();
    }

    public void LoadFloat(double num, bool longform)
    {
        Function.IncStackSize();
        if (longform) Emit(OpCodes.Ldc_R8, num);
        else Emit(OpCodes.Ldc_R4, num);
    }
    
    public void Ret() => Emit(OpCodes.Ret);
    public void Dup()
    {
        Function.IncStackSize();
        Emit(OpCodes.Dup);
    }

    public void StoreLoc(int n)
    {
        Function.DecStackSize();
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
        Function.IncStackSize();
        Emit(OpCodes.Ldstr, $"{Utils.Quote()}{str}{Utils.Quote()}");
    }

    public void StoreArg(int n)
    {
        Function.DecStackSize();
        Emit(OpCodes.Starg, n);
    }

    public void PerformOp(Operation operation)
    {
        Function.DecStackSize();
        switch (operation)
        {
            case Operation.Add: Emit(OpCodes.Add); break;
            case Operation.Sub: Emit(OpCodes.Sub); break;
            case Operation.Mul: Emit(OpCodes.Mul); break;
            case Operation.Div: Emit(OpCodes.Div); break;
            case Operation.Rem: Emit(OpCodes.Rem); break;
            case Operation.Equals: Emit(OpCodes.Ceq); break;
            case Operation.GreaterThan: Emit(OpCodes.Cgt); break;
            case Operation.GreaterThanEquals:
            {
                Emit(OpCodes.Clt);
                LoadInt(0, false);
                Emit(OpCodes.Ceq);
                break;
            }
            case Operation.LesserThan: Emit(OpCodes.Clt); break;
            case Operation.LesserThanEquals:
            {
                Emit(OpCodes.Cgt);
                LoadInt(0, false);
                Emit(OpCodes.Ceq);
                break;
            }
            case Operation.Not: Emit(OpCodes.Not); break;
            case Operation.Xor: Emit(OpCodes.Xor); break;
            default: throw new Exception("Invalid math-op");
        }
    }

    public void BranchIf(bool state, string label)
    {
        if (state) Emit(OpCodes.Brtrue, label);
        else Emit(OpCodes.Brfalse, label);
    }

    public void BranchIf(bool state, Segment segment) => BranchIf(state, segment._name);

    public void Branch(string label)
    {
        Emit(OpCodes.Br, label);
    }

    public void Branch(Segment segment) => Branch(segment._name);
    
    public void Loop() => Branch(this);
    
    public int InitVar(string name, TypeEm typeEm)
    {
        Function.LocalVariables.Add((name, typeEm));
        return Function.LocalVariables.Count - 1;
    }

    public void LoadLoc(int n)
    {
        Function.IncStackSize();
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
        stack.Add(funcAttrs.TypeEm.Get());
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

    public void Pop() => Emit(OpCodes.Pop);
}