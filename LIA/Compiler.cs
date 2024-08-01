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

public class Compiler
{
    public Compiler(CodeFile codeFile)
    {
        _codeFile = codeFile;
        _globalDeclarations = new Dictionary<string, ClassGen>
        {
            {
                "i32", BuiltinNameSpace.SpawnClass(true, "int32", "i32")
            },
            {
                "i64", BuiltinNameSpace.SpawnClass(true, "int64", "i64")
            },
            {
                "f32", BuiltinNameSpace.SpawnClass(true, "float32", "f32")
            },
            {
                "f64", BuiltinNameSpace.SpawnClass(true, "float64", "f64")
            },
            {
                "string", BuiltinNameSpace.SpawnClass(true, "string", "string")
            },
            {
                "void", BuiltinNameSpace.SpawnClass(true, "void", "void")
            }
        };

        foreach (var classGen in new List<ClassGen> {_globalDeclarations["i32"], _globalDeclarations["i64"], _globalDeclarations["f32"], _globalDeclarations["f64"]})
        {
            foreach (var operation in new List<Operation> {Operation.Add, Operation.Sub, Operation.Mul, Operation.Div, 
                         Operation.Rem, Operation.Xor, Operation.Equals, Operation.Not, Operation.GreaterThan, 
                         Operation.LesserThan, Operation.GreaterThanEquals, Operation.LesserThanEquals, 
                         Operation.IsFalse, Operation.IsTrue}) {
                var thisTypeEm = new TypeEm(new RealType(classGen));
                classGen.SpawnFunction(Operation2ClassMethod(operation), false, true, true, thisTypeEm, new List<(string, TypeEm)>
                {("a", thisTypeEm), ("b", thisTypeEm)}, true).SpawnSegment("Operation!").PerformOp(operation);  // Name this segment like this so it will throw an error if it is used
            }
        }
    }

    private CodeFile _codeFile;
    
    private int _miscInt = 0;
    private string Cond => $"Cond_{_miscInt++}";
    private string AfterCond => $"AfterCond_{_miscInt++}";
    private NameSpace BuiltinNameSpace = new NameSpace("Sys", true);

    private Dictionary<string, ClassGen> _globalDeclarations;
    private List<NameSpace> _nameSpaces = new List<NameSpace>() {new NameSpace("Program")};
    private NameSpace CurrentNameSpace => _nameSpaces[^1];
    
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

    private void ThrowUnimplementedClassMethod(BinaryExpr binaryExpr, RealType realType, Operation operation) =>
        ThrowError(binaryExpr, $"The type '{realType.ClassAttributes.CoverName}' does not implement the operation {operation}", ErrorCodes.UnimplementedClassMethod);
    
    private TypeEm ConvertType(IdentifierExpr identifierExpr)
    {
        var rawConvert = RawConvertType(identifierExpr.Name);
        if (rawConvert != null) return rawConvert;
        ThrowInvalidTypeError(identifierExpr);
        throw new Exception("Failed to Exit");
    }

    private TypeEm? RawConvertType(string name)
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
        
