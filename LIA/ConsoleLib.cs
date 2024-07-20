namespace LIA;

public static class ConsoleLib
{
    private const string Escape = "\u001B[";
    
    // Text Styles
    public const int TextDefault = 0;
    public const int TextBold = 1;
    public const int TextLight = 2;
    public const int TextItalic = 3;
    public const int TextUnderline = 4;

    // Foreground Colors
    public const int FgBlack = 30;
    public const int FgRed = 31;
    public const int FgGreen = 32;
    public const int FgYellow = 33;
    public const int FgBlue = 34;
    public const int FgMagenta = 35;
    public const int FgCyan = 36;
    public const int FgWhite = 37;
    public const int FgDefault = 39;
    public const int FgBrightBlack = 90;
    public const int FgBrightRed = 91;
    public const int FgBrightGreen = 92;
    public const int FgBrightYellow = 93;
    public const int FgBrightBlue = 94;
    public const int FgBrightMagenta = 95;
    public const int FgBrightCyan = 96;
    public const int FgBrightWhite = 97;

    // Background Colors
    public const int BgBlack = 40;
    public const int BgRed = 41;
    public const int BgGreen = 42;
    public const int BgYellow = 43;
    public const int BgBlue = 44;
    public const int BgMagenta = 45;
    public const int BgCyan = 46;
    public const int BgWhite = 47;
    public const int BgDefault = 49;
    public const int BgBrightBlack = 100;
    public const int BgBrightRed = 101;
    public const int BgBrightGreen = 102;
    public const int BgBrightYellow = 103;
    public const int BgBrightBlue = 104;
    public const int BgBrightMagenta = 105;
    public const int BgBrightCyan = 106;
    public const int BgBrightWhite = 107;

    // Erase Functions
    public const int EraseCurToEnd = 0;
    public const int EraseCurToBeginning = 1;
    public const int EraseEntire = 2;
    public const int EraseSaved = 3;

    // Cursor Visibility
    private const string CursorVisible = "?25h";
    private const string CursorInvisible = "?25l";

    public static void Apply(string c)
    {
        Console.Write(c);
    }

    public static string MakeColor(int textType = 0, int foreground = 1, int background = 1)
    {
        return Escape + $"{textType};{foreground};{background}m";
    }

    public static void ApplyColor(int textType = 0, int foreground = 1, int background = 1)
    {
        Apply(MakeColor(textType, foreground, background));
    }

    public static string ResetColor(bool resetText = true, bool resetForeground = true, bool resetBackground = true)
    {
        return MakeColor(Convert.ToInt32(resetText), Convert.ToInt32(resetForeground), Convert.ToInt32(resetBackground));
    }

    public static void ApplyResetColor()
    {
        Apply(ResetColor());
    }

    private static string AsStyle(string str, string style, string reset)
    {
        return Escape + style + str + Escape + reset;
    }

    public static string AsBold(string str)
    {
        return AsStyle(str, "1m", "22m");
    }

    public static string AsDim(string str)
    {
        return AsStyle(str, "2m", "22m");
    }

    public static string AsUnderline(string str)
    {
        return AsStyle(str, "4m", "24m");
    }

    public static string AsBlinking(string str)
    {
        return AsStyle(str, "5m", "25m");
    }

    public static string AsItalic(string str)
    {
        return AsStyle(str, "3m", "23m");
    }

    public static string As(string text, int textType = 0, int foreground = 1, int background = 1)
    {
        return MakeColor(textType, foreground, background) + text + ResetColor();
    }

    public static string RgbColor(bool foreground, int r, int g, int b)
    {
        return Escape + (foreground ? "38" : "48") + $";2;{r};{g};{b}m";
    }

    public static void ApplyRgbColor(bool foreground, int r, int g, int b)
    {
        Apply(RgbColor(foreground, r, g, b));
    }

    public static string Erase(bool screen, int eraseCode)
    {
        return screen ? Escape + $"{eraseCode}J" : Escape + $"{eraseCode}K";
    }

    public static void ApplyErase(bool screen, int eraseCode)
    {
        Apply(Erase(screen, eraseCode));
    }

    public static string ExactMove(int line, int column)
    {
        return Escape + $"{line};{column}H";
    }

    public static string Move(int amount, string code)
    {
        return Escape + $"{amount}{code}";
    }

    public static void ApplyExactMove(int line, int column)
    {
        Apply(ExactMove(line, column));
    }

    public static void ApplyMove(int amount, string code)
    {
        Apply(Move(amount, code));
    }

    public static string Cursor(bool visible)
    {
        return Escape + (visible ? CursorVisible : CursorInvisible);
    }

    public static void ApplyCursor(bool visible)
    {
        Apply(Cursor(visible));
    }

    // Dummy implementation for terminal size
    public static (int, int) WindowSize()
    {
        return (Console.WindowWidth, Console.WindowHeight);
    }

    public static void PrintX(int amount, string ind)
    {
        for (int i = 0; i < amount; i++)
        {
            Console.Write(ind);
        }
    }

    public static void PrintRightBound(string text)
    {
        var (width, height) = WindowSize();
        int l = width - text.Length;
        PrintX(l, " ");
        Console.WriteLine(text);
    }

    public static void PrintRightBoundRanged(string text, int rangeWidth)
    {
        PrintX(rangeWidth - text.Length, " ");
        Console.WriteLine(text);
    }

    public static void PrintCenterBound(string text)
    {
        var (width, height) = WindowSize();
        PrintX(width / 2 - text.Length, " ");
        Console.WriteLine(text);
    }

    public static void PrintCenterBoundRanged(string text, int rangeWidth)
    {
        PrintX(rangeWidth / 2 - text.Length, " ");
        Console.WriteLine(text);
    }

    public static void PrintAsHeader(string text, int range)
    {
        int size = (range - (text.Length + 2)) / 2;
        PrintX(size, "-");
        Console.Write(" " + text + " ");
        PrintX(size, "-");
        Console.WriteLine();
    }

    public static List<string> SplitStringByNewline(string str)
    {
        var result = new List<string>();
        var lines = str.Split('\n');
        result.AddRange(lines);
        return result;
    }

    public static void LinedF(string text, Action<string> func)
    {
        foreach (var line in SplitStringByNewline(text))
        {
            func(line);
        }
    }
}
