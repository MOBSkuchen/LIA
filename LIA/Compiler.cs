using System.Reflection.Emit;

namespace LIA;

public class LocalsLookup
{
    public Dictionary<string, int> RawLocalsLookup = new Dictionary<string, int>();
    public int Current => RawLocalsLookup.Count - 1;

    public int Add(string local)
    {
        int c = RawLocalsLookup.Count;
        RawLocalsLookup.Add(local, c);
        return c;
    }

    public int Get(string local) => RawLocalsLookup.ContainsKey(local) ? RawLocalsLookup[local] : -1;
}

public class Compiler(CodeFile codeFile)
{
    private CodeFile _codeFile = codeFile;

    private int _miscInt = 0;
    private string Cond => $"Cond_{_miscInt++}";
    private string AfterCond => $"AfterCond_{_miscInt++}";
    
    private List<NameSpace> _nameSpaces = new List<NameSpace>() {new NameSpace("Program")};
    private Dictionary<string, string> _declaredTypes = new Dictionary<string, string>()
    {
        {"i32", "int32"}, 
        {"f32", "float32"}, 
        {"i64", "int64"}, 
        {"f64", "float64"}, 
        {"void", "void"}, 
        {"none", "void"}, 
        {"string", "string"}, 
    };

    private Dictionary<string, FunctionAttributes> _callables = new Dictionary<string, FunctionAttributes>();
    
    private bool _mainDefined = false;

    private CodeLocation GenCodeLocForNode(AstNode token) => new CodeLocation(token.StartPos, token.EndPos, _codeFile);

    private void ThrowError(AstNode token, string message, ErrorCodes errorCode) =>
        Errors.ThrowCodeError(GenCodeLocForNode(token), message, errorCode);

    private void ThrowInvalidTypeError(IdentifierExpr token) => ThrowError(token, $"Expected a valid type, got '{token.Name}'", ErrorCodes.InvalidType);
    private void ThrowTypeConflictError(IdentifierExpr token, string rightType) => ThrowError(token, $"Type mismatch! '{token.Name}' should be of the same type as '{rightType}'", ErrorCodes.TypeConflict);
    private void ThrowTypeConflictFunctionError(FunctionDecl functionDecl, Expr exprToken) => ThrowError(exprToken,
        $"The function '{functionDecl.Name.Name}' must return type {functionDecl.ReturnType.Name}", ErrorCodes.TypeConflict);
    private void ThrowUnknownVariableError(IdentifierExpr token) => ThrowError(token,
        $"The variable '{token.Name}' is not defined in this context", ErrorCodes.UnknownVariable);

    private void ThrowUnknownFunctionError(FunctionCallExpr functionCall) => ThrowError(functionCall.Name,
        $"The function '{functionCall.Name.Name}' was not found or could not be acessed in this context!",
        ErrorCodes.UnknownFunction);
    
    private Type ConvertType(IdentifierExpr identifierExpr)
    {
        var rawConvert = RawConvertType(identifierExpr.Name);
        if (rawConvert != null) return rawConvert;
        ThrowInvalidTypeError(identifierExpr);
        throw new Exception("Failed to Exit");
    }

    private Type? RawConvertType(string name)
    {
        bool isRef = false;
        bool goesOut = false;
        if (name.EndsWith("_"))
        {
            isRef = true;
            name = name.Substring(0, name.Length - 1);
        }
        if (name.StartsWith("_"))
        {
            goesOut = true;
            name = name.Substring(1, name.Length - 1);
        }

        if (_declaredTypes.TryGetValue(name, out var type)) return new Type(type, isRef, goesOut);
        return null;
    }

    private List<(string, Type)> ConvertParameters(List<ParameterExpr> parameters)
    {
        List<(string, Type)> newParams = new List<(string, Type)>();
        foreach (var param in parameters)
        {
            newParams.Add(((param.Name.Name), ConvertType(param.Type)));
        }

        return newParams;
    }
    
    private void ParseNameSpace(NamespaceDecl namespaceDecl) =>
        _nameSpaces.Add(new NameSpace(namespaceDecl.Name));

    private void ParseClass(ClassDecl classDecl)
    {
        var nameSpace = _nameSpaces[^1];
        var classGen = nameSpace.SpawnClass(classDecl.Public, classDecl.Name);

        foreach (var func in classDecl.Methods)
        {
            _callables.Add(func.Name.Name, new FunctionAttributes(_nameSpaces[^1].Name, classDecl.Name, func.Name.Name, true, func.Public, ConvertType(func.ReturnType), ConvertParameters(func.Parameters)));
        }
        
        foreach (var func in classDecl.Methods)
        {
            ParseFunction(func, classGen.SpawnFunction(func.Name.Name, true, func.Public, ConvertType(func.ReturnType), ConvertParameters(func.Parameters)));
        }
    }

