namespace LIA
{
    public class Lexer
    {
        private string _code;
        private int _counter;
        public List<Token> Tokens { get; private set; }
        public CodeFile CodeFile { get; private set; }

        public Lexer(CodeFile codeFile)
        {
            CodeFile = codeFile;
            _code = codeFile.Text;
            _counter = 0;
            Tokens = new List<Token>();
        }

        private char CurrentChar => _counter < _code.Length ? _code[_counter] : '\0';

        private void Advance() => _counter++;

        private void AddToken(TokenType type, string content, int startPos, int endPos) =>
            Tokens.Add(new Token(type, content, this, startPos, endPos));

        private void AddSingleToken(TokenType type) =>
            AddToken(type, _code[_counter - 1].ToString(), _counter - 1, _counter);
        
        private void AddDoubleToken(TokenType type) =>
            AddToken(type, _code.Substring(_counter - 2, 2), _counter - 2, _counter);

        private void LexNumber()
        {
            int startPos = _counter - 1;
            bool isDot = false;
            while (char.IsDigit(CurrentChar) || CurrentChar == '.')
            {
                Advance();
                if (CurrentChar == '.' && isDot)
                {
                    _counter--;
                    break;
                };
                isDot = CurrentChar == '.';
            }
            AddToken(TokenType.Number, _code.Substring(startPos, _counter - startPos), startPos, _counter);
        }

        public void LexComment()
        {
            bool longComment = CurrentChar == '*';
            while (_counter < _code.Length)
            {
                char c = _code[_counter++];
                if (!longComment && c == '\n') break;
                if (longComment && c == '*')
                {
                    if (CurrentChar == '#')
                        break;
                }
            }
        }

        private void LexIdentifier()
        {
            int startPos = _counter - 1;
            while (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_')
                Advance();
            string identifier = _code.Substring(startPos, _counter - startPos);
            TokenType type = identifier switch
            {
                "public" => TokenType.Public,
                "private" => TokenType.Private,
                "class" => TokenType.Class,
                "def" => TokenType.Def,
                "return" => TokenType.Return,
                "if" => TokenType.If,
                "else" => TokenType.Else,
                "elif" => TokenType.Elif,
                "while" => TokenType.While,
                "namespace" => TokenType.Namespace,
                _ => TokenType.Identifier
            };
            AddToken(type, identifier, startPos, _counter);
        }

        private void LexString()
        {
            int startPos = _counter;
            Advance(); // Skip the opening quote
            while (CurrentChar != '"' && CurrentChar != '\0')
                Advance();
            Advance(); // Skip the closing quote
            AddToken(TokenType.String, _code.Substring(startPos, _counter - startPos), startPos, _counter);
        }
        
        public void Lex()
        {
            while (_counter < _code.Length)
            {
                char c = _code[_counter++];
                switch (c)
                {
                    case '+': AddSingleToken(TokenType.Plus); break;
                    case '-': AddSingleToken(TokenType.Minus); break;
                    case '*': AddSingleToken(TokenType.Star); break;
                    case '/': AddSingleToken(TokenType.Slash); break;
                    case ':': AddSingleToken(TokenType.Colon); break;
                    case ';': AddSingleToken(TokenType.Semicolon); break;
                    case '(': AddSingleToken(TokenType.OpenParen); break;
                    case ')': AddSingleToken(TokenType.CloseParen); break;
                    case '.': AddSingleToken(TokenType.Dot); break;
                    case ',': AddSingleToken(TokenType.Comma); break;
                    case '|': AddSingleToken(TokenType.Or); break;
                    case '!': {
                        if (_code[_counter + 1] == '=') AddDoubleToken(TokenType.NotEquals);
                        else AddSingleToken(TokenType.ExclamationMark);
                        break;
                    }
                    case '=':
                    {
                        if (_code[_counter + 1] == '=') AddDoubleToken(TokenType.DoubleEquals);
                        else AddSingleToken(TokenType.Equals);
                        break;
                    }
                    case '&': AddSingleToken(TokenType.And); break;
                    case '<': {
                        if (_code[_counter + 1] == '=') AddDoubleToken(TokenType.LessThanEquals);
                        else AddSingleToken(TokenType.LessThan);
                        break;
                    }
                    case '>': {
                        if (_code[_counter + 1] == '=') AddDoubleToken(TokenType.GreaterThanEquals);
                        else AddSingleToken(TokenType.GreaterThan);
                        break;
                    }
                    case '"': LexString(); break;
                    case '#': LexComment(); break;
                    default:
                        if (char.IsWhiteSpace(c))
                            continue;
                        if (char.IsDigit(c))
                            LexNumber();
                        else if (char.IsLetter(c) || c == '_')
                            LexIdentifier();
                        else
                            throw new Exception($"Unexpected character: {c}");
                        break;
                }
            }
        }
    }

    public enum TokenType
    {
        Plus,
        Minus,
        Star,
        Slash,
        Equals,
        ExclamationMark,
        String,
        Number,
        Public,
        Private,
        Class,
        Def,
        Comma,
        OpenParen,
        CloseParen,
        Colon,
        Semicolon,
        Identifier,
        Return,
        If,
        Else,
        Elif,
        While,
        GreaterThan,
        LessThan,
        GreaterThanEquals,
        LessThanEquals,
        NotEquals,
        DoubleEquals,
        Dot,
        And,
        Or,
        Namespace,
        // not real
        Statement,
        ExpressionLike,
        Operand,
        EndOfFile,
        Token,
        TopLevel,
        ClassLevel
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Content { get; }
        public Lexer Lexer { get; }
        public int StartPos { get; }
        public int EndPos { get; }

        public Token(TokenType type, string content, Lexer lexer, int startPos, int endPos)
        {
            Type = type;
            Content = content;
            Lexer = lexer;
            StartPos = startPos;
            EndPos = endPos;
        }
    }
}
