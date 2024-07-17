using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace LIA;

public class FunctionGen
{
    private string _head;
    private List<string> _instructions = new List<string>();
    private List<(string, Type)> _localVariables = new List<(string, Type)>();

    // Track stack size to dynamically set 'maxstack'
    private int _stacksize = 0;
    private int _maxstacksize = 0;
    
    public FunctionGen(FunctionAttributes attrs)
    {
        _head = attrs.Generate();
    }

    private void IncStackSize(int v = 1)
    {
        _stacksize += v;
        if (_stacksize > _maxstacksize) _maxstacksize = _stacksize;
    }

    private void DecStackSize(int v = 1) => _stacksize -= v;

    public string Get()
    {
        string total = ".method " + _head + " { ";
        total += $"\n  .maxstack {_stacksize}";
        if (_instructions.Count != 0 || _localVariables.Count != 0) total += "\n";
        var locals = new List<string>();
        if (_localVariables.Count != 0) locals.Add(".locals init (");
        foreach (var local in _localVariables)
        {
            locals.Add($"  {local.Item2.Get()} {local.Item1}");
        }
        if (_localVariables.Count != 0) locals.Add(")");
        foreach (var instruction in locals.Concat(_instructions))
        {
            total += "  " + instruction + "\n";
        }
        total += "}";
        return total;
    }

    private void AppendRaw(string operation) => _instructions.Add(operation);
    public void AddComment(string comment) => _instructions[^1] += ("    // " + comment);
    
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
        IncStackSize();
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
        IncStackSize();
        if (longform) Emit(OpCodes.Ldc_I8, num);
        else Emit(OpCodes.Ldc_I4, num);
    }

    public void Discard() {
        DecStackSize();
        Emit(OpCodes.Pop);
    }

    public void DiscardArg(int arg)
    {
        LoadArg(arg);
        Discard();
    }

    public void LoadFloat(double num, bool longform)
    {
        IncStackSize();
        if (longform) Emit(OpCodes.Ldc_R8, num);
        else Emit(OpCodes.Ldc_R4, num);
    }

    public void SetMaxStack(int maxstack) => _stacksize = _maxstacksize = maxstack;

    public void EntryPoint() => AppendRaw(".entrypoint");

    public void Ret() => Emit(OpCodes.Ret);
    public void Dup()
    {
        IncStackSize();
        Emit(OpCodes.Dup);
    }

    public void StoreLoc(int n)
    {
        DecStackSize();
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
        IncStackSize();
        Emit(OpCodes.Ldstr, str);
    }

    public void StoreArg(int n)
    {
        DecStackSize();
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
        DecStackSize();
        Emit(ConvMathOpCode(operation));
    }
    public void PerformOp(CmpOps operation)
    {
        DecStackSize();
        Emit(ConvCmpOpCode(operation, false));
    }

    public void PerformOpBranch(CmpOps cmpOps, string branchLabel)
    {
        DecStackSize(2);
        Emit(ConvCmpOpCode(cmpOps, true), branchLabel);
    }

    public int InitVar(string name, Type type)
    {
        _localVariables.Add((name, type));
        return _localVariables.Count - 1;
    }

    public void LoadLoc(int n)
    {
        IncStackSize();
        switch (n)
        {
            case 0: Emit(OpCodes.Ldloc_0); break;
            case 1: Emit(OpCodes.Ldloc_1); break;
            case 2: Emit(OpCodes.Ldloc_2); break;
            case 3: Emit(OpCodes.Ldloc_3); break;
            default: Emit(OpCodes.Ldloc, n); break;
        }
    }

    public void Label(string name) => AppendRaw($"{name}:");

    public void Call(FunctionAttributes funcAttrs)
    {
        List<string> stack = new List<string>();
        List<string> args = new List<string>();
        
        stack.Add("call");
        if (funcAttrs.Library != null)
        {
            stack.Add($"[{funcAttrs.Library}]");
        }
        stack.Add(funcAttrs.Type.Get());
        stack.Add($"{funcAttrs.Namespace}::{funcAttrs.Name}");

        foreach (var argument in funcAttrs.Arguments)
        {
            args.Add(argument.Item2.Get());
        }
        
        stack.Add($"({string.Join(", ", args)})");
        
        AppendRaw(string.Join(" ", stack));
    }
}