    private Operation ConvertOperation(TokenType operand)
    {
        switch (operand)
        {
            case TokenType.Plus: return Operation.Add;
            case TokenType.Slash: return Operation.Div;
            case TokenType.Star: return Operation.Mul;
            case TokenType.Minus: return Operation.Sub;
            case TokenType.Equals: return Operation.Equals;
            case TokenType.GreaterThan: return Operation.GreaterThan;
            case TokenType.GreaterThanEquals: return Operation.GreaterThanEquals;
            case TokenType.LessThan: return Operation.LesserThan;
            case TokenType.LessThanEquals: return Operation.LesserThanEquals;
            default: throw new Exception("oops");
        }
    }

    private void PutExprOnStack(Expr expr, Segment segment, LocalsLookup localsLookup)
    {
        if (expr.GetType() == typeof(FloatExpr))
            segment.LoadFloat(((FloatExpr)expr).Value, ((FloatExpr)expr).Value > float.MaxValue);
        else if (expr.GetType() == typeof(IntegerExpr))
            segment.LoadInt(((IntegerExpr)expr).Value, ((IntegerExpr)expr).Value > int.MaxValue);
        else if (expr.GetType() == typeof(StringExpr))
            segment.LoadString(((StringExpr)expr).Value);
        else if (expr.GetType() == typeof(FunctionCallExpr))
        {
            if (!_callables.ContainsKey(((FunctionCallExpr)expr).Name.Name)) ThrowUnknownFunctionError((FunctionCallExpr)expr);
            foreach (var argument in ((FunctionCallExpr)expr).Arguments!)
            {
                PutExprOnStack(argument, segment, localsLookup);
            }
            segment.Call(_callables[((FunctionCallExpr)expr).Name.Name]);
        }
        else if (expr.GetType() == typeof(BinaryExpr))
        {
            PutExprOnStack(((BinaryExpr)expr).Left, segment, localsLookup);
            PutExprOnStack(((BinaryExpr)expr).Right, segment, localsLookup);
            segment.PerformOp(ConvertOperation(((BinaryExpr)expr).Operator));
        }
        else if (expr.GetType() == typeof(UnaryExpr))
        {
            PutExprOnStack(((UnaryExpr)expr).Operand, segment, localsLookup);
            if (((UnaryExpr)expr).Operator == TokenType.ExclamationMark) segment.PerformOp(Operation.Not);
            if (((UnaryExpr)expr).Operator == TokenType.Minus)
            {
                segment.LoadInt(1, false);
                segment.PerformOp(Operation.Sub);
            }
        }
        else if (expr.GetType() == typeof(IdentifierExpr))
        {
            var value = localsLookup.Get(((IdentifierExpr)expr).Name);
            if (value == -1) ThrowUnknownVariableError((IdentifierExpr)expr);
            segment.LoadLoc(value);
        }
        else throw new Exception("Parser fucked up or I did, idk");
    }
    
    private Type InferExprType(Expr expr, FunctionGen functionGen, LocalsLookup localsLookup)
    {
        if (expr.GetType() == typeof(FloatExpr))
            return (((FloatExpr)expr).Value > float.MaxValue
                ? RawConvertType("f64")
                : RawConvertType("f32"))!;
        if (expr.GetType() == typeof(IntegerExpr))
            return (((IntegerExpr)expr).Value > int.MaxValue 
                ? RawConvertType("i64")
                : RawConvertType("i32"))!;
        if (expr.GetType() == typeof(StringExpr))
            return RawConvertType("string")!;
        if (expr.GetType() == typeof(UnaryExpr))
            return InferExprType(((UnaryExpr)expr).Operand, functionGen, localsLookup);
        if (expr.GetType() == typeof(BinaryExpr)) return InferExprType(((BinaryExpr)expr).Left, functionGen, localsLookup);
        if (expr.GetType() == typeof(FunctionCallExpr))
        {
            if (_callables.ContainsKey(((FunctionCallExpr)expr).Name.Name))
                return _callables[((FunctionCallExpr)expr).Name.Name].Type;
            ThrowUnknownFunctionError((FunctionCallExpr)expr);
        }
        if (expr.GetType() == typeof(IdentifierExpr))
        {
            var value = localsLookup.Get(((IdentifierExpr)expr).Name);
            if (value == -1) ThrowUnknownVariableError((IdentifierExpr)expr);
            return functionGen.GetLocalVariable(((IdentifierExpr)expr).Name)!.Value.Item2;
        }
        throw new Exception("Parser fucked up or I did, idk");
    }