        if (_globalDeclarations.TryGetValue(name, out var type)) return new TypeEm(type.GetRealType, isRef, goesOut);
        if (CurrentNameSpace.Classes.TryGetValue(name, out var type2)) return new TypeEm(type2.Item2.GetRealType, isRef, goesOut);
        return null;
    }

    private List<(string, TypeEm)> ConvertParameters(List<ParameterExpr> parameters)
    {
        List<(string, TypeEm)> newParams = new List<(string, TypeEm)>();
        foreach (var param in parameters)
        {
            newParams.Add(((param.Name.Name), ConvertType(param.Type)));
        }

        return newParams;
    }
    
    private void ParseNameSpace(NamespaceDecl namespaceDecl) =>
        _nameSpaces.Add(new NameSpace(namespaceDecl.Name));

    private void ParseField(FieldStmt fieldStmt, ClassGen classGen)
    {
        Field field = new Field(ConvertType(fieldStmt.Type).RealType, fieldStmt.Name.Name,
            fieldStmt.IsStatic, fieldStmt.IsPublic, fieldStmt.Value);
        classGen.AddField(field);
    }

    private void ParseClass(ClassDecl classDecl)
    {
        var classGen = CurrentNameSpace.SpawnClass(classDecl.Public, classDecl.Name);

        foreach (var field in classDecl.Field)
        {
            ParseField(field, classGen);
        }

        foreach (var func in classDecl.Methods)
        {
            ParseFunction(func, classGen.SpawnFunction(func.Name.Name, func.Static, func.Public, func.Class, ConvertType(func.ReturnType), ConvertParameters(func.Parameters), false));
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
            case TokenType.DoubleEquals: return Operation.Equals;
            case TokenType.GreaterThan: return Operation.GreaterThan;
            case TokenType.GreaterThanEquals: return Operation.GreaterThanEquals;
            case TokenType.LessThan: return Operation.LesserThan;
            case TokenType.ExclamationMark: return Operation.Not;
            case TokenType.As: return Operation.Cast;
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
            if (!segment.Function.Class.Functions.ContainsKey(((FunctionCallExpr)expr).Name.Name)) ThrowUnknownFunctionError((FunctionCallExpr)expr);
            foreach (var argument in ((FunctionCallExpr)expr).Arguments!)
            {
                PutExprOnStack(argument, segment, localsLookup);
            }
            segment.Call(segment.Function.Class.Functions[((FunctionCallExpr)expr).Name.Name].Item1);
        }
        else if (expr.GetType() == typeof(BinaryExpr))
        {
            var leftType = InferExprType(((BinaryExpr) expr).Left, segment.Function, localsLookup);
            var convOpR = ConvertOperation(((BinaryExpr) expr).Operator);
            var convOp = Operation2ClassMethod(convOpR);
            if (!leftType.RealType.ClassGen.ClassMethodAccess.TryGetValue(convOp, out var funcName))
                ThrowUnimplementedClassMethod((BinaryExpr) expr, leftType.RealType, convOpR);
            var func = leftType.RealType.ClassGen.Functions[funcName!];
            PutExprOnStack(((BinaryExpr)expr).Left, segment, localsLookup);
            PutExprOnStack(((BinaryExpr)expr).Right, segment, localsLookup);
            func.Item2.CreateOpConstructor().AddToSegment(segment);
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
        else if (expr.GetType() == typeof(CastExpr))
        {
            PutExprOnStack(((CastExpr) expr).PrevExpr, segment, localsLookup);
            segment.PerformCast(ConvertType(((CastExpr) expr).DestType));
        }
        else if (expr.GetType() == typeof(IdentifierExpr))
        {
            var value = localsLookup.Get(((IdentifierExpr)expr).Name);
            if (value == -1) ThrowUnknownVariableError((IdentifierExpr)expr);
            segment.LoadLoc(value);
        }
        else throw new Exception("Parser fucked up or I did, idk");
    }

    public string Operation2ClassMethod(Operation operation)
    {
        switch (operation)
        {
            case Operation.Add: return "opadd";
            case Operation.Sub: return "opsub";
            case Operation.Mul: return "opmul";
            case Operation.Div: return "opdiv";
            case Operation.GreaterThan: return "opgreater";
            case Operation.GreaterThanEquals: return "opgreaterequals";
            case Operation.LesserThan: return "oplesser";
            case Operation.LesserThanEquals: return "oplesserequals";
            case Operation.Not: return "opnot";
            case Operation.Rem: return "oprem";
            case Operation.Xor: return "opxor";
            case Operation.IsFalse: return "opfalse";
            case Operation.IsTrue: return "optrue";
            case Operation.Equals: return "opequals";
            default: throw new Exception($"invalid op {operation}");
        }
    }
    
    private TypeEm InferExprType(Expr expr, FunctionGen functionGen, LocalsLookup localsLookup)
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
        if (expr.GetType() == typeof(BinaryExpr))
        {
            var leftType = InferExprType(((BinaryExpr) expr).Left, functionGen, localsLookup);
            var convOpR = ConvertOperation(((BinaryExpr) expr).Operator);
            var convOp = Operation2ClassMethod(convOpR);
            if (!leftType.RealType.ClassGen.ClassMethodAccess.TryGetValue(convOp, out var funcName))
                ThrowUnimplementedClassMethod((BinaryExpr) expr, leftType.RealType, convOpR);
            var func = leftType.RealType.ClassGen.Functions[funcName!];
            return func.Item1.TypeEm;
        }
        if (expr.GetType() == typeof(FunctionCallExpr))
        {
            if (functionGen.Class.Functions.ContainsKey(((FunctionCallExpr)expr).Name.Name))
                return functionGen.Class.Functions[((FunctionCallExpr)expr).Name.Name].Item1.TypeEm;
            ThrowUnknownFunctionError((FunctionCallExpr)expr);
        }
        if (expr.GetType() == typeof(CastExpr))
        {
            return ConvertType(((CastExpr) expr).DestType);
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
                loopSegment.BranchIf(false, afterSegment);
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
                segment.BranchIf(true, thenSegment);
                ParseBody(functionDecl, functionGen, thenSegment, localsLookup, ifStmt.ThenBranch);
                thenSegment.Branch(afterSegment);

                if (ifStmt.ElifBranches != null)
                {
                    foreach (var elifBranch in ifStmt.ElifBranches)
                    {
                        Segment elifSegment = functionGen.SpawnSegment(Cond);
                        PutExprOnStack(elifBranch.Item1, segment, localsLookup);
                        segment.BranchIf(true, elifSegment);
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
        
        if (GlobalContext.RequireMainDefinition && !_mainDefined && !GlobalContext.CompilationOptions.DisableWarningMainNotDefined) Errors.Warning(WarningCodes.MainNotDefined, 
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

    public string WriteToPath(string path) => Utils.WriteStringTo(path, Get());
    public string GetNewPath(string ext = "il") => Path.GetFileNameWithoutExtension(_codeFile.Filepath) + "." + ext;
}