    private void ParseBody(FunctionDecl functionDecl, FunctionGen functionGen, Segment segment, LocalsLookup localsLookup, BlockStmt? body = null)
    {
        if (body == null) body = functionDecl.Body;
        for (var index = 0; index < body.Statements.Count; index++)
        {
            var statement = body.Statements[index];
            var statementType = statement.GetType();

            if (statementType == typeof(AssignmentStmt))
            {
                AssignmentStmt assignmentStmt = (AssignmentStmt)statement;
                if (assignmentStmt.Value != null) PutExprOnStack(assignmentStmt.Value, segment, localsLookup);
                var type = assignmentStmt.Type == null
                    ? InferExprType(assignmentStmt.Value!, functionGen, localsLookup)
                    : ConvertType(assignmentStmt.Type);
                var storeLoc = localsLookup.Get(assignmentStmt.Name.Name);
                if (storeLoc == -1)
                {
                    segment.InitVar(assignmentStmt.Name.Name, type);
                    localsLookup.Add(assignmentStmt.Name.Name);
                    storeLoc = localsLookup.Current;
                }
                else
                {
                    var localV = functionGen.GetLocalVariable(assignmentStmt.Name.Name)!;
                    if (localV.Value.Item2.Get() != type.Get())
                        ThrowTypeConflictError(assignmentStmt.Name, localV.Value.Item1);
                }
                if (assignmentStmt.Value != null) segment.StoreLoc(storeLoc);
            } 
            else if (statementType == typeof(ReturnStmt))
            {
                ReturnStmt returnStmt = (ReturnStmt)statement;
                PutExprOnStack(returnStmt.Value, segment, localsLookup);
                var type = InferExprType(returnStmt.Value, functionGen, localsLookup);
                if (ConvertType(functionDecl.ReturnType).Get() != type.Get()) ThrowTypeConflictFunctionError(functionDecl, returnStmt.Value);
                segment.Ret();
            }
            else if (statementType == typeof(WhileLoop))
            {
                WhileLoop whileLoop = (WhileLoop)statement;
                Segment loopSegment = functionGen.SpawnSegment(Cond);
                Segment afterSegment = functionGen.SpawnSegment(AfterCond);
                PutExprOnStack(whileLoop.Condition, loopSegment, localsLookup);
                loopSegment.PerformOpBranch(Operation.IsFalse, afterSegment);
                ParseBody(functionDecl, functionGen, loopSegment, localsLookup, whileLoop.Body);
                loopSegment.Loop();
                segment = afterSegment;
            }
            else if (statementType == typeof(IfStmt))
            {
                IfStmt ifStmt = (IfStmt)statement;

                Segment afterSegment = functionGen.SpawnSegment(AfterCond);

                Segment thenSegment = functionGen.SpawnSegment(Cond);
                PutExprOnStack(ifStmt.Condition, segment, localsLookup);
                segment.PerformOpBranch(Operation.IsTrue, thenSegment);
                ParseBody(functionDecl, functionGen, thenSegment, localsLookup, ifStmt.ThenBranch);
                thenSegment.Branch(afterSegment);

                if (ifStmt.ElifBranches != null)
                {
                    foreach (var elifBranch in ifStmt.ElifBranches)
                    {
                        Segment elifSegment = functionGen.SpawnSegment(Cond);
                        PutExprOnStack(elifBranch.Item1, segment, localsLookup);
                        segment.PerformOpBranch(Operation.IsTrue, elifSegment);
                        ParseBody(functionDecl, functionGen, elifSegment, localsLookup, elifBranch.Item2);
                        elifSegment.Branch(afterSegment);
                    }
                }

                segment = afterSegment;
                
                if (ifStmt.ElseBranch != null)
                {
                    ParseBody(functionDecl, functionGen, segment, localsLookup, ifStmt.ElseBranch);
                }
            } else if (statementType == typeof(ExprStmt))
            {
                ExprStmt exprStmt = (ExprStmt)statement;
                var useless = exprStmt.Expression.GetType() != typeof(FunctionCallExpr);
                if (useless) Errors.CodeWarning(GenCodeLocForNode(exprStmt), WarningCodes.UselessCode, "This code is useless, it does not do anything");
                PutExprOnStack(exprStmt.Expression, segment, localsLookup);
                segment.Pop();
            }
        }
    }

    private void ParseFunction(FunctionDecl functionDecl, FunctionGen functionGen)
    {
        if (functionDecl.Name.Name.ToLower() == "main")
        {
            _mainDefined = true;
            functionGen.EntryPoint();
        }

        LocalsLookup localsLookup = new LocalsLookup();
        Segment segment = functionGen.SpawnStartSegment();
        
        foreach (var parameter in functionDecl.Parameters)
        {
            segment.InitVar(parameter.Name.Name, ConvertType(parameter.Type));
            localsLookup.Add(parameter.Name.Name);
            segment.LoadArg(localsLookup.Current);
            segment.StoreLoc(localsLookup.Current);
        }

        ParseBody(functionDecl, functionGen, segment, localsLookup);
    }

    public void ParseTopLevel(List<Stmt> topLevelStatements)
    {
        foreach (var topLevelStatement in topLevelStatements)
        {
            if (topLevelStatement.GetType() == typeof(NamespaceDecl)) ParseNameSpace((NamespaceDecl) topLevelStatement);
            else if (topLevelStatement.GetType() == typeof(ClassDecl)) ParseClass((ClassDecl) topLevelStatement);
        }
        
        if (!_mainDefined) Errors.Warning(WarningCodes.MainNotDefined, 
            "Function 'main' (public static, public class) is not defined! This Program will not run!");
    }

    public string Get()
    {
        if (_nameSpaces[0].Classes.Count == 0) _nameSpaces.RemoveAt(0);
        string total = "";
        foreach (var nameSpace in _nameSpaces)
        {
            total += nameSpace.Get() + "\n";
        }

        return total;
    }

    public void WriteToFile(string path) => File.WriteAllText(path, Get());
}